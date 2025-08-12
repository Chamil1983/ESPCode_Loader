Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.IO
Imports System.Text.RegularExpressions

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
    Private parameterControls As New Dictionary(Of String, Object)()

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
    Private boardConfigOptions As Dictionary(Of String, List(Of KeyValuePair(Of String, String)))
    Private standardParameters As List(Of String) = New List(Of String) From {"CPUFreq", "FlashMode", "FlashFreq", "PartitionScheme", "UploadSpeed", "DebugLevel", "PSRAM"}

    ' Constructor
    Public Sub New(manager As BoardManager, board As String)
        MyBase.New()
        boardManager = manager
        boardName = board
        currentParameters = New Dictionary(Of String, String)() ' Initialize to prevent null reference
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

        ' Create a ScrollableControl to contain the config panel
        Dim scrollPanel As New Panel()
        scrollPanel.Dock = DockStyle.Fill
        scrollPanel.AutoScroll = True

        configPanel = New TableLayoutPanel()
        configPanel.Dock = DockStyle.Top
        configPanel.AutoSize = True
        configPanel.ColumnCount = 2
        configPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
        configPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        configPanel.Padding = New Padding(10)

        scrollPanel.Controls.Add(configPanel)
        tabBoardConfig.Controls.Add(scrollPanel)

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

        ' Set accept and cancel buttons
        Me.AcceptButton = btnSave
        Me.CancelButton = btnCancel
    End Sub

    Private Sub LoadBoardParameters()
        Try
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
        Catch ex As Exception
            MessageBox.Show($"Error loading board parameters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in LoadBoardParameters: {ex.Message}")
        End Try
    End Sub

    Private Sub PopulateBoardComboBox()
        cmbBoardType.Items.Clear()

        ' Add all known board types
        For Each board In boardManager.GetBoardNames()
            cmbBoardType.Items.Add(board)
        Next
    End Sub

    Private Sub LoadBoardConfiguration()
        Try
            If cmbBoardType.SelectedItem Is Nothing Then
                Return
            End If

            ' Get the current board name
            boardName = cmbBoardType.SelectedItem.ToString()

            ' Get the FQBN for this board
            currentFQBN = boardManager.GetFQBN(boardName)

            ' Update the form title
            Me.Text = $"Board Configuration: {boardName}"

            ' Extract parameters from the FQBN
            currentParameters = boardManager.ExtractParametersFromFQBN(currentFQBN)

            ' Get all configuration options for this board
            boardConfigOptions = boardManager.GetAllBoardConfigOptions(boardName)

            ' Update the FQBN text
            UpdateFQBNText()

            ' Create configuration UI based on available options
            CreateConfigurationUI()
        Catch ex As Exception
            MessageBox.Show($"Error loading board configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in LoadBoardConfiguration: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub CreateConfigurationUI()
        Try
            ' Clear existing controls
            configPanel.Controls.Clear()
            configPanel.RowStyles.Clear()
            parameterControls.Clear()

            ' Reset row count
            configPanel.RowCount = 0

            ' List to keep track of parameters in order of display
            Dim displayOrder As New List(Of String)

            ' First add standard parameters in specific order
            For Each param In standardParameters
                If boardConfigOptions.ContainsKey(param) Then
                    displayOrder.Add(param)
                End If
            Next

            ' Then add any additional parameters from the board
            For Each param In boardConfigOptions.Keys
                If Not displayOrder.Contains(param) Then
                    displayOrder.Add(param)
                End If
            Next

            ' Create UI controls for each parameter
            Dim row As Integer = 0
            For Each paramName In displayOrder
                ' Skip empty options
                If Not boardConfigOptions.ContainsKey(paramName) OrElse boardConfigOptions(paramName).Count = 0 Then
                    Continue For
                End If

                ' Add a new row to the table
                configPanel.RowCount += 1
                configPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))

                ' Create label
                Dim label As New Label()
                label.Text = GetFriendlyParameterName(paramName) & ":"
                label.TextAlign = ContentAlignment.MiddleRight
                label.Dock = DockStyle.Fill
                label.Margin = New Padding(3, 10, 3, 10)

                ' Determine control type based on parameter
                If paramName = "PSRAM" Then
                    ' Boolean parameter - use checkbox
                    Dim checkBox As New CheckBox()
                    checkBox.Text = "Enable"
                    ' Ensure parameter exists before accessing
                    If currentParameters.ContainsKey(paramName) Then
                        checkBox.Checked = (currentParameters(paramName) = "enabled")
                    Else
                        checkBox.Checked = False
                        currentParameters(paramName) = "disabled" ' Set default
                    End If

                    checkBox.Dock = DockStyle.Fill
                    checkBox.Margin = New Padding(5, 10, 5, 10)

                    ' Add handler
                    AddHandler checkBox.CheckedChanged, AddressOf Parameter_Changed

                    ' Add to panel
                    configPanel.Controls.Add(label, 0, row)
                    configPanel.Controls.Add(checkBox, 1, row)

                    ' Store control reference
                    parameterControls(paramName) = checkBox
                Else
                    ' Enum parameter - use dropdown
                    Dim comboBox As New ComboBox()
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList
                    comboBox.Dock = DockStyle.Fill
                    comboBox.Margin = New Padding(5, 10, 5, 10)
                    comboBox.DisplayMember = "Value"
                    comboBox.ValueMember = "Key"
                    comboBox.Tag = paramName  ' Store parameter name in Tag

                    ' Populate options
                    For Each kvpOption As KeyValuePair(Of String, String) In boardConfigOptions(paramName)
                        comboBox.Items.Add(kvpOption)
                    Next

                    ' Select current value if it exists in parameters
                    Dim selectedIndex As Integer = -1

                    ' Ensure parameter exists before accessing
                    If currentParameters.ContainsKey(paramName) Then
                        ' Find matching option
                        For i As Integer = 0 To comboBox.Items.Count - 1
                            Dim item = DirectCast(comboBox.Items(i), KeyValuePair(Of String, String))
                            If item.Key = currentParameters(paramName) Then
                                selectedIndex = i
                                Exit For
                            End If
                        Next
                    Else
                        ' Parameter not found, add default
                        If comboBox.Items.Count > 0 Then
                            Dim defaultItem = DirectCast(comboBox.Items(0), KeyValuePair(Of String, String))
                            currentParameters(paramName) = defaultItem.Key
                        End If
                    End If

                    If selectedIndex >= 0 Then
                        comboBox.SelectedIndex = selectedIndex
                    ElseIf comboBox.Items.Count > 0 Then
                        comboBox.SelectedIndex = 0
                        ' Update parameter with selected value
                        Dim item = DirectCast(comboBox.Items(0), KeyValuePair(Of String, String))
                        currentParameters(paramName) = item.Key
                    End If

                    ' Add handler
                    AddHandler comboBox.SelectedIndexChanged, AddressOf Parameter_Changed

                    ' Add to panel
                    configPanel.Controls.Add(label, 0, row)
                    configPanel.Controls.Add(comboBox, 1, row)

                    ' Store control reference
                    parameterControls(paramName) = comboBox
                End If

                row += 1
            Next

            ' Update layout
            configPanel.PerformLayout()
            Debug.WriteLine($"[2025-08-12 13:15:24] Created UI with {parameterControls.Count} parameters for board {boardName} by Chamil1983")
        Catch ex As Exception
            MessageBox.Show($"Error creating configuration UI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in CreateConfigurationUI: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Function GetFriendlyParameterName(paramName As String) As String
        ' Convert parameter names to user-friendly display names
        Select Case paramName
            Case "CPUFreq"
                Return "CPU Frequency"
            Case "FlashMode"
                Return "Flash Mode"
            Case "FlashFreq"
                Return "Flash Frequency"
            Case "PartitionScheme"
                Return "Partition Scheme"
            Case "UploadSpeed"
                Return "Upload Speed"
            Case "DebugLevel"
                Return "Debug Level"
            Case "PSRAM"
                Return "PSRAM"
            Case Else
                ' Convert CamelCase to spaces
                Return Regex.Replace(paramName, "([a-z])([A-Z])", "$1 $2")
        End Select
    End Function

    Private Sub Parameter_Changed(sender As Object, e As EventArgs)
        ' Update parameters from UI
        UpdateParametersFromUI()

        ' Update FQBN text if on FQBN tab
        If tabControl.SelectedTab Is tabFQBN Then
            UpdateFQBNText()
        End If
    End Sub

    Private Sub UpdateParametersFromUI()
        Try
            ' Update parameters from UI controls
            For Each paramName In parameterControls.Keys
                Dim control = parameterControls(paramName)

                If TypeOf control Is ComboBox Then
                    Dim comboBox = DirectCast(control, ComboBox)
                    If comboBox.SelectedItem IsNot Nothing Then
                        Dim selectedValue = DirectCast(comboBox.SelectedItem, KeyValuePair(Of String, String))
                        currentParameters(paramName) = selectedValue.Key
                    End If
                ElseIf TypeOf control Is CheckBox Then
                    Dim checkBox = DirectCast(control, CheckBox)
                    currentParameters(paramName) = If(checkBox.Checked, "enabled", "disabled")
                End If
            Next

            ' Update the FQBN based on the current parameters
            UpdateFQBN()
        Catch ex As Exception
            MessageBox.Show($"Error updating parameters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in UpdateParametersFromUI: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub UpdateFQBN()
        Try
            ' Generate new FQBN from parameters
            If currentFQBN.Contains(":") Then
                Dim parts = currentFQBN.Split(New Char() {":"c}, 4)
                If parts.Length >= 3 Then
                    Dim vendor = parts(0)
                    Dim architecture = parts(1)
                    Dim board = parts(2)

                    ' Build parameter string
                    Dim paramList As New List(Of String)

                    ' Always include the PartitionScheme parameter first
                    If currentParameters.ContainsKey("PartitionScheme") Then
                        paramList.Add($"PartitionScheme={currentParameters("PartitionScheme")}")
                    End If

                    ' Add other parameters
                    For Each kvp In currentParameters
                        ' Skip PartitionScheme as it's already added
                        If kvp.Key = "PartitionScheme" Then
                            Continue For
                        End If

                        ' Skip default values for these parameters
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

                    Debug.WriteLine($"[2025-08-12 13:15:24] Updated FQBN: {currentFQBN} by Chamil1983")
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error updating FQBN: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in UpdateFQBN: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub UpdateFQBNText()
        Try
            ' Create detailed FQBN text with current configuration
            Dim fqbnText As New System.Text.StringBuilder()

            ' Basic board information
            fqbnText.AppendLine($"Board: {boardName}")
            fqbnText.AppendLine($"FQBN: {currentFQBN}")
            fqbnText.AppendLine()

            ' Current configuration details
            fqbnText.AppendLine("Current Configuration:")

            ' List all parameters with their friendly display values
            For Each paramName In parameterControls.Keys
                If currentParameters.ContainsKey(paramName) Then
                    Dim paramValue = currentParameters(paramName)
                    Dim displayValue = GetDisplayValueForParam(paramName, paramValue)
                    fqbnText.AppendLine($"- {GetFriendlyParameterName(paramName)}: {displayValue}")
                End If
            Next

            fqbnText.AppendLine()

            ' FQBN editing instructions
            fqbnText.AppendLine("You can edit the FQBN directly using this format:")
            fqbnText.AppendLine("vendor:architecture:board:param1=value1,param2=value2")
            fqbnText.AppendLine()

            ' Current FQBN value that can be edited
            fqbnText.Append(currentFQBN)

            ' Update the text box
            txtFQBN.Text = fqbnText.ToString()
        Catch ex As Exception
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in UpdateFQBNText: {ex.Message} by Chamil1983")
        End Try
    End Sub

    ' Helper method to get display value for a parameter
    Private Function GetDisplayValueForParam(paramName As String, paramValue As String) As String
        Try
            ' Handle boolean parameters
            If paramName = "PSRAM" Then
                Return If(paramValue = "enabled", "Enabled", "Disabled")
            End If

            ' For dropdown parameters, find display value from options
            If boardConfigOptions.ContainsKey(paramName) Then
                For Each kvpOption As KeyValuePair(Of String, String) In boardConfigOptions(paramName)
                    If kvpOption.Key = paramValue Then
                        Return kvpOption.Value
                    End If
                Next
            End If

            ' Fall back to parameter value
            Return paramValue
        Catch ex As Exception
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in GetDisplayValueForParam: {ex.Message} by Chamil1983")
            Return paramValue
        End Try
    End Function

    Private Sub btnResetDefaults_Click(sender As Object, e As EventArgs)
        Try
            If MessageBox.Show("Reset all settings to default values?", "Reset Confirmation",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then

                ' Reset parameters to defaults based on the selected board
                ' Get the default FQBN for this board without any custom parameters
                Dim defaultFQBN = boardManager.GetFQBN(boardName)

                ' Extract default parameters
                currentParameters = boardManager.ExtractParametersFromFQBN(defaultFQBN)

                ' Update UI controls to match default values
                For Each paramName In parameterControls.Keys
                    Dim control = parameterControls(paramName)

                    ' Ensure parameter exists before accessing
                    If Not currentParameters.ContainsKey(paramName) Then
                        ' Set default value based on parameter type
                        If paramName = "PSRAM" Then
                            currentParameters(paramName) = "disabled"
                        ElseIf paramName = "DebugLevel" Then
                            currentParameters(paramName) = "none"
                        ElseIf paramName = "CPUFreq" Then
                            currentParameters(paramName) = "240"
                        ElseIf paramName = "FlashMode" Then
                            currentParameters(paramName) = "dio"
                        ElseIf paramName = "FlashFreq" Then
                            currentParameters(paramName) = "80"
                        ElseIf paramName = "PartitionScheme" Then
                            currentParameters(paramName) = "default"
                        ElseIf paramName = "UploadSpeed" Then
                            currentParameters(paramName) = "921600"
                        End If
                    End If

                    Dim paramValue = currentParameters(paramName)

                    If TypeOf control Is ComboBox Then
                        Dim comboBox = DirectCast(control, ComboBox)
                        For i As Integer = 0 To comboBox.Items.Count - 1
                            Dim item = DirectCast(comboBox.Items(i), KeyValuePair(Of String, String))
                            If item.Key = paramValue Then
                                comboBox.SelectedIndex = i
                                Exit For
                            End If
                        Next

                        ' If no match found, select first item
                        If comboBox.SelectedIndex < 0 AndAlso comboBox.Items.Count > 0 Then
                            comboBox.SelectedIndex = 0
                            ' Update parameter with selected value
                            Dim item = DirectCast(comboBox.Items(0), KeyValuePair(Of String, String))
                            currentParameters(paramName) = item.Key
                        End If
                    ElseIf TypeOf control Is CheckBox Then
                        Dim checkBox = DirectCast(control, CheckBox)
                        checkBox.Checked = (paramValue = "enabled")
                    End If
                Next

                ' Update FQBN
                UpdateFQBN()
                UpdateFQBNText()

                ' Log the reset operation
                Debug.WriteLine($"[2025-08-12 13:15:24] Board configuration reset to defaults for {boardName} by Chamil1983")

                MessageBox.Show("Board configuration has been reset to default values.", "Reset Complete",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error resetting defaults: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in btnResetDefaults_Click: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        Try
            ' Make sure parameters are up-to-date
            UpdateParametersFromUI()

            ' Save board configuration
            boardManager.UpdateBoardConfiguration(boardName, currentParameters)

            ' Save partition scheme to user settings for main form sync
            If currentParameters.ContainsKey("PartitionScheme") Then
                My.Settings.LastUsedPartition = currentParameters("PartitionScheme")
                My.Settings.Save()
            End If

            ' Show success message
            MessageBox.Show($"Board configuration for {boardName} has been saved successfully.",
                          "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Log the save operation
            Debug.WriteLine($"[2025-08-12 13:15:24] Board configuration saved for {boardName} by Chamil1983")
        Catch ex As Exception
            MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in btnSave_Click: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub btnReload_Click(sender As Object, e As EventArgs)
        Try
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

            ' Log the reload operation
            Debug.WriteLine($"[2025-08-12 13:15:24] Board configurations reloaded by Chamil1983")
        Catch ex As Exception
            MessageBox.Show($"Error reloading configurations: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in btnReload_Click: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub cmbBoardType_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            ' Load configuration for the selected board
            If cmbBoardType.SelectedItem IsNot Nothing Then
                boardName = cmbBoardType.SelectedItem.ToString()
                LoadBoardConfiguration()

                ' Log the selection change
                Debug.WriteLine($"[2025-08-12 13:15:24] Selected board changed to {boardName} by Chamil1983")
            End If
        Catch ex As Exception
            MessageBox.Show($"Error changing board selection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in cmbBoardType_SelectedIndexChanged: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub tabControl_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            If tabControl.SelectedTab Is tabFQBN Then
                ' Update FQBN text when switching to FQBN tab
                UpdateFQBNText()
            End If
        Catch ex As Exception
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in tabControl_SelectedIndexChanged: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub btnSaveFQBN_Click(sender As Object, e As EventArgs)
        Try
            ' Get the FQBN from text box
            Dim fqbn As String = txtFQBN.Text

            ' Extract only the FQBN part if the text contains multiple lines
            If fqbn.Contains(Environment.NewLine) Then
                Dim lines = fqbn.Split(New String() {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                For Each line In lines
                    If line.Contains(":") AndAlso Not line.StartsWith("Board:") AndAlso Not line.StartsWith("FQBN:") AndAlso
                       Not line.StartsWith("-") AndAlso Not line.StartsWith("You can") AndAlso Not line.StartsWith("Current") Then
                        fqbn = line.Trim()
                        Exit For
                    End If
                Next
            End If

            ' Update current FQBN
            currentFQBN = fqbn

            ' Extract parameters from the new FQBN
            currentParameters = boardManager.ExtractParametersFromFQBN(currentFQBN)

            ' Recreate UI to reflect new parameters
            CreateConfigurationUI()

            ' Switch to Board Configuration tab
            tabControl.SelectedTab = tabBoardConfig

            ' Log the FQBN change
            Debug.WriteLine($"[2025-08-12 13:15:24] FQBN manually updated to {currentFQBN} by Chamil1983")

            MessageBox.Show("FQBN applied successfully. Please review the settings in the Board Configuration tab.",
                           "FQBN Applied", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show($"Error applying FQBN: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in btnSaveFQBN_Click: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub btnBrowseBoardsFile_Click(sender As Object, e As EventArgs)
        Try
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Title = "Select boards.txt File"
                openFileDialog.Filter = "boards.txt|boards.txt|Text Files|*.txt|All Files|*.*"
                openFileDialog.FileName = "boards.txt"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtBoardsFile.Text = openFileDialog.FileName
                    boardManager.BoardsFilePath = openFileDialog.FileName

                    ' Log the change
                    Debug.WriteLine($"[2025-08-12 13:15:24] boards.txt path updated to {openFileDialog.FileName} by Chamil1983")

                    ' Ask if user wants to reload configurations
                    If MessageBox.Show("Would you like to reload board configurations from the selected file?",
                                    "Reload Configurations", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                        ReloadBoardsFile()
                    End If
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error selecting boards file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in btnBrowseBoardsFile_Click: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Private Sub btnReloadBoardsFile_Click(sender As Object, e As EventArgs)
        ReloadBoardsFile()
    End Sub

    Private Sub ReloadBoardsFile()
        Try
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

            Debug.WriteLine($"[2025-08-12 13:15:24] Board configurations reloaded from {boardManager.BoardsFilePath} by Chamil1983")
        Catch ex As Exception
            MessageBox.Show($"Error reloading boards file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[2025-08-12 13:15:24] Error in ReloadBoardsFile: {ex.Message} by Chamil1983")
        End Try
    End Sub
End Class