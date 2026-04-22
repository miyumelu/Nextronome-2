<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Splashscreen
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
        components = New ComponentModel.Container()
        Logo_IMG = New PictureBox()
        Company_Label = New Label()
        Version_Label = New Label()
        Waitress = New Timer(components)
        CType(Logo_IMG, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Logo_IMG
        ' 
        Logo_IMG.Anchor = AnchorStyles.None
        Logo_IMG.BackgroundImage = My.Resources.Resources.Logo
        Logo_IMG.BackgroundImageLayout = ImageLayout.Zoom
        Logo_IMG.Location = New Point(400, 250)
        Logo_IMG.Name = "Logo_IMG"
        Logo_IMG.Size = New Size(640, 400)
        Logo_IMG.TabIndex = 0
        Logo_IMG.TabStop = False
        ' 
        ' Company_Label
        ' 
        Company_Label.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Company_Label.Font = New Font("Adam Medium", 24F)
        Company_Label.ForeColor = Color.White
        Company_Label.Location = New Point(0, 845)
        Company_Label.Name = "Company_Label"
        Company_Label.Size = New Size(1440, 40)
        Company_Label.TabIndex = 1
        Company_Label.Text = "Kaan Audio Studio"
        Company_Label.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Version_Label
        ' 
        Version_Label.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Version_Label.Font = New Font("Adam Medium", 24F)
        Version_Label.ForeColor = Color.White
        Version_Label.Location = New Point(20, 845)
        Version_Label.Name = "Version_Label"
        Version_Label.Size = New Size(318, 40)
        Version_Label.TabIndex = 2
        Version_Label.Text = "v2.0.000a"
        Version_Label.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Waitress
        ' 
        Waitress.Interval = 4000
        ' 
        ' Splashscreen
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.Black
        BackgroundImageLayout = ImageLayout.None
        ClientSize = New Size(1440, 900)
        Controls.Add(Version_Label)
        Controls.Add(Company_Label)
        Controls.Add(Logo_IMG)
        FormBorderStyle = FormBorderStyle.None
        Name = "Splashscreen"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Splashscreen"
        WindowState = FormWindowState.Maximized
        CType(Logo_IMG, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents Logo_IMG As PictureBox
    Friend WithEvents Company_Label As Label
    Friend WithEvents Version_Label As Label
    Friend WithEvents Waitress As Timer
End Class
