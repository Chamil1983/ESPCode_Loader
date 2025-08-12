Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Diagnostics
Imports System.Threading
Imports System.IO.Ports


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
        Private esptoolVersion As String = ""
        Private serialPortsInUse As New List(Of String)
        Private serialPortOpener As SerialPort = Nothing

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
            AddHandler Me.FormClosing, AddressOf BinaryUploadForm_FormClosing

            ' Set accept and cancel buttons
            Me.AcceptButton = btnUpload
            Me.CancelButton = btnCancel
        End Sub

        Private Sub BinaryUploadForm_FormClosing(sender As Object, e As FormClosingEventArgs)
            ' Close any open serial ports
            CloseSerialPort()
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
            ' Remember current selection
            Dim currentSelection As String = ""
            If cmbSerialPort.SelectedItem IsNot Nothing Then
                currentSelection = cmbSerialPort.SelectedItem.ToString()
            End If

            cmbSerialPort.Items.Clear()
            serialPortsInUse.Clear()

            ' Get all available COM ports
            For Each port As String In My.Computer.Ports.SerialPortNames
                cmbSerialPort.Items.Add(port)

                ' Check if port is in use
                Try
                    Using testPort As New SerialPort(port)
                        testPort.Open()
                        testPort.Close()
                    End Using
                Catch ex As Exception
                    ' Port is likely in use
                    serialPortsInUse.Add(port)
                    AppendToOutput("Note: Port " & port & " appears to be in use by another application")
                End Try
            Next

            ' Try to restore previous selection
            If Not String.IsNullOrEmpty(currentSelection) AndAlso cmbSerialPort.Items.Contains(currentSelection) Then
                cmbSerialPort.SelectedItem = currentSelection
            ElseIf cmbSerialPort.Items.Count > 0 Then
                cmbSerialPort.SelectedIndex = 0
            End If

            AppendToOutput("Found " & cmbSerialPort.Items.Count & " serial ports")
            If serialPortsInUse.Count > 0 Then
                AppendToOutput("Note: " & serialPortsInUse.Count & " ports are currently in use")
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

        Private Sub CloseSerialPort()
            ' Close any open serial port
            If serialPortOpener IsNot Nothing Then
                Try
                    If serialPortOpener.IsOpen Then
                        serialPortOpener.Close()
                    End If
                    serialPortOpener.Dispose()
                    serialPortOpener = Nothing
                Catch ex As Exception
                    ' Ignore errors during close
                End Try
            End If
        End Sub

        Private Function IsPortAvailable(portName As String) As Boolean
            Try
                ' Close any previous serialPortOpener
                CloseSerialPort()

                ' Try to open the port to see if it's available
                serialPortOpener = New SerialPort(portName)
                serialPortOpener.Open()
                Return True
            Catch ex As Exception
                AppendToOutput("Port " & portName & " is busy: " & ex.Message)
                Return False
            End Try
        End Function

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

                ' Check if port is available
                Dim selectedPort = cmbSerialPort.SelectedItem.ToString()
                If Not IsPortAvailable(selectedPort) Then
                    Dim result = MessageBox.Show(
                        "The selected COM port (" & selectedPort & ") appears to be in use by another application." & Environment.NewLine &
                        "Would you like to try to use it anyway?" & Environment.NewLine &
                        "Note: Close any serial monitors or other programs using this port first.",
                        "Port In Use",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning)

                    If result = DialogResult.No Then
                        Return
                    End If
                Else
                    ' Close the port now that we've verified it works
                    CloseSerialPort()
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

            ' Add file list for upload
            Dim fileList As String = ""

            If Not String.IsNullOrEmpty(bootloaderPath) AndAlso File.Exists(bootloaderPath) Then
                fileList += bootloaderAddr & " """ & bootloaderPath & """ "
                worker.ReportProgress(15, "Using bootloader: " & Path.GetFileName(bootloaderPath))
            End If

            If Not String.IsNullOrEmpty(partitionPath) AndAlso File.Exists(partitionPath) Then
                fileList += partitionAddr & " """ & partitionPath & """ "
                worker.ReportProgress(15, "Using partition table: " & Path.GetFileName(partitionPath))
            End If

            If Not String.IsNullOrEmpty(bootApp0Path) AndAlso File.Exists(bootApp0Path) Then
                fileList += bootApp0Addr & " """ & bootApp0Path & """ "
                worker.ReportProgress(15, "Using boot_app0: " & Path.GetFileName(bootApp0Path))
            End If

            If Not String.IsNullOrEmpty(applicationPath) AndAlso File.Exists(applicationPath) Then
                fileList += applicationAddr & " """ & applicationPath & """"
                worker.ReportProgress(15, "Using application: " & Path.GetFileName(applicationPath))
            End If

            worker.ReportProgress(10, "Preparing to flash binaries...")
            worker.ReportProgress(15, "Command: write_flash --chip esp32 --port " & port & " --baud 460800 --flash_mode dio --flash_freq 40m --flash_size detect " & fileList)

            ' Make sure any serial monitors or other applications release the port
            worker.ReportProgress(15, "Ensuring port " & port & " is available...")

            ' Close any applications that might be using the port
            ClosePortProcesses(port)

            ' Allow a moment for the port to be released
            Thread.Sleep(500)

            ' Execute the esptool command with the correct format
            Try
                Dim processInfo As New ProcessStartInfo()
                Dim cmdArgs As String = ""

                ' Check if this is the Arduino ESP32 esptool.exe
                If esptoolPath.EndsWith(".exe") Then
                    ' Standalone executable - for ESP32 we need to put chip parameter before write_flash
                    processInfo.FileName = esptoolPath
                    cmdArgs = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                    worker.ReportProgress(20, "Using Arduino ESP32 esptool.exe")
                ElseIf esptoolPath.Contains(" -m ") Then
                    ' Python module
                    Dim parts = esptoolPath.Split(New String() {" -m "}, StringSplitOptions.None)
                    processInfo.FileName = parts(0) ' python or python3
                    cmdArgs = "-m esptool --chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                    worker.ReportProgress(20, "Using Python esptool.py module")
                ElseIf esptoolPath.EndsWith(".py") Then
                    ' Python script - need to find Python
                    Try
                        processInfo.FileName = "python"
                        cmdArgs = """" & esptoolPath & """ --chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                        worker.ReportProgress(20, "Using Python script esptool.py")
                    Catch ex As Exception
                        ' Try with python3
                        processInfo.FileName = "python3"
                        worker.ReportProgress(20, "Trying with python3 instead of python")
                    End Try
                Else
                    ' Unknown format - try direct execution
                    processInfo.FileName = esptoolPath
                    cmdArgs = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                    worker.ReportProgress(20, "Using direct execution of esptool")
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

                                                              ' Check for specific errors
                                                              If errorData.Data.Contains("Could not open") AndAlso
                                                                 errorData.Data.Contains("the port is busy or doesn't exist") Then
                                                                  worker.ReportProgress(0, "Port is busy. Try closing any serial monitors or other applications using the port.")
                                                                  worker.ReportProgress(0, "Note: Sometimes you need to restart the IDE to fully release the port.")
                                                              End If

                                                              uploadSuccess = False
                                                          End If
                                                      End Sub

                worker.ReportProgress(20, "Starting upload process...")
                worker.ReportProgress(20, "Command: " & processInfo.FileName & " " & processInfo.Arguments)

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

                    ' Try to find what's using the port
                    If process.ExitCode = 2 Then
                        worker.ReportProgress(0, "Checking what process is using the port...")
                        Dim portUserInfo = GetPortUserProcess(port)
                        If Not String.IsNullOrEmpty(portUserInfo) Then
                            worker.ReportProgress(0, "Port " & port & " is used by: " & portUserInfo)
                            worker.ReportProgress(0, "Please close this application before uploading.")
                        End If
                    End If

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

                        worker.ReportProgress(20, "Alternative command: " & altProcessInfo.FileName & " " & altProcessInfo.Arguments)

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

                        AddHandler altProcess.ErrorDataReceived, Sub(s, errorData)
                                                                     If Not String.IsNullOrEmpty(errorData.Data) Then
                                                                         worker.ReportProgress(0, "ERROR: " & errorData.Data)
                                                                     End If
                                                                 End Sub

                        worker.ReportProgress(20, "Starting alternative upload attempt...")

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

            e.Result = uploadSuccess
        End Sub

        Private Function GetPortUserProcess(portName As String) As String
            Try
                ' Use Windows Management Instrumentation (WMI) to find processes using the port
                Dim startInfo As New ProcessStartInfo()
                startInfo.FileName = "powershell"
                startInfo.Arguments = "-Command ""Get-Process | Where-Object {$_.Modules.FileName -like '*serial*' -or $_.Modules.FileName -like '*com*'} | Select-Object ProcessName, Id | Format-Table -AutoSize"""
                startInfo.UseShellExecute = False
                startInfo.RedirectStandardOutput = True
                startInfo.CreateNoWindow = True

                Dim process As New Process()
                process.StartInfo = startInfo
                process.Start()

                Dim output As String = process.StandardOutput.ReadToEnd()
                process.WaitForExit()

                If Not String.IsNullOrEmpty(output) Then
                    Return output
                End If

                Return "Could not determine which process is using the port"
            Catch ex As Exception
                Return "Error checking port usage: " & ex.Message
            End Try
        End Function

        Private Sub ClosePortProcesses(portName As String)
            Try
                ' Close any known port-using processes
                Dim knownProcesses As String() = {"Serial Monitor", "putty", "terminal", "serialmon", "coolterm", "terraterm"}

                For Each procName In knownProcesses
                    Try
                        Dim procs = Process.GetProcessesByName(procName)
                        For Each proc In procs
                            Try
                                proc.CloseMainWindow()
                                ' Give it a moment to close
                                Thread.Sleep(100)
                                If Not proc.HasExited Then
                                    proc.Kill()
                                End If
                            Catch ex As Exception
                                ' Ignore errors trying to close processes
                            End Try
                        Next
                    Catch ex As Exception
                        ' Ignore errors if a process type isn't found
                    End Try
                Next
            Catch ex As Exception
                ' Ignore any errors in the cleanup process
            End Try
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

                ' Show more helpful error message
                Dim extraHelp = "Common issues:" & Environment.NewLine &
                                "1. Serial port is in use by another application" & Environment.NewLine &
                                "2. You need to reset your board manually before upload" & Environment.NewLine &
                                "3. A serial monitor is still open" & Environment.NewLine & Environment.NewLine &
                                "Try closing all other applications that might be using the serial port."

                MessageBox.Show("Binary upload failed. " & extraHelp, "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            ' Close any opened serial port
            CloseSerialPort()
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