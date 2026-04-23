Imports System.IO
Imports System.Runtime.InteropServices

' FIX: Imports Nextronome_Aveatronic.NextronomeCore entfernt – gleiche Projekt, kein Namespace mehr

Public Class Styleselect

    Private ReadOnly xorange As Color = Color.FromArgb(255, 244, 136, 30)
    Private ReadOnly xdarkgray As Color = Color.FromArgb(255, 38, 38, 38)

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function
    Private Const LVM_ENSUREVISIBLE As Integer = &H1000 + 19

    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.StartPosition = FormStartPosition.Manual
        Me.Location = New Point(0, 55)
        Me.Height = My.Computer.Screen.Bounds.Height - 55
        Me.Width = My.Computer.Screen.Bounds.Width
        Me.TopMost = True

        ConfigureListView()
        LoadFolders()
        MarkAndScrollToCurrentStyle()
    End Sub

    Private Sub ConfigureListView()
        ListView1.View = View.Details
        ListView1.MultiSelect = False
        ListView1.FullRowSelect = True
        ListView1.HideSelection = False
        ListView1.GridLines = True
        ListView1.Columns.Clear()
        ListView1.Columns.Add("Style", 715, HorizontalAlignment.Center)
        ListView1.Columns.Add("Type", 715, HorizontalAlignment.Center)
    End Sub

    Private Sub LoadFolders()
        Dim path As String = "C:\KAVN\Applications\Accompaniment\Styles"
        If Not Directory.Exists(path) Then
            MessageBox.Show("Pfad nicht gefunden: " & path)
            Return
        End If

        ListView1.Items.Clear()
        For Each folder In Directory.GetDirectories(path)
            Dim item As New ListViewItem(IO.Path.GetFileName(folder))
            item.SubItems.Add("KAVN Style Package")
            item.ForeColor = Color.White
            item.BackColor = Color.Black
            ListView1.Items.Add(item)
        Next
    End Sub

    Private Sub MarkAndScrollToCurrentStyle()
        Dim currentStyle As String = If(Form1._core.State.Active = 1,
                                        Form1.Style_Name.Text,
                                        Form1.Style2_Name.Text)
        If String.IsNullOrEmpty(currentStyle) Then Return

        For Each item As ListViewItem In ListView1.Items
            If item.Text = currentStyle Then
                item.BackColor = xorange
                item.ForeColor = Color.Black
                item.Selected = True
                item.EnsureVisible()
                SendMessage(ListView1.Handle, LVM_ENSUREVISIBLE, item.Index, 0)
                Exit For
            End If
        Next
    End Sub

    Private Sub ListView1_Click(sender As Object, e As EventArgs) Handles ListView1.Click
        For Each item As ListViewItem In ListView1.Items
            item.ForeColor = Color.White
            item.BackColor = Color.Black
        Next
        If ListView1.SelectedItems.Count > 0 Then
            ListView1.SelectedItems(0).BackColor = xorange
            ListView1.SelectedItems(0).ForeColor = Color.Black
        End If
    End Sub

    Private Sub ApplySelectedStyle(slot As Integer)
        If ListView1.SelectedItems.Count = 0 Then Return
        Dim styleName As String = ListView1.SelectedItems(0).Text
        Dim basePath As String = "C:\KAVN\Applications\Accompaniment\Styles\"

        Dim bpmText As String = StyleLoader.ReadValue(basePath & styleName & "\bpm.val", "-")
        Dim familyName As String = StyleLoader.ReadValue(basePath & styleName & "\family.word", "Unknown family")

        If slot = 1 Then
            Form1.Style_Name.Text = styleName
            Form1.BPM_Label.Text = If(bpmText <> "-", "BPM " & bpmText, "BPM -")
            Form1.Family_Name.Text = familyName
            My.Settings.Style1 = styleName
        Else
            Form1.Style2_Name.Text = styleName
            Form1.BPM2_Label.Text = If(bpmText <> "-", "BPM " & bpmText, "BPM -")
            Form1.Family2_Name.Text = familyName
            My.Settings.Style2 = styleName
        End If
        My.Settings.Save()

        If Form1._core.State.Active = slot Then
            Form1.RefreshCurrentStyle()
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ApplySelectedStyle(1)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ApplySelectedStyle(2)
    End Sub

    Private Sub Form2_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then
            LoadFolders()
            MarkAndScrollToCurrentStyle()
        End If
    End Sub

End Class