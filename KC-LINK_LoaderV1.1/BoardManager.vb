Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Windows.Forms
Imports System.Text.RegularExpressions
Imports System.Linq

Public Class BoardManager

    ' Private fields
    Private boardConfigurations As Dictionary(Of String, String) = New Dictionary(Of String, String)()
    Private boardParameters As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))()
    Private boardMenuOptions As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))) = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))()
    Private boardIdMap As Dictionary(Of String, String) = New Dictionary(Of String, String)()
    Private boardSupportedMenus As Dictionary(Of String, HashSet(Of String)) = New Dictionary(Of String, HashSet(Of String))()
    Private boardUnsupportedMenus As Dictionary(Of String, HashSet(Of String)) = New Dictionary(Of String, HashSet(Of String))()
    Private boardFixedParams As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))()
    Private boardConfigOrder As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))()
    Private customPartitionFile As String = String.Empty
    Private boardsFileContent As String = String.Empty

    ' Properties
    Public Property BoardsFilePath As String = String.Empty

    ' Constructor
    Public Sub New()
        ' Set default boards file location in application directory
        BoardsFilePath = Path.Combine(Application.StartupPath, "hardware", "esp32", "boards.txt")

        ' Try to use Arduino's default location if available
        Dim defaultLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "arduino15", "packages", "esp32", "hardware", "esp32")

        If Directory.Exists(defaultLocation) Then
            ' Find the latest version directory
            Dim versionDirs = Directory.GetDirectories(defaultLocation)
            If versionDirs.Length > 0 Then
                ' Sort by version number (assuming semantic versioning)
                Array.Sort(versionDirs, New VersionComparer())
                BoardsFilePath = Path.Combine(versionDirs(versionDirs.Length - 1), "boards.txt")
            End If
        End If

        ' Check if the file exists, otherwise use the application directory
        If Not File.Exists(BoardsFilePath) Then
            BoardsFilePath = Path.Combine(Application.StartupPath, "hardware", "esp32", "boards.txt")
        End If

        ' Load configurations
        LoadBoardConfigurations()
    End Sub

    ' Public methods
    Public Sub LoadBoardConfigurations()
        ' Clear existing configurations
        boardConfigurations.Clear()
        boardParameters.Clear()
        boardMenuOptions.Clear()
        boardIdMap.Clear()
        boardSupportedMenus.Clear()
        boardUnsupportedMenus.Clear()
        boardFixedParams.Clear()
        boardConfigOrder.Clear()
        boardsFileContent = String.Empty

        ' Add default configurations (fallback only)
        AddDefaultConfigurations()

        ' Load custom configurations if file exists
        If File.Exists(BoardsFilePath) Then
            Try
                ' Read the entire file content for analysis
                boardsFileContent = File.ReadAllText(BoardsFilePath)
                Dim lines As String() = File.ReadAllLines(BoardsFilePath)
                Debug.WriteLine($"[2025-08-16 20:22:36] Loading boards from: {BoardsFilePath} by Chamil1983")

                ParseBoardsFile(lines)

                ' Perform post-processing to ensure proper compatibility
                PostProcessBoardConfigs()

                ' Log loaded configurations
                Debug.WriteLine($"[2025-08-16 20:22:36] Loaded {boardConfigurations.Count} board configurations by Chamil1983")

            Catch ex As Exception
                MessageBox.Show($"Error loading board configurations: {ex.Message}",
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Debug.WriteLine($"[2025-08-16 20:22:36] Error loading boards: {ex.Message} by Chamil1983")
            End Try
        Else
            Debug.WriteLine($"[2025-08-16 20:22:36] Boards file not found: {BoardsFilePath} by Chamil1983")
        End If
    End Sub

    ' Post-process board configurations to ensure proper compatibility
    Private Sub PostProcessBoardConfigs()
        ' Process each board to identify fixed parameters and incompatible options
        For Each boardName In boardIdMap.Keys
            Dim boardId = boardIdMap(boardName)

            ' Create fixed parameters dictionary if it doesn't exist
            If Not boardFixedParams.ContainsKey(boardName) Then
                boardFixedParams(boardName) = New Dictionary(Of String, String)()
            End If

            ' Create config order list if it doesn't exist
            If Not boardConfigOrder.ContainsKey(boardName) Then
                boardConfigOrder(boardName) = New List(Of String)()
            End If

            ' Default order of parameters
            Dim defaultOrder As New List(Of String) From {
                "UploadSpeed", "CPUFreq", "FlashFreq", "FlashMode", "PartitionScheme",
                "DebugLevel", "PSRAM", "EraseFlash", "JTAGAdapter", "LoopCore",
                "EventsCore", "ZigbeeMode"
            }

            ' Add board-specific parameters to the order
            If boardSupportedMenus.ContainsKey(boardName) Then
                For Each menu In boardSupportedMenus(boardName)
                    If Not defaultOrder.Contains(menu) Then
                        defaultOrder.Add(menu)
                    End If
                Next
            End If

            ' Set the config order
            boardConfigOrder(boardName) = defaultOrder

            ' Analyze boards.txt content for special handling
            AnalyzeSpecialBoardHandling(boardName, boardId)

            Debug.WriteLine($"[2025-08-16 20:22:36] Post-processed board: {boardName}, ID: {boardId} by Chamil1983")
        Next
    End Sub

    ' Analyze boards.txt for special board handling requirements
    Private Sub AnalyzeSpecialBoardHandling(boardName As String, boardId As String)
        ' Don't process if we don't have the boards.txt content
        If String.IsNullOrEmpty(boardsFileContent) Then
            Return
        End If

        ' Patterns to look for in boards.txt that indicate fixed parameters
        Dim fixedPatterns As New Dictionary(Of String, String) From {
            {"build.psram_type", "PSRAM"},
            {"build.flash_type", "FlashMode"},
            {"build.flash_freq", "FlashFreq"},
            {"build.f_cpu", "CPUFreq"},
            {"build.default_psram", "PSRAM"}
        }

        ' Check if this is a Wrover board (needs special handling)
        Dim isWroverBoard = (boardId.ToLower().Contains("wrover") OrElse boardName.ToLower().Contains("wrover"))

        If isWroverBoard Then
            Debug.WriteLine($"[2025-08-16 20:22:36] Special handling for Wrover board: {boardName} by Chamil1983")

            ' Search for relevant sections in boards.txt
            Dim wroverSection = ExtractBoardSection(boardId)

            ' Look for fixed PSRAM indications
            If wroverSection.Contains("build.default_psram=true") OrElse
               wroverSection.Contains("build.has_psram=true") Then

                ' PSRAM is always enabled in Wrover and cannot be configured
                If Not boardUnsupportedMenus.ContainsKey(boardName) Then
                    boardUnsupportedMenus(boardName) = New HashSet(Of String)()
                End If
                boardUnsupportedMenus(boardName).Add("PSRAM")
                boardFixedParams(boardName)("PSRAM") = "enabled"
                Debug.WriteLine($"[2025-08-16 20:22:36] Detected Wrover has fixed PSRAM=enabled by Chamil1983")
            End If

            ' Look for fixed CPU frequency indications
            If wroverSection.Contains("build.f_cpu=240000000L") AndAlso
               Not wroverSection.Contains("menu.CPUFreq") Then

                ' CPU frequency is fixed at 240MHz in Wrover
                If Not boardUnsupportedMenus.ContainsKey(boardName) Then
                    boardUnsupportedMenus(boardName) = New HashSet(Of String)()
                End If
                boardUnsupportedMenus(boardName).Add("CPUFreq")
                boardFixedParams(boardName)("CPUFreq") = "240"
                Debug.WriteLine($"[2025-08-16 20:22:36] Detected Wrover has fixed CPUFreq=240MHz by Chamil1983")
            End If
        End If

        ' Handle ESP32-S2/S3/C3 specific incompatibilities
        If boardId.Contains("esp32s3") OrElse boardId.Contains("esp32c5") Then

            ' These boards don't support the FlashFreq parameter
            If Not boardUnsupportedMenus.ContainsKey(boardName) Then
                boardUnsupportedMenus(boardName) = New HashSet(Of String)()
            End If
            boardUnsupportedMenus(boardName).Add("FlashFreq")
            Debug.WriteLine($"[2025-08-16 20:22:36] Detected {boardId} doesn't support FlashFreq by Chamil1983")
        End If

        ' Handle boards.txt pattern analysis
        Dim boardSection = ExtractBoardSection(boardId)

        For Each pattern In fixedPatterns
            ' See if this parameter has a fixed value in boards.txt
            Dim buildPattern = pattern.Key & "="
            Dim menuPattern = "menu." & pattern.Value

            If boardSection.Contains(buildPattern) AndAlso Not boardSection.Contains(menuPattern) Then
                ' This parameter appears to be fixed (has build value but no menu options)
                Dim patternRegex = New Regex(pattern.Key & "=([^\r\n]+)")
                Dim match = patternRegex.Match(boardSection)

                If match.Success Then
                    Dim fixedValue = match.Groups(1).Value.Trim()

                    ' Convert to appropriate parameter value
                    Select Case pattern.Value
                        Case "CPUFreq"
                            If fixedValue.EndsWith("000000L") Then
                                fixedValue = (Long.Parse(fixedValue.Replace("L", "")) / 1000000).ToString()
                            End If
                        Case "PSRAM"
                            fixedValue = If(fixedValue.ToLower() = "true", "enabled", "disabled")
                    End Select

                    ' Add to fixed parameters
                    If Not boardUnsupportedMenus.ContainsKey(boardName) Then
                        boardUnsupportedMenus(boardName) = New HashSet(Of String)()
                    End If
                    boardUnsupportedMenus(boardName).Add(pattern.Value)
                    boardFixedParams(boardName)(pattern.Value) = fixedValue

                    Debug.WriteLine($"[2025-08-16 20:22:36] Detected {boardName} has fixed {pattern.Value}={fixedValue} by Chamil1983")
                End If
            End If
        Next
    End Sub

    ' Extract a board's section from boards.txt
    Private Function ExtractBoardSection(boardId As String) As String
        ' Create a pattern to match the entire board section
        Dim pattern = boardId & "\.[^=]+=.*?(?=\r?\n\r?\n|\r?\n[^\.]|$)"
        Dim regex = New Regex(pattern, RegexOptions.Singleline)
        Dim match = regex.Match(boardsFileContent)

        If match.Success Then
            Return match.Value
        End If

        Return String.Empty
    End Function

    ' Parse boards.txt file to extract all board configurations
    Private Sub ParseBoardsFile(lines As String())
        Debug.WriteLine($"[2025-08-16 20:22:36] Parsing boards.txt file with {lines.Length} lines by Chamil1983")

        ' First pass: extract global menu options
        Dim globalMenus As New Dictionary(Of String, String)()

        For Each line In lines
            If line.Trim().StartsWith("menu.") AndAlso line.Contains("=") Then
                Try
                    Dim parts = line.Split(New Char() {"="c}, 2)
                    If parts.Length = 2 Then
                        Dim menuKey = parts(0).Trim()
                        Dim menuValue = parts(1).Trim()
                        globalMenus(menuKey) = menuValue
                        Debug.WriteLine($"[2025-08-16 20:22:36] Found global menu: {menuKey}={menuValue} by Chamil1983")
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[2025-08-16 20:22:36] Error parsing global menu: {line}, {ex.Message} by Chamil1983")
                End Try
            End If
        Next

        ' Second pass: identify all boards
        For Each line In lines
            If line.Contains(".name=") Then
                Try
                    Dim parts = line.Split(New Char() {"."c}, 2)
                    If parts.Length >= 2 Then
                        Dim boardId = parts(0).Trim()
                        parts = parts(1).Split(New Char() {"="c}, 2)
                        If parts.Length >= 2 Then
                            Dim boardName = parts(1).Trim()

                            ' Only add specified board types
                            If IsValidBoardName(boardName, boardId) Then
                                ' Add board ID to name mapping
                                boardIdMap(boardName) = boardId

                                ' Initialize parameter dictionary for this board
                                boardParameters(boardName) = New Dictionary(Of String, String)()

                                ' Initialize menu options dictionary for this board
                                boardMenuOptions(boardName) = New Dictionary(Of String, Dictionary(Of String, String))()

                                ' Initialize supported menus set for this board
                                boardSupportedMenus(boardName) = New HashSet(Of String)()

                                ' Initialize unsupported menus set for this board
                                boardUnsupportedMenus(boardName) = New HashSet(Of String)()

                                ' Initialize fixed parameters dictionary for this board
                                boardFixedParams(boardName) = New Dictionary(Of String, String)()

                                Debug.WriteLine($"[2025-08-16 20:22:36] Found board: {boardId}={boardName} by Chamil1983")
                            End If
                        End If
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[2025-08-16 20:22:36] Error parsing board name: {line}, {ex.Message} by Chamil1983")
                End Try
            End If
        Next

        ' Third pass: extract all parameters and menu options for each board
        For Each boardName In boardIdMap.Keys
            Dim boardId = boardIdMap(boardName)
            Dim parameters = boardParameters(boardName)
            Dim menuOptions = boardMenuOptions(boardName)
            Dim supportedMenus = boardSupportedMenus(boardName)

            ' Copy global menu titles to this board
            For Each menuEntry In globalMenus
                Dim menuKey = menuEntry.Key
                Dim menuTitle = menuEntry.Value

                ' Extract menu type (e.g., menu.FlashFreq -> FlashFreq)
                If menuKey.StartsWith("menu.") AndAlso menuKey.IndexOf(".", 5) = -1 Then
                    Dim menuType = menuKey.Substring(5)
                    parameters(menuKey) = menuTitle
                    menuOptions(menuType) = New Dictionary(Of String, String)()
                End If
            Next

            ' Extract all parameters for this board
            For Each line In lines
                If line.StartsWith(boardId & ".") Then
                    Try
                        Dim lineWithoutBoardId = line.Substring(boardId.Length + 1)
                        Dim equalsPos = lineWithoutBoardId.IndexOf("=")

                        If equalsPos > 0 Then
                            Dim key = lineWithoutBoardId.Substring(0, equalsPos).Trim()
                            Dim value = lineWithoutBoardId.Substring(equalsPos + 1).Trim()

                            ' Store all parameters
                            parameters(key) = value

                            ' Check if this is a menu option
                            If key.StartsWith("menu.") Then
                                Dim menuParts = key.Split(New Char() {"."c}, 4)

                                If menuParts.Length >= 3 Then
                                    Dim menuType = menuParts(1)
                                    Dim optionKey = menuParts(2)

                                    ' Add this menu type to supported menus for this board
                                    supportedMenus.Add(menuType)

                                    ' Make sure the menu type dictionary exists
                                    If Not menuOptions.ContainsKey(menuType) Then
                                        menuOptions(menuType) = New Dictionary(Of String, String)()
                                    End If

                                    ' Handle different menu option formats
                                    If menuParts.Length >= 4 Then
                                        ' Format: menu.CPUFreq.240.name=240MHz
                                        If menuParts(3) = "name" Then
                                            menuOptions(menuType)(optionKey) = value
                                            Debug.WriteLine($"[2025-08-16 20:22:36] Board {boardName} named menu option: {menuType}.{optionKey}={value} by Chamil1983")
                                        End If
                                    Else
                                        ' Format: menu.CPUFreq.240=240MHz (older style)
                                        If Not menuOptions(menuType).ContainsKey(optionKey) Then
                                            menuOptions(menuType)(optionKey) = value
                                            Debug.WriteLine($"[2025-08-16 20:22:36] Board {boardName} direct menu option: {menuType}.{optionKey}={value} by Chamil1983")
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"[2025-08-16 20:22:36] Error parsing parameter: {line}, {ex.Message} by Chamil1983")
                    End Try
                End If
            Next

            ' Add missing menu options with standard defaults when needed
            AddMissingMenuOptions(boardName, boardId, menuOptions, supportedMenus)

            ' Build FQBN with default parameters based on board type
            BuildBoardFQBN(boardId, boardName, parameters, menuOptions, supportedMenus)
        Next

        Debug.WriteLine($"[2025-08-16 20:22:36] Finished parsing boards.txt file, found {boardIdMap.Count} boards by Chamil1983")
    End Sub

    ' Add missing menu options with defaults
    Private Sub AddMissingMenuOptions(boardName As String, boardId As String,
                                    menuOptions As Dictionary(Of String, Dictionary(Of String, String)),
                                    supportedMenus As HashSet(Of String))

        ' Only add defaults for menus that this board actually supports
        If supportedMenus.Contains("CPUFreq") AndAlso (menuOptions.ContainsKey("CPUFreq") AndAlso menuOptions("CPUFreq").Count = 0) Then
            ' CPU Frequency options
            If boardId.Contains("esp32c6") Then
                menuOptions("CPUFreq").Add("160", "160MHz")
                menuOptions("CPUFreq").Add("120", "120MHz")
                menuOptions("CPUFreq").Add("80", "80MHz")
            ElseIf boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c6") Then
                menuOptions("CPUFreq").Add("160", "160MHz")
                menuOptions("CPUFreq").Add("80", "80MHz")
            ElseIf boardId.Contains("esp32h2") Then
                menuOptions.Remove("CPUFreq")
            Else
                menuOptions("CPUFreq").Add("240", "240MHz")
                menuOptions("CPUFreq").Add("160", "160MHz")
                menuOptions("CPUFreq").Add("80", "80MHz")
            End If
        End If

        If supportedMenus.Contains("FlashMode") AndAlso (menuOptions.ContainsKey("FlashMode") AndAlso menuOptions("FlashMode").Count = 0) Then
            ' Flash Mode options
            menuOptions("FlashMode").Add("qio", "QIO")
            menuOptions("FlashMode").Add("dio", "DIO")
        End If

        If supportedMenus.Contains("FlashFreq") AndAlso (menuOptions.ContainsKey("FlashFreq") AndAlso menuOptions("FlashFreq").Count = 0) Then
            ' Flash Frequency options
            'If boardId.Contains("esp32h2") Then
            '    menuOptions("FlashFreq").Add("64", "64MHz")
            '    menuOptions("FlashFreq").Add("32", "32MHz")
            '    menuOptions("FlashFreq").Add("16", "16MHz")


            'Else
            menuOptions("FlashFreq").Add("80", "80MHz")
            menuOptions("FlashFreq").Add("40", "40MHz")
            'End If


        End If

        If supportedMenus.Contains("PartitionScheme") AndAlso (menuOptions.ContainsKey("PartitionScheme") AndAlso menuOptions("PartitionScheme").Count = 0) Then
            ' Partition Scheme options
            menuOptions("PartitionScheme").Add("default", "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)")
            menuOptions("PartitionScheme").Add("min_spiffs", "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)")
            menuOptions("PartitionScheme").Add("minimal", "Minimal (1.3MB APP/700KB SPIFFS)")
            menuOptions("PartitionScheme").Add("huge_app", "Huge APP (3MB No OTA/1MB SPIFFS)")
            menuOptions("PartitionScheme").Add("no_ota", "No OTA (2MB APP/2MB SPIFFS)")
            menuOptions("PartitionScheme").Add("noota_3g", "No OTA (1MB APP/3MB SPIFFS)")
        End If

        If supportedMenus.Contains("UploadSpeed") AndAlso (menuOptions.ContainsKey("UploadSpeed") AndAlso menuOptions("UploadSpeed").Count = 0) Then
            ' Upload Speed options
            menuOptions("UploadSpeed").Add("921600", "921600")
            menuOptions("UploadSpeed").Add("512000", "512000")
            menuOptions("UploadSpeed").Add("460800", "460800")
            menuOptions("UploadSpeed").Add("230400", "230400")
            menuOptions("UploadSpeed").Add("115200", "115200")
        End If

        If supportedMenus.Contains("DebugLevel") AndAlso (menuOptions.ContainsKey("DebugLevel") AndAlso menuOptions("DebugLevel").Count = 0) Then
            ' Debug Level options
            menuOptions("DebugLevel").Add("none", "None")
            menuOptions("DebugLevel").Add("error", "Error")
            menuOptions("DebugLevel").Add("warn", "Warning")
            menuOptions("DebugLevel").Add("info", "Info")
            menuOptions("DebugLevel").Add("debug", "Debug")
            menuOptions("DebugLevel").Add("verbose", "Verbose")
        End If

        If supportedMenus.Contains("PSRAM") AndAlso (menuOptions.ContainsKey("PSRAM") AndAlso menuOptions("PSRAM").Count = 0) Then
            ' PSRAM options
            menuOptions("PSRAM").Add("disabled", "Disabled")
            menuOptions("PSRAM").Add("enabled", "Enabled")
        End If

        If supportedMenus.Contains("EraseFlash") AndAlso (menuOptions.ContainsKey("EraseFlash") AndAlso menuOptions("EraseFlash").Count = 0) Then
            ' EraseFlash options
            menuOptions("EraseFlash").Add("none", "None")
            menuOptions("EraseFlash").Add("all", "All")
        End If

        ' ADD MISSING MENU OPTIONS - JTAGAdapter
        If supportedMenus.Contains("JTAGAdapter") AndAlso (menuOptions.ContainsKey("JTAGAdapter") AndAlso menuOptions("JTAGAdapter").Count = 0) Then
            menuOptions("JTAGAdapter").Add("default", "Disabled")
            menuOptions("JTAGAdapter").Add("external", "FTDI Adapter")
            menuOptions("JTAGAdapter").Add("bridge", "ESP USB Bridge")
        End If

        ' ADD MISSING MENU OPTIONS - LoopCore
        If supportedMenus.Contains("LoopCore") AndAlso (menuOptions.ContainsKey("LoopCore") AndAlso menuOptions("LoopCore").Count = 0) Then
            menuOptions("LoopCore").Add("1", "Core 1")
            menuOptions("LoopCore").Add("0", "Core 0")
        End If

        ' ADD MISSING MENU OPTIONS - EventsCore
        If supportedMenus.Contains("EventsCore") AndAlso (menuOptions.ContainsKey("EventsCore") AndAlso menuOptions("EventsCore").Count = 0) Then
            menuOptions("EventsCore").Add("1", "Core 1")
            menuOptions("EventsCore").Add("0", "Core 0")
        End If

        ' ADD MISSING MENU OPTIONS - ZigbeeMode
        If supportedMenus.Contains("ZigbeeMode") AndAlso (menuOptions.ContainsKey("ZigbeeMode") AndAlso menuOptions("ZigbeeMode").Count = 0) Then
            menuOptions("ZigbeeMode").Add("default", "Disabled")
            menuOptions("ZigbeeMode").Add("zczr", "Zigbee ZCZR (coordinator/router)")
        End If
    End Sub

    ' Check if the board name is in our valid list
    Private Function IsValidBoardName(boardName As String, boardId As String) As Boolean
        ' List of valid board names to filter
        Dim validBoards As New List(Of String) From {
            "ESP32 Dev Module",
            "ESP32 Wrover Module",
            "ESP32 Wrover Kit",
            "ESP32 PICO-D4",
            "ESP32-S2 Dev Module",
            "ESP32-S3 Dev Module",
            "ESP32-C3 Dev Module",
            "ESP32-C6 Dev Module",
            "ESP32-H2 Dev Module",
            "ESP32-C5 Dev Module",
            "ESP32-P4 Dev Module",
            "KC-Link PRO A8 (Default)",
            "KC-Link PRO A8 (Minimal)",
            "KC-Link PRO A8 (OTA)"
        }

        ' Special case for Arduino-labeled boards that match our list
        If boardId.Contains("esp32") Then
            For Each validBoard In validBoards
                If boardName.Contains(validBoard) OrElse validBoard.Contains(boardName) Then
                    Return True
                End If
            Next
        End If

        ' Direct match on valid board names
        Return validBoards.Contains(boardName)
    End Function

    ' Build FQBN for a board with its default parameters
    Private Sub BuildBoardFQBN(boardId As String, boardName As String, parameters As Dictionary(Of String, String),
                              menuOptions As Dictionary(Of String, Dictionary(Of String, String)),
                              supportedMenus As HashSet(Of String))

        ' Default configuration
        Dim vendor = "esp32"
        Dim architecture = "esp32"
        Dim paramList As New Dictionary(Of String, String)()

        ' Extract build.variant to determine the architecture if available
        If parameters.ContainsKey("build.variant") Then
            Dim variantValue As String = parameters("build.variant")
            If variantValue.Contains(":") Then
                Dim parts = variantValue.Split(New Char() {":"c}, 2)
                architecture = parts(0)
            End If
        End If

        ' Extract build.core to determine vendor if available
        If parameters.ContainsKey("build.core") Then
            Dim core = parameters("build.core")
            If core.Contains(":") Then
                Dim parts = core.Split(New Char() {":"c}, 2)
                vendor = parts(0)
            End If
        End If

        ' Process menu options to extract defaults - ONLY for supported menus
        For Each menuType In menuOptions.Keys
            ' Only process if this menu is supported by this board
            If supportedMenus.Contains(menuType) Then
                Select Case menuType
                    Case "PartitionScheme"
                        ' Check for default partition scheme
                        If parameters.ContainsKey("build.partitions") Then
                            paramList("PartitionScheme") = parameters("build.partitions")
                        Else
                            paramList("PartitionScheme") = "default"
                        End If

                    Case "CPUFreq"
                        ' Check for default CPU frequency
                        If parameters.ContainsKey("build.f_cpu") Then
                            Dim cpuFreq = parameters("build.f_cpu").Replace("L", "").Replace("UL", "")
                            If cpuFreq.EndsWith("000000") Then
                                Dim freqMhz = (Long.Parse(cpuFreq) / 1000000).ToString()
                                paramList("CPUFreq") = freqMhz
                            End If
                        Else
                            ' Only add if supported
                            If boardId = "esp32" OrElse
                               boardId.Contains("esp32s") OrElse
                               boardId.Contains("esp32c") OrElse
                               boardId.Contains("esp32h") OrElse
                               boardId.Contains("esp32p") Then
                                paramList("CPUFreq") = "240" ' Default
                            End If
                        End If

                    Case "FlashMode"
                        ' Check for default flash mode
                        If parameters.ContainsKey("build.flash_mode") Then
                            paramList("FlashMode") = parameters("build.flash_mode")
                        Else
                            paramList("FlashMode") = "dio" ' Default
                        End If

                    Case "FlashFreq"
                        ' Check for default flash frequency - only add for ESP32 but not S2/S3/C3 variants
                        If Not boardId.Contains("esp32s3") AndAlso Not boardId.Contains("esp32c3") AndAlso Not boardId.Contains("esp32c5") Then
                            'Not boardId.Contains("esp32c6") AndAlso Not boardId.Contains("esp32h2") AndAlso 

                            If parameters.ContainsKey("build.flash_freq") Then
                                Dim flashFreq = parameters("build.flash_freq")
                                paramList("FlashFreq") = flashFreq
                            Else
                                ' Special handling for esp32wroverkit - use 40MHz default
                                If boardId = "esp32wroverkit" Then
                                    paramList("FlashFreq") = "40" ' Default for Wrover Kit per Main.txt
                                ElseIf boardId = "esp32h2" Then
                                    paramList("FlashFreq") = "64"
                                Else
                                    paramList("FlashFreq") = "80" ' Default
                                End If

                            End If
                        End If

                    Case "UploadSpeed"
                        ' Check for default upload speed
                        If parameters.ContainsKey("upload.speed") Then
                            paramList("UploadSpeed") = parameters("upload.speed")
                        Else
                            paramList("UploadSpeed") = "921600" ' Default
                        End If

                    Case "DebugLevel"
                        ' Default debug level is none
                        paramList("DebugLevel") = "none"

                    Case "PSRAM"
                        ' Check for default PSRAM
                        If parameters.ContainsKey("build.psram_type") OrElse
                           parameters.ContainsKey("build.has_psram") Then
                            ' PSRAM is built-in
                            paramList("PSRAM") = "enabled"
                        Else
                            ' Default PSRAM is disabled
                            paramList("PSRAM") = "disabled"
                        End If

                    Case "EraseFlash"
                        ' Default is none
                        paramList("EraseFlash") = "none"

                    ' ADD MISSING DEFAULT HANDLING - JTAGAdapter
                    Case "JTAGAdapter"
                        paramList("JTAGAdapter") = "default" ' Default is disabled

                    ' ADD MISSING DEFAULT HANDLING - LoopCore
                    Case "LoopCore"
                        paramList("LoopCore") = "1" ' Default is Core 1

                    ' ADD MISSING DEFAULT HANDLING - EventsCore
                    Case "EventsCore"
                        paramList("EventsCore") = "1" ' Default is Core 1

                    ' ADD MISSING DEFAULT HANDLING - ZigbeeMode
                    Case "ZigbeeMode"
                        paramList("ZigbeeMode") = "default" ' Default is disabled

                    Case Else
                        ' For any other menu type, try to find default
                        Dim defaultKey = menuType.ToLower() & ".default"
                        If parameters.ContainsKey(defaultKey) Then
                            paramList(menuType) = parameters(defaultKey)
                        End If
                End Select
            End If
        Next

        ' Only set defaults for supported menus
        If supportedMenus.Contains("PartitionScheme") AndAlso Not paramList.ContainsKey("PartitionScheme") Then
            paramList("PartitionScheme") = "default"
        End If

        If supportedMenus.Contains("CPUFreq") AndAlso Not paramList.ContainsKey("CPUFreq") Then
            ' Only add CPUFreq for boards that support it
            If boardId = "esp32" OrElse
               boardId.Contains("esp32s") OrElse
               boardId.Contains("esp32c") OrElse                'boardId.Contains("esp32h") OrElse
                boardId.Contains("esp32p") Then
                paramList("CPUFreq") = "240"
            End If
        End If

        If supportedMenus.Contains("FlashMode") AndAlso Not paramList.ContainsKey("FlashMode") Then
            paramList("FlashMode") = "dio"
        End If

        ' Add FlashFreq only for original ESP32 not for S2/S3/C3 variants
        If supportedMenus.Contains("FlashFreq") AndAlso Not paramList.ContainsKey("FlashFreq") Then
            If Not boardId.Contains("esp32s3") AndAlso Not boardId.Contains("esp32c3") AndAlso Not boardId.Contains("esp32c5") Then
                'Not boardId.Contains("esp32c6") AndAlso Not boardId.Contains("esp32h2") AndAlso


                ' Special handling for esp32wroverkit - use 40MHz default
                If boardId = "esp32wroverkit" Then
                    paramList("FlashFreq") = "40" ' Default for Wrover Kit per Main.txt
                ElseIf boardId = "esp32h2" Then
                    paramList("FlashFreq") = "64"
                Else
                    paramList("FlashFreq") = "80"
                End If
            End If
        End If

        If supportedMenus.Contains("UploadSpeed") AndAlso Not paramList.ContainsKey("UploadSpeed") Then
            paramList("UploadSpeed") = "921600"
        End If

        If supportedMenus.Contains("DebugLevel") AndAlso Not paramList.ContainsKey("DebugLevel") Then
            paramList("DebugLevel") = "none"
        End If

        If supportedMenus.Contains("PSRAM") AndAlso Not paramList.ContainsKey("PSRAM") Then
            paramList("PSRAM") = "disabled"
        End If

        If supportedMenus.Contains("EraseFlash") AndAlso Not paramList.ContainsKey("EraseFlash") Then
            paramList("EraseFlash") = "none"
        End If

        ' ADD MISSING DEFAULT PARAMETERS
        If supportedMenus.Contains("JTAGAdapter") AndAlso Not paramList.ContainsKey("JTAGAdapter") Then
            paramList("JTAGAdapter") = "default"
        End If

        If supportedMenus.Contains("LoopCore") AndAlso Not paramList.ContainsKey("LoopCore") Then
            paramList("LoopCore") = "1"
        End If

        If supportedMenus.Contains("EventsCore") AndAlso Not paramList.ContainsKey("EventsCore") Then
            paramList("EventsCore") = "1"
        End If

        If supportedMenus.Contains("ZigbeeMode") AndAlso Not paramList.ContainsKey("ZigbeeMode") Then
            paramList("ZigbeeMode") = "default"
        End If

        ' Add specific parameters for newer boards
        If boardId.Contains("esp32s3") Then
            ' ESP32-S3 specific parameters
            If supportedMenus.Contains("USBMode") Then paramList("USBMode") = "hwcdc"
            If supportedMenus.Contains("CDCOnBoot") Then paramList("CDCOnBoot") = "default"
            If supportedMenus.Contains("MSCOnBoot") Then paramList("MSCOnBoot") = "default"
            If supportedMenus.Contains("DFUOnBoot") Then paramList("DFUOnBoot") = "default"
            If supportedMenus.Contains("UploadMode") Then paramList("UploadMode") = "default"
            If supportedMenus.Contains("FlashSize") Then paramList("FlashSize") = "4M"
            If supportedMenus.Contains("LoopCore") Then paramList("LoopCore") = "1"
            If supportedMenus.Contains("EventsCore") Then paramList("EventsCore") = "1"
            If supportedMenus.Contains("JTAGAdapter") Then paramList("JTAGAdapter") = "default"

        ElseIf boardId.Contains("esp32s2") Then
            ' ESP32-S2 specific parameters
            'If supportedMenus.Contains("USBMode") Then paramList("USBMode") = "hwcdc"
            If supportedMenus.Contains("CDCOnBoot") Then paramList("CDCOnBoot") = "default"
            If supportedMenus.Contains("MSCOnBoot") Then paramList("MSCOnBoot") = "default"
            If supportedMenus.Contains("DFUOnBoot") Then paramList("DFUOnBoot") = "default"
            If supportedMenus.Contains("UploadMode") Then paramList("UploadMode") = "default"
        End If

        ' Build parameter string
        Dim paramStrings As New List(Of String)

        For Each kvp In paramList
            paramStrings.Add($"{kvp.Key}={kvp.Value}")
        Next

        Dim paramStr = String.Join(",", paramStrings)
        Dim fqbn = $"{vendor}:{architecture}:{boardId}"

        If paramStrings.Count > 0 Then
            fqbn += ":" & paramStr
        End If

        ' Add to configurations
        boardConfigurations(boardName) = fqbn

        Debug.WriteLine($"[2025-08-16 20:22:36] Added board: {boardName}, FQBN: {fqbn} by Chamil1983")
        Debug.WriteLine($"[2025-08-16 20:22:36] Supported menus: {String.Join(", ", supportedMenus)} by Chamil1983")
    End Sub

    ' Add default ESP32 board configurations
    Private Sub AddDefaultConfigurations()
        ' KC-Link boards with default configurations
        boardIdMap("KC-Link PRO A8 (Default)") = "esp32"
        boardIdMap("KC-Link PRO A8 (Minimal)") = "esp32"
        boardIdMap("KC-Link PRO A8 (OTA)") = "esp32"

        boardConfigurations("KC-Link PRO A8 (Default)") = "esp32:esp32:esp32:PartitionScheme=default,CPUFreq=240,FlashMode=qio,FlashFreq=80"
        boardConfigurations("KC-Link PRO A8 (Minimal)") = "esp32:esp32:esp32:PartitionScheme=min_spiffs,CPUFreq=240,FlashMode=qio,FlashFreq=80"
        boardConfigurations("KC-Link PRO A8 (OTA)") = "esp32:esp32:esp32:PartitionScheme=minimal,CPUFreq=240,FlashMode=qio,FlashFreq=80"

        ' Initialize menu options dictionaries for KC-Link boards
        boardMenuOptions("KC-Link PRO A8 (Default)") = CreateDefaultMenuOptions()
        boardMenuOptions("KC-Link PRO A8 (Minimal)") = CreateDefaultMenuOptions()
        boardMenuOptions("KC-Link PRO A8 (OTA)") = CreateDefaultMenuOptions()

        ' Initialize parameter dictionaries for KC-Link boards
        boardParameters("KC-Link PRO A8 (Default)") = CreateDefaultBoardParameters()
        boardParameters("KC-Link PRO A8 (Minimal)") = CreateDefaultBoardParameters()
        boardParameters("KC-Link PRO A8 (OTA)") = CreateDefaultBoardParameters()

        ' Initialize supported menus for KC-Link boards
        boardSupportedMenus("KC-Link PRO A8 (Default)") = CreateDefaultSupportedMenus()
        boardSupportedMenus("KC-Link PRO A8 (Minimal)") = CreateDefaultSupportedMenus()
        boardSupportedMenus("KC-Link PRO A8 (OTA)") = CreateDefaultSupportedMenus()

        ' Initialize unsupported menus for KC-Link boards
        boardUnsupportedMenus("KC-Link PRO A8 (Default)") = New HashSet(Of String)()
        boardUnsupportedMenus("KC-Link PRO A8 (Minimal)") = New HashSet(Of String)()
        boardUnsupportedMenus("KC-Link PRO A8 (OTA)") = New HashSet(Of String)()

        ' Initialize fixed parameters for KC-Link boards
        boardFixedParams("KC-Link PRO A8 (Default)") = New Dictionary(Of String, String)()
        boardFixedParams("KC-Link PRO A8 (Minimal)") = New Dictionary(Of String, String)()
        boardFixedParams("KC-Link PRO A8 (OTA)") = New Dictionary(Of String, String)()

        ' Initialize config order for KC-Link boards
        boardConfigOrder("KC-Link PRO A8 (Default)") = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}
        boardConfigOrder("KC-Link PRO A8 (Minimal)") = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}
        boardConfigOrder("KC-Link PRO A8 (OTA)") = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}

        ' Standard ESP32 boards - these will be overridden by boards.txt if available
        boardIdMap("ESP32 Dev Module") = "esp32"
        boardIdMap("ESP32 Wrover Module") = "esp32wrover"
        boardIdMap("ESP32 Wrover Kit") = "esp32wroverkit"  ' MODIFIED: Changed from "esp32wrover" to "esp32wroverkit"
        boardIdMap("ESP32 PICO-D4") = "pico32"
        boardIdMap("ESP32-S2 Dev Module") = "esp32s2"
        boardIdMap("ESP32-S3 Dev Module") = "esp32s3"
        boardIdMap("ESP32-C3 Dev Module") = "esp32c3"
        boardIdMap("ESP32-C6 Dev Module") = "esp32c6"
        boardIdMap("ESP32-H2 Dev Module") = "esp32h2"
        boardIdMap("ESP32-C5 Dev Module") = "esp32c5"
        boardIdMap("ESP32-P4 Dev Module") = "esp32p4"

        ' Standard ESP32 boards - original ESP32
        boardConfigurations("ESP32 Dev Module") = "esp32:esp32:esp32:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80"
        boardConfigurations("ESP32 PICO-D4") = "esp32:esp32:pico32:PartitionScheme=default,UploadSpeed=921600,DebugLevel=none,EraseFlash=none"

        ' Wrover boards - different handling for Module vs Kit
        boardConfigurations("ESP32 Wrover Module") = "esp32:esp32:esp32wrover:PartitionScheme=default,FlashMode=dio,FlashFreq=80"
        boardConfigurations("ESP32 Wrover Kit") = "esp32:esp32:esp32wroverkit:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=40,UploadSpeed=921600,DebugLevel=none,EraseFlash=none" ' MODIFIED: Different config for Wrover Kit

        ' ESP32-S2/S3 and newer boards - no FlashFreq parameter
        boardConfigurations("ESP32-S2 Dev Module") = "esp32:esp32:esp32s2:PartitionScheme=default,CPUFreq=240,FlashMode=dio"
        boardConfigurations("ESP32-S3 Dev Module") = "esp32:esp32:esp32s3:PartitionScheme=default,CPUFreq=240,FlashMode=dio,USBMode=hwcdc"
        boardConfigurations("ESP32-C3 Dev Module") = "esp32:esp32:esp32c3:PartitionScheme=default,CPUFreq=160,FlashMode=qio"
        boardConfigurations("ESP32-C6 Dev Module") = "esp32:esp32:esp32c6:PartitionScheme=default,CPUFreq=160,FlashMode=qio"
        boardConfigurations("ESP32-H2 Dev Module") = "esp32:esp32:esp32h2:PartitionScheme=default,FlashMode=qio,FlashFreq=64"
        boardConfigurations("ESP32-C5 Dev Module") = "esp32:esp32:esp32c5:PartitionScheme=default,CPUFreq=240,FlashMode=dio"
        boardConfigurations("ESP32-P4 Dev Module") = "esp32:esp32:esp32p4:PartitionScheme=default,CPUFreq=360,FlashMode=qio,,FlashFreq=80"

        ' Initialize menu options dictionaries for standard boards
        boardMenuOptions("ESP32 Dev Module") = CreateDefaultMenuOptions()
        boardMenuOptions("ESP32 PICO-D4") = CreatePICOMenuOptions()
        boardMenuOptions("ESP32 Wrover Module") = CreateWroverMenuOptions()
        boardMenuOptions("ESP32 Wrover Kit") = CreateWroverKitMenuOptions()  ' MODIFIED: Use separate function
        boardMenuOptions("ESP32-S2 Dev Module") = CreateS2MenuOptions()
        boardMenuOptions("ESP32-S3 Dev Module") = CreateS3MenuOptions()
        boardMenuOptions("ESP32-C3 Dev Module") = CreateC3MenuOptions()
        boardMenuOptions("ESP32-C6 Dev Module") = CreateC6MenuOptions()
        boardMenuOptions("ESP32-H2 Dev Module") = CreateH2MenuOptions()
        boardMenuOptions("ESP32-C5 Dev Module") = CreateC5MenuOptions()
        boardMenuOptions("ESP32-P4 Dev Module") = CreateP4MenuOptions()

        ' Initialize parameter dictionaries for standard boards
        boardParameters("ESP32 Dev Module") = CreateDefaultBoardParameters()
        boardParameters("ESP32 PICO-D4") = CreatePICOBoardParameters()
        boardParameters("ESP32 Wrover Module") = CreateWroverBoardParameters()
        boardParameters("ESP32 Wrover Kit") = CreateWroverKitBoardParameters()  ' MODIFIED: Use separate function
        boardParameters("ESP32-S2 Dev Module") = CreateS2BoardParameters()
        boardParameters("ESP32-S3 Dev Module") = CreateS3BoardParameters()
        boardParameters("ESP32-C3 Dev Module") = CreateC3BoardParameters()
        boardParameters("ESP32-C6 Dev Module") = CreateC6BoardParameters()
        boardParameters("ESP32-H2 Dev Module") = CreateH2BoardParameters()
        boardParameters("ESP32-C5 Dev Module") = CreateC5BoardParameters()
        boardParameters("ESP32-P4 Dev Module") = CreateP4BoardParameters()

        ' Initialize supported menus for standard boards
        boardSupportedMenus("ESP32 Dev Module") = CreateDefaultSupportedMenus()
        boardSupportedMenus("ESP32 PICO-D4") = CreatePICOSupportedMenus()
        boardSupportedMenus("ESP32 Wrover Module") = CreateWroverSupportedMenus()
        boardSupportedMenus("ESP32 Wrover Kit") = CreateWroverKitSupportedMenus()  ' MODIFIED: Use separate function
        boardSupportedMenus("ESP32-S2 Dev Module") = CreateS2SupportedMenus()
        boardSupportedMenus("ESP32-S3 Dev Module") = CreateS3SupportedMenus()
        boardSupportedMenus("ESP32-C3 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-C6 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-H2 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-C5 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-P4 Dev Module") = CreateS3SupportedMenus()

        ' Initialize unsupported menus
        boardUnsupportedMenus("ESP32 Dev Module") = New HashSet(Of String)()
        boardUnsupportedMenus("ESP32 PICO-D4") = CreatePICOUnsupportedMenus()
        boardUnsupportedMenus("ESP32 Wrover Module") = CreateWroverUnsupportedMenus()
        boardUnsupportedMenus("ESP32 Wrover Kit") = CreateWroverKitUnsupportedMenus()  ' MODIFIED: Use separate function
        boardUnsupportedMenus("ESP32-S2 Dev Module") = CreateS2UnsupportedMenus()
        boardUnsupportedMenus("ESP32-S3 Dev Module") = CreateS3UnsupportedMenus()
        boardUnsupportedMenus("ESP32-C3 Dev Module") = CreateC3UnsupportedMenus()
        boardUnsupportedMenus("ESP32-C6 Dev Module") = CreateC3UnsupportedMenus()
        boardUnsupportedMenus("ESP32-H2 Dev Module") = CreateH2UnsupportedMenus()
        boardUnsupportedMenus("ESP32-C5 Dev Module") = CreateC3UnsupportedMenus()
        boardUnsupportedMenus("ESP32-P4 Dev Module") = CreateP4UnsupportedMenus()

        ' Initialize fixed parameters
        boardFixedParams("ESP32 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32 PICO-D4") = CreatePICOFixedParams()
        boardFixedParams("ESP32 Wrover Module") = CreateWroverFixedParams()
        boardFixedParams("ESP32 Wrover Kit") = CreateWroverKitFixedParams()  ' MODIFIED: Use separate function
        boardFixedParams("ESP32-S2 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-S3 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-C3 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-C6 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-H2 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-C5 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-P4 Dev Module") = New Dictionary(Of String, String)()

        ' Initialize config order for standard boards
        Dim defaultOrder As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "EraseFlash", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}
        Dim picoOrder As New List(Of String) From {"PartitionScheme", "UploadSpeed", "DebugLevel", "EraseFlash"}
        Dim wroverOrder As New List(Of String) From {"PartitionScheme", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "EraseFlash"}
        Dim wroverKitOrder As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "EraseFlash"}  ' MODIFIED: Separate order for Wrover Kit
        Dim s2Order As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "CDCOnBoot", "MSCOnBoot", "DFUOnBoot", "UploadMode", "EraseFlash", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}
        Dim s3Order As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "UploadSpeed", "DebugLevel", "PSRAM", "USBMode", "CDCOnBoot", "MSCOnBoot", "DFUOnBoot", "UploadMode", "FlashSize", "LoopCore", "EventsCore", "EraseFlash", "JTAGAdapter", "ZigbeeMode"}
        Dim c3Order As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "CDCOnBoot", "FlashSize", "EraseFlash", "JTAGAdapter", "ZigbeeMode"}
        Dim h2Order As New List(Of String) From {"PartitionScheme", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "CDCOnBoot", "FlashSize", "EraseFlash", "JTAGAdapter", "ZigbeeMode"}
        Dim cOrder As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "UploadSpeed", "DebugLevel", "EraseFlash", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}
        Dim p4Order As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "USBMode", "CDCOnBoot", "MSCOnBoot", "DFUOnBoot", "UploadMode", "FlashSize", "EraseFlash", "JTAGAdapter"}

        boardConfigOrder("ESP32 Dev Module") = defaultOrder
        boardConfigOrder("ESP32 PICO-D4") = picoOrder
        boardConfigOrder("ESP32 Wrover Module") = wroverOrder
        boardConfigOrder("ESP32 Wrover Kit") = wroverKitOrder  ' MODIFIED: Use separate order
        boardConfigOrder("ESP32-S2 Dev Module") = s2Order
        boardConfigOrder("ESP32-S3 Dev Module") = s3Order
        boardConfigOrder("ESP32-C3 Dev Module") = c3Order
        boardConfigOrder("ESP32-C6 Dev Module") = c3Order
        boardConfigOrder("ESP32-H2 Dev Module") = h2Order
        boardConfigOrder("ESP32-C5 Dev Module") = cOrder
        boardConfigOrder("ESP32-P4 Dev Module") = cOrder
    End Sub

    ' Create default supported menus for ESP32 boards
    Private Function CreateDefaultSupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)
        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("EraseFlash")
        supportedMenus.Add("EventsCore")
        supportedMenus.Add("FlashFreq")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("FlashSize")
        supportedMenus.Add("JTAGAdapter")
        supportedMenus.Add("LoopCore")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("PSRAM")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("ZigbeeMode")
        Return supportedMenus
    End Function

    ' Create unsupported menus for ESP32 Wrover boards
    Private Function CreateWroverUnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        ' Wrover boards don't support CPU frequency configuration (fixed at 240MHz)
        unsupportedMenus.Add("CPUFreq")
        ' Wrover boards have built-in PSRAM, no need for the parameter
        unsupportedMenus.Add("PSRAM")
        Return unsupportedMenus
    End Function

    ' NEW FUNCTION: Create unsupported menus for ESP32 Wrover Kit
    Private Function CreateWroverKitUnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        ' Wrover Kit boards don't support LoopCore configuration
        unsupportedMenus.Add("LoopCore")
        ' Wrover Kit boards have built-in EventsCore, no need for the parameter
        unsupportedMenus.Add("EventsCore")
        unsupportedMenus.Add("JTAGAdapter")
        unsupportedMenus.Add("ZigbeeMode")
        Return unsupportedMenus
    End Function

    ' NEW FUNCTION: Create unsupported menus for ESP32 PICO-D4
    Private Function CreatePICOUnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        ' Wrover Kit boards don't support LoopCore configuration
        unsupportedMenus.Add("CPUFreq")
        unsupportedMenus.Add("LoopCore")
        unsupportedMenus.Add("PSRAM")
        unsupportedMenus.Add("FlashMode")
        unsupportedMenus.Add("FlashFreq")
        unsupportedMenus.Add("FlashSize")
        ' Wrover Kit boards have built-in EventsCore, no need for the parameter
        unsupportedMenus.Add("EventsCore")
        unsupportedMenus.Add("JTAGAdapter")
        unsupportedMenus.Add("ZigbeeMode")
        Return unsupportedMenus
    End Function


    ' Create fixed parameters for ESP32 Wrover boards
    Private Function CreateWroverFixedParams() As Dictionary(Of String, String)
        Dim fixedParams As New Dictionary(Of String, String)()
        fixedParams("CPUFreq") = "240" ' Fixed at 240MHz per Main.txt
        fixedParams("PSRAM") = "enabled" ' PSRAM is always enabled per Main.txt
        Return fixedParams
    End Function

    ' NEW FUNCTION: Create fixed parameters for ESP32 Wrover Kit
    Private Function CreateWroverKitFixedParams() As Dictionary(Of String, String)
        Dim fixedParams As New Dictionary(Of String, String)()
        ' fixedParams("CPUFreq") = "240" ' Fixed at 240MHz per Main.txt
        fixedParams("PSRAM") = "enabled" ' PSRAM is always enabled per Main.txt
        Return fixedParams
    End Function

    ' NEW FUNCTION: Create fixed parameters for ESP32 Wrover Kit
    Private Function CreatePICOFixedParams() As Dictionary(Of String, String)
        Dim fixedParams As New Dictionary(Of String, String)()
        fixedParams("CPUFreq") = "240" ' Fixed at 240MHz per Main.txt
        fixedParams("FlashSize") = "4M"

        Return fixedParams
    End Function

    ' Create supported menus for ESP32 Wrover boards
    Private Function CreateWroverSupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("FlashFreq")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("EraseFlash")
        supportedMenus.Add("JTAGAdapter")
        supportedMenus.Add("LoopCore")
        supportedMenus.Add("EventsCore")
        supportedMenus.Add("ZigbeeMode")
        Return supportedMenus
    End Function

    ' NEW FUNCTION: Create supported menus for ESP32 Wrover Kit
    Private Function CreateWroverKitSupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)
        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("FlashSize")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("FlashFreq")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("PSRAM")
        supportedMenus.Add("EraseFlash")
        Return supportedMenus
    End Function

    ' NEW FUNCTION: Create supported menus for PICO-D4
    Private Function CreatePICOSupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("EraseFlash")
        Return supportedMenus
    End Function

    ' Create unsupported menus for ESP32-S2 boards
    Private Function CreateS2UnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        ' S2 doesn't support USBMode
        unsupportedMenus.Add("USBMode")
        Return unsupportedMenus
    End Function

    ' Create supported menus for ESP32-S2 boards
    Private Function CreateS2SupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)
        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("FlashFreq")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("PSRAM")
        supportedMenus.Add("CDCOnBoot")
        supportedMenus.Add("MSCOnBoot")
        supportedMenus.Add("DFUOnBoot")
        supportedMenus.Add("UploadMode")
        supportedMenus.Add("EraseFlash")
        supportedMenus.Add("JTAGAdapter")
        supportedMenus.Add("ZigbeeMode")
        Return supportedMenus
    End Function

    ' Create unsupported menus for ESP32-S3 boards
    Private Function CreateS3UnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        ' S3 doesn't support FlashFreq
        unsupportedMenus.Add("FlashFreq")
        Return unsupportedMenus
    End Function

    ' Create unsupported menus for ESP32-C3 boards
    Private Function CreateC3UnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        ' S3 doesn't support FlashFreq
        unsupportedMenus.Add("LoopCore")
        unsupportedMenus.Add("EventsCore")
        Return unsupportedMenus
    End Function

    ' Create unsupported menus for ESP32-H2 boards
    Private Function CreateH2UnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        unsupportedMenus.Add("CPUFreq")
        unsupportedMenus.Add("LoopCore")
        unsupportedMenus.Add("EventsCore")
        Return unsupportedMenus
    End Function

    ' Create unsupported menus for ESP32-P4 boards
    Private Function CreateP4UnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)
        unsupportedMenus.Add("ZigbeeMode")
        unsupportedMenus.Add("LoopCore")
        unsupportedMenus.Add("EventsCore")
        Return unsupportedMenus
    End Function

    ' Create supported menus for ESP32-S3 boards
    Private Function CreateS3SupportedMenus() As HashSet(Of String)
        'Dim supportedMenus = CreateS2SupportedMenus() ' S3 has all the S2 options
        Dim supportedMenus As New HashSet(Of String)
        ' Add S3-specific options
        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("PSRAM")
        supportedMenus.Add("CDCOnBoot")
        supportedMenus.Add("MSCOnBoot")
        supportedMenus.Add("DFUOnBoot")
        supportedMenus.Add("UploadMode")
        supportedMenus.Add("EraseFlash")
        supportedMenus.Add("JTAGAdapter")
        supportedMenus.Add("ZigbeeMode")
        supportedMenus.Add("FlashSize")
        supportedMenus.Add("USBMode")
        supportedMenus.Add("LoopCore")
        supportedMenus.Add("EventsCore")

        Return supportedMenus
    End Function


    Private Function CreateCSupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)
        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("FlashFreq")
        supportedMenus.Add("FlashSize")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("EraseFlash")
        supportedMenus.Add("JTAGAdapter")
        supportedMenus.Add("LoopCore")
        supportedMenus.Add("EventsCore")
        supportedMenus.Add("CDCOnBoot")
        supportedMenus.Add("ZigbeeMode")
        Return supportedMenus
    End Function

    ' Create default menu options for ESP32 boards
    Private Function CreateDefaultMenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency options
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("240", "240MHz (WiFi/BT)")
        cpuFreqOptions.Add("160", "160MHz (WiFi/BT)")
        cpuFreqOptions.Add("80", "80MHz (WiFi/BT)")
        cpuFreqOptions.Add("40", "40MHz (40MHz XTAL)")
        cpuFreqOptions.Add("26", "26MHz (26MHz XTAL)")
        cpuFreqOptions.Add("20", "20MHz (40MHz XTAL)")
        cpuFreqOptions.Add("13", "13MHz (26MHz XTAL)")
        cpuFreqOptions.Add("10", "10MHz (40MHz XTAL)")
        menuOptions.Add("CPUFreq", cpuFreqOptions)

        ' Flash Mode options
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz")
        flashFreqOptions.Add("40", "40MHz")
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default")
        partitionOptions.Add("minimal", "Minimal")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS")
        partitionOptions.Add("huge_app", "Huge APP")
        partitionOptions.Add("no_ota", "No OTA")
        partitionOptions.Add("noota_3g", "No OTA (3G)")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)



        ' EraseFlash options
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "None")
        eraseFlashOptions.Add("all", "All")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

        Dim zigbeeModeOptions As New Dictionary(Of String, String)
        zigbeeModeOptions.Add("default", "Disabled")
        zigbeeModeOptions.Add("zczr", "Zigbee ZCZR (coordinator/router)")
        menuOptions.Add("ZigbeeMode", zigbeeModeOptions)

        Return menuOptions
    End Function

    ' Create menu options for ESP32 Wrover boards (Updated per Main.txt)
    Private Function CreateWroverMenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' Flash Mode options - includes QIO and DIO per Main.txt
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options - 80MHz and 40MHz per Main.txt, default 40MHz
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz")
        flashFreqOptions.Add("40", "40MHz") ' Default per Main.txt
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options - extensive list per Main.txt
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("defaultffat", "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)")
        partitionOptions.Add("default_8MB", "8M with spiffs (3MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("minimal", "Minimal (1.3MB APP/700KB SPIFFS)")
        partitionOptions.Add("no_ota", "No OTA (2MB APP/2MB SPIFFS)")
        partitionOptions.Add("noota_3g", "No OTA (1MB APP/3MB SPIFFS)")
        partitionOptions.Add("noota_ffat", "No OTA (2MB APP/2MB FATFS)")
        partitionOptions.Add("noota_3gffat", "No OTA (1MB APP/3MB FATFS)")
        partitionOptions.Add("huge_app", "Huge APP (3MB No OTA/1MB SPIFFS)")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)")
        partitionOptions.Add("fatflash", "16M Flash (2MB APP/12.5MB FATFS)")
        partitionOptions.Add("rainmaker", "RainMaker 4MB")
        partitionOptions.Add("rainmaker_4MB", "RainMaker 4MB No OTA")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options per Main.txt
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("256000", "256000")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)

        ' Erase Flash options per Main.txt
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "Disabled")
        eraseFlashOptions.Add("all", "Enabled")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

        ' Additional options for completeness
        Dim jtagAdapterOptions As New Dictionary(Of String, String)
        jtagAdapterOptions.Add("default", "Disabled")
        jtagAdapterOptions.Add("external", "FTDI Adapter")
        jtagAdapterOptions.Add("bridge", "ESP USB Bridge")
        menuOptions.Add("JTAGAdapter", jtagAdapterOptions)

        Dim loopCoreOptions As New Dictionary(Of String, String)
        loopCoreOptions.Add("1", "Core 1")
        loopCoreOptions.Add("0", "Core 0")
        menuOptions.Add("LoopCore", loopCoreOptions)

        Dim eventsCoreOptions As New Dictionary(Of String, String)
        eventsCoreOptions.Add("1", "Core 1")
        eventsCoreOptions.Add("0", "Core 0")
        menuOptions.Add("EventsCore", eventsCoreOptions)

        Dim zigbeeModeOptions As New Dictionary(Of String, String)
        zigbeeModeOptions.Add("default", "Disabled")
        zigbeeModeOptions.Add("zczr", "Zigbee ZCZR (coordinator/router)")
        menuOptions.Add("ZigbeeMode", zigbeeModeOptions)

        Return menuOptions
    End Function

    ' NEW FUNCTION: Create menu options for ESP32 Wrover Kit (specific to esp32wroverkit)
    Private Function CreateWroverKitMenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency options
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("240", "240MHz (WiFi/BT)")
        cpuFreqOptions.Add("160", "160MHz (WiFi/BT)")
        cpuFreqOptions.Add("80", "80MHz (WiFi/BT)")
        cpuFreqOptions.Add("40", "40MHz (40MHz XTAL)")
        cpuFreqOptions.Add("26", "26MHz (26MHz XTAL)")
        cpuFreqOptions.Add("20", "20MHz (40MHz XTAL)")
        cpuFreqOptions.Add("13", "13MHz (26MHz XTAL)")
        cpuFreqOptions.Add("10", "10MHz (40MHz XTAL)")
        menuOptions.Add("CPUFreq", cpuFreqOptions)

        Dim flashSizeOptions As New Dictionary(Of String, String)
        flashSizeOptions.Add("4M", "4MB")
        flashSizeOptions.Add("8M", "8MB")
        flashSizeOptions.Add("16M", "16MB")
        flashSizeOptions.Add("32M", "32MB")
        menuOptions.Add("FlashSize", flashSizeOptions)

        ' Flash Mode options - includes QIO and DIO per Main.txt
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options - 80MHz and 40MHz per Main.txt, default 40MHz for Wrover Kit
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz")
        flashFreqOptions.Add("40", "40MHz") ' Default for Wrover Kit per Main.txt
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options - extensive list per Main.txt
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("defaultffat", "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)")
        partitionOptions.Add("default_8MB", "8M with spiffs (3MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("default_16MB", "16M with spiffs (6.25MB APP/3.43MB SPIFFS)")
        partitionOptions.Add("minimal", "Minimal (1.3MB APP/700KB SPIFFS)")
        partitionOptions.Add("no_ota", "No OTA (2MB APP/2MB SPIFFS)")
        partitionOptions.Add("noota_3g", "No OTA (1MB APP/3MB SPIFFS)")
        partitionOptions.Add("noota_ffat", "No OTA (2MB APP/2MB FATFS)")
        partitionOptions.Add("noota_3gffat", "No OTA (1MB APP/3MB FATFS)")
        partitionOptions.Add("huge_app", "Huge APP (3MB No OTA/1MB SPIFFS)")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)")
        partitionOptions.Add("fatflash", "16M Flash (2MB APP/12.5MB FATFS)")
        partitionOptions.Add("rainmaker", "RainMaker 4MB")
        partitionOptions.Add("rainmaker_4MB", "RainMaker 4MB No OTA")
        partitionOptions.Add("rainmaker_8MB", "RainMaker 8MB")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options per Main.txt
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("256000", "256000")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)

        ' PSRAM options
        Dim psramOptions As New Dictionary(Of String, String)
        psramOptions.Add("disabled", "Disabled")
        psramOptions.Add("enabled", "Enabled")
        menuOptions.Add("PSRAM", psramOptions)

        ' Erase Flash options per Main.txt
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "Disabled")
        eraseFlashOptions.Add("all", "Enabled")
        menuOptions.Add("EraseFlash", eraseFlashOptions)



        Return menuOptions
    End Function

    ' NEW FUNCTION: Create menu options for ESP32 Wrover Kit (specific to esp32wroverkit)
    Private Function CreatePICOMenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' Partition Scheme options - extensive list per Main.txt
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default")
        partitionOptions.Add("no_ota", "No OTA (Large APP)")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS (Large APPS with OTA)")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options per Main.txt
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("256000", "256000")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)


        ' Erase Flash options per Main.txt
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "Disabled")
        eraseFlashOptions.Add("all", "Enabled")
        menuOptions.Add("EraseFlash", eraseFlashOptions)



        Return menuOptions
    End Function

    ' Create menu options for ESP32-S2 boards
    Private Function CreateS2MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = CreateDefaultMenuOptions()

        ' Add S2-specific options

        Dim cdcOnBootOptions As New Dictionary(Of String, String)
        cdcOnBootOptions.Add("default", "Disabled")
        cdcOnBootOptions.Add("cdc", "Enabled")
        menuOptions.Add("CDCOnBoot", cdcOnBootOptions)

        Dim mscOnBootOptions As New Dictionary(Of String, String)
        mscOnBootOptions.Add("default", "Disabled")
        mscOnBootOptions.Add("msc", "Enabled")
        menuOptions.Add("MSCOnBoot", mscOnBootOptions)

        Dim dfuOnBootOptions As New Dictionary(Of String, String)
        dfuOnBootOptions.Add("default", "Disabled")
        dfuOnBootOptions.Add("enabled", "Enabled")
        menuOptions.Add("DFUOnBoot", dfuOnBootOptions)

        Dim uploadModeOptions As New Dictionary(Of String, String)
        uploadModeOptions.Add("default", "UART0")
        uploadModeOptions.Add("dfu", "Internal USB")
        menuOptions.Add("UploadMode", uploadModeOptions)

        Return menuOptions
    End Function

    ' Create menu options for ESP32-S3 boards
    Private Function CreateS3MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        'Dim menuOptions = CreateDefaultMenuOptions()
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))


        ' CPU Frequency options
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("240", "240MHz (WiFi/BT)")
        cpuFreqOptions.Add("160", "160MHz (WiFi/BT)")
        cpuFreqOptions.Add("80", "80MHz (WiFi/BT)")
        cpuFreqOptions.Add("40", "40MHz (40MHz XTAL)")
        cpuFreqOptions.Add("26", "26MHz (26MHz XTAL)")
        cpuFreqOptions.Add("20", "20MHz (40MHz XTAL)")
        cpuFreqOptions.Add("13", "13MHz (26MHz XTAL)")
        cpuFreqOptions.Add("10", "10MHz (40MHz XTAL)")
        menuOptions.Add("CPUFreq", cpuFreqOptions)

        ' Flash Mode options - includes QIO and DIO per Main.txt
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO 80MHz")
        flashModeOptions.Add("qio120", "QIO 120MHz")
        flashModeOptions.Add("dio", "DIO 80MHz")
        flashModeOptions.Add("opi", "OPI 80MHz")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz")
        flashFreqOptions.Add("40", "40MHz")
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default")
        partitionOptions.Add("minimal", "Minimal")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS")
        partitionOptions.Add("huge_app", "Huge APP")
        partitionOptions.Add("no_ota", "No OTA")
        partitionOptions.Add("noota_3g", "No OTA (3G)")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)

        ' EraseFlash options
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "None")
        eraseFlashOptions.Add("all", "All")
        menuOptions.Add("EraseFlash", eraseFlashOptions)


        Dim cdcOnBootOptions As New Dictionary(Of String, String)
        cdcOnBootOptions.Add("default", "Disabled")
        cdcOnBootOptions.Add("cdc", "Enabled")
        menuOptions.Add("CDCOnBoot", cdcOnBootOptions)

        Dim mscOnBootOptions As New Dictionary(Of String, String)
        mscOnBootOptions.Add("default", "Disabled")
        mscOnBootOptions.Add("msc", "Enabled")
        menuOptions.Add("MSCOnBoot", mscOnBootOptions)

        Dim dfuOnBootOptions As New Dictionary(Of String, String)
        dfuOnBootOptions.Add("default", "Disabled")
        dfuOnBootOptions.Add("enabled", "Enabled")
        menuOptions.Add("DFUOnBoot", dfuOnBootOptions)

        Dim uploadModeOptions As New Dictionary(Of String, String)
        uploadModeOptions.Add("default", "UART0 / Hardware CDC")
        uploadModeOptions.Add("cdc", "USB-OTG CDC (TinyUSB)")
        menuOptions.Add("UploadMode", uploadModeOptions)

        ' Add S3-specific options
        Dim flashSizeOptions As New Dictionary(Of String, String)
        flashSizeOptions.Add("4M", "4MB")
        flashSizeOptions.Add("8M", "8MB")
        flashSizeOptions.Add("16M", "16MB")
        flashSizeOptions.Add("32M", "32MB")
        menuOptions.Add("FlashSize", flashSizeOptions)

        Dim loopCoreOptions As New Dictionary(Of String, String)
        loopCoreOptions.Add("1", "Core 1")
        loopCoreOptions.Add("0", "Core 0")
        menuOptions.Add("LoopCore", loopCoreOptions)


        Dim eventsCoreOptions As New Dictionary(Of String, String)
        eventsCoreOptions.Add("1", "Core 1")
        eventsCoreOptions.Add("0", "Core 0")
        menuOptions.Add("EventsCore", eventsCoreOptions)

        ' JTAG options
        Dim jtagAdapterOptions As New Dictionary(Of String, String)
        jtagAdapterOptions.Add("default", "Disabled")
        jtagAdapterOptions.Add("builtin", "Integrated USB JTAG")
        jtagAdapterOptions.Add("external", "FTDI Adapter")
        jtagAdapterOptions.Add("bridge", "ESP USB Bridge")
        menuOptions.Add("JTAGAdapter", jtagAdapterOptions)

        ' PSRAM options
        Dim psramOptions As New Dictionary(Of String, String)
        psramOptions.Add("disabled", "Disabled")
        psramOptions.Add("enabled", "QSPI PSRAM")
        psramOptions.Add("opi", "OPI PSRAM")
        menuOptions.Add("PSRAM", psramOptions)


        ' USB options
        Dim usbModeOptions As New Dictionary(Of String, String)
        usbModeOptions.Add("hwcdc", "Hardware CDC and JTAG")
        usbModeOptions.Add("default", "USB-OTG (TinyUSB)")
        menuOptions.Add("USBMode", usbModeOptions)

        Dim zigbeeModeOptions As New Dictionary(Of String, String)
        zigbeeModeOptions.Add("default", "Disabled")
        zigbeeModeOptions.Add("zczr", "Zigbee ZCZR (coordinator/router)")
        menuOptions.Add("ZigbeeMode", zigbeeModeOptions)

        Return menuOptions
    End Function



    ' Create menu options for ESP32-C3 boards
    Private Function CreateC3MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency options
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("160", "160MHz (WiFi)")
        cpuFreqOptions.Add("80", "80MHz (WiFi)")
        cpuFreqOptions.Add("40", "40MHz")
        cpuFreqOptions.Add("20", "20MHz")
        cpuFreqOptions.Add("10", "10MHz")
        menuOptions.Add("CPUFreq", cpuFreqOptions)

        Dim flashSizeOptions As New Dictionary(Of String, String)
        flashSizeOptions.Add("4M", "4MB (32Mb)")
        flashSizeOptions.Add("8M", "8MB (64Mb)")
        flashSizeOptions.Add("2M", "2MB (16Mb)")
        flashSizeOptions.Add("16M", "16MB (128Mb")
        menuOptions.Add("FlashSize", flashSizeOptions)

        ' Flash Mode options - includes QIO and DIO per Main.txt
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options - 80MHz and 40MHz per Main.txt, default 40MHz for Wrover Kit
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz")
        flashFreqOptions.Add("40", "40MHz") ' Default for Wrover Kit per Main.txt
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options - extensive list per Main.txt
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("defaultffat", "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)")
        partitionOptions.Add("default_8MB", "8M with spiffs (3MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("minimal", "Minimal (1.3MB APP/700KB SPIFFS)")
        partitionOptions.Add("no_ota", "No OTA (2MB APP/2MB SPIFFS)")
        partitionOptions.Add("noota_3g", "No OTA (1MB APP/3MB SPIFFS)")
        partitionOptions.Add("noota_ffat", "No OTA (2MB APP/2MB FATFS)")
        partitionOptions.Add("noota_3gffat", "No OTA (1MB APP/3MB FATFS)")
        partitionOptions.Add("huge_app", "Huge APP (3MB No OTA/1MB SPIFFS)")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)")
        partitionOptions.Add("fatflash", "16M Flash (2MB APP/12.5MB FATFS)")
        partitionOptions.Add("rainmaker", "RainMaker 4MB")
        partitionOptions.Add("rainmaker_4MB", "RainMaker 4MB No OTA")
        partitionOptions.Add("rainmaker_8MB", "RainMaker 8MB")
        partitionOptions.Add("zigbee_zczr", "Zigbee ZCZR 4MB with spiffs")
        partitionOptions.Add("zigbee_zczr_8MB", "Zigbee ZCZR 8MB with spiffs")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options per Main.txt
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("256000", "256000")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)

        ' CDC options
        Dim cdcOnBootOptions As New Dictionary(Of String, String)
        cdcOnBootOptions.Add("default", "Disabled")
        cdcOnBootOptions.Add("cdc", "Enabled")
        menuOptions.Add("CDCOnBoot", cdcOnBootOptions)

        ' Erase Flash options per Main.txt
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "Disabled")
        eraseFlashOptions.Add("all", "Enabled")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

        ' JTAG options
        Dim jtagAdapterOptions As New Dictionary(Of String, String)
        jtagAdapterOptions.Add("default", "Disabled")
        jtagAdapterOptions.Add("builtin", "Integrated USB JTAG")
        jtagAdapterOptions.Add("external", "FTDI Adapter")
        jtagAdapterOptions.Add("bridge", "ESP USB Bridge")
        menuOptions.Add("JTAGAdapter", jtagAdapterOptions)


        'Zigbee optionns
        Dim zigbeeModeOptions As New Dictionary(Of String, String)
        zigbeeModeOptions.Add("default", "Disabled")
        zigbeeModeOptions.Add("zczr", "Zigbee ZCZR (coordinator/router)")
        menuOptions.Add("ZigbeeMode", zigbeeModeOptions)

        Return menuOptions
    End Function

    ' Create menu options for ESP32-C6 boards
    Private Function CreateC6MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Return CreateC3MenuOptions() ' Similar options to C3
    End Function

    ' Create menu options for ESP32-H2 boards
    Private Function CreateH2MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))


        Dim flashSizeOptions As New Dictionary(Of String, String)
        flashSizeOptions.Add("4M", "4MB (32Mb)")
        flashSizeOptions.Add("8M", "8MB (64Mb)")
        flashSizeOptions.Add("2M", "2MB (16Mb)")
        flashSizeOptions.Add("16M", "16MB (128Mb)")
        menuOptions.Add("FlashSize", flashSizeOptions)

        ' Flash Mode options - includes QIO and DIO per Main.txt
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options - per Main.txt, default 40MHz for Wrover Kit
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("64", "64MHz") ' Default for ESP32H2 per Main.txt
        flashFreqOptions.Add("16", "16MHz")
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options - extensive list per Main.txt
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("defaultffat", "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)")
        partitionOptions.Add("default_8MB", "8M with spiffs (3MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("minimal", "Minimal (1.3MB APP/700KB SPIFFS)")
        partitionOptions.Add("no_ota", "No OTA (2MB APP/2MB SPIFFS)")
        partitionOptions.Add("noota_3g", "No OTA (1MB APP/3MB SPIFFS)")
        partitionOptions.Add("noota_ffat", "No OTA (2MB APP/2MB FATFS)")
        partitionOptions.Add("noota_3gffat", "No OTA (1MB APP/3MB FATFS)")
        partitionOptions.Add("huge_app", "Huge APP (3MB No OTA/1MB SPIFFS)")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)")
        partitionOptions.Add("fatflash", "16M Flash (2MB APP/12.5MB FATFS)")
        partitionOptions.Add("rainmaker", "RainMaker 4MB")
        partitionOptions.Add("rainmaker_4MB", "RainMaker 4MB No OTA")
        partitionOptions.Add("rainmaker_8MB", "RainMaker 8MB")
        partitionOptions.Add("zigbee_zczr", "Zigbee ZCZR 4MB with spiffs")
        partitionOptions.Add("zigbee_zczr_8MB", "Zigbee ZCZR 8MB with spiffs")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options per Main.txt
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("256000", "256000")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)

        ' CDC options
        Dim cdcOnBootOptions As New Dictionary(Of String, String)
        cdcOnBootOptions.Add("default", "Disabled")
        cdcOnBootOptions.Add("cdc", "Enabled")
        menuOptions.Add("CDCOnBoot", cdcOnBootOptions)

        ' Erase Flash options per Main.txt
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "Disabled")
        eraseFlashOptions.Add("all", "Enabled")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

        ' JTAG options
        Dim jtagAdapterOptions As New Dictionary(Of String, String)
        jtagAdapterOptions.Add("default", "Disabled")
        jtagAdapterOptions.Add("builtin", "Integrated USB JTAG")
        jtagAdapterOptions.Add("external", "FTDI Adapter")
        jtagAdapterOptions.Add("bridge", "ESP USB Bridge")
        menuOptions.Add("JTAGAdapter", jtagAdapterOptions)


        'Zigbee optionns
        Dim zigbeeModeOptions As New Dictionary(Of String, String)
        zigbeeModeOptions.Add("default", "Disabled")
        zigbeeModeOptions.Add("ed", "Zigbee ED (end device)")
        zigbeeModeOptions.Add("zczr", "Zigbee ZCZR (coordinator/router)")
        zigbeeModeOptions.Add("ed_debug", "Zigbee ED (end device) - Debug")
        zigbeeModeOptions.Add("zczr_debug", "Zigbee ZCZR (coordinator/router) - Debug")
        menuOptions.Add("ZigbeeMode", zigbeeModeOptions)

        Return menuOptions

    End Function

    ' Create menu options for ESP32-C5 boards
    Private Function CreateC5MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = CreateDefaultMenuOptions()

        ' Remove FlashFreq as it's not compatible with C5
        menuOptions.Remove("FlashFreq")

        Return menuOptions
    End Function

    ' Create menu options for ESP32-P4 boards
    Private Function CreateP4MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency options
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("360", "360MHz")
        cpuFreqOptions.Add("40", "40MHz")
        menuOptions.Add("CPUFreq", cpuFreqOptions)

        Dim flashSizeOptions As New Dictionary(Of String, String)
        flashSizeOptions.Add("4M", "4MB (32Mb)")
        flashSizeOptions.Add("8M", "8MB (64Mb)")
        flashSizeOptions.Add("2M", "2MB (16Mb)")
        flashSizeOptions.Add("16M", "16MB (128Mb)")
        flashSizeOptions.Add("32M", "32MB (256Mb)")
        menuOptions.Add("FlashSize", flashSizeOptions)

        ' Flash Mode options - includes QIO and DIO per Main.txt
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options - per Main.txt, default 40MHz for Wrover Kit
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz") ' Default for ESP32H2 per Main.txt
        flashFreqOptions.Add("40", "40MHz")
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options - extensive list per Main.txt
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("defaultffat", "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)")
        partitionOptions.Add("default_8MB", "8M with spiffs (3MB APP/1.5MB SPIFFS)")
        partitionOptions.Add("minimal", "Minimal (1.3MB APP/700KB SPIFFS)")
        partitionOptions.Add("no_ota", "No OTA (2MB APP/2MB SPIFFS)")
        partitionOptions.Add("noota_3g", "No OTA (1MB APP/3MB SPIFFS)")
        partitionOptions.Add("noota_ffat", "No OTA (2MB APP/2MB FATFS)")
        partitionOptions.Add("noota_3gffat", "No OTA (1MB APP/3MB FATFS)")
        partitionOptions.Add("huge_app", "Huge APP (3MB No OTA/1MB SPIFFS)")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)")
        partitionOptions.Add("fatflash", "16M Flash (2MB APP/12.5MB FATFS)")
        partitionOptions.Add("app3M_fat9M_16MB", "16M Flash (3MB APP/9.9MB FATFS)")
        partitionOptions.Add("app5M_fat24M_32MB", "32M Flash (4.8MB APP/22MB FATFS)")
        partitionOptions.Add("app5M_little24M_32MB", "32M Flash (4.8MB APP/22MB LittleFS)")
        partitionOptions.Add("app13M_data7M_32MB", "32M Flash (13MB APP/6.75MB SPIFFS)")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options per Main.txt
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
        uploadSpeedOptions.Add("512000", "512000")
        uploadSpeedOptions.Add("460800", "460800")
        uploadSpeedOptions.Add("256000", "256000")
        uploadSpeedOptions.Add("230400", "230400")
        uploadSpeedOptions.Add("115200", "115200")
        menuOptions.Add("UploadSpeed", uploadSpeedOptions)

        ' Debug Level options
        Dim debugOptions As New Dictionary(Of String, String)
        debugOptions.Add("none", "None")
        debugOptions.Add("error", "Error")
        debugOptions.Add("warn", "Warning")
        debugOptions.Add("info", "Info")
        debugOptions.Add("debug", "Debug")
        debugOptions.Add("verbose", "Verbose")
        menuOptions.Add("DebugLevel", debugOptions)

        ' PSRAM options
        Dim psramOptions As New Dictionary(Of String, String)
        psramOptions.Add("disabled", "Disabled")
        psramOptions.Add("enabled", "Enabled")
        menuOptions.Add("PSRAM", psramOptions)

        ' CDC options
        Dim cdcOnBootOptions As New Dictionary(Of String, String)
        cdcOnBootOptions.Add("default", "Disabled")
        cdcOnBootOptions.Add("cdc", "Enabled")
        menuOptions.Add("CDCOnBoot", cdcOnBootOptions)

        ' MSC options
        Dim mscOnBootOptions As New Dictionary(Of String, String)
        mscOnBootOptions.Add("default", "Disabled")
        mscOnBootOptions.Add("msc", "Enabled (Requires USB-OTG Mode)")
        menuOptions.Add("MSCOnBoot", mscOnBootOptions)

        ' DFU options
        Dim dfuOnBootOptions As New Dictionary(Of String, String)
        dfuOnBootOptions.Add("default", "Disabled")
        dfuOnBootOptions.Add("dfu", "Enabled (Requires USB-OTG Mode)")
        menuOptions.Add("DFUOnBoot", dfuOnBootOptions)

        ' USB options
        Dim usbModeOptions As New Dictionary(Of String, String)
        usbModeOptions.Add("hwcdc", "Hardware CDC and JTAG")
        usbModeOptions.Add("default", "USB-OTG (TinyUSB)")
        menuOptions.Add("USBMode", usbModeOptions)

        ' Upload Mode options
        Dim uploadModeOptions As New Dictionary(Of String, String)
        uploadModeOptions.Add("default", "UART0 / Hardware CDC")
        uploadModeOptions.Add("cdc", "USB-OTG CDC (TinyUSB)")
        menuOptions.Add("UploadMode", uploadModeOptions)

        ' Erase Flash options per Main.txt
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "Disabled")
        eraseFlashOptions.Add("all", "Enabled")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

        ' JTAG options
        Dim jtagAdapterOptions As New Dictionary(Of String, String)
        jtagAdapterOptions.Add("default", "Disabled")
        jtagAdapterOptions.Add("builtin", "Integrated USB JTAG")
        jtagAdapterOptions.Add("external", "FTDI Adapter")
        jtagAdapterOptions.Add("bridge", "ESP USB Bridge")
        menuOptions.Add("JTAGAdapter", jtagAdapterOptions)

        Return menuOptions
    End Function

    ' Create default parameters for a board
    Private Function CreateDefaultBoardParameters() As Dictionary(Of String, String)
        Dim parameters As New Dictionary(Of String, String)()

        ' Common menu parameters
        parameters("menu.UploadSpeed") = "Upload Speed"
        parameters("menu.CPUFreq") = "CPU Frequency"
        parameters("menu.FlashFreq") = "Flash Frequency"
        parameters("menu.FlashMode") = "Flash Mode"
        parameters("menu.FlashSize") = "FlashSize"
        parameters("menu.PartitionScheme") = "Partition Scheme"
        parameters("menu.DebugLevel") = "Debug Level"
        parameters("menu.PSRAM") = "PSRAM"
        parameters("menu.LoopCore") = "Loop Core"
        parameters("menu.EventsCore") = "Events Core"
        parameters("menu.EraseFlash") = "Erase Flash"
        parameters("menu.JTAGAdapter") = "JTAG Adapter"
        parameters("menu.ZigbeeMode") = "Zigbee Mode"

        ' Default values
        parameters("menu.UploadSpeed.921600") = "921600"
        parameters("menu.UploadSpeed.512000") = "512000"
        parameters("menu.UploadSpeed.460800") = "460800"
        parameters("menu.UploadSpeed.230400") = "230400"
        parameters("menu.UploadSpeed.115200") = "115200"

        parameters("menu.CPUFreq.240") = "240MHz (WiFi/BT)"
        parameters("menu.CPUFreq.160") = "160MHz (WiFi/BT)"
        parameters("menu.CPUFreq.80") = "80MHz (WiFi/BT)"
        parameters("menu.CPUFreq.40") = "40MHz (40MHz XTAL)"
        parameters("menu.CPUFreq.26") = "26MHz (26MHz XTAL)"
        parameters("menu.CPUFreq.20") = "20MHz (40MHz XTAL)"
        parameters("menu.CPUFreq.13") = "13MHz (26MHz XTAL)"
        parameters("menu.CPUFreq.10") = "10MHz (40MHz XTAL)"

        parameters("menu.FlashFreq.80") = "80MHz"
        parameters("menu.FlashFreq.40") = "40MHz"

        parameters("menu.FlashMode.qio") = "QIO"
        parameters("menu.FlashMode.dio") = "DIO"

        parameters("menu.FlashSize.4M") = "4MB (32Mb)"
        parameters("menu.FlashSize.8M") = "8MB (64Mb)"
        parameters("menu.FlashSize.2M") = "2MB (16Mb)"
        parameters("menu.FlashSize.16M") = "16MB (128Mb)"

        parameters("menu.PartitionScheme.default") = "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)"
        parameters("menu.PartitionScheme.minimal") = "Minimal (1.3MB APP/700KB SPIFFS)"
        parameters("menu.PartitionScheme.min_spiffs") = "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)"
        parameters("menu.PartitionScheme.huge_app") = "Huge APP (3MB No OTA/1MB SPIFFS)"
        parameters("menu.PartitionScheme.no_ota") = "No OTA (2MB APP/2MB SPIFFS)"
        parameters("menu.PartitionScheme.noota_3g") = "No OTA (1MB APP/3MB SPIFFS)"
        parameters("menu.PartitionScheme.custom") = "Custom"

        parameters("menu.DebugLevel.none") = "None"
        parameters("menu.DebugLevel.error") = "Error"
        parameters("menu.DebugLevel.warn") = "Warning"
        parameters("menu.DebugLevel.info") = "Info"
        parameters("menu.DebugLevel.debug") = "Debug"
        parameters("menu.DebugLevel.verbose") = "Verbose"

        parameters("menu.PSRAM.disabled") = "Disabled"
        parameters("menu.PSRAM.enabled") = "Enabled"

        parameters("menu.LoopCore.1") = "Core 1"
        parameters("menu.LoopCore.0") = "Core 0"

        parameters("menu.EventsCore.1") = "Core 1"
        parameters("menu.EventsCore.0") = "Core 0"

        parameters("menu.EraseFlash.none") = "None"
        parameters("menu.EraseFlash.all") = "All"

        ' ADD MISSING DEFAULT BOARD PARAMETERS - JTAGAdapter
        parameters("menu.JTAGAdapter.default") = "Disabled"
        parameters("menu.JTAGAdapter.external") = "FTDI Adapter"
        parameters("menu.JTAGAdapter.bridge") = "ESP USB Bridge"

        ' ADD MISSING DEFAULT BOARD PARAMETERS - ZigbeeMode
        parameters("menu.ZigbeeMode.default") = "Disabled"
        parameters("menu.ZigbeeMode.zczr") = "Zigbee ZCZR (coordinator/router)"



        Return parameters
    End Function

    ' Create parameters for ESP32 Wrover boards
    Private Function CreateWroverBoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Add special Wrover information
        parameters("build.board") = "ESP32_DEV"
        parameters("build.variants_dir") = "variants"
        parameters("build.variant") = "esp32"
        parameters("build.has_psram") = "true" ' Wrover has PSRAM built-in
        parameters("build.f_cpu") = "240000000L" ' Wrover fixed at 240MHz

        Return parameters
    End Function

    ' NEW FUNCTION: Create parameters for ESP32 Wrover Kit
    Private Function CreateWroverKitBoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Add special Wrover Kit information
        parameters("build.board") = "ESP32_WROVER_KIT"
        parameters("build.variants_dir") = "variants"
        parameters("build.variant") = "esp32"
        parameters("build.has_psram") = "true" ' Wrover Kit has PSRAM built-in
        parameters("build.f_cpu") = "240000000L" ' Wrover Kit fixed at 240MHz
        parameters("build.flash_size") = "4MB" ' Wrover Kit fixed at 4MB
        parameters("build.flash_freq") = "40m" ' Default flash frequency for Wrover Kit
        parameters("build.flash_mode") = "dio" ' Wrover Kit fixed at dio

        Return parameters
    End Function

    ' NEW FUNCTION: Create parameters for ESP32 Wrover Kit
    Private Function CreatePICOBoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Add special Wrover Kit information
        parameters("build.board") = "ESP32_PICO"
        parameters("build.variants_dir") = "variants"
        parameters("build.variant") = "pico32"
        parameters("build.has_psram") = "false" ' Wrover Kit has PSRAM built-in
        parameters("build.f_cpu") = "240000000L" ' Wrover Kit fixed at 240MHz
        parameters("build.flash_size") = "4MB" ' Wrover Kit fixed at 4MB
        parameters("build.flash_freq") = "40m" ' Default flash frequency for Wrover Kit
        parameters("build.flash_mode") = "dio" ' Wrover Kit fixed at dio

        Return parameters
    End Function

    ' Create parameters for ESP32-S2 boards
    Private Function CreateS2BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()


        ' Add S2-specific parameters

        parameters.Remove("menu.Loop Core")
        parameters.Remove("menu.LoopCore.1")
        parameters.Remove("menu.LoopCore.0")

        parameters.Remove("menu.Events Core")
        parameters.Remove("menu.EventsCore.1")
        parameters.Remove("menu.EventsCore.0")

        parameters("menu.CDCOnBoot") = "CDC On Boot"
        parameters("menu.CDCOnBoot.default") = "Disabled"
        parameters("menu.CDCOnBoot.cdc") = "Enabled"

        parameters("menu.MSCOnBoot") = "MSC On Boot"
        parameters("menu.MSCOnBoot.default") = "Disabled"
        parameters("menu.MSCOnBoot.msc") = "Enabled"

        parameters("menu.DFUOnBoot") = "DFU On Boot"
        parameters("menu.DFUOnBoot.default") = "Disabled"
        parameters("menu.DFUOnBoot.dfu") = "Enabled"

        parameters("menu.UploadMode") = "Upload Mode"
        parameters("menu.UploadMode.default") = "UART0"
        parameters("menu.UploadMode.cdc") = "Internal USB"




        Return parameters
    End Function

    ' Create parameters for ESP32-S3 boards
    Private Function CreateS3BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateS2BoardParameters() ' S3 has all the S2 parameters plus some extras

        parameters("menu.PSRAM") = "PSRAM"
        parameters("menu.PSRAM.disabled") = "Disabled"
        parameters("menu.PSRAM.enabled") = "QSPI PSRAM"
        parameters("menu.PSRAM.opi") = "OPI PSRAM"

        parameters("menu.FlashMode") = "Flash Mode"
        parameters("menu.FlashMode.qio") = "QIO 80MHz"
        parameters("menu.FlashMode.dio") = "DIO 80MHz"
        parameters("menu.FlashMode.qio120") = "QIO 120MHz"
        parameters("menu.FlashMode.opi") = "OPI 80MHz"

        ' Add S3-specific parameters
        parameters("menu.FlashSize") = "Flash Size"
        parameters("menu.FlashSize.4M") = "4MB"
        parameters("menu.FlashSize.8M") = "8MB"
        parameters("menu.FlashSize.16M") = "16MB"
        parameters("menu.FlashSize.32M") = "32MB"

        parameters("menu.LoopCore") = "Arduino Loop Core"
        parameters("menu.LoopCore.1") = "Core 1"
        parameters("menu.LoopCore.0") = "Core 0"

        parameters("menu.EventsCore") = "Events Run On Core"
        parameters("menu.EventsCore.1") = "Core 1"
        parameters("menu.EventsCore.0") = "Core 0"

        parameters("menu.USBMode") = "USB Mode"
        parameters("menu.USBMode.hwcdc") = "Hardware CDC and JTAG"
        parameters("menu.USBMode.default") = "USB-OTG (TinyUSB)"

        parameters("menu.JTAGAdapter") = "JTAG Adapter"
        parameters("menu.JTAGAdapter.default") = "Disabled"
        parameters("menu.JTAGAdapter.builtin") = "Integrated USB JTAG"
        parameters("menu.JTAGAdapter.external") = "FTDI Adapter"
        parameters("menu.JTAGAdapter.bridge") = "ESP USB Bridge"

        parameters("menu.UploadMode.default") = "UART0 / Hardware CDC"

        Return parameters
    End Function


    ' Create parameters for ESP32-C3 boards
    Private Function CreateC3BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Common menu parameters
        parameters("menu.UploadSpeed") = "Upload Speed"
        parameters("menu.CPUFreq") = "CPU Frequency"
        parameters("menu.FlashFreq") = "Flash Frequency"
        parameters("menu.FlashMode") = "Flash Mode"
        parameters("menu.FlashSize") = "FlashSize"
        parameters("menu.PartitionScheme") = "Partition Scheme"
        parameters("menu.DebugLevel") = "Debug Level"
        parameters("menu.EraseFlash") = "Erase Flash"
        parameters("menu.CDCOnBoot") = "CDC On Boot"
        parameters("menu.JTAGAdapter") = "JTAG Adapter"
        parameters("menu.ZigbeeMode") = "Zigbee Mode"

        ' Default values
        parameters("menu.UploadSpeed.921600") = "921600"
        parameters("menu.UploadSpeed.512000") = "512000"
        parameters("menu.UploadSpeed.460800") = "460800"
        parameters("menu.UploadSpeed.230400") = "230400"
        parameters("menu.UploadSpeed.115200") = "115200"


        parameters("menu.CPUFreq.160") = "160MHz (WiFi)"
        parameters("menu.CPUFreq.80") = "80MHz (WiFi)"
        parameters("menu.CPUFreq.40") = "40MHz"
        parameters("menu.CPUFreq.20") = "20MHz"
        parameters("menu.CPUFreq.10") = "10MHz)"

        parameters("menu.FlashFreq.80") = "80MHz"
        parameters("menu.FlashFreq.40") = "40MHz"

        parameters("menu.FlashMode.qio") = "QIO"
        parameters("menu.FlashMode.dio") = "DIO"

        parameters("menu.FlashSize.4M") = "4MB (32Mb)"
        parameters("menu.FlashSize.8M") = "8MB (64Mb)"
        parameters("menu.FlashSize.2M") = "2MB (16Mb)"
        parameters("menu.FlashSize.16M") = "16MB (128Mb)"

        parameters("menu.PartitionScheme.default") = "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)"
        parameters("menu.PartitionScheme.defaultffat") = "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)"
        parameters("menu.PartitionScheme.default_8MB") = "8M with spiffs (3MB APP/1.5MB SPIFFS)"
        parameters("menu.PartitionScheme.minimal") = "Minimal (1.3MB APP/700KB SPIFFS)"
        parameters("menu.PartitionScheme.no_ota") = "No OTA (2MB APP/2MB SPIFFS)"
        parameters("menu.PartitionScheme.noota_3g") = "No OTA (1MB APP/3MB SPIFFS)"
        parameters("menu.PartitionScheme.noota_ffat") = "No OTA (2MB APP/2MB FATFS)"
        parameters("menu.PartitionScheme.noota_3gffat") = "No OTA (1MB APP/3MB FATFS)"
        parameters("menu.PartitionScheme.huge_app") = "Huge APP (3MB No OTA/1MB SPIFFS)"
        parameters("menu.PartitionScheme.min_spiffs") = "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)"
        parameters("menu.PartitionScheme.fatflash") = "16M Flash (2MB APP/12.5MB FATFS)"
        parameters("menu.PartitionScheme.rainmaker") = "RainMaker 4MB"
        parameters("menu.PartitionScheme.rainmaker_4MB") = "RainMaker 4MB No OTA"
        parameters("menu.PartitionScheme.rainmaker_8MB") = "RainMaker 8MB"
        parameters("menu.PartitionScheme.zigbee_zczr") = "Zigbee ZCZR 4MB with spiffs"
        parameters("menu.PartitionScheme.zigbee_zczr_8MB") = "Zigbee ZCZR 8MB with spiffs"
        parameters("menu.PartitionScheme.custom") = "Custom"



        parameters("menu.DebugLevel.none") = "None"
        parameters("menu.DebugLevel.error") = "Error"
        parameters("menu.DebugLevel.warn") = "Warning"
        parameters("menu.DebugLevel.info") = "Info"
        parameters("menu.DebugLevel.debug") = "Debug"
        parameters("menu.DebugLevel.verbose") = "Verbose"

        parameters("menu.EraseFlash.none") = "None"
        parameters("menu.EraseFlash.all") = "All"

        ' ADD MISSING DEFAULT BOARD PARAMETERS - JTAGAdapter
        parameters("menu.JTAGAdapter.default") = "Disabled"
        parameters("menu.JTAGAdapter.builtin") = "Integrated USB JTAG"
        parameters("menu.JTAGAdapter.external") = "FTDI Adapter"
        parameters("menu.JTAGAdapter.bridge") = "ESP USB Bridge"

        parameters("menu.CDCOnBoot.default") = "Disabled"
        parameters("menu.CDCOnBoot.cdc") = "Enabled"

        ' ADD MISSING DEFAULT BOARD PARAMETERS - ZigbeeMode
        parameters("menu.ZigbeeMode.default") = "Disabled"
        parameters("menu.ZigbeeMode.zczr") = "Zigbee ZCZR (coordinator/router)"

        ' Add special esp32c3 information
        parameters("build.board") = "ESP32C3_DEV"
        parameters("build.variants_dir") = "variants"
        parameters("build.variant") = "esp32c3"
        parameters("build.has_psram") = "false" ' esp32c3 has no PSRAM built-in
        parameters("build.f_cpu") = "160000000L" ' esp32c3 fixed at 160MHz
        parameters("build.flash_size") = "4MB" ' esp32c3 fixed at 4MB
        parameters("build.flash_freq") = "80m" ' Default flash frequency for esp32c3
        parameters("build.flash_mode") = "qio" ' esp32c3 fixed at qio


        Return parameters
    End Function

    ' Create parameters for ESP32-C6 boards
    Private Function CreateC6BoardParameters() As Dictionary(Of String, String)
        Return CreateC3BoardParameters() ' Similar parameters to C3
    End Function

    ' Create parameters for ESP32-H2 boards
    Private Function CreateH2BoardParameters() As Dictionary(Of String, String)
        Dim parameters As New Dictionary(Of String, String)()

        ' Common menu parameters
        parameters("menu.UploadSpeed") = "Upload Speed"
        parameters("menu.FlashFreq") = "Flash Frequency"
        parameters("menu.FlashMode") = "Flash Mode"
        parameters("menu.FlashSize") = "FlashSize"
        parameters("menu.PartitionScheme") = "Partition Scheme"
        parameters("menu.DebugLevel") = "Debug Level"
        parameters("menu.EraseFlash") = "Erase Flash"
        parameters("menu.CDCOnBoot") = "CDC On Boot"
        parameters("menu.JTAGAdapter") = "JTAG Adapter"
        parameters("menu.ZigbeeMode") = "Zigbee Mode"

        ' Default values
        parameters("menu.UploadSpeed.921600") = "921600"
        parameters("menu.UploadSpeed.512000") = "512000"
        parameters("menu.UploadSpeed.460800") = "460800"
        parameters("menu.UploadSpeed.230400") = "230400"
        parameters("menu.UploadSpeed.115200") = "115200"


        parameters("menu.FlashFreq.64") = "64MHz"
        parameters("menu.FlashFreq.16") = "16MHz"

        parameters("menu.FlashMode.qio") = "QIO"
        parameters("menu.FlashMode.dio") = "DIO"

        parameters("menu.FlashSize.4M") = "4MB (32Mb)"
        parameters("menu.FlashSize.8M") = "8MB (64Mb)"
        parameters("menu.FlashSize.2M") = "2MB (16Mb)"
        parameters("menu.FlashSize.16M") = "16MB (128Mb)"

        parameters("menu.PartitionScheme.default") = "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)"
        parameters("menu.PartitionScheme.defaultffat") = "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)"
        parameters("menu.PartitionScheme.default_8MB") = "8M with spiffs (3MB APP/1.5MB SPIFFS)"
        parameters("menu.PartitionScheme.minimal") = "Minimal (1.3MB APP/700KB SPIFFS)"
        parameters("menu.PartitionScheme.no_ota") = "No OTA (2MB APP/2MB SPIFFS)"
        parameters("menu.PartitionScheme.noota_3g") = "No OTA (1MB APP/3MB SPIFFS)"
        parameters("menu.PartitionScheme.noota_ffat") = "No OTA (2MB APP/2MB FATFS)"
        parameters("menu.PartitionScheme.noota_3gffat") = "No OTA (1MB APP/3MB FATFS)"
        parameters("menu.PartitionScheme.huge_app") = "Huge APP (3MB No OTA/1MB SPIFFS)"
        parameters("menu.PartitionScheme.min_spiffs") = "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)"
        parameters("menu.PartitionScheme.fatflash") = "16M Flash (2MB APP/12.5MB FATFS)"
        parameters("menu.PartitionScheme.rainmaker") = "RainMaker 4MB"
        parameters("menu.PartitionScheme.rainmaker_4MB") = "RainMaker 4MB No OTA"
        parameters("menu.PartitionScheme.rainmaker_8MB") = "RainMaker 8MB"
        parameters("menu.PartitionScheme.zigbee_zczr") = "Zigbee ZCZR 4MB with spiffs"
        parameters("menu.PartitionScheme.zigbee_zczr_8MB") = "Zigbee ZCZR 8MB with spiffs"
        parameters("menu.PartitionScheme.custom") = "Custom"

        parameters("menu.DebugLevel.none") = "None"
        parameters("menu.DebugLevel.error") = "Error"
        parameters("menu.DebugLevel.warn") = "Warning"
        parameters("menu.DebugLevel.info") = "Info"
        parameters("menu.DebugLevel.debug") = "Debug"
        parameters("menu.DebugLevel.verbose") = "Verbose"

        parameters("menu.EraseFlash.none") = "None"
        parameters("menu.EraseFlash.all") = "All"

        ' ADD MISSING DEFAULT BOARD PARAMETERS - JTAGAdapter
        parameters("menu.JTAGAdapter.default") = "Disabled"
        parameters("menu.JTAGAdapter.builtin") = "Integrated USB JTAG"
        parameters("menu.JTAGAdapter.external") = "FTDI Adapter"
        parameters("menu.JTAGAdapter.bridge") = "ESP USB Bridge"

        parameters("menu.CDCOnBoot.default") = "Disabled"
        parameters("menu.CDCOnBoot.cdc") = "Enabled"

        ' ADD MISSING DEFAULT BOARD PARAMETERS - ZigbeeMode
        parameters("menu.ZigbeeMode.default") = "Disabled"
        parameters("menu.ZigbeeMode.ed") = "Zigbee ED (end device)"
        parameters("menu.ZigbeeMode.zczr") = "Zigbee ZCZR (coordinator/router)"
        parameters("menu.ZigbeeMode.ed_debug") = "Zigbee ED (end device) - Debug"
        parameters("menu.ZigbeeMode.zczr_debug") = "Zigbee ZCZR (coordinator/router) - Debug"


        ' Add special esp32c3 information
        parameters("build.board") = "ESP32H2_DEV"
        parameters("build.variants_dir") = "variants"
        parameters("build.variant") = "esp32h2"
        parameters("build.f_cpu") = "96000000L" ' esp32c3 fixed at 160MHz
        parameters("build.flash_size") = "4MB" ' esp32c3 fixed at 4MB
        parameters("build.flash_freq") = "64m" ' Default flash frequency for esp32c3
        parameters("build.flash_mode") = "qio" ' esp32c3 fixed at qio


        Return parameters
    End Function

    ' Create parameters for ESP32-C5 boards
    Private Function CreateC5BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Remove Flash Frequency as it's not compatible with C5
        parameters.Remove("menu.FlashFreq")
        parameters.Remove("menu.FlashFreq.80")
        parameters.Remove("menu.FlashFreq.40")


        Return parameters
    End Function

    ' Create parameters for ESP32-P4 boards
    Private Function CreateP4BoardParameters() As Dictionary(Of String, String)
        Dim parameters As New Dictionary(Of String, String)()

        ' Common menu parameters
        parameters("menu.UploadSpeed") = "Upload Speed"
        parameters("menu.CPUFreq") = "CPU Frequency"
        parameters("menu.FlashFreq") = "Flash Frequency"
        parameters("menu.FlashMode") = "Flash Mode"
        parameters("menu.FlashSize") = "FlashSize"
        parameters("menu.PartitionScheme") = "Partition Scheme"
        parameters("menu.PSRAM") = "PSRAM"
        parameters("menu.DebugLevel") = "Debug Level"
        parameters("menu.EraseFlash") = "Erase Flash"
        parameters("menu.JTAGAdapter") = "JTAG Adapter"
        parameters("menu.ZigbeeMode") = "Zigbee Mode"

        ' Default values
        parameters("menu.UploadSpeed.921600") = "921600"
        parameters("menu.UploadSpeed.512000") = "512000"
        parameters("menu.UploadSpeed.460800") = "460800"
        parameters("menu.UploadSpeed.230400") = "230400"
        parameters("menu.UploadSpeed.115200") = "115200"


        parameters("menu.CPUFreq.360") = "360MHz"
        parameters("menu.CPUFreq.40") = "40MHz"


        parameters("menu.FlashFreq.80") = "80MHz"
        parameters("menu.FlashFreq.40") = "40MHz"

        parameters("menu.FlashMode.qio") = "QIO"
        parameters("menu.FlashMode.dio") = "DIO"

        parameters("menu.FlashSize.4M") = "4MB (32Mb)"
        parameters("menu.FlashSize.8M") = "8MB (64Mb)"
        parameters("menu.FlashSize.2M") = "2MB (16Mb)"
        parameters("menu.FlashSize.16M") = "16MB (128Mb)"
        parameters("menu.FlashSize.32M") = "32MB (256Mb)"


        parameters("menu.PartitionScheme.default") = "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)"
        parameters("menu.PartitionScheme.defaultffat") = "Default 4MB with ffat (1.2MB APP/1.5MB FATFS)"
        parameters("menu.PartitionScheme.default_8MB") = "8M with spiffs (3MB APP/1.5MB SPIFFS)"
        parameters("menu.PartitionScheme.minimal") = "Minimal (1.3MB APP/700KB SPIFFS)"
        parameters("menu.PartitionScheme.no_ota") = "No OTA (2MB APP/2MB SPIFFS)"
        parameters("menu.PartitionScheme.noota_3g") = "No OTA (1MB APP/3MB SPIFFS)"
        parameters("menu.PartitionScheme.noota_ffat") = "No OTA (2MB APP/2MB FATFS)"
        parameters("menu.PartitionScheme.noota_3gffat") = "No OTA (1MB APP/3MB FATFS)"
        parameters("menu.PartitionScheme.huge_app") = "Huge APP (3MB No OTA/1MB SPIFFS)"
        parameters("menu.PartitionScheme.min_spiffs") = "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)"
        parameters("menu.PartitionScheme.fatflash") = "16M Flash (2MB APP/12.5MB FATFS)"
        parameters("menu.PartitionScheme.app3M_fat9M_16MB") = "16M Flash (3MB APP/9.9MB FATFS)"
        parameters("menu.PartitionScheme.app5M_fat24M_32MB") = "32M Flash (4.8MB APP/22MB FATFS)"
        parameters("menu.PartitionScheme.app5M_little24M_32MB") = "32M Flash (4.8MB APP/22MB LittleFS)"
        parameters("menu.PartitionScheme.app13M_data7M_32MB") = "32M Flash (13MB APP/6.75MB SPIFFS)"
        parameters("menu.PartitionScheme.custom") = "Custom"

        parameters("menu.DebugLevel.none") = "None"
        parameters("menu.DebugLevel.error") = "Error"
        parameters("menu.DebugLevel.warn") = "Warning"
        parameters("menu.DebugLevel.info") = "Info"
        parameters("menu.DebugLevel.debug") = "Debug"
        parameters("menu.DebugLevel.verbose") = "Verbose"

        parameters("menu.EraseFlash.none") = "None"
        parameters("menu.EraseFlash.all") = "All"

        ' ADD MISSING DEFAULT BOARD PARAMETERS - JTAGAdapter
        parameters("menu.JTAGAdapter.default") = "Disabled"
        parameters("menu.JTAGAdapter.builtin") = "Integrated USB JTAG"
        parameters("menu.JTAGAdapter.external") = "FTDI Adapter"
        parameters("menu.JTAGAdapter.bridge") = "ESP USB Bridge"

        parameters("menu.PSRAM.disabled") = "Disabled"
        parameters("menu.PSRAM.enabled") = "Enabled"

        parameters("menu.USBMode") = "USB Mode"
        parameters("menu.USBMode.hwcdc") = "Hardware CDC and JTAG"
        parameters("menu.USBMode.default") = "USB-OTG (TinyUSB)"

        parameters("menu.CDCOnBoot") = "CDC On Boot"
        parameters("menu.CDCOnBoot.default") = "Disabled"
        parameters("menu.CDCOnBoot.cdc") = "Enabled"

        parameters("menu.MSCOnBoot") = "MSC On Boot"
        parameters("menu.MSCOnBoot.default") = "Disabled"
        parameters("menu.MSCOnBoot.msc") = "Enabled (Requires USB-OTG Mode)"

        parameters("menu.DFUOnBoot") = "DFU On Boot"
        parameters("menu.DFUOnBoot.default") = "Disabled"
        parameters("menu.DFUOnBoot.dfu") = "Enabled (Requires USB-OTG Mode)"

        parameters("menu.UploadMode") = "Upload Mode"
        parameters("menu.UploadMode.default") = "UART0 / Hardware CDC"
        parameters("menu.UploadMode.cdc") = "USB-OTG CDC (TinyUSB)"


        ' Add special esp32p4 information
        parameters("build.board") = "ESP32P4_DEV"
        parameters("build.variants_dir") = "variants"
        parameters("build.variant") = "esp32p4"
        parameters("build.f_cpu") = "360000000L" ' esp32p4 fixed at 160MHz
        parameters("build.flash_size") = "4MB" ' esp32p4 fixed at 4MB
        parameters("build.flash_freq") = "80m" ' Default flash frequency for esp32p4
        parameters("build.flash_mode") = "qio" ' esp32p4 fixed at qio


        Return parameters
    End Function

    ' Extract all parameters from FQBN
    Public Function ExtractParametersFromFQBN(fqbn As String) As Dictionary(Of String, String)
        Dim parameters As New Dictionary(Of String, String)()

        Debug.WriteLine($"[2025-08-16 20:22:36] Extracting parameters from FQBN: {fqbn} by Chamil1983")

        ' Parse board ID from FQBN
        Dim boardId As String = "esp32"
        If fqbn.Contains(":") Then
            Dim parts = fqbn.Split(New Char() {":"c})
            If parts.Length >= 3 Then
                boardId = parts(2)
            End If
        End If

        ' Get board name, supported and unsupported menus, and fixed parameters
        Dim boardName As String = String.Empty
        Dim supportedMenus As New HashSet(Of String)()
        Dim unsupportedMenus As New HashSet(Of String)()
        Dim fixedParams As New Dictionary(Of String, String)()

        ' Find the board name from the ID
        For Each kvp In boardIdMap
            If kvp.Value = boardId Then
                boardName = kvp.Key
                If boardSupportedMenus.ContainsKey(boardName) Then
                    supportedMenus = boardSupportedMenus(boardName)
                End If
                If boardUnsupportedMenus.ContainsKey(boardName) Then
                    unsupportedMenus = boardUnsupportedMenus(boardName)
                End If
                If boardFixedParams.ContainsKey(boardName) Then
                    fixedParams = boardFixedParams(boardName)
                End If
                Exit For
            End If
        Next

        Debug.WriteLine($"[2025-08-16 20:22:36] Board ID: {boardId}, Board Name: {boardName} by Chamil1983")

        ' Add fixed parameters first
        For Each kvp In fixedParams
            parameters(kvp.Key) = kvp.Value
            Debug.WriteLine($"[2025-08-16 20:22:36] Adding fixed parameter: {kvp.Key}={kvp.Value} by Chamil1983")
        Next

        ' Only add default values for menus that are supported by this board and not explicitly unsupported
        If supportedMenus.Contains("CPUFreq") AndAlso Not unsupportedMenus.Contains("CPUFreq") Then

            If boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c6") Then
                parameters("CPUFreq") = "160"
            ElseIf boardId.Contains("esp32h2") Then
                parameters.Remove("CPUFreq")
            Else
                parameters("CPUFreq") = "240"
            End If
        End If

        If supportedMenus.Contains("FlashMode") Then parameters("FlashMode") = "dio"
        If supportedMenus.Contains("PartitionScheme") Then parameters("PartitionScheme") = "default"
        If supportedMenus.Contains("UploadSpeed") Then parameters("UploadSpeed") = "921600"
        If supportedMenus.Contains("DebugLevel") Then parameters("DebugLevel") = "none"
        If supportedMenus.Contains("EraseFlash") Then parameters("EraseFlash") = "none"

        ' Only add PSRAM if supported and not explicitly unsupported or fixed
        If supportedMenus.Contains("PSRAM") AndAlso Not unsupportedMenus.Contains("PSRAM") AndAlso Not fixedParams.ContainsKey("PSRAM") Then
            parameters("PSRAM") = "disabled"
        End If

        ' Only set FlashFreq for compatible boards
        If supportedMenus.Contains("FlashFreq") AndAlso Not unsupportedMenus.Contains("FlashFreq") Then
            ' Special handling for esp32wroverkit - use 40MHz default
            If boardId = "esp32wroverkit" Then
                parameters("FlashFreq") = "40" ' Default for Wrover Kit per Main.txt
            ElseIf boardId = "esp32h2" Then
                parameters("FlashFreq") = "64"
            Else
                    parameters("FlashFreq") = "80"
            End If


        End If

        ' ADD DEFAULT PARAMETERS FOR MISSING SETTINGS

        If supportedMenus.Contains("JTAGAdapter") AndAlso Not unsupportedMenus.Contains("JTAGAdapter") AndAlso Not fixedParams.ContainsKey("JTAGAdapter") Then
            parameters("JTAGAdapter") = "default"
        End If

        If supportedMenus.Contains("LoopCore") AndAlso Not unsupportedMenus.Contains("LoopCore") AndAlso Not fixedParams.ContainsKey("LoopCore") Then
            parameters("LoopCore") = "1"
        End If

        If supportedMenus.Contains("EventsCore") AndAlso Not unsupportedMenus.Contains("EventsCore") AndAlso Not fixedParams.ContainsKey("EventsCore") Then
            parameters("EventsCore") = "1"
        End If

        If supportedMenus.Contains("ZigbeeMode") AndAlso Not unsupportedMenus.Contains("ZigbeeMode") AndAlso Not fixedParams.ContainsKey("ZigbeeMode") Then
            parameters("ZigbeeMode") = "default"
        End If

        ' Parse parameters from FQBN
        If fqbn.Contains(":") Then
            Dim parts = fqbn.Split(New Char() {":"c})
            If parts.Length >= 4 Then
                Dim paramPart = parts(3)
                Dim paramPairs = paramPart.Split(New Char() {","c})

                For Each pair In paramPairs
                    If pair.Contains("=") Then
                        Dim keyValue = pair.Split(New Char() {"="c}, 2)
                        If keyValue.Length = 2 Then
                            ' Only add parameters that are supported by this board and not explicitly unsupported or fixed
                            If (String.IsNullOrEmpty(boardName) OrElse
                                (supportedMenus.Contains(keyValue(0)) AndAlso
                                 Not unsupportedMenus.Contains(keyValue(0)) AndAlso
                                 Not fixedParams.ContainsKey(keyValue(0)))) Then
                                parameters(keyValue(0)) = keyValue(1)
                                Debug.WriteLine($"[2025-08-16 20:22:36] Found parameter: {keyValue(0)}={keyValue(1)} by Chamil1983")
                            End If
                        End If
                    End If
                Next
            End If
        End If

        ' Add ESP32-S3 specific parameters if supported
        If boardId.Contains("esp32s3") Then
            If supportedMenus.Contains("USBMode") Then parameters("USBMode") = "hwcdc"
            If supportedMenus.Contains("CDCOnBoot") Then parameters("CDCOnBoot") = "default"
            If supportedMenus.Contains("MSCOnBoot") Then parameters("MSCOnBoot") = "default"
            If supportedMenus.Contains("DFUOnBoot") Then parameters("DFUOnBoot") = "default"
            If supportedMenus.Contains("UploadMode") Then parameters("UploadMode") = "default"
            If supportedMenus.Contains("FlashSize") Then parameters("FlashSize") = "4M"
            If supportedMenus.Contains("LoopCore") Then parameters("LoopCore") = "1"
            If supportedMenus.Contains("EventsCore") Then parameters("EventsCore") = "1"
            If supportedMenus.Contains("JTAGAdapter") Then parameters("JTAGAdapter") = "default"
        ElseIf boardId.Contains("esp32s2") Then
            'If supportedMenus.Contains("USBMode") Then parameters("USBMode") = "hwcdc"
            If supportedMenus.Contains("CDCOnBoot") Then parameters("CDCOnBoot") = "default"
            If supportedMenus.Contains("MSCOnBoot") Then parameters("MSCOnBoot") = "default"
            If supportedMenus.Contains("DFUOnBoot") Then parameters("DFUOnBoot") = "default"
            If supportedMenus.Contains("UploadMode") Then parameters("UploadMode") = "default"
        End If

        Debug.WriteLine($"[2025-08-16 20:22:36] Extracted {parameters.Count} parameters from FQBN by Chamil1983")
        Return parameters
    End Function

    ' Get all available configuration options for a board with user-friendly values
    Public Function GetAllBoardConfigOptions(boardName As String) As Dictionary(Of String, List(Of KeyValuePair(Of String, String)))
        Dim allOptions As New Dictionary(Of String, List(Of KeyValuePair(Of String, String)))()

        Debug.WriteLine($"[2025-08-15 00:00:50] Getting board config options for {boardName} by Chamil1983")

        ' Get supported menus, unsupported menus, and fixed parameters for this board
        Dim supportedMenus As New HashSet(Of String)()
        Dim unsupportedMenus As New HashSet(Of String)()
        Dim fixedParams As New Dictionary(Of String, String)()

        If boardSupportedMenus.ContainsKey(boardName) Then
            supportedMenus = boardSupportedMenus(boardName)
            Debug.WriteLine($"[2025-08-15 00:00:50] Found {supportedMenus.Count} supported menus for {boardName} by Chamil1983")
        End If

        If boardUnsupportedMenus.ContainsKey(boardName) Then
            unsupportedMenus = boardUnsupportedMenus(boardName)
            Debug.WriteLine($"[2025-08-15 00:00:50] Found {unsupportedMenus.Count} unsupported menus for {boardName} by Chamil1983")
        End If

        If boardFixedParams.ContainsKey(boardName) Then
            fixedParams = boardFixedParams(boardName)
            Debug.WriteLine($"[2025-08-15 00:00:50] Found {fixedParams.Count} fixed parameters for {boardName} by Chamil1983")
        End If

        ' Get menu options for the board
        If boardMenuOptions.ContainsKey(boardName) Then
            ' Use config order if available
            Dim configOrder As List(Of String)
            If boardConfigOrder.ContainsKey(boardName) Then
                configOrder = boardConfigOrder(boardName)
            Else
                ' Default order
                configOrder = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "EraseFlash", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}

                ' Add all keys from menu options
                For Each menuType In boardMenuOptions(boardName).Keys
                    If Not configOrder.Contains(menuType) Then
                        configOrder.Add(menuType)
                    End If
                Next
            End If

            ' Process menu options in the correct order
            For Each category In configOrder
                ' Skip if this category is not supported by this board, is explicitly unsupported, or is a fixed parameter
                If Not supportedMenus.Contains(category) OrElse unsupportedMenus.Contains(category) OrElse fixedParams.ContainsKey(category) Then
                    Debug.WriteLine($"[2025-08-15 00:00:50] Skipping category {category} for {boardName}: " &
                                  $"supported={supportedMenus.Contains(category)}, " &
                                  $"unsupported={unsupportedMenus.Contains(category)}, " &
                                  $"fixed={fixedParams.ContainsKey(category)} by Chamil1983")
                    Continue For
                End If

                If boardMenuOptions(boardName).ContainsKey(category) Then
                    Dim options As New List(Of KeyValuePair(Of String, String))
                    Dim uniqueKeys As New HashSet(Of String)
                    Dim uniqueValues As New HashSet(Of String)

                    ' Convert dictionary to sorted list of KeyValuePairs
                    For Each kvp As KeyValuePair(Of String, String) In boardMenuOptions(boardName)(category)
                        ' Only add if both key and value are unique
                        If Not uniqueKeys.Contains(kvp.Key) AndAlso Not uniqueValues.Contains(kvp.Value) Then
                            options.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
                            uniqueKeys.Add(kvp.Key)
                            uniqueValues.Add(kvp.Value)
                        End If
                    Next

                    ' Add to result dictionary
                    If options.Count > 0 Then
                        allOptions(category) = options
                        Debug.WriteLine($"[2025-08-15 00:00:50] Added {options.Count} options for category {category} by Chamil1983")
                    End If
                End If
            Next
        End If

        ' If options are still missing, add defaults only for supported menus
        If boardConfigOrder.ContainsKey(boardName) Then
            For Each category In boardConfigOrder(boardName)
                ' Skip if already added, not supported, explicitly unsupported, or fixed
                If allOptions.ContainsKey(category) OrElse
                   Not supportedMenus.Contains(category) OrElse
                   unsupportedMenus.Contains(category) OrElse
                   fixedParams.ContainsKey(category) Then
                    Continue For
                End If

                Dim defaultOptions = GetParameterOptions(boardName, category)
                Dim options As New List(Of KeyValuePair(Of String, String))
                Dim uniqueKeys As New HashSet(Of String)
                Dim uniqueValues As New HashSet(Of String)

                ' Convert dictionary to sorted list of KeyValuePairs
                For Each kvp As KeyValuePair(Of String, String) In defaultOptions
                    If Not uniqueKeys.Contains(kvp.Key) AndAlso Not uniqueValues.Contains(kvp.Value) Then
                        options.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
                        uniqueKeys.Add(kvp.Key)
                        uniqueValues.Add(kvp.Value)
                    End If
                Next

                ' Add to result dictionary if we have options
                If options.Count > 0 Then
                    allOptions(category) = options
                    Debug.WriteLine($"[2025-08-15 00:00:50] Added {options.Count} default options for category {category} by Chamil1983")
                End If
            Next
        End If

        Debug.WriteLine($"[2025-08-15 00:00:50] Returning {allOptions.Count} configuration categories for {boardName} by Chamil1983")
        For Each category In allOptions.Keys
            Debug.WriteLine($"[2025-08-15 00:00:50] Category {category} has {allOptions(category).Count} options by Chamil1983")
        Next

        Return allOptions
    End Function

    ' Helper method to get parameter options
    Private Function GetParameterOptions(boardName As String, parameterName As String) As Dictionary(Of String, String)
        Dim options As New Dictionary(Of String, String)()

        ' Get board ID for board-specific options
        Dim boardId As String = GetBoardId(boardName)

        Select Case parameterName
            Case "CPUFreq"
                If boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c6") Then
                        options.Add("160", "160MHz")
                        options.Add("80", "80MHz")
                    options.Add("40", "40MHz")
                Else
                    options.Add("240", "240MHz (WiFi/BT)")
                    options.Add("160", "160MHz (WiFi/BT)")
                    options.Add("80", "80MHz (WiFi/BT)")
                    options.Add("40", "40MHz (40MHz XTAL)")
                End If

            Case "FlashMode"
                options.Add("qio", "QIO")
                options.Add("dio", "DIO")

            Case "FlashFreq"
                If boardId.Contains("esp32h2") Then
                    options.Add("64", "64MHz")
                    options.Add("32", "32MHz")
                    options.Add("16", "16MHz")
                Else
                    options.Add("80", "80MHz")
                    options.Add("40", "40MHz")

                End If




            Case "PartitionScheme"
                options.Add("default", "Default 4MB with spiffs (1.2MB APP/1.5MB SPIFFS)")
                options.Add("min_spiffs", "Minimal SPIFFS (1.9MB APP with OTA/190KB SPIFFS)")
                options.Add("minimal", "Minimal (1.3MB APP/700KB SPIFFS)")
                options.Add("huge_app", "Huge APP (3MB No OTA/1MB SPIFFS)")
                options.Add("no_ota", "No OTA (2MB APP/2MB SPIFFS)")
                options.Add("noota_3g", "No OTA (1MB APP/3MB SPIFFS)")
                options.Add("custom", "Custom")

            Case "UploadSpeed"
                options.Add("921600", "921600")
                options.Add("512000", "512000")
                options.Add("460800", "460800")
                options.Add("230400", "230400")
                options.Add("115200", "115200")

            Case "DebugLevel"
                options.Add("none", "None")
                options.Add("error", "Error")
                options.Add("warn", "Warning")
                options.Add("info", "Info")
                options.Add("debug", "Debug")
                options.Add("verbose", "Verbose")

            Case "PSRAM"
                options.Add("disabled", "Disabled")
                options.Add("enabled", "Enabled")

            Case "EraseFlash"
                options.Add("none", "None")
                options.Add("all", "All")

            Case "JTAGAdapter"
                options.Add("default", "Disabled")
                options.Add("external", "FTDI Adapter")
                options.Add("bridge", "ESP USB Bridge")

            Case "LoopCore"
                options.Add("1", "Core 1")
                options.Add("0", "Core 0")

            Case "EventsCore"
                options.Add("1", "Core 1")
                options.Add("0", "Core 0")

            Case "ZigbeeMode"
                options.Add("default", "Disabled")
                options.Add("zczr", "Zigbee ZCZR (coordinator/router)")

            Case "USBMode"
                options.Add("hwcdc", "Hardware CDC")
                options.Add("default", "Default")

            Case "CDCOnBoot", "MSCOnBoot", "DFUOnBoot"
                options.Add("default", "Default")
                options.Add("enabled", "Enabled")
                options.Add("disabled", "Disabled")

            Case "UploadMode"
                options.Add("default", "Default")
                options.Add("usb", "USB")
                options.Add("uart", "UART")

            Case "FlashSize"
                options.Add("4M", "4MB")
                options.Add("8M", "8MB")
                options.Add("16M", "16MB")
                options.Add("32M", "32MB")

        End Select

        Return options
    End Function

    ' Get board names
    Public Function GetBoardNames() As List(Of String)
        Return boardIdMap.Keys.ToList()
    End Function

    ' Get board ID from board name
    Public Function GetBoardId(boardName As String) As String
        If boardIdMap.ContainsKey(boardName) Then
            Return boardIdMap(boardName)
        End If
        Return "esp32" ' Default fallback
    End Function

    ' Get FQBN for a board
    Public Function GetFQBN(boardName As String) As String
        If boardConfigurations.ContainsKey(boardName) Then
            Return boardConfigurations(boardName)
        End If
        Return "esp32:esp32:esp32" ' Default fallback
    End Function

    ' Get supported menus for a board
    Public Function GetSupportedMenus(boardName As String) As HashSet(Of String)
        If boardSupportedMenus.ContainsKey(boardName) Then
            Return boardSupportedMenus(boardName)
        End If
        Return New HashSet(Of String)()
    End Function

    ' Get unsupported menus for a board
    Public Function GetUnsupportedMenus(boardName As String) As HashSet(Of String)
        If boardUnsupportedMenus.ContainsKey(boardName) Then
            Return boardUnsupportedMenus(boardName)
        End If
        Return New HashSet(Of String)()
    End Function

    ' Get fixed parameters for a board
    Public Function GetFixedParameters(boardName As String) As Dictionary(Of String, String)
        If boardFixedParams.ContainsKey(boardName) Then
            Return boardFixedParams(boardName)
        Else
            ' Default empty dictionary
            Return New Dictionary(Of String, String)()
        End If
    End Function

    ' Get configuration order for a board
    Public Function GetConfigOrder(boardName As String) As List(Of String)
        ' Return the parameter ordering for a board
        If boardConfigOrder.ContainsKey(boardName) Then
            Return boardConfigOrder(boardName)
        Else
            ' Default order with all missing settings included
            Return New List(Of String) From {"UploadSpeed", "CPUFreq", "FlashFreq", "FlashMode", "FlashSize", "PartitionScheme", "DebugLevel", "PSRAM", "EraseFlash", "JTAGAdapter", "LoopCore", "EventsCore", "ZigbeeMode"}
        End If
    End Function

    ' Update board configuration
    Public Sub UpdateBoardConfiguration(boardName As String, parameters As Dictionary(Of String, String))
        If boardConfigurations.ContainsKey(boardName) Then
            Dim fqbn = boardConfigurations(boardName)
            Dim boardId = GetBoardId(boardName)

            ' Get supported and unsupported menus, and fixed parameters for this board
            Dim supportedMenus = GetSupportedMenus(boardName)
            Dim unsupportedMenus = GetUnsupportedMenus(boardName)
            Dim fixedParams = GetFixedParameters(boardName)

            ' Parse the FQBN into parts
            Dim parts = fqbn.Split(New Char() {":"c})

            ' Check if we have enough parts for a valid FQBN
            If parts.Length >= 3 Then
                ' Extract vendor, architecture, board ID
                Dim vendor = parts(0)
                Dim architecture = parts(1)

                ' Build parameter string
                Dim paramList As New List(Of String)

                ' First add all fixed parameters
                For Each kvp In fixedParams
                    paramList.Add($"{kvp.Key}={kvp.Value}")
                Next

                ' Add partition scheme first if available and supported
                If parameters.ContainsKey("PartitionScheme") AndAlso
                   supportedMenus.Contains("PartitionScheme") AndAlso
                   Not fixedParams.ContainsKey("PartitionScheme") Then
                    paramList.Add($"PartitionScheme={parameters("PartitionScheme")}")
                End If

                ' Add remaining parameters
                For Each kvp In parameters
                    ' Skip if not supported by this board, is explicitly unsupported, or is a fixed parameter
                    If Not supportedMenus.Contains(kvp.Key) OrElse
                       unsupportedMenus.Contains(kvp.Key) OrElse
                       fixedParams.ContainsKey(kvp.Key) Then
                        Continue For
                    End If

                    ' Skip partition scheme as it's already added
                    If kvp.Key = "PartitionScheme" Then
                        Continue For
                    End If

                    ' Skip empty parameters
                    If String.IsNullOrEmpty(kvp.Value) Then
                        Continue For
                    End If

                    ' Skip default values for DebugLevel and PSRAM
                    If (kvp.Key = "DebugLevel" AndAlso kvp.Value = "none") Then
                        Continue For
                    End If

                    If (kvp.Key = "PSRAM" AndAlso kvp.Value = "disabled" AndAlso Not boardId.Contains("wrover")) Then
                        Continue For
                    End If

                    paramList.Add($"{kvp.Key}={kvp.Value}")
                Next

                Dim paramStr = String.Join(",", paramList)

                ' Create new FQBN with updated parameters
                Dim newFqbn = $"{vendor}:{architecture}:{boardId}"
                If Not String.IsNullOrEmpty(paramStr) Then
                    newFqbn &= ":" & paramStr
                End If

                ' Update the configuration
                boardConfigurations(boardName) = newFqbn

                ' Log the change
                Debug.WriteLine($"[2025-08-15 00:00:50] Updated board configuration for {boardName}: {newFqbn} by Chamil1983")
            End If
        Else
            ' Add new board configuration (implementation as in the original code)
            Debug.WriteLine($"[2025-08-15 00:00:50] Adding new board configuration for {boardName} by Chamil1983")
        End If
    End Sub

    ' Apply partition scheme to FQBN
    Public Function ApplyPartitionScheme(fqbn As String, partitionScheme As String) As String
        If String.IsNullOrEmpty(fqbn) OrElse String.IsNullOrEmpty(partitionScheme) Then
            Return fqbn
        End If

        ' Parse FQBN
        Dim parts = fqbn.Split(New Char() {":"c})
        If parts.Length < 3 Then
            Return fqbn
        End If

        Dim vendor = parts(0)
        Dim architecture = parts(1)
        Dim boardId = parts(2)

        ' Extract existing parameters
        Dim existingParams As New Dictionary(Of String, String)()
        If parts.Length >= 4 Then
            Dim paramPairs = parts(3).Split(New Char() {","c})
            For Each pair In paramPairs
                If pair.Contains("=") Then
                    Dim keyValue = pair.Split(New Char() {"="c}, 2)
                    If keyValue.Length = 2 Then
                        existingParams(keyValue(0)) = keyValue(1)
                    End If
                End If
            Next
        End If

        ' Update partition scheme
        existingParams("PartitionScheme") = partitionScheme

        ' Build new parameter string
        Dim paramList As New List(Of String)()
        For Each kvp In existingParams
            paramList.Add($"{kvp.Key}={kvp.Value}")
        Next

        ' Build new FQBN
        Dim newFqbn = $"{vendor}:{architecture}:{boardId}"
        If paramList.Count > 0 Then
            newFqbn += ":" + String.Join(",", paramList)
        End If

        Return newFqbn
    End Function

    ' Apply custom partition file
    Public Function ApplyCustomPartitionFile(fqbn As String) As String
        ' Implementation for custom partition file handling
        If Not String.IsNullOrEmpty(customPartitionFile) Then
            ' Apply custom partition file logic here
            Return ApplyPartitionScheme(fqbn, "custom")
        End If
        Return fqbn
    End Function

    ' Get custom partitions
    Public Function GetCustomPartitions() As List(Of String)
        Dim customPartitions As New List(Of String)()
        ' Add any custom partition schemes found
        Return customPartitions
    End Function

    ' Get default partition for board
    Public Function GetDefaultPartitionForBoard(boardName As String) As String
        ' Return the default partition scheme for a specific board
        If boardName.Contains("Minimal") Then
            Return "min_spiffs"
        ElseIf boardName.Contains("OTA") Then
            Return "minimal"
        Else
            Return "default"
        End If
    End Function

    ' Set custom partition file
    Public Sub SetCustomPartitionFile(filePath As String)
        customPartitionFile = filePath
        Debug.WriteLine($"[2025-08-15 00:00:50] Custom partition file set: {filePath} by Chamil1983")
    End Sub

End Class

' Version comparer class for sorting version directories
Public Class VersionComparer
    Implements IComparer(Of String)

    Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare
        Try
            ' Extract version numbers from directory names
            Dim xVersion = ExtractVersionNumber(Path.GetFileName(x))
            Dim yVersion = ExtractVersionNumber(Path.GetFileName(y))

            ' Compare version numbers
            Return xVersion.CompareTo(yVersion)
        Catch
            ' Fall back to string comparison if version parsing fails
            Return String.Compare(x, y, StringComparison.OrdinalIgnoreCase)
        End Try
    End Function

    Private Function ExtractVersionNumber(versionString As String) As Version
        ' Try to extract version number from string like "2.0.11" or "v2.0.11"
        Dim versionPattern As String = "(\d+\.\d+\.\d+)"
        Dim match As Match = Regex.Match(versionString, versionPattern)

        If match.Success Then
            Return New Version(match.Groups(1).Value)
        Else
            ' Return default version if parsing fails
            Return New Version(0, 0, 0)
        End If
    End Function

End Class

