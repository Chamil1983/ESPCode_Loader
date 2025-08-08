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
    Private Const DEFAULT_BOARDS_TXT_PATH As String = "C:\Users\gen_rms_testroom\Documents\Arduino\VB.net\KC-Link\ArduinoWeb\App_Data\hardware\esp32\3.2.0\boards.txt"

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

    ' Flag to control whether to force flash settings - now set to false by default
    Private Property ForceFlashSettings As Boolean
        Get
            If Session("ForceFlashSettings") Is Nothing Then
                Session("ForceFlashSettings") = False
            End If
            Return CBool(Session("ForceFlashSettings"))
        End Get
        Set(value As Boolean)
            Session("ForceFlashSettings") = value
        End Set
    End Property

    Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
        ' Critical fix: Always update the version info with current date and user
        Dim currentDateTime = "2025-08-03 11:05:46"
        Dim currentUser = "Chamil1983I"
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "updateVersionInfo", $"
            document.addEventListener('DOMContentLoaded', function() {{
                var versionElement = document.querySelector('.version-info');
                if (versionElement) {{
                    versionElement.innerHTML = 'ESP32 Arduino Web Loader v1.5.2 | Last Updated: {currentDateTime} | User: {currentUser}';
                }}
            }});
        ", True)

        If Not IsPostBack Then
            ' Clear session data for a clean start
            Session("BoardConfigOptions") = Nothing
            Session("OutputBuffer") = Nothing
            Session("ForceFlashSettings") = False  ' Don't force flash settings by default

            ' First, try to load boards.txt
            If String.IsNullOrEmpty(txtBoardsTxtPath.Text) Then
                txtBoardsTxtPath.Text = DEFAULT_BOARDS_TXT_PATH
                If File.Exists(DEFAULT_BOARDS_TXT_PATH) Then
                    ImportBoardsTxtFromPath(DEFAULT_BOARDS_TXT_PATH)
                End If
            End If

            ' Load any custom partitions
            boardManager.LoadCustomPartitions()

            ' Populate boards dropdown first
            PopulateBoards()

            ' Set default board configurations
            SetDefaultBoardConfigurations(ddlBoard.SelectedValue)

            ' Populate partitions and serial ports
            PopulatePartitions()
            PopulateSerialPorts()

            ' EXPLICITLY set default partition scheme
            If ddlPartition.Items.Count > 0 Then
                ' Look for "default" item first
                If ddlPartition.Items.FindByValue("default") IsNot Nothing Then
                    ddlPartition.SelectedValue = "default"
                    AppendToLog("Selected 'default' partition scheme")
                Else
                    ddlPartition.SelectedIndex = 0 ' Fallback to first item
                    AppendToLog($"Default partition not found, selected: {ddlPartition.SelectedValue}")
                End If
            End If

            ' Now populate board options
            PopulateBoardOptions()

            ' Set the UI status
            txtOutput.Text = "Ready."
            lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-info-circle'></i> Status: Ready</span>"

            ' Set CLI path if it exists in session
            If Not String.IsNullOrEmpty(ArduinoCliPath) Then
                txtCliPath.Text = ArduinoCliPath
            End If

            ' Update UI elements
            UpdatePartitionCount()
            ShowBoardsTxtStatus()
            ShowCustomPartitionStatus()
            UpdateFQBNPreview()
        Else
            ' This is crucial: re-populate the board options UI after postbacks
            PopulateBoardOptions()
            UpdatePartitionCount()
            ShowBoardsTxtStatus()
            ShowCustomPartitionStatus()

            ' Update FQBN after UI is repopulated
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "updateFQBNAfterPostback", "setTimeout(function() { updateFQBNPreview(); }, 500);", True)
        End If

        ' Always register client scripts - for both initial load and postbacks
        RegisterClientScripts()

        ' Register script for binary file selection UI update
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "binaryFileSelectionHandler", String.Format("
        document.addEventListener('DOMContentLoaded', function() {{
            var fileInput = document.getElementById('{0}');
            var filePathDisplay = document.getElementById('{1}');
            
            if (fileInput && filePathDisplay) {{
                fileInput.addEventListener('change', function() {{
                    if (fileInput.files.length > 0) {{
                        filePathDisplay.value = fileInput.files[0].name;
                        console.log('Binary file selected: ' + fileInput.files[0].name);
                    }} else {{
                        filePathDisplay.value = '';
                    }}
                }});
            }}
        }});
    ", fuBinaryZip.ClientID, txtBinaryPath.ClientID), True)

        ' Add this to disable validation on the form
        Form.ValidateRequestMode = ValidateRequestMode.Disabled
    End Sub

    ' Register all client scripts needed for functionality
    Private Sub RegisterClientScripts()
        ' Register the stats initialization script
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "StatsSetup", "
            document.addEventListener('DOMContentLoaded', function() {
                initializeStatsAndCharts();
                setupBoardOptionListeners();
                setupCompileTracking();
            });
        ", True)

        ' Fix for board options listeners - runs after controls are fully rendered
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "setupBoardOptions", "
            setTimeout(function() {
                console.log('Setting up board option listeners after delay');
                setupBoardOptionListeners();
                updateFQBNPreview();
            }, 800);
        ", True)

        ' Debug script to help troubleshoot control rendering - FIXED QUOTES HERE
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "debugControls", "
            setTimeout(function() {
                var optionDropdowns = document.querySelectorAll('[id^=""ddlOption_""]');
                console.log('Found ' + optionDropdowns.length + ' board option dropdowns after delay');
                
                // Check if FQBN preview exists
                var fqbnPreview = document.getElementById('fqbnPreview');
                console.log('FQBN preview element exists: ' + (fqbnPreview ? 'Yes' : 'No'));
                
                // Check if stats panel exists
                var statsPanel = document.getElementById('statsPanel');
                console.log('Stats panel exists: ' + (statsPanel ? 'Yes' : 'No'));
            }, 1000);
        ", True)

        ' Add board option styling for better visualization of default values
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "boardOptionStyling", "
            document.addEventListener('DOMContentLoaded', function() {
                styleBoardOptions();
            });
            
            function styleBoardOptions() {
                setTimeout(function() {
                    // Get all option dropdowns
                    var dropdowns = document.querySelectorAll('[id^=""ddlOption_""]');
                    
                    dropdowns.forEach(function(dropdown) {
                        // Check if dropdown has data attribute for default value
                        var defaultVal = dropdown.getAttribute('data-default-value');
                        if (defaultVal) {
                            // Find and style the default option
                            Array.from(dropdown.options).forEach(function(option) {
                                if (option.value === defaultVal) {
                                    option.style.fontWeight = 'bold';
                                    option.style.color = '#0052cc';
                                    option.innerHTML += ' ★';
                                }
                                
                                // If option is marked as 'Arduino default'
                                if (option.text.includes('(Arduino IDE)')) {
                                    option.style.backgroundColor = '#e6f7ff';
                                    option.style.fontWeight = 'bold';
                                }
                            });
                            
                            // Style the dropdown if current value is default
                            if (dropdown.value === defaultVal) {
                                dropdown.style.borderColor = '#36B37E';
                                dropdown.style.boxShadow = '0 0 0 1px rgba(54, 179, 126, 0.3)';
                                
                                // Add small indicator next to dropdown
                                var parent = dropdown.parentElement;
                                if (parent && !parent.querySelector('.default-indicator')) {
                                    var indicator = document.createElement('span');
                                    indicator.className = 'default-indicator';
                                    indicator.innerHTML = '<i class=""fas fa-check-circle"" style=""color:#36B37E; margin-left:5px;"" title=""Arduino IDE default value""></i>';
                                    parent.appendChild(indicator);
                                }
                            }
                        }
                    });
                }, 500);
            }
        ", True)

        ' Ensure FQBN displays correctly with proper word wrapping
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "fixFQBNDisplay", "
    document.addEventListener('DOMContentLoaded', function() {
        function updateFQBNStyles() {
            var fqbnElements = document.querySelectorAll('.fqbn-preview-value, .actual-fqbn-value');
            fqbnElements.forEach(function(el) {
                el.style.wordBreak = 'break-all';
                el.style.maxWidth = '100%';
                el.style.overflowWrap = 'break-word';
                el.style.whiteSpace = 'normal';
            });
        }
        
        // Apply immediately and after any AJAX updates
        updateFQBNStyles();
        
        // Also try again after a short delay to catch dynamically added elements
        setTimeout(updateFQBNStyles, 500);
    });
", True)


    End Sub

    ' Import boards.txt from specified path
    Private Function ImportBoardsTxtFromPath(boardsTxtPath As String) As Boolean
        If File.Exists(boardsTxtPath) Then
            Try
                ' Copy the boards.txt to server App_Data path
                Dim appData = Server.MapPath("~/App_Data")
                If Not Directory.Exists(appData) Then Directory.CreateDirectory(appData)
                Dim destBoardsTxt = Path.Combine(appData, "boards.txt")
                File.Copy(boardsTxtPath, destBoardsTxt, True)

                ' Import partition schemes and board options
                Dim result = boardManager.ImportFromBoardsTxt(destBoardsTxt)
                Return result.Success
            Catch ex As Exception
                ' Log error for debugging
                AppendToLog($"Error importing boards.txt: {ex.Message}")
                Return False
            End Try
        End If
        Return False
    End Function

    ' Set defaults for board config options for given board
    ' Set defaults for board config options for given board
    Private Sub SetDefaultBoardConfigurations(boardName As String)
        BoardConfigOptions.Clear()

        ' Get board options from manager
        Dim options = boardManager.GetBoardOptions(boardName)

        If options IsNot Nothing AndAlso options.Count > 0 Then
            AppendToLog($"Found {options.Count} options for {boardName} in board manager")

            For Each opt In options
                ' Get the default value for this option
                Dim def = boardManager.GetOptionDefault(boardName, opt.Key)
                ' Get what "default" actually resolves to
                Dim actualValue = boardManager.GetOptionActualValue(boardName, opt.Key, def)

                ' Store in our configuration
                BoardConfigOptions(opt.Key) = actualValue
                AppendToLog($"Setting default for {opt.Key} = {actualValue} (default={def}, actual={actualValue})")
            Next
        Else
            AppendToLog($"No options found for board {boardName} in board manager - using board-specific defaults")

            ' Set board-specific defaults based on actual Arduino IDE configurations
            Select Case boardName
                Case "ESP32 Dev Module"
                    BoardConfigOptions("CPUFreq") = "240"
                    BoardConfigOptions("FlashFreq") = "80"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("FlashSize") = "4M"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("PSRAM") = "disabled"
                    BoardConfigOptions("EraseFlash") = "none"
                    BoardConfigOptions("EventsCore") = "1"
                    BoardConfigOptions("LoopCore") = "1"

                Case "ESP32-S2"
                    BoardConfigOptions("CPUFreq") = "240"
                    BoardConfigOptions("FlashFreq") = "80"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("FlashSize") = "4M"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("PSRAM") = "disabled"
                    BoardConfigOptions("EraseFlash") = "none"
                    BoardConfigOptions("USBCDCOnBoot") = "default"
                    BoardConfigOptions("USBDFUOnBoot") = "default"
                    BoardConfigOptions("USBMSCOnBoot") = "default"
                    BoardConfigOptions("JTAGAdapter") = "default"
                    BoardConfigOptions("UploadMode") = "default"
                    BoardConfigOptions("ZigbeeMode") = "default"


                Case "ESP32-S3"
                    BoardConfigOptions("CPUFreq") = "240"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("FlashSize") = "8M"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("CDCOnBoot") = "none"
                    BoardConfigOptions("DFUOnBoot") = "default"
                    BoardConfigOptions("MSCOnBoot") = "default"
                    BoardConfigOptions("PSRAM") = "disabled"
                    BoardConfigOptions("EraseFlash") = "none"
                    BoardConfigOptions("EventsCore") = "1"
                    BoardConfigOptions("LoopCore") = "1"
                    BoardConfigOptions("USBMode") = "hwcdc"
                    BoardConfigOptions("ZigbeeMode") = "default"


                Case "ESP32-C3"
                    BoardConfigOptions("CPUFreq") = "160"
                    BoardConfigOptions("FlashFreq") = "80"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("FlashSize") = "4M"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("EraseFlash") = "none"
                    BoardConfigOptions("CDCOnBoot") = "default"
                    BoardConfigOptions("JTAGAdapter") = "default"
                    BoardConfigOptions("ZigbeeMode") = "default"


                Case "ESP32 Wrover Kit"
                    BoardConfigOptions("CPUFreq") = "240"
                    BoardConfigOptions("FlashFreq") = "80"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("FlashSize") = "4M"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("PSRAM") = "enabled"
                    BoardConfigOptions("EraseFlash") = "none"


                Case "ESP32 Wrover"
                    BoardConfigOptions("FlashFreq") = "80"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("EraseFlash") = "none"


                Case "ESP32 Pico Kit"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("EraseFlash") = "none"


                Case "ESP32-C6", "ESP32-H2"
                    BoardConfigOptions("CPUFreq") = "160"
                    BoardConfigOptions("FlashFreq") = "80"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("FlashSize") = "4M"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("CDCOnBoot") = "default"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("EraseFlash") = "none"
                    BoardConfigOptions("JTAGAdapter") = "default"
                    BoardConfigOptions("ZigbeeMode") = "default"


                Case "ESP32-H2"
                    BoardConfigOptions("CPUFreq") = "64"
                    BoardConfigOptions("FlashMode") = "qio"
                    BoardConfigOptions("FlashSize") = "4M"
                    BoardConfigOptions("UploadSpeed") = "921600"
                    BoardConfigOptions("DebugLevel") = "none"
                    BoardConfigOptions("EraseFlash") = "none"
                    BoardConfigOptions("CDCOnBoot") = "default"
                    BoardConfigOptions("JTAGAdapter") = "default"
                    BoardConfigOptions("ZigbeeMode") = "default"


                    ' NOTE: ESP32-C6/H2 do NOT have PSRAM, EventsCore, LoopCore, or USBMode
            End Select
        End If

        ' Store in session
        Session("BoardConfigOptions") = BoardConfigOptions
        AppendToLog($"Saved {BoardConfigOptions.Count} board configuration options to session")
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

        ' Add all boards from board manager (which should have default boards already)
        For Each board In boardManager.GetBoardNames()
            If Not ddlBoard.Items.Contains(New ListItem(board, board)) Then
                ddlBoard.Items.Add(board)
            End If
        Next

        If ddlBoard.Items.Count > 0 Then
            ddlBoard.SelectedIndex = 0
            AppendToLog($"Populated boards dropdown with {ddlBoard.Items.Count} items")
        Else
            ' Add fallback default boards if none found in board manager
            ddlBoard.Items.Add(New ListItem("ESP32 Dev Module", "ESP32 Dev Module"))
            ddlBoard.Items.Add(New ListItem("ESP32-S2", "ESP32-S2"))
            ddlBoard.Items.Add(New ListItem("ESP32-C3", "ESP32-C3"))
            ddlBoard.Items.Add(New ListItem("ESP32-S3", "ESP32-S3"))
            ddlBoard.Items.Add(New ListItem("ESP32 Wrover Kit", "ESP32 Wrover Kit"))
            ddlBoard.Items.Add(New ListItem("ESP32 Wrover", "ESP32 Wrover"))
            ddlBoard.Items.Add(New ListItem("ESP32 Pico Kit", "ESP32 Pico Kit"))
            ddlBoard.Items.Add(New ListItem("ESP32-C6", "ESP32-C6"))
            ddlBoard.Items.Add(New ListItem("ESP32-H2", "ESP32-H2"))
            ddlBoard.SelectedIndex = 0
            AppendToLog($"Populated boards dropdown with {ddlBoard.Items.Count} default items")
        End If
    End Sub

    Protected Sub ddlBoard_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' When board changes, reset to default config for that board
        SetDefaultBoardConfigurations(ddlBoard.SelectedValue)
        PopulatePartitions()
        PopulateBoardOptions()
        UpdateFQBNPreview()

        ' Show notification about board change
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "boardChangeNotification", "
            showNotification('Board Changed', 'Settings have been updated to match " + ddlBoard.SelectedItem.Text + " defaults', 'info');
            styleBoardOptions();
        ", True)
    End Sub

    Private Sub PopulatePartitions()
        Dim currentSelection = If(ddlPartition.SelectedValue, "")
        ddlPartition.Items.Clear()
        Dim board As String = ddlBoard.SelectedValue

        ' Get partition options for this board from board manager
        Dim options = boardManager.GetPartitionOptions(board)

        ' Add all options from board manager
        For Each part In options
            If Not ddlPartition.Items.Contains(New ListItem(part, part)) Then
                ddlPartition.Items.Add(part)
            End If
        Next

        ' If no options were found, add default partition schemes
        If ddlPartition.Items.Count = 0 Then
            ddlPartition.Items.Add(New ListItem("Default", "default"))
            ddlPartition.Items.Add(New ListItem("Huge App", "huge_app"))
            ddlPartition.Items.Add(New ListItem("Minimal SPIFFS", "min_spiffs"))
            ddlPartition.Items.Add(New ListItem("No OTA", "no_ota"))
        End If

        AppendToLog($"Populated partition dropdown with {ddlPartition.Items.Count} items")

        ' Choose appropriate partition scheme
        If Not String.IsNullOrEmpty(currentSelection) AndAlso ddlPartition.Items.FindByValue(currentSelection) IsNot Nothing Then
            ddlPartition.SelectedValue = currentSelection
            AppendToLog($"Selected previous partition: {currentSelection}")
        ElseIf ddlPartition.Items.FindByValue("default") IsNot Nothing Then
            ' Use default as first choice
            ddlPartition.SelectedValue = "default"
            AppendToLog($"Selected default partition")
        ElseIf ddlPartition.Items.FindByValue("min_spiffs") IsNot Nothing Then
            ' Use min_spiffs for safer operation with 4MB flash as second choice
            ddlPartition.SelectedValue = "min_spiffs"
            AppendToLog($"Selected min_spiffs partition")
        ElseIf HasCustomPartition AndAlso ddlPartition.Items.FindByValue("custom") IsNot Nothing Then
            ' Custom partition if available
            ddlPartition.SelectedValue = "custom"
            AppendToLog($"Selected custom partition")
        ElseIf ddlPartition.Items.Count > 0 Then
            ' Default fallback to first item
            ddlPartition.SelectedIndex = 0
            AppendToLog($"Selected first available partition: {ddlPartition.SelectedValue}")
        End If

        UpdatePartitionCount()
        ShowCustomPartitionStatus()
    End Sub

    ' Improved PopulateBoardOptions with fixes for control rendering issues and better display of default values
    Private Sub PopulateBoardOptions()
        ' Clear the placeholder first
        plhBoardOptions.Controls.Clear()

        ' Get board name and options
        Dim boardName = ddlBoard.SelectedValue
        If String.IsNullOrEmpty(boardName) Then
            AppendToLog("No board name selected")
            Return
        End If

        ' Get options for this board
        Dim options = boardManager.GetBoardOptions(boardName)

        ' If no options found in board manager, create default options
        If options Is Nothing OrElse options.Count = 0 Then
            AppendToLog($"No options found in board manager for board: {boardName} - creating default options")
            options = CreateDefaultBoardOptions()
        End If

        ' Even after trying to create defaults, if still no options, show a message
        If options Is Nothing OrElse options.Count = 0 Then
            AppendToLog($"No options available for board: {boardName}")
            ' Create a message if no options available
            Dim noOptionsMsg As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
            noOptionsMsg.Attributes("class") = "board-config-container"
            noOptionsMsg.InnerHtml = $"<div class='info-box'><i class='fas fa-info-circle'></i> No configuration options available for {boardName}. Try importing a boards.txt file.</div>"
            plhBoardOptions.Controls.Add(noOptionsMsg)
            Return
        End If

        AppendToLog($"Found {options.Count} options for board: {boardName}")

        ' Store base FQBN
        hidBaseFQBN.Value = boardManager.GetFQBN(boardName, "", "")

        ' Create the container for all board options
        Dim configContainer As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        configContainer.Attributes("class") = "board-config-container"
        configContainer.ID = "boardConfigContainer"

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
        optionsContainer.ID = "boardOptionsGrid"
        configContainer.Controls.Add(optionsContainer)

        ' Log the number of options
        AppendToLog($"Building UI for {options.Count} board options")

        ' Add each option to the container
        For Each optKvp As KeyValuePair(Of String, Dictionary(Of String, String)) In options
            ' Skip USBMode for non-USB boards (ESP32 standard)
            If optKvp.Key = "USBMode" AndAlso
               (boardName = "ESP32 Dev Module" OrElse boardName = "ESP32 Wrover Kit" OrElse boardName = "ESP32 Wrover" OrElse boardName = "ESP32 Pico Kit") Then
                AppendToLog($"Skipping USBMode for {boardName} as it's not supported")
                Continue For
            End If

            ' Skip JTAGAdapter if it doesn't support proper values
            If optKvp.Key = "JTAGAdapter" AndAlso optKvp.Value.ContainsKey("disabled") Then
                AppendToLog($"Removing 'disabled' from JTAGAdapter options for compatibility")
                optKvp.Value.Remove("disabled")
                If optKvp.Value.Count = 0 Then
                    ' Add a default value if needed
                    optKvp.Value.Add("default", "Default")
                End If
            End If

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
            dropdown.CssClass = "form-control board-option-dropdown"
            dropdown.ClientIDMode = System.Web.UI.ClientIDMode.Static
            dropdown.Attributes("data-option-name") = optKvp.Key

            ' Get default value for this option and use it for data attribute
            Dim defaultValue = boardManager.GetOptionDefault(boardName, optKvp.Key)
            dropdown.Attributes("data-default-value") = defaultValue

            ' FIXED: Add explicit onchange handler using client script
            dropdown.Attributes("onchange") = "updateFQBNPreview(); styleBoardOptions(); console.log('Option changed: " + optKvp.Key + "');"

            ' Get default value for this option
            AppendToLog($"Option: {optKvp.Key}, Default: {defaultValue}, Values: {optKvp.Value.Count}")

            ' Add options from the board manager with clear indicators for Arduino IDE defaults
            For Each valueKvp As KeyValuePair(Of String, String) In optKvp.Value
                Dim isDefault = (valueKvp.Key = defaultValue)
                Dim displayText = valueKvp.Value

                ' Add visual indicator for Arduino IDE default
                If isDefault Then
                    displayText += " (Arduino IDE)"
                End If

                dropdown.Items.Add(New ListItem(displayText, valueKvp.Key))
            Next

            ' Determine which value to select
            Dim valueToSelect As String = defaultValue
            If BoardConfigOptions.ContainsKey(optKvp.Key) Then
                valueToSelect = BoardConfigOptions(optKvp.Key)
                AppendToLog($"Using stored value for {optKvp.Key}: {valueToSelect}")
            End If

            ' Select the appropriate value in dropdown
            If dropdown.Items.FindByValue(valueToSelect) IsNot Nothing Then
                dropdown.SelectedValue = valueToSelect
                AppendToLog($"Selected value {valueToSelect} for {optKvp.Key}")
            Else
                ' If we can't find the value to select, try to find the default
                If dropdown.Items.FindByValue(defaultValue) IsNot Nothing Then
                    dropdown.SelectedValue = defaultValue
                    BoardConfigOptions(optKvp.Key) = defaultValue
                    AppendToLog($"Fallback to board default {defaultValue} for {optKvp.Key}")
                Else
                    ' Select first item as a last resort
                    dropdown.SelectedIndex = 0
                    If dropdown.Items.Count > 0 Then
                        BoardConfigOptions(optKvp.Key) = dropdown.SelectedValue
                    End If
                    AppendToLog($"Fallback to first item for {optKvp.Key}")
                End If
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
        fqbnPreviewValue.ClientIDMode = System.Web.UI.ClientIDMode.Static

        ' Generate FQBN with current board options
        Dim fullFQBN = boardManager.GetFQBN(boardName, ddlPartition.SelectedValue, "", BoardConfigOptions)
        fqbnPreviewValue.InnerText = fullFQBN
        fqbnPreviewContainer.Controls.Add(fqbnPreviewValue)

        ' Add the compile FQBN preview - now it's the same as the UI FQBN
        Dim actualFqbnContainer As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        actualFqbnContainer.Attributes("class") = "actual-fqbn-container"
        actualFqbnContainer.Attributes("style") = "margin-top:10px; background-color:#F4F5F7; padding:10px; border-radius:4px;"

        Dim actualFqbnLabel As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        actualFqbnLabel.Attributes("class") = "actual-fqbn-label"
        actualFqbnLabel.InnerHtml = "<strong>Compilation FQBN:</strong> <small>(used during compile/upload)</small>"
        actualFqbnContainer.Controls.Add(actualFqbnLabel)

        ' Use the same FQBN for compilation (no forced settings)
        Dim compilationOptions = New Dictionary(Of String, String)(BoardConfigOptions)

        ' Only sanitize options for compatibility issues, don't force flash settings
        SanitizeBoardOptions(boardName, compilationOptions)

        Dim actualFQBN = boardManager.GetFQBNForCompilation(boardName, ddlPartition.SelectedValue, "", compilationOptions)

        Dim actualFqbnValue As New System.Web.UI.HtmlControls.HtmlGenericControl("div")
        actualFqbnValue.Attributes("class") = "actual-fqbn-value"
        ' Improve styling for long FQBN text
        actualFqbnValue.Attributes("style") = "font-family:monospace; background-color:white; padding:8px; border:1px solid #DFE1E6; border-radius:4px; margin-top:5px; word-break:break-all; max-width:100%; overflow-wrap:break-word;"
        actualFqbnValue.ID = "actualFqbnPreview"
        actualFqbnValue.ClientIDMode = System.Web.UI.ClientIDMode.Static
        actualFqbnValue.InnerText = actualFQBN
        actualFqbnContainer.Controls.Add(actualFqbnValue)

        fqbnPreviewContainer.Controls.Add(actualFqbnContainer)
        configContainer.Controls.Add(fqbnPreviewContainer)

        ' Add the main container to the page
        plhBoardOptions.Controls.Add(configContainer)

        ' Store board options in session
        Session("BoardConfigOptions") = BoardConfigOptions

        ' Also update hidFullFQBN to store the complete FQBN
        hidFullFQBN.Value = fullFQBN

        ' Log completion of board options population
        AppendToLog($"Board options UI populated with FQBN: {fullFQBN}")
        AppendToLog($"Compilation FQBN: {actualFQBN}")
    End Sub

    ' Create default board options if none are available from board manager
    Private Function CreateDefaultBoardOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim options As New Dictionary(Of String, Dictionary(Of String, String))

        ' Create CPU Frequency options
        Dim cpuFreq As New Dictionary(Of String, String)
        cpuFreq.Add("240", "240MHz (WiFi/BT)")
        cpuFreq.Add("160", "160MHz")
        cpuFreq.Add("80", "80MHz")
        options.Add("CPUFreq", cpuFreq)

        ' Create Flash Frequency options
        Dim flashFreq As New Dictionary(Of String, String)
        flashFreq.Add("80", "80MHz")
        flashFreq.Add("40", "40MHz")
        options.Add("FlashFreq", flashFreq)

        ' Create Flash Mode options
        Dim flashMode As New Dictionary(Of String, String)
        flashMode.Add("qio", "QIO")
        flashMode.Add("dio", "DIO")
        flashMode.Add("qout", "QOUT")
        flashMode.Add("dout", "DOUT")
        options.Add("FlashMode", flashMode)

        ' Create Flash Size options
        Dim flashSize As New Dictionary(Of String, String)
        flashSize.Add("4M", "4MB (32Mb)")
        flashSize.Add("2M", "2MB (16Mb)")
        flashSize.Add("8M", "8MB (64Mb)")
        flashSize.Add("16M", "16MB (128Mb)")
        options.Add("FlashSize", flashSize)

        ' Create Upload Speed options
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        uploadSpeed.Add("512000", "512000")
        options.Add("UploadSpeed", uploadSpeed)

        ' Create Debug Level options
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        debugLevel.Add("verbose", "Verbose")
        options.Add("DebugLevel", debugLevel)

        ' Create PSRAM options
        Dim psram As New Dictionary(Of String, String)
        psram.Add("disabled", "Disabled")
        psram.Add("enabled", "Enabled")
        options.Add("PSRAM", psram)

        ' Create Erase Flash options
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        options.Add("EraseFlash", eraseFlash)

        ' Create Events Core options
        Dim eventsCore As New Dictionary(Of String, String)
        eventsCore.Add("1", "Core 1")
        eventsCore.Add("0", "Core 0")
        options.Add("EventsCore", eventsCore)

        ' Create Loop Core options
        Dim loopCore As New Dictionary(Of String, String)
        loopCore.Add("1", "Core 1")
        loopCore.Add("0", "Core 0")
        options.Add("LoopCore", loopCore)

        ' Create JTAG Adapter options - FIXED with valid values
        Dim jtagAdapter As New Dictionary(Of String, String)
        jtagAdapter.Add("default", "Default")
        ' "disabled" is not a valid value - removed
        jtagAdapter.Add("external", "External")
        jtagAdapter.Add("bridge", "Bridge")
        options.Add("JTAGAdapter", jtagAdapter)

        AppendToLog("Created default board options")
        Return options
    End Function

    ' Improved method to capture board options from UI
    Private Sub CaptureBoardOptionsFromUI()
        ' Create a new dictionary to avoid shared references
        Dim options As New Dictionary(Of String, String)

        Try
            ' Process all dropdowns with class "board-option-dropdown"
            Dim dropdowns = New List(Of DropDownList)()

            ' Find all dropdowns recursively
            FindDropdownControls(plhBoardOptions, dropdowns)

            AppendToLog($"Found {dropdowns.Count} board option dropdowns")

            ' Process each dropdown
            For Each dropdown In dropdowns
                If dropdown.ID IsNot Nothing AndAlso dropdown.ID.StartsWith("ddlOption_") Then
                    Dim optionName = dropdown.ID.Replace("ddlOption_", "")
                    options(optionName) = dropdown.SelectedValue
                    AppendToLog($"Captured option: {optionName} = {dropdown.SelectedValue}")
                End If
            Next
        Catch ex As Exception
            AppendToLog($"Error capturing board options: {ex.Message}")
        End Try

        ' Check if we found any options
        If options.Count = 0 Then
            AppendToLog("WARNING: No options captured from UI")

            ' Preserve existing options if we can't capture from UI
            If BoardConfigOptions.Count > 0 Then
                AppendToLog($"Using {BoardConfigOptions.Count} existing options from session")
                Return ' Keep using existing BoardConfigOptions
            Else
                ' Add critical default options if nothing is available
                AppendToLog("Adding critical default options")
                options("FlashSize") = "4M"
                options("FlashMode") = "qio"
            End If
        End If

        ' Update BoardConfigOptions with the new values - without forcing any settings
        BoardConfigOptions = options

        ' Update session with the new values
        Session("BoardConfigOptions") = options

        ' Log the options for debugging
        AppendToLog($"Captured {options.Count} options from UI and stored in session")
    End Sub

    ' Helper method to find all DropDownList controls recursively
    Private Sub FindDropdownControls(control As Control, dropdowns As List(Of DropDownList))
        For Each childControl As Control In control.Controls
            If TypeOf childControl Is DropDownList Then
                dropdowns.Add(DirectCast(childControl, DropDownList))
            End If

            ' Recursively search in child controls
            If childControl.HasControls() Then
                FindDropdownControls(childControl, dropdowns)
            End If
        Next
    End Sub

    ' Sanitize board options based on the board type - only fix compatibility issues, don't force settings
    Private Sub SanitizeBoardOptions(boardName As String, ByRef options As Dictionary(Of String, String))
        ' Copy of options to check what needs to be removed
        Dim removeOptions As New List(Of String)

        ' Validate JTAGAdapter option
        If options.ContainsKey("JTAGAdapter") Then
            Dim validValues = New List(Of String) From {"default", "external", "bridge"}
            If Not validValues.Contains(options("JTAGAdapter").ToLower()) Then
                AppendToLog($"Corrected invalid JTAGAdapter value '{options("JTAGAdapter")}' to 'default'")
                options("JTAGAdapter") = "default"
            End If
        End If

        ' Handle USBMode for non-USB capable boards
        If boardName.Contains("ESP32 Dev Module") Or boardName.Contains("ESP32 Wrover Kit") Or boardName.Contains("ESP32 Wrover") Or
           boardName.Contains("ESP32 Pico Kit") Or boardName.Contains("ESP32-C3") Then
            If options.ContainsKey("USBMode") Then
                AppendToLog($"Removed USBMode option for {boardName} - not supported")
                removeOptions.Add("USBMode")
            End If
        End If

        ' Remove any invalid options
        For Each optName In removeOptions
            If options.ContainsKey(optName) Then
                options.Remove(optName)
                AppendToLog($"Removed option {optName} for compatibility")
            End If
        Next

        ' Log the sanitized options
        AppendToLog($"Sanitized board options: {String.Join(", ", options.Select(Function(kvp) $"{kvp.Key}={kvp.Value}"))}")
    End Sub

    Protected Sub ddlPartition_SelectedIndexChanged(sender As Object, e As EventArgs)
        UpdatePartitionCount()
        ShowCustomPartitionStatus()
        UpdateFQBNPreview()
    End Sub

    Private Sub UpdateFQBNPreview()
        Try
            ' Get current board and partition
            Dim boardName = ddlBoard.SelectedValue
            Dim partitionScheme = ddlPartition.SelectedValue

            ' Capture current board options from UI
            CaptureBoardOptionsFromUI()

            ' Generate FQBN with current settings (displayed to user)
            Dim fullFQBN = boardManager.GetFQBN(boardName, partitionScheme, "", BoardConfigOptions)

            ' Create a copy of board options
            Dim compilationOptions = New Dictionary(Of String, String)(BoardConfigOptions)

            ' Only sanitize options for compatibility issues, don't force settings
            SanitizeBoardOptions(boardName, compilationOptions)

            ' Generate the compilation FQBN
            Dim actualFQBN = boardManager.GetFQBNForCompilation(boardName, partitionScheme, "", compilationOptions)

            ' Update FQBN preview in UI
            Dim fqbnPreview = FindControlRecursive(plhBoardOptions, "fqbnPreview")
            If fqbnPreview IsNot Nothing AndAlso TypeOf fqbnPreview Is System.Web.UI.HtmlControls.HtmlGenericControl Then
                DirectCast(fqbnPreview, System.Web.UI.HtmlControls.HtmlGenericControl).InnerText = fullFQBN
            End If

            ' Update compilation FQBN in UI
            Dim actualFqbnPreview = FindControlRecursive(plhBoardOptions, "actualFqbnPreview")
            If actualFqbnPreview IsNot Nothing AndAlso TypeOf actualFqbnPreview Is System.Web.UI.HtmlControls.HtmlGenericControl Then
                DirectCast(actualFqbnPreview, System.Web.UI.HtmlControls.HtmlGenericControl).InnerText = actualFQBN
            End If

            ' Update the hidden field for FQBN and literal
            litFQBN.Text = fullFQBN
            hidFullFQBN.Value = fullFQBN

            ' Also update client-side with JavaScript for redundancy
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "updateFQBNDirectly", $"
                if (document.getElementById('fqbnPreview')) {{
                    document.getElementById('fqbnPreview').innerText = '{fullFQBN}';
                }}
                if (document.getElementById('actualFqbnPreview')) {{
                    document.getElementById('actualFqbnPreview').innerText = '{actualFQBN}';
                }}
            ", True)

            ' Update session with current board options
            Session("BoardConfigOptions") = BoardConfigOptions

            ' Log the FQBN for debugging
            Dim logEntry = $"Updated FQBN: {fullFQBN} with options: {String.Join(", ", BoardConfigOptions.Select(Function(kvp) $"{kvp.Key}={kvp.Value}"))}"
            AppendToLog(logEntry)
            AppendToLog($"Compilation FQBN: {actualFQBN}")
        Catch ex As Exception
            AppendToLog($"Error updating FQBN preview: {ex.Message}")
        End Try
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

    Private Function FindControlRecursive(root As Control, id As String) As Control
        If root Is Nothing Then Return Nothing
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

        ' Create compilation options - use the user's selected settings
        Dim compilationOptions = New Dictionary(Of String, String)(BoardConfigOptions)

        ' Only sanitize for compatibility issues, don't force settings
        SanitizeBoardOptions(board, compilationOptions)

        ' Generate FQBN using user's selected settings
        Dim fqbn = boardManager.GetFQBNForCompilation(board, schemeValue, projDir, compilationOptions)

        ' Log the FQBN being used for compilation
        AppendToLog($"Compiling with FQBN: {fqbn}")

        ' Clear output buffer and textboxSetDefaultBoardConfigurations 
        Dim bufferList = OutputBuffer
        bufferList.Clear()
        txtOutput.Text = ""

        ' Update status to indicate compilation started
        lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Compiling...</span>"

        ' Show the FQBN being used in output
        bufferList.Add("Executing task: arduino-cli compile")
        bufferList.Add(String.Format("Using FQBN: {0}", fqbn))
        bufferList.Add(String.Format("FQBN: {0}", fqbn))
        txtOutput.Text = String.Join(Environment.NewLine, bufferList)

        ' Add JavaScript to record start time and reset the UI
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "resetCompileUI", "
            window.compileStartTime = Date.now(); 
            console.log('Compilation started at ' + window.compileStartTime);
            resetCompilationState();
            resetAndStartProgress();
        ", True)

        ' Run compile with real-time output
        Dim result As ArduinoUtil.ExecResult =
            ArduinoUtil.RunCompileRealtime(ArduinoCliPath, projDir, fqbn,
                Sub(line)
                    Dim bufferRef = OutputBuffer
                    bufferRef.Add(line)
                    txtOutput.Text = String.Join(Environment.NewLine, bufferRef)
                End Sub)

        ' Update status based on result
        If result.Success Then
            Dim bufferRef = OutputBuffer
            bufferRef.Add("Compiled OK")
            lblStatus.Text = "<span class='status-indicator success'><i class='fas fa-check-circle'></i> Compilation Successful</span>"

            ' Add script to trigger statistics display - with retries for reliability
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showCompileStats", "
                function tryProcessOutput(attempts) {
                    console.log('Processing compilation output, attempt ' + attempts);
                    if (attempts <= 0) return;
                    
                    try {
                        var outputText = document.getElementById('txtOutput').value;
                        if (outputText && outputText.includes('Compiled OK')) {
                            processCompilationOutput(outputText);
                            
                            // Make sure stats panel is visible
                            setTimeout(function() {
                                var statsPanel = document.getElementById('statsPanel');
                                if (statsPanel) {
                                    statsPanel.style.display = 'block';
                                    console.log('Stats panel displayed');
                                }
                            }, 100);
                        } else {
                            console.log('Output not ready or compilation not successful');
                            setTimeout(function() { tryProcessOutput(attempts - 1); }, 500);
                        }
                    } catch (e) {
                        console.error('Error processing output:', e);
                        setTimeout(function() { tryProcessOutput(attempts - 1); }, 500);
                    }
                }
                
                // Try several times to ensure statistics are processed
                setTimeout(function() { tryProcessOutput(5); }, 500);
            ", True)
        Else
            Dim bufferRef = OutputBuffer
            bufferRef.Add("Compile failed")
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Compilation Failed</span>"

            ' Show failure notification
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "compileFailed", "
                showNotification('Compilation Failed', 'Check the output for errors.', 'error');
            ", True)
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

        ' Create compilation options - use the user's selected settings
        Dim compilationOptions = New Dictionary(Of String, String)(BoardConfigOptions)

        ' Only sanitize for compatibility issues, don't force settings
        SanitizeBoardOptions(board, compilationOptions)

        ' Generate FQBN using user's selected settings
        Dim fqbn = boardManager.GetFQBNForCompilation(board, schemeValue, projDir, compilationOptions)

        ' Log the FQBN being used for upload
        AppendToLog($"Uploading with FQBN: {fqbn}")

        ' Clear output buffer and textbox
        Dim bufferList = OutputBuffer
        bufferList.Clear()
        txtOutput.Text = ""

        ' Update status to indicate upload started
        lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Uploading...</span>"

        ' Show the FQBN being used in output
        bufferList.Add("Executing task: arduino-cli compile")
        bufferList.Add(String.Format("Using FQBN: {0}", fqbn))
        bufferList.Add(String.Format("FQBN: {0}", fqbn))
        txtOutput.Text = String.Join(Environment.NewLine, bufferList)

        ' Also initialize progress tracking
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "startUploadProgress", "resetAndStartProgress();", True)

        ' Run upload with real-time output
        Dim result As ArduinoUtil.ExecResult =
            ArduinoUtil.RunUploadRealtime(ArduinoCliPath, projDir, fqbn, serialPort,
                Sub(line)
                    Dim bufferRef = OutputBuffer
                    bufferRef.Add(line)
                    txtOutput.Text = String.Join(Environment.NewLine, bufferRef)
                End Sub)

        ' Update status based on result
        If result.Success Then
            Dim bufferRef = OutputBuffer
            bufferRef.Add("Upload OK")
            lblStatus.Text = "<span class='status-indicator success'><i class='fas fa-check-circle'></i> Upload Successful</span>"

            ' Add script to show success alert
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadSuccess", "
                showNotification('Upload Successful', 'The sketch has been successfully uploaded to the ESP32 device.', 'success');
            ", True)
        Else
            Dim bufferRef = OutputBuffer
            bufferRef.Add("Upload failed")
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Upload Failed</span>"

            ' Add script to show error alert
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadFailed", "
                showNotification('Upload Failed', 'Failed to upload the sketch. Check the error message in the output.', 'error');
            ", True)
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
            ' Ignore errors on deletion
        End Try
    End Sub

    Protected Sub btnExportBinary_Click(sender As Object, e As EventArgs)
        Try
            ' Capture and save the latest board configurations before compiling
            CaptureBoardOptionsFromUI()
            UpdateFQBNPreview()

            ' Initialize the output buffer once
            Dim bufferList As List(Of String) = OutputBuffer
            bufferList.Clear()
            txtOutput.Text = ""

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

            ' Create compilation options - use the user's selected settings
            Dim compilationOptions = New Dictionary(Of String, String)(BoardConfigOptions)

            ' Only sanitize for compatibility issues, don't force settings
            SanitizeBoardOptions(board, compilationOptions)

            ' Generate FQBN using user's selected settings
            Dim fqbn = boardManager.GetFQBNForCompilation(board, schemeValue, projDir, compilationOptions)

            ' Create export directory
            Dim exportDir = Path.Combine(projDir, "export")
            If Not Directory.Exists(exportDir) Then
                Directory.CreateDirectory(exportDir)
            End If

            ' Get sketch name for file naming
            Dim sketchName = Path.GetFileName(projDir)
            If String.IsNullOrEmpty(sketchName) Then
                sketchName = "sketch"
            End If

            ' Update status to indicate export started
            lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Exporting binary files...</span>"

            ' Show the FQBN being used in output
            bufferList.Add("Executing task: arduino-cli compile with binary export")
            bufferList.Add(String.Format("Using FQBN: {0}", fqbn))
            bufferList.Add(String.Format("Export directory: {0}", exportDir))
            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

            ' Add JavaScript to record start time and reset the UI
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "resetExportUI", "
            window.compileStartTime = Date.now(); 
            console.log('Binary export started at ' + window.compileStartTime);
            resetCompilationState();
            resetAndStartProgress();
        ", True)

            ' Run binary export with real-time output - using the public method
            Dim result As ArduinoUtil.ExecResult =
            ArduinoUtil.RunBinaryExportRealtime(ArduinoCliPath, projDir, fqbn, exportDir,
                Sub(line)
                    ' Don't re-declare bufferList here, use the one from outer scope
                    bufferList.Add(line)
                    txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                End Sub)

            ' Update status based on result
            If result.Success Then
                bufferList.Add("Compiled and exported binary files successfully")

                ' Find the generated binary files - look in both the export directory and the sketch directory
                ' Arduino CLI with -e flag exports binaries to the sketch folder
                Dim binFiles = Directory.GetFiles(exportDir, "*.bin", SearchOption.AllDirectories).ToList()
                binFiles.AddRange(Directory.GetFiles(projDir, "*.bin", SearchOption.TopDirectoryOnly))

                Dim elfFiles = Directory.GetFiles(exportDir, "*.elf", SearchOption.AllDirectories).ToList()
                elfFiles.AddRange(Directory.GetFiles(projDir, "*.elf", SearchOption.TopDirectoryOnly))

                Dim hexFiles = Directory.GetFiles(exportDir, "*.hex", SearchOption.AllDirectories).ToList()
                hexFiles.AddRange(Directory.GetFiles(projDir, "*.hex", SearchOption.TopDirectoryOnly))

                ' Copy the bin, elf, and hex files to the export directory with proper names
                Dim timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss")
                Dim successCount = 0

                ' List the exported files
                bufferList.Add("")
                bufferList.Add("Exported files:")

                ' Process BIN files
                For Each binFile In binFiles
                    Try
                        Dim destBinFile = Path.Combine(exportDir, String.Format("{0}_{1}.bin", sketchName, timestamp))
                        File.Copy(binFile, destBinFile, True)
                        bufferList.Add(String.Format("- BIN: {0}", Path.GetFileName(destBinFile)))
                        successCount += 1
                    Catch ex As Exception
                        bufferList.Add(String.Format("- Error copying BIN file: {0}", ex.Message))
                    End Try
                Next

                ' Process ELF files
                For Each elfFile In elfFiles
                    Try
                        Dim destElfFile = Path.Combine(exportDir, String.Format("{0}_{1}.elf", sketchName, timestamp))
                        File.Copy(elfFile, destElfFile, True)
                        bufferList.Add(String.Format("- ELF: {0}", Path.GetFileName(destElfFile)))
                        successCount += 1
                    Catch ex As Exception
                        bufferList.Add(String.Format("- Error copying ELF file: {0}", ex.Message))
                    End Try
                Next

                ' Process HEX files
                For Each hexFile In hexFiles
                    Try
                        Dim destHexFile = Path.Combine(exportDir, String.Format("{0}_{1}.hex", sketchName, timestamp))
                        File.Copy(hexFile, destHexFile, True)
                        bufferList.Add(String.Format("- HEX: {0}", Path.GetFileName(destHexFile)))
                        successCount += 1
                    Catch ex As Exception
                        bufferList.Add(String.Format("- Error copying HEX file: {0}", ex.Message))
                    End Try
                Next

                ' Create a ZIP file with all the exported files - with error handling
                Try
                    Dim zipPath = Path.Combine(exportDir, String.Format("{0}_{1}_binaries.zip", sketchName, timestamp))
                    If File.Exists(zipPath) Then
                        File.Delete(zipPath)
                    End If

                    ' Create an info file first
                    Dim tempInfoPath = Path.Combine(exportDir, "export_info.txt")
                    Using writer As New StreamWriter(tempInfoPath)
                        writer.WriteLine("ESP32 Firmware Export")
                        writer.WriteLine(String.Format("Date: {0}", DateTime.Now))
                        writer.WriteLine(String.Format("Sketch: {0}", sketchName))
                        writer.WriteLine(String.Format("Board: {0}", board))
                        writer.WriteLine(String.Format("Partition Scheme: {0}", schemeValue))
                        writer.WriteLine(String.Format("FQBN: {0}", fqbn))
                    End Using

                    ' Create a new list of files to include in ZIP
                    Dim zipFiles As New List(Of String)()
                    zipFiles.AddRange(binFiles)
                    zipFiles.AddRange(elfFiles)
                    zipFiles.AddRange(hexFiles)
                    zipFiles.Add(tempInfoPath)

                    ' Create the ZIP file
                    Using zipArchive As ZipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create)
                        ' Add all binary files to the ZIP
                        For Each filePath In zipFiles.Distinct()
                            If File.Exists(filePath) Then
                                Dim entryName = Path.GetFileName(filePath)
                                zipArchive.CreateEntryFromFile(filePath, entryName)
                            End If
                        Next
                    End Using

                    ' Delete temp info file
                    If File.Exists(tempInfoPath) Then
                        File.Delete(tempInfoPath)
                    End If

                    bufferList.Add(String.Format("- ZIP: {0}", Path.GetFileName(zipPath)))
                    successCount += 1
                Catch ex As Exception
                    bufferList.Add(String.Format("- Error creating ZIP file: {0}", ex.Message))
                    AppendToLog(String.Format("ZIP creation error: {0}", ex.ToString()))
                End Try

                ' Update the status based on found files
                If successCount > 0 Then
                    lblStatus.Text = String.Format("<span class='status-indicator success'><i class='fas fa-check-circle'></i> Binary Export Successful ({0} files)</span>", successCount)
                    bufferList.Add("")
                    bufferList.Add(String.Format("Export successful! {0} files exported to {1}", successCount, exportDir))

                    ' Add script to show success alert
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "exportSuccess", String.Format("
                    showNotification('Export Successful', '{0} binary files have been exported to {1}', 'success');
                ", successCount, exportDir.Replace("\", "\\")), True)
                Else
                    lblStatus.Text = "<span class='status-indicator warning'><i class='fas fa-exclamation-triangle'></i> Export Completed (No Files Found)</span>"
                    bufferList.Add("")
                    bufferList.Add("No binary files were found after compilation. Check the output for errors.")

                    ' Add script to show warning alert
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "exportWarning", "
                    showNotification('Export Completed', 'No binary files were found after compilation. Check the output for details.', 'info');
                ", True)
                End If
            Else
                bufferList.Add("Export failed")
                lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Export Failed</span>"

                ' Add script to show error alert
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "exportFailed", "
                showNotification('Export Failed', 'Failed to export binary files. Check the error message in the output.', 'error');
            ", True)
            End If

            ' Update output text
            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

        Catch ex As Exception
            AppendToLog(String.Format("Binary export error: {0}", ex.ToString()))
            txtOutput.Text = String.Format("Error during binary export: {0}", ex.Message)
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Export Failed - Exception</span>"

            ' Show error notification
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "exportExceptionError", String.Format("
            showNotification('Export Error', 'An error occurred: {0}', 'error');
        ", ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ")), True)
        End Try
    End Sub

    Protected Sub btnUploadBinary_Click(sender As Object, e As EventArgs)
        Try
            ' Capture and save the latest board configurations before uploading
            CaptureBoardOptionsFromUI()
            UpdateFQBNPreview()

            ' Initialize the output buffer once
            Dim bufferList As List(Of String) = OutputBuffer
            bufferList.Clear()
            txtOutput.Text = ""

            ' Check if a binary file has been uploaded or selected
            Dim binaryFilePath As String = txtBinaryPath.Text.Trim()

            ' If no binary file is already selected, check if one is being uploaded
            If String.IsNullOrEmpty(binaryFilePath) AndAlso fuBinaryZip.HasFile Then
                ' Get the uploaded file
                Dim fileExtension = Path.GetExtension(fuBinaryZip.FileName).ToLower()

                ' Check if it's a valid binary or zip file
                If Not (fileExtension = ".bin" OrElse fileExtension = ".hex" OrElse fileExtension = ".elf" OrElse fileExtension = ".zip") Then
                    txtOutput.Text = "Error: Only .bin, .hex, .elf, or .zip files are allowed for binary upload."
                    Return
                End If

                ' Display file size for debugging
                Dim fileSize As Long = fuBinaryZip.PostedFile.ContentLength
                AppendToLog(String.Format("Uploading file: {0}, Size: {1:N0} bytes", fuBinaryZip.FileName, fileSize))

                ' Create a temp directory to save the file
                Dim tempDir = Path.Combine(Server.MapPath("~/App_Data"), "TempBinary_" + DateTime.Now.Ticks.ToString())
                If Not Directory.Exists(tempDir) Then
                    Directory.CreateDirectory(tempDir)
                End If

                ' Update UI to show progress
                bufferList.Add(String.Format("Uploading file: {0} ({1:N0} bytes)...", fuBinaryZip.FileName, fileSize))
                txtOutput.Text = String.Join(Environment.NewLine, bufferList)

                ' Save the uploaded file - with try/catch for better error handling
                Try
                    binaryFilePath = Path.Combine(tempDir, fuBinaryZip.FileName)
                    fuBinaryZip.SaveAs(binaryFilePath)
                    txtBinaryPath.Text = binaryFilePath
                    bufferList.Add(String.Format("File saved successfully to: {0}", binaryFilePath))
                Catch ex As Exception
                    bufferList.Add(String.Format("Error saving file: {0}", ex.Message))
                    AppendToLog(String.Format("Error saving file: {0}", ex.ToString()))
                    txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                    Return
                End Try

                ' If it's a ZIP file, extract it
                If fileExtension = ".zip" Then
                    Try
                        bufferList.Add("Extracting ZIP file...")
                        txtOutput.Text = String.Join(Environment.NewLine, bufferList)

                        Dim extractDir = Path.Combine(tempDir, "extracted")
                        Directory.CreateDirectory(extractDir)

                        ' Use more robust extraction for larger files
                        Using archive As ZipArchive = ZipFile.OpenRead(binaryFilePath)
                            bufferList.Add(String.Format("ZIP contains {0} files", archive.Entries.Count))

                            For Each entry As ZipArchiveEntry In archive.Entries
                                Dim destinationPath = Path.Combine(extractDir, entry.FullName)
                                Dim destinationDir = Path.GetDirectoryName(destinationPath)

                                If Not String.IsNullOrEmpty(destinationDir) AndAlso Not Directory.Exists(destinationDir) Then
                                    Directory.CreateDirectory(destinationDir)
                                End If

                                If Not String.IsNullOrEmpty(entry.Name) Then
                                    entry.ExtractToFile(destinationPath, True)
                                End If
                            Next
                        End Using

                        bufferList.Add("ZIP file extracted successfully")
                        txtOutput.Text = String.Join(Environment.NewLine, bufferList)

                        ' Look for binary files in the extracted directory
                        Dim binFiles = Directory.GetFiles(extractDir, "*.bin", SearchOption.AllDirectories)
                        Dim hexFiles = Directory.GetFiles(extractDir, "*.hex", SearchOption.AllDirectories)
                        Dim elfFiles = Directory.GetFiles(extractDir, "*.elf", SearchOption.AllDirectories)

                        bufferList.Add(String.Format("Found: {0} .bin files, {1} .hex files, {2} .elf files",
                                               binFiles.Length, hexFiles.Length, elfFiles.Length))

                        ' Prioritize bin files, then hex, then elf
                        If binFiles.Length > 0 Then
                            binaryFilePath = binFiles(0)
                            bufferList.Add(String.Format("Using .bin file: {0}", Path.GetFileName(binaryFilePath)))
                        ElseIf hexFiles.Length > 0 Then
                            binaryFilePath = hexFiles(0)
                            bufferList.Add(String.Format("Using .hex file: {0}", Path.GetFileName(binaryFilePath)))
                        ElseIf elfFiles.Length > 0 Then
                            binaryFilePath = elfFiles(0)
                            bufferList.Add(String.Format("Using .elf file: {0}", Path.GetFileName(binaryFilePath)))
                        Else
                            bufferList.Add("Error: No binary files found in the ZIP archive.")
                            txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                            Return
                        End If

                        txtBinaryPath.Text = binaryFilePath
                    Catch ex As Exception
                        bufferList.Add(String.Format("Error extracting ZIP file: {0}", ex.Message))
                        AppendToLog(String.Format("Error extracting ZIP: {0}", ex.ToString()))
                        txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                        Return
                    End Try
                End If
            ElseIf String.IsNullOrEmpty(binaryFilePath) Then
                txtOutput.Text = "Error: Please select a binary file to upload."
                Return
            ElseIf Not File.Exists(binaryFilePath) Then
                txtOutput.Text = String.Format("Error: The binary file '{0}' does not exist.", binaryFilePath)
                Return
            End If

            ' Get selected board and partition scheme
            Dim board = ddlBoard.SelectedValue
            Dim partitionScheme = ddlPartition.SelectedValue
            Dim serialPort = ddlSerial.SelectedValue
            Dim verifyUpload = chkVerifyUpload.Checked

            ' Validate serial port
            If String.IsNullOrEmpty(serialPort) Then
                txtOutput.Text = "Error: No serial port available. Please connect your board and refresh ports."
                Return
            End If

            ' Validate CLI path
            If String.IsNullOrWhiteSpace(txtCliPath.Text) Then
                txtOutput.Text = "Error: Please set the Arduino CLI path."
                Return
            End If
            ArduinoCliPath = txtCliPath.Text.Trim()

            ' Create compilation options for FQBN
            Dim compilationOptions = New Dictionary(Of String, String)(BoardConfigOptions)
            SanitizeBoardOptions(board, compilationOptions)

            ' Generate FQBN using user's selected settings
            Dim fqbn = boardManager.GetFQBNForCompilation(board, partitionScheme, "", compilationOptions)

            ' Clear the buffer again before starting upload
            bufferList.Clear()
            txtOutput.Text = ""

            ' Update status to indicate upload started
            lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Uploading Binary...</span>"

            ' Show the information about the upload
            bufferList.Add("Executing task: Upload binary file directly to board")
            bufferList.Add(String.Format("Binary file: {0}", Path.GetFileName(binaryFilePath)))
            bufferList.Add(String.Format("Board: {0}", board))
            bufferList.Add(String.Format("FQBN: {0}", fqbn))
            bufferList.Add(String.Format("Port: {0}", serialPort))
            bufferList.Add(String.Format("Verify after upload: {0}", verifyUpload))
            bufferList.Add("Starting upload...")
            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

            ' Register progress tracking script
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "resetUploadBinaryUI", "
            window.compileStartTime = Date.now(); 
            console.log('Binary upload started at ' + window.compileStartTime);
            resetCompilationState();
            resetAndStartProgress();
        ", True)

            ' Run the binary upload with real-time output
            Dim result As ArduinoUtil.ExecResult =
            ArduinoUtil.RunBinaryUploadRealtime(ArduinoCliPath, binaryFilePath, fqbn, serialPort, verifyUpload,
                Sub(line)
                    ' Don't re-declare bufferList here, use the one from outer scope
                    bufferList.Add(line)
                    txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                End Sub)

            ' Update status based on result
            If result.Success Then
                bufferList.Add("Binary upload completed successfully")

                ' Check output for verification success
                Dim verificationSuccessful = True
                If verifyUpload Then
                    If result.Output.Contains("Verify Failed") OrElse
                   result.Output.Contains("verification error") OrElse
                   result.Output.Contains("verify failed") Then
                        verificationSuccessful = False
                        bufferList.Add("WARNING: Upload verification failed! The binary may not have been uploaded correctly.")
                    Else
                        bufferList.Add("Upload verification successful. Binary file matches device memory.")
                    End If
                End If

                ' Final status message
                If verifyUpload AndAlso Not verificationSuccessful Then
                    lblStatus.Text = "<span class='status-indicator warning'><i class='fas fa-exclamation-triangle'></i> Upload Completed but Verification Failed</span>"

                    ' Show warning notification
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadVerifyFailed", "
                    showNotification('Upload Completed', 'Binary was uploaded but verification failed. The binary may not match the device memory.', 'warning');
                ", True)
                Else
                    lblStatus.Text = "<span class='status-indicator success'><i class='fas fa-check-circle'></i> Binary Upload Successful</span>"

                    ' Show success notification
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadBinarySuccess", "
                    showNotification('Upload Successful', 'Binary file has been uploaded to the ESP32 device successfully.', 'success');
                ", True)
                End If

                ' Add extra information about the board configuration
                bufferList.Add("")
                bufferList.Add("Board Configuration Summary:")
                bufferList.Add(String.Format("- Board: {0}", board))
                bufferList.Add(String.Format("- Partition Scheme: {0}", partitionScheme))

                ' Add specific board options - Fixed the For Each loop syntax
                If compilationOptions.Count > 0 Then
                    bufferList.Add("- Board Options:")
                    For Each kvp As KeyValuePair(Of String, String) In compilationOptions
                        bufferList.Add(String.Format("  • {0}: {1}", boardManager.GetOptionName(kvp.Key), kvp.Value))
                    Next
                End If

                bufferList.Add(String.Format("- Binary File: {0}", Path.GetFileName(binaryFilePath)))

                ' Use correct format for file size
                Dim fileSize As Long = New FileInfo(binaryFilePath).Length
                bufferList.Add(String.Format("- File Size: {0:N0} bytes", fileSize))

                ' Calculate upload time
                Dim uploadTimeSeconds As Double = Math.Round((DateTime.Now - DateTime.Now.AddMilliseconds(-Environment.TickCount)).TotalSeconds, 1)
                bufferList.Add(String.Format("- Upload Time: {0} seconds", uploadTimeSeconds))

            Else
                bufferList.Add("Binary upload failed")
                lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Binary Upload Failed</span>"

                ' Try to analyze the error for common issues
                Dim errorMsg = "Check the error message in the output"

                If result.Output.Contains("No such file or directory") Then
                    errorMsg = "Binary file not found or CLI path incorrect"
                ElseIf result.Output.Contains("Cannot open") AndAlso result.Output.Contains(serialPort) Then
                    errorMsg = String.Format("Cannot open serial port {0}. Port may be in use or unavailable", serialPort)
                ElseIf result.Output.Contains("Invalid board") Then
                    errorMsg = "Invalid board or FQBN configuration"
                End If

                ' Show error notification
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadBinaryFailed", String.Format("
                showNotification('Upload Failed', 'Failed to upload binary file. {0}.', 'error');
            ", errorMsg), True)
            End If

            ' Update output text
            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

        Catch ex As Exception
            ' Global exception handler
            AppendToLog(String.Format("Binary upload error: {0}", ex.ToString()))
            txtOutput.Text = String.Format("Error during binary upload: {0}", ex.Message)
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Upload Failed - Exception</span>"

            ' Show error notification
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadExceptionError", String.Format("
            showNotification('Upload Error', 'An error occurred: {0}', 'error');
        ", ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ")), True)
        End Try
    End Sub

    ' Handle binary file selection
    Protected Sub fuBinaryZip_Change(sender As Object, e As EventArgs)
        If fuBinaryZip.HasFile Then
            txtBinaryPath.Text = fuBinaryZip.FileName

            ' Add UI notification
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "binaryFileSelected", String.Format("
            showNotification('File Selected', 'Binary file {0} selected for upload.', 'info');
        ", fuBinaryZip.FileName), True)
        End If
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
            ' Reset board configuration options
            BoardConfigOptions.Clear()

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
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showMainTab", "
                showTab('mainTab');
                showNotification('boards.txt Imported', 'Board configurations have been updated from boards.txt', 'success');
                styleBoardOptions();
            ", True)
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
