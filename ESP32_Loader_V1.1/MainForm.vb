Imports System.IO
Imports System.IO.Ports
Imports System.Diagnostics
Imports System.ComponentModel
Imports System.Text
Imports System.Threading
Imports System.Text.RegularExpressions

Public Class MainForm
    ' Fields for custom partition support
    Private customPartitionFilePath As String = ""
    Private partitionBackupPath As String = ""
    Private arduinoCliPath As String = ""
    Private sketchPath As String = ""
    Private arduinoUserDir As String = ""
    Private boardList As New List(Of ArduinoBoard)
    Private boardConfig As ArduinoBoardConfig
    Private boardsTxtPath As String = ""  ' Will be set during initialization
    Private arduinoCliVersion As String = ""
    Private lastCompileStats As CompileStats = Nothing

    ' Memory reporting mode enum and field
    Public Enum MemoryReportingMode
        ArduinoCli ' Default arduino-cli style
        ArduinoIDE ' IDE style (adds more components to RAM usage)
        Both       ' Show both calculations
    End Enum

    Private memReportingMode As MemoryReportingMode = MemoryReportingMode.ArduinoCli

    ' UI Controls if not defined in designer
    Private WithEvents cmbMemoryMode As ComboBox

    ' Background workers for compile and upload
    Private WithEvents bgwCompile As New BackgroundWorker With {.WorkerReportsProgress = True, .WorkerSupportsCancellation = True}
    Private WithEvents bgwUpload As New BackgroundWorker With {.WorkerReportsProgress = True, .WorkerSupportsCancellation = True}

    ' Stages for progress tracking
    Private cliStagesCompile As String() = {
        "Detecting libraries",
        "Compiling sketch",
        "Compiling core",
        "Compiling libraries",
        "Linking everything together",
        "Building into",
        "Sketch uses"
    }
    Private cliStagesUpload As String() = {
        "Uploading",
        "Writing at",
        "Hash of data verified",
        "Hard resetting via RTS",
        "Leaving"
    }

    ' Update the CompileStats class to better handle Arduino IDE mode conversion
    Private Class CompileStats
        Public Property FlashUsed As Integer
        Public Property FlashTotal As Integer
        Public Property FlashPercent As Integer
        Public Property RamUsed As Integer
        Public Property RamAvailable As Integer
        Public Property RamTotal As Integer
        Public Property RamPercent As Integer

        ' Method to convert arduino-cli stats to IDE-style stats
        Public Function ConvertToIDEStyle() As CompileStats
            Dim result As New CompileStats
            result.FlashUsed = Me.FlashUsed
            result.FlashTotal = Me.FlashTotal
            result.FlashPercent = Me.FlashPercent

            ' Calculate Arduino IDE-style RAM usage
            ' IDE includes more system components in the calculation
            result.RamTotal = Me.RamTotal

            ' Check if we're dealing with ESP32P4 (newer chip)
            Dim isESP32P4 = (Me.RamTotal >= 327680)  ' 320KB RAM or more

            If isESP32P4 Then
                ' For ESP32P4: IDE shows about 9-10% more memory usage
                result.RamAvailable = Math.Max(0, Me.RamAvailable - CInt(Me.RamTotal * 0.09))  ' Estimate overhead
            Else
                ' For regular ESP32: IDE shows about 5-6% more memory usage
                result.RamAvailable = Math.Max(0, Me.RamAvailable - CInt(Me.RamTotal * 0.06))  ' Estimate overhead
            End If

            result.RamUsed = result.RamTotal - result.RamAvailable
            result.RamPercent = CInt(Math.Min(100, (result.RamUsed / CDbl(result.RamTotal)) * 100))

            Return result
        End Function
    End Class

    ' Arguments for compile/upload operations
    Private Class CompileArgs
        Public Property SelectedBoardIndex As Integer
        Public Property SelectedComPort As String
        Public Property ConfigCopy As ArduinoBoardConfig
        Public Property IsBin As Boolean
    End Class

    ' Make sure memory mode is properly initialized in New()
    Public Sub New()
        InitializeComponent()

        ' Set default paths - boardsTxtPath will be set appropriately later
        boardsTxtPath = Path.Combine(Application.StartupPath, "boards.txt")
        arduinoUserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Arduino15")
        If Not Directory.Exists(arduinoUserDir) Then
            arduinoUserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arduino15")
            If Not Directory.Exists(arduinoUserDir) Then
                arduinoUserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Local", "Arduino15")
            End If
        End If

        ' Initialize memory reporting mode to Both by default
        memReportingMode = MemoryReportingMode.Both

        ' Initialize UI components
        If Not DesignMode Then
            InitializeControls()
        End If

        ' Load saved settings if available
        LoadSettings()
    End Sub

    ' Update the cmbMemoryMode_SelectedIndexChanged to properly handle mode changes


    Private Sub InitializeControls()
        ' First check and ensure txtOutput exists and is properly configured
        If txtOutput Is Nothing Then
            txtOutput = New TextBox With {
            .Name = "txtOutput",
            .Multiline = True,
            .ScrollBars = ScrollBars.Both,
            .ReadOnly = True,
            .BackColor = Color.White,
            .Dock = DockStyle.None,  ' Change to None initially so we can position it
            .Visible = True  ' Explicitly set visible
        }
            Controls.Add(txtOutput)
            txtOutput.BringToFront()  ' Ensure it's at the front of z-order
        End If

        ' Initialize progress bars and labels
        If lblCompileProgress IsNot Nothing Then lblCompileProgress.Text = "Compile Progress: 0%"
        If lblUploadProgress IsNot Nothing Then lblUploadProgress.Text = "Upload Progress: 0%"
        If pbCompile IsNot Nothing Then
            pbCompile.Value = 0
            pbCompile.Maximum = 100
        End If
        If pbUpload IsNot Nothing Then
            pbUpload.Value = 0
            pbUpload.Maximum = 100
        End If

        ' Calculate form dimensions to ensure all controls fit
        Dim formPadding = 20  ' Padding from form edges
        Dim controlWidth = Width - (formPadding * 2)  ' Width for most controls
        Dim labelWidth = 110  ' Width for labels
        Dim inputWidth = 260  ' Width for input fields
        Dim buttonWidth = 120  ' Width for buttons
        Dim controlHeight = 23  ' Height for most controls
        Dim spacing = 35  ' Space between control groups

        ' Start with proper Y positions - create a consistent layout
        Dim currentY = formPadding

        ' Arduino CLI path controls
        If Controls.Find("lblCliPath", True).Length = 0 Then
            Dim lblCliPath As New Label With {
            .Name = "lblCliPath",
            .Text = "Arduino CLI:",
            .AutoSize = True,
            .Location = New Point(formPadding, currentY + 3)
        }
            Controls.Add(lblCliPath)
        End If

        If txtCliPath IsNot Nothing Then
            txtCliPath.Location = New Point(formPadding + labelWidth, currentY)
            txtCliPath.Size = New Size(inputWidth, controlHeight)
        End If

        If btnSelectCli IsNot Nothing Then
            btnSelectCli.Location = New Point(formPadding + labelWidth + inputWidth + 10, currentY)
            btnSelectCli.Size = New Size(buttonWidth, 30)
        End If

        currentY += spacing

        ' Sketch path controls
        If Controls.Find("lblSketchPath", True).Length = 0 Then
            Dim lblSketchPath As New Label With {
            .Name = "lblSketchPath",
            .Text = "Sketch Path:",
            .AutoSize = True,
            .Location = New Point(formPadding, currentY + 3)
        }
            Controls.Add(lblSketchPath)
        End If

        If txtSketchPath IsNot Nothing Then
            txtSketchPath.Location = New Point(formPadding + labelWidth, currentY)
            txtSketchPath.Size = New Size(inputWidth, controlHeight)
        End If

        If btnSelectSketch IsNot Nothing Then
            btnSelectSketch.Location = New Point(formPadding + labelWidth + inputWidth + 10, currentY)
            btnSelectSketch.Size = New Size(buttonWidth, 30)
        End If

        currentY += spacing

        ' Boards.txt path controls
        If Controls.Find("lblBoardsTxt", True).Length = 0 Then
            Dim lblBoardsTxt As New Label With {
            .Name = "lblBoardsTxt",
            .Text = "boards.txt Path:",
            .AutoSize = True,
            .Location = New Point(formPadding, currentY + 3)
        }
            Controls.Add(lblBoardsTxt)
        End If

        Dim txtBoardsTxtPath = TryCast(Controls.Find("txtBoardsTxtPath", True).FirstOrDefault(), TextBox)
        If txtBoardsTxtPath Is Nothing Then
            txtBoardsTxtPath = New TextBox With {
            .Name = "txtBoardsTxtPath",
            .ReadOnly = True,
            .Location = New Point(formPadding + labelWidth, currentY),
            .Size = New Size(inputWidth, controlHeight),
            .Text = boardsTxtPath
        }
            Controls.Add(txtBoardsTxtPath)
        Else
            txtBoardsTxtPath.Location = New Point(formPadding + labelWidth, currentY)
            txtBoardsTxtPath.Size = New Size(inputWidth, controlHeight)
        End If

        Dim btnSelectBoardsTxt = TryCast(Controls.Find("btnSelectBoardsTxt", True).FirstOrDefault(), Button)
        If btnSelectBoardsTxt Is Nothing Then
            btnSelectBoardsTxt = New Button With {
            .Name = "btnSelectBoardsTxt",
            .Text = "Select boards.txt",
            .Location = New Point(formPadding + labelWidth + inputWidth + 10, currentY),
            .Size = New Size(buttonWidth, 30),
            .Visible = True
        }
            AddHandler btnSelectBoardsTxt.Click, AddressOf btnSelectBoardsTxt_Click
            Controls.Add(btnSelectBoardsTxt)
        Else
            btnSelectBoardsTxt.Location = New Point(formPadding + labelWidth + inputWidth + 10, currentY)
            btnSelectBoardsTxt.Size = New Size(buttonWidth, 30)
        End If

        currentY += spacing

        ' Custom partition controls
        If Controls.Find("lblPartition", True).Length = 0 Then
            Dim lblPartition As New Label With {
            .Name = "lblPartition",
            .Text = "Partition File:",
            .AutoSize = True,
            .Location = New Point(formPadding, currentY + 3)
        }
            Controls.Add(lblPartition)
        End If

        If txtPartitionPath IsNot Nothing Then
            txtPartitionPath.Location = New Point(formPadding + labelWidth, currentY)
            txtPartitionPath.Size = New Size(inputWidth, controlHeight)
        End If

        If btnSelectPartition IsNot Nothing Then
            btnSelectPartition.Location = New Point(formPadding + labelWidth + inputWidth + 10, currentY)
            btnSelectPartition.Size = New Size(buttonWidth, 30)
        End If

        currentY += spacing

        ' Board selection controls
        If Controls.Find("lblBoard", True).Length = 0 Then
            Dim lblBoard As New Label With {
            .Name = "lblBoard",
            .Text = "Select Board:",
            .AutoSize = True,
            .Location = New Point(formPadding, currentY + 3)
        }
            Controls.Add(lblBoard)
        End If

        If cmbBoards IsNot Nothing Then
            cmbBoards.Location = New Point(formPadding + labelWidth, currentY)
            cmbBoards.Size = New Size(inputWidth, controlHeight)
        End If

        If btnBoardConfig IsNot Nothing Then
            btnBoardConfig.Location = New Point(formPadding + labelWidth + inputWidth + 10, currentY)
            btnBoardConfig.Size = New Size(buttonWidth, 30)
        End If

        currentY += spacing

        ' COM port selection controls
        If Controls.Find("lblComPort", True).Length = 0 Then
            Dim lblComPort As New Label With {
            .Name = "lblComPort",
            .Text = "COM Port:",
            .AutoSize = True,
            .Location = New Point(formPadding, currentY + 3)
        }
            Controls.Add(lblComPort)
        End If

        If cmbComPorts IsNot Nothing Then
            cmbComPorts.Location = New Point(formPadding + labelWidth, currentY)
            cmbComPorts.Size = New Size(inputWidth, controlHeight)
        End If

        If btnRefreshPorts IsNot Nothing Then
            btnRefreshPorts.Location = New Point(formPadding + labelWidth + inputWidth + 10, currentY)
            btnRefreshPorts.Size = New Size(buttonWidth, 30)
        End If

        currentY += spacing

        ' Memory reporting mode dropdown
        If Controls.Find("lblMemoryMode", True).Length = 0 Then
            Dim lblMemoryMode As New Label With {
            .Name = "lblMemoryMode",
            .Text = "Memory Reporting:",
            .AutoSize = True,
            .Location = New Point(formPadding, currentY + 3)
        }
            Controls.Add(lblMemoryMode)
        End If

        ' Create or reposition memory mode ComboBox
        cmbMemoryMode = TryCast(Me.Controls.Find("cmbMemoryMode", True).FirstOrDefault(), ComboBox)
        If cmbMemoryMode Is Nothing Then
            cmbMemoryMode = New ComboBox() With {
            .Name = "cmbMemoryMode",
            .Location = New Point(formPadding + labelWidth, currentY),
            .Size = New Size(inputWidth, controlHeight),
            .DropDownStyle = ComboBoxStyle.DropDownList
        }
            AddHandler cmbMemoryMode.SelectedIndexChanged, AddressOf cmbMemoryMode_SelectedIndexChanged
            Controls.Add(cmbMemoryMode)
        Else
            cmbMemoryMode.Location = New Point(formPadding + labelWidth, currentY)
            cmbMemoryMode.Size = New Size(inputWidth, controlHeight)
        End If

        ' Initialize the memory reporting dropdown
        cmbMemoryMode.Items.Clear()
        cmbMemoryMode.Items.AddRange([Enum].GetNames(GetType(MemoryReportingMode)))
        cmbMemoryMode.SelectedItem = memReportingMode.ToString()

        currentY += spacing + 10  ' Add extra spacing before compile/build buttons

        ' Action buttons (compile, bin, upload) in a row
        Dim actionButtonWidth = 120
        Dim buttonSpacing = 20
        Dim buttonStartX = (Width - (3 * actionButtonWidth + 2 * buttonSpacing)) / 2  ' Center the buttons

        If btnCompile IsNot Nothing Then
            btnCompile.Location = New Point(buttonStartX, currentY)
            btnCompile.Size = New Size(actionButtonWidth, 30)
        End If

        If btnGenBin IsNot Nothing Then
            btnGenBin.Location = New Point(buttonStartX + actionButtonWidth + buttonSpacing, currentY)
            btnGenBin.Size = New Size(actionButtonWidth, 30)
        End If

        If btnUpload IsNot Nothing Then
            btnUpload.Location = New Point(buttonStartX + 2 * actionButtonWidth + 2 * buttonSpacing, currentY)
            btnUpload.Size = New Size(actionButtonWidth, 30)
        End If

        currentY += 40

        ' Progress bars and their labels - must ensure they're visible
        If lblCompileProgress IsNot Nothing Then
            lblCompileProgress.Location = New Point(formPadding, currentY)
            lblCompileProgress.AutoSize = True
        End If

        currentY += 25

        If pbCompile IsNot Nothing Then
            pbCompile.Location = New Point(formPadding, currentY)
            pbCompile.Size = New Size(Width - (formPadding * 2), 20)
        End If

        currentY += 30

        If lblUploadProgress IsNot Nothing Then
            lblUploadProgress.Location = New Point(formPadding, currentY)
            lblUploadProgress.AutoSize = True
        End If

        currentY += 25

        If pbUpload IsNot Nothing Then
            pbUpload.Location = New Point(formPadding, currentY)
            pbUpload.Size = New Size(Width - (formPadding * 2), 20)
        End If

        currentY += 35

        ' Output textbox - make it fill the remaining space and ensure it's visible
        If txtOutput IsNot Nothing Then
            txtOutput.Location = New Point(formPadding, currentY)
            txtOutput.Size = New Size(Width - (formPadding * 2), Height - currentY - 50)  ' Leave space at bottom
            txtOutput.Multiline = True
            txtOutput.ScrollBars = ScrollBars.Both
            txtOutput.BackColor = Color.White
            txtOutput.ReadOnly = True
            txtOutput.Visible = True  ' Explicitly set visible again
            txtOutput.BringToFront()  ' Make sure it's at the front of the z-order

            ' Debug information to see if txtOutput is being created correctly
            AppendOutputLine("Output textbox initialized")
        End If

        ' Increase the size of the form to ensure all controls are visible
        MinimumSize = New Size(600, currentY + 300)  ' Ensure form is large enough for all controls
        Size = New Size(Math.Max(Size.Width, MinimumSize.Width),
                    Math.Max(Size.Height, MinimumSize.Height))

        ' Force a layout update
        PerformLayout()
        Refresh()
    End Sub

    Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        ' Adjust the output textbox size when the form is resized
        If txtOutput IsNot Nothing Then
            ' Find the position of the output box
            Dim formPadding = 20
            Dim outputTop = txtOutput.Top

            ' Resize the output box to fill the remaining space
            txtOutput.Width = Width - (formPadding * 2)
            txtOutput.Height = Height - outputTop - 50  ' Leave space at the bottom
        End If
    End Sub
    Private Sub EnsureTextOutputVisible()
        If txtOutput Is Nothing Then Return

        ' Make sure txtOutput is properly configured and visible
        txtOutput.Visible = True
        txtOutput.BringToFront()

        ' If txtOutput appears to be hidden, try setting it to dock at the bottom
        If Not txtOutput.Visible OrElse txtOutput.Height < 50 Then
            txtOutput.Dock = DockStyle.Bottom
            txtOutput.Height = 200  ' Set a reasonable height
        End If

        ' Add a test message
        txtOutput.AppendText("Output window initialized. " + DateTime.Now.ToString() + vbCrLf)

        ' Force layout update
        PerformLayout()
        Refresh()
    End Sub


    Private Sub btnSelectBoardsTxt_Click(sender As Object, e As EventArgs)
        ' Allow user to select a boards.txt file
        Using ofd As New OpenFileDialog()
            ofd.Filter = "Boards Definition (*.txt)|*.txt|All Files (*.*)|*.*"
            ofd.Title = "Select boards.txt file"
            ofd.FileName = "boards.txt"

            If ofd.ShowDialog() = DialogResult.OK Then
                boardsTxtPath = ofd.FileName

                ' Update textbox if it exists
                Dim txtBoardsTxtPath = TryCast(Controls.Find("txtBoardsTxtPath", True).FirstOrDefault(), TextBox)
                If txtBoardsTxtPath IsNot Nothing Then
                    txtBoardsTxtPath.Text = boardsTxtPath
                End If

                ' Save this path to settings
                SaveSettings()

                ' Try to load the boards file
                LoadBoardsTxt()
            End If
        End Using
    End Sub

    Private Sub LoadSettings()
        Try
            Dim settingsPath = Path.Combine(Application.StartupPath, "esp32loader_settings.ini")
            If File.Exists(settingsPath) Then
                Dim lines = File.ReadAllLines(settingsPath)
                For Each line In lines
                    If line.StartsWith("ArduinoCliPath=") Then
                        arduinoCliPath = line.Substring("ArduinoCliPath=".Length)
                        txtCliPath.Text = arduinoCliPath
                    ElseIf line.StartsWith("LastSketchPath=") Then
                        sketchPath = line.Substring("LastSketchPath=".Length)
                        txtSketchPath.Text = sketchPath
                    ElseIf line.StartsWith("CustomPartitionPath=") Then
                        customPartitionFilePath = line.Substring("CustomPartitionPath=".Length)
                        If File.Exists(customPartitionFilePath) Then
                            txtPartitionPath.Text = customPartitionFilePath
                        Else
                            customPartitionFilePath = ""
                            txtPartitionPath.Text = "No custom partition file selected"
                        End If
                    ElseIf line.StartsWith("MemoryReportingMode=") Then
                        Try
                            memReportingMode = CType([Enum].Parse(GetType(MemoryReportingMode), line.Substring("MemoryReportingMode=".Length)), MemoryReportingMode)
                        Catch
                            memReportingMode = MemoryReportingMode.Both
                        End Try
                    ElseIf line.StartsWith("BoardsTxtPath=") Then
                        ' Load saved boards.txt path if it exists
                        Dim savedPath = line.Substring("BoardsTxtPath=".Length)
                        If File.Exists(savedPath) Then
                            boardsTxtPath = savedPath

                            ' Update textbox if it exists
                            Dim txtBoardsTxtPath = TryCast(Controls.Find("txtBoardsTxtPath", True).FirstOrDefault(), TextBox)
                            If txtBoardsTxtPath IsNot Nothing Then
                                txtBoardsTxtPath.Text = boardsTxtPath
                            End If
                        End If
                    End If
                Next

                ' Auto-detect if any CLI was found
                If Not String.IsNullOrEmpty(arduinoCliPath) AndAlso File.Exists(arduinoCliPath) Then
                    GetArduinoCliVersion()
                    DetectEsp32Core()
                    DetectBoardList()
                End If
            End If
        Catch ex As Exception
            ' Silently fail on settings load
            txtOutput.AppendText("Error loading settings: " & ex.Message & vbCrLf)
        End Try
    End Sub

    Private Sub SaveSettings()
        Try
            Dim settingsPath = Path.Combine(Application.StartupPath, "esp32loader_settings.ini")
            Dim settings As New List(Of String)

            settings.Add($"ArduinoCliPath={arduinoCliPath}")
            settings.Add($"LastSketchPath={sketchPath}")
            settings.Add($"CustomPartitionPath={customPartitionFilePath}")
            settings.Add($"MemoryReportingMode={memReportingMode}")
            settings.Add($"BoardsTxtPath={boardsTxtPath}")

            File.WriteAllLines(settingsPath, settings.ToArray())
        Catch ex As Exception
            ' Silently fail on settings save
            txtOutput.AppendText("Error saving settings: " & ex.Message & vbCrLf)
        End Try
    End Sub

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        RefreshComPorts()
        LoadBoardsTxt()
        If String.IsNullOrEmpty(txtPartitionPath.Text) Then
            txtPartitionPath.Text = "No custom partition file selected"
        End If

        ' Update textbox for boards.txt path
        Dim txtBoardsTxtPath = TryCast(Controls.Find("txtBoardsTxtPath", True).FirstOrDefault(), TextBox)
        If txtBoardsTxtPath IsNot Nothing Then
            txtBoardsTxtPath.Text = boardsTxtPath
        End If

        ' Set the memory reporting mode in the combobox
        If cmbMemoryMode IsNot Nothing Then
            cmbMemoryMode.SelectedItem = memReportingMode.ToString()
        End If

        ' Make sure txtOutput is visible
        EnsureTextOutputVisible()
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' Save settings when closing
        SaveSettings()
    End Sub

    Private Sub btnRefreshPorts_Click(sender As Object, e As EventArgs) Handles btnRefreshPorts.Click
        RefreshComPorts()
    End Sub

    Private Sub btnSelectCli_Click(sender As Object, e As EventArgs) Handles btnSelectCli.Click
        Using ofd As New OpenFileDialog()
            ofd.Filter = "arduino-cli.exe|arduino-cli.exe"
            If ofd.ShowDialog() = DialogResult.OK Then
                arduinoCliPath = ofd.FileName
                txtCliPath.Text = arduinoCliPath
                GetArduinoCliVersion()
                DetectEsp32Core()
                DetectBoardList()
                SaveSettings()
            End If
        End Using
    End Sub

    Private Sub btnSelectPartition_Click(sender As Object, e As EventArgs) Handles btnSelectPartition.Click
        Using ofd As New OpenFileDialog()
            ofd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            ofd.Title = "Select custom partitions.csv"
            If ofd.ShowDialog() = DialogResult.OK Then
                customPartitionFilePath = ofd.FileName
                txtPartitionPath.Text = customPartitionFilePath
                SaveSettings()

                ' Verify it's a valid partition file
                Try
                    Dim content As String = File.ReadAllText(customPartitionFilePath)
                    If content.Contains("nvs") AndAlso content.Contains("app") Then
                        AppendOutputLine("✅ Valid partition file detected.")
                        AppendOutputLine("Custom partition will be used when 'PartitionScheme=custom' is selected.")
                    Else
                        AppendOutputLine("⚠️ Warning: This might not be a valid partition file.")
                        AppendOutputLine("It should contain sections for 'nvs' and 'app' partitions.")
                    End If

                    ' If board config has PartitionScheme=custom, notify user
                    If boardConfig IsNot Nothing Then
                        Dim hasCustomPartition = False
                        For Each opt In boardConfig.Options
                            If opt.Key.ToLower() = "partitionscheme" AndAlso opt.Value.ToLower() = "custom" Then
                                hasCustomPartition = True
                                Exit For
                            End If
                        Next

                        If hasCustomPartition Then
                            AppendOutputLine("✅ Custom partition file will be used for the next build.")
                        Else
                            AppendOutputLine("ℹ️ To use this partition file, select 'PartitionScheme=custom' in Board Config.")
                        End If
                    End If
                Catch ex As Exception
                    AppendOutputLine("Error reading partition file: " & ex.Message)
                End Try
            End If
        End Using
    End Sub

    ' Update the cmbMemoryMode_SelectedIndexChanged to properly handle mode changes
    Private Sub cmbMemoryMode_SelectedIndexChanged(sender As Object, e As EventArgs)
        If cmbMemoryMode Is Nothing OrElse cmbMemoryMode.SelectedIndex < 0 Then Return

        ' Parse the selected enum value
        Try
            memReportingMode = CType([Enum].Parse(GetType(MemoryReportingMode), cmbMemoryMode.SelectedItem.ToString()), MemoryReportingMode)

            ' Update displayed stats if we have them
            If lastCompileStats IsNot Nothing Then
                DisplayCompilationStats(lastCompileStats)
            End If

            SaveSettings()
            AppendOutputLine($"Memory reporting mode changed to {memReportingMode}")
        Catch ex As Exception
            AppendOutputLine($"Error changing memory reporting mode: {ex.Message}")
        End Try
    End Sub

    Private Sub GetArduinoCliVersion()
        If String.IsNullOrEmpty(arduinoCliPath) Then Return

        Try
            Dim psi As New ProcessStartInfo(arduinoCliPath, "version")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            Dim p As Process = Process.Start(psi)
            arduinoCliVersion = p.StandardOutput.ReadToEnd().Trim()
            p.WaitForExit()

            ' Log the version
            txtOutput.AppendText("Arduino CLI Version: " & arduinoCliVersion & vbCrLf)
        Catch ex As Exception
            MessageBox.Show("Error checking Arduino CLI version: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnSelectSketch_Click(sender As Object, e As EventArgs) Handles btnSelectSketch.Click
        Using fbd As New FolderBrowserDialog()
            If fbd.ShowDialog() = DialogResult.OK Then
                sketchPath = fbd.SelectedPath
                txtSketchPath.Text = sketchPath
                SaveSettings()

                ' Check if sketch directory contains a custom partition file
                Dim sketchPartitionFile = Path.Combine(sketchPath, "partitions.csv")
                If File.Exists(sketchPartitionFile) Then
                    AppendOutputLine("Found partitions.csv in sketch directory.")
                    If MessageBox.Show("Found partitions.csv in sketch directory. Use it as custom partition?",
                                     "Custom Partition Found", MessageBoxButtons.YesNo,
                                     MessageBoxIcon.Question) = DialogResult.Yes Then
                        customPartitionFilePath = sketchPartitionFile
                        txtPartitionPath.Text = customPartitionFilePath
                        SaveSettings()
                    End If
                End If
            End If
        End Using
    End Sub

    Private Sub LoadBoardsTxt()
        boardList.Clear()
        cmbBoards.Items.Clear()

        ' Check if the boards.txt exists
        If Not File.Exists(boardsTxtPath) Then
            AppendOutputLine($"⚠️ boards.txt not found at: {boardsTxtPath}")
            AppendOutputLine("Please select a valid boards.txt file using the 'Select boards.txt' button.")

            ' Check if we can find it in possible Arduino locations
            Dim foundPath = FindBoardsTxtInCommonLocations()
            If Not String.IsNullOrEmpty(foundPath) Then
                boardsTxtPath = foundPath
                AppendOutputLine($"✅ Found boards.txt at: {boardsTxtPath}")

                ' Update textbox if it exists
                Dim txtBoardsTxtPath = TryCast(Controls.Find("txtBoardsTxtPath", True).FirstOrDefault(), TextBox)
                If txtBoardsTxtPath IsNot Nothing Then
                    txtBoardsTxtPath.Text = boardsTxtPath
                End If

                ' Save this path to settings
                SaveSettings()
            Else
                Return  ' Can't proceed without boards.txt
            End If
        End If

        Try
            boardList = ArduinoBoard.ParseBoardsTxt(boardsTxtPath)
            AppendOutputLine($"✅ Successfully loaded {boardList.Count} boards from boards.txt")

            For Each b In boardList
                cmbBoards.Items.Add(b.Name)
            Next
            If cmbBoards.Items.Count > 0 Then
                cmbBoards.SelectedIndex = 0
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading boards.txt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            AppendOutputLine($"❌ Failed to load boards.txt: {ex.Message}")
            AppendOutputLine("Please select a valid boards.txt file using the 'Select boards.txt' button.")
        End Try
    End Sub

    Private Function FindBoardsTxtInCommonLocations() As String
        ' Try to find boards.txt in common Arduino locations
        Dim possibleLocations = New List(Of String) From {
            Path.Combine(arduinoUserDir, "packages", "esp32", "hardware", "esp32"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arduino15", "packages", "esp32", "hardware", "esp32"),
            Path.Combine(Application.StartupPath, "hardware", "esp32", "esp32"),
            Path.GetDirectoryName(If(String.IsNullOrEmpty(arduinoCliPath), "", arduinoCliPath))
        }

        For Each location As String In possibleLocations
            If String.IsNullOrEmpty(location) Then Continue For

            ' For ESP32 core, we need to find the version directory
            If Directory.Exists(location) Then
                ' Check direct location
                Dim directBoardsTxt = Path.Combine(location, "boards.txt")
                If File.Exists(directBoardsTxt) Then
                    Return directBoardsTxt
                End If

                ' Check in version subdirectories
                Dim dirs() As String = Directory.GetDirectories(location)
                If dirs.Length > 0 Then
                    ' Sort by name (which is version) and get the latest
                    Array.Sort(dirs)
                    Dim latestVersionDir = dirs(dirs.Length - 1)

                    ' Check for boards.txt in version dir
                    Dim versionBoardsTxt = Path.Combine(latestVersionDir, "boards.txt")
                    If File.Exists(versionBoardsTxt) Then
                        Return versionBoardsTxt
                    End If
                End If
            End If
        Next

        Return ""  ' Not found in any common location
    End Function

    Private Sub DetectBoardList()
        If String.IsNullOrEmpty(arduinoCliPath) Then Return

        Try
            Dim psi As New ProcessStartInfo(arduinoCliPath, "board list")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            Dim p As Process = Process.Start(psi)
            Dim output = p.StandardOutput.ReadToEnd()
            p.WaitForExit()

            ' Log the output for debugging
            txtOutput.Text = "Detected boards: " & vbCrLf & output & vbCrLf
        Catch ex As Exception
            MessageBox.Show("Error detecting boards: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function FindEsp32CorePath() As String
        ' Find ESP32 core directory in common locations
        Dim possibleLocations = New List(Of String) From {
            Path.Combine(arduinoUserDir, "packages", "esp32", "hardware", "esp32"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arduino15", "packages", "esp32", "hardware", "esp32"),
            Path.Combine(Application.StartupPath, "hardware", "esp32", "esp32"),
            Path.Combine(Path.GetDirectoryName(arduinoCliPath), "hardware", "esp32", "esp32")
        }

        ' Check each location
        For Each location As String In possibleLocations
            If Directory.Exists(location) Then
                ' Find the latest version directory
                Dim dirs() As String = Directory.GetDirectories(location)
                If dirs.Length > 0 Then
                    ' Sort by name (which is version) and get the latest
                    Array.Sort(dirs)
                    Return dirs(dirs.Length - 1)
                End If
            End If
        Next

        Return ""
    End Function

    Private Sub DetectEsp32Core()
        If String.IsNullOrEmpty(arduinoCliPath) Then Return

        Try
            ' First, check if we need to upgrade the core index
            Dim psi As New ProcessStartInfo(arduinoCliPath, "core update-index")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            Dim p As Process = Process.Start(psi)
            p.WaitForExit()

            ' Then check installed cores
            psi = New ProcessStartInfo(arduinoCliPath, "core list")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            p = Process.Start(psi)
            Dim output = p.StandardOutput.ReadToEnd()
            p.WaitForExit()

            ' Log the output for debugging
            txtOutput.AppendText("Detected cores: " & vbCrLf & output & vbCrLf)

            If Not output.ToLower().Contains("esp32") Then
                If MessageBox.Show("ESP32 core not found in arduino-cli. Would you like to install it now?", "ESP32 Core Not Found",
                                 MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    InstallEsp32Core()
                End If
            End If

            ' List the available boards
            psi = New ProcessStartInfo(arduinoCliPath, "board listall")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            p = Process.Start(psi)
            output = p.StandardOutput.ReadToEnd()
            p.WaitForExit()

            txtOutput.AppendText("Available boards: " & vbCrLf & output & vbCrLf)

            ' Look for ESP32 core path and check partition files
            Dim esp32CorePath = FindEsp32CorePath()
            If Not String.IsNullOrEmpty(esp32CorePath) Then
                txtOutput.AppendText($"Found ESP32 core at: {esp32CorePath}" & vbCrLf)

                ' Check if we have a huge_app partition file
                Dim partitionsDir = Path.Combine(esp32CorePath, "tools", "partitions")
                If Directory.Exists(partitionsDir) Then
                    Dim partitionFiles = Directory.GetFiles(partitionsDir, "*.csv")
                    txtOutput.AppendText($"Found {partitionFiles.Length} partition files in ESP32 core" & vbCrLf)

                    Dim hasHugeApp = False
                    For Each partFile In partitionFiles
                        If Path.GetFileName(partFile).ToLower().Contains("huge_app") Then
                            hasHugeApp = True
                            txtOutput.AppendText($"Found huge_app partition: {Path.GetFileName(partFile)}" & vbCrLf)
                            Exit For
                        End If
                    Next

                    If Not hasHugeApp Then
                        ' Create a huge app partition file
                        txtOutput.AppendText("No huge_app partition found. Creating one..." & vbCrLf)
                        CreateHugeAppPartition(partitionsDir)
                    End If
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Error checking ESP32 core: " & ex.Message)
        End Try
    End Sub

    Private Sub CreateHugeAppPartition(partitionsDir As String)
        ' Create a huge app partition file for 16MB flash
        Dim hugeAppPartitionFile = Path.Combine(partitionsDir, "huge_app.csv")

        ' Standard huge app partition for 16MB flash
        Dim partitionContent = "# Name,   Type, SubType, Offset,  Size, Flags" & vbCrLf &
                             "nvs,      data, nvs,     0x9000,  0x5000," & vbCrLf &
                             "otadata,  data, ota,     0xe000,  0x2000," & vbCrLf &
                             "app0,     app,  ota_0,   0x10000, 0xF00000," & vbCrLf &
                             "app1,     app,  ota_1,   0xF10000,0xF00000," & vbCrLf &
                             "spiffs,   data, spiffs,  0x1E10000,0x1F0000,"

        Try
            File.WriteAllText(hugeAppPartitionFile, partitionContent)
            txtOutput.AppendText($"Created huge_app partition file: {hugeAppPartitionFile}" & vbCrLf)

            ' Also modify boards.txt to add this partition scheme
            Dim esp32CorePath = Path.GetDirectoryName(partitionsDir)
            While Not Path.GetFileName(esp32CorePath).Equals("esp32")
                esp32CorePath = Path.GetDirectoryName(esp32CorePath)
            End While

            Dim boardsFile = Path.Combine(esp32CorePath, "boards.txt")
            If File.Exists(boardsFile) Then
                Dim boardsContent = File.ReadAllText(boardsFile)

                ' Check if the huge_app is already in boards.txt for ESP32P4
                If Not boardsContent.Contains("esp32p4.menu.PartitionScheme.huge_app") Then
                    Dim hugeAppConfig = vbCrLf &
                        "esp32p4.menu.PartitionScheme.huge_app=Huge App (16MB)" & vbCrLf &
                        "esp32p4.menu.PartitionScheme.huge_app.build.partitions=huge_app" & vbCrLf &
                        "esp32p4.menu.PartitionScheme.huge_app.upload.maximum_size=16777216" & vbCrLf

                    ' Add to the end of the file
                    File.AppendAllText(boardsFile, hugeAppConfig)
                    txtOutput.AppendText("Added huge_app partition scheme to ESP32P4 board configuration" & vbCrLf)
                End If
            End If
        Catch ex As Exception
            txtOutput.AppendText($"Error creating partition file: {ex.Message}" & vbCrLf)
        End Try
    End Sub

    Private Sub InstallEsp32Core()
        If String.IsNullOrEmpty(arduinoCliPath) Then Return

        txtOutput.AppendText("Installing ESP32 core..." & vbCrLf)

        Try
            Dim psi As New ProcessStartInfo(arduinoCliPath, "core update-index")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True

            Dim p As Process = Process.Start(psi)
            txtOutput.AppendText(p.StandardOutput.ReadToEnd() & vbCrLf)
            p.WaitForExit()

            psi = New ProcessStartInfo(arduinoCliPath, "core install esp32:esp32")
            psi.RedirectStandardOutput = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True

            p = Process.Start(psi)
            txtOutput.AppendText(p.StandardOutput.ReadToEnd() & vbCrLf)
            p.WaitForExit()

            txtOutput.AppendText("ESP32 core installation completed." & vbCrLf)
        Catch ex As Exception
            MessageBox.Show("Error installing ESP32 core: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RefreshComPorts()
        Dim selectedPort = If(cmbComPorts.SelectedItem IsNot Nothing, cmbComPorts.SelectedItem.ToString(), "")

        cmbComPorts.Items.Clear()
        For Each port In SerialPort.GetPortNames()
            cmbComPorts.Items.Add(port)
        Next

        If cmbComPorts.Items.Count > 0 Then
            ' Try to reselect the previously selected port if it still exists
            Dim index = cmbComPorts.FindStringExact(selectedPort)
            If index >= 0 Then
                cmbComPorts.SelectedIndex = index
            Else
                cmbComPorts.SelectedIndex = 0
            End If
        End If
    End Sub

    Private Sub btnBoardConfig_Click(sender As Object, e As EventArgs) Handles btnBoardConfig.Click
        If cmbBoards.SelectedIndex = -1 Then
            MessageBox.Show("Select a board first.")
            Return
        End If
        Dim selectedBoard = boardList(cmbBoards.SelectedIndex)
        Dim frm As New BoardConfigForm(selectedBoard)
        If frm.ShowDialog() = DialogResult.OK Then
            boardConfig = frm.SelectedConfig
            ' Show the user that configuration has been saved
            txtOutput.AppendText("Board configuration saved: " & selectedBoard.Name & vbCrLf)
            For Each opt In boardConfig.Options
                ' Check if custom partition is selected - remind user to select a partition file
                If opt.Key.ToLower() = "partitionscheme" AndAlso opt.Value.ToLower() = "custom" Then
                    If String.IsNullOrEmpty(customPartitionFilePath) OrElse Not File.Exists(customPartitionFilePath) Then
                        AppendOutputLine("⚠️ You selected 'custom' partition scheme but no custom partition file is set.")
                        AppendOutputLine("Please select a custom partition file using the 'Select Partition' button.")
                    End If
                End If
                txtOutput.AppendText($"  - {opt.Key}: {opt.Value}" & vbCrLf)
            Next
        End If
    End Sub

    Private Sub btnCompile_Click(sender As Object, e As EventArgs) Handles btnCompile.Click
        If Not ReadyToBuild() Then Exit Sub
        pbCompile.Value = 0
        lblCompileProgress.Text = "Compile Progress: 0%"
        btnCompile.Enabled = False
        btnGenBin.Enabled = False
        btnUpload.Enabled = False
        txtOutput.Text = "Compiling in progress..." & vbCrLf

        Dim selectedBoardIndex As Integer = cmbBoards.SelectedIndex
        Dim selectedComPort As String = If(cmbComPorts.SelectedItem Is Nothing, "", cmbComPorts.SelectedItem.ToString())
        Dim configCopy As ArduinoBoardConfig = boardConfig

        bgwCompile.RunWorkerAsync(New CompileArgs With {
            .SelectedBoardIndex = selectedBoardIndex,
            .SelectedComPort = selectedComPort,
            .ConfigCopy = configCopy,
            .IsBin = False
        })
    End Sub

    Private Sub btnGenBin_Click(sender As Object, e As EventArgs) Handles btnGenBin.Click
        If Not ReadyToBuild() Then Exit Sub
        pbCompile.Value = 0
        lblCompileProgress.Text = "Compile Progress: 0%"
        btnCompile.Enabled = False
        btnGenBin.Enabled = False
        btnUpload.Enabled = False
        txtOutput.Text = "Binary generation in progress..." & vbCrLf

        Dim selectedBoardIndex As Integer = cmbBoards.SelectedIndex
        Dim selectedComPort As String = If(cmbComPorts.SelectedItem Is Nothing, "", cmbComPorts.SelectedItem.ToString())
        Dim configCopy As ArduinoBoardConfig = boardConfig

        bgwCompile.RunWorkerAsync(New CompileArgs With {
            .SelectedBoardIndex = selectedBoardIndex,
            .SelectedComPort = selectedComPort,
            .ConfigCopy = configCopy,
            .IsBin = True
        })
    End Sub

    Private Sub btnUpload_Click(sender As Object, e As EventArgs) Handles btnUpload.Click
        If Not ReadyToBuild() Then Exit Sub
        pbUpload.Value = 0
        lblUploadProgress.Text = "Upload Progress: 0%"
        btnCompile.Enabled = False
        btnGenBin.Enabled = False
        btnUpload.Enabled = False
        txtOutput.Text = "Upload in progress..." & vbCrLf

        Dim selectedBoardIndex As Integer = cmbBoards.SelectedIndex
        Dim selectedComPort As String = If(cmbComPorts.SelectedItem Is Nothing, "", cmbComPorts.SelectedItem.ToString())
        Dim configCopy As ArduinoBoardConfig = boardConfig

        bgwUpload.RunWorkerAsync(New CompileArgs With {
            .SelectedBoardIndex = selectedBoardIndex,
            .SelectedComPort = selectedComPort,
            .ConfigCopy = configCopy,
            .IsBin = False
        })
    End Sub

    Private Function ReadyToBuild() As Boolean
        If String.IsNullOrEmpty(arduinoCliPath) OrElse Not File.Exists(arduinoCliPath) Then
            MessageBox.Show("Please select a valid arduino-cli.exe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        If String.IsNullOrEmpty(sketchPath) OrElse Not Directory.Exists(sketchPath) Then
            MessageBox.Show("Please select a valid sketch folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        ' Check if sketch has .ino file
        Dim inoFiles = Directory.GetFiles(sketchPath, "*.ino")
        If inoFiles.Length = 0 Then
            MessageBox.Show("The selected folder does not contain an .ino sketch file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        ' Check if boards.txt is valid
        If Not File.Exists(boardsTxtPath) Then
            MessageBox.Show("Please select a valid boards.txt file. Current path doesn't exist: " & boardsTxtPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)

            ' Try to auto-find a valid boards.txt
            Dim foundPath = FindBoardsTxtInCommonLocations()
            If Not String.IsNullOrEmpty(foundPath) Then
                boardsTxtPath = foundPath

                ' Update textbox if it exists
                Dim txtBoardsTxtPath = TryCast(Controls.Find("txtBoardsTxtPath", True).FirstOrDefault(), TextBox)
                If txtBoardsTxtPath IsNot Nothing Then
                    txtBoardsTxtPath.Text = boardsTxtPath
                End If

                AppendOutputLine($"✅ Auto-found boards.txt at: {boardsTxtPath}")
                LoadBoardsTxt()
            Else
                ' Ask user to select boards.txt
                If MessageBox.Show("No boards.txt found. Would you like to select one now?", "boards.txt Required",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    ' Get the button we created and click it
                    Dim btnSelectBoardsTxt = TryCast(Controls.Find("btnSelectBoardsTxt", True).FirstOrDefault(), Button)
                    If btnSelectBoardsTxt IsNot Nothing Then
                        btnSelectBoardsTxt.PerformClick()
                    Else
                        ' Fall back to direct method call if button not found
                        btnSelectBoardsTxt_Click(Nothing, EventArgs.Empty)
                    End If

                    ' Check if we now have a valid boards.txt
                    If Not File.Exists(boardsTxtPath) Then
                        Return False
                    End If
                Else
                    Return False
                End If
            End If
        End If

        If cmbBoards.SelectedIndex = -1 Then
            MessageBox.Show("Please select a board from the dropdown menu.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        If cmbComPorts.SelectedIndex = -1 Then
            MessageBox.Show("Please select a COM port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        If boardConfig Is Nothing Then
            MessageBox.Show("Please configure and save the board settings (Board Config).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            btnBoardConfig.PerformClick()
            If boardConfig Is Nothing Then Return False
        End If

        ' Check if custom partition is selected but no file is set
        Dim hasCustomPartition = False
        If boardConfig IsNot Nothing Then
            For Each opt In boardConfig.Options
                If opt.Key.ToLower() = "partitionscheme" AndAlso opt.Value.ToLower() = "custom" Then
                    hasCustomPartition = True
                    Exit For
                End If
            Next
        End If

        If hasCustomPartition AndAlso (String.IsNullOrEmpty(customPartitionFilePath) OrElse Not File.Exists(customPartitionFilePath)) Then
            Dim result = MessageBox.Show("You selected 'custom' partition scheme but no custom partition file is set." & vbCrLf &
                                      "Do you want to select a partition file now?",
                                     "Custom Partition Required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If result = DialogResult.Yes Then
                btnSelectPartition.PerformClick()
                If String.IsNullOrEmpty(customPartitionFilePath) OrElse Not File.Exists(customPartitionFilePath) Then
                    Return False
                End If
            Else
                ' User decided not to select a custom partition, warn that one will be created
                AppendOutputLine("⚠️ No custom partition file selected. Using default 16MB partition layout.")
            End If
        End If

        Return True
    End Function

    ' --- BackgroundWorker Compile ---
    Private Sub bgwCompile_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgwCompile.DoWork
        Dim args = CType(e.Argument, CompileArgs)
        e.Result = RunArduinoCliWithRealtimeProgress(
            If(args.IsBin, "compile --export-binaries", "compile"),
            CType(sender, BackgroundWorker),
            args.SelectedBoardIndex,
            args.SelectedComPort,
            args.ConfigCopy,
            cliStagesCompile
        )
    End Sub

    Private Sub bgwCompile_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles bgwCompile.ProgressChanged
        pbCompile.Value = Math.Min(e.ProgressPercentage, 100)
        lblCompileProgress.Text = $"Compile Progress: {pbCompile.Value}%"
    End Sub

    Private Sub bgwCompile_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bgwCompile.RunWorkerCompleted
        pbCompile.Value = 100
        lblCompileProgress.Text = "Compile Progress: 100%"
        btnCompile.Enabled = True
        btnGenBin.Enabled = True
        btnUpload.Enabled = True
        If e.Error IsNot Nothing Then
            txtOutput.Text &= vbCrLf & "Error: " & e.Error.Message
        ElseIf e.Result IsNot Nothing Then
            txtOutput.Text &= e.Result.ToString()
        End If

        AppendOutputLine("Compilation completed.")
    End Sub

    ' --- BackgroundWorker Upload ---
    Private Sub bgwUpload_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgwUpload.DoWork
        Dim args = CType(e.Argument, CompileArgs)
        e.Result = RunArduinoCliWithRealtimeProgress(
            "upload",
            CType(sender, BackgroundWorker),
            args.SelectedBoardIndex,
            args.SelectedComPort,
            args.ConfigCopy,
            cliStagesUpload
        )
    End Sub

    Private Sub bgwUpload_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles bgwUpload.ProgressChanged
        pbUpload.Value = Math.Min(e.ProgressPercentage, 100)
        lblUploadProgress.Text = $"Upload Progress: {pbUpload.Value}%"
    End Sub

    Private Sub bgwUpload_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bgwUpload.RunWorkerCompleted
        pbUpload.Value = 100
        lblUploadProgress.Text = "Upload Progress: 100%"
        btnCompile.Enabled = True
        btnGenBin.Enabled = True
        btnUpload.Enabled = True
        If e.Error IsNot Nothing Then
            txtOutput.Text &= vbCrLf & "Error: " & e.Error.Message
        ElseIf e.Result IsNot Nothing Then
            txtOutput.Text &= e.Result.ToString()
        End If

        AppendOutputLine("Upload completed.")
    End Sub

    ' Parse memory usage statistics from compiler output
    ' Update the ParseCompileStats method to better handle Arduino CLI output
    Private Function ParseCompileStats(output As String) As CompileStats
        Dim result As New CompileStats()

        Try
            ' Parse program storage (flash) usage - expanded pattern matching for different formats
            Dim flashMatch = Regex.Match(output, "Sketch uses (\d+) bytes \((\d+)%\) of program storage space. Maximum is (\d+) bytes")
            If flashMatch.Success Then
                result.FlashUsed = Integer.Parse(flashMatch.Groups(1).Value)
                result.FlashPercent = Integer.Parse(flashMatch.Groups(2).Value)
                result.FlashTotal = Integer.Parse(flashMatch.Groups(3).Value)
            Else
                ' Alternative pattern for some Arduino CLI versions
                flashMatch = Regex.Match(output, "(\d+) bytes \((\d+\.\d+)%\) of program storage space. Maximum is (\d+) bytes")
                If flashMatch.Success Then
                    result.FlashUsed = Integer.Parse(flashMatch.Groups(1).Value)
                    result.FlashPercent = CInt(Math.Round(Double.Parse(flashMatch.Groups(2).Value)))
                    result.FlashTotal = Integer.Parse(flashMatch.Groups(3).Value)
                End If
            End If

            ' Parse RAM usage - expanded pattern matching for different formats
            Dim ramMatch = Regex.Match(output, "Global variables use (\d+) bytes \((\d+)%\) of dynamic memory, leaving (\d+) bytes for local variables")
            If ramMatch.Success Then
                result.RamUsed = Integer.Parse(ramMatch.Groups(1).Value)
                result.RamPercent = Integer.Parse(ramMatch.Groups(2).Value)
                result.RamAvailable = Integer.Parse(ramMatch.Groups(3).Value)
                result.RamTotal = result.RamUsed + result.RamAvailable
            Else
                ' Alternative pattern for some Arduino CLI versions
                ramMatch = Regex.Match(output, "Global variables use (\d+) bytes \((\d+\.\d+)%\) of dynamic memory, leaving (\d+) bytes")
                If ramMatch.Success Then
                    result.RamUsed = Integer.Parse(ramMatch.Groups(1).Value)
                    result.RamPercent = CInt(Math.Round(Double.Parse(ramMatch.Groups(2).Value)))
                    result.RamAvailable = Integer.Parse(ramMatch.Groups(3).Value)
                    result.RamTotal = result.RamUsed + result.RamAvailable
                End If
            End If

            ' If RAM statistics weren't found, look for values in output lines
            If result.RamTotal = 0 Then
                Dim lines = output.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                For Each line In lines
                    If line.Contains("Global variables") AndAlso line.Contains("bytes") AndAlso line.Contains("%") Then
                        ' Try to extract numbers from the line
                        Dim numbers = Regex.Matches(line, "\d+").Cast(Of Match)().Select(Function(m) Integer.Parse(m.Value)).ToList()
                        If numbers.Count >= 3 Then
                            result.RamUsed = numbers(0)
                            result.RamPercent = numbers(1)
                            result.RamAvailable = numbers(2)
                            result.RamTotal = result.RamUsed + result.RamAvailable
                            Exit For
                        End If
                    End If
                Next
            End If

            ' If we still don't have complete data, create default values for IDE mode
            If result.FlashUsed > 0 AndAlso result.FlashTotal > 0 AndAlso result.RamTotal = 0 Then
                ' Estimate RAM based on typical ESP32 values
                result.RamTotal = 327680  ' Typical ESP32 RAM
                result.RamUsed = CInt(result.FlashUsed * 0.15)  ' Estimate RAM usage as 15% of flash usage
                result.RamAvailable = result.RamTotal - result.RamUsed
                result.RamPercent = CInt((result.RamUsed / CDbl(result.RamTotal)) * 100)

                AppendOutputLine("⚠️ Warning: RAM statistics not found in output. Using estimated values.")
            End If
        Catch ex As Exception
            AppendOutputLine($"Error parsing memory statistics: {ex.Message}")
        End Try

        Return result
    End Function

    ' Display compilation statistics in the output, accounting for reporting mode
    ' Update the DisplayCompilationStats method to better handle different reporting modes
    Private Sub DisplayCompilationStats(stats As CompileStats)
        If stats Is Nothing Then
            AppendOutputLine("No compilation statistics available to display.")
            Return
        End If

        ' Common prefix for both modes
        Dim memoryReport As New StringBuilder()

        ' Check if we have valid flash statistics
        Dim hasValidFlash = (stats.FlashUsed > 0 AndAlso stats.FlashTotal > 0)

        ' Check if we have valid RAM statistics
        Dim hasValidRam = (stats.RamUsed > 0 AndAlso stats.RamTotal > 0)

        ' Display stats based on selected mode
        Select Case memReportingMode
            Case MemoryReportingMode.ArduinoCli
                ' Display original arduino-cli numbers
                memoryReport.AppendLine("────────────────────────────────────────────────────────────")
                memoryReport.AppendLine($"Memory Usage (arduino-cli):")

                If hasValidFlash Then
                    memoryReport.AppendLine($"Flash: {stats.FlashUsed:N0} bytes ({stats.FlashPercent}%) of {stats.FlashTotal:N0} bytes")
                Else
                    memoryReport.AppendLine("Flash: Not available")
                End If

                If hasValidRam Then
                    memoryReport.AppendLine($"RAM:   {stats.RamUsed:N0} bytes ({stats.RamPercent}%) of {stats.RamTotal:N0} bytes")
                Else
                    memoryReport.AppendLine("RAM:   Not available")
                End If

            Case MemoryReportingMode.ArduinoIDE
                ' Calculate and display Arduino IDE style numbers
                Dim ideStats = If(hasValidRam, stats.ConvertToIDEStyle(), New CompileStats())

                memoryReport.AppendLine("────────────────────────────────────────────────────────────")
                memoryReport.AppendLine($"Memory Usage (Arduino IDE-style):")

                If hasValidFlash Then
                    memoryReport.AppendLine($"Flash: {stats.FlashUsed:N0} bytes ({stats.FlashPercent}%) of {stats.FlashTotal:N0} bytes")
                Else
                    memoryReport.AppendLine("Flash: Not available")
                End If

                If hasValidRam Then
                    memoryReport.AppendLine($"RAM:   {ideStats.RamUsed:N0} bytes ({ideStats.RamPercent}%) of {ideStats.RamTotal:N0} bytes")
                Else
                    memoryReport.AppendLine("RAM:   Not available")
                End If

            Case MemoryReportingMode.Both
                ' Show both styles for comparison
                Dim ideStats = If(hasValidRam, stats.ConvertToIDEStyle(), New CompileStats())

                memoryReport.AppendLine("────────────────────────────────────────────────────────────")
                memoryReport.AppendLine($"Memory Usage (arduino-cli / Arduino IDE):")

                If hasValidFlash Then
                    memoryReport.AppendLine($"Flash: {stats.FlashUsed:N0} bytes ({stats.FlashPercent}%) / {stats.FlashUsed:N0} bytes ({stats.FlashPercent}%) of {stats.FlashTotal:N0} bytes")
                Else
                    memoryReport.AppendLine("Flash: Not available")
                End If

                If hasValidRam Then
                    memoryReport.AppendLine($"RAM:   {stats.RamUsed:N0} bytes ({stats.RamPercent}%) / {ideStats.RamUsed:N0} bytes ({ideStats.RamPercent}%) of {stats.RamTotal:N0} bytes")
                    memoryReport.AppendLine("")
                    memoryReport.AppendLine("Note: Arduino IDE reports higher RAM usage because it")
                    memoryReport.AppendLine("includes additional system components in the calculation.")
                Else
                    memoryReport.AppendLine("RAM:   Not available")
                End If
        End Select

        memoryReport.AppendLine("────────────────────────────────────────────────────────────")

        AppendOutputLine(memoryReport.ToString())
    End Sub

    ' --- Core CLI handler with smooth real-time progress ---
    Private Function RunArduinoCliWithRealtimeProgress(command As String, bgw As BackgroundWorker, selectedBoardIndex As Integer, selectedComPort As String, configCopy As ArduinoBoardConfig, stages() As String) As String
        Dim selectedBoard = boardList(selectedBoardIndex)
        Dim baseFqbn = selectedBoard.FQBN
        Dim outputText As New StringBuilder()

        ' Build command properly with board options
        ' Log options for debugging
        If configCopy IsNot Nothing AndAlso configCopy.Options.Count > 0 Then
            AppendOutputLine("Board options:")
            For Each opt In configCopy.Options
                AppendOutputLine($"  - {opt.Key}: {opt.Value}")
            Next
        End If

        ' Check if we're dealing with custom partition
        Dim isEsp32P4 As Boolean = baseFqbn.Contains("esp32:esp32:esp32p4") OrElse baseFqbn.Contains("esp32p4")
        Dim hasCustomPartition As Boolean = False
        Dim partitionScheme As String = ""

        If configCopy IsNot Nothing Then
            For Each opt In configCopy.Options
                If opt.Key.ToLower() = "partitionscheme" Then
                    partitionScheme = opt.Value.ToLower()
                    If partitionScheme = "custom" Then
                        hasCustomPartition = True
                    End If
                    Exit For
                End If
            Next
        End If

        ' For custom partition, we need special handling
        Dim hugeAppPartitionPath As String = ""
        Dim backupPartitionPath As String = ""

        ' Generate default partition file if needed
        If hasCustomPartition AndAlso (String.IsNullOrEmpty(customPartitionFilePath) OrElse Not File.Exists(customPartitionFilePath)) Then
            ' Create a default custom partition file in the sketch directory
            customPartitionFilePath = Path.Combine(sketchPath, "partitions.csv")

            ' Standard huge app partition for 16MB flash
            Dim partitionContent = "# Name,   Type, SubType, Offset,  Size, Flags" & vbCrLf &
                            "nvs,      data, nvs,     0x9000,  0x5000," & vbCrLf &
                            "otadata,  data, ota,     0xe000,  0x2000," & vbCrLf &
                            "app0,     app,  ota_0,   0x10000, 0xF00000," & vbCrLf &
                            "app1,     app,  ota_1,   0xF10000,0xF00000," & vbCrLf &
                            "spiffs,   data, spiffs,  0x1E10000,0x1F0000,"

            Try
                File.WriteAllText(customPartitionFilePath, partitionContent)
                AppendOutputLine($"Created default 16MB partition file in sketch directory: {customPartitionFilePath}")
                txtPartitionPath.Text = customPartitionFilePath
                SaveSettings()
            Catch ex As Exception
                AppendOutputLine($"Error creating partition file: {ex.Message}")
            End Try
        End If

        ' Process custom partition if needed
        If hasCustomPartition AndAlso Not String.IsNullOrEmpty(customPartitionFilePath) AndAlso File.Exists(customPartitionFilePath) Then
            Dim esp32CorePath As String = FindEsp32CorePath()
            If Not String.IsNullOrEmpty(esp32CorePath) Then
                AppendOutputLine($"Found ESP32 core at: {esp32CorePath}")

                ' Prepare custom partition - copy to ESP32 core's huge_app.csv
                Dim partitionsDir = Path.Combine(esp32CorePath, "tools", "partitions")
                hugeAppPartitionPath = Path.Combine(partitionsDir, "huge_app.csv")
                backupPartitionPath = hugeAppPartitionPath & ".bak"

                If Directory.Exists(partitionsDir) Then
                    Try
                        ' Backup original huge_app.csv if it exists
                        If File.Exists(hugeAppPartitionPath) AndAlso Not File.Exists(backupPartitionPath) Then
                            File.Copy(hugeAppPartitionPath, backupPartitionPath, True)
                            AppendOutputLine("Backed up original huge_app.csv")
                        End If

                        ' Copy our custom partition file to huge_app.csv
                        File.Copy(customPartitionFilePath, hugeAppPartitionPath, True)
                        AppendOutputLine($"Copied custom partition to ESP32 core: {hugeAppPartitionPath}")

                        ' Change partition scheme in board config to huge_app
                        If configCopy IsNot Nothing Then
                            Dim keys = configCopy.Options.Keys.ToList()
                            For i As Integer = 0 To keys.Count - 1
                                If keys(i).ToLower() = "partitionscheme" Then
                                    configCopy.Options(keys(i)) = "huge_app"
                                    AppendOutputLine("Changed partition scheme to huge_app in FQBN")
                                    Exit For
                                End If
                            Next
                        End If

                        ' Also create build.json in sketch directory to force 16MB partition
                        Dim buildJsonFile = Path.Combine(sketchPath, "build.json")
                        Dim buildJsonContent = "{" & vbCrLf &
                                     "  ""build.partitions"": ""huge_app""," & vbCrLf &
                                     "  ""build.flash_size"": ""16MB""," & vbCrLf &
                                     "  ""upload.maximum_size"": 16777216" & vbCrLf &
                                     "}"

                        File.WriteAllText(buildJsonFile, buildJsonContent)
                        AppendOutputLine("Created build.json in sketch directory to force 16MB partition")

                    Catch ex As Exception
                        AppendOutputLine("Error handling custom partition: " & ex.Message)
                    End Try
                End If
            End If
        End If

        ' Build FQBN string based on board and options
        Dim fullFqbn As String = baseFqbn

        ' Fix FQBN format: ensure it has vendor:architecture:board structure
        If Not fullFqbn.Contains(":") Then
            ' If it's just a board name, assume it's ESP32
            fullFqbn = "esp32:esp32:" & fullFqbn
            AppendOutputLine($"Corrected FQBN format to: {fullFqbn}")
        ElseIf fullFqbn.Split(":"c).Length = 2 Then
            ' If it has only one colon (vendor:board), add architecture
            Dim parts = fullFqbn.Split(":"c)
            fullFqbn = parts(0) & ":" & parts(0) & ":" & parts(1)
            AppendOutputLine($"Corrected FQBN format to: {fullFqbn}")
        End If

        ' For ESP32P4 with custom partition, use simplified FQBN with huge_app
        If isEsp32P4 AndAlso hasCustomPartition Then
            fullFqbn = "esp32:esp32:esp32p4:PartitionScheme=huge_app"
            AppendOutputLine($"Using simplified FQBN with huge_app: {fullFqbn}")
        Else
            ' For other cases, use full FQBN with all options
            If configCopy IsNot Nothing AndAlso configCopy.Options.Count > 0 Then
                Dim optionsString As String = ""
                For Each opt In configCopy.Options
                    optionsString &= ":" & opt.Key & "=" & opt.Value
                Next
                fullFqbn &= optionsString
            End If
            AppendOutputLine($"Using FQBN with options: {fullFqbn}")
        End If

        ' Build CLI command
        Dim args As String
        If command.Contains("upload") Then
            args = $"{command} -p {selectedComPort} -b {fullFqbn} ""{sketchPath}"""
        Else
            args = $"{command} -b {fullFqbn} ""{sketchPath}"""
        End If

        AppendOutputLine("Running command: " & arduinoCliPath & " " & args)

        ' Setup process
        Dim psi As New ProcessStartInfo(arduinoCliPath, args) With {
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .WorkingDirectory = Path.GetDirectoryName(sketchPath)
        }

        ' Create a process and get ready to read output
        Dim proc As New Process() With {.StartInfo = psi, .EnableRaisingEvents = True}

        ' Setup an event handler for output and error
        Dim currentStage As Integer = 0
        Dim progress As Integer = 5 ' Start at 5%
        Dim lastReportedProgress As Integer = 0
        Dim outputCompleted As Boolean = False
        Dim errorCompleted As Boolean = False
        Dim foundCorrectPartitionSize As Boolean = False

        ' Event handlers for real-time output
        AddHandler proc.OutputDataReceived, Sub(sender, e)
                                                If e.Data IsNot Nothing Then
                                                    outputText.AppendLine(e.Data)
                                                    AppendOutputLine(e.Data)

                                                    ' Check for 16MB partition size in output
                                                    If e.Data.Contains("Maximum is 16777216 bytes") Then
                                                        AppendOutputLine("✅ Successfully using 16MB partition scheme!")
                                                        foundCorrectPartitionSize = True
                                                    End If

                                                    ' Check for stage completion
                                                    For i = 0 To stages.Length - 1
                                                        If e.Data.IndexOf(stages(i), StringComparison.OrdinalIgnoreCase) >= 0 Then
                                                            Dim newProgress = ((i + 1) * 90) / stages.Length
                                                            If newProgress > progress Then
                                                                progress = newProgress
                                                                If bgw IsNot Nothing AndAlso bgw.WorkerReportsProgress Then
                                                                    bgw.ReportProgress(progress)
                                                                    lastReportedProgress = progress
                                                                End If
                                                            End If
                                                            Exit For
                                                        End If
                                                    Next

                                                    ' Force periodic progress updates even without specific stage marker
                                                    If Not e.Data.Trim().Equals(String.Empty) Then
                                                        progress = Math.Min(95, progress + 1)
                                                        If progress > lastReportedProgress Then
                                                            If bgw IsNot Nothing AndAlso bgw.WorkerReportsProgress Then
                                                                bgw.ReportProgress(progress)
                                                                lastReportedProgress = progress
                                                            End If
                                                        End If
                                                    End If
                                                Else
                                                    outputCompleted = True
                                                End If
                                            End Sub

        AddHandler proc.ErrorDataReceived, Sub(sender, e)
                                               If e.Data IsNot Nothing Then
                                                   outputText.AppendLine(e.Data)
                                                   AppendOutputLine(e.Data)
                                               Else
                                                   errorCompleted = True
                                               End If
                                           End Sub

        ' Start the process and read output asynchronously
        proc.Start()
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()

        ' Report initial progress
        bgw.ReportProgress(5)

        ' Wait for process to complete with periodic UI updates
        Dim startTime = DateTime.Now
        While Not proc.HasExited OrElse Not outputCompleted OrElse Not errorCompleted
            ' Check for cancellation
            If bgw.CancellationPending Then
                Try
                    ' Attempt to kill the process
                    If Not proc.HasExited Then
                        proc.Kill()
                    End If
                Catch ex As Exception
                    ' Ignore errors on killing the process
                End Try
                Return "Operation was canceled."
            End If

            ' Periodically update progress if there's been a long time without updates
            If (DateTime.Now - startTime).TotalSeconds > 5 AndAlso lastReportedProgress < 90 Then
                progress = Math.Min(90, lastReportedProgress + 5)
                bgw.ReportProgress(progress)
                lastReportedProgress = progress
                startTime = DateTime.Now
            End If

            Thread.Sleep(100)  ' Don't burn CPU cycles
        End While

        ' Clean up
        RemoveHandler proc.OutputDataReceived, Nothing
        RemoveHandler proc.ErrorDataReceived, Nothing

        ' Report final status
        bgw.ReportProgress(100)

        ' Clean up custom partition files if needed
        If hasCustomPartition AndAlso Not String.IsNullOrEmpty(hugeAppPartitionPath) AndAlso File.Exists(backupPartitionPath) Then
            Try
                ' Restore original huge_app.csv
                File.Copy(backupPartitionPath, hugeAppPartitionPath, True)
                File.Delete(backupPartitionPath)
                AppendOutputLine("Restored original huge_app.csv")
            Catch ex As Exception
                AppendOutputLine("Warning: Could not restore original partition file: " & ex.Message)
            End Try
        End If

        ' Check if we succeeded with 16MB partition
        If hasCustomPartition AndAlso Not foundCorrectPartitionSize Then
            AppendOutputLine("⚠️ Warning: Not using 16MB partition scheme. Will try to improve for next compile.")
        End If

        ' Extract compilation stats from output
        lastCompileStats = ParseCompileStats(outputText.ToString())
        If lastCompileStats IsNot Nothing Then
            ' Check if we got valid stats
            If lastCompileStats.FlashUsed > 0 AndAlso lastCompileStats.FlashTotal > 0 Then
                DisplayCompilationStats(lastCompileStats)
            Else
                AppendOutputLine("⚠️ Warning: Could not extract complete memory statistics from compilation output.")

                ' Create basic stats from the output text
                Dim rawOutput = outputText.ToString()
                AppendOutputLine("Memory usage (raw output):")

                Dim lines = rawOutput.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                For Each line In lines
                    If line.Contains("Sketch uses") OrElse
               line.Contains("Global variables") OrElse
               line.Contains("bytes of program") OrElse
               line.Contains("dynamic memory") Then
                        AppendOutputLine(line.Trim())
                    End If
                Next
            End If
        End If

        ' Return result
        Return outputText.ToString()
    End Function

    Private Sub AppendOutputLine(line As String)
        If txtOutput.InvokeRequired Then
            txtOutput.Invoke(Sub()
                                 txtOutput.AppendText(line & vbCrLf)
                                 txtOutput.SelectionStart = txtOutput.TextLength
                                 txtOutput.ScrollToCaret()
                             End Sub)
        Else
            txtOutput.AppendText(line & vbCrLf)
            txtOutput.SelectionStart = txtOutput.TextLength
            txtOutput.ScrollToCaret()
        End If
    End Sub
End Class