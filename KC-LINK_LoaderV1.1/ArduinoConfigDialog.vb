Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Diagnostics

Namespace KC_LINK_LoaderV1
    Public Class ArduinoConfigDialog
        Inherits Form

        ' UI Controls
        Private WithEvents lblArduinoCliPath As Label
        Private WithEvents txtArduinoCliPath As TextBox
        Private WithEvents btnBrowse As Button
        Private WithEvents btnOK As Button
        Private WithEvents btnCancel As Button
        Private WithEvents btnDownload As Button
        Private WithEvents lblStatus As Label

        ' Properties
        Public Property ArduinoCliPath As String = ""

        ' Constructor
        Public Sub New(currentPath As String)
            MyBase.New()
            ArduinoCliPath = currentPath
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            ' Form setup
            Me.Text = "Configure Arduino CLI"
            Me.Size = New Size(550, 180)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Padding = New Padding(10)

            ' Main layout
            Dim mainLayout As New TableLayoutPanel()
            mainLayout.Dock = DockStyle.Fill
            mainLayout.RowCount = 3
            mainLayout.ColumnCount = 1
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))  ' Path selection
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))  ' Status/help
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))  ' Buttons

            ' Path selection panel
            Dim pathPanel As New TableLayoutPanel()
            pathPanel.Dock = DockStyle.Fill
            pathPanel.ColumnCount = 3
            pathPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            pathPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            pathPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))

            lblArduinoCliPath = New Label()
            lblArduinoCliPath.Text = "Arduino CLI Path:"
            lblArduinoCliPath.TextAlign = ContentAlignment.MiddleRight
            lblArduinoCliPath.Dock = DockStyle.Fill

            txtArduinoCliPath = New TextBox()
            txtArduinoCliPath.Dock = DockStyle.Fill
            txtArduinoCliPath.Text = ArduinoCliPath

            btnBrowse = New Button()
            btnBrowse.Text = "Browse..."
            btnBrowse.Dock = DockStyle.Fill

            pathPanel.Controls.Add(lblArduinoCliPath, 0, 0)
            pathPanel.Controls.Add(txtArduinoCliPath, 1, 0)
            pathPanel.Controls.Add(btnBrowse, 2, 0)

            ' Status panel
            Dim statusPanel As New TableLayoutPanel()
            statusPanel.Dock = DockStyle.Fill
            statusPanel.ColumnCount = 2
            statusPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70))
            statusPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))

            lblStatus = New Label()
            lblStatus.Text = "Specify the path to arduino-cli.exe"
            lblStatus.TextAlign = ContentAlignment.MiddleLeft
            lblStatus.Dock = DockStyle.Fill

            btnDownload = New Button()
            btnDownload.Text = "Download Arduino CLI"
            btnDownload.Dock = DockStyle.Fill

            statusPanel.Controls.Add(lblStatus, 0, 0)
            statusPanel.Controls.Add(btnDownload, 1, 0)

            ' Buttons panel
            Dim buttonsPanel As New FlowLayoutPanel()
            buttonsPanel.Dock = DockStyle.Fill
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft
            buttonsPanel.WrapContents = False

            btnCancel = New Button()
            btnCancel.Text = "Cancel"
            btnCancel.Size = New Size(100, 30)
            btnCancel.Margin = New Padding(5)
            btnCancel.DialogResult = DialogResult.Cancel

            btnOK = New Button()
            btnOK.Text = "OK"
            btnOK.Size = New Size(100, 30)
            btnOK.Margin = New Padding(5)
            btnOK.DialogResult = DialogResult.OK

            buttonsPanel.Controls.Add(btnCancel)
            buttonsPanel.Controls.Add(btnOK)

            ' Add panels to main layout
            mainLayout.Controls.Add(pathPanel, 0, 0)
            mainLayout.Controls.Add(statusPanel, 0, 1)
            mainLayout.Controls.Add(buttonsPanel, 0, 2)

            ' Add main layout to form
            Me.Controls.Add(mainLayout)

            ' Wire up events
            AddHandler btnBrowse.Click, AddressOf Browse_Click
            AddHandler btnDownload.Click, AddressOf Download_Click
            AddHandler btnOK.Click, AddressOf OK_Click
            AddHandler btnCancel.Click, AddressOf Cancel_Click

            ' Set accept and cancel buttons
            Me.AcceptButton = btnOK
            Me.CancelButton = btnCancel
        End Sub

        Private Sub Browse_Click(sender As Object, e As EventArgs)
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Title = "Select Arduino CLI Executable"
                openFileDialog.Filter = "Arduino CLI (arduino-cli.exe)|arduino-cli.exe|All Executables (*.exe)|*.exe|All Files (*.*)|*.*"
                openFileDialog.FileName = "arduino-cli.exe"

                If Not String.IsNullOrEmpty(txtArduinoCliPath.Text) Then
                    Dim directory = System.IO.Path.GetDirectoryName(txtArduinoCliPath.Text)
                    If System.IO.Directory.Exists(directory) Then
                        openFileDialog.InitialDirectory = directory
                    End If
                End If

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtArduinoCliPath.Text = openFileDialog.FileName
                    ValidateArduinoCli()
                End If
            End Using
        End Sub

        Private Sub Download_Click(sender As Object, e As EventArgs)
            ' Open Arduino CLI download page in browser
            System.Diagnostics.Process.Start("https://arduino.github.io/arduino-cli/latest/installation/")
        End Sub

        Private Sub OK_Click(sender As Object, e As EventArgs)
            ' Validate Arduino CLI path
            If ValidateArduinoCli() Then
                ArduinoCliPath = txtArduinoCliPath.Text

                ' Log the configuration change
                Try
                    Dim logMessage As String = "2025-08-12 04:19:49 Arduino CLI path updated by Chamil1983: " & txtArduinoCliPath.Text
                    Dim logPath As String = System.IO.Path.Combine(Application.StartupPath, "settings_log.txt")
                    System.IO.File.AppendAllText(logPath, logMessage & Environment.NewLine)
                Catch ex As Exception
                    ' Ignore logging errors
                End Try

                ' Close the dialog with OK result
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Else
                ' Show error message
                MessageBox.Show("Please select a valid Arduino CLI executable (arduino-cli.exe).",
                              "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Sub Cancel_Click(sender As Object, e As EventArgs)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Sub

        Private Function ValidateArduinoCli() As Boolean
            Dim path = txtArduinoCliPath.Text.Trim()

            If String.IsNullOrEmpty(path) Then
                lblStatus.Text = "Please specify the path to arduino-cli.exe"
                lblStatus.ForeColor = Color.Red
                Return False
            End If

            If Not System.IO.File.Exists(path) Then
                lblStatus.Text = "The specified file does not exist"
                lblStatus.ForeColor = Color.Red
                Return False
            End If

            If Not path.EndsWith("arduino-cli.exe", StringComparison.OrdinalIgnoreCase) AndAlso
               Not path.EndsWith("arduino-cli", StringComparison.OrdinalIgnoreCase) Then
                lblStatus.Text = "The file may not be Arduino CLI executable"
                lblStatus.ForeColor = Color.Orange
                Return True ' Allow but warn
            End If

            ' Try to execute Arduino CLI to verify it works
            Try
                Dim processInfo As New ProcessStartInfo()
                processInfo.FileName = path
                processInfo.Arguments = "version"
                processInfo.UseShellExecute = False
                processInfo.RedirectStandardOutput = True
                processInfo.CreateNoWindow = True

                Dim process As New Process()
                process.StartInfo = processInfo
                process.Start()

                Dim output = process.StandardOutput.ReadToEnd()
                process.WaitForExit()

                If process.ExitCode = 0 AndAlso output.Contains("arduino-cli") Then
                    lblStatus.Text = "Arduino CLI validated: " & output.Trim()
                    lblStatus.ForeColor = Color.Green
                    Return True
                Else
                    lblStatus.Text = "Failed to execute Arduino CLI"
                    lblStatus.ForeColor = Color.Red
                    Return False
                End If
            Catch ex As Exception
                lblStatus.Text = "Error validating Arduino CLI: " & ex.Message
                lblStatus.ForeColor = Color.Red
                Return False
            End Try
        End Function
    End Class
End Namespace