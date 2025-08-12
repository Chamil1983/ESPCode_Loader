Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Diagnostics
Imports System.Threading
Imports System.IO.Compression


Namespace KC_LINK_LoaderV1
    Public Class ZipUploadForm
        Inherits Form

        ' UI Controls
        Private WithEvents lblZipFile As Label
        Private WithEvents txtZipPath As TextBox
        Private WithEvents btnBrowseZip As Button

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
        Private esptoolVersion As String = ""
        Private tempDirectory As String = ""

        ' Constructor
        Public Sub New()
            MyBase.New()
            InitializeComponent()
            RefreshPortList()
            FindEsptoolPath()
        End Sub

        Private Sub InitializeComponent()
            ' Form setup
            Me.Text = "ESP32 Zip Upload"
            Me.Size = New Size(600, 500)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            ' Main layout
            Dim mainLayout As New TableLayoutPanel()
            mainLayout.Dock = DockStyle.Fill
            mainLayout.RowCount = 7
            mainLayout.ColumnCount = 1
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))   ' Zip file selection
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 10))   ' Spacer
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))   ' Serial port selection
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 10))   ' Spacer
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))   ' Buttons
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))   ' Progress bar
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' Output log
            mainLayout.Padding = New Padding(10)

            ' Zip file panel
            Dim zipFilePanel As New TableLayoutPanel()
            zipFilePanel.Dock = DockStyle.Fill
            zipFilePanel.ColumnCount = 3
            zipFilePanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
            zipFilePanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            zipFilePanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))

            lblZipFile = New Label()
            lblZipFile.Text = "Zip File:"
            lblZipFile.TextAlign = ContentAlignment.MiddleRight
            lblZipFile.Dock = DockStyle.Fill

            txtZipPath = New TextBox()
            txtZipPath.Dock = DockStyle.Fill

            btnBrowseZip = New Button()
            btnBrowseZip.Text = "Browse..."
            btnBrowseZip.Dock = DockStyle.Fill

            zipFilePanel.Controls.Add(lblZipFile, 0, 0)
            zipFilePanel.Controls.Add(txtZipPath, 1, 0)
            zipFilePanel.Controls.Add(btnBrowseZip, 2, 0)

            ' Serial port panel
            Dim serialPortPanel As New TableLayoutPanel()
            serialPortPanel.Dock = DockStyle.Fill
            serialPortPanel.ColumnCount = 3
            serialPortPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
            serialPortPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            serialPortPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))

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
            mainLayout.Controls.Add(zipFilePanel, 0, 0)
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
            AddHandler btnBrowseZip.Click, AddressOf BrowseZip_Click
            AddHandler btnRefreshPorts.Click, AddressOf RefreshPorts_Click
            AddHandler btnUpload.Click, AddressOf Upload_Click
            AddHandler btnCancel.Click, AddressOf Cancel_Click
            AddHandler bgWorker.DoWork, AddressOf BgWorker_DoWork
            AddHandler bgWorker.ProgressChanged, AddressOf BgWorker_ProgressChanged
            AddHandler bgWorker.RunWorkerCompleted, AddressOf BgWorker_RunWorkerCompleted
            AddHandler Me.FormClosing, AddressOf ZipUploadForm_FormClosing

            ' Set accept and cancel buttons
            Me.AcceptButton = btnUpload
            Me.CancelButton = btnCancel
        End Sub

        Private Sub BrowseZip_Click(sender As Object, e As EventArgs)
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Zip Files (*.zip)|*.zip|All Files (*.*)|*.*"
                openFileDialog.Title = "Select ESP32 Firmware ZIP File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtZipPath.Text = openFileDialog.FileName
                    ValidateZipFile(openFileDialog.FileName)
                End If
            End Using
        End Sub

        Private Sub ValidateZipFile(zipPath As String)
            Try
                ' Check if it's a valid zip file
                Using zipArchive As ZipArchive = ZipFile.OpenRead(zipPath)
                    Dim hasBinFiles As Boolean = False

                    AppendToOutput("Zip file contents:")
                    For Each entry As ZipArchiveEntry In zipArchive.Entries
                        If entry.FullName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) Then
                            hasBinFiles = True
                            AppendToOutput("- " & entry.FullName & " (" & entry.Length & " bytes)")
                        End If
                    Next

                    If Not hasBinFiles Then
                        AppendToOutput("Warning: No .bin files found in the zip archive.")
                        MessageBox.Show("No .bin files found in the selected zip archive. This may not be a valid ESP32 firmware package.",
                                   "Invalid Zip Content", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End Using
            Catch ex As Exception
                AppendToOutput("Error validating zip file: " & ex.Message)
                MessageBox.Show("Error validating zip file: " & ex.Message, "Zip Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
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

        Private Sub FindEsptoolPath()
            ' Try to find esptool.py or esptool.exe in common locations
            esptoolPath = ""
            esptoolVersion = ""

            ' First check in Arduino ESP32 package (most reliable for ESP32 uploads)
            Dim esp32PackageDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "arduino15", "packages", "esp32")

            If Directory.Exists(esp32PackageDir) Then
                Dim esptoolFiles = Directory.GetFiles(esp32PackageDir, "esptool*", SearchOption.AllDirectories)
                If esptoolFiles.Length > 0 Then
                    For Each toolPath In esptoolFiles
                        If toolPath.EndsWith(".exe") OrElse toolPath.EndsWith(".py") Then
                            ' Found esptool - check version
                            esptoolPath = toolPath
                            AppendToOutput("Found ESP32 esptool at: " & esptoolPath)

                            ' Get version info
                            Try
                                Dim versionProcess As New Process()
                                versionProcess.StartInfo.FileName = esptoolPath
                                versionProcess.StartInfo.Arguments = "version"
                                versionProcess.StartInfo.UseShellExecute = False
                                versionProcess.StartInfo.CreateNoWindow = True
                                versionProcess.StartInfo.RedirectStandardOutput = True
                                versionProcess.StartInfo.RedirectStandardError = True

                                versionProcess.Start()
                                Dim versionOutput = versionProcess.StandardOutput.ReadToEnd()
                                versionProcess.WaitForExit()

                                If versionProcess.ExitCode = 0 Then
                                    esptoolVersion = versionOutput.Trim()
                                    AppendToOutput("Esptool version: " & esptoolVersion)
                                    Return ' Found working esptool
                                End If
                            Catch ex As Exception
                                ' Continue searching
                                AppendToOutput("Error checking esptool version: " & ex.Message)
                            End Try
                        End If
                    Next
                End If
            End If

            ' Look in typical Arduino CLI installation paths
            Dim arduinoCliPath As String = My.Settings.ArduinoCliPath
            If Not String.IsNullOrEmpty(arduinoCliPath) Then
                Dim arduinoDir = Path.GetDirectoryName(arduinoCliPath)

                ' Look for the ESP32 Arduino core
                Dim possibleEspToolPaths = New String() {
                    Path.Combine(arduinoDir, "esptool.exe"),
                    Path.Combine(arduinoDir, "esptool.py"),
                    Path.Combine(Path.GetDirectoryName(arduinoDir), "hardware", "espressif", "esp32", "tools", "esptool.py"),
                    Path.Combine(Path.GetDirectoryName(arduinoDir), "hardware", "espressif", "esp32", "tools", "esptool", "esptool.py")
                }

                For Each toolPath In possibleEspToolPaths
                    If File.Exists(toolPath) Then
                        esptoolPath = toolPath
                        AppendToOutput("Found esptool at: " & esptoolPath)
                        Return
                    End If
                Next
            End If

            ' If still not found, try to use the Python esptool module
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
                            esptoolVersion = output.Trim()
                            AppendToOutput("Using esptool.py module: " & esptoolPath)
                            AppendToOutput("Esptool version: " & esptoolVersion)
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

            ' Last attempt - try to find in Arduino package directories directly
            Try
                Dim packagesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "arduino15", "packages")

                If Directory.Exists(packagesDir) Then
                    ' Search for esptool in all packages
                    Dim esptoolFiles = Directory.GetFiles(packagesDir, "esptool*.exe", SearchOption.AllDirectories)
                    If esptoolFiles.Length > 0 Then
                        esptoolPath = esptoolFiles(0)
                        AppendToOutput("Found esptool in packages: " & esptoolPath)
                        Return
                    End If

                    ' Try Python scripts
                    Dim esptoolPyFiles = Directory.GetFiles(packagesDir, "esptool*.py", SearchOption.AllDirectories)
                    If esptoolPyFiles.Length > 0 Then
                        esptoolPath = esptoolPyFiles(0)
                        AppendToOutput("Found esptool.py in packages: " & esptoolPath)
                        Return
                    End If
                End If
            Catch ex As Exception
                AppendToOutput("Error searching package directories: " & ex.Message)
            End Try

            ' If we get here, no esptool found
            esptoolPath = ""
            AppendToOutput("WARNING: Could not find esptool. Upload may fail.")
            AppendToOutput("Please install esptool via 'pip install esptool' or specify path manually.")
        End Sub

        Private Sub Upload_Click(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(txtZipPath.Text) OrElse Not File.Exists(txtZipPath.Text) Then
                MessageBox.Show("Please select a valid ZIP file.", "No ZIP File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

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
                AppendToOutput("Starting ZIP upload process...")

                btnUpload.Text = "Cancel"
                btnUpload.BackColor = Color.IndianRed
                isUploading = True

                ' Start upload in background
                bgWorker.RunWorkerAsync(New String() {txtZipPath.Text, cmbSerialPort.SelectedItem.ToString()})
            End If
        End Sub

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
            Dim args As String() = CType(e.Argument, String())
            Dim zipPath As String = args(0)
            Dim port As String = args(1)
            Dim uploadSuccess As Boolean = True

            ' Create temporary directory for extraction
            tempDirectory = Path.Combine(Path.GetTempPath(), "ESP32Upload_" & Guid.NewGuid().ToString())
            Directory.CreateDirectory(tempDirectory)

            worker.ReportProgress(10, "Extracting ZIP file to: " & tempDirectory)

            Try
                ' Extract the zip file
                ZipFile.ExtractToDirectory(zipPath, tempDirectory)
                worker.ReportProgress(20, "ZIP file extracted successfully")

                ' Find all .bin files in the extracted directory
                Dim binFiles As List(Of String) = Directory.GetFiles(tempDirectory, "*.bin", SearchOption.AllDirectories).ToList()

                If binFiles.Count = 0 Then
                    worker.ReportProgress(0, "No .bin files found in the ZIP archive")
                    uploadSuccess = False
                    e.Result = uploadSuccess
                    Return
                End If

                worker.ReportProgress(30, "Found " & binFiles.Count & " binary files")

                ' Look for specific files by name pattern
                Dim bootloaderFile As String = binFiles.FirstOrDefault(Function(f) Path.GetFileName(f).ToLower().Contains("bootloader"))
                Dim partitionFile As String = binFiles.FirstOrDefault(Function(f) Path.GetFileName(f).ToLower().Contains("partition"))
                Dim bootAppFile As String = binFiles.FirstOrDefault(Function(f) Path.GetFileName(f).ToLower().Contains("boot_app0"))
                Dim applicationFile As String = binFiles.FirstOrDefault(Function(f) Not (
                Path.GetFileName(f).ToLower().Contains("bootloader") OrElse
                Path.GetFileName(f).ToLower().Contains("partition") OrElse
                Path.GetFileName(f).ToLower().Contains("boot_app0")))

                ' If no application file is found, use the first bin file
                If String.IsNullOrEmpty(applicationFile) AndAlso binFiles.Count > 0 Then
                    applicationFile = binFiles(0)
                End If

                ' Default addresses from BinaryExporter class
                Dim bootloaderAddr As String = KC_LINK_LoaderV1.BinaryExporter.DefaultBootloaderAddress    ' 0x1000
                Dim partitionAddr As String = KC_LINK_LoaderV1.BinaryExporter.DefaultPartitionAddress      ' 0x8000
                Dim bootAppAddr As String = KC_LINK_LoaderV1.BinaryExporter.DefaultBootApp0Address         ' 0xe000
                Dim applicationAddr As String = KC_LINK_LoaderV1.BinaryExporter.DefaultApplicationAddress  ' 0x10000

                worker.ReportProgress(35, "Using standard ESP32 flash addresses:")
                worker.ReportProgress(35, "Bootloader: " & bootloaderAddr)
                worker.ReportProgress(35, "Partition Table: " & partitionAddr)
                worker.ReportProgress(35, "Boot App 0: " & bootAppAddr)
                worker.ReportProgress(35, "Application: " & applicationAddr)

                ' Check for address info in manifest file
                Dim manifestFile = Directory.GetFiles(tempDirectory, "flash_addresses.txt", SearchOption.AllDirectories).FirstOrDefault()
                If Not String.IsNullOrEmpty(manifestFile) Then
                    worker.ReportProgress(40, "Found address manifest file: " & Path.GetFileName(manifestFile))
                    Try
                        Dim addressMap As New Dictionary(Of String, String)
                        Dim lines = File.ReadAllLines(manifestFile)
                        For Each line In lines
                            ' Skip comments and empty lines
                            If line.Trim().StartsWith("#") OrElse String.IsNullOrWhiteSpace(line) Then Continue For

                            ' Parse address info (format: filename: address)
                            Dim parts = line.Split(New Char() {":"c}, 2)
                            If parts.Length = 2 Then
                                Dim fileName = parts(0).Trim()
                                Dim address = parts(1).Trim()

                                addressMap(fileName) = address

                                If fileName.ToLower().Contains("bootloader") Then
                                    bootloaderAddr = address
                                    worker.ReportProgress(40, "Using custom bootloader address: " & bootloaderAddr)
                                ElseIf fileName.ToLower().Contains("partition") Then
                                    partitionAddr = address
                                    worker.ReportProgress(40, "Using custom partition address: " & partitionAddr)
                                ElseIf fileName.ToLower().Contains("boot_app0") Then
                                    bootAppAddr = address
                                    worker.ReportProgress(40, "Using custom boot_app0 address: " & bootAppAddr)
                                ElseIf Not (fileName.ToLower().Contains("bootloader") OrElse
                                           fileName.ToLower().Contains("partition") OrElse
                                           fileName.ToLower().Contains("boot_app0") OrElse
                                           fileName.ToLower().Contains("merged")) Then
                                    applicationAddr = address
                                    worker.ReportProgress(40, "Using custom application address: " & applicationAddr)
                                End If
                            End If
                        Next

                        ' Now that we have all addresses in a map, match them to the actual files we found
                        If Not String.IsNullOrEmpty(bootloaderFile) Then
                            Dim bootloaderName = Path.GetFileName(bootloaderFile)
                            If addressMap.ContainsKey(bootloaderName) Then
                                bootloaderAddr = addressMap(bootloaderName)
                                worker.ReportProgress(42, "Using exact address for " & bootloaderName & ": " & bootloaderAddr)
                            End If
                        End If

                        If Not String.IsNullOrEmpty(partitionFile) Then
                            Dim partitionName = Path.GetFileName(partitionFile)
                            If addressMap.ContainsKey(partitionName) Then
                                partitionAddr = addressMap(partitionName)
                                worker.ReportProgress(42, "Using exact address for " & partitionName & ": " & partitionAddr)
                            End If
                        End If

                        If Not String.IsNullOrEmpty(bootAppFile) Then
                            Dim bootAppName = Path.GetFileName(bootAppFile)
                            If addressMap.ContainsKey(bootAppName) Then
                                bootAppAddr = addressMap(bootAppName)
                                worker.ReportProgress(42, "Using exact address for " & bootAppName & ": " & bootAppAddr)
                            End If
                        End If

                        If Not String.IsNullOrEmpty(applicationFile) Then
                            Dim appName = Path.GetFileName(applicationFile)
                            If addressMap.ContainsKey(appName) Then
                                applicationAddr = addressMap(appName)
                                worker.ReportProgress(42, "Using exact address for " & appName & ": " & applicationAddr)
                            End If
                        End If
                    Catch ex As Exception
                        worker.ReportProgress(40, "Error parsing address manifest: " & ex.Message)
                    End Try
                End If

                ' Add file list for upload
                Dim fileList As String = ""

                If Not String.IsNullOrEmpty(bootloaderFile) Then
                    fileList += bootloaderAddr & " """ & bootloaderFile & """ "
                    worker.ReportProgress(45, "Using bootloader: " & Path.GetFileName(bootloaderFile))
                End If

                If Not String.IsNullOrEmpty(partitionFile) Then
                    fileList += partitionAddr & " """ & partitionFile & """ "
                    worker.ReportProgress(46, "Using partition table: " & Path.GetFileName(partitionFile))
                End If

                If Not String.IsNullOrEmpty(bootAppFile) Then
                    fileList += bootAppAddr & " """ & bootAppFile & """ "
                    worker.ReportProgress(47, "Using boot_app0: " & Path.GetFileName(bootAppFile))
                End If

                If Not String.IsNullOrEmpty(applicationFile) Then
                    fileList += applicationAddr & " """ & applicationFile & """"
                    worker.ReportProgress(48, "Using application: " & Path.GetFileName(applicationFile))
                End If

                worker.ReportProgress(50, "Command: write_flash --chip esp32 --port " & port & " --baud 460800 --flash_mode dio --flash_freq 40m --flash_size detect " & fileList)

                ' Execute the correct esptool command format based on the path
                Try
                    Dim processInfo As New ProcessStartInfo()
                    Dim cmdArgs As String = ""

                    ' Check if this is the Arduino ESP32 esptool.exe
                    If esptoolPath.EndsWith(".exe") Then
                        ' Standalone executable - don't use --chip parameter with write_flash
                        processInfo.FileName = esptoolPath
                        cmdArgs = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                        worker.ReportProgress(52, "Using Arduino ESP32 esptool.exe")
                    ElseIf esptoolPath.Contains(" -m ") Then
                        ' Python module
                        Dim parts = esptoolPath.Split(New String() {" -m "}, StringSplitOptions.None)
                        processInfo.FileName = parts(0) ' python or python3
                        cmdArgs = "-m esptool --chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                        worker.ReportProgress(52, "Using Python esptool.py module")
                    ElseIf esptoolPath.EndsWith(".py") Then
                        ' Python script - need to find Python
                        Try
                            processInfo.FileName = "python"
                            cmdArgs = """" & esptoolPath & """ --chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                            worker.ReportProgress(52, "Using Python script esptool.py")
                        Catch ex As Exception
                            ' Try with python3
                            processInfo.FileName = "python3"
                            worker.ReportProgress(52, "Trying with python3 instead of python")
                        End Try
                    Else
                        ' Unknown format - try direct execution
                        processInfo.FileName = esptoolPath
                        cmdArgs = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                        worker.ReportProgress(52, "Using direct execution of esptool")
                    End If

                    processInfo.Arguments = cmdArgs
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

                    AddHandler process.ErrorDataReceived, Sub(s, errorData)
                                                              If Not String.IsNullOrEmpty(errorData.Data) Then
                                                                  worker.ReportProgress(0, "ERROR: " & errorData.Data)
                                                                  uploadSuccess = False
                                                              End If
                                                          End Sub

                    worker.ReportProgress(55, "Starting upload process...")
                    worker.ReportProgress(56, "Command: " & processInfo.FileName & " " & processInfo.Arguments)

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

                        ' Try one more time with a different command format as fallback
                        If uploadSuccess = False Then
                            worker.ReportProgress(0, "Trying alternative command format...")

                            Dim altProcessInfo As New ProcessStartInfo()
                            altProcessInfo.FileName = processInfo.FileName

                            ' Swap the parameter order for write_flash
                            If processInfo.Arguments.Contains("write_flash --flash_mode") Then
                                ' Move chip/port parameters after write_flash
                                altProcessInfo.Arguments = "write_flash --chip esp32 --port " & port & " --baud 460800 --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                            Else
                                ' Move flash mode parameters before write_flash
                                altProcessInfo.Arguments = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                            End If

                            worker.ReportProgress(56, "Alternative command: " & altProcessInfo.FileName & " " & altProcessInfo.Arguments)

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
                                                                         End If
                                                                     End Sub

                            worker.ReportProgress(55, "Starting alternative upload attempt...")

                            altProcess.Start()
                            altProcess.BeginOutputReadLine()
                            altProcess.BeginErrorReadLine()

                            ' Wait for process to exit or cancellation
                            While Not altProcess.HasExited
                                If worker.CancellationPending Then
                                    altProcess.Kill()
                                    e.Cancel = True
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
                            End If
                        End If
                    Else
                        worker.ReportProgress(100, "Upload completed successfully!")
                    End If

                Catch ex As Exception
                    worker.ReportProgress(0, "Error during upload: " & ex.Message)
                    uploadSuccess = False
                End Try

            Catch ex As Exception
                worker.ReportProgress(0, "Error processing ZIP file: " & ex.Message)
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
            ElseIf e.Result IsNot Nothing AndAlso CBool(e.Result) Then
                AppendToOutput("ZIP upload completed successfully!")
                progressBar.Value = 100
            Else
                AppendToOutput("ZIP upload failed. Check output for details.")
                MessageBox.Show("ZIP upload failed. Check output for details.", "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            ' Cleanup temporary directory in the background
            If Not String.IsNullOrEmpty(tempDirectory) AndAlso Directory.Exists(tempDirectory) Then
                Task.Run(Sub()
                             Try
                                 Directory.Delete(tempDirectory, True)
                             Catch ex As Exception
                                 ' Ignore cleanup errors
                             End Try
                         End Sub)
            End If
        End Sub

        Private Sub ZipUploadForm_FormClosing(sender As Object, e As FormClosingEventArgs)
            ' Cancel any ongoing operation
            If isUploading AndAlso bgWorker.IsBusy Then
                bgWorker.CancelAsync()
                isUploading = False
            End If

            ' Cleanup temporary directory in the background
            If Not String.IsNullOrEmpty(tempDirectory) AndAlso Directory.Exists(tempDirectory) Then
                Task.Run(Sub()
                             Try
                                 Directory.Delete(tempDirectory, True)
                             Catch ex As Exception
                                 ' Ignore cleanup errors
                             End Try
                         End Sub)
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