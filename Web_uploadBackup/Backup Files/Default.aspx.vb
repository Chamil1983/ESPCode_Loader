Imports System.IO
Imports System.IO.Compression
Imports System.IO.Ports
Imports System.Web.Services
Imports System.Web.Script.Services
Imports System.Text
Imports System.Threading

Public Class [Default]
    Inherits System.Web.UI.Page

    Private Shared ReadOnly boardManager As New BoardManager()

    ' Default Arduino ESP32 boards.txt path
    Private Const DEFAULT_BOARDS_TXT_PATH As String = "D:\Projects\Visual_Studio\ArduinoWeb_V1\App_Data\boards.txt"

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

    ' Add this to the top of your code-behind file
    Protected Property chkVerifyUpload As CheckBox
        Get
            ' Determine which verification checkbox to use based on the active tab
            Dim zipTabIsActive As Boolean = True

            ' Try to infer which tab is active based on which file upload controls have files
            If fuBootloader.HasFile OrElse fuPartitionTable.HasFile OrElse
           fuApplication.HasFile OrElse fuSpiffs.HasFile Then
                zipTabIsActive = False
            ElseIf fuBinaryZip.HasFile Then
                zipTabIsActive = True
            End If

            ' Return the appropriate checkbox
            If zipTabIsActive Then
                Return chkVerifyZipUpload
            Else
                Return chkVerifyBinUpload
            End If
        End Get
        Set(value As CheckBox)
            ' This is a read-only property
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
        Dim currentDateTime = "2025-08-10 11:03:55"
        Dim currentUser = "Chamil1983"
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "updateVersionInfo", $"
            document.addEventListener('DOMContentLoaded', function() {{
                var versionElement = document.querySelector('.version-info');
                if (versionElement) {{
                    versionElement.innerHTML = 'ESP32 Arduino Web Loader v1.5.3 | Last Updated: {currentDateTime} | User: {currentUser}';
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

            ' Initialize hardware boards
            LoadHardwareBoards()
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

    ' ZIP Binary Upload - Dedicated method for ZIP uploads only
    Protected Sub btnUploadZipBinary_Click(sender As Object, e As EventArgs)
        Try
            ' Capture and save the latest board configurations before uploading
            CaptureBoardOptionsFromUI()
            UpdateFQBNPreview()

            ' Initialize the output buffer once
            Dim bufferList As List(Of String) = OutputBuffer
            bufferList.Clear()
            txtOutput.Text = ""

            ' Get selected board and partition scheme
            Dim board = ddlBoard.SelectedValue
            Dim partitionScheme = ddlPartition.SelectedValue
            Dim serialPort = ddlSerial.SelectedValue
            Dim verifyUpload = chkVerifyZipUpload.Checked

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

            ' Create a temp directory for files
            Dim tempDir = Path.Combine(Server.MapPath("~/App_Data"), "TempBinary_" + DateTime.Now.Ticks.ToString())
            If Not Directory.Exists(tempDir) Then
                Directory.CreateDirectory(tempDir)
            End If

            ' Add mode indicator for logging
            bufferList.Add("ZIP Upload Mode Selected")

            ' Check if a binary file is already selected or being uploaded
            Dim binaryFilePath As String = txtBinaryPath.Text.Trim()

            ' If no binary file is already selected, check if one is being uploaded
            If String.IsNullOrEmpty(binaryFilePath) AndAlso fuBinaryZip.HasFile Then
                ' Get the uploaded file
                Dim fileExtension = Path.GetExtension(fuBinaryZip.FileName).ToLower()

                ' Check if it's a valid zip file
                If Not (fileExtension = ".zip") Then
                    txtOutput.Text = "Error: Only .zip files are allowed for ZIP upload mode."
                    Return
                End If

                ' Display file size for debugging
                Dim fileSize As Long = fuBinaryZip.PostedFile.ContentLength
                AppendToLog(String.Format("Uploading ZIP: {0}, Size: {1:N0} bytes", fuBinaryZip.FileName, fileSize))

                ' Update UI to show progress
                bufferList.Add(String.Format("Uploading file: {0} ({1:N0} bytes)...", fuBinaryZip.FileName, fileSize))
                txtOutput.Text = String.Join(Environment.NewLine, bufferList)

                ' Save the uploaded file
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

                ' Extract the ZIP file
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
                Catch ex As Exception
                    bufferList.Add(String.Format("Error extracting ZIP file: {0}", ex.Message))
                    AppendToLog(String.Format("Error extracting ZIP: {0}", ex.ToString()))
                    txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                    Return
                End Try
            ElseIf String.IsNullOrEmpty(binaryFilePath) Then
                txtOutput.Text = "Error: Please select a ZIP file to upload."
                Return
            ElseIf Not File.Exists(binaryFilePath) Then
                txtOutput.Text = String.Format("Error: The binary file '{0}' does not exist.", binaryFilePath)
                Return
            End If

            ' Update status to indicate upload started
            lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Uploading ZIP Binary...</span>"

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
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "resetUploadZipBinaryUI", "
            window.compileStartTime = Date.now(); 
            console.log('ZIP binary upload started at ' + window.compileStartTime);
            resetCompilationState();
            resetAndStartProgress();
        ", True)

            ' Run the binary upload with real-time output
            Dim result As ArduinoUtil.ExecResult =
            ArduinoUtil.RunBinaryUploadRealtime(ArduinoCliPath, binaryFilePath, fqbn, serialPort, verifyUpload,
                Sub(line)
                    bufferList.Add(line)
                    txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                End Sub)

            ' Handle upload result
            HandleUploadResult(result, bufferList, board, partitionScheme, compilationOptions, binaryFilePath)

        Catch ex As Exception
            ' Global exception handler
            AppendToLog(String.Format("ZIP binary upload error: {0}", ex.ToString()))
            txtOutput.Text = String.Format("Error during ZIP binary upload: {0}", ex.Message)
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> ZIP Upload Failed - Exception</span>"

            ' Show error notification
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadZipExceptionError", String.Format("
            showNotification('ZIP Upload Error', 'An error occurred: {0}', 'error');
        ", ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ")), True)
        End Try
    End Sub

    ' Multi-Binary Upload - Dedicated method for multiple binary files
    Protected Sub btnUploadMultiBinary_Click(sender As Object, e As EventArgs)
        Try
            ' Capture and save the latest board configurations before uploading
            CaptureBoardOptionsFromUI()
            UpdateFQBNPreview()

            ' Initialize the output buffer once
            Dim bufferList As List(Of String) = OutputBuffer
            bufferList.Clear()
            txtOutput.Text = ""

            ' Get selected board and partition scheme
            Dim board = ddlBoard.SelectedValue
            Dim partitionScheme = ddlPartition.SelectedValue
            Dim serialPort = ddlSerial.SelectedValue
            Dim verifyUpload = chkVerifyBinUpload.Checked

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

            ' Create a temp directory for files
            Dim tempDir = Path.Combine(Server.MapPath("~/App_Data"), "TempBinary_" + DateTime.Now.Ticks.ToString())
            If Not Directory.Exists(tempDir) Then
                Directory.CreateDirectory(tempDir)
            End If

            ' Add mode indicator for logging
            bufferList.Add("Multi-Binary Upload Mode Selected")

            ' Check if we have at least one binary file
            Dim hasBinaryFiles As Boolean = fuBootloader.HasFile OrElse fuPartitionTable.HasFile OrElse
                                       fuApplication.HasFile OrElse fuSpiffs.HasFile

            If Not hasBinaryFiles Then
                txtOutput.Text = "Error: Please select at least one binary file to upload."
                Return
            End If

            ' Update status to indicate upload started
            lblStatus.Text = "<span class='status-indicator pending'><i class='fas fa-spinner fa-spin'></i> Uploading Multiple Binaries...</span>"

            ' Save uploaded files to temp directory
            Dim binFiles As New Dictionary(Of String, String)() ' Address -> FilePath

            ' Process bootloader binary
            If fuBootloader.HasFile Then
                Dim bootloaderPath = Path.Combine(tempDir, "bootloader.bin")
                fuBootloader.SaveAs(bootloaderPath)
                binFiles.Add(txtBootloaderAddr.Text.Trim(), bootloaderPath)
                bufferList.Add(String.Format("Bootloader binary saved: {0} at address {1}",
                Path.GetFileName(bootloaderPath), txtBootloaderAddr.Text.Trim()))
            End If

            ' Process partition table binary
            If fuPartitionTable.HasFile Then
                Dim partitionPath = Path.Combine(tempDir, "partition-table.bin")
                fuPartitionTable.SaveAs(partitionPath)
                binFiles.Add(txtPartitionAddr.Text.Trim(), partitionPath)
                bufferList.Add(String.Format("Partition table binary saved: {0} at address {1}",
                Path.GetFileName(partitionPath), txtPartitionAddr.Text.Trim()))
            End If

            ' Process application binary
            If fuApplication.HasFile Then
                Dim appPath = Path.Combine(tempDir, "application.bin")
                fuApplication.SaveAs(appPath)
                binFiles.Add(txtAppAddr.Text.Trim(), appPath)
                bufferList.Add(String.Format("Application binary saved: {0} at address {1}",
                Path.GetFileName(appPath), txtAppAddr.Text.Trim()))
            End If

            ' Process SPIFFS/Data binary
            If fuSpiffs.HasFile Then
                Dim spiffsPath = Path.Combine(tempDir, "spiffs.bin")
                fuSpiffs.SaveAs(spiffsPath)
                binFiles.Add(txtSpiffsAddr.Text.Trim(), spiffsPath)
                bufferList.Add(String.Format("SPIFFS binary saved: {0} at address {1}",
                Path.GetFileName(spiffsPath), txtSpiffsAddr.Text.Trim()))
            End If

            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

            ' Register progress tracking script
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "resetUploadMultiBinaryUI", "
            window.compileStartTime = Date.now(); 
            console.log('Multi-binary upload started at ' + window.compileStartTime);
            resetCompilationState();
            resetAndStartProgress();
        ", True)

            ' Upload each binary file in sequence
            Dim overallSuccess As Boolean = True
            Dim uploadResults As New Dictionary(Of String, Boolean)()

            bufferList.Add(String.Format("Starting upload of {0} binary files...", binFiles.Count))
            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

            ' Find esptool path in common locations
            Dim espToolPath As String = ""
            Dim chipArg As String = "esp32"

            ' Try to find esptool.py from the FQBN
            If fqbn.Contains("esp32c3") Then
                chipArg = "esp32c3"
            ElseIf fqbn.Contains("esp32s2") Then
                chipArg = "esp32s2"
            ElseIf fqbn.Contains("esp32s3") Then
                chipArg = "esp32s3"
            ElseIf fqbn.Contains("esp32c6") Then
                chipArg = "esp32c6"
            ElseIf fqbn.Contains("esp32h2") Then
                chipArg = "esp32h2"
            End If

            ' Find esptool.py path in common locations
            Try
                ' Try to find in common paths instead of querying arduino-cli
                Dim arduinoDir = Path.GetDirectoryName(ArduinoCliPath)
                Dim userPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)

                Dim potentialPaths = New String() {
                Path.Combine(userPath, "Arduino15", "packages", "esp32", "tools", "esptool_py", "4.9.dev3", "esptool.exe"),
                Path.Combine(userPath, "Arduino15", "packages", "esp32", "tools", "esptool_py", "4.6.0", "esptool.exe"),
                Path.Combine(userPath, "Arduino15", "packages", "esp32", "tools", "esptool_py", "4.5.1", "esptool.exe"),
                Path.Combine(userPath, "Arduino15", "packages", "esp32", "tools", "esptool_py", "4.5", "esptool.exe"),
                Path.Combine(userPath, "Arduino15", "packages", "esp32", "tools", "esptool_py", "4.2.1", "esptool.exe"),
                Path.Combine(userPath, "Arduino15", "packages", "esp32", "tools", "esptool_py", "3.3.0", "esptool.exe"),
                Path.Combine(arduinoDir, "tools", "esptool", "esptool.exe"),
                Path.Combine(arduinoDir, "hardware", "esp32", "tools", "esptool.exe")
            }

                For Each path In potentialPaths
                    If File.Exists(path) Then
                        espToolPath = path
                        bufferList.Add("Found esptool in default location: " & espToolPath)
                        Exit For
                    End If
                Next

                ' If still not found, look for directory containing esptool
                If String.IsNullOrEmpty(espToolPath) Then
                    Dim arduinoPackagesDir = Path.Combine(userPath, "Arduino15", "packages")
                    If Directory.Exists(arduinoPackagesDir) Then
                        ' FIXED: Use 'dirPath' instead of 'Dir' as variable name
                        Dim esptoolDirs = Directory.GetDirectories(arduinoPackagesDir, "*esptool*", SearchOption.AllDirectories)
                        For Each dirPath In esptoolDirs
                            Dim esptoolFiles = Directory.GetFiles(dirPath, "esptool*", SearchOption.AllDirectories)
                            If esptoolFiles.Length > 0 Then
                                espToolPath = esptoolFiles(0)
                                bufferList.Add("Found esptool by directory search: " & espToolPath)
                                Exit For
                            End If
                        Next
                    End If
                End If

                ' If still not found, use a generic esptool command (may not work)
                If String.IsNullOrEmpty(espToolPath) Then
                    espToolPath = "esptool.py"
                    bufferList.Add("Could not find esptool.py, using generic command: " & espToolPath)
                End If
            Catch ex As Exception
                bufferList.Add("Error finding esptool: " & ex.Message)
                espToolPath = "esptool.py" ' Fallback
            End Try

            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

            ' Now upload each binary file directly with esptool.py specifying the correct address
            For Each binFile In binFiles
                Dim address = binFile.Key
                Dim filePath = binFile.Value
                Dim fileName = Path.GetFileName(filePath)

                bufferList.Add("")
                bufferList.Add(String.Format("=== Uploading {0} to address {1} ===", fileName, address))
                txtOutput.Text = String.Join(Environment.NewLine, bufferList)

                ' Construct esptool.py command with the correct address and file
                Dim verifyFlag = If(verifyUpload, " --verify", "")
                Dim baudRate = "921600" ' Default baud rate

                ' Extract baud rate from upload speed in board options
                If compilationOptions.ContainsKey("UploadSpeed") Then
                    baudRate = compilationOptions("UploadSpeed")
                End If

                ' Build the esptool.py command with correct format
                Dim esptoolCmd As String
                If espToolPath.EndsWith(".py") Then
                    esptoolCmd = "python " & espToolPath
                Else
                    esptoolCmd = espToolPath
                End If

                ' Create direct esptool.py command with proper arguments - only flash one binary at a time
                Dim esptoolArgs = String.Format("--chip {0} --port {1} --baud {2} --before default_reset --after hard_reset write_flash{3} -z {4} ""{5}""",
                chipArg, serialPort, baudRate, verifyFlag, address, filePath)

                bufferList.Add("Command: " & esptoolCmd & " " & esptoolArgs)
                txtOutput.Text = String.Join(Environment.NewLine, bufferList)

                ' Execute esptool.py directly using Process class
                Try
                    Dim proc As New Process()
                    proc.StartInfo.FileName = If(espToolPath.EndsWith(".py"), "python", espToolPath)

                    ' If using python, we need to add the esptool.py path as an argument
                    If espToolPath.EndsWith(".py") Then
                        proc.StartInfo.Arguments = espToolPath & " " & esptoolArgs
                    Else
                        proc.StartInfo.Arguments = esptoolArgs
                    End If

                    proc.StartInfo.UseShellExecute = False
                    proc.StartInfo.RedirectStandardOutput = True
                    proc.StartInfo.RedirectStandardError = True
                    proc.StartInfo.CreateNoWindow = True

                    ' Set up output handlers - with corrected parameter naming
                    Dim outputBuffer As New StringBuilder()
                    AddHandler proc.OutputDataReceived, Sub(s, evt)
                                                            If evt.Data IsNot Nothing Then
                                                                outputBuffer.AppendLine(evt.Data)
                                                                bufferList.Add(evt.Data)
                                                                txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                                                            End If
                                                        End Sub

                    AddHandler proc.ErrorDataReceived, Sub(s, evt)
                                                           If evt.Data IsNot Nothing Then
                                                               outputBuffer.AppendLine(evt.Data)
                                                               bufferList.Add(evt.Data)
                                                               txtOutput.Text = String.Join(Environment.NewLine, bufferList)
                                                           End If
                                                       End Sub

                    ' Start the process
                    proc.Start()
                    proc.BeginOutputReadLine()
                    proc.BeginErrorReadLine()
                    proc.WaitForExit()

                    ' Check result
                    If proc.ExitCode = 0 Then
                        uploadResults(address) = True
                        bufferList.Add(String.Format("Successfully uploaded {0} to address {1}", fileName, address))
                    Else
                        uploadResults(address) = False
                        overallSuccess = False
                        bufferList.Add(String.Format("Failed to upload {0} to address {1}", fileName, address))
                    End If
                Catch ex As Exception
                    uploadResults(address) = False
                    overallSuccess = False
                    bufferList.Add(String.Format("Error executing esptool: {0}", ex.Message))
                End Try

                txtOutput.Text = String.Join(Environment.NewLine, bufferList)
            Next

            ' Show final results
            bufferList.Add("")
            bufferList.Add("=== Multi-Binary Upload Results ===")

            For Each result In uploadResults
                bufferList.Add(String.Format("{0}: {1}", result.Key, If(result.Value, "Success ✓", "Failed ✗")))
            Next

            ' Update status based on overall result
            If overallSuccess Then
                lblStatus.Text = "<span class='status-indicator success'><i class='fas fa-check-circle'></i> Multi-Binary Upload Successful</span>"
                bufferList.Add("")
                bufferList.Add("All binary files uploaded successfully!")

                ' Show success notification
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadMultiBinarySuccess", "
                showNotification('Upload Successful', 'All binary files have been uploaded to the ESP32 device successfully.', 'success');
            ", True)
            Else
                lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Multi-Binary Upload Failed</span>"
                bufferList.Add("")
                bufferList.Add("Some binary files failed to upload. Check the results above.")

                ' Show error notification
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadMultiBinaryFailed", "
                showNotification('Upload Failed', 'Some binary files failed to upload. Check the output for details.', 'error');
            ", True)
            End If

            txtOutput.Text = String.Join(Environment.NewLine, bufferList)

        Catch ex As Exception
            ' Global exception handler
            AppendToLog(String.Format("Multi-binary upload error: {0}", ex.ToString()))
            txtOutput.Text = String.Format("Error during multi-binary upload: {0}", ex.Message)
            lblStatus.Text = "<span class='status-indicator error'><i class='fas fa-exclamation-circle'></i> Multi-Binary Upload Failed - Exception</span>"

            ' Show error notification
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "uploadMultiBinaryExceptionError", String.Format("
            showNotification('Multi-Binary Upload Error', 'An error occurred: {0}', 'error');
        ", ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ")), True)
        End Try
    End Sub

    ' Keep the original method for backwards compatibility if needed
    Protected Sub btnUploadBinary_Click(sender As Object, e As EventArgs)
        ' This is a compatibility method that delegates to the appropriate new method

        ' For server-side determination, check which files are present
        If fuBootloader.HasFile OrElse fuPartitionTable.HasFile OrElse
       fuApplication.HasFile OrElse fuSpiffs.HasFile Then
            ' Multi-binary upload
            btnUploadMultiBinary_Click(sender, e)
        ElseIf fuBinaryZip.HasFile OrElse Not String.IsNullOrEmpty(txtBinaryPath.Text.Trim()) Then
            ' ZIP upload
            btnUploadZipBinary_Click(sender, e)
        Else
            ' No files selected, show error
            txtOutput.Text = "Error: Please select files to upload in either the ZIP or Multi-Binary tabs."
        End If
    End Sub

    ' Helper method to handle upload results
    Private Sub HandleUploadResult(result As ArduinoUtil.ExecResult, bufferList As List(Of String), board As String, partitionScheme As String, compilationOptions As Dictionary(Of String, String), binaryFilePath As String)
        ' Update status based on result
        If result.Success Then
            bufferList.Add("Binary upload completed successfully")

            ' Check output for verification success
            Dim verificationSuccessful = True
            If chkVerifyUpload.Checked Then
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
            If chkVerifyUpload.Checked AndAlso Not verificationSuccessful Then
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

            ' Add specific board options
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
            ElseIf result.Output.Contains("Cannot open") AndAlso result.Output.Contains(ddlSerial.SelectedValue) Then
                errorMsg = String.Format("Cannot open serial port {0}. Port may be in use or unavailable", ddlSerial.SelectedValue)
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
    End Sub

    ' Handle binary file selection change events
    Protected Sub fuBootloader_Change(sender As Object, e As EventArgs)
        If fuBootloader.HasFile Then
            txtBootloaderPath.Text = fuBootloader.FileName

            ' Add UI notification via JavaScript
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "bootloaderSelected", String.Format("
            showNotification('File Selected', 'Bootloader binary {0} selected for address 0x1000.', 'info');
            var row = document.querySelector('.binary-row:nth-child(1)');
            if(row) {{
                row.classList.add('selected');
                setTimeout(function() {{
                    row.classList.remove('selected');
                }}, 1500);
            }}
        ", fuBootloader.FileName), True)
        End If
    End Sub

    Protected Sub fuPartitionTable_Change(sender As Object, e As EventArgs)
        If fuPartitionTable.HasFile Then
            txtPartitionPath.Text = fuPartitionTable.FileName

            ' Add UI notification via JavaScript
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "partitionSelected", String.Format("
            showNotification('File Selected', 'Partition table binary {0} selected for address 0x8000.', 'info');
            var row = document.querySelector('.binary-row:nth-child(2)');
            if(row) {{
                row.classList.add('selected');
                setTimeout(function() {{
                    row.classList.remove('selected');
                }}, 1500);
            }}
        ", fuPartitionTable.FileName), True)
        End If
    End Sub

    Protected Sub fuApplication_Change(sender As Object, e As EventArgs)
        If fuApplication.HasFile Then
            txtAppPath.Text = fuApplication.FileName

            ' Add UI notification via JavaScript
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "appSelected", String.Format("
            showNotification('File Selected', 'Application binary {0} selected for address 0x10000.', 'info');
            var row = document.querySelector('.binary-row:nth-child(3)');
            if(row) {{
                row.classList.add('selected');
                setTimeout(function() {{
                    row.classList.remove('selected');
                }}, 1500);
            }}
        ", fuApplication.FileName), True)
        End If
    End Sub

    Protected Sub fuSpiffs_Change(sender As Object, e As EventArgs)
        If fuSpiffs.HasFile Then
            txtSpiffsPath.Text = fuSpiffs.FileName

            ' Add UI notification via JavaScript
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "spiffsSelected", String.Format("
            showNotification('File Selected', 'SPIFFS binary {0} selected for address 0x290000.', 'info');
            var row = document.querySelector('.binary-row:nth-child(4)');
            if(row) {{
                row.classList.add('selected');
                setTimeout(function() {{
                    row.classList.remove('selected');
                }}, 1500);
            }}
        ", fuSpiffs.FileName), True)
        End If
    End Sub

    ' File upload event handlers
    Protected Sub fuBinaryZip_Change(sender As Object, e As EventArgs)
        If fuBinaryZip.HasFile Then
            txtBinaryPath.Text = fuBinaryZip.FileName

            ' Add UI notification
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "zipFileSelected", String.Format("
            showNotification('File Selected', 'ZIP file {0} selected for upload.', 'info');
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

    ' -------------------- HARDWARE BOARD MANAGEMENT --------------------
    ' Event handler for hardware board selection change
    Protected Sub lstHardwareBoards_SelectedIndexChanged(sender As Object, e As EventArgs)
        If lstHardwareBoards.SelectedIndex >= 0 Then
            Dim selectedBoardId As Guid
            If Guid.TryParse(lstHardwareBoards.SelectedValue, selectedBoardId) Then
                hidSelectedBoardId.Value = selectedBoardId.ToString()

                ' Display board details and update dropdowns
                DisplaySelectedBoardDetails(selectedBoardId)

                ' Show a notification to confirm selection
                ShowNotification("Board Selected",
                $"Hardware board '{lstHardwareBoards.SelectedItem.Text}' has been selected. Board settings have been applied.",
                "info")

                ' Update the last used timestamp
                HardwareBoardManager.UseBoard(selectedBoardId)
            End If
        Else
            ClearBoardDetails()
        End If
    End Sub

    ' Display selected hardware board details with enhanced information
    Private Sub DisplaySelectedBoardDetails(boardId As Guid)
        Dim board = HardwareBoardManager.GetBoard(boardId)
        If board IsNot Nothing Then
            ' Basic board information
            txtBoardName.Text = board.Name
            txtBoardDescription.Text = board.Description
            lblBoardProcessor.Text = board.ProcessorType
            lblBoardPartition.Text = board.PartitionScheme
            lblBoardLastUsed.Text = board.LastUsed.ToString("yyyy-MM-dd HH:mm:ss")

            ' Additional information - add these labels to your boardDetailsPanel
            If lblBoardUsageCount IsNot Nothing Then
                lblBoardUsageCount.Text = board.UseCount.ToString()
            End If

            ' Display FQBN information if we have the controls
            Dim fqbnPreview = ""
            Try
                ' Generate FQBN from board settings
                fqbnPreview = boardManager.GetFQBN(board.ProcessorType, board.PartitionScheme, "", board.ConfigOptions)

                If lblBoardFQBN IsNot Nothing Then
                    lblBoardFQBN.Text = fqbnPreview
                End If
            Catch ex As Exception
                ' Handle any FQBN generation errors
                AppendToLog($"Error generating FQBN for board details: {ex.Message}")
                If lblBoardFQBN IsNot Nothing Then
                    lblBoardFQBN.Text = "Error generating FQBN"
                End If
            End Try

            ' Count options
            If lblBoardOptionCount IsNot Nothing Then
                lblBoardOptionCount.Text = board.ConfigOptions.Count.ToString()
            End If

            ' Update processor and partition scheme selectors to match the board
            UpdateProcessorAndPartition(board)

            ' Show the details panel
            boardDetailsPanel.Visible = True
        Else
            ClearBoardDetails()
        End If
    End Sub

    ' Update processor and partition dropdowns to match the selected hardware board
    Private Sub UpdateProcessorAndPartition(board As HardwareBoard)
        Try
            ' Update processor dropdown
            If board IsNot Nothing AndAlso Not String.IsNullOrEmpty(board.ProcessorType) Then
                If ddlBoard.Items.FindByValue(board.ProcessorType) IsNot Nothing Then
                    ' Only change if different to avoid postbacks
                    If ddlBoard.SelectedValue <> board.ProcessorType Then
                        ddlBoard.SelectedValue = board.ProcessorType

                        ' This is important - we need to update partitions after changing the processor
                        PopulatePartitions()

                        AppendToLog($"Updated processor dropdown to {board.ProcessorType} for hardware board {board.Name}")
                    End If
                Else
                    AppendToLog($"Warning: Processor {board.ProcessorType} not found in dropdown for hardware board {board.Name}")
                End If
            End If

            ' Update partition dropdown
            If board IsNot Nothing AndAlso Not String.IsNullOrEmpty(board.PartitionScheme) Then
                If ddlPartition.Items.FindByValue(board.PartitionScheme) IsNot Nothing Then
                    ' Only change if different to avoid postbacks
                    If ddlPartition.SelectedValue <> board.PartitionScheme Then
                        ddlPartition.SelectedValue = board.PartitionScheme
                        AppendToLog($"Updated partition dropdown to {board.PartitionScheme} for hardware board {board.Name}")
                    End If
                Else
                    AppendToLog($"Warning: Partition scheme {board.PartitionScheme} not found in dropdown for hardware board {board.Name}")
                End If
            End If

            ' Update board options to match the hardware board
            BoardConfigOptions.Clear()
            For Each kvp As KeyValuePair(Of String, String) In board.ConfigOptions
                BoardConfigOptions(kvp.Key) = kvp.Value
            Next

            ' Update session storage
            Session("BoardConfigOptions") = BoardConfigOptions

            ' Refresh the board options UI to reflect the hardware board settings
            PopulateBoardOptions()

            ' Update FQBN preview
            UpdateFQBNPreview()

            ' Update status panels
            ShowBoardsTxtStatus()
            ShowCustomPartitionStatus()
            UpdatePartitionCount()

        Catch ex As Exception
            AppendToLog($"Error updating processor and partition dropdowns: {ex.Message}")
        End Try
    End Sub

    ' Clear hardware board details
    Private Sub ClearBoardDetails()
        txtBoardName.Text = String.Empty
        txtBoardDescription.Text = String.Empty
        lblBoardProcessor.Text = String.Empty
        lblBoardPartition.Text = String.Empty
        lblBoardLastUsed.Text = String.Empty
        hidSelectedBoardId.Value = String.Empty

        boardDetailsPanel.Visible = False
    End Sub

    ' Load hardware boards into the listbox
    Private Sub LoadHardwareBoards()
        lstHardwareBoards.Items.Clear()

        Dim boards = HardwareBoardManager.GetAllBoards()
        For Each board In boards
            Dim item = New ListItem(board.GetDisplayName(), board.Id.ToString())
            lstHardwareBoards.Items.Add(item)
        Next

        ' If there are boards, select the first one
        If lstHardwareBoards.Items.Count > 0 Then
            lstHardwareBoards.SelectedIndex = 0

            ' Get selected board ID
            Dim selectedBoardId As Guid
            If Guid.TryParse(lstHardwareBoards.SelectedValue, selectedBoardId) Then
                hidSelectedBoardId.Value = selectedBoardId.ToString()
                DisplaySelectedBoardDetails(selectedBoardId)
            End If
        Else
            ClearBoardDetails()
        End If
    End Sub

    ' Add a new hardware board
    Protected Sub btnAddBoard_Click(sender As Object, e As EventArgs)
        ' Validate input
        If String.IsNullOrWhiteSpace(txtBoardName.Text) Then
            ShowNotification("Error", "Please enter a name for the hardware board", "error")
            Return
        End If

        ' Create a new hardware board with current settings
        Dim board = HardwareBoard.CreateFromCurrentSettings(
            txtBoardName.Text.Trim(),
            txtBoardDescription.Text.Trim(),
            ddlBoard.SelectedValue,
            ddlPartition.SelectedValue,
            BoardConfigOptions
        )

        ' Add the board
        If HardwareBoardManager.AddBoard(board) Then
            ShowNotification("Success", "Hardware board added successfully", "success")
            LoadHardwareBoards()
        Else
            ShowNotification("Error", "A hardware board with this name already exists", "error")
        End If
    End Sub

    ' Update an existing hardware board
    Protected Sub btnUpdateBoard_Click(sender As Object, e As EventArgs)
        ' Validate input
        If String.IsNullOrWhiteSpace(txtBoardName.Text) Then
            ShowNotification("Error", "Please enter a name for the hardware board", "error")
            Return
        End If

        ' Check if a board is selected
        Dim selectedBoardId As Guid
        If Not Guid.TryParse(hidSelectedBoardId.Value, selectedBoardId) Then
            ShowNotification("Error", "No hardware board selected", "error")
            Return
        End If

        ' Get the selected board
        Dim board = HardwareBoardManager.GetBoard(selectedBoardId)
        If board Is Nothing Then
            ShowNotification("Error", "Selected hardware board not found", "error")
            Return
        End If

        ' Update board properties
        board.Name = txtBoardName.Text.Trim()
        board.Description = txtBoardDescription.Text.Trim()
        board.ProcessorType = ddlBoard.SelectedValue
        board.PartitionScheme = ddlPartition.SelectedValue

        ' Update configuration options
        board.ConfigOptions.Clear()
        For Each kvp As KeyValuePair(Of String, String) In BoardConfigOptions
            board.ConfigOptions(kvp.Key) = kvp.Value
        Next

        ' Save the changes
        If HardwareBoardManager.UpdateBoard(board) Then
            ShowNotification("Success", "Hardware board updated successfully", "success")
            LoadHardwareBoards()
        Else
            ShowNotification("Error", "Failed to update hardware board", "error")
        End If
    End Sub

    ' Delete a hardware board
    Protected Sub btnDeleteBoard_Click(sender As Object, e As EventArgs)
        ' Check if a board is selected
        Dim selectedBoardId As Guid
        If Not Guid.TryParse(hidSelectedBoardId.Value, selectedBoardId) Then
            ShowNotification("Error", "No hardware board selected", "error")
            Return
        End If

        ' Delete the board
        If HardwareBoardManager.DeleteBoard(selectedBoardId) Then
            ShowNotification("Success", "Hardware board deleted successfully", "success")
            LoadHardwareBoards()
            ClearBoardDetails()
        Else
            ShowNotification("Error", "Failed to delete hardware board", "error")
        End If
    End Sub

    ' Use a selected hardware board
    Protected Sub btnUseBoard_Click(sender As Object, e As EventArgs)
        ' Check if a board is selected
        Dim selectedBoardId As Guid
        If Not Guid.TryParse(hidSelectedBoardId.Value, selectedBoardId) Then
            ShowNotification("Error", "No hardware board selected", "error")
            Return
        End If

        ' Get the selected board
        Dim board = HardwareBoardManager.GetBoard(selectedBoardId)
        If board Is Nothing Then
            ShowNotification("Error", "Selected hardware board not found", "error")
            Return
        End If

        ' Apply board settings
        Try
            ' Set processor
            If ddlBoard.Items.FindByValue(board.ProcessorType) IsNot Nothing Then
                ddlBoard.SelectedValue = board.ProcessorType

                ' After changing processor, update dependent dropdowns
                SetDefaultBoardConfigurations(board.ProcessorType)
                PopulatePartitions()
                PopulateBoardOptions()
            End If

            ' Set partition scheme (do this after board selection)
            If ddlPartition.Items.FindByValue(board.PartitionScheme) IsNot Nothing Then
                ddlPartition.SelectedValue = board.PartitionScheme
            End If

            ' Apply configuration options
            BoardConfigOptions.Clear()
            For Each kvp As KeyValuePair(Of String, String) In board.ConfigOptions
                BoardConfigOptions(kvp.Key) = kvp.Value
            Next

            ' Refresh UI
            PopulateBoardOptions()
            UpdateFQBNPreview()

            ' Mark board as used
            HardwareBoardManager.UseBoard(selectedBoardId)

            ' Update display
            DisplaySelectedBoardDetails(selectedBoardId)

            ShowNotification("Success", $"Now using hardware board: {board.Name}", "success")
        Catch ex As Exception
            AppendToLog($"Error applying hardware board settings: {ex.Message}")
            ShowNotification("Error", "Failed to apply hardware board settings", "error")
        End Try
    End Sub

    ' Helper method to show notifications
    Private Sub ShowNotification(title As String, message As String, type As String)
        ' Register script for notification
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "notification_" & DateTime.Now.Ticks,
            $"showNotification('{title}', '{message}', '{type}');", True)
    End Sub
End Class