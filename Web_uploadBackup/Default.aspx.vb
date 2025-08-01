Imports System.IO
Imports System.IO.Compression
Imports System.IO.Ports
Imports ArduinoWeb
Imports System.Web.Services
Imports System.Web.Script.Services
Imports System.Text
Imports System.Threading

Public Class [Default]
    Inherits System.Web.UI.Page

    Private Shared ReadOnly boardManager As New BoardManager()

    ' Default Arduino ESP32 boards.txt path
    Private Const DEFAULT_BOARDS_TXT_PATH As String = "C:\Users\gen_rms_testroom\AppData\Local\arduino15\packages\esp32\hardware\esp32\3.2.0\boards.txt"

    Protected ReadOnly Property IsUsingBoardsTxt As Boolean
        Get
            Return boardManager.IsUsingBoardsTxt()
        End Get
    End Property

    Protected ReadOnly Property HasCustomPartition As Boolean
        Get
            Return boardManager.HasCustomPartition
        End Get
    End Property

    Private Property ArduinoCliPath As String
        Get
            Return If(Session("CliPath"), "")
        End Get
        Set(value As String)
            Session("CliPath") = value
        End Set
    End Property

    ' Store current board configuration options
    Private Property BoardConfigOptions As Dictionary(Of String, String)
        Get
            If Session("BoardConfigOptions") Is Nothing Then
                Session("BoardConfigOptions") = New Dictionary(Of String, String)()
            End If
            Return CType(Session("BoardConfigOptions"), Dictionary(Of String, String))
        End Get
        Set(value As Dictionary(Of String, String))
            Session("BoardConfigOptions") = value
        End Set
    End Property

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ' Set default boards.txt path
            If String.IsNullOrEmpty(txtBoardsTxtPath.Text) Then
                txtBoardsTxtPath.Text = DEFAULT_BOARDS_TXT_PATH
                ' Try to automatically load the boards.txt
                If File.Exists(DEFAULT_BOARDS_TXT_PATH) Then
                    ImportBoardsTxtFromPath(DEFAULT_BOARDS_TXT_PATH)
                End If
            End If

            boardManager.LoadCustomPartitions()
            PopulateBoards()
            PopulatePartitions()

            ' Set the default flash size to 4M for ESP32 (very important to prevent reset issues)
            SetDefaultBoardConfigurations(ddlBoard.SelectedValue)

            ' Override specific settings for flash size
            If BoardConfigOptions.ContainsKey("FlashSize") Then
                BoardConfigOptions("FlashSize") = "4M"  ' Make sure flash size is 4MB
            End If

            ' Set flash mode to DIO based on boot log
            If BoardConfigOptions.ContainsKey("FlashMode") Then
                BoardConfigOptions("FlashMode") = "dio"  ' Boot log shows mode:DIO
            End If

            PopulateSerialPorts()
            PopulateBoardOptions()

            ' Set default partition scheme to "min_spiffs" which is safer for 4MB flash
            If ddlPartition.Items.FindByValue("min_spiffs") IsNot Nothing Then
                ddlPartition.SelectedValue = "min_spiffs"  ' This partition scheme works better with 4MB flash
            ElseIf ddlPartition.Items.Count > 0 Then
                ddlPartition.SelectedIndex = 0 ' Select first partition as fallback
            End If

            txtOutput.Text = "Ready."
            lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-info-circle'></i> Status: Ready</span>"
            If Not String.IsNullOrEmpty(ArduinoCliPath) Then
                txtCliPath.Text = ArduinoCliPath
            End If
            UpdatePartitionCount()
            ShowBoardsTxtStatus()
            ShowCustomPartitionStatus()
            UpdateFQBNPreview()
        End If
    End Sub

    ' Import boards.txt from specified path
    Private Function ImportBoardsTxtFromPath(boardsTxtPath As String) As Boolean
        If File.Exists(boardsTxtPath) Then
            ' Copy the boards.txt to server App_Data path
            Dim appData = Server.MapPath("~/App_Data")
            If Not Directory.Exists(appData) Then Directory.CreateDirectory(appData)
            Dim destBoardsTxt = Path.Combine(appData, "boards.txt")
            File.Copy(boardsTxtPath, destBoardsTxt, True)

            ' Import partition schemes and board options
            Dim result = boardManager.ImportFromBoardsTxt(destBoardsTxt)
            Return result.Success
        End If
        Return False
    End Function

    ' Set defaults for board config options for given board
    Private Sub SetDefaultBoardConfigurations(boardName As String)
        BoardConfigOptions.Clear()
        Dim options = boardManager.GetBoardOptions(boardName)
        If options IsNot Nothing Then
            For Each opt In options
                Dim def = boardManager.GetOptionDefault(boardName, opt.Key)
                BoardConfigOptions(opt.Key) = def
            Next
        End If

        ' Force specific settings that we know are correct for your hardware
        BoardConfigOptions("FlashSize") = "4M"       ' Set flash size to 4MB
        BoardConfigOptions("FlashMode") = "dio"      ' Set flash mode to DIO

        Session("BoardConfigOptions") = BoardConfigOptions
    End Sub

    Private Sub ShowBoardsTxtStatus()
        If boardManager.IsUsingBoardsTxt() Then
            lblBoardsTxtStatus.Text = String.Format("<i class='fas fa-check-circle'></i> <span class='success'>Using boards.txt partitions: {0}</span>", boardManager.BoardsTxtPath)
            boardsTxtStatusPanel.Visible = True
            btnClearBoardsTxt.Visible = True
        Else
            boardsTxtStatusPanel.Visible = False
            btnClearBoardsTxt.Visible = False
        End If
    End Sub

    Private Sub ShowCustomPartitionStatus()
        Dim showPanel As Boolean = False
        Dim partitionName = ddlPartition.SelectedItem?.Text

        If String.IsNullOrEmpty(partitionName) Then
            partitionName = ddlPartition.SelectedValue
        End If

        If partitionName?.ToLower() = "custom" Or HasCustomPartition Then
            showPanel = True

            If partitionName?.ToLower() = "custom" Then
                If HasCustomPartition Then
                    lblCustomPartitionStatus.Text = String.Format("<i class='fas fa-check-circle'></i> <span class='success'>Using custom partition scheme: {0}</span>", partitionName)
                Else
                    lblCustomPartitionStatus.Text = String.Format("<i class='fas fa-exclamation-triangle'></i> <span class='fail'>Warning: {0} partition selected but no partitions.csv file imported</span>", partitionName)
                End If
            ElseIf HasCustomPartition Then
                lblCustomPartitionStatus.Text = String.Format("<i class='fas fa-info-circle'></i> <span class='info'>Custom partition is available but {0} scheme is selected</span>", partitionName)
            End If
        End If

        customPartitionStatusPanel.Visible = showPanel
        btnClearCustom.Visible = HasCustomPartition
    End Sub

    Private Sub UpdatePartitionCount()
        Dim selectedScheme = ddlPartition.SelectedValue
        Dim selectedSchemeText = ddlPartition.SelectedItem?.Text

        Dim countMessage As String = boardManager.GetPartitionCount()

        If Not String.IsNullOrEmpty(selectedScheme) Then
            Dim schemeDetails = boardManager.GetPartitionDetails(selectedScheme)
            If Not String.IsNullOrEmpty(schemeDetails) Then
                countMessage = String.Format("<span class='info'><i class='fas fa-info-circle'></i> <strong>{0}</strong>: {1}</span>", selectedSchemeText, schemeDetails)
            ElseIf selectedScheme.ToLower() = "custom" AndAlso HasCustomPartition Then
                countMessage = "<span class='success'><i class='fas fa-check-circle'></i> Using custom partition scheme</span>"
            End If
        End If

        litPartitionCount.Text = countMessage
    End Sub

    Private Sub PopulateBoards()
        ddlBoard.Items.Clear()
        ddlBoard.Items.Add(New ListItem("ESP32 Dev Module", "ESP32 Dev Module"))
        ddlBoard.Items.Add(New ListItem("ESP32-S2", "ESP32-S2"))
        ddlBoard.Items.Add(New ListItem("ESP32-C3", "ESP32-C3"))
        ddlBoard.Items.Add(New ListItem("ESP32-S3", "ESP32-S3"))
        ddlBoard.Items.Add(New ListItem("ESP32 Wrover Kit", "ESP32 Wrover Kit"))
        ddlBoard.Items.Add(New ListItem("ESP32 Pico Kit", "ESP32 Pico Kit"))
        ddlBoard.Items.Add(New ListItem("ESP32-C6", "ESP32-C6"))
        ddlBoard.Items.Add(New ListItem("ESP32-H2", "ESP32-H2"))
        For Each board In boardManager.GetBoardNames()
            If Not ddlBoard.Items.Contains(New ListItem(board, board)) Then
                ddlBoard.Items.Add(board)
            End If
        Next
        ddlBoard.SelectedIndex = 0
    End Sub

    Protected Sub ddlBoard_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' When board changes, reset to default config for that board
        SetDefaultBoardConfigurations(ddlBoard.SelectedValue)
        PopulatePartitions()
        PopulateBoardOptions()
        UpdateFQBNPreview()
    End Sub

    Private Sub PopulatePartitions()
        Dim currentSelection = If(ddlPartition.SelectedValue, "")
        ddlPartition.Items.Clear()
        Dim board As String = ddlBoard.SelectedValue
        ddlPartition.Items.Add(New ListItem("Default", "default"))
        ddlPartition.Items.Add(New ListItem("Huge App", "huge_app"))
        ddlPartition.Items.Add(New ListItem("Minimal SPIFFS", "min_spiffs"))
        ddlPartition.Items.Add(New ListItem("No OTA", "no_ota"))
        Dim options = boardManager.GetPartitionOptions(board)
        For Each part In options
            If Not ddlPartition.Items.Contains(New ListItem(part, part)) Then
                ddlPartition.Items.Add(part)
            End If
        Next

        ' Choose appropriate partition scheme for this device based on the flash size
        If Not String.IsNullOrEmpty(currentSelection) AndAlso ddlPartition.Items.FindByValue(currentSelection) IsNot Nothing Then
            ddlPartition.SelectedValue = currentSelection
        ElseIf ddlPartition.Items.FindByValue("min_spiffs") IsNot Nothing Then
            ' min_spiffs is generally safe for 4MB flash
            ddlPartition.SelectedValue = "min_spiffs"
        ElseIf HasCustomPartition AndAlso ddlPartition.Items.FindByValue("custom") IsNot Nothing Then
            ddlPartition.SelectedValue = "custom"
        ElseIf ddlPartition.Items.Count > 0 Then
            ddlPartition.SelectedIndex = 0
        End If

        UpdatePartitionCount()
        ShowCustomPartitionStatus()
    End Sub

    Private Sub PopulateBoardOptions()
        ' Clear the placeholder first
        plhBoardOptions.Controls.Clear()

        ' Get board name and options
        Dim boardName = ddlBoard.SelectedValue
        If String.IsNullOrEmpty(boardName) Then Return

        ' Get options for this board
        Dim options = boardManager.GetBoardOptions(boardName)
        If options Is Nothing OrElse options.Count = 0 Then
            ' Create a message if no options available
            Dim noOptionsMsg As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
            noOptionsMsg.Attributes("class") = "board-config-container"
            noOptionsMsg.InnerHtml = $"<div class='info-box'><i class='fas fa-info-circle'></i> No configuration options available for {boardName}. Try importing a boards.txt file.</div>"
            plhBoardOptions.Controls.Add(noOptionsMsg)
            Return
        End If

        ' Store base FQBN
        hidBaseFQBN.Value = boardManager.GetFQBN(boardName, "", "")

        ' Create the container for all board options
        Dim configContainer As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        configContainer.Attributes("class") = "board-config-container"

        ' Add heading
        Dim heading As New System.Web.UI.HtmlControls.HtmlGenericControl("h3")
        heading.Attributes("class") = "board-options-title"
        heading.InnerHtml = String.Format("<i class='fas fa-microchip'></i> {0} Configuration", boardName)
        configContainer.Controls.Add(heading)

        ' Add description
        Dim description As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        description.Attributes("class") = "board-options-description"
        description.InnerText = "Configure ESP32 board settings similar to Arduino IDE"
        configContainer.Controls.Add(description)

        ' Create option groups container
        Dim optionsContainer As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        optionsContainer.Attributes("class") = "board-options-grid"
        configContainer.Controls.Add(optionsContainer)

        ' Add each option to the container
        For Each optKvp As KeyValuePair(Of String, Dictionary(Of String, String)) In options
            ' Create option card
            Dim optionCard As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
            optionCard.Attributes("class") = "board-option-card"

            ' Create option header
            Dim optionHeader As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
            optionHeader.Attributes("class") = "board-option-header"

            ' Get option name and description
            Dim optionName = boardManager.GetOptionName(optKvp.Key)
            Dim optionDesc = boardManager.GetOptionDescription(optKvp.Key)

            ' Create option title
            Dim optionTitle As New System.Web.UI.HtmlControls.HtmlGenericControl("h4")
            optionTitle.InnerText = optionName
            optionHeader.Controls.Add(optionTitle)

            ' Add description if available
            If Not String.IsNullOrEmpty(optionDesc) Then
                Dim optionDescElem As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
                optionDescElem.Attributes("class") = "board-option-desc"
                optionDescElem.InnerText = optionDesc
                optionHeader.Controls.Add(optionDescElem)
            End If

            optionCard.Controls.Add(optionHeader)

            ' Create option content
            Dim optionContent As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
            optionContent.Attributes("class") = "board-option-content"

            ' Create dropdown for option
            Dim dropdown As New System.Web.UI.WebControls.DropDownList()
            dropdown.ID = "ddlOption_" + optKvp.Key
            dropdown.CssClass = "form-control"
            dropdown.ClientIDMode = System.Web.UI.ClientIDMode.Static
            dropdown.Attributes("onchange") = "updateFQBNPreview();"

            ' Add default option
            dropdown.Items.Add(New ListItem("Default", "default"))

            ' Get default value for this option
            Dim defaultValue = boardManager.GetOptionDefault(boardName, optKvp.Key)

            ' Add options from the board manager
            For Each valueKvp As KeyValuePair(Of String, String) In optKvp.Value
                Dim isDefault = (valueKvp.Key = defaultValue)
                Dim displayText = valueKvp.Value
                If isDefault Then displayText += " (Default)"
                dropdown.Items.Add(New ListItem(displayText, valueKvp.Key))
            Next

            ' Determine which value to select
            Dim valueToSelect As String = "default"
            If BoardConfigOptions.ContainsKey(optKvp.Key) Then
                valueToSelect = BoardConfigOptions(optKvp.Key)
            Else
                valueToSelect = defaultValue
                BoardConfigOptions(optKvp.Key) = defaultValue
            End If

            ' Special handling for Flash Size and Flash Mode based on boot log
            If optKvp.Key = "FlashSize" Then
                valueToSelect = "4M" ' Force 4MB flash size
            ElseIf optKvp.Key = "FlashMode" Then
                valueToSelect = "dio" ' Force DIO mode from boot log
            End If

            ' Select the appropriate value in dropdown
            If dropdown.Items.FindByValue(valueToSelect) IsNot Nothing Then
                dropdown.SelectedValue = valueToSelect
            Else
                ' Fallback to default if the specific value isn't available
                dropdown.SelectedValue = "default"
                BoardConfigOptions(optKvp.Key) = "default"
            End If

            optionContent.Controls.Add(dropdown)
            optionCard.Controls.Add(optionContent)
            optionsContainer.Controls.Add(optionCard)
        Next

        ' Add FQBN preview
        Dim fqbnPreviewContainer As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        fqbnPreviewContainer.Attributes("class") = "fqbn-preview-container"

        Dim fqbnPreviewLabel As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        fqbnPreviewLabel.Attributes("class") = "fqbn-preview-label"
        fqbnPreviewLabel.InnerHtml = "<strong>Full Qualified Board Name (FQBN):</strong>"
        fqbnPreviewContainer.Controls.Add(fqbnPreviewLabel)

        Dim fqbnPreviewValue As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        fqbnPreviewValue.Attributes("class") = "fqbn-preview-value"
        fqbnPreviewValue.ID = "fqbnPreview"

        ' Generate FQBN with current board options
        Dim fullFQBN = boardManager.GetFQBN(boardName, ddlPartition.SelectedValue, "", BoardConfigOptions)
        fqbnPreviewValue.InnerText = fullFQBN
        fqbnPreviewContainer.Controls.Add(fqbnPreviewValue)
        configContainer.Controls.Add(fqbnPreviewContainer)

        ' Add the main container to the page
        plhBoardOptions.Controls.Add(configContainer)

        ' Store board options in session
        Session("BoardConfigOptions") = BoardConfigOptions

        ' Also update hidFullFQBN to store the complete FQBN
        hidFullFQBN.Value = fullFQBN
    End Sub

    ' Reads all board option dropdowns and updates BoardConfigOptions
    Private Sub CaptureBoardOptionsFromUI()
        ' Create a new dictionary to avoid shared references
        Dim options As New Dictionary(Of String, String)

        ' Process all dropdowns in the board options section
        For Each ctrl As Control In plhBoardOptions.Controls
            If TypeOf ctrl Is System.Web.UI.HtmlControls.HtmlGenericControl Then
                Dim container = DirectCast(ctrl, System.Web.UI.HtmlControls.HtmlGenericControl)
                FindAndProcessDropdowns(container, options)
            End If
        Next

        ' Always force these settings to prevent ESP32 reset issues
        options("FlashSize") = "4M"  ' Force 4MB flash size
        options("FlashMode") = "dio"  ' Force DIO mode from boot log

        ' Update BoardConfigOptions with the new values
        BoardConfigOptions = options

        ' Update session with the new values
        Session("BoardConfigOptions") = options
    End Sub

    Protected Sub ddlPartition_SelectedIndexChanged(sender As Object, e As EventArgs)
        UpdatePartitionCount()
        ShowCustomPartitionStatus()
        UpdateFQBNPreview()
    End Sub

    Private Sub UpdateFQBNPreview()
        ' Get current board and partition
        Dim boardName = ddlBoard.SelectedValue
        Dim partitionScheme = ddlPartition.SelectedValue

        ' Capture current board options from UI
        CaptureBoardOptionsFromUI()

        ' Generate FQBN with current settings
        Dim fullFQBN = boardManager.GetFQBN(boardName, partitionScheme, "", BoardConfigOptions)

        ' Update FQBN preview in UI
        Dim fqbnPreview = FindControlRecursive(plhBoardOptions, "fqbnPreview")
        If fqbnPreview IsNot Nothing AndAlso TypeOf fqbnPreview Is System.Web.UI.HtmlControls.HtmlGenericControl Then
            DirectCast(fqbnPreview, System.Web.UI.HtmlControls.HtmlGenericControl).InnerText = fullFQBN
        End If

        ' Update the hidden field for FQBN and literal
        litFQBN.Text = fullFQBN
        hidFullFQBN.Value = fullFQBN

        ' Update session with current board options
        Session("BoardConfigOptions") = BoardConfigOptions

        ' Log the FQBN for debugging
        Dim logEntry = $"Updated FQBN: {fullFQBN} with options: {String.Join(", ", BoardConfigOptions.Select(Function(kvp) $"{kvp.Key}={kvp.Value}"))}"
        AppendToLog(logEntry)
    End Sub

    ' Log message to a file for debugging
    Private Sub AppendToLog(message As String)
        Try
            Dim logPath = Server.MapPath("~/App_Data/debug.log")
            Using writer As New StreamWriter(logPath, True)
                writer.WriteLine($"{DateTime.Now}: {message}")
            End Using
        Catch
            ' Ignore errors in logging
        End Try
    End Sub

    Private Sub FindAndProcessDropdowns(container As Control, optionsDict As Dictionary(Of String, String))
        For Each ctrl As Control In container.Controls
            If TypeOf ctrl Is DropDownList Then
                Dim dropdown = DirectCast(ctrl, DropDownList)
                If dropdown.ID IsNot Nothing AndAlso dropdown.ID.StartsWith("ddlOption_") Then
                    Dim optionName = dropdown.ID.Replace("ddlOption_", "")
                    optionsDict(optionName) = dropdown.SelectedValue
                End If
            ElseIf ctrl.HasControls() Then
                FindAndProcessDropdowns(ctrl, optionsDict)
            End If
        Next
    End Sub

    Private Function FindControlRecursive(root As Control, id As String) As Control
        If root.ID = id Then Return root
        For Each ctrl As Control In root.Controls
            Dim found = FindControlRecursive(ctrl, id)
            If found IsNot Nothing Then Return found
        Next
        Return Nothing
    End Function

    Private Sub PopulateSerialPorts()
        ddlSerial.Items.Clear()
        Try
            For Each port In SerialPort.GetPortNames()
                ddlSerial.Items.Add(port)
            Next
            If ddlSerial.Items.Count > 0 Then ddlSerial.SelectedIndex = 0
        Catch ex As Exception
            ddlSerial.Items.Add("No Ports")
        End Try
    End Sub

    Private Function GetValidSketchFolder(ByRef errorMsg As String) As String
        Dim userInput = txtProjectDir.Text.Trim()
        errorMsg = ""
        If String.IsNullOrWhiteSpace(userInput) Then
            errorMsg = "Please enter the path to a sketch folder or a .ino file."
            Return Nothing
        End If
        Dim projDir As String = userInput
        If File.Exists(projDir) AndAlso projDir.ToLower().EndsWith(".ino") Then
            projDir = Path.GetDirectoryName(projDir)
        End If
        If Not Directory.Exists(projDir) Then
            errorMsg = "Project folder does not exist on server."
            Return Nothing
        End If
        Dim folderName = Path.GetFileName(projDir)
        Dim sketchFile = Path.Combine(projDir, folderName & ".ino")
        If Not File.Exists(sketchFile) Then
            Dim inoFiles = Directory.GetFiles(projDir, "*.ino")
            If inoFiles.Length = 0 Then
                errorMsg = String.Format("No .ino file found in the folder '{0}'.", projDir)
                Return Nothing
            Else
                Try
                    File.Copy(inoFiles(0), sketchFile, True)
                Catch ex As Exception
                    errorMsg = String.Format("Failed to prepare sketch: {0}", ex.Message)
                    Return Nothing
                End Try
            End If
        End If
        Return projDir
    End Function

    Private Property OutputBuffer As List(Of String)
        Get
            If Session("OutputBuffer") Is Nothing Then
                Session("OutputBuffer") = New List(Of String)()
            End If
            Return CType(Session("OutputBuffer"), List(Of String))
        End Get
        Set(value As List(Of String))
            Session("OutputBuffer") = value
        End Set
    End Property

    Protected Sub btnCompile_Click(sender As Object, e As EventArgs)
        ' Capture and save the latest board configurations before compiling
        CaptureBoardOptionsFromUI()
        UpdateFQBNPreview()

        ' Get current partition scheme selection
        Dim selectedScheme = ddlPartition.SelectedValue
        Dim selectedSchemeText = ddlPartition.SelectedItem?.Text

        ' Validate sketch folder
        Dim err As String = ""
        Dim projDir = GetValidSketchFolder(err)
        If projDir Is Nothing Then
            txtOutput.Text = err
            Return
        End If

        ' Get selected board and partition scheme
        Dim board = ddlBoard.SelectedValue
        Dim schemeValue = selectedScheme

        ' Validate CLI path
        If String.IsNullOrWhiteSpace(txtCliPath.Text) Then
            txtOutput.Text = "Please set the Arduino CLI path."
            Return
        End If
        ArduinoCliPath = txtCliPath.Text.Trim()

        ' Validate partition scheme
        If String.IsNullOrEmpty(schemeValue) OrElse Not boardManager.PartitionExists(schemeValue) Then
            txtOutput.Text = String.Format("Error: Selected partition scheme '{0}' not found. Please select a valid partition scheme.", schemeValue)
            Return
        End If

        ' Generate FQBN with all board options
        Dim fqbn = boardManager.GetFQBN(board, schemeValue, projDir, BoardConfigOptions)

        ' Log the FQBN being used for compilation
        AppendToLog($"Compiling with FQBN: {fqbn}")

        ' Clear output buffer and textbox
        OutputBuffer = New List(Of String)()
        txtOutput.Text = ""

        ' Update status to indicate compilation started
        lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Compiling...</span>"

        ' Show the FQBN being used in output
        OutputBuffer.Add("Executing task: arduino-cli compile")
        OutputBuffer.Add(String.Format("Using FQBN: {0}", fqbn))
        txtOutput.Text = String.Join(Environment.NewLine, OutputBuffer)

        ' Run compile with real-time output
        Dim result As ArduinoUtil.ExecResult =
            ArduinoUtil.RunCompileRealtime(ArduinoCliPath, projDir, fqbn,
                Sub(line)
                    OutputBuffer.Add(line)
                    txtOutput.Text = String.Join(Environment.NewLine, OutputBuffer)
                End Sub)

        ' Update status based on result
        If result.Success Then
            OutputBuffer.Add("Compiled OK")
            lblStatus.Text = "<span class='status-indicator success'><i class='fas fa-check-circle'></i> Compilation Successful</span>"
        Else
            OutputBuffer.Add("Compile failed")
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Compilation Failed</span>"
        End If

        ' Update output text
        txtOutput.Text = String.Join(Environment.NewLine, OutputBuffer)

        ' Make sure partition dropdown reflects what we just compiled with
        If ddlPartition.Items.FindByValue(selectedScheme) IsNot Nothing Then
            ddlPartition.SelectedValue = selectedScheme
        End If

        ' Update status panel with what we just compiled with
        ShowCustomPartitionStatus()
    End Sub

    Protected Sub btnUpload_Click(sender As Object, e As EventArgs)
        ' Capture and save the latest board configurations before uploading
        CaptureBoardOptionsFromUI()
        UpdateFQBNPreview()

        ' Get current partition scheme selection
        Dim selectedScheme = ddlPartition.SelectedValue
        Dim selectedSchemeText = ddlPartition.SelectedItem?.Text

        ' Validate sketch folder
        Dim err As String = ""
        Dim projDir = GetValidSketchFolder(err)
        If projDir Is Nothing Then
            txtOutput.Text = err
            Return
        End If

        ' Get selected board, partition scheme, and port
        Dim board = ddlBoard.SelectedValue
        Dim schemeValue = selectedScheme
        Dim serialPort = ddlSerial.SelectedValue

        ' Validate serial port
        If String.IsNullOrEmpty(serialPort) Then
            txtOutput.Text = "No serial port available."
            Return
        End If

        ' Validate CLI path
        If String.IsNullOrWhiteSpace(txtCliPath.Text) Then
            txtOutput.Text = "Please set the Arduino CLI path."
            Return
        End If
        ArduinoCliPath = txtCliPath.Text.Trim()

        ' Validate partition scheme
        If String.IsNullOrEmpty(schemeValue) OrElse Not boardManager.PartitionExists(schemeValue) Then
            txtOutput.Text = String.Format("Error: Selected partition scheme '{0}' not found. Please select a valid partition scheme.", schemeValue)
            Return
        End If

        ' Generate FQBN with all board options
        Dim fqbn = boardManager.GetFQBN(board, schemeValue, projDir, BoardConfigOptions)

        ' Log the FQBN being used for upload
        AppendToLog($"Uploading with FQBN: {fqbn}")

        ' Clear output buffer and textbox
        OutputBuffer = New List(Of String)()
        txtOutput.Text = ""

        ' Update status to indicate upload started
        lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Uploading...</span>"

        ' Show the FQBN being used in output
        OutputBuffer.Add("Executing task: arduino-cli compile")
        OutputBuffer.Add(String.Format("Using FQBN: {0}", fqbn))
        txtOutput.Text = String.Join(Environment.NewLine, OutputBuffer)

        ' Run upload with real-time output
        Dim result As ArduinoUtil.ExecResult =
            ArduinoUtil.RunUploadRealtime(ArduinoCliPath, projDir, fqbn, serialPort,
                Sub(line)
                    OutputBuffer.Add(line)
                    txtOutput.Text = String.Join(Environment.NewLine, OutputBuffer)
                End Sub)

        ' Update status based on result
        If result.Success Then
            OutputBuffer.Add("Upload OK")
            lblStatus.Text = "<span class='status-indicator success'><i class='fas fa-check-circle'></i> Upload Successful</span>"
        Else
            OutputBuffer.Add("Upload failed")
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Upload Failed</span>"
        End If

        ' Update output text
        txtOutput.Text = String.Join(Environment.NewLine, OutputBuffer)

        ' Make sure partition dropdown reflects what we just uploaded with
        If ddlPartition.Items.FindByValue(selectedScheme) IsNot Nothing Then
            ddlPartition.SelectedValue = selectedScheme
        End If

        ' Update status panel with what we just uploaded with
        ShowCustomPartitionStatus()
    End Sub

    Protected Sub btnUploadSketch_Click(sender As Object, e As EventArgs)
        If Not fuSketchZip.HasFile Then
            lblUploadResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> Please select a .zip file</span>"
            Return
        End If
        Dim ext = Path.GetExtension(fuSketchZip.FileName).ToLower()
        If ext <> ".zip" Then
            lblUploadResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> Only .zip files are allowed</span>"
            Return
        End If
        Dim tempRoot = Server.MapPath("~/UploadedSketches/")
        If Not Directory.Exists(tempRoot) Then Directory.CreateDirectory(tempRoot)
        Dim zipPath = Path.Combine(tempRoot, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) & ".zip")
        fuSketchZip.SaveAs(zipPath)
        Dim destDir = Path.Combine(tempRoot, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()))
        Directory.CreateDirectory(destDir)
        Try
            ZipFile.ExtractToDirectory(zipPath, destDir)
            Dim inoFiles = Directory.GetFiles(destDir, "*.ino", SearchOption.AllDirectories)
            If inoFiles.Length = 0 Then
                lblUploadResult.Text = "<span class='badge badge-danger'><i class='fas fa-times-circle'></i> No .ino file found in uploaded ZIP</span>"
                Return
            End If
            Dim sketchFolder = Path.GetDirectoryName(inoFiles(0))
            Dim partitionFiles = Directory.GetFiles(sketchFolder, "partitions.csv", SearchOption.AllDirectories)
            If partitionFiles.Length > 0 Then
                lblUploadResult.Text = "<span class='badge badge-success'><i class='fas fa-check-circle'></i> Uploaded and extracted. Partitions.csv found in sketch</span>"
                boardManager.SetCustomPartitionsCsvFile(partitionFiles(0))
                PopulatePartitions()
                If ddlPartition.Items.FindByValue("custom") IsNot Nothing Then
                    ddlPartition.SelectedValue = "custom"
                End If
                UpdatePartitionCount()
                ShowCustomPartitionStatus()
            Else
                lblUploadResult.Text = "<span class='badge badge-success'><i class='fas fa-check-circle'></i> Uploaded and extracted. You may now compile</span>"
            End If
            txtProjectDir.Text = sketchFolder
        Catch ex As Exception
            lblUploadResult.Text = String.Format("<span class='badge badge-danger'><i class='fas fa-times-circle'></i> Extraction failed: {0}</span>", ex.Message)
        End Try
        Try
            File.Delete(zipPath)
        Catch
        End Try
    End Sub

    Protected Sub btnUploadSingleSketch_Click(sender As Object, e As EventArgs)
        lblSketchFileResult.Text = ""
        If Not fuSingleSketch.HasFile Then
            lblSketchFileResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> Please choose a .ino file to upload</span>"
            Return
        End If
        Dim ext = Path.GetExtension(fuSingleSketch.FileName).ToLower()
        If ext <> ".ino" Then
            lblSketchFileResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> Only .ino files are allowed</span>"
            Return
        End If
        Dim tempRoot = Server.MapPath("~/UploadedSketches/")
        If Not Directory.Exists(tempRoot) Then Directory.CreateDirectory(tempRoot)
        Dim sketchFileName = Path.GetFileName(fuSingleSketch.FileName)
        Dim sketchFolderName = Path.GetFileNameWithoutExtension(fuSingleSketch.FileName)
        Dim sketchFolder = Path.Combine(tempRoot, sketchFolderName)
        If Not Directory.Exists(sketchFolder) Then Directory.CreateDirectory(sketchFolder)
        Dim destPath = Path.Combine(sketchFolder, sketchFileName)
        fuSingleSketch.SaveAs(destPath)
        txtSketchFile.Text = destPath
        txtProjectDir.Text = sketchFolder
        lblSketchFileResult.Text = "<span class='badge badge-success'><i class='fas fa-check-circle'></i> Sketch file uploaded. Sketch folder set for compile</span>"
    End Sub

    Protected Sub btnRefreshSerial_Click(sender As Object, e As EventArgs)
        PopulateSerialPorts()
        txtOutput.Text = "Serial port list refreshed."
    End Sub

    Protected Sub btnClearBoardsTxt_Click(sender As Object, e As EventArgs)
        boardManager.ClearBoardsTxtPartitions()
        boardsTxtStatusPanel.Visible = False
        btnClearBoardsTxt.Visible = False
        PopulatePartitions()
        PopulateBoardOptions()
        UpdateFQBNPreview()
        UpdatePartitionCount()
        ShowBoardsTxtStatus()
    End Sub

    Protected Sub btnClearCustom_Click(sender As Object, e As EventArgs)
        boardManager.ClearCustomPartition()
        customPartitionStatusPanel.Visible = False
        btnClearCustom.Visible = False
        PopulatePartitions()
        UpdatePartitionCount()
        ShowCustomPartitionStatus()
        UpdateFQBNPreview()
    End Sub

    Protected Sub btnImportBoardsTxt_Click(sender As Object, e As EventArgs)
        lblBoardsTxtResult.Text = ""
        Dim boardsTxtPath As String = ""
        If Not String.IsNullOrWhiteSpace(txtBoardsTxtPath.Text) Then
            boardsTxtPath = txtBoardsTxtPath.Text.Trim()
            If Not File.Exists(boardsTxtPath) Then
                lblBoardsTxtResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> boards.txt file not found at the specified path</span>"
                Return
            End If
            ' Copy the boards.txt to server App_Data path
            Dim appData = Server.MapPath("~/App_Data")
            If Not Directory.Exists(appData) Then Directory.CreateDirectory(appData)
            Dim destBoardsTxt = Path.Combine(appData, "boards.txt")
            File.Copy(boardsTxtPath, destBoardsTxt, True)
            boardsTxtPath = destBoardsTxt
        ElseIf fuBoardsTxt.HasFile Then
            Dim appData = Server.MapPath("~/App_Data")
            If Not Directory.Exists(appData) Then Directory.CreateDirectory(appData)
            Dim destBoardsTxt = Path.Combine(appData, "boards.txt")
            fuBoardsTxt.SaveAs(destBoardsTxt)
            boardsTxtPath = destBoardsTxt
        Else
            lblBoardsTxtResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> Please provide a path to boards.txt or upload the file</span>"
            Return
        End If

        ' Clear custom partition when using boards.txt
        boardManager.ClearCustomPartition()

        ' Import partition schemes and board options from boards.txt
        Dim result = boardManager.ImportFromBoardsTxt(boardsTxtPath)

        ' Clean up temp file if we created one and import failed
        If Not result.Success AndAlso File.Exists(boardsTxtPath) Then
            Try
                File.Delete(boardsTxtPath)
            Catch
                ' Ignore errors on cleanup
            End Try
        End If

        If result.Success Then
            ' Reload UI elements
            PopulatePartitions()

            ' If board options were found, refresh those too and set defaults
            If result.FoundBoardOptions Then
                ' Set default configuration for current board
                SetDefaultBoardConfigurations(ddlBoard.SelectedValue)

                ' Refresh board options UI
                PopulateBoardOptions()
            End If

            ' Update UI
            UpdateFQBNPreview()
            UpdatePartitionCount()
            ShowBoardsTxtStatus()
            ShowCustomPartitionStatus()

            ' Select the first imported partition if available
            If Not String.IsNullOrEmpty(result.FirstPartitionName) AndAlso ddlPartition.Items.FindByValue(result.FirstPartitionName) IsNot Nothing Then
                ddlPartition.SelectedValue = result.FirstPartitionName
            End If

            ' Show warnings if any
            Dim warningMsg = ""
            If result.Warnings.Count > 0 Then
                warningMsg = String.Format("<br/><span class='badge badge-warning'><i class='fas fa-exclamation-triangle'></i> Warnings: {0}</span>", result.Warnings.Count)
            End If

            ' Show success message
            lblBoardsTxtResult.Text = String.Format("<span class='badge badge-success'><i class='fas fa-check-circle'></i> {0}</span>{1}", result.Message, warningMsg)

            ' Switch to main tab to show the imported partitions in action
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showMainTab", "showTab('mainTab');", True)
        Else
            lblBoardsTxtResult.Text = String.Format("<span class='badge badge-danger'><i class='fas fa-times-circle'></i> {0}</span>", result.Message)
        End If
    End Sub

    Protected Sub btnImportPartitionsCsv_Click(sender As Object, e As EventArgs)
        lblCsvResult.Text = ""
        If Not fuPartitionsCsv.HasFile Then
            lblCsvResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> Please select a partitions.csv file</span>"
            Return
        End If
        Dim ext = Path.GetExtension(fuPartitionsCsv.FileName).ToLower()
        If ext <> ".csv" Then
            lblCsvResult.Text = "<span class='badge badge-danger'><i class='fas fa-exclamation-circle'></i> Only .csv files are allowed</span>"
            Return
        End If
        Try
            boardManager.ClearBoardsTxtPartitions()
            Dim csvPath = Path.Combine(Server.MapPath("~/App_Data"), String.Format("custom_partitions_{0}.csv", DateTime.Now.Ticks))
            fuPartitionsCsv.SaveAs(csvPath)
            Dim result = boardManager.ImportCustomPartition(csvPath)
            If result.Success Then
                PopulatePartitions()
                UpdatePartitionCount()
                ShowBoardsTxtStatus()
                ShowCustomPartitionStatus()
                UpdateFQBNPreview()
                If ddlPartition.Items.FindByValue("custom") IsNot Nothing Then
                    ddlPartition.SelectedValue = "custom"
                End If
                lblCsvResult.Text = String.Format("<span class='badge badge-success'><i class='fas fa-check-circle'></i> {0}</span>", result.Message)
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showMainTab", "showTab('mainTab');", True)
            Else
                lblCsvResult.Text = String.Format("<span class='badge badge-danger'><i class='fas fa-times-circle'></i> {0}</span>", result.Message)
            End If
        Catch ex As Exception
            lblCsvResult.Text = String.Format("<span class='badge badge-danger'><i class='fas fa-times-circle'></i> Error importing CSV: {0}</span>", ex.Message)
        End Try
    End Sub

    Private Function Max(a As Integer, b As Integer) As Integer
        Return If(a > b, a, b)
    End Function
End Class