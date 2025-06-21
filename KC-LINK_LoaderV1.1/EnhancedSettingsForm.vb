Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Collections.Generic

Public Class EnhancedSettingsForm
    Inherits Form

    ' Form controls
    Private txtArduinoCliPath As TextBox
    Private btnBrowseCli As Button
    Private chkEnableLogging As CheckBox
    Private chkVerboseOutput As CheckBox
    Private txtDefaultSketchDir As TextBox
    Private btnBrowseSketchDir As Button
    Private txtHardwarePath As TextBox
    Private btnBrowseHardware As Button
    Private numCompileTimeout As NumericUpDown
    Private cmbDefaultBoard As ComboBox
    Private cmbDefaultPartition As ComboBox
    Private txtBoardConfig As TextBox
    Private btnSaveBoardConfig As Button
    Private btnOK As Button
    Private btnCancel As Button
    Private tabControl As TabControl

    ' Reference to board manager
    Private boardManager As BoardManager

    ' Constructor
    Public Sub New(boardMgr As BoardManager)
        MyBase.New()

        ' Store reference to board manager
        boardManager = boardMgr

        InitializeComponent()
        LoadSettings()
    End Sub

    ' Initialize form components
    Private Sub InitializeComponent()
        ' Form setup
        Me.Text = "KC-Link Loader Settings"
        Me.ClientSize = New Size(600, 450)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Segoe UI", 9)

        ' Create tab control
        tabControl = New TabControl()
        tabControl.Dock = DockStyle.Fill
        tabControl.Padding = New Point(12, 4)

        ' Create tabs
        Dim tabGeneral As New TabPage("General")
        Dim tabBoards As New TabPage("Board Configurations")
        Dim tabAdvanced As New TabPage("Advanced")

        ' General settings tab
        Dim generalPanel As New TableLayoutPanel()
        generalPanel.Dock = DockStyle.Fill
        generalPanel.Padding = New Padding(10)
        generalPanel.ColumnCount = 3
        generalPanel.RowCount = 4
        generalPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        generalPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        generalPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
        generalPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        generalPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        generalPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        generalPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

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

        generalPanel.Controls.Add(lblArduinoCliPath, 0, 0)
        generalPanel.Controls.Add(txtArduinoCliPath, 1, 0)
        generalPanel.Controls.Add(btnBrowseCli, 2, 0)

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

        generalPanel.Controls.Add(lblSketchDir, 0, 1)
        generalPanel.Controls.Add(txtDefaultSketchDir, 1, 1)
        generalPanel.Controls.Add(btnBrowseSketchDir, 2, 1)

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

        generalPanel.Controls.Add(lblHardwarePath, 0, 2)
        generalPanel.Controls.Add(txtHardwarePath, 1, 2)
        generalPanel.Controls.Add(btnBrowseHardware, 2, 2)

        ' Checkboxes for options
        Dim optionsPanel As New FlowLayoutPanel()
        optionsPanel.FlowDirection = FlowDirection.LeftToRight
        optionsPanel.WrapContents = True
        optionsPanel.AutoSize = True
        optionsPanel.Dock = DockStyle.Fill

        chkEnableLogging = New CheckBox()
        chkEnableLogging.Text = "Enable Logging"
        chkEnableLogging.AutoSize = True
        chkEnableLogging.Margin = New Padding(5)

        chkVerboseOutput = New CheckBox()
        chkVerboseOutput.Text = "Verbose Compile Output"
        chkVerboseOutput.AutoSize = True
        chkVerboseOutput.Margin = New Padding(15, 5, 5, 5)

        optionsPanel.Controls.Add(chkEnableLogging)
        optionsPanel.Controls.Add(chkVerboseOutput)

        generalPanel.Controls.Add(optionsPanel, 1, 3)

        ' Board configurations tab
        Dim boardsPanel As New TableLayoutPanel()
        boardsPanel.Dock = DockStyle.Fill
        boardsPanel.Padding = New Padding(10)
        boardsPanel.ColumnCount = 3
        boardsPanel.RowCount = 5
        boardsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        boardsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        boardsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
        boardsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        boardsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        boardsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        boardsPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        boardsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        ' Default board selection
        Dim lblDefaultBoard As New Label()
        lblDefaultBoard.Text = "Default Board:"
        lblDefaultBoard.TextAlign = ContentAlignment.MiddleRight
        lblDefaultBoard.Dock = DockStyle.Fill

        cmbDefaultBoard = New ComboBox()
        cmbDefaultBoard.DropDownStyle = ComboBoxStyle.DropDownList
        cmbDefaultBoard.Dock = DockStyle.Fill

        boardsPanel.Controls.Add(lblDefaultBoard, 0, 0)
        boardsPanel.Controls.Add(cmbDefaultBoard, 1, 0)
        boardsPanel.SetColumnSpan(cmbDefaultBoard, 2)

        ' Default partition scheme
        Dim lblDefaultPartition As New Label()
        lblDefaultPartition.Text = "Default Partition:"
        lblDefaultPartition.TextAlign = ContentAlignment.MiddleRight
        lblDefaultPartition.Dock = DockStyle.Fill

        cmbDefaultPartition = New ComboBox()
        cmbDefaultPartition.DropDownStyle = ComboBoxStyle.DropDownList
        cmbDefaultPartition.Dock = DockStyle.Fill

        boardsPanel.Controls.Add(lblDefaultPartition, 0, 1)
        boardsPanel.Controls.Add(cmbDefaultPartition, 1, 1)
        boardsPanel.SetColumnSpan(cmbDefaultPartition, 2)

        ' Board configuration editor
        Dim lblBoardConfig As New Label()
        lblBoardConfig.Text = "Board Configuration:"
        lblBoardConfig.TextAlign = ContentAlignment.MiddleLeft
        lblBoardConfig.Dock = DockStyle.Fill
        lblBoardConfig.Padding = New Padding(0, 10, 0, 5)

        boardsPanel.Controls.Add(lblBoardConfig, 0, 2)
        boardsPanel.SetColumnSpan(lblBoardConfig, 3)

        txtBoardConfig = New TextBox()
        txtBoardConfig.Multiline = True
        txtBoardConfig.ScrollBars = ScrollBars.Vertical
        txtBoardConfig.Dock = DockStyle.Fill
        txtBoardConfig.Font = New Font("Consolas", 9)

        boardsPanel.Controls.Add(txtBoardConfig, 0, 3)
        boardsPanel.SetColumnSpan(txtBoardConfig, 3)

        btnSaveBoardConfig = New Button()
        btnSaveBoardConfig.Text = "Save Configuration"
        btnSaveBoardConfig.AutoSize = True
        btnSaveBoardConfig.Padding = New Padding(10, 3, 10, 3)
        btnSaveBoardConfig.Anchor = AnchorStyles.Right

        boardsPanel.Controls.Add(btnSaveBoardConfig, 2, 4)

        ' Advanced settings tab
        Dim advancedPanel As New TableLayoutPanel()
        advancedPanel.Dock = DockStyle.Fill
        advancedPanel.Padding = New Padding(10)
        advancedPanel.ColumnCount = 3
        advancedPanel.RowCount = 2
        advancedPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        advancedPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        advancedPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        advancedPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        advancedPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

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

        advancedPanel.Controls.Add(lblCompileTimeout, 0, 0)
        advancedPanel.Controls.Add(numCompileTimeout, 1, 0)

        ' Add all panels to tabs
        tabGeneral.Controls.Add(generalPanel)
        tabBoards.Controls.Add(boardsPanel)
        tabAdvanced.Controls.Add(advancedPanel)

        ' Add tabs to tab control
        tabControl.TabPages.Add(tabGeneral)
        tabControl.TabPages.Add(tabBoards)
        tabControl.TabPages.Add(tabAdvanced)

        ' Button panel
        Dim buttonPanel As New FlowLayoutPanel()
        buttonPanel.Dock = DockStyle.Bottom
        buttonPanel.FlowDirection = FlowDirection.RightToLeft
        buttonPanel.WrapContents = False
        buttonPanel.AutoSize = True
        buttonPanel.Padding = New Padding(10)

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

        ' Add controls to form
        Me.Controls.Add(tabControl)
        Me.Controls.Add(buttonPanel)

        ' Wire up events
        AddHandler btnBrowseCli.Click, AddressOf btnBrowseCli_Click
        AddHandler btnBrowseSketchDir.Click, AddressOf btnBrowseSketchDir_Click
        AddHandler btnBrowseHardware.Click, AddressOf btnBrowseHardware_Click
        AddHandler btnOK.Click, AddressOf btnOK_Click
        AddHandler btnSaveBoardConfig.Click, AddressOf btnSaveBoardConfig_Click
        AddHandler cmbDefaultBoard.SelectedIndexChanged, AddressOf cmbDefaultBoard_SelectedIndexChanged

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
        chkVerboseOutput.Checked = My.Settings.VerboseOutput

        ' Populate board list
        PopulateBoardsList()

        ' Populate partition schemes
        PopulatePartitionSchemes()

        ' Set default board if configured
        If Not String.IsNullOrEmpty(My.Settings.DefaultBoard) Then
            Dim index = cmbDefaultBoard.Items.IndexOf(My.Settings.DefaultBoard)
            If index >= 0 Then
                cmbDefaultBoard.SelectedIndex = index
            Else
                cmbDefaultBoard.SelectedIndex = 0
            End If
        Else
            cmbDefaultBoard.SelectedIndex = 0
        End If

        ' Set default partition if configured
        If Not String.IsNullOrEmpty(My.Settings.DefaultPartition) Then
            Dim index = cmbDefaultPartition.Items.IndexOf(My.Settings.DefaultPartition)
            If index >= 0 Then
                cmbDefaultPartition.SelectedIndex = index
            Else
                cmbDefaultPartition.SelectedIndex = 0
            End If
        Else
            cmbDefaultPartition.SelectedIndex = 0
        End If

        ' Load board configuration for selected board
        LoadBoardConfiguration()
    End Sub

    ' Populate board types list
    Private Sub PopulateBoardsList()
        cmbDefaultBoard.Items.Clear()

        ' Add KC-Link boards
        cmbDefaultBoard.Items.Add("KC-Link PRO A8 (Default)")
        cmbDefaultBoard.Items.Add("KC-Link PRO A8 (Minimal)")
        cmbDefaultBoard.Items.Add("KC-Link PRO A8 (OTA)")

        ' Add standard ESP32 boards
        cmbDefaultBoard.Items.Add("ESP32 Dev Module")
        cmbDefaultBoard.Items.Add("ESP32 Wrover Kit")
        cmbDefaultBoard.Items.Add("ESP32 Pico Kit")
        cmbDefaultBoard.Items.Add("ESP32-S2 Dev Module")
        cmbDefaultBoard.Items.Add("ESP32-S3 Dev Module")
        cmbDefaultBoard.Items.Add("ESP32-C3 Dev Module")

        ' Add any custom boards from config
        For Each boardName In boardManager.GetBoardNames()
            If Not cmbDefaultBoard.Items.Contains(boardName) Then
                cmbDefaultBoard.Items.Add(boardName)
            End If
        Next
    End Sub

    ' Populate partition scheme list
    Private Sub PopulatePartitionSchemes()
        cmbDefaultPartition.Items.Clear()

        ' Add standard partition schemes
        cmbDefaultPartition.Items.Add("default")
        cmbDefaultPartition.Items.Add("min_spiffs")
        cmbDefaultPartition.Items.Add("min_ota")
        cmbDefaultPartition.Items.Add("huge_app")
        cmbDefaultPartition.Items.Add("custom")

        ' Add any custom partition schemes
        For Each scheme In boardManager.GetCustomPartitions()
            If Not cmbDefaultPartition.Items.Contains(scheme) Then
                cmbDefaultPartition.Items.Add(scheme)
            End If
        Next
    End Sub

    ' Load board configuration for selected board
    Private Sub LoadBoardConfiguration()
        If cmbDefaultBoard.SelectedItem IsNot Nothing Then
            Dim boardName = cmbDefaultBoard.SelectedItem.ToString()
            Dim fqbn = boardManager.GetFQBN(boardName)

            ' Format the board configuration for display
            Dim config = $"// Configuration for {boardName}{Environment.NewLine}"
            config &= $"Board FQBN: {fqbn}{Environment.NewLine}{Environment.NewLine}"

            ' Add board parameters
            config &= "// Board Parameters:{Environment.NewLine}"

            ' Parse FQBN to extract parameters
            If fqbn.Contains(":") Then
                Dim parts = fqbn.Split(New Char() {":"c})
                If parts.Length >= 4 Then
                    Dim parameters = parts(3).Split(New Char() {","c})
                    For Each param In parameters
                        If param.Contains("=") Then
                            Dim keyValue = param.Split(New Char() {"="c})
                            config &= $"{keyValue(0)}={keyValue(1)}{Environment.NewLine}"
                        End If
                    Next
                End If
            End If

            ' Display the configuration
            txtBoardConfig.Text = config
        Else
            txtBoardConfig.Text = "// No board selected"
        End If
    End Sub

    ' Save settings
    Private Sub SaveSettings()
        My.Settings.ArduinoCliPath = txtArduinoCliPath.Text
        My.Settings.DefaultSketchDir = txtDefaultSketchDir.Text
        My.Settings.HardwarePath = txtHardwarePath.Text
        My.Settings.CompileTimeout = CInt(numCompileTimeout.Value)
        My.Settings.EnableLogging = chkEnableLogging.Checked
        My.Settings.VerboseOutput = chkVerboseOutput.Checked

        ' Save board and partition preferences
        If cmbDefaultBoard.SelectedItem IsNot Nothing Then
            My.Settings.DefaultBoard = cmbDefaultBoard.SelectedItem.ToString()
        End If

        If cmbDefaultPartition.SelectedItem IsNot Nothing Then
            My.Settings.DefaultPartition = cmbDefaultPartition.SelectedItem.ToString()
        End If

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

                ' Check if boards.txt exists
                Dim boardsFile = Path.Combine(dialog.SelectedPath, "boards.txt")
                If File.Exists(boardsFile) Then
                    boardManager.BoardsFilePath = boardsFile
                    boardManager.LoadBoardConfigurations()

                    ' Refresh boards list
                    PopulateBoardsList()
                Else

                    ' Check if this is an Arduino installation directory
                    Dim espPath = Path.Combine(dialog.SelectedPath, "hardware", "espressif", "esp32")
                    If Directory.Exists(espPath) Then
                        boardsFile = Path.Combine(espPath, "boards.txt")
                        If File.Exists(boardsFile) Then
                            boardManager.BoardsFilePath = boardsFile
                            boardManager.LoadBoardConfigurations()

                            ' Refresh boards list
                            PopulateBoardsList()
                        Else
                            MessageBox.Show("Could not find boards.txt in the Arduino ESP32 hardware directory.",
                                         "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        End If
                    Else
                        MessageBox.Show("Could not find boards.txt file in the selected directory.",
                                     "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End If
            End If
        End Using
    End Sub

    Private Sub cmbDefaultBoard_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Load configuration for selected board
        LoadBoardConfiguration()

        ' Set default partition scheme based on selected board
        If cmbDefaultBoard.SelectedItem IsNot Nothing Then
            Dim boardName = cmbDefaultBoard.SelectedItem.ToString()
            Dim defaultPartition = boardManager.GetDefaultPartitionForBoard(boardName)

            If Not String.IsNullOrEmpty(defaultPartition) Then
                Dim index = cmbDefaultPartition.Items.IndexOf(defaultPartition)
                If index >= 0 Then
                    cmbDefaultPartition.SelectedIndex = index
                End If
            End If
        End If
    End Sub

    Private Sub btnSaveBoardConfig_Click(sender As Object, e As EventArgs)
        ' Parse and save board configuration from text box
        If cmbDefaultBoard.SelectedItem IsNot Nothing Then
            Dim boardName = cmbDefaultBoard.SelectedItem.ToString()
            Dim config = txtBoardConfig.Text

            ' Extract parameters from text
            Dim parameters As New Dictionary(Of String, String)

            Using reader As New StringReader(config)
                Dim line As String

                While True
                    line = reader.ReadLine()
                    If line Is Nothing Then
                        Exit While
                    End If

                    ' Skip comment lines and empty lines
                    If line.Trim().StartsWith("//") OrElse String.IsNullOrWhiteSpace(line) Then
                        Continue While
                    End If

                    ' Look for parameter=value lines
                    If line.Contains("=") Then
                        Dim parts = line.Split(New Char() {"="c}, 2)
                        If parts.Length = 2 Then
                            Dim key = parts(0).Trim()
                            Dim value = parts(1).Trim()
                            parameters(key) = value
                        End If
                    End If
                End While
            End Using

            ' Update board configuration
            If parameters.Count > 0 Then
                boardManager.UpdateBoardConfiguration(boardName, parameters)
                MessageBox.Show("Board configuration updated successfully.",
                               "Configuration Updated", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Log successful update with current date and user information
                Debug.WriteLine($"[2025-06-19 04:09:09] Board configuration updated by Chamil1983")
            Else
                MessageBox.Show("No valid parameters found in the configuration.",
                               "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Else
            MessageBox.Show("Please select a board first.",
                           "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
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