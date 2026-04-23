Public Class Form1

    ' Farben
    Private ReadOnly xblue As Color = Color.FromArgb(255, 97, 180, 241)
    Private ReadOnly xgreen As Color = Color.FromArgb(255, 102, 246, 159)
    Private ReadOnly xorange As Color = Color.FromArgb(255, 244, 136, 30)
    Private ReadOnly xdarkgray As Color = Color.FromArgb(255, 38, 38, 38)

    Public ReadOnly _core As New NextronomeController()
    Private ReadOnly _basePath As String = "C:\KAVN\Applications\Accompaniment\Styles\"

    Private _lightEffect As Integer = 0

    Private _ledMap As Dictionary(Of String, PictureBox)
    Private _buttonMap As Dictionary(Of String, Control)

    ' ════════════════════════════════════════════════════════
    '  INITIALISIERUNG
    ' ════════════════════════════════════════════════════════

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitMaps()
        ResetBeatDisplay()

        Dim ledOk As Boolean = _core.Initialize(_basePath)
        If Not ledOk Then
            MessageBox.Show("Konnte keine Verbindung zu Logitech G HUB herstellen. Läuft G HUB im Hintergrund?")
        End If

        AddHandler _core.BeatTick, AddressOf OnBeatTick
        AddHandler _core.BarStart, AddressOf OnBarStart
        AddHandler _core.Stopped, AddressOf OnMetronomeStopped
        AddHandler _core.StyleLoaded, AddressOf OnStyleLoaded
        AddHandler _core.SlotSwitchReady, AddressOf OnSlotSwitchReady

        ' Gespeicherte Settings laden
        Dim savedStyle As String = My.Settings.Style1
        If Not String.IsNullOrEmpty(savedStyle) Then Style_Name.Text = savedStyle

        Dim savedStyle2 As String = My.Settings.Style2
        If Not String.IsNullOrEmpty(savedStyle2) Then Style2_Name.Text = savedStyle2

        ' Slot-2-Labels ohne Audio laden (inaktiver Slot)
        Dim s2 As String = Style2_Name.Text
        If Not String.IsNullOrEmpty(s2) Then
            Dim bpm2 As String = StyleLoader.ReadValue(_basePath & s2 & "\bpm.val", "-")
            Dim fam2 As String = StyleLoader.ReadValue(_basePath & s2 & "\family.word", "Unknown family")
            BPM2_Label.Text = If(bpm2 <> "-", "BPM " & bpm2, "BPM -")
            Family2_Name.Text = fam2
        End If

        ' Aktiven Slot (1) vollständig laden
        RefreshCurrentStyle()

        If Screen.AllScreens.Length > 1 Then
            Me.Location = Screen.AllScreens(1).WorkingArea.Location
        End If
        Me.WindowState = FormWindowState.Maximized
    End Sub

    Private Sub InitMaps()
        _ledMap = New Dictionary(Of String, PictureBox) From {
            {"Intro1", IntroLED1}, {"Intro2", IntroLED2}, {"Intro3", IntroLED3},
            {"Main1", MainLED1}, {"Main2", MainLED2}, {"Main3", MainLED3}, {"Main4", MainLED4},
            {"Break", BreakLED},
            {"Outro1", OutroLED1}, {"Outro2", OutroLED2}, {"Outro3", OutroLED3}
        }
        _buttonMap = New Dictionary(Of String, Control) From {
            {"Intro1", Intro1Button}, {"Intro2", Intro2Button}, {"Intro3", Intro3Button},
            {"Main1", Main1Button}, {"Main2", Main2Button}, {"Main3", Main3Button}, {"Main4", Main4Button},
            {"Break", BreakButton},
            {"Outro1", Outro1Button}, {"Outro2", Outro2Button}, {"Outro3", Outro3Button}
        }
    End Sub

    ' ════════════════════════════════════════════════════════
    '  STYLE LADEN
    ' ════════════════════════════════════════════════════════

    ''' <summary>Vollständiges Laden inkl. Audio – nur wenn Metronom NICHT läuft.</summary>
    Public Sub RefreshCurrentStyle()
        Dim styleName As String = If(_core.State.Active = 1, Style_Name.Text, Style2_Name.Text)
        If String.IsNullOrEmpty(styleName) Then Return

        Dim data As StyleData = _core.LoadStyle(styleName, _core.StyleData.BPM)
        UpdateStyleUI(data, _core.State.Active, styleName)
    End Sub

    ''' <summary>Nur UI-Controls aktualisieren – kein File-I/O, kein Audio-Laden.</summary>
    Private Sub UpdateStyleUI(data As StyleData, slot As Integer, styleName As String)
        If Not _core.MetronomMode Then
            Current_BPM.Text = data.BPM.ToString()
        End If

        Dim family As String = StyleLoader.ReadValue(_basePath & styleName & "\family.word", "Unknown family")
        If slot = 1 Then
            BPM_Label.Text = "BPM " & data.BPM.ToString()
            Family_Name.Text = family
        Else
            BPM2_Label.Text = "BPM " & data.BPM.ToString()
            Family2_Name.Text = family
        End If

        ' Buttons und LEDs nach verfügbaren Sections
        For Each kv In _ledMap
            If kv.Key = "Break" Then Continue For
            Dim available As Boolean = data.HasSection(kv.Key)
            kv.Value.BackColor = If(available, xblue, xdarkgray)
            If _buttonMap.ContainsKey(kv.Key) Then
                _buttonMap(kv.Key).Enabled = available
            End If
        Next

        HighlightActiveSection()
    End Sub

    Private Sub HighlightActiveSection()
        Dim state = _core.State

        For Each kv In _ledMap
            If kv.Key = "Break" Then Continue For
            If _core.StyleData.HasSection(kv.Key) Then
                kv.Value.BackColor = xblue
            End If
        Next
        BreakLED.BackColor = xblue

        If state.Break Then
            BreakLED.BackColor = xgreen
        ElseIf state.Intro Then
            SetLED("Intro" & state.SelectIntro, xgreen)
        ElseIf state.Outro Then
            SetLED("Outro" & state.SelectOutro, xgreen)
        ElseIf state.Ritim > 0 Then
            SetLED("Main" & state.Ritim, xgreen)
        End If
    End Sub

    Private Sub SetLED(sectionKey As String, color As Color)
        If _ledMap.ContainsKey(sectionKey) Then
            _ledMap(sectionKey).BackColor = color
        End If
    End Sub

    ' ════════════════════════════════════════════════════════
    '  STYLE-WECHSEL  (Slot 1 ↔ Slot 2)
    ' ════════════════════════════════════════════════════════

    Private Sub SetActiveStyle(slot As Integer)
        ' Bereits dieser Slot aktiv und kein Wechsel ausstehend → nichts tun
        If _core.State.Active = slot AndAlso _core.State.PendingSlot = 0 Then Return

        If _core.Engine.IsRunning Then
            ' Metronom läuft → auf nächsten Bar-Start vormerken + im Hintergrund vorladen
            _core.State.PendingSlot = slot
            Dim pendingName As String = If(slot = 1, Style_Name.Text, Style2_Name.Text)
            _core.PreloadPendingStyle(pendingName)
        Else
            ' Kein Metronom → sofort wechseln
            DoSwitchStyleUI(slot)
            RefreshCurrentStyle()
        End If
    End Sub

    ''' <summary>
    ''' Wird vom Core-Event gefeuert wenn Puffer getauscht wurde.
    ''' Nur UI-Update – Audio ist bereits geladen.
    ''' </summary>
    Private Sub OnSlotSwitchReady(slot As Integer, data As StyleData, styleName As String)
        Me.Invoke(Sub()
                      DoSwitchStyleUI(slot)
                      ' UpdateStyleUI statt RefreshCurrentStyle – kein Audio-Laden!
                      UpdateStyleUI(data, slot, styleName)
                      _core.Leds.RefreshSectionLEDs(data, _core.State)
                  End Sub)
    End Sub

    ''' <summary>Nur die Panel-Farben für den Slot-Wechsel setzen.</summary>
    Private Sub DoSwitchStyleUI(slot As Integer)
        Dim isSlot1 As Boolean = (slot = 1)

        Style1_Panel.BackgroundImage = If(isSlot1, My.Resources.StyleTab_selected, My.Resources.StyleTab_empty)
        Dim col1 As Color = If(isSlot1, Color.White, Color.Black)
        Dim col1d As Color = If(isSlot1, Color.White, xdarkgray)
        Style1_Label.ForeColor = col1
        Style_Name.ForeColor = col1d
        Family_Name.ForeColor = col1d
        BPM_Label.ForeColor = col1d
        DataType_Label.ForeColor = col1d

        Style2_Panel.BackgroundImage = If(Not isSlot1, My.Resources.StyleTab_selected, My.Resources.StyleTab_empty)
        Dim col2 As Color = If(Not isSlot1, Color.White, Color.Black)
        Dim col2d As Color = If(Not isSlot1, Color.White, xdarkgray)
        Style2_Label.ForeColor = col2
        Style2_Name.ForeColor = col2d
        Family2_Name.ForeColor = col2d
        BPM2_Label.ForeColor = col2d
        DataType2_Label.ForeColor = col2d
    End Sub

    ' ════════════════════════════════════════════════════════
    '  SECTION-AUSWAHL
    ' ════════════════════════════════════════════════════════

    Private Sub SelectMain(index As Integer)
        _core.State.ClearTransitions()
        _core.State.Ritim = index
        ApplyChangeMode()
        HighlightActiveSection()
        _core.Leds.RefreshSectionLEDs(_core.StyleData, _core.State)
    End Sub

    Private Sub ToggleIntro(index As Integer)
        If _core.State.Intro AndAlso _core.State.SelectIntro = index Then
            _core.State.Intro = False
        Else
            _core.State.Break = False
            _core.State.Outro = False
            _core.State.Intro = True
            _core.State.SelectIntro = index
        End If
        ApplyChangeMode()
        HighlightActiveSection()
        _core.Leds.RefreshSectionLEDs(_core.StyleData, _core.State)
    End Sub

    Private Sub ToggleBreak()
        _core.State.Intro = False
        _core.State.Outro = False
        _core.State.Break = Not _core.State.Break
        ApplyChangeMode()
        HighlightActiveSection()
        _core.Leds.RefreshSectionLEDs(_core.StyleData, _core.State)
    End Sub

    Private Sub ToggleOutro(index As Integer)
        If _core.State.Outro AndAlso _core.State.SelectOutro = index Then
            _core.State.Outro = False
        Else
            _core.State.Break = False
            _core.State.Intro = False
            _core.State.Outro = True
            _core.State.SelectOutro = index
        End If
        ApplyChangeMode()
        HighlightActiveSection()
        _core.Leds.RefreshSectionLEDs(_core.StyleData, _core.State)
    End Sub

    Private Sub ApplyChangeMode()
        If _core.State.ChangeMode = "Direct" AndAlso _core.Engine.IsRunning Then
            _core.Engine.SetBeatsPerBar(_core.StyleData.BeatsFor(_core.State.GetCurrentSection()))
        End If
    End Sub

    ' ════════════════════════════════════════════════════════
    '  METRONOM
    ' ════════════════════════════════════════════════════════

    Private Sub ToggleMetronome()
        If _core.Engine.IsRunning Then
            _core.StopMetronome()
            PlayPauseButton.Text = "Start"
        Else
            _core.StartMetronome(_core.StyleData.BPM, My.Settings.BPMCalcVal)
            PlayPauseButton.Text = "Stop"
        End If
    End Sub

    Private Sub ButtonStart_Click(sender As Object, e As EventArgs) Handles PlayPauseButton.Click
        ToggleMetronome()
    End Sub

    ' ════════════════════════════════════════════════════════
    '  CORE-EVENTS
    ' ════════════════════════════════════════════════════════

    Private Sub OnBeatTick(taktBeat As Integer)
        Me.Invoke(Sub()
                      UpdateBeatDisplay(taktBeat)
                      _core.Leds.SetBeatLED(taktBeat, _core.State.PendingSlot > 0)

                      Dim timer As New System.Windows.Forms.Timer() With {
                          .Interval = 120,
                          .Tag = taktBeat
                      }
                      If _lightEffect = 0 Then
                          AddHandler timer.Tick, AddressOf ResetStartStopLED
                      Else
                          AddHandler timer.Tick, AddressOf ResetAllBeatLEDs
                      End If
                      timer.Start()
                  End Sub)
    End Sub

    Private Sub OnBarStart(section As String)
        Me.Invoke(Sub() HighlightActiveSection())
    End Sub

    Private Sub OnMetronomeStopped()
        Me.Invoke(Sub()
                      ResetBeatDisplay()
                      _core.Leds.ResetBeatLED()
                      PlayPauseButton.Text = "Start"
                      DasLabel.Text = "-"
                  End Sub)
    End Sub

    Private Sub OnStyleLoaded(data As StyleData)
        ' Nichts nötig – RefreshCurrentStyle übernimmt alles
    End Sub

    ' ════════════════════════════════════════════════════════
    '  BEAT-ANZEIGE
    ' ════════════════════════════════════════════════════════

    Private Sub UpdateBeatDisplay(beat As Integer)
        ResetBeatDisplay()
        DasLabel.Text = beat.ToString()

        Dim displayBeat As Integer = ((beat - 1) Mod 4) + 1
        Dim isPending As Boolean = (_core.State.PendingSlot > 0)

        If isPending Then
            ' Wechsel ausstehend → ALLE Beats Lime (kein Orange)
            PlayPauseLED.BackColor = xgreen
            Select Case displayBeat
                Case 1 : Beat1.BackColor = xgreen
                Case 2 : Beat2.BackColor = xgreen
                Case 3 : Beat3.BackColor = xgreen
                Case 4 : Beat4.BackColor = xgreen
            End Select
        Else
            ' Normal → Beat 1 Orange, Rest Grün
            PlayPauseLED.BackColor = If(displayBeat = 1, xorange, xgreen)
            Select Case displayBeat
                Case 1 : Beat1.BackColor = xorange
                Case 2 : Beat2.BackColor = xgreen
                Case 3 : Beat3.BackColor = xgreen
                Case 4 : Beat4.BackColor = xgreen
            End Select
        End If
    End Sub

    Private Sub ResetBeatDisplay()
        Beat1.BackColor = xdarkgray
        Beat2.BackColor = xdarkgray
        Beat3.BackColor = xdarkgray
        Beat4.BackColor = xdarkgray
        PlayPauseLED.BackColor = xdarkgray
    End Sub

    Private Sub ResetStartStopLED(sender As Object, e As EventArgs)
        DisposeTimer(sender)
        PlayPauseLED.BackColor = xdarkgray
        _core.Leds.ResetBeatLED()
    End Sub

    Private Sub ResetAllBeatLEDs(sender As Object, e As EventArgs)
        Dim beat As Integer = CInt(DirectCast(sender, System.Windows.Forms.Timer).Tag)
        DisposeTimer(sender)
        PlayPauseLED.BackColor = xdarkgray
        Select Case beat
            Case 1 : Beat1.BackColor = xdarkgray
            Case 2 : Beat2.BackColor = xdarkgray
            Case 3 : Beat3.BackColor = xdarkgray
            Case 4 : Beat4.BackColor = xdarkgray
        End Select
        _core.Leds.ResetBeatLED()
    End Sub

    Private Sub DisposeTimer(sender As Object)
        Dim t = DirectCast(sender, System.Windows.Forms.Timer)
        t.Stop()
        t.Dispose()
    End Sub

    ' ════════════════════════════════════════════════════════
    '  BUTTON-HANDLER
    ' ════════════════════════════════════════════════════════

    Private Sub Button3_Click(s As Object, e As EventArgs) Handles Main1Button.Click
        SelectMain(1) : End Sub
    Private Sub Button4_Click(s As Object, e As EventArgs) Handles Main2Button.Click
        SelectMain(2) : End Sub
    Private Sub Button5_Click(s As Object, e As EventArgs) Handles Main3Button.Click
        SelectMain(3) : End Sub
    Private Sub Button6_Click(s As Object, e As EventArgs) Handles Main4Button.Click
        SelectMain(4) : End Sub

    Private Sub Button10_Click(s As Object, e As EventArgs) Handles Intro1Button.Click
        ToggleIntro(1) : End Sub
    Private Sub Button9_Click(s As Object, e As EventArgs) Handles Intro2Button.Click
        ToggleIntro(2) : End Sub
    Private Sub Button8_Click(s As Object, e As EventArgs) Handles Intro3Button.Click
        ToggleIntro(3) : End Sub

    Private Sub Button7_Click(s As Object, e As EventArgs) Handles BreakButton.Click
        ToggleBreak() : End Sub

    Private Sub Button11_Click(s As Object, e As EventArgs) Handles Outro1Button.Click
        ToggleOutro(1) : End Sub
    Private Sub Button12_Click(s As Object, e As EventArgs) Handles Outro2Button.Click
        ToggleOutro(2) : End Sub
    Private Sub Button13_Click(s As Object, e As EventArgs) Handles Outro3Button.Click
        ToggleOutro(3) : End Sub

    ' ════════════════════════════════════════════════════════
    '  TASTATUR-SHORTCUTS
    ' ════════════════════════════════════════════════════════

    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not My.Settings.FKey Then Return

        Select Case e.KeyCode
            Case Keys.Escape : ToggleMetronome()
            Case Keys.F1 : ToggleChangeMode()
            Case Keys.F2 : ToggleIntro(1)
            Case Keys.F3 : ToggleIntro(2)
            Case Keys.F4 : ToggleIntro(3)
            Case Keys.F5 : SelectMain(1)
            Case Keys.F6 : SelectMain(2)
            Case Keys.F7 : SelectMain(3)
            Case Keys.F8 : SelectMain(4)
            Case Keys.F9 : ToggleBreak()
            Case Keys.F10 : ToggleOutro(1)
            Case Keys.F11 : ToggleOutro(2)
            Case Keys.F12 : ToggleOutro(3)
            Case Keys.PageUp : SetActiveStyle(1)
            Case Keys.PageDown : SetActiveStyle(2)
            Case Keys.Alt : _lightEffect = 1
            Case Keys.D1 : SetAccompaniment("Off")
            Case Keys.D2 : SetAccompaniment("Assist")
            Case Keys.D3 : SetAccompaniment("Full")
        End Select
    End Sub

    ' ════════════════════════════════════════════════════════
    '  BEGLEITUNGS-MODUS
    ' ════════════════════════════════════════════════════════

    Private Sub SetAccompaniment(mode As String)
        Dim styleName As String = If(_core.State.Active = 1, Style_Name.Text, Style2_Name.Text)
        _core.SetAccompaniment(mode, styleName)
    End Sub

    ' ════════════════════════════════════════════════════════
    '  CHANGE-MODE
    ' ════════════════════════════════════════════════════════

    Private Sub ToggleChangeMode()
        If _core.State.ChangeMode = "Direct" Then
            _core.State.ChangeMode = "Ending"
            _core.Leds.SetKey(LedController.KEY_F1, LedController.COLOR_ORANGE)
        Else
            _core.State.ChangeMode = "Direct"
            _core.Leds.SetKey(LedController.KEY_F1, LedController.COLOR_ACTIVE)
        End If
    End Sub

    ' ════════════════════════════════════════════════════════
    '  STYLE-SELECTOR ÖFFNEN
    ' ════════════════════════════════════════════════════════

    Private Sub OpenStyleSelector(s As Object, e As EventArgs) _
        Handles Style1_Panel.Click, Style_Name.Click, Family_Name.Click, BPM_Label.Click,
                DataType_Label.Click,
                Style2_Panel.Click, Style2_Name.Click, BPM2_Label.Click,
                Family2_Name.Click, DataType2_Label.Click, Style2_Label.Click
        Styleselect.Show()
    End Sub

    Private Sub Style1_Label_Click(s As Object, e As EventArgs) Handles Style1_Label.Click
        SetActiveStyle(1)
    End Sub

    ' ════════════════════════════════════════════════════════
    '  SONSTIGES
    ' ════════════════════════════════════════════════════════

    Private Sub PictureBox4_Click(s As Object, e As EventArgs) Handles PictureBox4.Click
        ' Settings.Show()
    End Sub

    Private Sub Label37_Click(sender As Object, e As EventArgs) Handles Label37.Click
        Touch_Control.Hide()
    End Sub

    Private Sub Touch_Control_Button_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
        Touch_Control.Show()
    End Sub
End Class