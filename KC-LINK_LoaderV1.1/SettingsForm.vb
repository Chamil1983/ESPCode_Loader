Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing

Public Class SettingsForm
    Inherits Form
    Implements IDisposable

    ' Form controls
    Private txtArduinoCliPath As TextBox
    Private btnBrowseCli As Button
    Private chkEnableLogging As CheckBox
    Private txtDefaultSketchDir As TextBox
    Private btnBrowseSketchDir As Button
    Private txtHardwarePath As TextBox
    Private btnBrowseHardware As Button
    Private numCompileTimeout As NumericUpDown
    Private btnOK As Button
    Private btnCancel As Button

    ' Constructor
    Public Sub New()
        MyBase.New()
        InitializeComponent()
        LoadSettings()
    End Sub

    ' Initialize form components
    Private Sub InitializeComponent()
        ' Form setup
        Me.Text = "KC-Link Loader Settings"
        Me.ClientSize = New Size(500, 300)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent

        ' Create layout
        Dim layoutPanel As New TableLayoutPanel()
        layoutPanel.Dock = DockStyle.Fill
        layoutPanel.Padding = New Padding(10)
        layoutPanel.ColumnCount = 3
        layoutPanel.RowCount = 6
        layoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        layoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        layoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
        layoutPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layoutPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layoutPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layoutPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layoutPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layoutPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        ' Arduino CLI path
        Dim lblArduinoCliPath As New Label()
        lblArduinoCliPath.Text = "Arduino CLI Path:"
        lblArduinoCliPath.TextAlign = ContentAlignment.MiddleRight
        lblArduinoCliPath.Dock = DockStyle.Fill

        txtArduinoCliPath = New TextBox()
        txtArduinoCliPath.Dock = DockStyle.Fill

        btnBrowseCli = New Button()
        btnBrowseCli.Text = "Browse..."
        btnBrowseCli.Dock = DockStyle.Fill

        layoutPanel.Controls.Add(lblArduinoCliPath, 0, 0)
        layoutPanel.Controls.Add(txtArduinoCliPath, 1, 0)
        layoutPanel.Controls.Add(btnBrowseCli, 2, 0)

        ' Default sketch directory
        Dim lblSketchDir As New Label()
        lblSketchDir.Text = "Default Sketch Directory:"
        lblSketchDir.TextAlign = ContentAlignment.MiddleRight
        lblSketchDir.Dock = DockStyle.Fill

        txtDefaultSketchDir = New TextBox()
        txtDefaultSketchDir.Dock = DockStyle.Fill

        btnBrowseSketchDir = New Button()
        btnBrowseSketchDir.Text = "Browse..."
        btnBrowseSketchDir.Dock = DockStyle.Fill

        layoutPanel.Controls.Add(lblSketchDir, 0, 1)
        layoutPanel.Controls.Add(txtDefaultSketchDir, 1, 1)
        layoutPanel.Controls.Add(btnBrowseSketchDir, 2, 1)

        ' ESP32 Hardware Path
        Dim lblHardwarePath As New Label()
        lblHardwarePath.Text = "ESP32 Hardware Path:"
        lblHardwarePath.TextAlign = ContentAlignment.MiddleRight
        lblHardwarePath.Dock = DockStyle.Fill

        txtHardwarePath = New TextBox()
        txtHardwarePath.Dock = DockStyle.Fill

        btnBrowseHardware = New Button()
        btnBrowseHardware.Text = "Browse..."
        btnBrowseHardware.Dock = DockStyle.Fill

        layoutPanel.Controls.Add(lblHardwarePath, 0, 2)
        layoutPanel.Controls.Add(txtHardwarePath, 1, 2)
        layoutPanel.Controls.Add(btnBrowseHardware, 2, 2)

        ' Compile timeout
        Dim lblCompileTimeout As New Label()
        lblCompileTimeout.Text = "Compile Timeout (sec):"
        lblCompileTimeout.TextAlign = ContentAlignment.MiddleRight
        lblCompileTimeout.Dock = DockStyle.Fill

        numCompileTimeout = New NumericUpDown()
        numCompileTimeout.Minimum = 30
        numCompileTimeout.Maximum = 600
        numCompileTimeout.Value = 300
        numCompileTimeout.Dock = DockStyle.Left
        numCompileTimeout.Width = 100

        layoutPanel.Controls.Add(lblCompileTimeout, 0, 3)
        layoutPanel.Controls.Add(numCompileTimeout, 1, 3)

        ' Enable logging checkbox
        chkEnableLogging = New CheckBox()
        chkEnableLogging.Text = "Enable Logging"
        chkEnableLogging.Dock = DockStyle.Fill

        layoutPanel.Controls.Add(chkEnableLogging, 1, 4)

        ' Buttons panel
        Dim buttonPanel As New FlowLayoutPanel()
        buttonPanel.Dock = DockStyle.Fill
        buttonPanel.FlowDirection = FlowDirection.RightToLeft
        buttonPanel.WrapContents = False
        buttonPanel.AutoSize = True

        btnCancel = New Button()
        btnCancel.Text = "Cancel"
        btnCancel.DialogResult = DialogResult.Cancel
        btnCancel.Size = New Size(80, 30)
        btnCancel.Margin = New Padding(5)

        btnOK = New Button()
        btnOK.Text = "OK"
        btnOK.DialogResult = DialogResult.OK
        btnOK.Size = New Size(80, 30)
        btnOK.Margin = New Padding(5)

        buttonPanel.Controls.Add(btnCancel)
        buttonPanel.Controls.Add(btnOK)

        layoutPanel.Controls.Add(buttonPanel, 1, 5)
        layoutPanel.SetColumnSpan(buttonPanel, 2)

        ' Add main layout to form
        Me.Controls.Add(layoutPanel)

        ' Wire up events
        AddHandler btnBrowseCli.Click, AddressOf btnBrowseCli_Click
        AddHandler btnBrowseSketchDir.Click, AddressOf btnBrowseSketchDir_Click
        AddHandler btnBrowseHardware.Click, AddressOf btnBrowseHardware_Click
        AddHandler btnOK.Click, AddressOf btnOK_Click

        ' Set accept and cancel buttons
        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel
    End Sub

    ' Load settings from app settings
    Private Sub LoadSettings()
        txtArduinoCliPath.Text = My.Settings.ArduinoCliPath
        txtDefaultSketchDir.Text = My.Settings.DefaultSketchDir
        txtHardwarePath.Text = My.Settings.HardwarePath

        ' Set default timeout if not set
        If My.Settings.CompileTimeout <= 0 Then
            numCompileTimeout.Value = 300 ' Default 5 minutes
        Else
            numCompileTimeout.Value = My.Settings.CompileTimeout
        End If

        chkEnableLogging.Checked = My.Settings.EnableLogging
    End Sub

    ' Save settings
    Private Sub SaveSettings()
        My.Settings.ArduinoCliPath = txtArduinoCliPath.Text
        My.Settings.DefaultSketchDir = txtDefaultSketchDir.Text
        My.Settings.HardwarePath = txtHardwarePath.Text
        My.Settings.CompileTimeout = CInt(numCompileTimeout.Value)
        My.Settings.EnableLogging = chkEnableLogging.Checked
        My.Settings.Save()
    End Sub

    ' Event handlers
    Private Sub btnBrowseCli_Click(sender As Object, e As EventArgs)
        Using dialog As New OpenFileDialog()
            dialog.Title = "Select Arduino CLI executable"
            dialog.Filter = "Arduino CLI|arduino-cli.exe|All files|*.*"

            If dialog.ShowDialog() = DialogResult.OK Then
                txtArduinoCliPath.Text = dialog.FileName
            End If
        End Using
    End Sub

    Private Sub btnBrowseSketchDir_Click(sender As Object, e As EventArgs)
        Using dialog As New FolderBrowserDialog()
            dialog.Description = "Select default sketch directory"

            If dialog.ShowDialog() = DialogResult.OK Then
                txtDefaultSketchDir.Text = dialog.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnBrowseHardware_Click(sender As Object, e As EventArgs)
        Using dialog As New FolderBrowserDialog()
            dialog.Description = "Select ESP32 hardware directory"

            If dialog.ShowDialog() = DialogResult.OK Then
                txtHardwarePath.Text = dialog.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnOK_Click(sender As Object, e As EventArgs)
        ' Validate settings
        If String.IsNullOrEmpty(txtArduinoCliPath.Text) Then
            MessageBox.Show("Arduino CLI path cannot be empty.", "Validation Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.DialogResult = DialogResult.None
            Return
        End If

        ' Save settings
        SaveSettings()
    End Sub
End Class