Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Windows.Forms
Imports System.Text.RegularExpressions

Public Class BoardManager
    ' Private fields
    Private boardConfigurations As Dictionary(Of String, String) = New Dictionary(Of String, String)()
    Private boardParameters As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))()
    Private boardMenuOptions As Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String))) = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))()
    Private boardIdMap As Dictionary(Of String, String) = New Dictionary(Of String, String)() ' Map board names to board IDs
    Private customPartitionFile As String = String.Empty

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

        ' Add default configurations
        AddDefaultConfigurations()

        ' Load custom configurations if file exists
        If File.Exists(BoardsFilePath) Then
            Try
                Dim lines As String() = File.ReadAllLines(BoardsFilePath)

                Debug.WriteLine($"[2025-08-13 12:33:59] Loading boards from: {BoardsFilePath}")
                ParseBoardsFile(lines)

                ' Log loaded configurations
                Debug.WriteLine($"[2025-08-13 12:33:59] Loaded {boardConfigurations.Count} board configurations")
            Catch ex As Exception
                MessageBox.Show($"Error loading board configurations: {ex.Message}",
                              "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Debug.WriteLine($"[2025-08-13 12:33:59] Error loading boards: {ex.Message}")
            End Try
        Else
            Debug.WriteLine($"[2025-08-13 12:33:59] Boards file not found: {BoardsFilePath}")
        End If
    End Sub

    ' Parse boards.txt file to extract all board configurations
    Private Sub ParseBoardsFile(lines As String())
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
                        Debug.WriteLine($"[2025-08-13 12:33:59] Found global menu: {menuKey}={menuValue}")
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[2025-08-13 12:33:59] Error parsing global menu: {line}, {ex.Message}")
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

                                Debug.WriteLine($"[2025-08-13 12:33:59] Found board: {boardId}={boardName}")
                            End If
                        End If
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[2025-08-13 12:33:59] Error parsing board name: {line}, {ex.Message}")
                End Try
            End If
        Next

        ' Third pass: extract all parameters and menu options for each board
        For Each boardName In boardIdMap.Keys
            Dim boardId = boardIdMap(boardName)
            Dim parameters = boardParameters(boardName)
            Dim menuOptions = boardMenuOptions(boardName)

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

                            ' Check if this is a menu option
                            If key.StartsWith("menu.") Then
                                Dim menuParts = key.Split(New Char() {"."c}, 3)
                                If menuParts.Length >= 3 Then
                                    Dim menuType = menuParts(1)
                                    Dim optionKey = menuParts(2)

                                    ' Make sure the menu type dictionary exists
                                    If Not menuOptions.ContainsKey(menuType) Then
                                        menuOptions(menuType) = New Dictionary(Of String, String)()
                                    End If

                                    ' Add the menu option
                                    menuOptions(menuType)(optionKey) = value
                                    Debug.WriteLine($"[2025-08-13 12:33:59] Board {boardName} menu option: {menuType}.{optionKey}={value}")
                                End If
                            End If

                            ' Store all parameters
                            parameters(key) = value
                        End If
                    Catch ex As Exception
                        Debug.WriteLine($"[2025-08-13 12:33:59] Error parsing parameter: {line}, {ex.Message}")
                    End Try
                End If
            Next

            ' Build FQBN with default parameters based on board type
            BuildBoardFQBN(boardId, boardName, parameters, menuOptions)
        Next
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
    Private Sub BuildBoardFQBN(boardId As String, boardName As String, parameters As Dictionary(Of String, String), menuOptions As Dictionary(Of String, Dictionary(Of String, String)))
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

        ' Process menu options to extract defaults
        For Each menuType In menuOptions.Keys
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
                        paramList("CPUFreq") = "240" ' Default
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
                    ' Default PSRAM is disabled
                    paramList("PSRAM") = "disabled"

                Case Else
                    ' For any other menu type, try to find default
                    Dim defaultKey = menuType.ToLower() & ".default"
                    If parameters.ContainsKey(defaultKey) Then
                        paramList(menuType) = parameters(defaultKey)
                    End If
            End Select
        Next

        ' Set defaults for core parameters if not already set
        If Not paramList.ContainsKey("PartitionScheme") Then paramList("PartitionScheme") = "default"
        If Not paramList.ContainsKey("CPUFreq") Then paramList("CPUFreq") = "240"
        If Not paramList.ContainsKey("FlashMode") Then paramList("FlashMode") = "dio"

        ' Add FlashFreq only for original ESP32 not for S2/S3/C3 variants
        If Not boardId.Contains("esp32s2") AndAlso Not boardId.Contains("esp32s3") AndAlso
           Not boardId.Contains("esp32c3") AndAlso Not boardId.Contains("esp32c2") AndAlso
           Not boardId.Contains("esp32c6") AndAlso Not boardId.Contains("esp32h2") AndAlso
           Not boardId.Contains("esp32c5") AndAlso Not boardId.Contains("esp32p4") Then
            If Not paramList.ContainsKey("FlashFreq") Then paramList("FlashFreq") = "80"
        End If

        If Not paramList.ContainsKey("UploadSpeed") Then paramList("UploadSpeed") = "921600"
        If Not paramList.ContainsKey("DebugLevel") Then paramList("DebugLevel") = "none"
        If Not paramList.ContainsKey("PSRAM") Then paramList("PSRAM") = "disabled"

        ' Add specific parameters for newer boards
        If boardId.Contains("esp32s3") Then
            ' ESP32-S3 specific parameters
            paramList("USBMode") = "hwcdc"
            paramList("CDCOnBoot") = "default"
            paramList("MSCOnBoot") = "default"
            paramList("DFUOnBoot") = "default"
            paramList("UploadMode") = "default"
            paramList("FlashSize") = "4M"
            paramList("LoopCore") = "1"
            paramList("EventsCore") = "1"
            paramList("EraseFlash") = "none"
            paramList("JTAGAdapter") = "default"
        ElseIf boardId.Contains("esp32s2") Then
            ' ESP32-S2 specific parameters
            paramList("USBMode") = "hwcdc"
            paramList("CDCOnBoot") = "default"
            paramList("MSCOnBoot") = "default"
            paramList("DFUOnBoot") = "default"
            paramList("UploadMode") = "default"
        End If

        ' Build parameter string
        Dim paramStrings As New List(Of String)
        For Each kvp In paramList
            paramStrings.Add($"{kvp.Key}={kvp.Value}")
        Next

        Dim paramStr = String.Join(",", paramStrings)
        Dim fqbn = $"{vendor}:{architecture}:{boardId}:{paramStr}"

        ' Add to configurations
        boardConfigurations(boardName) = fqbn

        Debug.WriteLine($"[2025-08-13 12:33:59] Added board: {boardName}, FQBN: {fqbn}")
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
        boardConfigurations("ESP32 Wrover Module") = "esp32:esp32:esp32wrover:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80"
        boardConfigurations("ESP32 Wrover Kit") = "esp32:esp32:esp32wrover:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80"
        boardConfigurations("ESP32 PICO-D4") = "esp32:esp32:pico32:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80"

        ' ESP32-S2/S3 and newer boards - no FlashFreq parameter
        boardConfigurations("ESP32-S2 Dev Module") = "esp32:esp32:esp32s2:PartitionScheme=default,CPUFreq=240,FlashMode=dio,USBMode=hwcdc"
        boardConfigurations("ESP32-S3 Dev Module") = "esp32:esp32:esp32s3:PartitionScheme=default,CPUFreq=240,FlashMode=dio,USBMode=hwcdc"
        boardConfigurations("ESP32-C2 Dev Module") = "esp32:esp32:esp32c2:PartitionScheme=default,CPUFreq=120,FlashMode=dio"
        boardConfigurations("ESP32-C3 Dev Module") = "esp32:esp32:esp32c3:PartitionScheme=default,CPUFreq=160,FlashMode=dio"
        boardConfigurations("ESP32-C6 Dev Module") = "esp32:esp32:esp32c6:PartitionScheme=default,CPUFreq=160,FlashMode=dio"
        boardConfigurations("ESP32-H2 Dev Module") = "esp32:esp32:esp32h2:PartitionScheme=default,CPUFreq=96,FlashMode=dio"
        boardConfigurations("ESP32-C5 Dev Module") = "esp32:esp32:esp32c5:PartitionScheme=default,CPUFreq=240,FlashMode=dio"
        boardConfigurations("ESP32-P4 Dev Module") = "esp32:esp32:esp32p4:PartitionScheme=default,CPUFreq=240,FlashMode=dio"

        ' Initialize menu options dictionaries for standard boards
        boardMenuOptions("ESP32 Dev Module") = CreateDefaultMenuOptions()
        boardMenuOptions("ESP32 Wrover Module") = CreateDefaultMenuOptions()
        boardMenuOptions("ESP32 Wrover Kit") = CreateDefaultMenuOptions()
        boardMenuOptions("ESP32 PICO-D4") = CreateDefaultMenuOptions()

        ' Special menu options for S2/S3 boards
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
        boardParameters("ESP32 Wrover Module") = CreateDefaultBoardParameters()
        boardParameters("ESP32 Wrover Kit") = CreateDefaultBoardParameters()
        boardParameters("ESP32 PICO-D4") = CreateDefaultBoardParameters()

        ' Special parameters for S2/S3 boards
        boardParameters("ESP32-S2 Dev Module") = CreateS2BoardParameters()
        boardParameters("ESP32-S3 Dev Module") = CreateS3BoardParameters()
        boardParameters("ESP32-C2 Dev Module") = CreateC2BoardParameters()
        boardParameters("ESP32-C3 Dev Module") = CreateC3BoardParameters()
        boardParameters("ESP32-C6 Dev Module") = CreateC6BoardParameters()
        boardParameters("ESP32-H2 Dev Module") = CreateH2BoardParameters()
        boardParameters("ESP32-C5 Dev Module") = CreateC5BoardParameters()
        boardParameters("ESP32-P4 Dev Module") = CreateP4BoardParameters()
    End Sub

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

        Dim eraseFlashOptions As New Dictionary(Of String, String)
        eraseFlashOptions.Add("none", "None")
        eraseFlashOptions.Add("all", "All")
        menuOptions.Add("EraseFlash", eraseFlashOptions)

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

        ' Default values
        parameters("menu.PartitionScheme.default") = "Default"
        parameters("menu.PartitionScheme.min_spiffs") = "Minimal SPIFFS"
        parameters("menu.PartitionScheme.min_ota") = "Minimal OTA"
        parameters("menu.PartitionScheme.huge_app") = "Huge APP"
        parameters("menu.PartitionScheme.no_ota") = "No OTA"
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

        ' Add S2-specific parameters
        parameters("menu.USBMode") = "USB Mode"
        parameters("menu.USBMode.hwcdc") = "Hardware CDC"
        parameters("menu.USBMode.default") = "Default"

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

        parameters("menu.EraseFlash") = "Erase Flash"
        parameters("menu.EraseFlash.none") = "None"
        parameters("menu.EraseFlash.all") = "All"

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

    Public Function GetMenuOptions(boardName As String) As Dictionary(Of String, Dictionary(Of String, String))
        ' Return menu options for the given board
        If boardMenuOptions.ContainsKey(boardName) Then
            Return boardMenuOptions(boardName)
        Else
            ' Return default menu options if board not found
            Return CreateDefaultMenuOptions()
        End If
    End Function

    Public Function GetParameterOptions(boardName As String, paramName As String) As Dictionary(Of String, String)
        Dim result As New Dictionary(Of String, String)()

        ' Check if this is an S2/S3/C3 board and if trying to access FlashFreq
        If paramName = "FlashFreq" Then
            Dim boardId = GetBoardId(boardName)
            If boardId.Contains("esp32s2") OrElse boardId.Contains("esp32s3") OrElse
               boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c2") OrElse
               boardId.Contains("esp32c6") OrElse boardId.Contains("esp32h2") OrElse
               boardId.Contains("esp32c5") OrElse boardId.Contains("esp32p4") Then
                ' These boards don't support FlashFreq option
                Return result
            End If
        End If

        ' First check if we have menu options for this board and parameter
        If boardMenuOptions.ContainsKey(boardName) AndAlso boardMenuOptions(boardName).ContainsKey(paramName) Then
            ' Return the menu options for this parameter
            Return New Dictionary(Of String, String)(boardMenuOptions(boardName)(paramName))
        End If

        ' Fall back to looking in board parameters
        If boardParameters.ContainsKey(boardName) Then
            Dim parameters = boardParameters(boardName)
            Dim prefix = $"menu.{paramName}."

            For Each key In parameters.Keys
                If key.StartsWith(prefix) Then
                    Dim value = key.Substring(prefix.Length)
                    Dim displayName = parameters(key)
                    result(value) = displayName
                End If
            Next
        End If

        ' Add default options if none found
        If result.Count = 0 Then
            Select Case paramName
                Case "CPUFreq"
                    ' Check board type for appropriate CPU frequencies
                    Dim boardId = GetBoardId(boardName)
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
                    ' Only add if this is a compatible board
                    Dim boardId = GetBoardId(boardName)
                    If Not boardId.Contains("esp32s2") AndAlso Not boardId.Contains("esp32s3") AndAlso
                       Not boardId.Contains("esp32c3") AndAlso Not boardId.Contains("esp32c2") AndAlso
                       Not boardId.Contains("esp32c6") AndAlso Not boardId.Contains("esp32h2") AndAlso
                       Not boardId.Contains("esp32c5") AndAlso Not boardId.Contains("esp32p4") Then
                        result.Add("80", "80MHz")
                        result.Add("40", "40MHz")
                        result.Add("20", "20MHz")
                    End If
                Case "PartitionScheme"
                    result.Add("default", "Default")
                    result.Add("min_spiffs", "Minimal SPIFFS")
                    result.Add("min_ota", "Minimal OTA")
                    result.Add("huge_app", "Huge APP")
                    result.Add("no_ota", "No OTA")
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
                Case "EraseFlash"
                    result.Add("none", "None")
                    result.Add("all", "All")
                Case "JTAGAdapter"
                    result.Add("default", "Default")
                    result.Add("custom", "Custom")
            End Select
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
                Debug.WriteLine($"[2025-08-13 12:33:59] Custom partition file copied to {destFile}")
            End If
        Catch ex As Exception
            ' Log error but continue
            Debug.WriteLine($"[2025-08-13 12:33:59] Error copying custom partition file: {ex.Message}")
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

    Public Sub UpdateBoardConfiguration(boardName As String, parameters As Dictionary(Of String, String))
        If boardConfigurations.ContainsKey(boardName) Then
            Dim fqbn = boardConfigurations(boardName)
            Dim boardId = GetBoardId(boardName)

            ' Parse the FQBN into parts
            Dim parts = fqbn.Split(New Char() {":"c})

            ' Check if we have enough parts for a valid FQBN
            If parts.Length >= 3 Then
                ' Extract vendor, architecture, board ID
                Dim vendor = parts(0)
                Dim architecture = parts(1)

                ' Check if this is an S2/S3/C3 board and remove FlashFreq if present
                Dim isNewBoard = boardId.Contains("esp32s2") OrElse boardId.Contains("esp32s3") OrElse
                                boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c2") OrElse
                                boardId.Contains("esp32c6") OrElse boardId.Contains("esp32h2") OrElse
                                boardId.Contains("esp32c5") OrElse boardId.Contains("esp32p4")

                If isNewBoard AndAlso parameters.ContainsKey("FlashFreq") Then
                    parameters.Remove("FlashFreq")
                End If

                ' Build parameter string
                Dim paramList As New List(Of String)

                ' Add partition scheme first if available
                If parameters.ContainsKey("PartitionScheme") Then
                    paramList.Add($"PartitionScheme={parameters("PartitionScheme")}")
                End If

                ' Add remaining parameters
                For Each kvp In parameters
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

                    If (kvp.Key = "PSRAM" AndAlso kvp.Value = "disabled") Then
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
                Debug.WriteLine($"[2025-08-13 12:33:59] Updated board configuration for {boardName}: {newFqbn}")
                Debug.WriteLine($"[2025-08-13 12:33:59] Configuration updated by Chamil1983")
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

            ' Check if this is an S2/S3/C3 board and remove FlashFreq if present
            Dim isNewBoard = boardId.Contains("esp32s2") OrElse boardId.Contains("esp32s3") OrElse
                            boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c2") OrElse
                            boardId.Contains("esp32c6") OrElse boardId.Contains("esp32h2") OrElse
                            boardId.Contains("esp32c5") OrElse boardId.Contains("esp32p4")

            If isNewBoard AndAlso parameters.ContainsKey("FlashFreq") Then
                parameters.Remove("FlashFreq")
            End If

            ' Build parameter string
            Dim paramList As New List(Of String)

            ' Add partition scheme first if available
            If parameters.ContainsKey("PartitionScheme") Then
                paramList.Add($"PartitionScheme={parameters("PartitionScheme")}")
            End If

            ' Add remaining parameters
            For Each kvp In parameters
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

                If (kvp.Key = "PSRAM" AndAlso kvp.Value = "disabled") Then
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

            ' Log the addition
            Debug.WriteLine($"[2025-08-13 12:38:37] Added new board configuration for {boardName}: {newFqbn}")
            Debug.WriteLine($"[2025-08-13 12:38:37] Configuration created by Chamil1983")
        End If
    End Sub

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
                Debug.WriteLine($"[2025-08-13 12:38:37] Error reading partition files: {ex.Message}")
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

        Return partitionsList.Distinct().ToList()
    End Function

    ' Extract all parameters from FQBN
    Public Function ExtractParametersFromFQBN(fqbn As String) As Dictionary(Of String, String)
        Dim parameters As New Dictionary(Of String, String)()

        ' Default values
        parameters("CPUFreq") = "240"
        parameters("FlashMode") = "dio"
        parameters("PartitionScheme") = "default"
        parameters("UploadSpeed") = "921600"
        parameters("DebugLevel") = "none"
        parameters("PSRAM") = "disabled"

        ' Only set FlashFreq for original ESP32 boards
        If Not fqbn.Contains("esp32s2") AndAlso Not fqbn.Contains("esp32s3") AndAlso
           Not fqbn.Contains("esp32c3") AndAlso Not fqbn.Contains("esp32c2") AndAlso
           Not fqbn.Contains("esp32c6") AndAlso Not fqbn.Contains("esp32h2") AndAlso
           Not fqbn.Contains("esp32c5") AndAlso Not fqbn.Contains("esp32p4") Then
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
                            parameters(keyValue(0)) = keyValue(1)
                        End If
                    End If
                Next
            End If
        End If

        ' Add additional parameters for ESP32-S3 if needed
        If fqbn.Contains("esp32s3") Then
            If Not parameters.ContainsKey("USBMode") Then parameters("USBMode") = "hwcdc"
            If Not parameters.ContainsKey("CDCOnBoot") Then parameters("CDCOnBoot") = "default"
            If Not parameters.ContainsKey("MSCOnBoot") Then parameters("MSCOnBoot") = "default"
            If Not parameters.ContainsKey("DFUOnBoot") Then parameters("DFUOnBoot") = "default"
            If Not parameters.ContainsKey("UploadMode") Then parameters("UploadMode") = "default"
            If Not parameters.ContainsKey("FlashSize") Then parameters("FlashSize") = "4M"
            If Not parameters.ContainsKey("LoopCore") Then parameters("LoopCore") = "1"
            If Not parameters.ContainsKey("EventsCore") Then parameters("EventsCore") = "1"
            If Not parameters.ContainsKey("EraseFlash") Then parameters("EraseFlash") = "none"
            If Not parameters.ContainsKey("JTAGAdapter") Then parameters("JTAGAdapter") = "default"
        ElseIf fqbn.Contains("esp32s2") Then
            If Not parameters.ContainsKey("USBMode") Then parameters("USBMode") = "hwcdc"
            If Not parameters.ContainsKey("CDCOnBoot") Then parameters("CDCOnBoot") = "default"
            If Not parameters.ContainsKey("MSCOnBoot") Then parameters("MSCOnBoot") = "default"
            If Not parameters.ContainsKey("DFUOnBoot") Then parameters("DFUOnBoot") = "default"
            If Not parameters.ContainsKey("UploadMode") Then parameters("UploadMode") = "default"
        End If

        Return parameters
    End Function

    ' Get all available configuration options for a board with user-friendly values
    Public Function GetAllBoardConfigOptions(boardName As String) As Dictionary(Of String, List(Of KeyValuePair(Of String, String)))
        Dim allOptions As New Dictionary(Of String, List(Of KeyValuePair(Of String, String)))

        ' Standard configuration categories
        Dim standardCategories As String() = {"CPUFreq", "FlashMode", "FlashFreq", "PartitionScheme", "UploadSpeed", "DebugLevel", "PSRAM"}

        ' Get menu options for the board
        If boardMenuOptions.ContainsKey(boardName) Then
            For Each category In standardCategories
                ' Skip FlashFreq for S2/S3/C3 boards
                If category = "FlashFreq" Then
                    Dim boardId = GetBoardId(boardName)
                    If boardId.Contains("esp32s2") OrElse boardId.Contains("esp32s3") OrElse
                       boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c2") OrElse
                       boardId.Contains("esp32c6") OrElse boardId.Contains("esp32h2") OrElse
                       boardId.Contains("esp32c5") OrElse boardId.Contains("esp32p4") Then
                        Continue For
                    End If
                End If

                If boardMenuOptions(boardName).ContainsKey(category) Then
                    Dim options As New List(Of KeyValuePair(Of String, String))

                    ' Convert dictionary to sorted list of KeyValuePairs
                    For Each kvp In boardMenuOptions(boardName)(category)
                        options.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
                    Next

                    ' Add to result dictionary
                    allOptions(category) = options
                End If
            Next

            ' Add any board-specific categories not in the standard list
            For Each category In boardMenuOptions(boardName).Keys
                If Not standardCategories.Contains(category) AndAlso Not allOptions.ContainsKey(category) Then
                    Dim options As New List(Of KeyValuePair(Of String, String))

                    ' Convert dictionary to sorted list of KeyValuePairs
                    For Each kvp In boardMenuOptions(boardName)(category)
                        options.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
                    Next

                    ' Add to result dictionary
                    allOptions(category) = options
                End If
            Next
        End If

        ' If options are still missing, add defaults
        For Each category In standardCategories
            ' Skip FlashFreq for S2/S3/C3 boards
            If category = "FlashFreq" Then
                Dim boardId = GetBoardId(boardName)
                If boardId.Contains("esp32s2") OrElse boardId.Contains("esp32s3") OrElse
                   boardId.Contains("esp32c3") OrElse boardId.Contains("esp32c2") OrElse
                   boardId.Contains("esp32c6") OrElse boardId.Contains("esp32h2") OrElse
                   boardId.Contains("esp32c5") OrElse boardId.Contains("esp32p4") Then
                    Continue For
                End If
            End If

            If Not allOptions.ContainsKey(category) Then
                Dim defaultOptions = GetParameterOptions(boardName, category)
                Dim options As New List(Of KeyValuePair(Of String, String))

                ' Convert dictionary to sorted list of KeyValuePairs
                For Each kvp In defaultOptions
                    options.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
                Next

                ' Add to result dictionary if we have options
                If options.Count > 0 Then
                    allOptions(category) = options
                End If
            End If
        Next

        ' Add ESP32-S2/S3 specific options if needed
        Dim boardType = GetBoardId(boardName)
        If boardType.Contains("esp32s2") Then
            AddS2SpecificOptions(allOptions, boardName)
        ElseIf boardType.Contains("esp32s3") Then
            AddS3SpecificOptions(allOptions, boardName)
        End If

        Return allOptions
    End Function

    ' Add ESP32-S2 specific options
    Private Sub AddS2SpecificOptions(allOptions As Dictionary(Of String, List(Of KeyValuePair(Of String, String))), boardName As String)
        Dim s2Categories As String() = {"USBMode", "CDCOnBoot", "MSCOnBoot", "DFUOnBoot", "UploadMode"}

        For Each category In s2Categories
            If Not allOptions.ContainsKey(category) Then
                Dim options = GetParameterOptions(boardName, category)
                Dim optionsList As New List(Of KeyValuePair(Of String, String))

                For Each kvp In options
                    optionsList.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
                Next

                If optionsList.Count > 0 Then
                    allOptions(category) = optionsList
                End If
            End If
        Next
    End Sub

    ' Add ESP32-S3 specific options
    Private Sub AddS3SpecificOptions(allOptions As Dictionary(Of String, List(Of KeyValuePair(Of String, String))), boardName As String)
        ' First add all S2 options
        AddS2SpecificOptions(allOptions, boardName)

        ' Then add S3-specific options
        Dim s3Categories As String() = {"FlashSize", "LoopCore", "EventsCore", "EraseFlash", "JTAGAdapter"}

        For Each category In s3Categories
            If Not allOptions.ContainsKey(category) Then
                Dim options = GetParameterOptions(boardName, category)
                Dim optionsList As New List(Of KeyValuePair(Of String, String))

                For Each kvp In options
                    optionsList.Add(New KeyValuePair(Of String, String)(kvp.Key, kvp.Value))
                Next

                If optionsList.Count > 0 Then
                    allOptions(category) = optionsList
                End If
            End If
        Next
    End Sub

    ' Helper class to sort version directories
    Private Class VersionComparer
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
End Class