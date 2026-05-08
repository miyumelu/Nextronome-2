Imports System.IO
Imports System.Runtime.InteropServices

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
        Style_Select_Panel.View = View.Details
        Style_Select_Panel.MultiSelect = False
        Style_Select_Panel.FullRowSelect = True
        Style_Select_Panel.HideSelection = False
        Style_Select_Panel.GridLines = True
        Style_Select_Panel.Columns.Clear()
        Style_Select_Panel.Columns.Add("Style", 715, HorizontalAlignment.Center)
        Style_Select_Panel.Columns.Add("Type", 715, HorizontalAlignment.Center)
    End Sub

    Private Sub LoadFolders()
        Dim path As String = "C:\KAVN\Applications\Accompaniment\Styles"
        If Not Directory.Exists(path) Then
            MessageBox.Show("Path not found: " & path)
            Return
        End If

        Style_Select_Panel.Items.Clear()
        For Each folder In Directory.GetDirectories(path)
            Dim item As New ListViewItem(IO.Path.GetFileName(folder))
            item.SubItems.Add("neXtronome Style Package")
            item.ForeColor = Color.White
            item.BackColor = Color.Black
            Style_Select_Panel.Items.Add(item)
        Next
    End Sub

    Private Sub MarkAndScrollToCurrentStyle()
        Dim currentStyle As String = If(MainPage._core.State.Active = 1,
                                        MainPage.Style_Name.Text,
                                        MainPage.Style2_Name.Text)
        If String.IsNullOrEmpty(currentStyle) Then Return

        For Each item As ListViewItem In Style_Select_Panel.Items
            If item.Text = currentStyle Then
                item.BackColor = xorange
                item.ForeColor = Color.Black
                item.Selected = True
                item.EnsureVisible()
                SendMessage(Style_Select_Panel.Handle, LVM_ENSUREVISIBLE, item.Index, 0)
                Exit For
            End If
        Next
    End Sub

    Private Sub ListView1_Click(sender As Object, e As EventArgs)
        For Each item As ListViewItem In Style_Select_Panel.Items
            item.ForeColor = Color.White
            item.BackColor = Color.Black
        Next
        If Style_Select_Panel.SelectedItems.Count > 0 Then
            Style_Select_Panel.SelectedItems(0).BackColor = xorange
            Style_Select_Panel.SelectedItems(0).ForeColor = Color.Black
        End If
    End Sub

    Private Sub ApplySelectedStyle(slot As Integer)
        If Style_Select_Panel.SelectedItems.Count = 0 Then Return
        Dim styleName As String = Style_Select_Panel.SelectedItems(0).Text
        Dim basePath As String = "C:\KAVN\Applications\Accompaniment\Styles\"

        ' UI updates and settings save
        Dim bpmText As String = StyleLoader.ReadValue(basePath & styleName & "\bpm.val", "-")
        Dim familyName As String = StyleLoader.ReadValue(basePath & styleName & "\family.word", "Unknown family")

        If slot = 1 Then
            MainPage.Style_Name.Text = styleName
            MainPage.BPM_Label.Text = If(bpmText <> "-", "BPM " & bpmText, "BPM -")
            MainPage.Family_Name.Text = familyName
            My.Settings.Style1 = styleName
        Else
            MainPage.Style2_Name.Text = styleName
            MainPage.BPM2_Label.Text = If(bpmText <> "-", "BPM " & bpmText, "BPM -")
            MainPage.Family2_Name.Text = familyName
            My.Settings.Style2 = styleName
        End If
        My.Settings.Save()

        If MainPage._core.State.Active = slot Then
            If MainPage._core.Engine.IsRunning Then
                ' Using pending mechanism to avoid disturbing the metronome if it's running
                MainPage._core.State.PendingSlot = slot
                MainPage.SetPanelPending(slot)
                Dim isDirect As Boolean = (MainPage._core.State.ChangeMode = "Direct")
                MainPage._core.PreloadPendingStyle(styleName, isDirect)
            Else
                ' Uses normal loading method if not running.
                MainPage.RefreshCurrentStyle()
            End If
        End If
    End Sub

    Private Sub Style1_Button_Click(sender As Object, e As EventArgs) Handles Style1_Button.Click
        ApplySelectedStyle(1)
    End Sub

    Private Sub Style2_Button_Click(sender As Object, e As EventArgs) Handles Style2_Button.Click
        ApplySelectedStyle(2)
    End Sub

    Private Sub Styleselect_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then
            LoadFolders()
            MarkAndScrollToCurrentStyle()
        End If
    End Sub

    Private Sub Close_Button_Click(sender As Object, e As EventArgs) Handles Close_Button.Click
        Me.Close()
    End Sub
End Class