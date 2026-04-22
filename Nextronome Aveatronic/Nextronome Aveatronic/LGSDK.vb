Imports System.Runtime.InteropServices

Public Class LogitechGSDK
    <DllImport("LogitechLedEnginesWrapper.dll", CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function LogiLedInit() As Boolean
    End Function

    ' Einzelne Taste setzen (R, G, B in Prozent: 0 bis 100)
    <DllImport("LogitechLedEnginesWrapper.dll", CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function LogiLedSetLightingForKeyWithKeyName(keyCode As Integer, red As Integer, green As Integer, blue As Integer) As Boolean
    End Function

    ' Ganze Tastatur setzen (Zum Testen)
    <DllImport("LogitechLedEnginesWrapper.dll", CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function LogiLedSetLighting(red As Integer, green As Integer, blue As Integer) As Boolean
    End Function

    <DllImport("LogitechLedEnginesWrapper.dll", CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Sub LogiLedShutdown()
    End Sub

    <DllImport("LogitechLedEnginesWrapper.dll", CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function LogiLedSetTargetDevice(targetDevice As Integer) As Boolean
    End Function

    ' 1 = Keyboard, 2 = Mouse, 4 = Mousepad, 8 = Headset
    Public Shared ReadOnly LOGI_DEVICETYPE_KEYBOARD As Integer = 1

    ' Key-Code für ESC ist 1
    Public Shared ReadOnly ESC_KEY As Integer = &H1
End Class