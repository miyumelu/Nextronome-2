<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Styleselect
    Inherits System.Windows.Forms.Form

    'Das Formular überschreibt den Löschvorgang, um die Komponentenliste zu bereinigen.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Wird vom Windows Form-Designer benötigt.
    Private components As System.ComponentModel.IContainer

    'Hinweis: Die folgende Prozedur ist für den Windows Form-Designer erforderlich.
    'Das Bearbeiten ist mit dem Windows Form-Designer möglich.  
    'Das Bearbeiten mit dem Code-Editor ist nicht möglich.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Style1_Button = New Button()
        Style2_Button = New Button()
        Style_Select_Panel = New ListView()
        Close_Button = New Button()
        SuspendLayout()
        ' 
        ' Style1_Button
        ' 
        Style1_Button.FlatStyle = FlatStyle.Flat
        Style1_Button.Font = New Font("Adam Medium", 30F)
        Style1_Button.ForeColor = Color.FromArgb(CByte(102), CByte(246), CByte(159))
        Style1_Button.Location = New Point(8, 650)
        Style1_Button.Name = "Style1_Button"
        Style1_Button.Size = New Size(708, 100)
        Style1_Button.TabIndex = 1
        Style1_Button.Text = "Style 1"
        Style1_Button.UseVisualStyleBackColor = True
        ' 
        ' Style2_Button
        ' 
        Style2_Button.FlatStyle = FlatStyle.Flat
        Style2_Button.Font = New Font("Adam Medium", 30F)
        Style2_Button.ForeColor = Color.FromArgb(CByte(102), CByte(246), CByte(159))
        Style2_Button.Location = New Point(724, 650)
        Style2_Button.Name = "Style2_Button"
        Style2_Button.Size = New Size(708, 100)
        Style2_Button.TabIndex = 2
        Style2_Button.Text = "Style 2"
        Style2_Button.UseVisualStyleBackColor = True
        ' 
        ' Style_Select_Panel
        ' 
        Style_Select_Panel.BackColor = Color.Black
        Style_Select_Panel.BorderStyle = BorderStyle.None
        Style_Select_Panel.Font = New Font("Adam Medium", 30F)
        Style_Select_Panel.ForeColor = Color.White
        Style_Select_Panel.Location = New Point(0, 0)
        Style_Select_Panel.Name = "Style_Select_Panel"
        Style_Select_Panel.Size = New Size(1440, 644)
        Style_Select_Panel.TabIndex = 3
        Style_Select_Panel.UseCompatibleStateImageBehavior = False
        ' 
        ' Close_Button
        ' 
        Close_Button.FlatStyle = FlatStyle.Flat
        Close_Button.Font = New Font("Adam Medium", 30F)
        Close_Button.ForeColor = Color.White
        Close_Button.Location = New Point(8, 760)
        Close_Button.Name = "Close_Button"
        Close_Button.Size = New Size(1424, 80)
        Close_Button.TabIndex = 4
        Close_Button.Text = "Close"
        Close_Button.UseVisualStyleBackColor = True
        ' 
        ' Styleselect
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.Black
        ClientSize = New Size(1440, 847)
        Controls.Add(Close_Button)
        Controls.Add(Style_Select_Panel)
        Controls.Add(Style2_Button)
        Controls.Add(Style1_Button)
        FormBorderStyle = FormBorderStyle.None
        Name = "Styleselect"
        Text = "Styleselect"
        ResumeLayout(False)
    End Sub
    Friend WithEvents Style1_Button As Button
    Friend WithEvents Style2_Button As Button
    Friend WithEvents Style_Select_Panel As ListView
    Friend WithEvents Close_Button As Button
End Class
