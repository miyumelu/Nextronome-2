Imports System.Reflection.Emit
Imports System.Runtime
Imports Nextronome_Aveatronic.NextronomeCore

Public Class Form1

    ' Farben
    Private ReadOnly xblue As Color = Color.FromArgb(255, 97, 180, 241)
    Private ReadOnly xgreen As Color = Color.FromArgb(255, 102, 246, 159)
    Private ReadOnly xorange As Color = Color.FromArgb(255, 244, 136, 30)
    Private ReadOnly xdarkgray As Color = Color.FromArgb(255, 38, 38, 38)

    Private ReadOnly _core As New NextronomeController()
    Private ReadOnly _basePath As String = "C:\KAVN\Applications\Accompaniment\Styles\"

    Public Property TabLabel As Label        ' Referenz für Form2
    Private _lightEffect As Integer = 0      ' 0 = nur StartStop-LED blinkt; 1 = alle Beat-LEDs blinken

    Private _ledMap As Dictionary(Of String, Panel)

    Private _buttonMap As Dictionary(Of String, Button)

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitMaps()
        ResetBeatDisplay()

        Dim ledOk As Boolean = _core.Initialize(_basePath)
        If Not ledOk Then
            MessageBox.Show("Konnte keine Verbindung zu Logitech G HUB herstellen. Läuft G HUB im Hintergrund?")
        End If

        ' Events
        AddHandler _core.BeatTick, AddressOf OnBeatTick
        AddHandler _core.BarStart, AddressOf OnBarStart
        AddHandler _core.Stopped, AddressOf OnMetronomeStopped
        AddHandler _core.StyleLoaded, AddressOf OnStyleLoaded

        ' Gespeicherte Settings laden
        Dim savedStyle As String = My.Settings.Style1
        If Not String.IsNullOrEmpty(savedStyle) Then
            Style_Name.Text = savedStyle
        End If
        Dim savedStyle2 As String = My.Settings.Style2
        If Not String.IsNullOrEmpty(savedStyle2) Then
            Style2_Name.Text = savedStyle2
        End If

        ' Style laden
        RefreshCurrentStyle()

        ' Multi-Monitor: Fenster auf zweitem Bildschirm
        If Screen.AllScreens.Length > 1 Then
            Me.Location = Screen.AllScreens(1).WorkingArea.Location
        End If
        Me.WindowState = FormWindowState.Maximized

        BPMValue_Label.Text = My.Settings.BPMCalcVal & " ms"
        MetronomeBeats_Label.Text = "4"
    End Sub

    Private Sub InitMaps()
        _ledMap = New Dictionary(Of String, Panel) From {
            {"Intro1", IntroLED1}, {"Intro2", IntroLED2}, {"Intro3", IntroLED3},
            {"Main1", MainLED1}, {"Main2", MainLED2}, {"Main3", MainLED3}, {"Main4", MainLED4},
            {"Break", BreakLED},
            {"Outro1", OutroLED1}, {"Outro2", OutroLED2}, {"Outro3", OutroLED3}
        }
        _buttonMap = New Dictionary(Of String, Button) From {
            {"Intro1", Button10}, {"Intro2", Button9}, {"Intro3", Button8},
            {"Main1", Button3}, {"Main2", Button4}, {"Main3", Button5}, {"Main4", Button6},
            {"Break", Button7},
            {"Outro1", Button11}, {"Outro2", Button12}, {"Outro3", Button13}
        }
    End Sub

    Public Sub RefreshCurrentStyle()
        Dim styleName As String = If(_core.State.Active = 1, Style_Name.Text, Style2_Name.Text)
        If String.IsNullOrEmpty(styleName) Then Return

        Dim data As StyleData = _core.LoadStyle(styleName, _core.StyleData.BPM)

        ' BPM-Label aktualisieren
        If Not _core.MetronomMode Then
            Label8.Text = data.BPM.ToString()
        End If

        ' Buttons und LEDs nach verfügbaren Sections setzen
        For Each kv In _ledMap
            Dim sectionKey As String = kv.Key
            If sectionKey = "Break" Then Continue For ' Break immer verfügbar

            Dim available As Boolean = data.HasSection(sectionKey)
            kv.Value.BackColor = If(available, xblue, xdarkgray)
            If _buttonMap.ContainsKey(sectionKey) Then
                _buttonMap(sectionKey).Enabled = available
            End If
        Next

        HighlightActiveSection()
    End Sub

    Private Sub HighlightActiveSection()
        Dim state = _core.State

        ' Alles auf Blau zurücksetzen
        For Each kv In _ledMap
            If kv.Key = "Break" Then Continue For
            If _core.StyleData.HasSection(kv.Key) Then
                kv.Value.BackColor = xblue
            End If
        Next
        BreakLED.BackColor = xblue

        ' Aktive Section grün
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

    Private Sub SetActiveStyle(slot As Integer)
        _core.State.Active = slot
        Dim isSlot1 As Boolean = (slot = 1)

        ' Panel 1
        Style1_Panel.BackgroundImage = If(isSlot1, My.Resources.BTN_Toggled, My.Resources.BTN_Untoggled)
        Dim col1 As Color = If(isSlot1, Color.Black, xdarkgray)
        Dim col1data As Color = If(isSlot1, xblue, xdarkgray)
        Style1_Label.ForeColor = col1
        Style_Name.ForeColor = col1data
        Family_Name.ForeColor = col1data
        BPM_Label.ForeColor = col1data
        DataType_Label.ForeColor = col1data

        ' Panel 2
        Style2_Panel.BackgroundImage = If(Not isSlot1, My.Resources.BTN_Toggled, My.Resources.BTN_Untoggled)
        Dim col2 As Color = If(Not isSlot1, Color.Black, xdarkgray)
        Dim col2data As Color = If(Not isSlot1, xblue, xdarkgray)
        Style2_Label.ForeColor = col2
        Style2_Name.ForeColor = col2data
        Family2_Name.ForeColor = col2data
        BPM2_Label.ForeColor = col2data
        DataType2_Label.ForeColor = col2data

        RefreshCurrentStyle()
    End Sub

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
            Dim bpm As Integer = _core.StyleData.BPM
            _core.Engine.SetBeatsPerBar(_core.StyleData.BeatsFor(_core.State.GetCurrentSection()))
        End If
    End Sub

    Private Sub ToggleMetronome()
        If _core.Engine.IsRunning Then
            _core.StopMetronome()
            Button1.Text = "Start"
        Else
            _core.StartMetronome(_core.StyleData.BPM, My.Settings.BPMCalcVal)
            Button1.Text = "Stop"
        End If
    End Sub

    Private Sub ButtonStart_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ToggleMetronome()
    End Sub

    Private Sub OnBeatTick(taktBeat As Integer)
        Me.Invoke(Sub()
                      UpdateBeatDisplay(taktBeat)
                      _core.Leds.SetBeatLED(taktBeat)

                      ' Blink-Timer
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
        Me.Invoke(Sub()
                      HighlightActiveSection()
                      BreakLED.BackColor = xblue
                  End Sub)
    End Sub

    Private Sub OnMetronomeStopped()
        Me.Invoke(Sub()
                      ResetBeatDisplay()
                      _core.Leds.ResetBeatLED()
                      Button1.Text = "Start"
                      DasLabel.Text = "-"
                  End Sub)
    End Sub

    Private Sub OnStyleLoaded(data As StyleData)
        ' Nichts extra nötig – RefreshCurrentStyle() hat es ersetzt... hoffentlich.
    End Sub

    Private Sub UpdateBeatDisplay(beat As Integer)
        ResetBeatDisplay()
        DasLabel.Text = beat.ToString()

        Dim activeColor As Color = If(beat = 1, xorange, xgreen)
        StartStopLED.BackColor = activeColor

        Select Case beat
            Case 1 : Beat1.BackColor = xorange
            Case 2 : Beat2.BackColor = xgreen
            Case 3 : Beat3.BackColor = xgreen
            Case 4 : Beat4.BackColor = xgreen
        End Select
    End Sub

    Private Sub ResetBeatDisplay()
        Beat1.BackColor = xdarkgray
        Beat2.BackColor = xdarkgray
        Beat3.BackColor = xdarkgray
        Beat4.BackColor = xdarkgray
        StartStopLED.BackColor = xdarkgray
    End Sub

    Private Sub ResetStartStopLED(sender As Object, e As EventArgs)
        DisposeTimer(sender)
        StartStopLED.BackColor = xdarkgray
        _core.Leds.ResetBeatLED()
    End Sub

    Private Sub ResetAllBeatLEDs(sender As Object, e As EventArgs)
        Dim beat As Integer = CInt(DirectCast(sender, System.Windows.Forms.Timer).Tag)
        DisposeTimer(sender)
        StartStopLED.BackColor = xdarkgray
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

    Private Sub Button3_Click(s As Object, e As EventArgs) Handles Button3.Click : SelectMain(1) : End Sub
    Private Sub Button4_Click(s As Object, e As EventArgs) Handles Button4.Click : SelectMain(2) : End Sub
    Private Sub Button5_Click(s As Object, e As EventArgs) Handles Button5.Click : SelectMain(3) : End Sub
    Private Sub Button6_Click(s As Object, e As EventArgs) Handles Button6.Click : SelectMain(4) : End Sub

    Private Sub Button10_Click(s As Object, e As EventArgs) Handles Button10.Click : ToggleIntro(1) : End Sub
    Private Sub Button9_Click(s As Object, e As EventArgs) Handles Button9.Click : ToggleIntro(2) : End Sub
    Private Sub Button8_Click(s As Object, e As EventArgs) Handles Button8.Click : ToggleIntro(3) : End Sub

    Private Sub Button7_Click(s As Object, e As EventArgs) Handles Button7.Click : ToggleBreak() : End Sub

    Private Sub Button11_Click(s As Object, e As EventArgs) Handles Button11.Click : ToggleOutro(1) : End Sub
    Private Sub Button12_Click(s As Object, e As EventArgs) Handles Button12.Click : ToggleOutro(2) : End Sub
    Private Sub Button13_Click(s As Object, e As EventArgs) Handles Button13.Click : ToggleOutro(3) : End Sub

    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown, Button1.KeyDown
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
            Case Keys.ControlKey : BPMValueSetting.Show()
            Case Keys.Alt : _lightEffect = 1
            Case Keys.D1 : SetAccompaniment("Off")
            Case Keys.D2 : SetAccompaniment("Assist")
            Case Keys.D3 : SetAccompaniment("Full")
        End Select
    End Sub

    Private Sub SetAccompaniment(mode As String)
        Rhythmonly_Label.ForeColor = If(mode = "Off", xgreen, xdarkgray)
        AutoAssist_Label.ForeColor = If(mode = "Assist", xgreen, xdarkgray)
        AutoPlay_Label.ForeColor = If(mode = "Full", xgreen, xdarkgray)

        Dim styleName As String = If(_core.State.Active = 1, Style_Name.Text, Style2_Name.Text)
        _core.SetAccompaniment(mode, styleName)
    End Sub

    Private Sub Label2_Click(s As Object, e As EventArgs) Handles AutoAssist_Label.Click : SetAccompaniment("Assist") : End Sub
    Private Sub Rhythmonly_Click(s As Object, e As EventArgs) Handles Rhythmonly_Label.Click : SetAccompaniment("Off") : End Sub
    Private Sub AutoPlay_Click(s As Object, e As EventArgs) Handles AutoPlay_Label.Click : SetAccompaniment("Full") : End Sub

    Private Sub ToggleChangeMode()
        If _core.State.ChangeMode = "Direct" Then
            _core.State.ChangeMode = "Ending"
            Changemode_Label.Text = "Ending"
            _core.Leds.SetKey(LedController.KEY_F1, LedController.COLOR_ORANGE)
        Else
            _core.State.ChangeMode = "Direct"
            Changemode_Label.Text = "Direct"
            _core.Leds.SetKey(LedController.KEY_F1, LedController.COLOR_ACTIVE)
        End If
    End Sub

    Private Sub Changemode_Label_Click(s As Object, e As EventArgs) Handles Changemode_Label.Click
        ToggleChangeMode()
    End Sub

    Private Sub MetronomeModeChange(s As Object, e As EventArgs) Handles Label12.Click, Label11.Click, Panel3.Click
        _core.MetronomMode = Not _core.MetronomMode
        Label11.Text = If(_core.MetronomMode, "On - Metronome mode", "Off - Style mode")
    End Sub

    Private Sub OpenStyleSelector(s As Object, e As EventArgs) _
        Handles Style1_Panel.Click, Style_Name.Click, BPM_Label.Click, Family_Name.Click, DataType_Label.Click,
                Style2_Panel.Click, Style2_Name.Click, BPM2_Label.Click, Family2_Name.Click, DataType2_Label.Click,
                Style2_Label.Click
        Form2.Show()
    End Sub

    Private Sub Style1_Label_Click(s As Object, e As EventArgs) Handles Style1_Label.Click
        SetActiveStyle(1)
    End Sub

    Private Sub Style2_LabelSwitch_Click(s As Object, e As EventArgs) Handles Style2_Label.Click
        SetActiveStyle(2)
    End Sub

    Private Sub BPMClick(s As Object, e As EventArgs) Handles Label8.Click, Label10.Click, Panel2.Click
        BPMSetting.Show()
    End Sub

    Private Sub BPMValueClick(s As Object, e As EventArgs) Handles Label7.Click, BPMValue_Label.Click
        BPMValueSetting.Show()
    End Sub

    Private Sub HomeButton_Click(s As Object, e As EventArgs) Handles HomeButton.Click
        _core.Dispose()
        Application.Exit()
    End Sub

    Private Sub PictureBox4_Click(s As Object, e As EventArgs) Handles PictureBox4.Click
        Settings.Show()
    End Sub
End Class
