Imports System.Diagnostics
Imports System.IO
Imports System.Media
Imports System.Runtime.InteropServices
Imports System.Threading

Namespace NextronomeCore

    Public Class StyleData
        Public Property BPM As Integer = 120

        Public Property Beats As New Dictionary(Of String, Integer)

        Public Function HasSection(name As String) As Boolean
            Return Beats.ContainsKey(name)
        End Function

        Public Function BeatsFor(name As String) As Integer
            Dim v As Integer
            If Beats.TryGetValue(name, v) Then Return v
            Return 4 ' Fallback
        End Function
    End Class

    Public Class PlaybackState
        Public Property Intro As Boolean = False
        Public Property Break As Boolean = False
        Public Property Outro As Boolean = False
        Public Property SelectIntro As Integer = 1
        Public Property SelectOutro As Integer = 1
        Public Property Ritim As Integer = 0
        Public Property Accompaniment As String = "Full"
        Public Property ChangeMode As String = "Ending"
        Public Property Active As Integer = 1

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
                data.BPM = currentBPM ' Fallback: Wert behalten
            End If

            For Each section In SectionNames
                Dim sectionPath As String = Path.Combine(stylePath, section)
                If Directory.Exists(sectionPath) Then
                    Dim beatsFile As String = Path.Combine(sectionPath, "beats.val")
                    Dim beats As Integer = 4 ' Standardwert
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

        Public Sub LoadAll(basePath As String, styleName As String, accompaniment As String, styleData As StyleData)
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

        Public Sub Play(sectionName As String)
            SyncLock _lock
                Dim player As SoundPlayer = Nothing
                If _players.TryGetValue(sectionName, player) Then
                    Try
                        player.Play()
                    Catch ex As Exception
                        Debug.WriteLine($"[Audio] Fehler beim Abspielen '{sectionName}': {ex.Message}")
                    End Try
                End If
            End SyncLock
        End Sub

        Public Sub StopAll()
            SyncLock _lock
                For Each p In _players.Values
                    Try
                        p.Stop()
                    Catch
                    End Try
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
                Try
                    p.Stop()
                    p.Dispose()
                Catch
                End Try
            Next
            _players.Clear()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Clear()
        End Sub
    End Class

    Public Class LedController

        ' Key-Codes (Logitech GSDK scan codes)
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

        ' Farb-Presets (RGB 0–100)
        Public Shared ReadOnly COLOR_AVAILABLE As (R As Integer, G As Integer, B As Integer) = (0, 80, 100)   ' Blau
        Public Shared ReadOnly COLOR_ACTIVE As (R As Integer, G As Integer, B As Integer) = (0, 100, 60)   ' Grün
        Public Shared ReadOnly COLOR_DISABLED As (R As Integer, G As Integer, B As Integer) = (50, 50, 50)   ' Grau
        Public Shared ReadOnly COLOR_STOP As (R As Integer, G As Integer, B As Integer) = (100, 0, 0)    ' Rot
        Public Shared ReadOnly COLOR_ORANGE As (R As Integer, G As Integer, B As Integer) = (100, 70, 0)   ' Orange
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
            ' Section-Key-Map: Sectionname > Keyboard-Key
            Dim keyMap As New Dictionary(Of String, Integer) From {
                {"Intro1", KEY_INTRO1}, {"Intro2", KEY_INTRO2}, {"Intro3", KEY_INTRO3},
                {"Main1", KEY_MAIN1}, {"Main2", KEY_MAIN2}, {"Main3", KEY_MAIN3}, {"Main4", KEY_MAIN4},
                {"Outro1", KEY_OUTRO1}, {"Outro2", KEY_OUTRO2}, {"Outro3", KEY_OUTRO3}
            }

            For Each kv In keyMap
                If style.HasSection(kv.Key) Then
                    SetKey(kv.Value, COLOR_AVAILABLE)
                Else
                    SetKey(kv.Value, COLOR_DISABLED)
                End If
            Next

            ' Break (Blau)
            SetKey(KEY_BREAK, COLOR_AVAILABLE)

            ' Aktive Section (Grün)
            Dim activeKey As Integer = GetKeyForSection(state)
            If activeKey <> 0 Then SetKey(activeKey, COLOR_ACTIVE)

            ' Break-LED
            If state.Break Then SetKey(KEY_BREAK, COLOR_ACTIVE)

            ' ESC = Stop (Rot)
            SetKey(KEY_ESC, COLOR_STOP)

            ' F1 = ChangeMode
            If state.ChangeMode = "Direct" Then
                SetKey(KEY_F1, COLOR_ACTIVE) ' Grün
            Else
                SetKey(KEY_F1, COLOR_ORANGE) ' Orange
            End If

            ' PageUp/PageDown = Style-Auswahl
            If state.Active = 1 Then
                SetKey(KEY_PGUP, COLOR_ACTIVE)
                SetKey(KEY_PGDN, COLOR_DISABLED)
            Else
                SetKey(KEY_PGUP, COLOR_DISABLED)
                SetKey(KEY_PGDN, COLOR_ACTIVE)
            End If
        End Sub

        Public Sub SetBeatLED(beat As Integer)
            If beat = 1 Then
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
        Private _calcVal As Integer = 60000 ' ms * 1000 / bpm = BPMCalcVal

        Public ReadOnly Property IsRunning As Boolean = False

        Public Sub Start(bpm As Integer, calcVal As Integer, beatsPerBar As Integer)
            If _IsRunning Then [Stop]()
            _bpm = bpm
            _calcVal = calcVal
            _beatsPerBar = beatsPerBar
            _stopFlag = False
            _IsRunning = True
            _thread = New Thread(AddressOf RunLoop) With {
                .Priority = ThreadPriority.Highest,
                .IsBackground = True
            }
            _thread.Start()
        End Sub

        Public Sub [Stop]()
            _stopFlag = True
            _IsRunning = False
            If _thread IsNot Nothing AndAlso _thread.IsAlive Then
                _thread.Join(200)
                If _thread.IsAlive Then _thread.Abort()
            End If
            RaiseEvent Stopped()
        End Sub

        Public Sub SetBPM(bpm As Integer, calcVal As Integer)
            _bpm = bpm
            _calcVal = calcVal
        End Sub

        Public Sub SetBeatsPerBar(beats As Integer)
            _beatsPerBar = beats
        End Sub

        Private Sub RunLoop()
            Dim sw As New Stopwatch()
            sw.Start()
            Dim nextBeatTime As Long = sw.ElapsedMilliseconds
            Dim beatCount As Integer = 0
            Dim currentSection As String = ""

            While Not _stopFlag
                Dim interval As Long = CLng(_calcVal / _bpm)
                nextBeatTime += interval

                beatCount += 1
                If beatCount > _beatsPerBar Then beatCount = 1

                Dim sectionForEvent As String = ""
                If beatCount = 1 Then
                    sectionForEvent = currentSection ' wird von außen per Callback aktualisiert
                    RaiseEvent BarStart(sectionForEvent)
                End If

                RaiseEvent BeatTick(beatCount)

                Dim remaining As Long = nextBeatTime - sw.ElapsedMilliseconds
                If remaining > 1 Then
                    Thread.Sleep(CInt(Math.Min(remaining - 1, remaining)))
                End If
                ' Sub-Millisekunden-Genauigkeit
                Dim fine As New Stopwatch()
                fine.Start()
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

        Public Event BeatTick(taktBeat As Integer)
        Public Event BarStart(section As String)
        Public Event Stopped()
        Public Event StyleLoaded(data As StyleData)

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

        Public Function LoadStyle(styleName As String, currentBPM As Integer) As StyleData
            _styleData = StyleLoader.Load(_basePath, styleName, currentBPM)

            Audio.Clear()
            Audio.LoadAll(_basePath, styleName, State.Accompaniment, _styleData)

            ' LEDs 
            Leds.RefreshSectionLEDs(_styleData, State)

            ' BPM im Engine aktualisieren 
            If Not _metronomMode Then
                If Engine.IsRunning Then
                    Engine.SetBPM(_styleData.BPM, My.Settings.BPMCalcVal)
                End If
            End If

            RaiseEvent StyleLoaded(_styleData)
            Return _styleData
        End Function

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
            ' Audio abspielen
            If Not _metronomMode Then
                PlayCurrentSection()
            End If
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

End Namespace
