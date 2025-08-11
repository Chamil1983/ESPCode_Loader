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
        Private WithEvents txtBinaryPath As TextBox

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
        Private tempDirectory As String = ""

        ' Constructor
        Public Sub New()
            MyBase.New()
            InitializeComponent()
            RefreshPortList()
            arduinoCliPath = My.Settings.ArduinoCliPath
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

            ' Hidden textbox for binary path (as per requirements)
            txtBinaryPath = New TextBox()
            txtBinaryPath.Visible = False

            btnBrowseZip = New Button()
            btnBrowseZip.Text = "Browse..."
            btnBrowseZip.Dock = DockStyle.Fill

            zipFilePanel.Controls.Add(lblZipFile, 0, 0)
            zipFilePanel.Controls.Add(txtZipPath, 1, 0)
            zipFilePanel.Controls.Add(btnBrowseZip, 2, 0)
            zipFilePanel.Controls.Add(txtBinaryPath, 0, 0)
            txtBinaryPath.Visible = False

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
                    txtBinaryPath.Text = openFileDialog.FileName  ' Set the hidden binary path field too
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
                            AppendToOutput($"- {entry.FullName} ({entry.Length} bytes)")
                        End If
                    Next

                    If Not hasBinFiles Then
                        AppendToOutput("Warning: No .bin files found in the zip archive.")
                        MessageBox.Show("No .bin files found in the selected zip archive. This may not be a valid ESP32 firmware package.",
                                   "Invalid Zip Content", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End Using
            Catch ex As Exception
                AppendToOutput($"Error validating zip file: {ex.Message}")
                MessageBox.Show($"Error validating zip file: {ex.Message}", "Zip Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                AppendToOutput($"Found {cmbSerialPort.Items.Count} serial ports")
            Else
                AppendToOutput("No serial ports found")
            End If
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

            worker.ReportProgress(10, $"Extracting ZIP file to: {tempDirectory}")

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

                worker.ReportProgress(30, $"Found {binFiles.Count} binary files")

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

                ' Default addresses
                Dim bootloaderAddr As String = "0x1000"
                Dim partitionAddr As String = "0x8000"
                Dim bootAppAddr As String = "0xe000"
                Dim applicationAddr As String = "0x10000"

                ' Build esptool.py command for flashing
                Dim command As String = "write_flash --chip esp32 --port " & port & " --baud 460800"

                ' Add flash options
                command += " --flash_mode dio --flash_freq 40m --flash_size detect"

                ' Add flash addresses and files
                If Not String.IsNullOrEmpty(bootloaderFile) Then
                    command += " " & bootloaderAddr & " """ & bootloaderFile & """"
                    worker.ReportProgress(35, $"Using bootloader: {Path.GetFileName(bootloaderFile)}")
                End If

                If Not String.IsNullOrEmpty(partitionFile) Then
                    command += " " & partitionAddr & " """ & partitionFile & """"
                    worker.ReportProgress(40, $"Using partition table: {Path.GetFileName(partitionFile)}")
                End If

                If Not String.IsNullOrEmpty(bootAppFile) Then
                    command += " " & bootAppAddr & " """ & bootAppFile & """"
                    worker.ReportProgress(45, $"Using boot_app0: {Path.GetFileName(bootAppFile)}")
                End If

                If Not String.IsNullOrEmpty(applicationFile) Then
                    command += " " & applicationAddr & " """ & applicationFile & """"
                    worker.ReportProgress(50, $"Using application: {Path.GetFileName(applicationFile)}")
                End If

                worker.ReportProgress(55, "Command: " & command)

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

                    worker.ReportProgress(60, "Starting upload process...")

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