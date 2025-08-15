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
        Private esptoolPath As String = ""

        ' Constructor
        Public Sub New()
            MyBase.New()
            InitializeComponent()
            RefreshPortList()
            LoadDefaultAddresses()
            FindEsptoolPath()
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
                AppendToOutput("Found " & cmbSerialPort.Items.Count & " serial ports")
            Else
                AppendToOutput("No serial ports found")
            End If
        End Sub

        Private Sub LoadDefaultAddresses()
            ' Standard ESP32 flash addresses - updated based on ESP32 documentation
            txtBootloaderAddr.Text = KC_LINK_LoaderV1.BinaryExporter.DefaultBootloaderAddress    ' 0x1000
            txtPartitionAddr.Text = KC_LINK_LoaderV1.BinaryExporter.DefaultPartitionAddress      ' 0x8000
            txtBootApp0Addr.Text = KC_LINK_LoaderV1.BinaryExporter.DefaultBootApp0Address        ' 0xe000
            txtApplicationAddr.Text = KC_LINK_LoaderV1.BinaryExporter.DefaultApplicationAddress  ' 0x10000

            AppendToOutput("Using standard ESP32 flash addresses:")
            AppendToOutput("Bootloader: " & txtBootloaderAddr.Text)
            AppendToOutput("Partition Table: " & txtPartitionAddr.Text)
            AppendToOutput("Boot App 0: " & txtBootApp0Addr.Text)
            AppendToOutput("Application: " & txtApplicationAddr.Text)
        End Sub

        Private Sub FindEsptoolPath()
            ' Try to find esptool.py or esptool.exe in common locations
            esptoolPath = ""

            ' First check if arduino-cli path is set
            Dim arduinoCliPath As String = My.Settings.ArduinoCliPath
            If Not String.IsNullOrEmpty(arduinoCliPath) Then
                Dim arduinoDir = Path.GetDirectoryName(arduinoCliPath)

                ' Check for esptool in the same directory as arduino-cli
                Dim esptoolExePath = Path.Combine(arduinoDir, "esptool.exe")
                If File.Exists(esptoolExePath) Then
                    esptoolPath = esptoolExePath
                    AppendToOutput("Found esptool.exe at: " & esptoolPath)
                    Return
                End If

                ' Check in packages directory
                Dim packagesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "arduino15", "packages")

                If Directory.Exists(packagesDir) Then
                    Dim espToolPaths = Directory.GetFiles(packagesDir, "esptool*.exe", SearchOption.AllDirectories)
                    If espToolPaths.Length > 0 Then
                        esptoolPath = espToolPaths(0)
                        AppendToOutput("Found esptool.exe at: " & esptoolPath)
                        Return
                    End If
                End If
            End If

            ' Try to find Python and esptool.py
            Try
                Dim pythonPath As String = ""
                Dim pythonProcess As New Process()
                pythonProcess.StartInfo.FileName = "python"
                pythonProcess.StartInfo.Arguments = "--version"
                pythonProcess.StartInfo.UseShellExecute = False
                pythonProcess.StartInfo.CreateNoWindow = True
                pythonProcess.StartInfo.RedirectStandardOutput = True

                Try
                    pythonProcess.Start()
                    pythonProcess.WaitForExit()
                    pythonPath = "python"
                    AppendToOutput("Found Python in PATH")
                Catch ex As Exception
                    ' Python not in PATH, try python3
                    Try
                        pythonProcess.StartInfo.FileName = "python3"
                        pythonProcess.Start()
                        pythonProcess.WaitForExit()
                        pythonPath = "python3"
                        AppendToOutput("Found Python3 in PATH")
                    Catch ex2 As Exception
                        ' Neither python nor python3 found
                        AppendToOutput("Python not found in PATH. Please install Python and esptool.")
                    End Try
                End Try

                If Not String.IsNullOrEmpty(pythonPath) Then
                    ' Check if esptool.py is installed
                    Dim esptoolProcess As New Process()
                    esptoolProcess.StartInfo.FileName = pythonPath
                    esptoolProcess.StartInfo.Arguments = "-m esptool version"
                    esptoolProcess.StartInfo.UseShellExecute = False
                    esptoolProcess.StartInfo.CreateNoWindow = True
                    esptoolProcess.StartInfo.RedirectStandardOutput = True

                    Try
                        esptoolProcess.Start()
                        Dim output = esptoolProcess.StandardOutput.ReadToEnd()
                        esptoolProcess.WaitForExit()

                        If esptoolProcess.ExitCode = 0 Then
                            esptoolPath = pythonPath & " -m esptool"
                            AppendToOutput("Using esptool.py module: " & esptoolPath)
                            AppendToOutput("esptool version: " & output.Trim())
                            Return
                        End If
                    Catch ex As Exception
                        ' esptool.py not installed as module
                        AppendToOutput("esptool.py module not found")
                    End Try
                End If
            Catch ex As Exception
                AppendToOutput("Error checking for Python and esptool: " & ex.Message)
            End Try

            ' If we get here, no esptool found
            esptoolPath = ""
            AppendToOutput("WARNING: Could not find esptool. Upload may fail.")
            AppendToOutput("Please install esptool via 'pip install esptool' or specify path manually.")
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

                ' Check if esptool was found
                If String.IsNullOrEmpty(esptoolPath) Then
                    Dim result = MessageBox.Show(
                        "esptool was not found automatically. Upload may fail." & Environment.NewLine &
                        "Do you want to continue anyway?" & Environment.NewLine &
                        "(You may need to install esptool via 'pip install esptool')",
                        "esptool Not Found",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning)

                    If result = DialogResult.No Then
                        Return
                    End If
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
            Dim chipType As String = "esp32" ' Default chip type

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

            ' Try to detect chip type using esptool if not already detected
            Try
                worker.ReportProgress(10, "Attempting to detect chip type using esptool...")
                Dim chipDetectProcessInfo As New ProcessStartInfo()

                If esptoolPath.EndsWith(".exe") Then
                    chipDetectProcessInfo.FileName = esptoolPath
                    chipDetectProcessInfo.Arguments = $"--port {port} chip_id"
                ElseIf esptoolPath.Contains(" -m ") Then
                    Dim parts = esptoolPath.Split(New String() {" -m "}, StringSplitOptions.None)
                    chipDetectProcessInfo.FileName = parts(0) ' python or python3
                    chipDetectProcessInfo.Arguments = "-m esptool --port " & port & " chip_id"
                ElseIf esptoolPath.EndsWith(".py") Then
                    chipDetectProcessInfo.FileName = "python"
                    chipDetectProcessInfo.Arguments = """" & esptoolPath & """ --port " & port & " chip_id"
                End If

                chipDetectProcessInfo.UseShellExecute = False
                chipDetectProcessInfo.CreateNoWindow = True
                chipDetectProcessInfo.RedirectStandardOutput = True
                chipDetectProcessInfo.RedirectStandardError = True

                Dim chipDetectProcess As New Process()
                chipDetectProcess.StartInfo = chipDetectProcessInfo

                Dim chipOutput As New System.Text.StringBuilder()

                AddHandler chipDetectProcess.OutputDataReceived, Sub(s, outputData)
                                                                     If Not String.IsNullOrEmpty(outputData.Data) Then
                                                                         chipOutput.AppendLine(outputData.Data)
                                                                     End If
                                                                 End Sub

                AddHandler chipDetectProcess.ErrorDataReceived, Sub(s, errorData)
                                                                    If Not String.IsNullOrEmpty(errorData.Data) Then
                                                                        chipOutput.AppendLine("ERROR: " & errorData.Data)
                                                                    End If
                                                                End Sub

                chipDetectProcess.Start()
                chipDetectProcess.BeginOutputReadLine()
                chipDetectProcess.BeginErrorReadLine()

                ' Wait max 5 seconds for chip detection
                chipDetectProcess.WaitForExit(5000)

                Dim chipOutputStr = chipOutput.ToString()
                worker.ReportProgress(15, "Chip detection output: " & chipOutputStr)

                ' Check for specific chip mentions in the output
                If chipOutputStr.Contains("ESP32-S3") Then
                    chipType = "esp32s3"
                    worker.ReportProgress(15, "Detected ESP32-S3 chip via esptool")
                ElseIf chipOutputStr.Contains("ESP32-S2") Then
                    chipType = "esp32s2"
                    worker.ReportProgress(15, "Detected ESP32-S2 chip via esptool")
                ElseIf chipOutputStr.Contains("ESP32-C3") Then
                    chipType = "esp32c3"
                    worker.ReportProgress(15, "Detected ESP32-C3 chip via esptool")
                End If
            Catch ex As Exception
                worker.ReportProgress(15, "Error detecting chip type: " & ex.Message)
                worker.ReportProgress(15, "Continuing with default chip type: " & chipType)
            End Try

            ' Build file list for flashing
            Dim fileList As String = ""

            If Not String.IsNullOrEmpty(bootloaderPath) AndAlso File.Exists(bootloaderPath) Then
                fileList += bootloaderAddr & " """ & bootloaderPath & """ "
            End If

            If Not String.IsNullOrEmpty(partitionPath) AndAlso File.Exists(partitionPath) Then
                fileList += partitionAddr & " """ & partitionPath & """ "
            End If

            If Not String.IsNullOrEmpty(bootApp0Path) AndAlso File.Exists(bootApp0Path) Then
                fileList += bootApp0Addr & " """ & bootApp0Path & """ "
            End If

            If Not String.IsNullOrEmpty(applicationPath) AndAlso File.Exists(applicationPath) Then
                fileList += applicationAddr & " """ & applicationPath & """"
            End If

            worker.ReportProgress(20, "Preparing to flash binaries...")
            worker.ReportProgress(20, $"Command: write_flash --chip {chipType} --port {port} --baud 460800 --flash_mode dio --flash_freq 40m --flash_size detect {fileList}")

            ' Execute the esptool command
            Try
                Dim processInfo As New ProcessStartInfo()
                Dim cmdArgs As String = ""

                ' Check if this is the Arduino ESP32 esptool.exe
                If esptoolPath.EndsWith(".exe") Then
                    ' Standalone executable
                    processInfo.FileName = esptoolPath
                    cmdArgs = $"--chip {chipType} --port {port} --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect {fileList}"
                    worker.ReportProgress(25, "Using Arduino ESP32 esptool.exe")
                ElseIf esptoolPath.Contains(" -m ") Then
                    ' Python module
                    Dim parts = esptoolPath.Split(New String() {" -m "}, StringSplitOptions.None)
                    processInfo.FileName = parts(0) ' python or python3
                    cmdArgs = $"-m esptool --chip {chipType} --port {port} --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect {fileList}"
                    worker.ReportProgress(25, "Using Python esptool.py module")
                ElseIf esptoolPath.EndsWith(".py") Then
                    ' Python script - need to find Python
                    Try
                        processInfo.FileName = "python"
                        cmdArgs = $"""" & esptoolPath & """ --chip {chipType} --port {port} --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect {fileList}"
                        worker.ReportProgress(25, "Using Python script esptool.py")
                    Catch ex As Exception
                        ' Try with python3
                        processInfo.FileName = "python3"
                        worker.ReportProgress(25, "Trying with python3 instead of python")
                    End Try
                Else
                    ' Unknown format - try direct execution
                    processInfo.FileName = esptoolPath
                    cmdArgs = $"--chip {chipType} --port {port} --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect {fileList}"
                    worker.ReportProgress(25, "Using direct execution of esptool")
                End If

                processInfo.Arguments = cmdArgs
                processInfo.UseShellExecute = False
                processInfo.CreateNoWindow = True
                processInfo.RedirectStandardOutput = True
                processInfo.RedirectStandardError = True

                Dim process As New Process()
                process.StartInfo = processInfo
                process.EnableRaisingEvents = True

                ' Flag to track if we detected a different chip type from error messages
                Dim detectedChipFromError As Boolean = False

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

                                                              ' Check for specific chip type errors and adjust if needed
                                                              If errorData.Data.Contains("This chip is ESP32-S3") Then
                                                                  chipType = "esp32s3"
                                                                  detectedChipFromError = True
                                                                  worker.ReportProgress(0, "Detected ESP32-S3 chip from error message, will retry with correct chip type")
                                                              ElseIf errorData.Data.Contains("This chip is ESP32-S2") Then
                                                                  chipType = "esp32s2"
                                                                  detectedChipFromError = True
                                                                  worker.ReportProgress(0, "Detected ESP32-S2 chip from error message, will retry with correct chip type")
                                                              ElseIf errorData.Data.Contains("This chip is ESP32-C3") Then
                                                                  chipType = "esp32c3"
                                                                  detectedChipFromError = True
                                                                  worker.ReportProgress(0, "Detected ESP32-C3 chip from error message, will retry with correct chip type")
                                                              End If
                                                          End If
                                                      End Sub

                worker.ReportProgress(30, "Starting upload process...")
                worker.ReportProgress(30, "Command: " & processInfo.FileName & " " & processInfo.Arguments)

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

                    ' If chip type was detected from errors, retry with correct chip type
                    If detectedChipFromError Then
                        worker.ReportProgress(0, $"Retrying with detected chip type: {chipType}")

                        ' Update the command with new chip type
                        If processInfo.Arguments.Contains("--chip esp32") Then
                            processInfo.Arguments = processInfo.Arguments.Replace("--chip esp32", $"--chip {chipType}")
                        Else
                            ' If for some reason the original command doesn't have the chip parameter as expected
                            worker.ReportProgress(0, "Warning: Could not update chip type in command string")
                        End If

                        ' Create a new process with the updated command
                        Dim retryProcess As New Process()
                        retryProcess.StartInfo = processInfo
                        retryProcess.EnableRaisingEvents = True

                        ' Re-attach the handlers
                        AddHandler retryProcess.OutputDataReceived, Sub(s, outputData)
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

                        AddHandler retryProcess.ErrorDataReceived, Sub(s, errorData)
                                                                       If Not String.IsNullOrEmpty(errorData.Data) Then
                                                                           worker.ReportProgress(0, "ERROR: " & errorData.Data)
                                                                       End If
                                                                   End Sub

                        worker.ReportProgress(0, "Starting retry with correct chip type...")
                        worker.ReportProgress(0, "Command: " & processInfo.FileName & " " & processInfo.Arguments)

                        retryProcess.Start()
                        retryProcess.BeginOutputReadLine()
                        retryProcess.BeginErrorReadLine()

                        ' Wait for process to exit or cancellation
                        While Not retryProcess.HasExited
                            If worker.CancellationPending Then
                                retryProcess.Kill()
                                e.Cancel = True
                                Return
                            End If
                            Thread.Sleep(100)
                        End While

                        ' Check if retry succeeded
                        If retryProcess.ExitCode = 0 Then
                            worker.ReportProgress(100, "Upload completed successfully after retry!")
                            uploadSuccess = True
                        Else
                            worker.ReportProgress(0, "Retry upload failed with exit code: " & retryProcess.ExitCode)

                            ' Try alternative command format as fallback
                            TryAlternativeCommandFormat(worker, processInfo.FileName, chipType, port, fileList, uploadSuccess)
                        End If
                    Else
                        ' Try alternative command format as fallback
                        TryAlternativeCommandFormat(worker, processInfo.FileName, chipType, port, fileList, uploadSuccess)
                    End If
                Else
                    worker.ReportProgress(100, "Upload completed successfully!")
                End If

            Catch ex As Exception
                worker.ReportProgress(0, "Error during upload: " & ex.Message)
                uploadSuccess = False
            End Try

            e.Result = uploadSuccess
        End Sub

        Private Sub TryAlternativeCommandFormat(worker As System.ComponentModel.BackgroundWorker,
                                       fileName As String,
                                       chipType As String,
                                       port As String,
                                       fileList As String,
                                       ByRef uploadSuccess As Boolean)
            worker.ReportProgress(0, "Trying alternative command format...")

            Dim altProcessInfo As New ProcessStartInfo()
            altProcessInfo.FileName = fileName

            ' IMPORTANT: Alternative format needs to specify chip type BEFORE write_flash
            ' This format works with both esptool.py v3+ and v4+
            If fileName.EndsWith(".exe") Then
                altProcessInfo.Arguments = $"--port {port} --chip {chipType} --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect {fileList}"
            ElseIf fileName.Contains("python") Then
                altProcessInfo.Arguments = $"-m esptool --port {port} --chip {chipType} --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect {fileList}"
            Else
                altProcessInfo.Arguments = $"--port {port} --chip {chipType} --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect {fileList}"
            End If

            worker.ReportProgress(0, "Alternative command: " & altProcessInfo.FileName & " " & altProcessInfo.Arguments)

            altProcessInfo.UseShellExecute = False
            altProcessInfo.CreateNoWindow = True
            altProcessInfo.RedirectStandardOutput = True
            altProcessInfo.RedirectStandardError = True

            Dim altProcess As New Process()
            altProcess.StartInfo = altProcessInfo
            altProcess.EnableRaisingEvents = True

            ' Setup output handlers
            AddHandler altProcess.OutputDataReceived, Sub(s, outputData)
                                                          If Not String.IsNullOrEmpty(outputData.Data) Then
                                                              worker.ReportProgress(0, outputData.Data)

                                                              ' Update progress based on output
                                                              If outputData.Data.Contains("Writing at") Then
                                                                  worker.ReportProgress(60, Nothing)
                                                              ElseIf outputData.Data.Contains("Written ") Then
                                                                  worker.ReportProgress(70, Nothing)
                                                              ElseIf outputData.Data.Contains("Verifying ") Then
                                                                  worker.ReportProgress(80, Nothing)
                                                              ElseIf outputData.Data.Contains("Hash of data verified") Then
                                                                  worker.ReportProgress(90, Nothing)
                                                              ElseIf outputData.Data.Contains("Hard resetting") Then
                                                                  worker.ReportProgress(95, Nothing)
                                                              End If
                                                          End If
                                                      End Sub

            AddHandler altProcess.ErrorDataReceived, Sub(s, errorData)
                                                         If Not String.IsNullOrEmpty(errorData.Data) Then
                                                             worker.ReportProgress(0, "ERROR: " & errorData.Data)

                                                             ' Check for port busy errors
                                                             If errorData.Data.Contains("busy") OrElse
                                                       errorData.Data.Contains("doesn't exist") OrElse
                                                       errorData.Data.Contains("cannot find") Then
                                                                 worker.ReportProgress(0, "Port access error detected. Trying with a brief delay...")
                                                                 Thread.Sleep(1000) ' Add a brief delay before final attempt
                                                             End If
                                                         End If
                                                     End Sub

            worker.ReportProgress(0, "Starting alternative upload attempt...")

            altProcess.Start()
            altProcess.BeginOutputReadLine()
            altProcess.BeginErrorReadLine()

            ' Wait for process to exit or cancellation
            While Not altProcess.HasExited
                If worker.CancellationPending Then
                    altProcess.Kill()
                    Return
                End If
                Thread.Sleep(100)
            End While

            ' Process completed, check exit code
            If altProcess.ExitCode = 0 Then
                worker.ReportProgress(100, "Alternative upload completed successfully!")
                uploadSuccess = True
            Else
                worker.ReportProgress(0, "Alternative upload failed with exit code: " & altProcess.ExitCode)

                ' If we had a port access error, try one final attempt with a fallback format
                worker.ReportProgress(0, "Trying final fallback command format...")

                ' Final fallback - simpler format that might work with older esptool versions
                Dim finalProcessInfo As New ProcessStartInfo()
                finalProcessInfo.FileName = fileName

                ' Use basic command format with minimal parameters
                If fileName.EndsWith(".exe") Then
                    finalProcessInfo.Arguments = $"--chip {chipType} --port {port} write_flash {fileList}"
                ElseIf fileName.Contains("python") Then
                    finalProcessInfo.Arguments = $"-m esptool --chip {chipType} --port {port} write_flash {fileList}"
                Else
                    finalProcessInfo.Arguments = $"--chip {chipType} --port {port} write_flash {fileList}"
                End If

                worker.ReportProgress(0, "Final command: " & finalProcessInfo.FileName & " " & finalProcessInfo.Arguments)

                finalProcessInfo.UseShellExecute = False
                finalProcessInfo.CreateNoWindow = True
                finalProcessInfo.RedirectStandardOutput = True
                finalProcessInfo.RedirectStandardError = True

                Dim finalProcess As New Process()
                finalProcess.StartInfo = finalProcessInfo
                finalProcess.EnableRaisingEvents = True

                ' Setup output handlers
                AddHandler finalProcess.OutputDataReceived, Sub(s, outputData)
                                                                If Not String.IsNullOrEmpty(outputData.Data) Then
                                                                    worker.ReportProgress(0, outputData.Data)

                                                                    ' Update progress based on output
                                                                    If outputData.Data.Contains("Writing at") Then
                                                                        worker.ReportProgress(60, Nothing)
                                                                    ElseIf outputData.Data.Contains("Written ") Then
                                                                        worker.ReportProgress(70, Nothing)
                                                                    ElseIf outputData.Data.Contains("Verifying ") Then
                                                                        worker.ReportProgress(80, Nothing)
                                                                    ElseIf outputData.Data.Contains("Hash of data verified") Then
                                                                        worker.ReportProgress(90, Nothing)
                                                                    ElseIf outputData.Data.Contains("Hard resetting") Then
                                                                        worker.ReportProgress(95, Nothing)
                                                                    End If
                                                                End If
                                                            End Sub

                AddHandler finalProcess.ErrorDataReceived, Sub(s, errorData)
                                                               If Not String.IsNullOrEmpty(errorData.Data) Then
                                                                   worker.ReportProgress(0, "ERROR: " & errorData.Data)
                                                               End If
                                                           End Sub

                worker.ReportProgress(0, "Starting final upload attempt...")

                finalProcess.Start()
                finalProcess.BeginOutputReadLine()
                finalProcess.BeginErrorReadLine()

                ' Wait for process to exit or cancellation
                While Not finalProcess.HasExited
                    If worker.CancellationPending Then
                        finalProcess.Kill()
                        Return
                    End If
                    Thread.Sleep(100)
                End While

                ' Process completed, check exit code
                If finalProcess.ExitCode = 0 Then
                    worker.ReportProgress(100, "Final upload attempt completed successfully!")
                    uploadSuccess = True
                Else
                    worker.ReportProgress(0, "All upload attempts failed with exit code: " & finalProcess.ExitCode)
                    worker.ReportProgress(0, "Possible causes:")
                    worker.ReportProgress(0, "1. COM port is busy or device is not connected")
                    worker.ReportProgress(0, "2. Device is in bootloader mode or needs to be reset")
                    worker.ReportProgress(0, "3. Incorrect chip type or flash settings")
                    worker.ReportProgress(0, "Please check your connections and try again.")
                End If
            End If
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