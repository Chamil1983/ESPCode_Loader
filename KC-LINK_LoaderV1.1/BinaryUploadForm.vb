Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Diagnostics
Imports System.Threading

Namespace KC_LINK_LoaderV1
    Public Class BinaryUploadForm
        Inherits Form

        ' UI Controls
        Private WithEvents lblBootloader As Label
        Private WithEvents txtBootloaderPath As TextBox
        Private WithEvents btnBrowseBootloader As Button
        Private WithEvents lblBootloaderAddr As Label
        Private WithEvents txtBootloaderAddr As TextBox

        Private WithEvents lblPartition As Label
        Private WithEvents txtPartitionPath As TextBox
        Private WithEvents btnBrowsePartition As Button
        Private WithEvents lblPartitionAddr As Label
        Private WithEvents txtPartitionAddr As TextBox

        Private WithEvents lblBootApp0 As Label
        Private WithEvents txtBootApp0Path As TextBox
        Private WithEvents btnBrowseBootApp0 As Button
        Private WithEvents lblBootApp0Addr As Label
        Private WithEvents txtBootApp0Addr As TextBox

        Private WithEvents lblApplication As Label
        Private WithEvents txtApplicationPath As TextBox
        Private WithEvents btnBrowseApplication As Button
        Private WithEvents lblApplicationAddr As Label
        Private WithEvents txtApplicationAddr As TextBox

        Private WithEvents cmbSerialPort As ComboBox
        Private WithEvents lblSerialPort As Label
        Private WithEvents btnRefreshPorts As Button

        Private WithEvents btnUpload As Button
        Private WithEvents btnCancel As Button
        Private WithEvents progressBar As ProgressBar
        Private WithEvents txtOutput As RichTextBox

        Private WithEvents bgWorker As System.ComponentModel.BackgroundWorker
        Private isUploading As Boolean = False
        Private arduinoCliPath As String = ""

        ' Constructor
        Public Sub New()
            MyBase.New()
            InitializeComponent()
            RefreshPortList()
            LoadDefaultAddresses()
            arduinoCliPath = My.Settings.ArduinoCliPath
        End Sub

        Private Sub InitializeComponent()
            ' Form setup
            Me.Text = "ESP32 Binary Upload"
            Me.Size = New Size(700, 600)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            ' Main layout
            Dim mainLayout As New TableLayoutPanel()
            mainLayout.Dock = DockStyle.Fill
            mainLayout.RowCount = 7
            mainLayout.ColumnCount = 1
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 200))  ' Binary files area
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 10))   ' Spacer
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))   ' Serial port area
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 10))   ' Spacer
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))   ' Buttons area
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))   ' Progress bar
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' Output log
            mainLayout.Padding = New Padding(10)

            ' Binary files panel
            Dim binaryFilesPanel As New TableLayoutPanel()
            binaryFilesPanel.Dock = DockStyle.Fill
            binaryFilesPanel.RowCount = 4
            binaryFilesPanel.ColumnCount = 4
            binaryFilesPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 25))
            binaryFilesPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 25))
            binaryFilesPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 25))
            binaryFilesPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 25))
            binaryFilesPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
            binaryFilesPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            binaryFilesPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
            binaryFilesPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))

            ' Bootloader row
            lblBootloader = New Label()
            lblBootloader.Text = "Bootloader:"
            lblBootloader.TextAlign = ContentAlignment.MiddleRight
            lblBootloader.Dock = DockStyle.Fill

            txtBootloaderPath = New TextBox()
            txtBootloaderPath.Dock = DockStyle.Fill

            btnBrowseBootloader = New Button()
            btnBrowseBootloader.Text = "Browse..."
            btnBrowseBootloader.Dock = DockStyle.Fill

            lblBootloaderAddr = New Label()
            lblBootloaderAddr.Text = "Address:"
            lblBootloaderAddr.TextAlign = ContentAlignment.MiddleRight
            lblBootloaderAddr.Dock = DockStyle.Fill

            txtBootloaderAddr = New TextBox()
            txtBootloaderAddr.Text = "0x1000"
            txtBootloaderAddr.Width = 80
            txtBootloaderAddr.Dock = DockStyle.Fill

            binaryFilesPanel.Controls.Add(lblBootloader, 0, 0)
            binaryFilesPanel.Controls.Add(txtBootloaderPath, 1, 0)
            binaryFilesPanel.Controls.Add(btnBrowseBootloader, 2, 0)
            binaryFilesPanel.Controls.Add(txtBootloaderAddr, 3, 0)

            ' Partition table row
            lblPartition = New Label()
            lblPartition.Text = "Partition Table:"
            lblPartition.TextAlign = ContentAlignment.MiddleRight
            lblPartition.Dock = DockStyle.Fill

            txtPartitionPath = New TextBox()
            txtPartitionPath.Dock = DockStyle.Fill

            btnBrowsePartition = New Button()
            btnBrowsePartition.Text = "Browse..."
            btnBrowsePartition.Dock = DockStyle.Fill

            lblPartitionAddr = New Label()
            lblPartitionAddr.Text = "Address:"
            lblPartitionAddr.TextAlign = ContentAlignment.MiddleRight
            lblPartitionAddr.Dock = DockStyle.Fill

            txtPartitionAddr = New TextBox()
            txtPartitionAddr.Text = "0x8000"
            txtPartitionAddr.Width = 80
            txtPartitionAddr.Dock = DockStyle.Fill

            binaryFilesPanel.Controls.Add(lblPartition, 0, 1)
            binaryFilesPanel.Controls.Add(txtPartitionPath, 1, 1)
            binaryFilesPanel.Controls.Add(btnBrowsePartition, 2, 1)
            binaryFilesPanel.Controls.Add(txtPartitionAddr, 3, 1)

            ' Boot App0 row
            lblBootApp0 = New Label()
            lblBootApp0.Text = "Boot App 0:"
            lblBootApp0.TextAlign = ContentAlignment.MiddleRight
            lblBootApp0.Dock = DockStyle.Fill

            txtBootApp0Path = New TextBox()
            txtBootApp0Path.Dock = DockStyle.Fill

            btnBrowseBootApp0 = New Button()
            btnBrowseBootApp0.Text = "Browse..."
            btnBrowseBootApp0.Dock = DockStyle.Fill

            lblBootApp0Addr = New Label()
            lblBootApp0Addr.Text = "Address:"
            lblBootApp0Addr.TextAlign = ContentAlignment.MiddleRight
            lblBootApp0Addr.Dock = DockStyle.Fill

            txtBootApp0Addr = New TextBox()
            txtBootApp0Addr.Text = "0xe000"
            txtBootApp0Addr.Width = 80
            txtBootApp0Addr.Dock = DockStyle.Fill

            binaryFilesPanel.Controls.Add(lblBootApp0, 0, 2)
            binaryFilesPanel.Controls.Add(txtBootApp0Path, 1, 2)
            binaryFilesPanel.Controls.Add(btnBrowseBootApp0, 2, 2)
            binaryFilesPanel.Controls.Add(txtBootApp0Addr, 3, 2)

            ' Application row
            lblApplication = New Label()
            lblApplication.Text = "Application:"
            lblApplication.TextAlign = ContentAlignment.MiddleRight
            lblApplication.Dock = DockStyle.Fill

            txtApplicationPath = New TextBox()
            txtApplicationPath.Dock = DockStyle.Fill

            btnBrowseApplication = New Button()
            btnBrowseApplication.Text = "Browse..."
            btnBrowseApplication.Dock = DockStyle.Fill

            lblApplicationAddr = New Label()
            lblApplicationAddr.Text = "Address:"
            lblApplicationAddr.TextAlign = ContentAlignment.MiddleRight
            lblApplicationAddr.Dock = DockStyle.Fill

            txtApplicationAddr = New TextBox()
            txtApplicationAddr.Text = "0x10000"
            txtApplicationAddr.Width = 80
            txtApplicationAddr.Dock = DockStyle.Fill

            binaryFilesPanel.Controls.Add(lblApplication, 0, 3)
            binaryFilesPanel.Controls.Add(txtApplicationPath, 1, 3)
            binaryFilesPanel.Controls.Add(btnBrowseApplication, 2, 3)
            binaryFilesPanel.Controls.Add(txtApplicationAddr, 3, 3)

            ' Serial port panel
            Dim serialPortPanel As New TableLayoutPanel()
            serialPortPanel.Dock = DockStyle.Fill
            serialPortPanel.ColumnCount = 3
            serialPortPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
            serialPortPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            serialPortPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))

            lblSerialPort = New Label()
            lblSerialPort.Text = "Serial Port:"
            lblSerialPort.TextAlign = ContentAlignment.MiddleRight
            lblSerialPort.Dock = DockStyle.Fill

            cmbSerialPort = New ComboBox()
            cmbSerialPort.DropDownStyle = ComboBoxStyle.DropDownList
            cmbSerialPort.Dock = DockStyle.Fill

            btnRefreshPorts = New Button()
            btnRefreshPorts.Text = "Refresh"
            btnRefreshPorts.Dock = DockStyle.Fill

            serialPortPanel.Controls.Add(lblSerialPort, 0, 0)
            serialPortPanel.Controls.Add(cmbSerialPort, 1, 0)
            serialPortPanel.Controls.Add(btnRefreshPorts, 2, 0)

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

            btnUpload = New Button()
            btnUpload.Text = "Upload"
            btnUpload.Size = New Size(100, 30)
            btnUpload.Margin = New Padding(5)
            btnUpload.BackColor = Color.LightGreen

            buttonsPanel.Controls.Add(btnCancel)
            buttonsPanel.Controls.Add(btnUpload)

            ' Progress Bar
            progressBar = New ProgressBar()
            progressBar.Dock = DockStyle.Fill
            progressBar.Minimum = 0
            progressBar.Maximum = 100
            progressBar.Value = 0

            ' Output Text Box
            txtOutput = New RichTextBox()
            txtOutput.Dock = DockStyle.Fill
            txtOutput.ReadOnly = True
            txtOutput.BackColor = Color.Black
            txtOutput.ForeColor = Color.LightGreen
            txtOutput.Font = New Font("Consolas", 9)

            ' Add all panels to main layout
            mainLayout.Controls.Add(binaryFilesPanel, 0, 0)
            mainLayout.Controls.Add(serialPortPanel, 0, 2)
            mainLayout.Controls.Add(buttonsPanel, 0, 4)
            mainLayout.Controls.Add(progressBar, 0, 5)
            mainLayout.Controls.Add(txtOutput, 0, 6)

            ' Add main layout to form
            Me.Controls.Add(mainLayout)

            ' Set up background worker
            bgWorker = New System.ComponentModel.BackgroundWorker()
            bgWorker.WorkerReportsProgress = True
            bgWorker.WorkerSupportsCancellation = True

            ' Wire up events
            AddHandler btnBrowseBootloader.Click, AddressOf BrowseBootloader_Click
            AddHandler btnBrowsePartition.Click, AddressOf BrowsePartition_Click
            AddHandler btnBrowseBootApp0.Click, AddressOf BrowseBootApp0_Click
            AddHandler btnBrowseApplication.Click, AddressOf BrowseApplication_Click
            AddHandler btnRefreshPorts.Click, AddressOf RefreshPorts_Click
            AddHandler btnUpload.Click, AddressOf Upload_Click
            AddHandler btnCancel.Click, AddressOf Cancel_Click
            AddHandler bgWorker.DoWork, AddressOf BgWorker_DoWork
            AddHandler bgWorker.ProgressChanged, AddressOf BgWorker_ProgressChanged
            AddHandler bgWorker.RunWorkerCompleted, AddressOf BgWorker_RunWorkerCompleted

            ' Set accept and cancel buttons
            Me.AcceptButton = btnUpload
            Me.CancelButton = btnCancel
        End Sub

        Private Sub BrowseBootloader_Click(sender As Object, e As EventArgs)
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Bootloader Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtBootloaderPath.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub BrowsePartition_Click(sender As Object, e As EventArgs)
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Partition Table Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtPartitionPath.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub BrowseBootApp0_Click(sender As Object, e As EventArgs)
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Boot App 0 Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtBootApp0Path.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub BrowseApplication_Click(sender As Object, e As EventArgs)
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Application Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtApplicationPath.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub RefreshPorts_Click(sender As Object, e As EventArgs)
            RefreshPortList()
        End Sub

        Private Sub RefreshPortList()
            cmbSerialPort.Items.Clear()

            ' Get all available COM ports
            For Each port As String In My.Computer.Ports.SerialPortNames
                cmbSerialPort.Items.Add(port)
            Next

            If cmbSerialPort.Items.Count > 0 Then
                cmbSerialPort.SelectedIndex = 0
                AppendToOutput($"Found {cmbSerialPort.Items.Count} serial ports")
            Else
                AppendToOutput("No serial ports found")
            End If
        End Sub

        Private Sub LoadDefaultAddresses()
            ' Standard ESP32 flash addresses
            txtBootloaderAddr.Text = "0x1000"
            txtPartitionAddr.Text = "0x8000"
            txtBootApp0Addr.Text = "0xe000"
            txtApplicationAddr.Text = "0x10000"
        End Sub

        Private Sub Upload_Click(sender As Object, e As EventArgs)
            If cmbSerialPort.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a serial port for uploading.",
                           "No Serial Port Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If isUploading Then
                ' Cancel upload
                If bgWorker.IsBusy Then
                    bgWorker.CancelAsync()
                End If
                btnUpload.Text = "Upload"
                btnUpload.BackColor = Color.LightGreen
                isUploading = False
                AppendToOutput("Upload cancelled")
            Else
                ' Validate input files
                If Not ValidateRequiredFiles() Then
                    Return
                End If

                ' Start upload
                txtOutput.Clear()
                progressBar.Value = 0
                AppendToOutput("Starting binary upload...")

                btnUpload.Text = "Cancel"
                btnUpload.BackColor = Color.IndianRed
                isUploading = True

                ' Start upload in background
                bgWorker.RunWorkerAsync(cmbSerialPort.SelectedItem.ToString())
            End If
        End Sub

        Private Function ValidateRequiredFiles() As Boolean
            ' At minimum, we need the application binary file
            If String.IsNullOrEmpty(txtApplicationPath.Text) OrElse Not File.Exists(txtApplicationPath.Text) Then
                MessageBox.Show("Please select a valid application binary file.",
                           "Missing Required File", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            ' Validate addresses (must be in hex format)
            Try
                If Not String.IsNullOrEmpty(txtBootloaderPath.Text) Then
                    Convert.ToInt32(txtBootloaderAddr.Text, 16)
                End If
                If Not String.IsNullOrEmpty(txtPartitionPath.Text) Then
                    Convert.ToInt32(txtPartitionAddr.Text, 16)
                End If
                If Not String.IsNullOrEmpty(txtBootApp0Path.Text) Then
                    Convert.ToInt32(txtBootApp0Addr.Text, 16)
                End If
                Convert.ToInt32(txtApplicationAddr.Text, 16)
            Catch ex As Exception
                MessageBox.Show("One or more addresses are not in valid hexadecimal format (e.g., 0x1000).",
                           "Invalid Address Format", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End Try

            Return True
        End Function

        Private Sub Cancel_Click(sender As Object, e As EventArgs)
            If isUploading AndAlso bgWorker.IsBusy Then
                bgWorker.CancelAsync()
                isUploading = False
                btnUpload.Text = "Upload"
                btnUpload.BackColor = Color.LightGreen
            Else
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            End If
        End Sub

        Private Sub BgWorker_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs)
            Dim worker As System.ComponentModel.BackgroundWorker = CType(sender, System.ComponentModel.BackgroundWorker)
            Dim port As String = CType(e.Argument, String)
            Dim uploadSuccess As Boolean = True

            ' Get the file paths and addresses from the UI thread
            Dim bootloaderPath As String = ""
            Dim bootloaderAddr As String = ""
            Dim partitionPath As String = ""
            Dim partitionAddr As String = ""
            Dim bootApp0Path As String = ""
            Dim bootApp0Addr As String = ""
            Dim applicationPath As String = ""
            Dim applicationAddr As String = ""

            Me.Invoke(Sub()
                          bootloaderPath = txtBootloaderPath.Text
                          bootloaderAddr = txtBootloaderAddr.Text
                          partitionPath = txtPartitionPath.Text
                          partitionAddr = txtPartitionAddr.Text
                          bootApp0Path = txtBootApp0Path.Text
                          bootApp0Addr = txtBootApp0Addr.Text
                          applicationPath = txtApplicationPath.Text
                          applicationAddr = txtApplicationAddr.Text
                      End Sub)

            ' Build esptool.py command for flashing
            Dim command As String = "write_flash --chip esp32 --port " & port & " --baud 460800"

            ' Add flash options
            command += " --flash_mode dio --flash_freq 40m --flash_size detect"

            ' Add flash addresses and files
            If Not String.IsNullOrEmpty(bootloaderPath) AndAlso File.Exists(bootloaderPath) Then
                command += " " & bootloaderAddr & " """ & bootloaderPath & """"
            End If

            If Not String.IsNullOrEmpty(partitionPath) AndAlso File.Exists(partitionPath) Then
                command += " " & partitionAddr & " """ & partitionPath & """"
            End If

            If Not String.IsNullOrEmpty(bootApp0Path) AndAlso File.Exists(bootApp0Path) Then
                command += " " & bootApp0Addr & " """ & bootApp0Path & """"
            End If

            command += " " & applicationAddr & " """ & applicationPath & """"

            worker.ReportProgress(10, "Preparing to flash binaries...")
            worker.ReportProgress(15, "Command: " & command)

            ' Execute the esptool.py command
            Try
                Dim processInfo As New ProcessStartInfo(arduinoCliPath)
                processInfo.Arguments = "esptool " & command
                processInfo.UseShellExecute = False
                processInfo.CreateNoWindow = True
                processInfo.RedirectStandardOutput = True
                processInfo.RedirectStandardError = True

                Dim process As New Process()
                process.StartInfo = processInfo
                process.EnableRaisingEvents = True

                ' Setup output handlers
                AddHandler process.OutputDataReceived, Sub(s, outputData)
                                                           If Not String.IsNullOrEmpty(outputData.Data) Then
                                                               worker.ReportProgress(0, outputData.Data)

                                                               ' Update progress based on output
                                                               If outputData.Data.Contains("Writing at") Then
                                                                   worker.ReportProgress(30, Nothing)
                                                               ElseIf outputData.Data.Contains("Written ") Then
                                                                   worker.ReportProgress(50, Nothing)
                                                               ElseIf outputData.Data.Contains("Verifying ") Then
                                                                   worker.ReportProgress(70, Nothing)
                                                               ElseIf outputData.Data.Contains("Hash of data verified") Then
                                                                   worker.ReportProgress(90, Nothing)
                                                               ElseIf outputData.Data.Contains("Hard resetting") Then
                                                                   worker.ReportProgress(95, Nothing)
                                                               End If
                                                           End If
                                                       End Sub

                AddHandler process.ErrorDataReceived, Sub(s, errorData)
                                                          If Not String.IsNullOrEmpty(errorData.Data) Then
                                                              worker.ReportProgress(0, "ERROR: " & errorData.Data)
                                                              uploadSuccess = False
                                                          End If
                                                      End Sub

                worker.ReportProgress(20, "Starting upload process...")

                process.Start()
                process.BeginOutputReadLine()
                process.BeginErrorReadLine()

                ' Wait for process to exit or cancellation
                While Not process.HasExited
                    If worker.CancellationPending Then
                        process.Kill()
                        e.Cancel = True
                        Return
                    End If
                    Thread.Sleep(100)
                End While

                ' Process completed, check exit code
                If process.ExitCode <> 0 Then
                    worker.ReportProgress(0, "Upload failed with exit code: " & process.ExitCode)
                    uploadSuccess = False
                Else
                    worker.ReportProgress(100, "Upload completed successfully!")
                End If

            Catch ex As Exception
                worker.ReportProgress(0, "Error during upload: " & ex.Message)
                uploadSuccess = False
            End Try

            e.Result = uploadSuccess
        End Sub

        Private Sub BgWorker_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs)
            ' Update progress bar for percentage changes
            If e.ProgressPercentage > 0 Then
                progressBar.Value = Math.Min(e.ProgressPercentage, 100)
            End If

            ' Update output with message if provided
            If e.UserState IsNot Nothing Then
                AppendToOutput(e.UserState.ToString())
            End If
        End Sub

        Private Sub BgWorker_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs)
            ' Reset UI state
            btnUpload.Text = "Upload"
            btnUpload.BackColor = Color.LightGreen
            isUploading = False

            If e.Cancelled Then
                AppendToOutput("Upload cancelled by user")
                progressBar.Value = 0
            ElseIf e.Error IsNot Nothing Then
                AppendToOutput("Error during upload: " & e.Error.Message)
                MessageBox.Show("Error during upload: " & e.Error.Message, "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ElseIf CBool(e.Result) Then
                AppendToOutput("Binary upload completed successfully!")
                progressBar.Value = 100
            Else
                AppendToOutput("Binary upload failed. Check output for details.")
                MessageBox.Show("Binary upload failed. Check output for details.", "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Sub AppendToOutput(text As String)
            If txtOutput.InvokeRequired Then
                txtOutput.Invoke(Sub() AppendToOutput(text))
            Else
                txtOutput.AppendText(text & Environment.NewLine)
                txtOutput.SelectionStart = txtOutput.Text.Length
                txtOutput.ScrollToCaret()
            End If
        End Sub
    End Class
End Namespace