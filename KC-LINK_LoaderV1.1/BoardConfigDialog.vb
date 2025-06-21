Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.IO

Public Class BoardConfigDialog
    Inherits Form

    ' UI Controls
    Private WithEvents tabControl As TabControl
    Private WithEvents tabBoardConfig As TabPage
    Private WithEvents tabFQBN As TabPage
    Private WithEvents tabAdvanced As TabPage

    Private WithEvents cmbBoardType As ComboBox
    Private WithEvents lblBoardSelection As Label
    Private WithEvents btnReload As Button

    Private WithEvents configPanel As TableLayoutPanel
    Private WithEvents lblCPUFreq As Label
    Private WithEvents cmbCPUFreq As ComboBox
    Private WithEvents lblFlashMode As Label
    Private WithEvents cmbFlashMode As ComboBox
    Private WithEvents lblFlashFreq As Label
    Private WithEvents cmbFlashFreq As ComboBox
    Private WithEvents lblPartitionScheme As Label
    Private WithEvents cmbPartitionScheme As ComboBox
    Private WithEvents lblUploadSpeed As Label
    Private WithEvents cmbUploadSpeed As ComboBox
    Private WithEvents lblDebugLevel As Label
    Private WithEvents cmbDebugLevel As ComboBox
    Private WithEvents chkPSRAM As CheckBox

    Private WithEvents txtFQBN As TextBox
    Private WithEvents btnSaveFQBN As Button

    Private WithEvents txtBoardsFile As TextBox
    Private WithEvents btnBrowseBoardsFile As Button
    Private WithEvents btnReloadBoardsFile As Button

    Private WithEvents btnSave As Button
    Private WithEvents btnCancel As Button
    Private WithEvents btnResetDefaults As Button

    ' Data
    Private boardManager As BoardManager
    Private boardName As String
    Private currentFQBN As String
    Private currentParameters As Dictionary(Of String, String)

    ' Constructor
    Public Sub New(manager As BoardManager, board As String)
        MyBase.New()
        boardManager = manager
        boardName = board
        InitializeComponent()
        LoadBoardParameters()
    End Sub

    Private Sub InitializeComponent()
        ' Form setup
        Me.Text = $"Board Configuration: {boardName}"
        Me.Size = New Size(700, 550)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent

        ' Main layout
        Dim mainLayout As New TableLayoutPanel()
        mainLayout.Dock = DockStyle.Fill
        mainLayout.RowCount = 3
        mainLayout.ColumnCount = 1
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))
        mainLayout.Padding = New Padding(10)

        ' Board selection area
        Dim boardSelectionPanel As New TableLayoutPanel()
        boardSelectionPanel.Dock = DockStyle.Fill
        boardSelectionPanel.ColumnCount = 3
        boardSelectionPanel.RowCount = 1
        boardSelectionPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
        boardSelectionPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        boardSelectionPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))

        lblBoardSelection = New Label()
        lblBoardSelection.Text = "Board Type:"
        lblBoardSelection.TextAlign = ContentAlignment.MiddleRight
        lblBoardSelection.Dock = DockStyle.Fill

        cmbBoardType = New ComboBox()
        cmbBoardType.DropDownStyle = ComboBoxStyle.DropDownList
        cmbBoardType.Dock = DockStyle.Fill
        cmbBoardType.Margin = New Padding(5, 10, 5, 10)

        btnReload = New Button()
        btnReload.Text = "Reload"
        btnReload.Dock = DockStyle.Fill
        btnReload.Margin = New Padding(5, 10, 5, 10)

        boardSelectionPanel.Controls.Add(lblBoardSelection, 0, 0)
        boardSelectionPanel.Controls.Add(cmbBoardType, 1, 0)
        boardSelectionPanel.Controls.Add(btnReload, 2, 0)

        ' Tab control for different views
        tabControl = New TabControl()
        tabControl.Dock = DockStyle.Fill

        ' Board configuration tab
        tabBoardConfig = New TabPage("Board Configuration")

        configPanel = New TableLayoutPanel()
        configPanel.Dock = DockStyle.Fill
        configPanel.ColumnCount = 2
        configPanel.RowCount = 7
        configPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        configPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        For i As Integer = 0 To 6
            configPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next
        configPanel.Padding = New Padding(10)

        ' CPU Frequency
        lblCPUFreq = New Label()
        lblCPUFreq.Text = "CPU Frequency:"
        lblCPUFreq.TextAlign = ContentAlignment.MiddleRight
        lblCPUFreq.Dock = DockStyle.Fill

        cmbCPUFreq = New ComboBox()
        cmbCPUFreq.DropDownStyle = ComboBoxStyle.DropDownList
        cmbCPUFreq.Dock = DockStyle.Fill
        cmbCPUFreq.Margin = New Padding(5, 10, 5, 10)

        ' Flash Mode
        lblFlashMode = New Label()
        lblFlashMode.Text = "Flash Mode:"
        lblFlashMode.TextAlign = ContentAlignment.MiddleRight
        lblFlashMode.Dock = DockStyle.Fill

        cmbFlashMode = New ComboBox()
        cmbFlashMode.DropDownStyle = ComboBoxStyle.DropDownList
        cmbFlashMode.Dock = DockStyle.Fill
        cmbFlashMode.Margin = New Padding(5, 10, 5, 10)

        ' Flash Frequency
        lblFlashFreq = New Label()
        lblFlashFreq.Text = "Flash Frequency:"
        lblFlashFreq.TextAlign = ContentAlignment.MiddleRight
        lblFlashFreq.Dock = DockStyle.Fill

        cmbFlashFreq = New ComboBox()
        cmbFlashFreq.DropDownStyle = ComboBoxStyle.DropDownList
        cmbFlashFreq.Dock = DockStyle.Fill
        cmbFlashFreq.Margin = New Padding(5, 10, 5, 10)

        ' Partition Scheme
        lblPartitionScheme = New Label()
        lblPartitionScheme.Text = "Partition Scheme:"
        lblPartitionScheme.TextAlign = ContentAlignment.MiddleRight
        lblPartitionScheme.Dock = DockStyle.Fill

        cmbPartitionScheme = New ComboBox()
        cmbPartitionScheme.DropDownStyle = ComboBoxStyle.DropDownList
        cmbPartitionScheme.Dock = DockStyle.Fill
        cmbPartitionScheme.Margin = New Padding(5, 10, 5, 10)

        ' Upload Speed
        lblUploadSpeed = New Label()
        lblUploadSpeed.Text = "Upload Speed:"
        lblUploadSpeed.TextAlign = ContentAlignment.MiddleRight
        lblUploadSpeed.Dock = DockStyle.Fill

        cmbUploadSpeed = New ComboBox()
        cmbUploadSpeed.DropDownStyle = ComboBoxStyle.DropDownList
        cmbUploadSpeed.Dock = DockStyle.Fill
        cmbUploadSpeed.Margin = New Padding(5, 10, 5, 10)

        ' Debug Level
        lblDebugLevel = New Label()
        lblDebugLevel.Text = "Debug Level:"
        lblDebugLevel.TextAlign = ContentAlignment.MiddleRight
        lblDebugLevel.Dock = DockStyle.Fill

        cmbDebugLevel = New ComboBox()
        cmbDebugLevel.DropDownStyle = ComboBoxStyle.DropDownList
        cmbDebugLevel.Dock = DockStyle.Fill
        cmbDebugLevel.Margin = New Padding(5, 10, 5, 10)

        ' PSRAM
        chkPSRAM = New CheckBox()
        chkPSRAM.Text = "Enable PSRAM"
        chkPSRAM.Dock = DockStyle.Fill
        chkPSRAM.Margin = New Padding(5, 10, 5, 10)

        ' Add controls to config panel
        configPanel.Controls.Add(lblCPUFreq, 0, 0)
        configPanel.Controls.Add(cmbCPUFreq, 1, 0)
        configPanel.Controls.Add(lblFlashMode, 0, 1)
        configPanel.Controls.Add(cmbFlashMode, 1, 1)
        configPanel.Controls.Add(lblFlashFreq, 0, 2)
        configPanel.Controls.Add(cmbFlashFreq, 1, 2)
        configPanel.Controls.Add(lblPartitionScheme, 0, 3)
        configPanel.Controls.Add(cmbPartitionScheme, 1, 3)
        configPanel.Controls.Add(lblUploadSpeed, 0, 4)
        configPanel.Controls.Add(cmbUploadSpeed, 1, 4)
        configPanel.Controls.Add(lblDebugLevel, 0, 5)
        configPanel.Controls.Add(cmbDebugLevel, 1, 5)
        configPanel.Controls.Add(chkPSRAM, 1, 6)

        tabBoardConfig.Controls.Add(configPanel)

        ' FQBN tab (raw view)
        tabFQBN = New TabPage("FQBN")

        Dim fqbnPanel As New TableLayoutPanel()
        fqbnPanel.Dock = DockStyle.Fill
        fqbnPanel.RowCount = 2
        fqbnPanel.ColumnCount = 2
        fqbnPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        fqbnPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))
        fqbnPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        fqbnPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        fqbnPanel.Padding = New Padding(10)

        txtFQBN = New TextBox()
        txtFQBN.Dock = DockStyle.Fill
        txtFQBN.Multiline = True
        txtFQBN.ReadOnly = False
        txtFQBN.Font = New Font("Consolas", 10)
        txtFQBN.ScrollBars = ScrollBars.Vertical

        btnSaveFQBN = New Button()
        btnSaveFQBN.Text = "Apply FQBN"
        btnSaveFQBN.Dock = DockStyle.Fill

        fqbnPanel.Controls.Add(txtFQBN, 0, 0)
        fqbnPanel.SetColumnSpan(txtFQBN, 2)
        fqbnPanel.Controls.Add(btnSaveFQBN, 1, 1)

        tabFQBN.Controls.Add(fqbnPanel)

        ' Advanced tab (boards.txt file)
        tabAdvanced = New TabPage("Advanced")

        Dim advancedPanel As New TableLayoutPanel()
        advancedPanel.Dock = DockStyle.Fill
        advancedPanel.RowCount = 2
        advancedPanel.ColumnCount = 3
        advancedPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))
        advancedPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        advancedPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        advancedPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        advancedPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        advancedPanel.Padding = New Padding(10)

        Dim lblBoardsFile As New Label()
        lblBoardsFile.Text = "boards.txt File:"
        lblBoardsFile.TextAlign = ContentAlignment.MiddleLeft
        lblBoardsFile.Dock = DockStyle.Fill

        txtBoardsFile = New TextBox()
        txtBoardsFile.Dock = DockStyle.Fill
        txtBoardsFile.ReadOnly = True

        btnBrowseBoardsFile = New Button()
        btnBrowseBoardsFile.Text = "Browse..."
        btnBrowseBoardsFile.Dock = DockStyle.Fill

        btnReloadBoardsFile = New Button()
        btnReloadBoardsFile.Text = "Reload"
        btnReloadBoardsFile.Dock = DockStyle.Fill

        Dim boardInfoTextBox As New RichTextBox()
        boardInfoTextBox.Dock = DockStyle.Fill
        boardInfoTextBox.ReadOnly = True
        boardInfoTextBox.BackColor = SystemColors.Window
        boardInfoTextBox.Text = "The boards.txt file contains definitions for ESP32 boards." & Environment.NewLine & Environment.NewLine &
                             "By default, the Arduino-CLI package installation path is used. You can select a custom file if needed." & Environment.NewLine & Environment.NewLine &
                             "After changing the boards.txt file, click 'Reload' to refresh the board configurations."

        advancedPanel.Controls.Add(lblBoardsFile, 0, 0)
        advancedPanel.Controls.Add(txtBoardsFile, 0, 0)
        advancedPanel.Controls.Add(btnBrowseBoardsFile, 1, 0)
        advancedPanel.Controls.Add(btnReloadBoardsFile, 2, 0)
        advancedPanel.Controls.Add(boardInfoTextBox, 0, 1)
        advancedPanel.SetColumnSpan(boardInfoTextBox, 3)

        tabAdvanced.Controls.Add(advancedPanel)

        ' Add tabs to tab control
        tabControl.TabPages.Add(tabBoardConfig)
        tabControl.TabPages.Add(tabFQBN)
        tabControl.TabPages.Add(tabAdvanced)

        ' Button panel
        Dim buttonPanel As New TableLayoutPanel()
        buttonPanel.Dock = DockStyle.Fill
        buttonPanel.ColumnCount = 3
        buttonPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        buttonPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        buttonPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))

        btnResetDefaults = New Button()
        btnResetDefaults.Text = "Reset to Defaults"
        btnResetDefaults.Dock = DockStyle.Right

        btnCancel = New Button()
        btnCancel.Text = "Cancel"
        btnCancel.DialogResult = DialogResult.Cancel
        btnCancel.Dock = DockStyle.Fill

        btnSave = New Button()
        btnSave.Text = "Save"
        btnSave.DialogResult = DialogResult.OK
        btnSave.Dock = DockStyle.Fill
        btnSave.Font = New Font(btnSave.Font, FontStyle.Bold)

        buttonPanel.Controls.Add(btnResetDefaults, 0, 0)
        buttonPanel.Controls.Add(btnCancel, 1, 0)
        buttonPanel.Controls.Add(btnSave, 2, 0)

        ' Add controls to main layout
        mainLayout.Controls.Add(boardSelectionPanel, 0, 0)
        mainLayout.Controls.Add(tabControl, 0, 1)
        mainLayout.Controls.Add(buttonPanel, 0, 2)

        ' Add main layout to form
        Me.Controls.Add(mainLayout)

        ' Set up event handlers
        AddHandler btnResetDefaults.Click, AddressOf btnResetDefaults_Click
        AddHandler btnSave.Click, AddressOf btnSave_Click
        AddHandler btnReload.Click, AddressOf btnReload_Click
        AddHandler cmbBoardType.SelectedIndexChanged, AddressOf cmbBoardType_SelectedIndexChanged
        AddHandler tabControl.SelectedIndexChanged, AddressOf tabControl_SelectedIndexChanged
        AddHandler btnSaveFQBN.Click, AddressOf btnSaveFQBN_Click
        AddHandler btnBrowseBoardsFile.Click, AddressOf btnBrowseBoardsFile_Click
        AddHandler btnReloadBoardsFile.Click, AddressOf btnReloadBoardsFile_Click

        ' Parameter change handlers
        AddHandler cmbCPUFreq.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbFlashMode.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbFlashFreq.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbPartitionScheme.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbUploadSpeed.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbDebugLevel.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler chkPSRAM.CheckedChanged, AddressOf Parameter_Changed

        ' Set accept and cancel buttons
        Me.AcceptButton = btnSave
        Me.CancelButton = btnCancel
    End Sub

    Private Sub LoadBoardParameters()
        ' Populate board type combobox
        PopulateBoardComboBox()

        ' Initialize boards.txt file path
        txtBoardsFile.Text = boardManager.BoardsFilePath

        ' Set selected board
        For i As Integer = 0 To cmbBoardType.Items.Count - 1
            If cmbBoardType.Items(i).ToString() = boardName Then
                cmbBoardType.SelectedIndex = i
                Exit For
            End If
        Next

        ' If no board is selected, select first item if available
        If cmbBoardType.SelectedIndex < 0 AndAlso cmbBoardType.Items.Count > 0 Then
            cmbBoardType.SelectedIndex = 0
        End If

        ' Load board configuration for selected board
        LoadBoardConfiguration()
    End Sub

    Private Sub PopulateBoardComboBox()
        cmbBoardType.Items.Clear()

        ' Add all known board types
        For Each board In boardManager.GetBoardNames()
            cmbBoardType.Items.Add(board)
        Next
    End Sub

    Private Sub LoadBoardConfiguration()
        If cmbBoardType.SelectedItem Is Nothing Then
            Return
        End If

        ' Get the current board name
        boardName = cmbBoardType.SelectedItem.ToString()

        ' Get the FQBN for this board
        currentFQBN = boardManager.GetFQBN(boardName)

        ' Update the form title
        Me.Text = $"Board Configuration: {boardName}"

        ' Update the FQBN text
        txtFQBN.Text = $"Board: {boardName}{Environment.NewLine}"
        txtFQBN.Text += $"FQBN: {currentFQBN}{Environment.NewLine}{Environment.NewLine}"
        txtFQBN.Text += "You can edit the FQBN directly using this format:{Environment.NewLine}"
        txtFQBN.Text += "vendor:architecture:board:param1=value1,param2=value2{Environment.NewLine}{Environment.NewLine}"
        txtFQBN.Text += currentFQBN

        ' Parse FQBN to extract parameters
        ExtractParametersFromFQBN()

        ' Populate parameter dropdowns
        PopulateParameterDropdowns()
    End Sub

    Private Sub ExtractParametersFromFQBN()
        currentParameters = New Dictionary(Of String, String)()

        ' Default values
        currentParameters("CPUFreq") = "240"
        currentParameters("FlashMode") = "dio"
        currentParameters("FlashFreq") = "80"
        currentParameters("PartitionScheme") = "default"
        currentParameters("UploadSpeed") = "921600"
        currentParameters("DebugLevel") = "none"
        currentParameters("PSRAM") = "disabled"

        ' Parse parameters from FQBN
        If currentFQBN.Contains(":") Then
            Dim parts = currentFQBN.Split(New Char() {":"c})
            If parts.Length >= 4 Then
                Dim paramPart = parts(3)
                Dim paramPairs = paramPart.Split(New Char() {","c})

                For Each pair In paramPairs
                    If pair.Contains("=") Then
                        Dim keyValue = pair.Split(New Char() {"="c}, 2)
                        If keyValue.Length = 2 Then
                            currentParameters(keyValue(0)) = keyValue(1)
                        End If
                    End If
                Next
            End If
        End If
    End Sub

    Private Sub PopulateParameterDropdowns()
        ' Avoid triggering change events during setup
        RemoveParameterChangeHandlers()

        ' Populate CPU Frequency dropdown
        PopulateDropdown(cmbCPUFreq, boardManager.GetParameterOptions(boardName, "CPUFreq"), currentParameters("CPUFreq"))

        ' Populate Flash Mode dropdown
        PopulateDropdown(cmbFlashMode, boardManager.GetParameterOptions(boardName, "FlashMode"), currentParameters("FlashMode"))

        ' Populate Flash Frequency dropdown
        PopulateDropdown(cmbFlashFreq, boardManager.GetParameterOptions(boardName, "FlashFreq"), currentParameters("FlashFreq"))

        ' Populate Partition Scheme dropdown
        PopulateDropdown(cmbPartitionScheme, boardManager.GetParameterOptions(boardName, "PartitionScheme"), currentParameters("PartitionScheme"))

        ' Populate Upload Speed dropdown
        PopulateDropdown(cmbUploadSpeed, boardManager.GetParameterOptions(boardName, "UploadSpeed"), currentParameters("UploadSpeed"))

        ' Populate Debug Level dropdown
        PopulateDropdown(cmbDebugLevel, boardManager.GetParameterOptions(boardName, "DebugLevel"), currentParameters("DebugLevel"))

        ' Set PSRAM checkbox
        chkPSRAM.Checked = (currentParameters("PSRAM") = "enabled")

        ' Restore change handlers
        AddParameterChangeHandlers()
    End Sub

    Private Sub PopulateDropdown(comboBox As ComboBox, options As Dictionary(Of String, String), selectedValue As String)
        comboBox.Items.Clear()
        comboBox.DisplayMember = "Value"
        comboBox.ValueMember = "Key"

        ' Add options to dropdown
        For Each kvp In options
            comboBox.Items.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
        Next

        ' Select the current value
        For i As Integer = 0 To comboBox.Items.Count - 1
            Dim item = DirectCast(comboBox.Items(i), KeyValuePair(Of String, String))
            If item.Key = selectedValue Then
                comboBox.SelectedIndex = i
                Exit For
            End If
        Next

        ' If nothing is selected, select the first item if available
        If comboBox.SelectedIndex < 0 AndAlso comboBox.Items.Count > 0 Then
            comboBox.SelectedIndex = 0
        End If
    End Sub

    Private Sub RemoveParameterChangeHandlers()
        RemoveHandler cmbCPUFreq.SelectedIndexChanged, AddressOf Parameter_Changed
        RemoveHandler cmbFlashMode.SelectedIndexChanged, AddressOf Parameter_Changed
        RemoveHandler cmbFlashFreq.SelectedIndexChanged, AddressOf Parameter_Changed
        RemoveHandler cmbPartitionScheme.SelectedIndexChanged, AddressOf Parameter_Changed
        RemoveHandler cmbUploadSpeed.SelectedIndexChanged, AddressOf Parameter_Changed
        RemoveHandler cmbDebugLevel.SelectedIndexChanged, AddressOf Parameter_Changed
        RemoveHandler chkPSRAM.CheckedChanged, AddressOf Parameter_Changed
    End Sub

    Private Sub AddParameterChangeHandlers()
        AddHandler cmbCPUFreq.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbFlashMode.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbFlashFreq.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbPartitionScheme.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbUploadSpeed.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler cmbDebugLevel.SelectedIndexChanged, AddressOf Parameter_Changed
        AddHandler chkPSRAM.CheckedChanged, AddressOf Parameter_Changed
    End Sub

    Private Sub Parameter_Changed(sender As Object, e As EventArgs)
        ' Update parameters from UI
        UpdateParametersFromUI()

        ' Update FQBN text if on FQBN tab
        If tabControl.SelectedTab Is tabFQBN Then
            UpdateFQBNText()
        End If
    End Sub

    Private Sub UpdateParametersFromUI()
        ' Update parameters from UI controls
        If cmbCPUFreq.SelectedItem IsNot Nothing Then
            currentParameters("CPUFreq") = DirectCast(cmbCPUFreq.SelectedItem, KeyValuePair(Of String, String)).Key
        End If

        If cmbFlashMode.SelectedItem IsNot Nothing Then
            currentParameters("FlashMode") = DirectCast(cmbFlashMode.SelectedItem, KeyValuePair(Of String, String)).Key
        End If

        If cmbFlashFreq.SelectedItem IsNot Nothing Then
            currentParameters("FlashFreq") = DirectCast(cmbFlashFreq.SelectedItem, KeyValuePair(Of String, String)).Key
        End If

        If cmbPartitionScheme.SelectedItem IsNot Nothing Then
            currentParameters("PartitionScheme") = DirectCast(cmbPartitionScheme.SelectedItem, KeyValuePair(Of String, String)).Key
        End If

        If cmbUploadSpeed.SelectedItem IsNot Nothing Then
            currentParameters("UploadSpeed") = DirectCast(cmbUploadSpeed.SelectedItem, KeyValuePair(Of String, String)).Key
        End If

        If cmbDebugLevel.SelectedItem IsNot Nothing Then
            currentParameters("DebugLevel") = DirectCast(cmbDebugLevel.SelectedItem, KeyValuePair(Of String, String)).Key
        End If

        currentParameters("PSRAM") = If(chkPSRAM.Checked, "enabled", "disabled")

        ' Generate new FQBN from parameters
        If currentFQBN.Contains(":") Then
            Dim parts = currentFQBN.Split(New Char() {":"c}, 4)
            If parts.Length >= 3 Then
                Dim vendor = parts(0)
                Dim architecture = parts(1)
                Dim board = parts(2)

                ' Build parameter string
                Dim paramList As New List(Of String)
                For Each kvp In currentParameters
                    ' Only include non-default parameters for DebugLevel and PSRAM
                    If (kvp.Key = "DebugLevel" AndAlso kvp.Value = "none") Then
                        Continue For
                    End If

                    If (kvp.Key = "PSRAM" AndAlso kvp.Value = "disabled") Then
                        Continue For
                    End If

                    paramList.Add($"{kvp.Key}={kvp.Value}")
                Next

                Dim paramStr = String.Join(",", paramList)

                ' Create new FQBN
                currentFQBN = $"{vendor}:{architecture}:{board}"
                If paramList.Count > 0 Then
                    currentFQBN += ":" & paramStr
                End If
            End If
        End If
    End Sub

    Private Sub UpdateFQBNText()
        txtFQBN.Text = $"Board: {boardName}{Environment.NewLine}"
        txtFQBN.Text += $"FQBN: {currentFQBN}{Environment.NewLine}{Environment.NewLine}"
        txtFQBN.Text += "You can edit the FQBN directly using this format:{Environment.NewLine}"
        txtFQBN.Text += "vendor:architecture:board:param1=value1,param2=value2{Environment.NewLine}{Environment.NewLine}"
        txtFQBN.Text += currentFQBN
    End Sub

    Private Sub btnResetDefaults_Click(sender As Object, e As EventArgs)
        If MessageBox.Show("Reset all settings to default values?", "Reset Confirmation",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then

            ' Reset parameters to defaults
            currentParameters("CPUFreq") = "240"
            currentParameters("FlashMode") = "dio"
            currentParameters("FlashFreq") = "80"
            currentParameters("PartitionScheme") = "default"
            currentParameters("UploadSpeed") = "921600"
            currentParameters("DebugLevel") = "none"
            currentParameters("PSRAM") = "disabled"

            ' Update UI
            PopulateParameterDropdowns()

            ' Update FQBN
            UpdateParametersFromUI()
            UpdateFQBNText()

            MessageBox.Show("Board configuration has been reset to default values.", "Reset Complete",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        ' Make sure parameters are up-to-date
        UpdateParametersFromUI()

        ' Save board configuration
        boardManager.UpdateBoardConfiguration(boardName, currentParameters)

        ' Show success message
        MessageBox.Show($"Board configuration for {boardName} has been saved successfully.",
                      "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)

        ' Log the save operation
        Debug.WriteLine($"[2025-06-20 02:33:09] Board configuration saved for {boardName}")
        Debug.WriteLine($"[2025-06-20 02:33:09] Updated by Chamil1983")
    End Sub

    Private Sub btnReload_Click(sender As Object, e As EventArgs)
        ' Reload board configurations
        boardManager.LoadBoardConfigurations()

        ' Refresh board list
        Dim currentBoard = If(cmbBoardType.SelectedItem IsNot Nothing, cmbBoardType.SelectedItem.ToString(), "")
        PopulateBoardComboBox()

        ' Try to reselect the current board
        If Not String.IsNullOrEmpty(currentBoard) Then
            For i As Integer = 0 To cmbBoardType.Items.Count - 1
                If cmbBoardType.Items(i).ToString() = currentBoard Then
                    cmbBoardType.SelectedIndex = i
                    Exit For
                End If
            Next
        End If

        ' If no board is selected, select first item if available
        If cmbBoardType.SelectedIndex < 0 AndAlso cmbBoardType.Items.Count > 0 Then
            cmbBoardType.SelectedIndex = 0
        End If
    End Sub

    Private Sub cmbBoardType_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Load configuration for the selected board
        If cmbBoardType.SelectedItem IsNot Nothing Then
            boardName = cmbBoardType.SelectedItem.ToString()
            LoadBoardConfiguration()
        End If
    End Sub

    Private Sub tabControl_SelectedIndexChanged(sender As Object, e As EventArgs)
        If tabControl.SelectedTab Is tabFQBN Then
            ' Update FQBN text when switching to FQBN tab
            UpdateFQBNText()
        End If
    End Sub

    Private Sub btnSaveFQBN_Click(sender As Object, e As EventArgs)
        ' Get the FQBN from text box
        Dim fqbn As String = txtFQBN.Text

        ' Extract only the FQBN part if the text contains multiple lines
        If fqbn.Contains(Environment.NewLine) Then
            Dim lines = fqbn.Split(New String() {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
            For Each line In lines
                If line.Contains(":") AndAlso Not line.StartsWith("Board:") AndAlso Not line.StartsWith("FQBN:") Then
                    fqbn = line.Trim()
                    Exit For
                End If
            Next
        End If

        ' Update current FQBN
        currentFQBN = fqbn

        ' Extract parameters from the new FQBN
        ExtractParametersFromFQBN()
        ' Extract parameters from the new FQBN
        ExtractParametersFromFQBN()

        ' Update UI controls
        PopulateParameterDropdowns()

        ' Switch to Board Configuration tab
        tabControl.SelectedTab = tabBoardConfig

        MessageBox.Show("FQBN applied successfully. Please review the settings in the Board Configuration tab.",
                       "FQBN Applied", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Debug.WriteLine($"[2025-06-20 02:52:18] FQBN manually updated to {currentFQBN} by Chamil1983")
    End Sub

    Private Sub btnBrowseBoardsFile_Click(sender As Object, e As EventArgs)
        Using openFileDialog As New OpenFileDialog()
            openFileDialog.Title = "Select boards.txt File"
            openFileDialog.Filter = "boards.txt|boards.txt|Text Files|*.txt|All Files|*.*"
            openFileDialog.FileName = "boards.txt"

            If openFileDialog.ShowDialog() = DialogResult.OK Then
                txtBoardsFile.Text = openFileDialog.FileName
                boardManager.BoardsFilePath = openFileDialog.FileName

                ' Log the change
                Debug.WriteLine($"[2025-06-20 02:52:18] boards.txt path updated to {openFileDialog.FileName} by Chamil1983")

                ' Ask if user wants to reload configurations
                If MessageBox.Show("Would you like to reload board configurations from the selected file?",
                                "Reload Configurations", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    ReloadBoardsFile()
                End If
            End If
        End Using
    End Sub

    Private Sub btnReloadBoardsFile_Click(sender As Object, e As EventArgs)
        ReloadBoardsFile()
    End Sub

    Private Sub ReloadBoardsFile()
        ' Check if the file exists
        If Not File.Exists(boardManager.BoardsFilePath) Then
            MessageBox.Show("The specified boards.txt file doesn't exist. Please select a valid file.",
                          "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' Reload board configurations
        boardManager.LoadBoardConfigurations()

        ' Refresh board list
        Dim currentBoard = If(cmbBoardType.SelectedItem IsNot Nothing, cmbBoardType.SelectedItem.ToString(), "")
        PopulateBoardComboBox()

        ' Try to reselect the current board
        If Not String.IsNullOrEmpty(currentBoard) Then
            For i As Integer = 0 To cmbBoardType.Items.Count - 1
                If cmbBoardType.Items(i).ToString() = currentBoard Then
                    cmbBoardType.SelectedIndex = i
                    Exit For
                End If
            Next
        End If

        ' If no board is selected, select first item if available
        If cmbBoardType.SelectedIndex < 0 AndAlso cmbBoardType.Items.Count > 0 Then
            cmbBoardType.SelectedIndex = 0
        End If

        MessageBox.Show("Board configurations reloaded successfully.",
                       "Reload Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Debug.WriteLine($"[2025-06-20 02:52:18] Board configurations reloaded from {boardManager.BoardsFilePath} by Chamil1983")
    End Sub
End Class

