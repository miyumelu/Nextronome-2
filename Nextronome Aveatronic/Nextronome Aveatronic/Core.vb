Imports System.Diagnostics
Imports System.IO
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks

' ── Kein Namespace – Core.vb ist direkt im Projekt eingebunden ──

Public Class StyleData
    Public Property BPM As Integer = 120
    Public Property Beats As New Dictionary(Of String, Integer)

    Public Function HasSection(name As String) As Boolean
        Return Beats.ContainsKey(name)
    End Function

    Public Function BeatsFor(name As String) As Integer
        Dim v As Integer
        If Beats.TryGetValue(name, v) Then Return v
        Return 4
    End Function
End Class

Public Class PlaybackState
    Public Property Intro As Boolean = False
    Public Property Break As Boolean = False
    Public Property Outro As Boolean = False
    Public Property SelectIntro As Integer = 1
    Public Property SelectOutro As Integer = 1
    Public Property Ritim As Integer = 1
    Public Property Accompaniment As String = "Full"
    Public Property ChangeMode As String = "Ending"
    Public Property Active As Integer = 1
    Public Property PendingSlot As Integer = 0

    Public Function GetCurrentSection() As String
        If Intro Then Return "Intro" & SelectIntro
        If Break Then Return "Break" & Ritim
        If Outro Then Return "Outro" & SelectOutro
        Return "Main" & Ritim
    End Function

    Public Sub ClearTransitions()
        Intro = False
        Break = False
        Outro = False
    End Sub
End Class

Public Class StyleLoader

    Private Shared ReadOnly SectionNames As String() = {
        "Intro1", "Intro2", "Intro3",
        "Main1", "Main2", "Main3", "Main4",
        "Auto1", "Auto2", "Auto3", "Auto4",
        "Outro1", "Outro2", "Outro3"
    }

    Public Shared Function Load(basePath As String, styleName As String, currentBPM As Integer) As StyleData
        Dim data As New StyleData()
        Dim stylePath As String = Path.Combine(basePath, styleName)

        Dim bpmFile As String = Path.Combine(stylePath, "bpm.val")
        If File.Exists(bpmFile) Then
            Dim parsed As Integer
            If Integer.TryParse(File.ReadAllText(bpmFile).Trim(), parsed) Then
                data.BPM = parsed
            End If
        Else
            data.BPM = currentBPM
        End If

        For Each section In SectionNames
            Dim sectionPath As String = Path.Combine(stylePath, section)
            If Directory.Exists(sectionPath) Then
                Dim beatsFile As String = Path.Combine(sectionPath, "beats.val")
                Dim beats As Integer = 4
                If File.Exists(beatsFile) Then
                    Integer.TryParse(File.ReadAllText(beatsFile).Trim(), beats)
                End If
                data.Beats(section) = beats
            End If
        Next

        Return data
    End Function

    Public Shared Function ReadValue(filePath As String, fallback As String) As String
        Try
            If File.Exists(filePath) Then Return File.ReadAllText(filePath).Trim()
        Catch
        End Try
        Return fallback
    End Function
End Class

Public Class AudioEngine
    Implements IDisposable

    Private ReadOnly _lock As New Object()
    Private ReadOnly _players As New Dictionary(Of String, SoundPlayer)

    ' ── Pending-Puffer für Hintergrund-Vorladen ──────────────────────
    Private ReadOnly _pendingLock As New Object()
    Private ReadOnly _pendingPlayers As New Dictionary(Of String, SoundPlayer)
    Private _pendingReady As Integer = 0   ' Interlocked: 0 = nicht bereit, 1 = bereit

    Public ReadOnly Property IsPendingReady As Boolean
        Get
            Return Interlocked.CompareExchange(_pendingReady, 0, 0) = 1
        End Get
    End Property

    ''' <summary>
    ''' Lädt neuen Stil in den Pending-Puffer. Muss auf einem Hintergrund-Thread aufgerufen werden.
    ''' </summary>
    Public Sub PreloadPending(basePath As String, styleName As String,
                               accompaniment As String, styleData As StyleData)
        Interlocked.Exchange(_pendingReady, 0)
        SyncLock _pendingLock
            DisposePending()
            For Each section In styleData.Beats.Keys
                Dim filePath As String = Path.Combine(basePath, styleName, section, accompaniment, "audio.wav")
                If File.Exists(filePath) Then
                    Try
                        Dim player As New SoundPlayer(filePath)
                        player.Load()
                        _pendingPlayers(section) = player
                    Catch ex As Exception
                        Debug.WriteLine($"[Audio] Pending-Fehler '{section}': {ex.Message}")
                    End Try
                End If
            Next
        End SyncLock
        Interlocked.Exchange(_pendingReady, 1)
    End Sub

    ''' <summary>
    ''' Tauscht Pending gegen aktiven Puffer. Kein File-I/O – sicher auf Metronom-Thread.
    ''' </summary>
    Public Sub PromotePending()
        SyncLock _lock
            SyncLock _pendingLock
                DisposeAll()
                For Each kv In _pendingPlayers
                    _players(kv.Key) = kv.Value   ' Referenz übergeben, kein Dispose!
                Next
                _pendingPlayers.Clear()           ' Nur Dictionary leeren, Players leben weiter
            End SyncLock
        End SyncLock
        Interlocked.Exchange(_pendingReady, 0)
    End Sub

    ''' <summary>Normales synchrones Laden (wenn Metronom nicht läuft).</summary>
    Public Sub LoadAll(basePath As String, styleName As String,
                       accompaniment As String, styleData As StyleData)
        SyncLock _lock
            DisposeAll()
            For Each section In styleData.Beats.Keys
                Dim filePath As String = Path.Combine(basePath, styleName, section, accompaniment, "audio.wav")
                BufferFile(section, filePath)
            Next
        End SyncLock
    End Sub

    Private Sub BufferFile(key As String, filePath As String)
        Try
            If File.Exists(filePath) Then
                Dim player As New SoundPlayer(filePath)
                player.Load()
                _players(key) = player
            End If
        Catch ex As Exception
            Debug.WriteLine($"[Audio] Fehler beim Laden '{key}': {ex.Message}")
        End Try
    End Sub

    ''' <summary>Play außerhalb des Locks – verhindert Deadlock mit Metronom-Thread.</summary>
    Public Sub Play(sectionName As String)
        Dim player As SoundPlayer = Nothing
        SyncLock _lock
            _players.TryGetValue(sectionName, player)
        End SyncLock
        If player IsNot Nothing Then
            Try
                player.Play()
            Catch ex As Exception
                Debug.WriteLine($"[Audio] Fehler beim Abspielen '{sectionName}': {ex.Message}")
            End Try
        End If
    End Sub

    Public Sub StopAll()
        SyncLock _lock
            For Each p In _players.Values
                Try : p.Stop() : Catch : End Try
            Next
        End SyncLock
    End Sub

    Public Sub Clear()
        SyncLock _lock
            DisposeAll()
        End SyncLock
    End Sub

    Private Sub DisposeAll()
        For Each p In _players.Values
            Try : p.Stop() : p.Dispose() : Catch : End Try
        Next
        _players.Clear()
    End Sub

    Private Sub DisposePending()
        For Each p In _pendingPlayers.Values
            Try : p.Stop() : p.Dispose() : Catch : End Try
        Next
        _pendingPlayers.Clear()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Clear()
        SyncLock _pendingLock
            DisposePending()
        End SyncLock
    End Sub
End Class

Public Class LedController

    Public Const KEY_ESC As Integer = &H1
    Public Const KEY_F1 As Integer = 59
    Public Const KEY_INTRO1 As Integer = 60
    Public Const KEY_INTRO2 As Integer = 61
    Public Const KEY_INTRO3 As Integer = 62
    Public Const KEY_MAIN1 As Integer = 63
    Public Const KEY_MAIN2 As Integer = 64
    Public Const KEY_MAIN3 As Integer = 65
    Public Const KEY_MAIN4 As Integer = 66
    Public Const KEY_BREAK As Integer = 67
    Public Const KEY_OUTRO1 As Integer = 68
    Public Const KEY_OUTRO2 As Integer = 87
    Public Const KEY_OUTRO3 As Integer = 88
    Public Const KEY_PGUP As Integer = 329
    Public Const KEY_PGDN As Integer = 337

    Public Shared ReadOnly COLOR_AVAILABLE As (R As Integer, G As Integer, B As Integer) = (0, 80, 100)
    Public Shared ReadOnly COLOR_ACTIVE As (R As Integer, G As Integer, B As Integer) = (0, 100, 60)
    Public Shared ReadOnly COLOR_DISABLED As (R As Integer, G As Integer, B As Integer) = (50, 50, 50)
    Public Shared ReadOnly COLOR_STOP As (R As Integer, G As Integer, B As Integer) = (100, 0, 0)
    Public Shared ReadOnly COLOR_ORANGE As (R As Integer, G As Integer, B As Integer) = (100, 70, 0)
    Public Shared ReadOnly COLOR_OFF As (R As Integer, G As Integer, B As Integer) = (0, 0, 0)

    Public Function Init() As Boolean
        Return LogitechGSDK.LogiLedInit()
    End Function

    Public Sub Shutdown()
        LogitechGSDK.LogiLedShutdown()
    End Sub

    Public Sub SetKey(keyCode As Integer, color As (R As Integer, G As Integer, B As Integer))
        LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(keyCode, color.R, color.G, color.B)
    End Sub

    Public Sub RefreshSectionLEDs(style As StyleData, state As PlaybackState)
        Dim keyMap As New Dictionary(Of String, Integer) From {
            {"Intro1", KEY_INTRO1}, {"Intro2", KEY_INTRO2}, {"Intro3", KEY_INTRO3},
            {"Main1", KEY_MAIN1}, {"Main2", KEY_MAIN2}, {"Main3", KEY_MAIN3}, {"Main4", KEY_MAIN4},
            {"Outro1", KEY_OUTRO1}, {"Outro2", KEY_OUTRO2}, {"Outro3", KEY_OUTRO3}
        }

        For Each kv In keyMap
            SetKey(kv.Value, If(style.HasSection(kv.Key), COLOR_AVAILABLE, COLOR_DISABLED))
        Next

        SetKey(KEY_BREAK, COLOR_AVAILABLE)

        Dim activeKey As Integer = GetKeyForSection(state)
        If activeKey <> 0 Then SetKey(activeKey, COLOR_ACTIVE)
        If state.Break Then SetKey(KEY_BREAK, COLOR_ACTIVE)

        SetKey(KEY_ESC, COLOR_STOP)
        SetKey(KEY_F1, If(state.ChangeMode = "Direct", COLOR_ACTIVE, COLOR_ORANGE))

        If state.Active = 1 Then
            SetKey(KEY_PGUP, COLOR_ACTIVE)
            SetKey(KEY_PGDN, COLOR_DISABLED)
        Else
            SetKey(KEY_PGUP, COLOR_DISABLED)
            SetKey(KEY_PGDN, COLOR_ACTIVE)
        End If
    End Sub

    ''' <summary>Beat-LED auf ESC. Bei isPending immer Grün (kein Orange für Beat 1).</summary>
    Public Sub SetBeatLED(beat As Integer, isPending As Boolean)
        If isPending Then
            SetKey(KEY_ESC, COLOR_ACTIVE)   ' Immer Grün während Wechsel ausstehend
        ElseIf beat = 1 Then
            SetKey(KEY_ESC, COLOR_ORANGE)
        Else
            SetKey(KEY_ESC, COLOR_ACTIVE)
        End If
    End Sub

    Public Sub ResetBeatLED()
        SetKey(KEY_ESC, COLOR_STOP)
    End Sub

    Private Function GetKeyForSection(state As PlaybackState) As Integer
        If state.Intro Then
            Select Case state.SelectIntro
                Case 1 : Return KEY_INTRO1
                Case 2 : Return KEY_INTRO2
                Case 3 : Return KEY_INTRO3
            End Select
        ElseIf state.Outro Then
            Select Case state.SelectOutro
                Case 1 : Return KEY_OUTRO1
                Case 2 : Return KEY_OUTRO2
                Case 3 : Return KEY_OUTRO3
            End Select
        ElseIf Not state.Break Then
            Select Case state.Ritim
                Case 1 : Return KEY_MAIN1
                Case 2 : Return KEY_MAIN2
                Case 3 : Return KEY_MAIN3
                Case 4 : Return KEY_MAIN4
            End Select
        End If
        Return 0
    End Function
End Class

Public Class MetronomeEngine
    Implements IDisposable

    Public Event BeatTick(taktBeat As Integer)
    Public Event BarStart(section As String)
    Public Event Stopped()

    Private _thread As Thread
    Private _stopFlag As Boolean = False
    Private _bpm As Integer = 120
    Private _beatsPerBar As Integer = 4
    Private _calcVal As Integer = 60000

    Private _isRunning As Boolean = False
    Public ReadOnly Property IsRunning As Boolean
        Get
            Return _isRunning
        End Get
    End Property

    Public Sub Start(bpm As Integer, calcVal As Integer, beatsPerBar As Integer)
        If _isRunning Then [Stop]()
        _bpm = bpm
        _calcVal = calcVal
        _beatsPerBar = beatsPerBar
        _stopFlag = False
        _isRunning = True
        _thread = New Thread(AddressOf RunLoop) With {
            .Priority = ThreadPriority.Highest,
            .IsBackground = True
        }
        _thread.Start()
    End Sub

    Public Sub [Stop]()
        _stopFlag = True
        _isRunning = False
        If _thread IsNot Nothing AndAlso _thread.IsAlive Then
            _thread.Join(500)
        End If
        _thread = Nothing
        RaiseEvent Stopped()
    End Sub

    Public Sub SetBPM(bpm As Integer, calcVal As Integer)
        _bpm = bpm
        _calcVal = calcVal
    End Sub

    Public Sub SetBeatsPerBar(beats As Integer)
        If beats > 0 Then _beatsPerBar = beats
    End Sub

    Private Sub RunLoop()
        Dim sw As New Stopwatch()
        sw.Start()
        Dim nextBeatTime As Long = sw.ElapsedMilliseconds
        Dim beatCount As Integer = 0

        While Not _stopFlag
            Dim interval As Long = CLng(_calcVal / _bpm)
            nextBeatTime += interval

            beatCount += 1
            If beatCount > _beatsPerBar Then beatCount = 1

            If beatCount = 1 Then
                RaiseEvent BarStart("")
            End If

            RaiseEvent BeatTick(beatCount)

            Dim remaining As Long = nextBeatTime - sw.ElapsedMilliseconds
            If remaining > 1 Then
                Thread.Sleep(CInt(remaining - 1))
            End If
            While sw.ElapsedMilliseconds < nextBeatTime AndAlso Not _stopFlag
                Thread.SpinWait(100)
            End While
        End While

        sw.Stop()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        [Stop]()
    End Sub
End Class

Public Class NextronomeController
    Implements IDisposable

    Public ReadOnly Audio As New AudioEngine()
    Public ReadOnly Leds As New LedController()
    Public ReadOnly Engine As New MetronomeEngine()
    Public ReadOnly State As New PlaybackState()

    Private _styleData As New StyleData()
    Private _basePath As String
    Private _metronomMode As Boolean = False

    ' Vorgeladener Stil für nahtlosen Wechsel
    Private _pendingStyleData As StyleData = Nothing
    Private _pendingStyleName As String = ""

    Public Event BeatTick(taktBeat As Integer)
    Public Event BarStart(section As String)
    Public Event Stopped()
    Public Event StyleLoaded(data As StyleData)
    ' Wechsel vollzogen – UI soll Labels/Panels aktualisieren (kein File-I/O nötig)
    Public Event SlotSwitchReady(slot As Integer, data As StyleData, styleName As String)

    Public Function Initialize(basePath As String) As Boolean
        _basePath = basePath
        AddHandler Engine.BeatTick, AddressOf OnBeatTick
        AddHandler Engine.BarStart, AddressOf OnBarStart
        AddHandler Engine.Stopped, AddressOf OnStopped
        Return Leds.Init()
    End Function

    Public Property MetronomMode As Boolean
        Get
            Return _metronomMode
        End Get
        Set(value As Boolean)
            _metronomMode = value
        End Set
    End Property

    Public ReadOnly Property StyleData As StyleData
        Get
            Return _styleData
        End Get
    End Property

    ''' <summary>Normales Laden – nur wenn Metronom NICHT läuft.</summary>
    Public Function LoadStyle(styleName As String, currentBPM As Integer) As StyleData
        If String.IsNullOrEmpty(styleName) Then Return _styleData

        _styleData = StyleLoader.Load(_basePath, styleName, currentBPM)
        Audio.Clear()
        Audio.LoadAll(_basePath, styleName, State.Accompaniment, _styleData)
        Leds.RefreshSectionLEDs(_styleData, State)

        If Not _metronomMode AndAlso Engine.IsRunning Then
            Engine.SetBPM(_styleData.BPM, My.Settings.BPMCalcVal)
        End If

        RaiseEvent StyleLoaded(_styleData)
        Return _styleData
    End Function

    ''' <summary>
    ''' Startet Hintergrund-Vorladen des neuen Stils.
    ''' Wird aufgerufen wenn Metronom läuft und PendingSlot gesetzt wird.
    ''' </summary>
    Public Sub PreloadPendingStyle(styleName As String)
        If String.IsNullOrEmpty(styleName) Then Return
        _pendingStyleName = styleName
        _pendingStyleData = Nothing

        Task.Run(Sub()
                     Try
                         Dim data As StyleData = StyleLoader.Load(_basePath, styleName, _styleData.BPM)
                         _pendingStyleData = data
                         ' Audio in Pending-Puffer laden (Hintergrund)
                         Audio.PreloadPending(_basePath, styleName, State.Accompaniment, data)
                     Catch ex As Exception
                         Debug.WriteLine($"[Core] Fehler beim Vorladen '{styleName}': {ex.Message}")
                     End Try
                 End Sub)
    End Sub

    Public Sub SetAccompaniment(mode As String, styleName As String)
        State.Accompaniment = mode
        Audio.Clear()
        Audio.LoadAll(_basePath, styleName, mode, _styleData)
    End Sub

    Public Sub StartMetronome(bpm As Integer, calcVal As Integer)
        Engine.Start(bpm, calcVal, _styleData.BeatsFor(State.GetCurrentSection()))
    End Sub

    Public Sub StopMetronome()
        Engine.Stop()
        Audio.StopAll()
    End Sub

    Private Sub OnBeatTick(taktBeat As Integer)
        RaiseEvent BeatTick(taktBeat)
    End Sub

    Private Sub OnBarStart(section As String)
        ' ── Wechsel: nur wenn Vorladen vollständig abgeschlossen ──────────
        If State.PendingSlot > 0 Then
            If Audio.IsPendingReady AndAlso _pendingStyleData IsNot Nothing Then
                ' Puffer tauschen – kein File-I/O, sicher auf Metronom-Thread
                Audio.PromotePending()
                _styleData = _pendingStyleData
                _pendingStyleData = Nothing

                ' BPM und Taktlänge sofort anpassen
                Engine.SetBPM(_styleData.BPM, My.Settings.BPMCalcVal)

                Dim slot As Integer = State.PendingSlot
                State.PendingSlot = 0
                State.Active = slot

                ' Ersten Beat des neuen Stils abspielen
                If Not _metronomMode Then PlayCurrentSection()

                ' UI-Update anfordern (enthält StyleData für Labels)
                RaiseEvent SlotSwitchReady(slot, _styleData, _pendingStyleName)
                RaiseEvent BarStart(State.GetCurrentSection())
            Else
                ' Vorladen noch nicht fertig → alten Stil einen Takt weiterspielen
                If Not _metronomMode Then PlayCurrentSection()
                RaiseEvent BarStart(State.GetCurrentSection())
            End If
            Return
        End If

        ' ── Normaler Bar-Start ─────────────────────────────────────────────
        If Not _metronomMode Then PlayCurrentSection()
        RaiseEvent BarStart(State.GetCurrentSection())
    End Sub

    Private Sub OnStopped()
        RaiseEvent Stopped()
    End Sub

    Private Sub PlayCurrentSection()
        Dim sectionName As String

        If State.Intro Then
            sectionName = "Intro" & State.SelectIntro
            Engine.SetBeatsPerBar(_styleData.BeatsFor(sectionName))
            Audio.Play(sectionName)
            State.Intro = False

        ElseIf State.Break Then
            sectionName = "Auto" & State.Ritim
            Engine.SetBeatsPerBar(_styleData.BeatsFor(sectionName))
            Audio.Play(sectionName)
            State.Break = False

        ElseIf State.Outro Then
            sectionName = "Outro" & State.SelectOutro
            Engine.SetBeatsPerBar(_styleData.BeatsFor(sectionName))
            Audio.Play(sectionName)
            State.Outro = False

        Else
            sectionName = "Main" & State.Ritim
            If _styleData.HasSection(sectionName) Then
                Engine.SetBeatsPerBar(_styleData.BeatsFor(sectionName))
                Audio.Play(sectionName)
            End If
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        StopMetronome()
        Audio.Dispose()
        Engine.Dispose()
        Leds.Shutdown()
    End Sub
End Class