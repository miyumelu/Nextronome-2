Public Class Splashscreen
    Private Sub Splashscreen_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Opacity = 0
        Do Until Me.Opacity = 1
            Me.Opacity += 0.02
            Me.Refresh()
            System.Threading.Thread.Sleep(20)
        Loop
        Waitress.Start()
    End Sub

    Private Sub Waitress_Tick(sender As Object, e As EventArgs) Handles Waitress.Tick
        Me.Hide()
        Form1.Show()
    End Sub
End Class