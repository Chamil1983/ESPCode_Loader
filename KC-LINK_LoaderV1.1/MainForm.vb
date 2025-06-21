Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms


Public Class MainForm
    Inherits Form

    ' Private fields
    Private WithEvents arduinoCliProcess As Process = Nothing
    Private WithEvents workerThread As BackgroundWorker
    Private projectPath As String = String.Empty
    Private compilationOutput As String = String.Empty
    Private isCompiling As Boolean = False
    Private isUploading As Boolean = False
    Private WithEvents serialMonitorTool As SerialMonitor
    Private hardwareStats As HardwareStats
    Private boardConfigManager As BoardManager
    Private WithEvents outputRefreshTimer As New Timer() With {
        .Interval = 250,
        .Enabled = False
    }
    Private lastOutputLine As String = String.Empty
    Private compilationStartTime As DateTime

    ' Add fields for enhanced compilation progress tracking
    Private compileOutputLines As New List(Of String)
    Private lastProgressUpdate As DateTime = DateTime.MinValue
    Private compilationPhase As String = ""
    Private expectedSteps As Integer = 10  ' ESP32 compilation typically has ~10 steps
    Private currentStep As Integer = 0
    Private builderExitCode As Integer = 0

    ' UI Controls Declaration
    Private WithEvents txtProjectPath As TextBox
    Private WithEvents txtCompilerOutput As RichTextBox
    Private WithEvents btnBrowseProject As Button
    Private WithEvents btnCompile As Button
    Private WithEvents btnUpload As Button
    Private WithEvents btnSettings As Button
    Private WithEvents btnMonitor As Button
    Private WithEvents btnRefreshPorts As Button
    Private WithEvents cmbSerialPort As ComboBox
    Private WithEvents cmbBoardType As ComboBox
    Private WithEvents cmbPartitionOption As ComboBox
    Private WithEvents btnConfigPath As Button
    Private WithEvents buildProgressBar As ProgressBar
    'Private WithEvents flashUsageBar As ProgressBar
    'Private WithEvents ramUsageBar As ProgressBar
    Private flashUsageBar As ColoredProgressBar
    Private ramUsageBar As ColoredProgressBar
    Private WithEvents lblFlashUsage As Label
    Private WithEvents lblRAMUsage As Label
    Private WithEvents lblCompileTime As Label
    Private WithEvents lblSuccessRate As Label
    Private WithEvents lblProjectName As Label
    Private WithEvents lblFileCount As Label
    Private WithEvents lblProjectType As Label
    Private WithEvents lblCodeLines As Label
    Private WithEvents lblPartitionScheme As Label
    Private WithEvents lblStatusIndicator As Label
    Private WithEvents toolStripStatus As StatusStrip
    Private WithEvents toolStripStatusLabel As ToolStripStatusLabel
    Private WithEvents toolStripProgressBar As ToolStripProgressBar

    ' Menu controls
    Private WithEvents mnuMain As MenuStrip
    Private WithEvents mnuFile As ToolStripMenuItem
    Private WithEvents mnuSettings As ToolStripMenuItem
    Private WithEvents mnuHelp As ToolStripMenuItem
    Private WithEvents mnuOpenProject As ToolStripMenuItem
    Private WithEvents mnuRecentProjects As ToolStripMenuItem
    Private WithEvents mnuExit As ToolStripMenuItem
    Private WithEvents mnuBoardConfig As ToolStripMenuItem
    Private WithEvents mnuLoadBoardsFile As ToolStripMenuItem
    Private WithEvents mnuBoardSettings As ToolStripMenuItem
    Private WithEvents mnuAbout As ToolStripMenuItem

    Public Sub New()
        ' Initialize components
        InitializeComponent()

        ' Initialize background worker for async compilation
        workerThread = New BackgroundWorker()
        workerThread.WorkerReportsProgress = True
        workerThread.WorkerSupportsCancellation = True

        ' Initialize serial monitor
        serialMonitorTool = New SerialMonitor()

        ' Initialize statistics tracker
        hardwareStats = New HardwareStats()

        ' Initialize board manager
        boardConfigManager = New BoardManager()

        ' Initialize menu
        InitializeMenu()

        ' Populate board types combobox
        PopulateBoardComboBox()
        PopulatePartitionSchemes()

        ' Set default values for controls
        SetDefaults()

        ' Load settings
        LoadSettings()

        RefreshProgressBarColors()

        ' Log startup info
        LogMessage($"Application started by Chamil1983 at UTC 2025-06-20 04:26:24")
    End Sub

    Private Sub InitializeComponent()
        ' Form setup - using MyBase to access Form properties
        MyBase.Text = "KC-Link A8 Loader v1.1"
        MyBase.Size = New Size(950, 700)
        MyBase.StartPosition = FormStartPosition.CenterScreen
        MyBase.MinimumSize = New Size(800, 600)

        ' Main layout
        Dim mainLayout As New TableLayoutPanel()
        mainLayout.Dock = DockStyle.Fill
        mainLayout.RowCount = 4
        mainLayout.ColumnCount = 1
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 170))  ' Increased top panel height
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' Middle panel
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))   ' Progress bar
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 22))   ' Status strip
        mainLayout.Padding = New Padding(10, 10, 10, 0)

        ' Top panel - Project selection and controls
        Dim topPanel As New TableLayoutPanel()
        topPanel.Dock = DockStyle.Fill
        topPanel.ColumnCount = 3
        topPanel.RowCount = 5  ' Added an extra row for partition scheme
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
        topPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))  ' Project path
        topPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))  ' Board type
        topPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))  ' Serial port
        topPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))  ' Partition scheme
        topPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))  ' Action buttons
        topPanel.Margin = New Padding(0, 0, 0, 10)

        ' Project path
        Dim lblProject As New Label()
        lblProject.Text = "Project Path:"
        lblProject.TextAlign = ContentAlignment.MiddleRight
        lblProject.Dock = DockStyle.Fill
        lblProject.Margin = New Padding(3, 5, 3, 5)

        txtProjectPath = New TextBox()
        txtProjectPath.Dock = DockStyle.Fill
        txtProjectPath.Margin = New Padding(3, 5, 3, 5)

        btnBrowseProject = New Button()
        btnBrowseProject.Text = "Browse..."
        btnBrowseProject.Dock = DockStyle.Fill
        btnBrowseProject.Margin = New Padding(3, 3, 10, 3)

        ' Board type
        Dim lblBoardType As New Label()
        lblBoardType.Text = "Board Type:"
        lblBoardType.TextAlign = ContentAlignment.MiddleRight
        lblBoardType.Dock = DockStyle.Fill
        lblBoardType.Margin = New Padding(3, 5, 3, 5)

        cmbBoardType = New ComboBox()
        cmbBoardType.DropDownStyle = ComboBoxStyle.DropDownList
        cmbBoardType.Dock = DockStyle.Fill
        cmbBoardType.Margin = New Padding(3, 5, 3, 5)

        btnConfigPath = New Button()
        btnConfigPath.Text = "Board Config..."
        btnConfigPath.Dock = DockStyle.Fill
        btnConfigPath.Margin = New Padding(3, 3, 10, 3)

        ' Serial port selection
        Dim lblSerialPort As New Label()
        lblSerialPort.Text = "Serial Port:"
        lblSerialPort.TextAlign = ContentAlignment.MiddleRight
        lblSerialPort.Dock = DockStyle.Fill
        lblSerialPort.Margin = New Padding(3, 5, 3, 5)

        cmbSerialPort = New ComboBox()
        cmbSerialPort.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSerialPort.Dock = DockStyle.Fill
        cmbSerialPort.Margin = New Padding(3, 5, 3, 5)

        btnRefreshPorts = New Button()
        btnRefreshPorts.Text = "Refresh"
        btnRefreshPorts.Dock = DockStyle.Fill
        btnRefreshPorts.Margin = New Padding(3, 3, 10, 3)

        ' Partition scheme
        lblPartitionScheme = New Label()
        lblPartitionScheme.Text = "Partition Scheme:"
        lblPartitionScheme.TextAlign = ContentAlignment.MiddleRight
        lblPartitionScheme.Dock = DockStyle.Fill
        lblPartitionScheme.Margin = New Padding(3, 5, 3, 5)

        cmbPartitionOption = New ComboBox()
        cmbPartitionOption.DropDownStyle = ComboBoxStyle.DropDownList
        cmbPartitionOption.Dock = DockStyle.Fill
        cmbPartitionOption.Margin = New Padding(3, 5, 3, 5)

        ' Status indicator label
        lblStatusIndicator = New Label()
        lblStatusIndicator.Text = "Ready"
        lblStatusIndicator.TextAlign = ContentAlignment.MiddleLeft
        lblStatusIndicator.Dock = DockStyle.Fill
        lblStatusIndicator.ForeColor = Color.Green
        lblStatusIndicator.Font = New Font(lblStatusIndicator.Font, FontStyle.Bold)
        lblStatusIndicator.Margin = New Padding(3, 5, 3, 5)

        ' Create action buttons with FlowLayoutPanel
        btnCompile = New Button()
        btnCompile.Text = "Compile"
        btnCompile.Size = New Size(120, 40)
        btnCompile.BackColor = Color.LightBlue
        btnCompile.FlatStyle = FlatStyle.Standard
        btnCompile.Font = New Font("Microsoft Sans Serif", 9, FontStyle.Bold)
        btnCompile.Margin = New Padding(5)

        btnUpload = New Button()
        btnUpload.Text = "Upload"
        btnUpload.Size = New Size(120, 40)
        btnUpload.BackColor = Color.LightGreen
        btnUpload.FlatStyle = FlatStyle.Standard
        btnUpload.Font = New Font("Microsoft Sans Serif", 9, FontStyle.Bold)
        btnUpload.Margin = New Padding(5)

        btnMonitor = New Button()
        btnMonitor.Text = "Serial Monitor"
        btnMonitor.Size = New Size(140, 40)
        btnMonitor.BackColor = Color.LightYellow
        btnMonitor.FlatStyle = FlatStyle.Standard
        btnMonitor.Font = New Font("Microsoft Sans Serif", 9, FontStyle.Bold)
        btnMonitor.Margin = New Padding(5)

        btnSettings = New Button()
        btnSettings.Text = "Settings"
        btnSettings.Size = New Size(120, 40)
        btnSettings.BackColor = Color.LightGray
        btnSettings.FlatStyle = FlatStyle.Standard
        btnSettings.Font = New Font("Microsoft Sans Serif", 9, FontStyle.Bold)
        btnSettings.Margin = New Padding(5)

        ' Action buttons panel with FlowLayoutPanel for better visibility
        Dim actionPanel As New FlowLayoutPanel()
        actionPanel.Dock = DockStyle.Fill
        actionPanel.FlowDirection = FlowDirection.LeftToRight
        actionPanel.WrapContents = False
        actionPanel.AutoSize = True
        actionPanel.Padding = New Padding(5)
        actionPanel.Margin = New Padding(0, 5, 0, 5)

        ' Add buttons to the panel
        actionPanel.Controls.Add(btnCompile)
        actionPanel.Controls.Add(btnUpload)
        actionPanel.Controls.Add(btnMonitor)
        actionPanel.Controls.Add(btnSettings)

        ' Add controls to top panel with proper spacing
        topPanel.Controls.Add(lblProject, 0, 0)
        topPanel.Controls.Add(txtProjectPath, 1, 0)
        topPanel.Controls.Add(btnBrowseProject, 2, 0)
        topPanel.Controls.Add(lblBoardType, 0, 1)
        topPanel.Controls.Add(cmbBoardType, 1, 1)
        topPanel.Controls.Add(btnConfigPath, 2, 1)
        topPanel.Controls.Add(lblSerialPort, 0, 2)
        topPanel.Controls.Add(cmbSerialPort, 1, 2)
        topPanel.Controls.Add(btnRefreshPorts, 2, 2)
        topPanel.Controls.Add(lblPartitionScheme, 0, 3)
        topPanel.Controls.Add(cmbPartitionOption, 1, 3)
        topPanel.Controls.Add(lblStatusIndicator, 2, 3)
        topPanel.Controls.Add(actionPanel, 0, 4)
        topPanel.SetColumnSpan(actionPanel, 3)

        ' Middle panel - Output and stats
        Dim middlePanel As New TableLayoutPanel()
        middlePanel.Dock = DockStyle.Fill
        middlePanel.ColumnCount = 2
        middlePanel.RowCount = 1
        middlePanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70))
        middlePanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))
        middlePanel.Margin = New Padding(0, 5, 0, 5)

        ' Compiler output with line numbers
        txtCompilerOutput = New RichTextBox()
        txtCompilerOutput.ReadOnly = True
        txtCompilerOutput.BackColor = Color.Black
        txtCompilerOutput.ForeColor = Color.LightGreen
        txtCompilerOutput.Font = New Font("Consolas", 9)
        txtCompilerOutput.Dock = DockStyle.Fill
        txtCompilerOutput.Margin = New Padding(3)
        txtCompilerOutput.WordWrap = False

        Dim outputGroup As New GroupBox()
        outputGroup.Text = "Compiler Output"
        outputGroup.Dock = DockStyle.Fill
        outputGroup.Margin = New Padding(0, 0, 5, 0)
        outputGroup.Controls.Add(txtCompilerOutput)

        ' Stats panel with better spacing
        Dim statsPanel As New TableLayoutPanel()
        statsPanel.Dock = DockStyle.Fill
        statsPanel.RowCount = 2
        statsPanel.ColumnCount = 1
        statsPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
        statsPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 50))

        ' Project info section
        Dim projectInfoGroup As New GroupBox()
        projectInfoGroup.Text = "Project Info"
        projectInfoGroup.Dock = DockStyle.Fill
        projectInfoGroup.Margin = New Padding(0, 0, 0, 5)

        Dim projectInfoPanel As New TableLayoutPanel()
        projectInfoPanel.Dock = DockStyle.Fill
        projectInfoPanel.RowCount = 4
        projectInfoPanel.ColumnCount = 1
        projectInfoPanel.Padding = New Padding(5)

        lblProjectName = New Label()
        lblProjectName.Text = "Project: None selected"
        lblProjectName.Dock = DockStyle.Fill
        lblProjectName.Margin = New Padding(3)

        lblFileCount = New Label()
        lblFileCount.Text = "Files: 0"
        lblFileCount.Dock = DockStyle.Fill
        lblFileCount.Margin = New Padding(3)

        lblCodeLines = New Label()
        lblCodeLines.Text = "Code lines: 0"
        lblCodeLines.Dock = DockStyle.Fill
        lblCodeLines.Margin = New Padding(3)

        lblProjectType = New Label()
        lblProjectType.Text = "Type: Custom Project"
        lblProjectType.Dock = DockStyle.Fill
        lblProjectType.Margin = New Padding(3)

        projectInfoPanel.Controls.Add(lblProjectName, 0, 0)
        projectInfoPanel.Controls.Add(lblFileCount, 0, 1)
        projectInfoPanel.Controls.Add(lblCodeLines, 0, 2)
        projectInfoPanel.Controls.Add(lblProjectType, 0, 3)

        projectInfoGroup.Controls.Add(projectInfoPanel)

        ' Resources section
        Dim resourcesGroup As New GroupBox()
        resourcesGroup.Text = "Hardware Resources"
        resourcesGroup.Dock = DockStyle.Fill
        resourcesGroup.Margin = New Padding(0, 5, 0, 0)

        Dim resourcesPanel As New TableLayoutPanel()
        resourcesPanel.Dock = DockStyle.Fill
        resourcesPanel.RowCount = 6
        resourcesPanel.ColumnCount = 1
        resourcesPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        resourcesPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        resourcesPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        resourcesPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        resourcesPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        resourcesPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        resourcesPanel.Padding = New Padding(5)

        lblFlashUsage = New Label()
        lblFlashUsage.Text = "Flash: 0 bytes (0%)"
        lblFlashUsage.Dock = DockStyle.Fill
        lblFlashUsage.Margin = New Padding(3)

        flashUsageBar = New ColoredProgressBar()
        flashUsageBar.Minimum = 0
        flashUsageBar.Maximum = 100
        flashUsageBar.Value = 0
        flashUsageBar.Height = 15
        flashUsageBar.Dock = DockStyle.Fill
        flashUsageBar.BarColor = Color.RoyalBlue ' default



        lblRAMUsage = New Label()
        lblRAMUsage.Text = "RAM: 0 bytes (0%)"
        lblRAMUsage.Dock = DockStyle.Fill
        lblRAMUsage.Margin = New Padding(3)

        ramUsageBar = New ColoredProgressBar()
        ramUsageBar.Minimum = 0
        ramUsageBar.Maximum = 100
        ramUsageBar.Value = 0
        ramUsageBar.Height = 15
        ramUsageBar.Dock = DockStyle.Fill
        ramUsageBar.BarColor = Color.Orange ' default



        lblCompileTime = New Label()
        lblCompileTime.Text = "Last compile: 0.0 seconds"
        lblCompileTime.Dock = DockStyle.Fill
        lblCompileTime.Margin = New Padding(3)

        lblSuccessRate = New Label()
        lblSuccessRate.Text = "Success rate: 100%"
        lblSuccessRate.Dock = DockStyle.Fill
        lblSuccessRate.Margin = New Padding(3)

        resourcesPanel.Controls.Add(lblFlashUsage, 0, 0)
        resourcesPanel.Controls.Add(flashUsageBar, 0, 1)
        resourcesPanel.Controls.Add(lblRAMUsage, 0, 2)
        resourcesPanel.Controls.Add(ramUsageBar, 0, 3)
        resourcesPanel.Controls.Add(lblCompileTime, 0, 4)
        resourcesPanel.Controls.Add(lblSuccessRate, 0, 5)

        resourcesGroup.Controls.Add(resourcesPanel)

        statsPanel.Controls.Add(projectInfoGroup, 0, 0)
        statsPanel.Controls.Add(resourcesGroup, 0, 1)

        Dim statsGroup As New GroupBox()
        statsGroup.Text = "Statistics"
        statsGroup.Dock = DockStyle.Fill
        statsGroup.Margin = New Padding(5, 0, 0, 0)
        statsGroup.Controls.Add(statsPanel)

        ' Progress panel at bottom
        buildProgressBar = New ProgressBar()
        buildProgressBar.Minimum = 0
        buildProgressBar.Maximum = 100
        buildProgressBar.Value = 0
        buildProgressBar.Height = 20
        buildProgressBar.Dock = DockStyle.Fill
        buildProgressBar.Margin = New Padding(0, 5, 0, 0)

        ' Status strip
        toolStripStatus = New StatusStrip()
        toolStripStatus.SizingGrip = False

        toolStripStatusLabel = New ToolStripStatusLabel()
        toolStripStatusLabel.Text = "Ready"
        toolStripStatusLabel.Spring = True
        toolStripStatusLabel.TextAlign = ContentAlignment.MiddleLeft

        toolStripProgressBar = New ToolStripProgressBar()
        toolStripProgressBar.Visible = False
        toolStripProgressBar.Width = 150

        toolStripStatus.Items.Add(toolStripStatusLabel)
        toolStripStatus.Items.Add(toolStripProgressBar)

        ' Add components to middle panel
        middlePanel.Controls.Add(outputGroup, 0, 0)
        middlePanel.Controls.Add(statsGroup, 1, 0)

        ' Add panels to main layout
        mainLayout.Controls.Add(topPanel, 0, 0)
        mainLayout.Controls.Add(middlePanel, 0, 1)
        mainLayout.Controls.Add(buildProgressBar, 0, 2)
        mainLayout.Controls.Add(toolStripStatus, 0, 3)

        ' Add main layout to form
        MyBase.Controls.Add(mainLayout)

        ' Set up event handlers
        AddHandler MyBase.Load, AddressOf MainForm_Load
        AddHandler btnBrowseProject.Click, AddressOf btnBrowseProject_Click
        AddHandler btnCompile.Click, AddressOf btnCompile_Click
        AddHandler btnUpload.Click, AddressOf btnUpload_Click
        AddHandler btnSettings.Click, AddressOf btnSettings_Click
        AddHandler btnMonitor.Click, AddressOf btnMonitor_Click
        AddHandler btnRefreshPorts.Click, AddressOf btnRefreshPorts_Click
        AddHandler btnConfigPath.Click, AddressOf btnConfigPath_Click
        AddHandler cmbBoardType.SelectedIndexChanged, AddressOf cmbBoardType_SelectedIndexChanged
        AddHandler cmbPartitionOption.SelectedIndexChanged, AddressOf cmbPartitionOption_SelectedIndexChanged
        AddHandler MyBase.FormClosing, AddressOf MainForm_FormClosing
    End Sub

    Private Sub InitializeMenu()
        ' Create menu strip
        mnuMain = New MenuStrip()
        mnuMain.Dock = DockStyle.Top

        ' File menu
        mnuFile = New ToolStripMenuItem("File")
        mnuOpenProject = New ToolStripMenuItem("Open Project...", Nothing, AddressOf mnuOpenProject_Click)
        mnuRecentProjects = New ToolStripMenuItem("Recent Projects")
        mnuExit = New ToolStripMenuItem("Exit", Nothing, AddressOf mnuExit_Click)

        mnuFile.DropDownItems.Add(mnuOpenProject)
        mnuFile.DropDownItems.Add(mnuRecentProjects)
        mnuFile.DropDownItems.Add(New ToolStripSeparator())
        mnuFile.DropDownItems.Add(mnuExit)

        ' Settings menu
        mnuSettings = New ToolStripMenuItem("Settings")
        mnuBoardConfig = New ToolStripMenuItem("Board Configuration...", Nothing, AddressOf mnuBoardConfig_Click)
        mnuLoadBoardsFile = New ToolStripMenuItem("Load boards.txt File...", Nothing, AddressOf mnuLoadBoardsFile_Click)
        mnuBoardSettings = New ToolStripMenuItem("Application Settings...", Nothing, AddressOf mnuBoardSettings_Click)

        mnuSettings.DropDownItems.Add(mnuBoardConfig)
        mnuSettings.DropDownItems.Add(mnuLoadBoardsFile)
        mnuSettings.DropDownItems.Add(New ToolStripSeparator())
        mnuSettings.DropDownItems.Add(mnuBoardSettings)

        ' Help menu
        mnuHelp = New ToolStripMenuItem("Help")
        mnuAbout = New ToolStripMenuItem("About", Nothing, AddressOf mnuAbout_Click)

        mnuHelp.DropDownItems.Add(mnuAbout)

        ' Add menus to menu strip
        mnuMain.Items.Add(mnuFile)
        mnuMain.Items.Add(mnuSettings)
        mnuMain.Items.Add(mnuHelp)

        ' Add menu strip to form
        MyBase.Controls.Add(mnuMain)

        ' Update recent projects menu
        UpdateRecentProjectsMenu()
    End Sub

    Private Sub MainForm_Load(sender As Object, e As EventArgs)
        LogMessage("Application started at 2025-06-20 04:26:24")
        UpdateStatusBar("Checking Arduino CLI configuration...")
        CheckArduinoCLI()
        RefreshPortList()
        UpdateStatusBar("Ready")
    End Sub

    Private Sub PopulateBoardComboBox()
        ' Clear existing items
        cmbBoardType.Items.Clear()

        ' Add KC-Link boards
        cmbBoardType.Items.Add("KC-Link PRO A8 (Default)")
        cmbBoardType.Items.Add("KC-Link PRO A8 (Minimal)")
        cmbBoardType.Items.Add("KC-Link PRO A8 (OTA)")

        ' Add standard ESP32 boards
        cmbBoardType.Items.Add("ESP32 Dev Module")
        cmbBoardType.Items.Add("ESP32 Wrover Kit")
        cmbBoardType.Items.Add("ESP32 Pico Kit")
        cmbBoardType.Items.Add("ESP32-S2 Dev Module")
        cmbBoardType.Items.Add("ESP32-S3 Dev Module")
        cmbBoardType.Items.Add("ESP32-C3 Dev Module")

        ' Add any custom boards from config
        For Each boardName In boardConfigManager.GetBoardNames()
            If Not cmbBoardType.Items.Contains(boardName) Then
                cmbBoardType.Items.Add(boardName)
            End If
        Next

        ' Select first item
        If cmbBoardType.Items.Count > 0 Then
            cmbBoardType.SelectedIndex = 0
        End If
    End Sub

    Private Sub PopulatePartitionSchemes()
        cmbPartitionOption.Items.Clear()

        ' Add standard partition schemes
        cmbPartitionOption.Items.Add("default")
        cmbPartitionOption.Items.Add("min_spiffs")
        cmbPartitionOption.Items.Add("min_ota")
        cmbPartitionOption.Items.Add("huge_app")
        cmbPartitionOption.Items.Add("custom")

        ' Add any custom partition schemes
        For Each partitionScheme In boardConfigManager.GetCustomPartitions()
            If Not cmbPartitionOption.Items.Contains(partitionScheme) Then
                cmbPartitionOption.Items.Add(partitionScheme)
            End If
        Next

        ' Set default selection
        cmbPartitionOption.SelectedIndex = 0
    End Sub

    Private Sub SetDefaults()
        txtCompilerOutput.ReadOnly = True
        buildProgressBar.Minimum = 0
        buildProgressBar.Maximum = 100
        buildProgressBar.Value = 0

        ' Default Arduino CLI location
        If String.IsNullOrEmpty(My.Settings.ArduinoCliPath) Then
            My.Settings.ArduinoCliPath = Path.Combine(Application.StartupPath, "C:\Users\gen_rms_testroom\Documents\Arduino\Arduino CLI\arduino-cli.exe")
            My.Settings.Save()
        End If
    End Sub

    Private Sub CheckArduinoCLI()
        If Not File.Exists(My.Settings.ArduinoCliPath) Then
            LogMessage("Arduino CLI not found at: " & My.Settings.ArduinoCliPath)
            MessageBox.Show("Arduino CLI not found. Please download and install Arduino CLI, then configure its path in the settings.",
                            "Arduino CLI Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Else
            LogMessage("Arduino CLI found at: " & My.Settings.ArduinoCliPath)
            ' Check Arduino CLI version
            Dim process As New Process()
            process.StartInfo.FileName = My.Settings.ArduinoCliPath
            process.StartInfo.Arguments = "version"
            process.StartInfo.UseShellExecute = False
            process.StartInfo.RedirectStandardOutput = True
            process.StartInfo.CreateNoWindow = True

            Try
                process.Start()
                Dim output As String = process.StandardOutput.ReadToEnd()
                process.WaitForExit()
                LogMessage("Arduino CLI version: " & output.Trim())
                UpdateStatusBar("Arduino CLI version: " & output.Trim())
            Catch ex As Exception
                LogMessage("Error checking Arduino CLI version: " & ex.Message)
                UpdateStatusBar("Error checking Arduino CLI")
            End Try
        End If
    End Sub

    Private Sub RefreshPortList()
        cmbSerialPort.Items.Clear()

        ' Get all available COM ports
        For Each port As String In My.Computer.Ports.SerialPortNames
            cmbSerialPort.Items.Add(port)
        Next

        If cmbSerialPort.Items.Count > 0 Then
            cmbSerialPort.SelectedIndex = 0
            UpdateStatusBar($"Found {cmbSerialPort.Items.Count} serial ports")
        Else
            UpdateStatusBar("No serial ports found")
        End If
    End Sub

    ' Event handlers for button clicks and UI interactions
    Private Sub btnBrowseProject_Click(sender As Object, e As EventArgs)
        Using folderDialog As New FolderBrowserDialog()
            folderDialog.Description = "Select Arduino Sketch Folder"

            ' Set initial directory if available
            If Not String.IsNullOrEmpty(My.Settings.DefaultSketchDir) AndAlso Directory.Exists(My.Settings.DefaultSketchDir) Then
                folderDialog.SelectedPath = My.Settings.DefaultSketchDir
            End If

            If folderDialog.ShowDialog() = DialogResult.OK Then
                projectPath = folderDialog.SelectedPath
                txtProjectPath.Text = projectPath

                ' Check if this is a valid Arduino project
                Dim ino As String = Path.Combine(projectPath, Path.GetFileName(projectPath) & ".ino")
                If Not File.Exists(ino) Then
                    ' Try to find any .ino file
                    Dim inoFiles As String() = Directory.GetFiles(projectPath, "*.ino")
                    If inoFiles.Length > 0 Then
                        LogMessage("Found sketch: " & Path.GetFileName(inoFiles(0)))
                        UpdateStatusBar("Found sketch: " & Path.GetFileName(inoFiles(0)))
                    Else
                        LogMessage("Warning: No .ino sketch file found in selected folder")
                        UpdateStatusBar("Warning: No .ino sketch file found")
                        MessageBox.Show("The selected folder does not appear to be a valid Arduino sketch folder. " &
                                        "Please select a folder containing an .ino file.",
                                        "Invalid Sketch Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                Else
                    LogMessage("Project selected: " & Path.GetFileName(projectPath))
                    UpdateStatusBar("Project loaded: " & Path.GetFileName(projectPath))
                End If

                ' Update the UI with project information
                UpdateProjectInfo()

                ' Save to recent projects
                SaveRecentProject(projectPath)
            End If
        End Using
    End Sub

    Private Sub btnConfigPath_Click(sender As Object, e As EventArgs)
        ' Open board configuration dialog
        If cmbBoardType.SelectedItem Is Nothing Then
            MessageBox.Show("Please select a board type first.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Use the instance variable boardConfigManager that's already defined in MainForm class
        Dim configDialog As New BoardConfigDialog(boardConfigManager, cmbBoardType.SelectedItem.ToString())

        If configDialog.ShowDialog() = DialogResult.OK Then
            LogMessage($"[2025-06-20 04:26:24] Board configuration updated by Chamil1983")
            UpdateStatusBar("Board configuration updated")

            ' Refresh board list to show any changes
            Dim currentBoard = If(cmbBoardType.SelectedItem IsNot Nothing, cmbBoardType.SelectedItem.ToString(), "")
            RefreshBoardList()

            ' Reselect the previously selected board if it still exists
            If Not String.IsNullOrEmpty(currentBoard) Then
                Dim index = cmbBoardType.Items.IndexOf(currentBoard)
                If index >= 0 Then
                    cmbBoardType.SelectedIndex = index
                End If
            End If

            ' Update partition schemes based on the selected board
            RefreshPartitionSchemes()

            ' Save the updated configuration to settings
            SaveBoardSettings()
        End If
    End Sub

    Private Sub ApplyBoardConfiguration()
        ' Ensure the current board configuration is saved and applied
        If cmbBoardType.SelectedItem IsNot Nothing Then
            Dim selectedBoard = cmbBoardType.SelectedItem.ToString()
            Dim selectedPartition = If(cmbPartitionOption.SelectedItem IsNot Nothing, cmbPartitionOption.SelectedItem.ToString(), "default")

            ' Save current board and partition settings
            My.Settings.LastUsedBoard = selectedBoard
            My.Settings.LastUsedPartition = selectedPartition
            My.Settings.Save()

            LogMessage($"[2025-06-20 04:26:24] Applied configuration for {selectedBoard} with partition {selectedPartition} by Chamil1983")
        End If
    End Sub

    Private Sub RefreshBoardList()
        ' Store the currently selected item
        Dim currentSelection = If(cmbBoardType.SelectedItem IsNot Nothing, cmbBoardType.SelectedItem.ToString(), "")

        ' Clear and repopulate the board list
        cmbBoardType.Items.Clear()

        ' Add KC-Link boards
        cmbBoardType.Items.Add("KC-Link PRO A8 (Default)")
        cmbBoardType.Items.Add("KC-Link PRO A8 (Minimal)")
        cmbBoardType.Items.Add("KC-Link PRO A8 (OTA)")

        ' Add standard ESP32 boards
        cmbBoardType.Items.Add("ESP32 Dev Module")
        cmbBoardType.Items.Add("ESP32 Wrover Kit")
        cmbBoardType.Items.Add("ESP32 Pico Kit")
        cmbBoardType.Items.Add("ESP32-S2 Dev Module")
        cmbBoardType.Items.Add("ESP32-S3 Dev Module")
        cmbBoardType.Items.Add("ESP32-C3 Dev Module")

        ' Add any custom boards found
        For Each boardName In boardConfigManager.GetBoardNames()
            If Not cmbBoardType.Items.Contains(boardName) Then
                cmbBoardType.Items.Add(boardName)
            End If
        Next

        ' Try to restore the previous selection
        Dim index = cmbBoardType.Items.IndexOf(currentSelection)
        If index >= 0 Then
            cmbBoardType.SelectedIndex = index
        Else
            cmbBoardType.SelectedIndex = 0
        End If
    End Sub

    Private Sub RefreshPartitionSchemes()
        ' Store the currently selected item
        Dim currentSelection = If(cmbPartitionOption.SelectedItem IsNot Nothing, cmbPartitionOption.SelectedItem.ToString(), "")

        ' Clear and repopulate the partition schemes
        cmbPartitionOption.Items.Clear()

        ' Add standard partition schemes
        cmbPartitionOption.Items.Add("default")
        cmbPartitionOption.Items.Add("min_spiffs")
        cmbPartitionOption.Items.Add("min_ota")
        cmbPartitionOption.Items.Add("huge_app")
        cmbPartitionOption.Items.Add("custom")

        ' Add any custom partition schemes found
        For Each partitionScheme In boardConfigManager.GetCustomPartitions()
            If Not cmbPartitionOption.Items.Contains(partitionScheme) Then
                cmbPartitionOption.Items.Add(partitionScheme)
            End If
        Next

        ' Try to restore the previous selection
        Dim index = cmbPartitionOption.Items.IndexOf(currentSelection)
        If index >= 0 Then
            cmbPartitionOption.SelectedIndex = index
        Else
            cmbPartitionOption.SelectedIndex = 0
        End If
    End Sub

    Private Sub RefreshProgressBarColors()
        ' Force redraw of progress bars with current values
        flashUsageBar.Invalidate()
        ramUsageBar.Invalidate()

        ' Log the color update
        LogMessage($"[2025-06-20 23:28:55] UI updated by Chamil1983")
    End Sub

    Private Sub cmbBoardType_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Update the partition scheme based on the selected board
        Dim selectedBoard = cmbBoardType.SelectedItem.ToString()
        Dim defaultPartition = boardConfigManager.GetDefaultPartitionForBoard(selectedBoard)

        If Not String.IsNullOrEmpty(defaultPartition) Then
            Dim index = cmbPartitionOption.Items.IndexOf(defaultPartition)
            If index >= 0 Then
                cmbPartitionOption.SelectedIndex = index
            End If
        End If

        LogMessage($"[2025-06-20 04:26:24] Selected board: {selectedBoard}, default partition: {defaultPartition}")
        UpdateStatusBar($"Selected board: {selectedBoard}")
    End Sub

    Private Sub cmbPartitionOption_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim selectedPartition = cmbPartitionOption.SelectedItem.ToString()
        LogMessage($"[2025-06-20 04:26:24] Selected partition scheme: {selectedPartition}")

        ' If "custom" is selected, prompt for custom partition file
        If selectedPartition = "custom" Then
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Title = "Select Custom Partition CSV File"
                openFileDialog.Filter = "Partition Files|*.csv|All Files|*.*"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    ' Set custom partition file
                    boardConfigManager.SetCustomPartitionFile(openFileDialog.FileName)
                    LogMessage("[2025-06-20 04:26:24] Custom partition file selected: " & Path.GetFileName(openFileDialog.FileName))
                    UpdateStatusBar("Custom partition file loaded")
                Else
                    ' User cancelled, select default
                    cmbPartitionOption.SelectedIndex = 0
                End If
            End Using
        End If
    End Sub

    Private Sub btnCompile_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectPath) Then
            MessageBox.Show("Please select an Arduino project folder first.",
                           "No Project Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If isCompiling Then
            ' Cancel compilation
            If workerThread.IsBusy Then
                workerThread.CancelAsync()
            End If
            btnCompile.Text = "Compile"
            btnCompile.BackColor = Color.LightBlue
            isCompiling = False
            StopCompileTimer()

            UpdateStatusBar("Compilation cancelled")
            lblStatusIndicator.Text = "Cancelled"
            lblStatusIndicator.ForeColor = Color.Orange
        Else
            ' Apply current board configuration
            ApplyBoardConfiguration()

            ' Save current board settings
            SaveBoardSettings()

            ' Start compilation
            compilationOutput = String.Empty
            compileOutputLines.Clear()
            txtCompilerOutput.Clear()
            buildProgressBar.Value = 0
            toolStripProgressBar.Value = 0
            toolStripProgressBar.Visible = True

            btnCompile.Text = "Cancel"
            btnCompile.BackColor = Color.IndianRed
            isCompiling = True
            compilationStartTime = DateTime.Now

            ' Update status
            UpdateStatusBar("Compiling...")
            lblStatusIndicator.Text = "Compiling..."
            lblStatusIndicator.ForeColor = Color.Blue

            ' Start compilation timer
            StartCompileTimer()

            ' Start compilation in background
            workerThread.RunWorkerAsync()
        End If
    End Sub

    Private Sub btnUpload_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectPath) Then
            MessageBox.Show("Please select an Arduino project folder first.",
                           "No Project Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If cmbSerialPort.SelectedItem Is Nothing Then
            MessageBox.Show("Please select a serial port for uploading.",
                           "No Serial Port Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If isUploading Then
            ' Cancel upload
            If workerThread.IsBusy Then
                workerThread.CancelAsync()
            End If
            btnUpload.Text = "Upload"
            btnUpload.BackColor = Color.LightGreen
            isUploading = False
            StopCompileTimer()

            UpdateStatusBar("Upload cancelled")
            lblStatusIndicator.Text = "Cancelled"
            lblStatusIndicator.ForeColor = Color.Orange
        Else
            ' Apply current board configuration
            ApplyBoardConfiguration()

            ' Save current board settings
            SaveBoardSettings()

            ' Start upload
            compilationOutput = String.Empty
            compileOutputLines.Clear()
            txtCompilerOutput.Clear()
            buildProgressBar.Value = 0
            toolStripProgressBar.Value = 0
            toolStripProgressBar.Visible = True

            btnUpload.Text = "Cancel"
            btnUpload.BackColor = Color.IndianRed
            isUploading = True
            compilationStartTime = DateTime.Now

            ' Update status with detailed port information
            UpdateStatusBar($"Uploading to {cmbSerialPort.SelectedItem.ToString()} using {cmbBoardType.SelectedItem.ToString()}...")
            lblStatusIndicator.Text = "Uploading..."
            lblStatusIndicator.ForeColor = Color.Blue

            ' Start compilation timer
            StartCompileTimer()

            ' Start upload in background
            workerThread.RunWorkerAsync("upload")

            LogMessage($"[2025-06-20 04:26:24] Started upload to {cmbSerialPort.SelectedItem.ToString()} by Chamil1983")
        End If
    End Sub

    Private Sub SaveBoardSettings()
        ' Save current board and partition settings
        If cmbBoardType.SelectedItem IsNot Nothing Then
            My.Settings.LastUsedBoard = cmbBoardType.SelectedItem.ToString()
        End If

        If cmbPartitionOption.SelectedItem IsNot Nothing Then
            My.Settings.LastUsedPartition = cmbPartitionOption.SelectedItem.ToString()
        End If

        ' Save boards.txt path
        My.Settings.BoardsFilePath = boardConfigManager.BoardsFilePath

        ' Save recently used project
        If Not String.IsNullOrEmpty(projectPath) Then
            SaveRecentProject(projectPath)
        End If

        My.Settings.Save()

        LogMessage($"[2025-06-20 04:26:24] Board settings saved by Chamil1983")
    End Sub

    Private Sub SaveRecentProject(path As String)
        ' Save a project path to recent projects
        If My.Settings.RecentProjects Is Nothing Then
            My.Settings.RecentProjects = New Collections.Specialized.StringCollection()
        End If

        ' Add project to recent projects list (avoid duplicates)
        If Not My.Settings.RecentProjects.Contains(path) Then
            My.Settings.RecentProjects.Add(path)

            ' Keep only the latest 10 projects
            While My.Settings.RecentProjects.Count > 10
                My.Settings.RecentProjects.RemoveAt(0)
            End While

            My.Settings.Save()

            ' Update recent projects menu
            UpdateRecentProjectsMenu()
        End If
    End Sub

    Private Sub StartCompileTimer()
        outputRefreshTimer.Enabled = True
    End Sub

    Private Sub StopCompileTimer()
        outputRefreshTimer.Enabled = False
    End Sub

    Private Sub outputRefreshTimer_Tick(sender As Object, e As EventArgs) Handles outputRefreshTimer.Tick
        ' Calculate elapsed time
        Dim elapsed = DateTime.Now - compilationStartTime
        Dim elapsedStr = $"{Math.Floor(elapsed.TotalMinutes):00}:{elapsed.Seconds:00}"

        ' Update status with phase information
        If isCompiling Then
            UpdateStatusBar($"Compiling... {compilationPhase} ({elapsedStr})")
        ElseIf isUploading Then
            UpdateStatusBar($"Uploading... {compilationPhase} ({elapsedStr})")
        End If

        ' Animate progress bar in marquee style if no real progress is being reported
        Try
            If buildProgressBar.Value = 0 AndAlso toolStripProgressBar.GetCurrentParent() IsNot Nothing Then
                If toolStripProgressBar.GetCurrentParent().InvokeRequired Then
                    toolStripProgressBar.GetCurrentParent().Invoke(
                        Sub() toolStripProgressBar.Style = ProgressBarStyle.Marquee)
                Else
                    toolStripProgressBar.Style = ProgressBarStyle.Marquee
                End If
            End If

            ' Pulse the progress bar slightly if no updates for a while
            If (DateTime.Now - lastProgressUpdate).TotalSeconds > 2 AndAlso buildProgressBar.Value < 95 Then
                Dim currentVal = buildProgressBar.Value
                Dim newVal = Math.Min(currentVal + 1, 95)  ' Never go past 95% automatically

                If buildProgressBar.InvokeRequired Then
                    buildProgressBar.Invoke(Sub() buildProgressBar.Value = newVal)
                Else
                    buildProgressBar.Value = newVal
                End If
            End If
        Catch ex As Exception
            ' Ignore errors pulsing progress bar
            Debug.WriteLine($"[2025-06-20 04:26:24] Error pulsing progress bar: {ex.Message}")
        End Try
    End Sub

    Private Sub workerThread_DoWork(sender As Object, e As DoWorkEventArgs) Handles workerThread.DoWork
        Dim worker As BackgroundWorker = DirectCast(sender, BackgroundWorker)
        Dim isUpload As Boolean = (e.Argument IsNot Nothing AndAlso e.Argument.ToString() = "upload")

        ' Reset tracking variables
        compileOutputLines.Clear()
        compilationPhase = "Preparing"
        currentStep = 0
        builderExitCode = -1  ' Initialize with error code

        ' Get the board and port info from the UI thread and store it for use in the background thread
        Dim selectedBoard As String = String.Empty
        Dim selectedPort As String = String.Empty
        Dim selectedPartition As String = String.Empty

        ' Safely get values from UI controls using Invoke if needed
        Me.Invoke(Sub()
                      selectedBoard = If(cmbBoardType.SelectedItem IsNot Nothing, cmbBoardType.SelectedItem.ToString(), "KC-Link PRO A8 (Default)")
                      selectedPartition = If(cmbPartitionOption.SelectedItem IsNot Nothing, cmbPartitionOption.SelectedItem.ToString(), "default")

                      If isUpload Then
                          selectedPort = If(cmbSerialPort.SelectedItem IsNot Nothing, cmbSerialPort.SelectedItem.ToString(), "")
                      End If
                  End Sub)

        ' Create compilation/upload command
        Dim fqbn As String = boardConfigManager.GetFQBN(selectedBoard)

        ' Apply partition scheme if not default
        If selectedPartition <> "default" AndAlso selectedPartition <> "custom" Then
            fqbn = boardConfigManager.ApplyPartitionScheme(fqbn, selectedPartition)
        ElseIf selectedPartition = "custom" Then
            fqbn = boardConfigManager.ApplyCustomPartitionFile(fqbn)
        End If

        ' Add verbose output for better progress tracking
        Dim arguments As String

        If isUpload Then
            arguments = $"compile -v --upload --port {selectedPort} --fqbn {fqbn} ""{projectPath}"""
        Else
            arguments = $"compile -v --fqbn {fqbn} ""{projectPath}"""
        End If

        ' Start Arduino CLI process
        Using process As New Process()
            process.StartInfo.FileName = My.Settings.ArduinoCliPath
            process.StartInfo.Arguments = arguments
            process.StartInfo.UseShellExecute = False
            process.StartInfo.RedirectStandardOutput = True
            process.StartInfo.RedirectStandardError = True
            process.StartInfo.CreateNoWindow = True

            ' Event handlers for output
            AddHandler process.OutputDataReceived, AddressOf OnProcessOutputDataReceived
            AddHandler process.ErrorDataReceived, AddressOf OnProcessErrorDataReceived

            worker.ReportProgress(1, "Starting " & If(isUpload, "upload", "compilation") & "...")
            worker.ReportProgress(1, $"Board: {selectedBoard}")
            worker.ReportProgress(1, $"FQBN: {fqbn}")
            If isUpload Then
                worker.ReportProgress(1, $"Port: {selectedPort}")
            End If
            worker.ReportProgress(1, $"Partition: {selectedPartition}")
            worker.ReportProgress(1, $"Command: arduino-cli {arguments}")

            Try
                process.Start()
                process.BeginOutputReadLine()
                process.BeginErrorReadLine()

                Dim startTime As DateTime = DateTime.Now
                Dim lastProgressPercentage As Integer = 0

                ' Wait for process to exit or cancellation
                While Not process.HasExited
                    If worker.CancellationPending Then
                        process.Kill()
                        e.Cancel = True
                        worker.ReportProgress(0, "Process cancelled by user")
                        Return
                    End If

                    ' Update progress every 500ms even if no output
                    If (DateTime.Now - lastProgressUpdate).TotalMilliseconds > 500 Then
                        Dim estimatedProgress As Integer = EstimateProgressFromOutput()
                        If estimatedProgress > lastProgressPercentage Then
                            worker.ReportProgress(estimatedProgress, $"Progress: {compilationPhase} - {estimatedProgress}%")
                            lastProgressPercentage = estimatedProgress
                        End If
                        lastProgressUpdate = DateTime.Now
                    End If

                    Threading.Thread.Sleep(100)
                End While

                Dim endTime As DateTime = DateTime.Now
                Dim duration As TimeSpan = endTime - startTime

                ' Process completed
                If process.ExitCode = 0 Then
                    worker.ReportProgress(100, If(isUpload, "Upload completed successfully!", "Compilation completed successfully!"))
                    worker.ReportProgress(100, $"Completed in {duration.TotalSeconds:F1} seconds")

                    ' Update statistics
                    hardwareStats.AddCompilation(Path.GetFileName(projectPath), process.ExitCode = 0, duration)

                    ' Parse output for binary size information
                    If Not isUpload Then
                        ParseCompilationOutput(compilationOutput)
                    End If
                Else
                    worker.ReportProgress(0, If(isUpload, "Upload failed with errors", "Compilation failed with errors"))
                    worker.ReportProgress(0, $"Process exited with code: {process.ExitCode}")

                    ' Update statistics for failed compilation
                    hardwareStats.AddCompilation(Path.GetFileName(projectPath), False, duration)
                End If
            Catch ex As Exception
                worker.ReportProgress(0, "Error: " & ex.Message)
                builderExitCode = -1  ' Set error code on exception
            End Try
        End Using
    End Sub

    Private Sub OnProcessOutputDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            ' Add to full output
            compilationOutput &= e.Data & Environment.NewLine

            ' Add to line collection for progress tracking
            compileOutputLines.Add(e.Data)

            ' Report line to UI thread
            workerThread.ReportProgress(0, e.Data)

            ' Save the last output line for display in status bar
            lastOutputLine = e.Data

            ' Update compilation phase and progress
            UpdateCompilationPhase(e.Data)

            ' Extract explicit progress percentage if available
            If e.Data.Contains("%") Then
                Try
                    Dim match As Match = Regex.Match(e.Data, "(\d+)%")
                    If match.Success Then
                        Dim percentage As Integer = Integer.Parse(match.Groups(1).Value)

                        ' Only report meaningful progress changes (avoid frequent small updates)
                        If percentage > 0 AndAlso (DateTime.Now - lastProgressUpdate).TotalMilliseconds > 250 Then
                            workerThread.ReportProgress(percentage)
                            lastProgressUpdate = DateTime.Now
                        End If
                    End If
                Catch
                    ' Ignore parsing errors
                End Try
            End If
        End If
    End Sub

    Private Sub OnProcessErrorDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            compilationOutput &= "ERROR: " & e.Data & Environment.NewLine
            workerThread.ReportProgress(0, "ERROR: " & e.Data)

            ' Save the last output line for display in status bar
            lastOutputLine = "ERROR: " & e.Data

            ' Also update compilation phase for error messages
            UpdateCompilationPhase(e.Data)
        End If
    End Sub

    Private Sub UpdateCompilationPhase(line As String)
        ' Detect compilation phases from output
        Dim lowerLine = line.ToLower()

        ' Determine which phase we're in based on output
        If lowerLine.Contains("verifying") Then
            compilationPhase = "Verifying sketch"
            currentStep = 1
        ElseIf lowerLine.Contains("detecting libraries") Then
            compilationPhase = "Detecting libraries"
            currentStep = 2
        ElseIf lowerLine.Contains("sketch uses") OrElse lowerLine.Contains("compiling") Then
            compilationPhase = "Compiling"
            currentStep = 3
        ElseIf lowerLine.Contains("linking") Then
            compilationPhase = "Linking"
            currentStep = 4
        ElseIf lowerLine.Contains("generating binary") Then
            compilationPhase = "Generating binary"
            currentStep = 5
        ElseIf lowerLine.Contains("flash size") Then
            compilationPhase = "Preparing flash"
            currentStep = 6
        ElseIf lowerLine.Contains("uploading") Then
            compilationPhase = "Uploading"
            currentStep = 7
        ElseIf lowerLine.Contains("writing") OrElse lowerLine.Contains("written") Then
            compilationPhase = "Writing flash"
            currentStep = 8
        ElseIf lowerLine.Contains("verifying flash") Then
            compilationPhase = "Verifying flash"
            currentStep = 9
        ElseIf lowerLine.Contains("hard resetting") Then
            compilationPhase = "Resetting device"
            currentStep = 10
        End If
    End Sub

    Private Function EstimateProgressFromOutput() As Integer
        ' If no steps detected, estimate progress based on output volume
        If currentStep = 0 Then
            ' Just estimate based on how many lines we've received
            ' ESP32 compilation typically produces 50-200 lines of output
            Dim linesEstimate As Integer = Math.Min(compileOutputLines.Count * 100 / 150, 90)
            Return Math.Max(5, linesEstimate)  ' At least 5%, at most 90%
        End If

        ' Return progress based on detected compilation step
        Return Math.Min(currentStep * 100 / expectedSteps, 99)  ' Reserve 100% for completion
    End Function

    Private Sub workerThread_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles workerThread.ProgressChanged
        ' Update progress bar when percentage is significant
        If e.ProgressPercentage > 0 Then
            Dim newValue = Math.Min(e.ProgressPercentage, 100)

            ' Update main progress bar safely
            Try
                If buildProgressBar.InvokeRequired Then
                    buildProgressBar.Invoke(Sub() buildProgressBar.Value = newValue)
                Else
                    buildProgressBar.Value = newValue
                End If

                ' Update status strip progress bar safely
                If toolStripProgressBar.GetCurrentParent() IsNot Nothing Then
                    If toolStripProgressBar.GetCurrentParent().InvokeRequired Then
                        toolStripProgressBar.GetCurrentParent().Invoke(
                            Sub()
                                toolStripProgressBar.Value = newValue
                                toolStripProgressBar.Style = ProgressBarStyle.Blocks
                                toolStripProgressBar.Visible = True
                            End Sub)
                    Else
                        toolStripProgressBar.Value = newValue
                        toolStripProgressBar.Style = ProgressBarStyle.Blocks
                        toolStripProgressBar.Visible = True
                    End If
                End If
            Catch ex As Exception
                ' Ignore errors updating progress UI
                Debug.WriteLine($"[2025-06-20 04:26:24] Error updating progress: {ex.Message}")
            End Try
        End If

        ' Update output text
        If e.UserState IsNot Nothing Then
            AppendToOutput(e.UserState.ToString())

            ' Update status bar with the latest message
            Dim statusText As String = e.UserState.ToString()
            If statusText.Length > 60 Then
                statusText = statusText.Substring(0, 57) & "..."
            End If
            UpdateStatusBar(statusText)
        End If
    End Sub

    Private Sub workerThread_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles workerThread.RunWorkerCompleted
        ' Stop the timer
        StopCompileTimer()

        ' Reset UI state
        btnCompile.Text = "Compile"
        btnCompile.BackColor = Color.LightBlue
        btnUpload.Text = "Upload"
        btnUpload.BackColor = Color.LightGreen
        isCompiling = False
        isUploading = False
        toolStripProgressBar.Visible = False

        If e.Cancelled Then
            AppendToOutput("Operation cancelled by user")
            lblStatusIndicator.Text = "Cancelled"
            lblStatusIndicator.ForeColor = Color.Orange
            UpdateStatusBar("Operation cancelled by user")
        ElseIf e.Error IsNot Nothing Then
            AppendToOutput("Error: " & e.Error.Message)
            lblStatusIndicator.Text = "Error"
            lblStatusIndicator.ForeColor = Color.Red
            UpdateStatusBar("Error: " & e.Error.Message)
        Else
            ' Operation completed - FIX: Properly detect success from return code and output
            Dim success = (builderExitCode = 0) OrElse
                     Not compilationOutput.Contains("error:") OrElse
                     Not compilationOutput.Contains("failed with errors") OrElse
                     compilationOutput.Contains("Compilation completed successfully") OrElse
                     compilationOutput.Contains("Upload completed successfully")

            If success Then
                lblStatusIndicator.Text = "Success"
                lblStatusIndicator.ForeColor = Color.Green
                UpdateStatusBar("Operation completed successfully")
                LogMessage($"[2025-06-20 22:33:14] Compilation/Upload completed successfully by Chamil1983")
            Else
                lblStatusIndicator.Text = "Failed"
                lblStatusIndicator.ForeColor = Color.Red
                UpdateStatusBar("Operation failed")
                LogMessage($"[2025-06-20 22:33:14] Compilation/Upload failed by Chamil1983")
            End If

            ' Update statistics UI
            UpdateStatisticsUI()
        End If
    End Sub

    Private Sub AppendToOutput(text As String)
        If txtCompilerOutput.InvokeRequired Then
            txtCompilerOutput.Invoke(Sub() AppendToOutput(text))
        Else
            txtCompilerOutput.AppendText(text & Environment.NewLine)
            txtCompilerOutput.SelectionStart = txtCompilerOutput.Text.Length
            txtCompilerOutput.ScrollToCaret()
        End If
    End Sub

    Private Sub ParseCompilationOutput(output As String)
        ' Extract binary size statistics
        ' Example: "Sketch uses 345678 bytes (26%) of program storage space."
        Try
            Dim sketchSizeMatch As Match = Regex.Match(output, "Sketch uses (\d+) bytes \((\d+)%\) of program storage space")
            If sketchSizeMatch.Success Then
                Dim sketchSize As Long = Long.Parse(sketchSizeMatch.Groups(1).Value)
                Dim sketchPercentage As Integer = Integer.Parse(sketchSizeMatch.Groups(2).Value)

                ' Update statistics
                hardwareStats.UpdateSketchSize(sketchSize, sketchPercentage)

                ' Log
                LogMessage($"[2025-06-20 04:26:24] Compiled sketch size: {sketchSize} bytes ({sketchPercentage}%)")
            End If

            ' Extract RAM usage if available
            Dim ramMatch As Match = Regex.Match(output, "Global variables use (\d+) bytes \((\d+)%\) of dynamic memory")
            If ramMatch.Success Then
                Dim ramSize As Long = Long.Parse(ramMatch.Groups(1).Value)
                Dim ramPercentage As Integer = Integer.Parse(ramMatch.Groups(2).Value)

                ' Update statistics
                hardwareStats.UpdateRAMUsage(ramSize, ramPercentage)

                ' Log
                LogMessage($"[2025-06-20 20:59:26] RAM usage: {ramSize} bytes ({ramPercentage}%)")
            End If
        Catch ex As Exception
            LogMessage($"[2025-06-20 20:59:26] Error parsing compilation statistics: {ex.Message}")
        End Try
    End Sub

    Private Sub UpdateStatusBar(text As String)
        If toolStripStatusLabel.Owner.InvokeRequired Then
            toolStripStatusLabel.Owner.Invoke(Sub() toolStripStatusLabel.Text = text)
        Else
            toolStripStatusLabel.Text = text
        End If
    End Sub

    Private Sub UpdateStatisticsUI()
        lblFlashUsage.Text = $"Flash: {hardwareStats.SketchSize} bytes ({hardwareStats.SketchSizePercentage}%)"
        lblRAMUsage.Text = $"RAM: {hardwareStats.RAMSize} bytes ({hardwareStats.RAMPercentage}%)"

        ' Update progress bars for flash and RAM usage
        flashUsageBar.Value = Math.Min(hardwareStats.SketchSizePercentage, 100)
        ramUsageBar.Value = Math.Min(hardwareStats.RAMPercentage, 100)

        ' Color logic: Blue (good), Orange (warn), Red (critical)
        If hardwareStats.SketchSizePercentage >= 90 Then
            flashUsageBar.BarColor = Color.Red
        ElseIf hardwareStats.SketchSizePercentage >= 75 Then
            flashUsageBar.BarColor = Color.Orange
        Else
            flashUsageBar.BarColor = Color.RoyalBlue
        End If

        If hardwareStats.RAMPercentage >= 90 Then
            ramUsageBar.BarColor = Color.Red
        ElseIf hardwareStats.RAMPercentage >= 75 Then
            ramUsageBar.BarColor = Color.Orange
        Else
            ramUsageBar.BarColor = Color.MediumPurple
        End If

        flashUsageBar.Invalidate()
        ramUsageBar.Invalidate()
    End Sub

    Private Sub ProgressBar_Paint(sender As Object, e As PaintEventArgs)
        Dim progressBar As ProgressBar = DirectCast(sender, ProgressBar)
        Dim rect As Rectangle = progressBar.ClientRectangle
        Dim g As Graphics = e.Graphics

        ' Calculate the filled portion of the progress bar
        Dim fillWidth As Integer = CInt((rect.Width * progressBar.Value) / progressBar.Maximum)
        Dim fillRect As New Rectangle(rect.X, rect.Y, fillWidth, rect.Height)

        ' Fill with gradient based on percentage
        Dim percentage As Integer = progressBar.Value
        Dim fillColor As Color

        If percentage >= 90 Then
            ' Red for critical
            fillColor = Color.FromArgb(255, 72, 71)
        ElseIf percentage >= 75 Then
            ' Orange/Amber for warning
            fillColor = Color.FromArgb(255, 165, 0)
        Else
            ' Blue for good
            fillColor = Color.FromArgb(0, 122, 204)
        End If

        ' Create gradient brush for better appearance
        Using fillBrush As New LinearGradientBrush(
        fillRect,
        Color.FromArgb(fillColor.R, fillColor.G, fillColor.B, 230), ' Slightly transparent version
        fillColor,
        LinearGradientMode.Vertical)

            ' Fill the progress portion
            If fillWidth > 0 Then
                g.FillRectangle(fillBrush, fillRect)
            End If
        End Using

        ' Draw border around the progress bar for better definition
        Using borderPen As New Pen(Color.DarkGray, 1)
            g.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1)
        End Using

        ' Draw percentage text centered in the progress bar
        Dim textBrush As Brush = Brushes.Black
        Dim text As String = $"{percentage}%"
        Dim textSize As SizeF = g.MeasureString(text, progressBar.Font)
        Dim textPos As New PointF(
        (rect.Width - textSize.Width) / 2,
        (rect.Height - textSize.Height) / 2)

        g.DrawString(text, progressBar.Font, textBrush, textPos)
    End Sub

    Private Sub UpdateProjectInfo()
        If Directory.Exists(projectPath) Then
            ' Find main sketch file
            Dim sketchName As String = Path.GetFileName(projectPath)
            Dim sketchFile As String = Path.Combine(projectPath, sketchName & ".ino")

            If Not File.Exists(sketchFile) Then
                ' Try to find any .ino file
                Dim inoFiles As String() = Directory.GetFiles(projectPath, "*.ino")
                If inoFiles.Length > 0 Then
                    sketchFile = inoFiles(0)
                    sketchName = Path.GetFileNameWithoutExtension(sketchFile)
                End If
            End If

            ' Update UI with project information
            lblProjectName.Text = $"Project: {sketchName}"

            ' Count files
            Dim inoCount As Integer = Directory.GetFiles(projectPath, "*.ino", SearchOption.AllDirectories).Length
            Dim cppCount As Integer = Directory.GetFiles(projectPath, "*.cpp", SearchOption.AllDirectories).Length
            Dim hCount As Integer = Directory.GetFiles(projectPath, "*.h", SearchOption.AllDirectories).Length

            lblFileCount.Text = $"Files: {inoCount} .ino, {cppCount} .cpp, {hCount} .h"

            ' Detect if this is a known example
            Dim isExample As Boolean = False
            For Each exampleName As String In {"home-automation", "datalogger-example", "mqtt-example", "modbus-monitor"}
                If sketchName.ToLower().Contains(exampleName.ToLower()) Then
                    lblProjectType.Text = "Type: KC-Link Example Project"
                    isExample = True
                    Exit For
                End If
            Next

            If Not isExample Then
                lblProjectType.Text = "Type: Custom Project"
            End If

            ' Count lines of code if the file exists
            If File.Exists(sketchFile) Then
                Try
                    Dim lines As Integer = File.ReadAllLines(sketchFile).Length
                    lblCodeLines.Text = $"Main sketch: {lines} lines"
                Catch ex As Exception
                    lblCodeLines.Text = "Could not read file"
                End Try
            End If
        End If
    End Sub

    Private Sub btnSettings_Click(sender As Object, e As EventArgs)
        ' Open settings form
        Dim settingsForm As New EnhancedSettingsForm(boardConfigManager)
        If settingsForm.ShowDialog(Me) = DialogResult.OK Then
            ' Reload settings
            LoadSettings()

            ' Refresh board list and partition schemes
            RefreshBoardList()
            RefreshPartitionSchemes()
        End If
    End Sub

    Private Sub btnMonitor_Click(sender As Object, e As EventArgs)
        ' Open serial monitor
        If cmbSerialPort.SelectedItem IsNot Nothing Then
            serialMonitorTool.OpenMonitor(cmbSerialPort.SelectedItem.ToString())
        Else
            MessageBox.Show("Please select a serial port first.",
                           "No Serial Port Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub btnRefreshPorts_Click(sender As Object, e As EventArgs)
        RefreshPortList()
    End Sub

    Private Sub LoadSettings()
        ' Load user settings
        ' This will be called on startup and after settings are changed

        ' Load Arduino CLI path
        If File.Exists(My.Settings.ArduinoCliPath) Then
            LogMessage($"[2025-06-20 20:59:26] Using Arduino CLI from: {My.Settings.ArduinoCliPath}")
        Else
            LogMessage($"[2025-06-20 20:59:26] Arduino CLI not found at configured path: {My.Settings.ArduinoCliPath}")

            ' Try to find arduino-cli in common locations
            Dim possiblePaths As String() = {
                Path.Combine(Application.StartupPath, "arduino-cli", "arduino-cli.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Arduino", "arduino-cli.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arduino15", "packages", "arduino", "tools", "arduino-cli", "arduino-cli.exe")
            }

            For Each path In possiblePaths
                If File.Exists(path) Then
                    My.Settings.ArduinoCliPath = path
                    My.Settings.Save()
                    LogMessage($"[2025-06-20 20:59:26] Found Arduino CLI at: {path}")
                    Exit For
                End If
            Next
        End If

        ' Load boards.txt path
        If Not String.IsNullOrEmpty(My.Settings.BoardsFilePath) AndAlso File.Exists(My.Settings.BoardsFilePath) Then
            boardConfigManager.BoardsFilePath = My.Settings.BoardsFilePath
            LogMessage($"[2025-06-20 20:59:26] Using boards.txt from: {My.Settings.BoardsFilePath}")
        End If

        ' Set last used board if available
        If Not String.IsNullOrEmpty(My.Settings.LastUsedBoard) Then
            Dim boardIndex = cmbBoardType.Items.IndexOf(My.Settings.LastUsedBoard)
            If boardIndex >= 0 Then
                cmbBoardType.SelectedIndex = boardIndex
            End If
        End If

        ' Set last used partition if available
        If Not String.IsNullOrEmpty(My.Settings.LastUsedPartition) Then
            Dim partitionIndex = cmbPartitionOption.Items.IndexOf(My.Settings.LastUsedPartition)
            If partitionIndex >= 0 Then
                cmbPartitionOption.SelectedIndex = partitionIndex
            End If
        End If

        ' Load last used project if available
        If My.Settings.RecentProjects IsNot Nothing AndAlso My.Settings.RecentProjects.Count > 0 Then
            Dim lastProject = My.Settings.RecentProjects(My.Settings.RecentProjects.Count - 1)
            If Directory.Exists(lastProject) Then
                projectPath = lastProject
                txtProjectPath.Text = projectPath
                UpdateProjectInfo()
            End If
        End If

        ' Update recent projects menu
        UpdateRecentProjectsMenu()
    End Sub

    Private Sub LogMessage(message As String)
        ' Log messages to file if enabled
        If My.Settings.EnableLogging Then
            Try
                Dim logFile As String = Path.Combine(Application.StartupPath, "app.log")
                Using writer As New StreamWriter(logFile, True)
                    writer.WriteLine(message)
                End Using
            Catch ex As Exception
                Debug.WriteLine($"[2025-06-20 20:59:26] Logging error: {ex.Message}")
            End Try
        End If

        ' Always log to debug output
        Debug.WriteLine(message)
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs)
        ' Clean up resources
        If serialMonitorTool IsNot Nothing AndAlso serialMonitorTool.IsOpen Then
            serialMonitorTool.CloseMonitor()
        End If

        ' Save any unsaved settings
        SaveBoardSettings()

        ' Log application exit
        LogMessage($"[2025-06-20 20:59:26] Application exited by Chamil1983")
    End Sub

    ' Menu event handlers
    Private Sub mnuOpenProject_Click(sender As Object, e As EventArgs)
        btnBrowseProject_Click(sender, e)
    End Sub

    Private Sub mnuExit_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub mnuBoardConfig_Click(sender As Object, e As EventArgs)
        btnConfigPath_Click(sender, e)
    End Sub

    Private Sub mnuLoadBoardsFile_Click(sender As Object, e As EventArgs)
        Using openFileDialog As New OpenFileDialog()
            openFileDialog.Title = "Select boards.txt File"
            openFileDialog.Filter = "boards.txt|boards.txt|Text Files|*.txt|All Files|*.*"
            openFileDialog.FileName = "boards.txt"

            If openFileDialog.ShowDialog() = DialogResult.OK Then
                boardConfigManager.BoardsFilePath = openFileDialog.FileName

                ' Log the change
                LogMessage($"[2025-06-20 20:59:26] boards.txt path updated to {openFileDialog.FileName} by Chamil1983")

                ' Ask if user wants to reload configurations
                If MessageBox.Show("Would you like to reload board configurations from the selected file?",
                                "Reload Configurations", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    boardConfigManager.LoadBoardConfigurations()

                    ' Refresh board list
                    RefreshBoardList()

                    ' Refresh partition schemes
                    RefreshPartitionSchemes()

                    ' Save the setting
                    My.Settings.BoardsFilePath = openFileDialog.FileName
                    My.Settings.Save()

                    MessageBox.Show("Board configurations reloaded successfully.",
                                "Reload Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End If
        End Using
    End Sub

    Private Sub mnuBoardSettings_Click(sender As Object, e As EventArgs)
        btnSettings_Click(sender, e)
    End Sub

    Private Sub mnuAbout_Click(sender As Object, e As EventArgs)
        Dim aboutMessage As String = "KC-Link Loader v1.1" + Environment.NewLine + Environment.NewLine +
                                 "A utility for compiling and uploading code to ESP32 boards." + Environment.NewLine + Environment.NewLine +
                                 "Copyright © 2025 Chamil1983" + Environment.NewLine +
                                 "Last updated: 2025-06-20"

        MessageBox.Show(aboutMessage, "About KC-Link Loader", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub UpdateRecentProjectsMenu()
        ' Initialize mnuRecentProjects if needed first
        If mnuRecentProjects Is Nothing Then
            mnuRecentProjects = New ToolStripMenuItem("Recent Projects")
            mnuFile.DropDownItems.Insert(1, mnuRecentProjects)
        End If

        ' Clear existing items
        mnuRecentProjects.DropDownItems.Clear()

        ' Add recent projects if available
        If My.Settings.RecentProjects IsNot Nothing AndAlso My.Settings.RecentProjects.Count > 0 Then
            For i As Integer = My.Settings.RecentProjects.Count - 1 To 0 Step -1
                Dim projectPath = My.Settings.RecentProjects(i)
                If Directory.Exists(projectPath) Then
                    Dim projectName = Path.GetFileName(projectPath)
                    Dim menuItem = New ToolStripMenuItem(projectName, Nothing, AddressOf RecentProject_Click)
                    menuItem.Tag = projectPath
                    mnuRecentProjects.DropDownItems.Add(menuItem)
                End If
            Next

            If mnuRecentProjects.DropDownItems.Count > 0 Then
                mnuRecentProjects.DropDownItems.Add(New ToolStripSeparator())
                mnuRecentProjects.DropDownItems.Add(New ToolStripMenuItem("Clear Recent Projects", Nothing, AddressOf ClearRecentProjects_Click))
            End If
        End If

        ' Enable/disable the menu based on item count
        mnuRecentProjects.Enabled = (mnuRecentProjects.DropDownItems.Count > 0)
    End Sub

    Private Sub RecentProject_Click(sender As Object, e As EventArgs)
        Dim menuItem = DirectCast(sender, ToolStripMenuItem)
        Dim path = menuItem.Tag.ToString()

        If Directory.Exists(path) Then
            projectPath = path
            txtProjectPath.Text = projectPath
            UpdateProjectInfo()
            LogMessage($"[2025-06-20 20:59:26] Opened recent project: {path}")
        Else
            MessageBox.Show("Project directory no longer exists.", "Project Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)

            ' Remove from recent projects
            If My.Settings.RecentProjects IsNot Nothing Then
                My.Settings.RecentProjects.Remove(path)
                My.Settings.Save()
                UpdateRecentProjectsMenu()
            End If
        End If
    End Sub

    Private Sub ClearRecentProjects_Click(sender As Object, e As EventArgs)
        If My.Settings.RecentProjects IsNot Nothing Then
            My.Settings.RecentProjects.Clear()
            My.Settings.Save()
            UpdateRecentProjectsMenu()
            LogMessage($"[2025-06-20 20:59:26] Recent projects list cleared by Chamil1983")
        End If
    End Sub
End Class