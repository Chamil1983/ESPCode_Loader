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
    Private boardIdMap As Dictionary(Of String, String) = New Dictionary(Of String, String)() ' Map board names to board IDs
    Private boardSupportedMenus As Dictionary(Of String, HashSet(Of String)) = New Dictionary(Of String, HashSet(Of String))() ' Track which menu options each board supports
    Private boardUnsupportedMenus As Dictionary(Of String, HashSet(Of String)) = New Dictionary(Of String, HashSet(Of String))() ' Track explicitly unsupported options
    Private boardFixedParams As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))() ' Track fixed parameters that can't be changed
    Private boardConfigOrder As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))() ' Track parameter ordering for each board
    Private customPartitionFile As String = String.Empty
    Private boardsFileContent As String = String.Empty ' Store the entire boards.txt content for deep analysis

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

                Debug.WriteLine($"[2025-08-14 23:54:08] Loading boards from: {BoardsFilePath} by Chamil1983")
                ParseBoardsFile(lines)

                ' Perform post-processing to ensure proper compatibility
                PostProcessBoardConfigs()

                ' Log loaded configurations
                Debug.WriteLine($"[2025-08-14 23:54:08] Loaded {boardConfigurations.Count} board configurations by Chamil1983")
            Catch ex As Exception
                MessageBox.Show($"Error loading board configurations: {ex.Message}",
                              "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Debug.WriteLine($"[2025-08-14 23:54:08] Error loading boards: {ex.Message} by Chamil1983")
            End Try
        Else
            Debug.WriteLine($"[2025-08-14 23:54:08] Boards file not found: {BoardsFilePath} by Chamil1983")
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
                "PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "EraseFlash"
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

            Debug.WriteLine($"[2025-08-14 23:54:08] Post-processed board: {boardName}, ID: {boardId} by Chamil1983")
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
            Debug.WriteLine($"[2025-08-14 23:54:08] Special handling for Wrover board: {boardName} by Chamil1983")

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
                Debug.WriteLine($"[2025-08-14 23:54:08] Detected Wrover has fixed PSRAM=enabled by Chamil1983")
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
                Debug.WriteLine($"[2025-08-14 23:54:08] Detected Wrover has fixed CPUFreq=240MHz by Chamil1983")
            End If
        End If

        ' Handle ESP32-S2/S3/C3 specific incompatibilities
        If boardId.Contains("esp32s2") OrElse boardId.Contains("esp32s3") OrElse
           boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c2") OrElse
           boardId.Contains("esp32c6") OrElse boardId.Contains("esp32h2") OrElse
           boardId.Contains("esp32c5") OrElse boardId.Contains("esp32p4") Then

            ' These boards don't support the FlashFreq parameter
            If Not boardUnsupportedMenus.ContainsKey(boardName) Then
                boardUnsupportedMenus(boardName) = New HashSet(Of String)()
            End If

            boardUnsupportedMenus(boardName).Add("FlashFreq")
            Debug.WriteLine($"[2025-08-14 23:54:08] Detected {boardId} doesn't support FlashFreq by Chamil1983")
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
                    Debug.WriteLine($"[2025-08-14 23:54:08] Detected {boardName} has fixed {pattern.Value}={fixedValue} by Chamil1983")
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
        Debug.WriteLine($"[2025-08-14 23:54:08] Parsing boards.txt file with {lines.Length} lines by Chamil1983")

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
                        Debug.WriteLine($"[2025-08-14 23:54:08] Found global menu: {menuKey}={menuValue} by Chamil1983")
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[2025-08-14 23:54:08] Error parsing global menu: {line}, {ex.Message} by Chamil1983")
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

                                Debug.WriteLine($"[2025-08-14 23:54:08] Found board: {boardId}={boardName} by Chamil1983")
                            End If
                        End If
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[2025-08-14 23:54:08] Error parsing board name: {line}, {ex.Message} by Chamil1983")
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
                                            Debug.WriteLine($"[2025-08-14 23:54:08] Board {boardName} named menu option: {menuType}.{optionKey}={value} by Chamil1983")
                                        End If
                                    Else
                                        ' Format: menu.CPUFreq.240=240MHz (older style)
                                        If Not menuOptions(menuType).ContainsKey(optionKey) Then
                                            menuOptions(menuType)(optionKey) = value
                                            Debug.WriteLine($"[2025-08-14 23:54:08] Board {boardName} direct menu option: {menuType}.{optionKey}={value} by Chamil1983")
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"[2025-08-14 23:54:08] Error parsing parameter: {line}, {ex.Message} by Chamil1983")
                    End Try
                End If
            Next

            ' Add missing menu options with standard defaults when needed
            AddMissingMenuOptions(boardName, boardId, menuOptions, supportedMenus)

            ' Build FQBN with default parameters based on board type
            BuildBoardFQBN(boardId, boardName, parameters, menuOptions, supportedMenus)
        Next

        Debug.WriteLine($"[2025-08-14 23:54:08] Finished parsing boards.txt file, found {boardIdMap.Count} boards by Chamil1983")
    End Sub

    ' Add missing menu options with defaults
    Private Sub AddMissingMenuOptions(boardName As String, boardId As String,
                                    menuOptions As Dictionary(Of String, Dictionary(Of String, String)),
                                    supportedMenus As HashSet(Of String))
        ' Only add defaults for menus that this board actually supports
        If supportedMenus.Contains("CPUFreq") AndAlso (menuOptions.ContainsKey("CPUFreq") AndAlso menuOptions("CPUFreq").Count = 0) Then
            ' CPU Frequency options
            If boardId.Contains("esp32c2") Then
                menuOptions("CPUFreq").Add("120", "120MHz")
                menuOptions("CPUFreq").Add("80", "80MHz")
            ElseIf boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c6") Then
                menuOptions("CPUFreq").Add("160", "160MHz")
                menuOptions("CPUFreq").Add("80", "80MHz")
            ElseIf boardId.Contains("esp32h2") Then
                menuOptions("CPUFreq").Add("96", "96MHz")
                menuOptions("CPUFreq").Add("48", "48MHz")
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
            menuOptions("FlashFreq").Add("80", "80MHz")
            menuOptions("FlashFreq").Add("40", "40MHz")
        End If

        If supportedMenus.Contains("PartitionScheme") AndAlso (menuOptions.ContainsKey("PartitionScheme") AndAlso menuOptions("PartitionScheme").Count = 0) Then
            ' Partition Scheme options
            menuOptions("PartitionScheme").Add("default", "Default")
            menuOptions("PartitionScheme").Add("min_spiffs", "Minimal SPIFFS")
            menuOptions("PartitionScheme").Add("min_ota", "Minimal OTA")
            menuOptions("PartitionScheme").Add("huge_app", "Huge APP")
            menuOptions("PartitionScheme").Add("no_ota", "No OTA")
            menuOptions("PartitionScheme").Add("noota_3g", "No OTA (3G)")
        End If

        If supportedMenus.Contains("UploadSpeed") AndAlso (menuOptions.ContainsKey("UploadSpeed") AndAlso menuOptions("UploadSpeed").Count = 0) Then
            ' Upload Speed options
            menuOptions("UploadSpeed").Add("921600", "921600")
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
            "ESP32-C2 Dev Module",
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
                        If Not boardId.Contains("esp32s2") AndAlso Not boardId.Contains("esp32s3") AndAlso
                           Not boardId.Contains("esp32c3") AndAlso Not boardId.Contains("esp32c2") AndAlso
                           Not boardId.Contains("esp32c6") AndAlso Not boardId.Contains("esp32h2") AndAlso
                           Not boardId.Contains("esp32c5") AndAlso Not boardId.Contains("esp32p4") Then
                            If parameters.ContainsKey("build.flash_freq") Then
                                Dim flashFreq = parameters("build.flash_freq")
                                paramList("FlashFreq") = flashFreq
                            Else
                                paramList("FlashFreq") = "80" ' Default
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
               boardId.Contains("esp32c") OrElse
               boardId.Contains("esp32h") OrElse
               boardId.Contains("esp32p") Then
                paramList("CPUFreq") = "240"
            End If
        End If

        If supportedMenus.Contains("FlashMode") AndAlso Not paramList.ContainsKey("FlashMode") Then
            paramList("FlashMode") = "dio"
        End If

        ' Add FlashFreq only for original ESP32 not for S2/S3/C3 variants
        If supportedMenus.Contains("FlashFreq") AndAlso Not paramList.ContainsKey("FlashFreq") Then
            If Not boardId.Contains("esp32s2") AndAlso Not boardId.Contains("esp32s3") AndAlso
               Not boardId.Contains("esp32c3") AndAlso Not boardId.Contains("esp32c2") AndAlso
               Not boardId.Contains("esp32c6") AndAlso Not boardId.Contains("esp32h2") AndAlso
               Not boardId.Contains("esp32c5") AndAlso Not boardId.Contains("esp32p4") Then
                paramList("FlashFreq") = "80"
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
            fqbn &= ":" & paramStr
        End If

        ' Add to configurations
        boardConfigurations(boardName) = fqbn

        Debug.WriteLine($"[2025-08-14 23:54:08] Added board: {boardName}, FQBN: {fqbn} by Chamil1983")
        Debug.WriteLine($"[2025-08-14 23:54:08] Supported menus: {String.Join(", ", supportedMenus)} by Chamil1983")
    End Sub

    ' Add default ESP32 board configurations
    Private Sub AddDefaultConfigurations()
        ' KC-Link boards with default configurations
        boardIdMap("KC-Link PRO A8 (Default)") = "esp32"
        boardIdMap("KC-Link PRO A8 (Minimal)") = "esp32"
        boardIdMap("KC-Link PRO A8 (OTA)") = "esp32"

        boardConfigurations("KC-Link PRO A8 (Default)") = "esp32:esp32:esp32:PartitionScheme=default,CPUFreq=240,FlashMode=qio,FlashFreq=80"
        boardConfigurations("KC-Link PRO A8 (Minimal)") = "esp32:esp32:esp32:PartitionScheme=min_spiffs,CPUFreq=240,FlashMode=qio,FlashFreq=80"
        boardConfigurations("KC-Link PRO A8 (OTA)") = "esp32:esp32:esp32:PartitionScheme=min_ota,CPUFreq=240,FlashMode=qio,FlashFreq=80"

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
        boardConfigOrder("KC-Link PRO A8 (Default)") = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM"}
        boardConfigOrder("KC-Link PRO A8 (Minimal)") = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM"}
        boardConfigOrder("KC-Link PRO A8 (OTA)") = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM"}

        ' Standard ESP32 boards - these will be overridden by boards.txt if available
        boardIdMap("ESP32 Dev Module") = "esp32"
        boardIdMap("ESP32 Wrover Module") = "esp32wrover"
        boardIdMap("ESP32 Wrover Kit") = "esp32wrover"
        boardIdMap("ESP32 PICO-D4") = "pico32"
        boardIdMap("ESP32-S2 Dev Module") = "esp32s2"
        boardIdMap("ESP32-S3 Dev Module") = "esp32s3"
        boardIdMap("ESP32-C2 Dev Module") = "esp32c2"
        boardIdMap("ESP32-C3 Dev Module") = "esp32c3"
        boardIdMap("ESP32-C6 Dev Module") = "esp32c6"
        boardIdMap("ESP32-H2 Dev Module") = "esp32h2"
        boardIdMap("ESP32-C5 Dev Module") = "esp32c5"
        boardIdMap("ESP32-P4 Dev Module") = "esp32p4"

        ' Standard ESP32 boards - original ESP32
        boardConfigurations("ESP32 Dev Module") = "esp32:esp32:esp32:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80"
        boardConfigurations("ESP32 PICO-D4") = "esp32:esp32:pico32:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80"

        ' Wrover boards - no CPUFreq or PSRAM parameters
        boardConfigurations("ESP32 Wrover Module") = "esp32:esp32:esp32wrover:PartitionScheme=default,FlashMode=dio,FlashFreq=80"
        boardConfigurations("ESP32 Wrover Kit") = "esp32:esp32:esp32wrover:PartitionScheme=default,FlashMode=dio,FlashFreq=80"

        ' ESP32-S2/S3 and newer boards - no FlashFreq parameter
        boardConfigurations("ESP32-S2 Dev Module") = "esp32:esp32:esp32s2:PartitionScheme=default,CPUFreq=240,FlashMode=dio"
        boardConfigurations("ESP32-S3 Dev Module") = "esp32:esp32:esp32s3:PartitionScheme=default,CPUFreq=240,FlashMode=dio,USBMode=hwcdc"
        boardConfigurations("ESP32-C2 Dev Module") = "esp32:esp32:esp32c2:PartitionScheme=default,CPUFreq=120,FlashMode=dio"
        boardConfigurations("ESP32-C3 Dev Module") = "esp32:esp32:esp32c3:PartitionScheme=default,CPUFreq=160,FlashMode=dio"
        boardConfigurations("ESP32-C6 Dev Module") = "esp32:esp32:esp32c6:PartitionScheme=default,CPUFreq=160,FlashMode=dio"
        boardConfigurations("ESP32-H2 Dev Module") = "esp32:esp32:esp32h2:PartitionScheme=default,CPUFreq=96,FlashMode=dio"
        boardConfigurations("ESP32-C5 Dev Module") = "esp32:esp32:esp32c5:PartitionScheme=default,CPUFreq=240,FlashMode=dio"
        boardConfigurations("ESP32-P4 Dev Module") = "esp32:esp32:esp32p4:PartitionScheme=default,CPUFreq=240,FlashMode=dio"

        ' Initialize menu options dictionaries for standard boards
        boardMenuOptions("ESP32 Dev Module") = CreateDefaultMenuOptions()
        boardMenuOptions("ESP32 PICO-D4") = CreateDefaultMenuOptions()
        boardMenuOptions("ESP32 Wrover Module") = CreateWroverMenuOptions()
        boardMenuOptions("ESP32 Wrover Kit") = CreateWroverMenuOptions()
        boardMenuOptions("ESP32-S2 Dev Module") = CreateS2MenuOptions()
        boardMenuOptions("ESP32-S3 Dev Module") = CreateS3MenuOptions()
        boardMenuOptions("ESP32-C2 Dev Module") = CreateC2MenuOptions()
        boardMenuOptions("ESP32-C3 Dev Module") = CreateC3MenuOptions()
        boardMenuOptions("ESP32-C6 Dev Module") = CreateC6MenuOptions()
        boardMenuOptions("ESP32-H2 Dev Module") = CreateH2MenuOptions()
        boardMenuOptions("ESP32-C5 Dev Module") = CreateC5MenuOptions()
        boardMenuOptions("ESP32-P4 Dev Module") = CreateP4MenuOptions()

        ' Initialize parameter dictionaries for standard boards
        boardParameters("ESP32 Dev Module") = CreateDefaultBoardParameters()
        boardParameters("ESP32 PICO-D4") = CreateDefaultBoardParameters()
        boardParameters("ESP32 Wrover Module") = CreateWroverBoardParameters()
        boardParameters("ESP32 Wrover Kit") = CreateWroverBoardParameters()
        boardParameters("ESP32-S2 Dev Module") = CreateS2BoardParameters()
        boardParameters("ESP32-S3 Dev Module") = CreateS3BoardParameters()
        boardParameters("ESP32-C2 Dev Module") = CreateC2BoardParameters()
        boardParameters("ESP32-C3 Dev Module") = CreateC3BoardParameters()
        boardParameters("ESP32-C6 Dev Module") = CreateC6BoardParameters()
        boardParameters("ESP32-H2 Dev Module") = CreateH2BoardParameters()
        boardParameters("ESP32-C5 Dev Module") = CreateC5BoardParameters()
        boardParameters("ESP32-P4 Dev Module") = CreateP4BoardParameters()

        ' Initialize supported menus for standard boards
        boardSupportedMenus("ESP32 Dev Module") = CreateDefaultSupportedMenus()
        boardSupportedMenus("ESP32 PICO-D4") = CreateDefaultSupportedMenus()
        boardSupportedMenus("ESP32 Wrover Module") = CreateWroverSupportedMenus()
        boardSupportedMenus("ESP32 Wrover Kit") = CreateWroverSupportedMenus()
        boardSupportedMenus("ESP32-S2 Dev Module") = CreateS2SupportedMenus()
        boardSupportedMenus("ESP32-S3 Dev Module") = CreateS3SupportedMenus()
        boardSupportedMenus("ESP32-C2 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-C3 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-C6 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-H2 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-C5 Dev Module") = CreateCSupportedMenus()
        boardSupportedMenus("ESP32-P4 Dev Module") = CreateCSupportedMenus()

        ' Initialize unsupported menus
        boardUnsupportedMenus("ESP32 Dev Module") = New HashSet(Of String)()
        boardUnsupportedMenus("ESP32 PICO-D4") = New HashSet(Of String)()
        boardUnsupportedMenus("ESP32 Wrover Module") = CreateWroverUnsupportedMenus()
        boardUnsupportedMenus("ESP32 Wrover Kit") = CreateWroverUnsupportedMenus()
        boardUnsupportedMenus("ESP32-S2 Dev Module") = CreateS2UnsupportedMenus()
        boardUnsupportedMenus("ESP32-S3 Dev Module") = CreateS3UnsupportedMenus()
        boardUnsupportedMenus("ESP32-C2 Dev Module") = CreateCUnsupportedMenus()
        boardUnsupportedMenus("ESP32-C3 Dev Module") = CreateCUnsupportedMenus()
        boardUnsupportedMenus("ESP32-C6 Dev Module") = CreateCUnsupportedMenus()
        boardUnsupportedMenus("ESP32-H2 Dev Module") = CreateCUnsupportedMenus()
        boardUnsupportedMenus("ESP32-C5 Dev Module") = CreateCUnsupportedMenus()
        boardUnsupportedMenus("ESP32-P4 Dev Module") = CreateCUnsupportedMenus()

        ' Initialize fixed parameters
        boardFixedParams("ESP32 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32 PICO-D4") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32 Wrover Module") = CreateWroverFixedParams()
        boardFixedParams("ESP32 Wrover Kit") = CreateWroverFixedParams()
        boardFixedParams("ESP32-S2 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-S3 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-C2 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-C3 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-C6 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-H2 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-C5 Dev Module") = New Dictionary(Of String, String)()
        boardFixedParams("ESP32-P4 Dev Module") = New Dictionary(Of String, String)()

        ' Initialize config order for standard boards
        Dim defaultOrder As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "EraseFlash"}
        Dim wroverOrder As New List(Of String) From {"PartitionScheme", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "EraseFlash"}
        Dim s2Order As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "UploadSpeed", "DebugLevel", "PSRAM", "CDCOnBoot", "MSCOnBoot", "DFUOnBoot", "UploadMode", "EraseFlash"}
        Dim s3Order As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "UploadSpeed", "DebugLevel", "PSRAM", "USBMode", "CDCOnBoot", "MSCOnBoot", "DFUOnBoot", "UploadMode", "FlashSize", "LoopCore", "EventsCore", "EraseFlash", "JTAGAdapter"}
        Dim cOrder As New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "UploadSpeed", "DebugLevel", "PSRAM", "EraseFlash"}

        boardConfigOrder("ESP32 Dev Module") = defaultOrder
        boardConfigOrder("ESP32 PICO-D4") = defaultOrder
        boardConfigOrder("ESP32 Wrover Module") = wroverOrder
        boardConfigOrder("ESP32 Wrover Kit") = wroverOrder
        boardConfigOrder("ESP32-S2 Dev Module") = s2Order
        boardConfigOrder("ESP32-S3 Dev Module") = s3Order
        boardConfigOrder("ESP32-C2 Dev Module") = cOrder
        boardConfigOrder("ESP32-C3 Dev Module") = cOrder
        boardConfigOrder("ESP32-C6 Dev Module") = cOrder
        boardConfigOrder("ESP32-H2 Dev Module") = cOrder
        boardConfigOrder("ESP32-C5 Dev Module") = cOrder
        boardConfigOrder("ESP32-P4 Dev Module") = cOrder
    End Sub

    ' Create default supported menus for ESP32 boards
    Private Function CreateDefaultSupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)

        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("FlashFreq")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("PSRAM")
        supportedMenus.Add("EraseFlash")

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

    ' Create fixed parameters for ESP32 Wrover boards
    Private Function CreateWroverFixedParams() As Dictionary(Of String, String)
        Dim fixedParams As New Dictionary(Of String, String)()

        fixedParams("CPUFreq") = "240" ' Fixed at 240MHz
        fixedParams("PSRAM") = "enabled" ' PSRAM is always enabled

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

        Return supportedMenus
    End Function

    ' Create unsupported menus for ESP32-S2 boards
    Private Function CreateS2UnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)

        ' S2 doesn't support FlashFreq
        unsupportedMenus.Add("FlashFreq")

        Return unsupportedMenus
    End Function

    ' Create supported menus for ESP32-S2 boards
    Private Function CreateS2SupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)

        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("PSRAM")
        'supportedMenus.Add("USBMode")
        supportedMenus.Add("CDCOnBoot")
        supportedMenus.Add("MSCOnBoot")
        supportedMenus.Add("DFUOnBoot")
        supportedMenus.Add("UploadMode")
        supportedMenus.Add("EraseFlash")

        Return supportedMenus
    End Function

    ' Create unsupported menus for ESP32-S3 boards
    Private Function CreateS3UnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)

        ' S3 doesn't support FlashFreq
        unsupportedMenus.Add("FlashFreq")

        Return unsupportedMenus
    End Function

    ' Create supported menus for ESP32-S3 boards
    Private Function CreateS3SupportedMenus() As HashSet(Of String)
        Dim supportedMenus = CreateS2SupportedMenus() ' S3 has all the S2 options

        ' Add S3-specific options
        supportedMenus.Add("FlashSize")
        supportedMenus.Add("LoopCore")
        supportedMenus.Add("EventsCore")
        supportedMenus.Add("JTAGAdapter")

        Return supportedMenus
    End Function

    ' Create unsupported menus for ESP32-C series boards
    Private Function CreateCUnsupportedMenus() As HashSet(Of String)
        Dim unsupportedMenus As New HashSet(Of String)

        ' C series doesn't support FlashFreq
        unsupportedMenus.Add("FlashFreq")

        Return unsupportedMenus
    End Function

    ' Create supported menus for ESP32-C series boards
    Private Function CreateCSupportedMenus() As HashSet(Of String)
        Dim supportedMenus As New HashSet(Of String)

        supportedMenus.Add("CPUFreq")
        supportedMenus.Add("FlashMode")
        supportedMenus.Add("PartitionScheme")
        supportedMenus.Add("UploadSpeed")
        supportedMenus.Add("DebugLevel")
        supportedMenus.Add("PSRAM")
        supportedMenus.Add("EraseFlash")

        Return supportedMenus
    End Function

    ' Create default menu options for ESP32 boards
    Private Function CreateDefaultMenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency options
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("240", "240MHz")
        cpuFreqOptions.Add("160", "160MHz")
        cpuFreqOptions.Add("80", "80MHz")
        cpuFreqOptions.Add("40", "40MHz")
        cpuFreqOptions.Add("20", "20MHz")
        cpuFreqOptions.Add("10", "10MHz")
        menuOptions.Add("CPUFreq", cpuFreqOptions)

        ' Flash Mode options
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        flashModeOptions.Add("qout", "QOUT")
        flashModeOptions.Add("dout", "DOUT")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz")
        flashFreqOptions.Add("40", "40MHz")
        flashFreqOptions.Add("20", "20MHz")
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS")
        partitionOptions.Add("min_ota", "Minimal OTA")
        partitionOptions.Add("huge_app", "Huge APP")
        partitionOptions.Add("no_ota", "No OTA")
        partitionOptions.Add("noota_3g", "No OTA (3G)")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
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

        ' PSRAM options
        Dim psramOptions As New Dictionary(Of String, String)
        psramOptions.Add("disabled", "Disabled")
        psramOptions.Add("enabled", "Enabled")
        menuOptions.Add("PSRAM", psramOptions)

        ' EraseFlash options
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "None")
        eraseFlashOptions.Add("all", "All")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

        Return menuOptions
    End Function

    ' Create menu options for ESP32 Wrover boards (No CPUFreq, No PSRAM)
    Private Function CreateWroverMenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' Flash Mode options
        Dim flashModeOptions As New Dictionary(Of String, String)
        flashModeOptions.Add("qio", "QIO")
        flashModeOptions.Add("dio", "DIO")
        flashModeOptions.Add("qout", "QOUT")
        flashModeOptions.Add("dout", "DOUT")
        menuOptions.Add("FlashMode", flashModeOptions)

        ' Flash Frequency options
        Dim flashFreqOptions As New Dictionary(Of String, String)
        flashFreqOptions.Add("80", "80MHz")
        flashFreqOptions.Add("40", "40MHz")
        flashFreqOptions.Add("20", "20MHz")
        menuOptions.Add("FlashFreq", flashFreqOptions)

        ' Partition Scheme options
        Dim partitionOptions As New Dictionary(Of String, String)
        partitionOptions.Add("default", "Default")
        partitionOptions.Add("min_spiffs", "Minimal SPIFFS")
        partitionOptions.Add("min_ota", "Minimal OTA")
        partitionOptions.Add("huge_app", "Huge APP")
        partitionOptions.Add("no_ota", "No OTA")
        partitionOptions.Add("noota_3g", "No OTA (3G)")
        partitionOptions.Add("custom", "Custom")
        menuOptions.Add("PartitionScheme", partitionOptions)

        ' Upload Speed options
        Dim uploadSpeedOptions As New Dictionary(Of String, String)
        uploadSpeedOptions.Add("921600", "921600")
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

        ' Erase Flash options
        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "None")
        eraseFlashOptions.Add("all", "All")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

        Return menuOptions
    End Function

    ' Create menu options for ESP32-S2 boards
    Private Function CreateS2MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = CreateDefaultMenuOptions()

        ' Remove FlashFreq as it's not compatible with S2
        menuOptions.Remove("FlashFreq")

        ' Add S2-specific options
        Dim usbModeOptions As New Dictionary(Of String, String)
        usbModeOptions.Add("hwcdc", "Hardware CDC")
        usbModeOptions.Add("default", "Default")
        menuOptions.Add("USBMode", usbModeOptions)

        Dim cdcOnBootOptions As New Dictionary(Of String, String)
        cdcOnBootOptions.Add("default", "Default")
        cdcOnBootOptions.Add("enabled", "Enabled")
        cdcOnBootOptions.Add("disabled", "Disabled")
        menuOptions.Add("CDCOnBoot", cdcOnBootOptions)

        Dim mscOnBootOptions As New Dictionary(Of String, String)
        mscOnBootOptions.Add("default", "Default")
        mscOnBootOptions.Add("enabled", "Enabled")
        mscOnBootOptions.Add("disabled", "Disabled")
        menuOptions.Add("MSCOnBoot", mscOnBootOptions)

        Dim dfuOnBootOptions As New Dictionary(Of String, String)
        dfuOnBootOptions.Add("default", "Default")
        dfuOnBootOptions.Add("enabled", "Enabled")
        dfuOnBootOptions.Add("disabled", "Disabled")
        menuOptions.Add("DFUOnBoot", dfuOnBootOptions)

        Dim uploadModeOptions As New Dictionary(Of String, String)
        uploadModeOptions.Add("default", "Default")
        uploadModeOptions.Add("usb", "USB")
        uploadModeOptions.Add("uart", "UART")
        menuOptions.Add("UploadMode", uploadModeOptions)

        Return menuOptions
    End Function

    ' Create menu options for ESP32-S3 boards
    Private Function CreateS3MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = CreateS2MenuOptions()  ' S3 has all the S2 options plus some extras

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

        Dim jtagAdapterOptions As New Dictionary(Of String, String)
        jtagAdapterOptions.Add("default", "Default")
        jtagAdapterOptions.Add("custom", "Custom")
        menuOptions.Add("JTAGAdapter", jtagAdapterOptions)

        Return menuOptions
    End Function

    ' Create menu options for ESP32-C2 boards
    Private Function CreateC2MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = CreateDefaultMenuOptions()

        ' Remove FlashFreq as it's not compatible with C2
        menuOptions.Remove("FlashFreq")

        ' Modify CPU frequencies for C2
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("120", "120MHz")
        cpuFreqOptions.Add("96", "96MHz")
        cpuFreqOptions.Add("80", "80MHz")
        cpuFreqOptions.Add("60", "60MHz")
        cpuFreqOptions.Add("48", "48MHz")
        cpuFreqOptions.Add("40", "40MHz")
        cpuFreqOptions.Add("26", "26MHz")
        cpuFreqOptions.Add("20", "20MHz")
        cpuFreqOptions.Add("10", "10MHz")
        menuOptions("CPUFreq") = cpuFreqOptions

        Return menuOptions
    End Function

    ' Create menu options for ESP32-C3 boards
    Private Function CreateC3MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = CreateDefaultMenuOptions()

        ' Remove FlashFreq as it's not compatible with C3
        menuOptions.Remove("FlashFreq")

        ' Modify CPU frequencies for C3
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("160", "160MHz")
        cpuFreqOptions.Add("80", "80MHz")
        cpuFreqOptions.Add("40", "40MHz")
        cpuFreqOptions.Add("20", "20MHz")
        cpuFreqOptions.Add("10", "10MHz")
        menuOptions("CPUFreq") = cpuFreqOptions

        Return menuOptions
    End Function

    ' Create menu options for ESP32-C6 boards
    Private Function CreateC6MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Return CreateC3MenuOptions()  ' Similar options to C3
    End Function

    ' Create menu options for ESP32-H2 boards
    Private Function CreateH2MenuOptions() As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = CreateDefaultMenuOptions()

        ' Remove FlashFreq as it's not compatible with H2
        menuOptions.Remove("FlashFreq")

        ' Modify CPU frequencies for H2
        Dim cpuFreqOptions As New Dictionary(Of String, String)
        cpuFreqOptions.Add("96", "96MHz")
        cpuFreqOptions.Add("48", "48MHz")
        cpuFreqOptions.Add("32", "32MHz")
        cpuFreqOptions.Add("16", "16MHz")
        cpuFreqOptions.Add("8", "8MHz")
        menuOptions("CPUFreq") = cpuFreqOptions

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
        Dim menuOptions = CreateDefaultMenuOptions()

        ' Remove FlashFreq as it's not compatible with P4
        menuOptions.Remove("FlashFreq")

        Return menuOptions
    End Function

    ' Create default parameters for a board
    Private Function CreateDefaultBoardParameters() As Dictionary(Of String, String)
        Dim parameters As New Dictionary(Of String, String)()

        ' Common menu parameters
        parameters("menu.PartitionScheme") = "Partition Scheme"
        parameters("menu.CPUFreq") = "CPU Frequency"
        parameters("menu.FlashMode") = "Flash Mode"
        parameters("menu.FlashFreq") = "Flash Frequency"
        parameters("menu.UploadSpeed") = "Upload Speed"
        parameters("menu.DebugLevel") = "Debug Level"
        parameters("menu.PSRAM") = "PSRAM"
        parameters("menu.EraseFlash") = "Erase Flash"

        ' Default values
        parameters("menu.PartitionScheme.default") = "Default"
        parameters("menu.PartitionScheme.min_spiffs") = "Minimal SPIFFS"
        parameters("menu.PartitionScheme.min_ota") = "Minimal OTA"
        parameters("menu.PartitionScheme.huge_app") = "Huge APP"
        parameters("menu.PartitionScheme.no_ota") = "No OTA"
        parameters("menu.PartitionScheme.noota_3g") = "No OTA (3G)"
        parameters("menu.PartitionScheme.custom") = "Custom"

        parameters("menu.CPUFreq.240") = "240MHz"
        parameters("menu.CPUFreq.160") = "160MHz"
        parameters("menu.CPUFreq.80") = "80MHz"
        parameters("menu.CPUFreq.40") = "40MHz"
        parameters("menu.CPUFreq.20") = "20MHz"
        parameters("menu.CPUFreq.10") = "10MHz"

        parameters("menu.FlashMode.qio") = "QIO"
        parameters("menu.FlashMode.dio") = "DIO"
        parameters("menu.FlashMode.qout") = "QOUT"
        parameters("menu.FlashMode.dout") = "DOUT"

        parameters("menu.FlashFreq.80") = "80MHz"
        parameters("menu.FlashFreq.40") = "40MHz"
        parameters("menu.FlashFreq.20") = "20MHz"

        parameters("menu.UploadSpeed.921600") = "921600"
        parameters("menu.UploadSpeed.460800") = "460800"
        parameters("menu.UploadSpeed.230400") = "230400"
        parameters("menu.UploadSpeed.115200") = "115200"

        parameters("menu.DebugLevel.none") = "None"
        parameters("menu.DebugLevel.error") = "Error"
        parameters("menu.DebugLevel.warn") = "Warning"
        parameters("menu.DebugLevel.info") = "Info"
        parameters("menu.DebugLevel.debug") = "Debug"
        parameters("menu.DebugLevel.verbose") = "Verbose"

        parameters("menu.PSRAM.disabled") = "Disabled"
        parameters("menu.PSRAM.enabled") = "Enabled"

        parameters("menu.EraseFlash.none") = "None"
        parameters("menu.EraseFlash.all") = "All"

        Return parameters
    End Function

    ' Create parameters for ESP32 Wrover boards
    Private Function CreateWroverBoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Add special Wrover information
        parameters("build.board") = "ESP32_DEV"
        parameters("build.variants_dir") = "variants"
        parameters("build.variant") = "esp32"
        parameters("build.has_psram") = "true"  ' Wrover has PSRAM built-in
        parameters("build.f_cpu") = "240000000L"  ' Wrover fixed at 240MHz

        Return parameters
    End Function

    ' Create parameters for ESP32-S2 boards
    Private Function CreateS2BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Remove Flash Frequency as it's not compatible with S2
        parameters.Remove("menu.FlashFreq")
        parameters.Remove("menu.FlashFreq.80")
        parameters.Remove("menu.FlashFreq.40")
        parameters.Remove("menu.FlashFreq.20")

        '' Add S2-specific parameters
        'parameters("menu.USBMode") = "USB Mode"
        'parameters("menu.USBMode.hwcdc") = "Hardware CDC"
        'parameters("menu.USBMode.default") = "Default"

        parameters("menu.CDCOnBoot") = "CDC On Boot"
        parameters("menu.CDCOnBoot.default") = "Default"
        parameters("menu.CDCOnBoot.enabled") = "Enabled"
        parameters("menu.CDCOnBoot.disabled") = "Disabled"

        parameters("menu.MSCOnBoot") = "MSC On Boot"
        parameters("menu.MSCOnBoot.default") = "Default"
        parameters("menu.MSCOnBoot.enabled") = "Enabled"
        parameters("menu.MSCOnBoot.disabled") = "Disabled"

        parameters("menu.DFUOnBoot") = "DFU On Boot"
        parameters("menu.DFUOnBoot.default") = "Default"
        parameters("menu.DFUOnBoot.enabled") = "Enabled"
        parameters("menu.DFUOnBoot.disabled") = "Disabled"

        parameters("menu.UploadMode") = "Upload Mode"
        parameters("menu.UploadMode.default") = "Default"
        parameters("menu.UploadMode.usb") = "USB"
        parameters("menu.UploadMode.uart") = "UART"

        Return parameters
    End Function

    ' Create parameters for ESP32-S3 boards
    Private Function CreateS3BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateS2BoardParameters()  ' S3 has all the S2 parameters plus some extras

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

        parameters("menu.JTAGAdapter") = "JTAG Adapter"
        parameters("menu.JTAGAdapter.default") = "Default"
        parameters("menu.JTAGAdapter.custom") = "Custom"

        Return parameters
    End Function

    ' Create parameters for ESP32-C2 boards
    Private Function CreateC2BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Remove Flash Frequency as it's not compatible with C2
        parameters.Remove("menu.FlashFreq")
        parameters.Remove("menu.FlashFreq.80")
        parameters.Remove("menu.FlashFreq.40")
        parameters.Remove("menu.FlashFreq.20")

        ' Replace CPU Frequencies for C2
        parameters.Remove("menu.CPUFreq.240")
        parameters.Remove("menu.CPUFreq.160")
        parameters.Remove("menu.CPUFreq.80")
        parameters.Remove("menu.CPUFreq.40")
        parameters.Remove("menu.CPUFreq.20")
        parameters.Remove("menu.CPUFreq.10")

        parameters("menu.CPUFreq.120") = "120MHz"
        parameters("menu.CPUFreq.96") = "96MHz"
        parameters("menu.CPUFreq.80") = "80MHz"
        parameters("menu.CPUFreq.60") = "60MHz"
        parameters("menu.CPUFreq.48") = "48MHz"
        parameters("menu.CPUFreq.40") = "40MHz"
        parameters("menu.CPUFreq.26") = "26MHz"
        parameters("menu.CPUFreq.20") = "20MHz"
        parameters("menu.CPUFreq.10") = "10MHz"

        Return parameters
    End Function

    ' Create parameters for ESP32-C3 boards
    Private Function CreateC3BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Remove Flash Frequency as it's not compatible with C3
        parameters.Remove("menu.FlashFreq")
        parameters.Remove("menu.FlashFreq.80")
        parameters.Remove("menu.FlashFreq.40")
        parameters.Remove("menu.FlashFreq.20")

        ' Replace CPU Frequencies for C3
        parameters.Remove("menu.CPUFreq.240")
        parameters.Remove("menu.CPUFreq.160")
        parameters.Remove("menu.CPUFreq.80")
        parameters.Remove("menu.CPUFreq.40")
        parameters.Remove("menu.CPUFreq.20")
        parameters.Remove("menu.CPUFreq.10")

        parameters("menu.CPUFreq.160") = "160MHz"
        parameters("menu.CPUFreq.80") = "80MHz"
        parameters("menu.CPUFreq.40") = "40MHz"
        parameters("menu.CPUFreq.20") = "20MHz"
        parameters("menu.CPUFreq.10") = "10MHz"

        Return parameters
    End Function

    ' Create parameters for ESP32-C6 boards
    Private Function CreateC6BoardParameters() As Dictionary(Of String, String)
        Return CreateC3BoardParameters()  ' Similar parameters to C3
    End Function

    ' Create parameters for ESP32-H2 boards
    Private Function CreateH2BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Remove Flash Frequency as it's not compatible with H2
        parameters.Remove("menu.FlashFreq")
        parameters.Remove("menu.FlashFreq.80")
        parameters.Remove("menu.FlashFreq.40")
        parameters.Remove("menu.FlashFreq.20")

        ' Replace CPU Frequencies for H2
        parameters.Remove("menu.CPUFreq.240")
        parameters.Remove("menu.CPUFreq.160")
        parameters.Remove("menu.CPUFreq.80")
        parameters.Remove("menu.CPUFreq.40")
        parameters.Remove("menu.CPUFreq.20")
        parameters.Remove("menu.CPUFreq.10")

        parameters("menu.CPUFreq.96") = "96MHz"
        parameters("menu.CPUFreq.48") = "48MHz"
        parameters("menu.CPUFreq.32") = "32MHz"
        parameters("menu.CPUFreq.16") = "16MHz"
        parameters("menu.CPUFreq.8") = "8MHz"

        Return parameters
    End Function

    ' Create parameters for ESP32-C5 boards
    Private Function CreateC5BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Remove Flash Frequency as it's not compatible with C5
        parameters.Remove("menu.FlashFreq")
        parameters.Remove("menu.FlashFreq.80")
        parameters.Remove("menu.FlashFreq.40")
        parameters.Remove("menu.FlashFreq.20")

        Return parameters
    End Function

    ' Create parameters for ESP32-P4 boards
    Private Function CreateP4BoardParameters() As Dictionary(Of String, String)
        Dim parameters = CreateDefaultBoardParameters()

        ' Remove Flash Frequency as it's not compatible with P4
        parameters.Remove("menu.FlashFreq")
        parameters.Remove("menu.FlashFreq.80")
        parameters.Remove("menu.FlashFreq.40")
        parameters.Remove("menu.FlashFreq.20")

        Return parameters
    End Function

    ' Extract all parameters from FQBN
    Public Function ExtractParametersFromFQBN(fqbn As String) As Dictionary(Of String, String)
        Dim parameters As New Dictionary(Of String, String)()

        Debug.WriteLine($"[2025-08-15 00:00:50] Extracting parameters from FQBN: {fqbn} by Chamil1983")

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

        Debug.WriteLine($"[2025-08-15 00:00:50] Board ID: {boardId}, Board Name: {boardName} by Chamil1983")

        ' Add fixed parameters first
        For Each kvp In fixedParams
            parameters(kvp.Key) = kvp.Value
            Debug.WriteLine($"[2025-08-15 00:00:50] Adding fixed parameter: {kvp.Key}={kvp.Value} by Chamil1983")
        Next

        ' Only add default values for menus that are supported by this board and not explicitly unsupported
        If supportedMenus.Contains("CPUFreq") AndAlso Not unsupportedMenus.Contains("CPUFreq") Then
            If boardId.Contains("esp32c2") Then
                parameters("CPUFreq") = "120"
            ElseIf boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c6") Then
                parameters("CPUFreq") = "160"
            ElseIf boardId.Contains("esp32h2") Then
                parameters("CPUFreq") = "96"
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
            parameters("FlashFreq") = "80"
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
                                Debug.WriteLine($"[2025-08-15 00:00:50] Found parameter: {keyValue(0)}={keyValue(1)} by Chamil1983")
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

        Debug.WriteLine($"[2025-08-15 00:00:50] Extracted {parameters.Count} parameters from FQBN by Chamil1983")
        Return parameters
    End Function

    ' Get all available configuration options for a board with user-friendly values
    Public Function GetAllBoardConfigOptions(boardName As String) As Dictionary(Of String, List(Of KeyValuePair(Of String, String)))
        Dim allOptions As New Dictionary(Of String, List(Of KeyValuePair(Of String, String)))

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
                configOrder = New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "EraseFlash"}

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
                If Not supportedMenus.Contains(category) OrElse
                   unsupportedMenus.Contains(category) OrElse
                   fixedParams.ContainsKey(category) Then
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
                    ' Only add if both key and value are unique
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

    Public Function GetParameterOptions(boardName As String, paramName As String) As Dictionary(Of String, String)
        Dim result As New Dictionary(Of String, String)()

        Debug.WriteLine($"[2025-08-15 00:00:50] Getting parameter options for {boardName}, parameter {paramName} by Chamil1983")

        ' Get supported and unsupported menus for this board
        Dim supportedMenus As New HashSet(Of String)()
        Dim unsupportedMenus As New HashSet(Of String)()
        Dim fixedParams As New Dictionary(Of String, String)()

        If boardSupportedMenus.ContainsKey(boardName) Then
            supportedMenus = boardSupportedMenus(boardName)
        End If

        If boardUnsupportedMenus.ContainsKey(boardName) Then
            unsupportedMenus = boardUnsupportedMenus(boardName)
        End If

        If boardFixedParams.ContainsKey(boardName) Then
            fixedParams = boardFixedParams(boardName)
        End If

        ' Skip if this parameter is not supported by this board, is explicitly unsupported, or is a fixed parameter
        If Not supportedMenus.Contains(paramName) OrElse
           unsupportedMenus.Contains(paramName) OrElse
           fixedParams.ContainsKey(paramName) Then
            Debug.WriteLine($"[2025-08-15 00:00:50] Parameter {paramName} is not available for {boardName} by Chamil1983")
            Return result
        End If

        ' First check if we have menu options for this board and parameter
        If boardMenuOptions.ContainsKey(boardName) AndAlso boardMenuOptions(boardName).ContainsKey(paramName) Then
            ' Return the menu options for this parameter
            For Each kvp As KeyValuePair(Of String, String) In boardMenuOptions(boardName)(paramName)
                ' Only add if not already in result to avoid duplicates
                If Not result.ContainsKey(kvp.Key) Then
                    result(kvp.Key) = kvp.Value
                End If
            Next

            Debug.WriteLine($"[2025-08-15 00:00:50] Found {result.Count} options in boardMenuOptions by Chamil1983")
        End If

        ' Fall back to looking in board parameters
        If result.Count = 0 AndAlso boardParameters.ContainsKey(boardName) Then
            Dim parameters = boardParameters(boardName)
            Dim prefix = $"menu.{paramName}."

            For Each key In parameters.Keys
                If key.StartsWith(prefix) Then
                    Dim value = key.Substring(prefix.Length)
                    Dim displayName = parameters(key)

                    ' Only add if not already in the result - avoid duplicates
                    If Not result.ContainsKey(value) Then
                        result(value) = displayName
                    End If
                End If
            Next

            Debug.WriteLine($"[2025-08-15 00:00:50] Found {result.Count} options in boardParameters by Chamil1983")
        End If

        ' Add default options if none found
        If result.Count = 0 Then
            Debug.WriteLine($"[2025-08-15 00:00:50] No options found, adding defaults for {paramName} by Chamil1983")

            ' Get board ID
            Dim boardId = GetBoardId(boardName)

            Select Case paramName
                Case "CPUFreq"
                    ' Check board type for appropriate CPU frequencies
                    If boardId.Contains("esp32c2") Then
                        result.Add("120", "120MHz")
                        result.Add("96", "96MHz")
                        result.Add("80", "80MHz")
                        result.Add("60", "60MHz")
                    ElseIf boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c6") Then
                        result.Add("160", "160MHz")
                        result.Add("80", "80MHz")
                        result.Add("40", "40MHz")
                    ElseIf boardId.Contains("esp32h2") Then
                        result.Add("96", "96MHz")
                        result.Add("48", "48MHz")
                        result.Add("32", "32MHz")
                        result.Add("16", "16MHz")
                    Else
                        result.Add("240", "240MHz")
                        result.Add("160", "160MHz")
                        result.Add("80", "80MHz")
                        result.Add("40", "40MHz")
                        result.Add("20", "20MHz")
                        result.Add("10", "10MHz")
                    End If
                Case "FlashMode"
                    result.Add("qio", "QIO")
                    result.Add("dio", "DIO")
                    result.Add("qout", "QOUT")
                    result.Add("dout", "DOUT")
                Case "FlashFreq"
                    result.Add("80", "80MHz")
                    result.Add("40", "40MHz")
                    result.Add("20", "20MHz")
                Case "PartitionScheme"
                    result.Add("default", "Default")
                    result.Add("min_spiffs", "Minimal SPIFFS")
                    result.Add("min_ota", "Minimal OTA")
                    result.Add("huge_app", "Huge APP")
                    result.Add("no_ota", "No OTA")
                    result.Add("noota_3g", "No OTA (3G)")
                    result.Add("custom", "Custom")
                Case "UploadSpeed"
                    result.Add("921600", "921600")
                    result.Add("460800", "460800")
                    result.Add("230400", "230400")
                    result.Add("115200", "115200")
                Case "DebugLevel"
                    result.Add("none", "None")
                    result.Add("error", "Error")
                    result.Add("warn", "Warning")
                    result.Add("info", "Info")
                    result.Add("debug", "Debug")
                    result.Add("verbose", "Verbose")
                Case "PSRAM"
                    result.Add("disabled", "Disabled")
                    result.Add("enabled", "Enabled")
                Case "EraseFlash"
                    result.Add("none", "None")
                    result.Add("all", "All")
                ' ESP32-S2/S3 specific options
                Case "USBMode"
                    result.Add("hwcdc", "Hardware CDC")
                    result.Add("default", "Default")
                Case "CDCOnBoot", "MSCOnBoot", "DFUOnBoot"
                    result.Add("default", "Default")
                    result.Add("enabled", "Enabled")
                    result.Add("disabled", "Disabled")
                Case "UploadMode"
                    result.Add("default", "Default")
                    result.Add("usb", "USB")
                    result.Add("uart", "UART")
                ' ESP32-S3 specific options
                Case "FlashSize"
                    result.Add("4M", "4MB")
                    result.Add("8M", "8MB")
                    result.Add("16M", "16MB")
                    result.Add("32M", "32MB")
                Case "LoopCore", "EventsCore"
                    result.Add("1", "Core 1")
                    result.Add("0", "Core 0")
                Case "JTAGAdapter"
                    result.Add("default", "Default")
                    result.Add("custom", "Custom")
            End Select
        End If

        Debug.WriteLine($"[2025-08-15 00:00:50] Returning {result.Count} options for parameter {paramName} by Chamil1983")
        Return result
    End Function

    Public Function GetFQBN(boardName As String) As String
        ' Return the FQBN for the given board name
        If boardConfigurations.ContainsKey(boardName) Then
            Return boardConfigurations(boardName)
        Else
            ' Default FQBN for ESP32
            Return "esp32:esp32:esp32"
        End If
    End Function

    Public Function GetBoardNames() As List(Of String)
        ' Return list of board names
        Return New List(Of String)(boardConfigurations.Keys)
    End Function

    Public Function GetBoardId(boardName As String) As String
        ' Return the board ID for a given board name
        If boardIdMap.ContainsKey(boardName) Then
            Return boardIdMap(boardName)
        Else
            ' Default board ID for ESP32
            Return "esp32"
        End If
    End Function

    Public Function GetBoardParameters(boardName As String) As Dictionary(Of String, String)
        ' Return parameters for the given board
        If boardParameters.ContainsKey(boardName) Then
            Return boardParameters(boardName)
        Else
            ' Return empty dictionary if board not found
            Return New Dictionary(Of String, String)()
        End If
    End Function

    Public Function GetSupportedMenus(boardName As String) As HashSet(Of String)
        ' Return the set of supported menu options for a board
        If boardSupportedMenus.ContainsKey(boardName) Then
            Return boardSupportedMenus(boardName)
        Else
            ' Default empty set
            Return New HashSet(Of String)()
        End If
    End Function

    Public Function GetUnsupportedMenus(boardName As String) As HashSet(Of String)
        ' Return the set of explicitly unsupported menu options for a board
        If boardUnsupportedMenus.ContainsKey(boardName) Then
            Return boardUnsupportedMenus(boardName)
        Else
            ' Default empty set
            Return New HashSet(Of String)()
        End If
    End Function

    Public Function GetFixedParameters(boardName As String) As Dictionary(Of String, String)
        ' Return the fixed parameters for a board
        If boardFixedParams.ContainsKey(boardName) Then
            Return boardFixedParams(boardName)
        Else
            ' Default empty dictionary
            Return New Dictionary(Of String, String)()
        End If
    End Function

    Public Function GetConfigOrder(boardName As String) As List(Of String)
        ' Return the parameter ordering for a board
        If boardConfigOrder.ContainsKey(boardName) Then
            Return boardConfigOrder(boardName)
        Else
            ' Default order
            Return New List(Of String) From {"PartitionScheme", "CPUFreq", "FlashMode", "FlashFreq", "UploadSpeed", "DebugLevel", "PSRAM", "EraseFlash"}
        End If
    End Function

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
            ' Add new board configuration
            Dim vendor = "esp32"
            Dim architecture = "esp32"
            Dim boardId = "esp32"

            ' Determine board ID from name
            If boardName.Contains("S2") Then
                boardId = "esp32s2"
            ElseIf boardName.Contains("S3") Then
                boardId = "esp32s3"
            ElseIf boardName.Contains("C2") Then
                boardId = "esp32c2"
            ElseIf boardName.Contains("C3") Then
                boardId = "esp32c3"
            ElseIf boardName.Contains("C6") Then
                boardId = "esp32c6"
            ElseIf boardName.Contains("H2") Then
                boardId = "esp32h2"
            ElseIf boardName.Contains("C5") Then
                boardId = "esp32c5"
            ElseIf boardName.Contains("P4") Then
                boardId = "esp32p4"
            ElseIf boardName.Contains("Wrover") Then
                boardId = "esp32wrover"
            ElseIf boardName.Contains("Pico") Then
                boardId = "pico32"
            End If

            ' Create default supported menus for this board type
            Dim supportedMenus As New HashSet(Of String)()

            ' Standard menus that all boards should support
            supportedMenus.Add("PartitionScheme")
            supportedMenus.Add("FlashMode")
            supportedMenus.Add("UploadSpeed")
            supportedMenus.Add("DebugLevel")
            supportedMenus.Add("EraseFlash")

            ' Create default unsupported menus
            Dim unsupportedMenus As New HashSet(Of String)()

            ' Create fixed parameters
            Dim fixedParams As New Dictionary(Of String, String)()

            ' Special handling for Wrover boards
            If boardId.Equals("esp32wrover") Then
                unsupportedMenus.Add("CPUFreq")
                unsupportedMenus.Add("PSRAM")
                fixedParams("CPUFreq") = "240"
                fixedParams("PSRAM") = "enabled"
            Else
                ' For non-Wrover boards, add CPUFreq and PSRAM
                supportedMenus.Add("CPUFreq")
                supportedMenus.Add("PSRAM")
            End If

            ' Handle FlashFreq compatibility
            If boardId.Contains("esp32s2") OrElse boardId.Contains("esp32s3") OrElse
               boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c2") OrElse
               boardId.Contains("esp32c6") OrElse boardId.Contains("esp32h2") OrElse
               boardId.Contains("esp32c5") OrElse boardId.Contains("esp32p4") Then
                unsupportedMenus.Add("FlashFreq")
            Else
                supportedMenus.Add("FlashFreq")
            End If

            ' Add S2-specific menus
            If boardId.Contains("esp32s2") Then
                'supportedMenus.Add("USBMode")
                supportedMenus.Add("CDCOnBoot")
                supportedMenus.Add("MSCOnBoot")
                supportedMenus.Add("DFUOnBoot")
                supportedMenus.Add("UploadMode")
            End If

            ' Add S3-specific menus
            If boardId.Contains("esp32s3") Then
                supportedMenus.Add("USBMode")
                supportedMenus.Add("CDCOnBoot")
                supportedMenus.Add("MSCOnBoot")
                supportedMenus.Add("DFUOnBoot")
                supportedMenus.Add("UploadMode")
                supportedMenus.Add("FlashSize")
                supportedMenus.Add("LoopCore")
                supportedMenus.Add("EventsCore")
                supportedMenus.Add("JTAGAdapter")
            End If

            ' Save the supported and unsupported menus, and fixed parameters for this board
            boardSupportedMenus(boardName) = supportedMenus
            boardUnsupportedMenus(boardName) = unsupportedMenus
            boardFixedParams(boardName) = fixedParams

            ' Build parameter string
            Dim paramList As New List(Of String)

            ' First add all fixed parameters
            For Each kvp In fixedParams
                paramList.Add($"{kvp.Key}={kvp.Value}")
            Next

            ' Add partition scheme first if available
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

            ' Create new FQBN with parameters
            Dim newFqbn = $"{vendor}:{architecture}:{boardId}"
            If Not String.IsNullOrEmpty(paramStr) Then
                newFqbn &= ":" & paramStr
            End If

            ' Add the configuration
            boardConfigurations(boardName) = newFqbn
            boardIdMap(boardName) = boardId

            ' Initialize menu options dictionary for this board
            boardMenuOptions(boardName) = New Dictionary(Of String, Dictionary(Of String, String))()

            ' Initialize parameter dictionary for this board
            boardParameters(boardName) = New Dictionary(Of String, String)()

            ' Log the addition
            Debug.WriteLine($"[2025-08-15 00:00:50] Added new board configuration for {boardName}: {newFqbn} by Chamil1983")
        End If
    End Sub

    ' Custom partition file methods
    Public Sub SetCustomPartitionFile(filePath As String)
        ' Set custom partition file path
        customPartitionFile = filePath

        ' Try to copy the partition file to the Arduino hardware directory
        Try
            ' Ensure the partition file has a proper name
            Dim fileName = Path.GetFileName(filePath)
            Dim partitionName = Path.GetFileNameWithoutExtension(fileName).ToLower()

            ' Check if boards.txt file exists and we can determine the Arduino directory
            If File.Exists(BoardsFilePath) Then
                Dim arduinoDir = Path.GetDirectoryName(BoardsFilePath)
                Dim partitionsDir = Path.Combine(arduinoDir, "tools", "partitions")

                ' Create partitions directory if it doesn't exist
                If Not Directory.Exists(partitionsDir) Then
                    Directory.CreateDirectory(partitionsDir)
                End If

                ' Copy the custom partition file to the partitions directory
                Dim destFile = Path.Combine(partitionsDir, "custom.csv")
                File.Copy(filePath, destFile, True)

                ' Log success
                Debug.WriteLine($"[2025-08-15 00:00:50] Custom partition file copied to {destFile} by Chamil1983")
            End If
        Catch ex As Exception
            ' Log error but continue
            Debug.WriteLine($"[2025-08-15 00:00:50] Error copying custom partition file: {ex.Message} by Chamil1983")
        End Try
    End Sub

    Public Function ApplyCustomPartitionFile(fqbn As String) As String
        ' Apply a custom partition file to the FQBN if one is set
        If String.IsNullOrEmpty(customPartitionFile) Then
            Return fqbn
        End If

        Dim result = fqbn

        ' Replace or add the partition scheme parameter
        If result.Contains("PartitionScheme=") Then
            result = Regex.Replace(result, "PartitionScheme=[^,]+", "PartitionScheme=custom")
        Else
            If result.Contains(":") Then
                If Not result.Contains(":PartitionScheme=") Then
                    If result.Contains(",") Then
                        result = $"{result},PartitionScheme=custom"
                    Else
                        result = $"{result}:PartitionScheme=custom"
                    End If
                End If
            End If
        End If

        ' Add additional arduino-cli build flag for custom partition file
        result += " --build-property build.partitions=custom"

        Return result
    End Function

    Public Function ApplyPartitionScheme(fqbn As String, partitionScheme As String) As String
        ' Apply a partition scheme to the FQBN
        Dim result = fqbn

        ' Check if the FQBN already has a partition scheme
        If result.Contains("PartitionScheme=") Then
            ' Replace existing partition scheme
            result = Regex.Replace(result, "PartitionScheme=[^,]+", $"PartitionScheme={partitionScheme}")
        Else
            ' Add partition scheme parameter
            If result.Contains(":") Then
                If Not result.Contains(":PartitionScheme=") Then
                    If result.Contains(",") Then
                        ' Add partition scheme to existing parameters
                        result = $"{result},PartitionScheme={partitionScheme}"
                    Else
                        ' Add partition scheme as the first parameter
                        result = $"{result}:PartitionScheme={partitionScheme}"
                    End If
                End If
            End If
        End If

        Return result
    End Function

    Public Function GetDefaultPartitionForBoard(boardName As String) As String
        ' Extract the default partition scheme for a board
        If boardConfigurations.ContainsKey(boardName) Then
            Dim fqbn = boardConfigurations(boardName)

            ' Look for partition scheme in FQBN
            Dim match = Regex.Match(fqbn, "PartitionScheme=([^,]+)")
            If match.Success Then
                Return match.Groups(1).Value
            End If

            ' Use naming conventions if available
            If boardName.Contains("OTA") Then
                Return "min_ota"
            ElseIf boardName.Contains("Minimal") Then
                Return "min_spiffs"
            End If

            ' Check if the board parameters contain build.partitions
            If boardParameters.ContainsKey(boardName) AndAlso boardParameters(boardName).ContainsKey("build.partitions") Then
                Return boardParameters(boardName)("build.partitions")
            End If
        End If

        Return "default"
    End Function

    Public Function GetCustomPartitions() As List(Of String)
        ' Return a list of available custom partition schemes
        Dim partitionsList As New List(Of String)

        ' Check for partition files in expected location
        Dim partitionsDir As String = String.Empty

        If File.Exists(BoardsFilePath) Then
            partitionsDir = Path.Combine(Path.GetDirectoryName(BoardsFilePath), "tools", "partitions")
        Else
            ' Try to find in standard locations
            Dim possiblePaths = New String() {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Arduino", "hardware", "espressif", "esp32", "tools", "partitions"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arduino15", "packages", "esp32", "hardware", "esp32", "tools", "partitions"),
                Path.Combine(Application.StartupPath, "hardware", "esp32", "tools", "partitions")
            }

            For Each path In possiblePaths
                If Directory.Exists(path) Then
                    partitionsDir = path
                    Exit For
                End If
            Next
        End If

        If Directory.Exists(partitionsDir) Then
            Try
                ' Find all .csv partition files
                Dim files As String() = Directory.GetFiles(partitionsDir, "*.csv")

                For Each file In files
                    Dim partitionName = Path.GetFileNameWithoutExtension(file)
                    If Not partitionsList.Contains(partitionName) Then
                        partitionsList.Add(partitionName)
                    End If
                Next
            Catch ex As Exception
                ' Ignore errors
                Debug.WriteLine($"[2025-08-15 00:00:50] Error reading partition files: {ex.Message} by Chamil1983")
            End Try
        End If

        ' Add default partitions if list is empty
        If partitionsList.Count = 0 OrElse Not partitionsList.Contains("default") Then
            partitionsList.Add("default")
        End If
        If Not partitionsList.Contains("min_spiffs") Then
            partitionsList.Add("min_spiffs")
        End If
        If Not partitionsList.Contains("min_ota") Then
            partitionsList.Add("min_ota")
        End If
        If Not partitionsList.Contains("huge_app") Then
            partitionsList.Add("huge_app")
        End If
        If Not partitionsList.Contains("no_ota") Then
            partitionsList.Add("no_ota")
        End If
        If Not partitionsList.Contains("noota_3g") Then
            partitionsList.Add("noota_3g")
        End If

        Return partitionsList.Distinct().ToList()
    End Function
End Class

' Helper class to sort version directories
Public Class VersionComparer
    Implements IComparer(Of String)

    Public Function Compare(x As String, y As String) As Integer Implements IComparer(Of String).Compare
        Dim xVersion = Path.GetFileName(x)
        Dim yVersion = Path.GetFileName(y)

        ' Parse version components
        Dim xParts = xVersion.Split(New Char() {"."c})
        Dim yParts = yVersion.Split(New Char() {"."c})

        ' Compare each part
        For i As Integer = 0 To Math.Min(xParts.Length, yParts.Length) - 1
            Dim xNum As Integer = 0
            Dim yNum As Integer = 0

            Integer.TryParse(xParts(i), xNum)
            Integer.TryParse(yParts(i), yNum)

            If xNum <> yNum Then
                Return xNum.CompareTo(yNum)
            End If
        Next

        ' If one version has more components, it's newer
        Return xParts.Length.CompareTo(yParts.Length)
    End Function
End Class