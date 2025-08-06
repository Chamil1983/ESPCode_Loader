Imports System.IO
Imports System.Text.RegularExpressions

Public Class BoardManager
    ' Board configurations
    Private ReadOnly boards As New Dictionary(Of String, Dictionary(Of String, String))
    ' Partition schemes (global and board-specific)
    Private ReadOnly partitionSchemes As New Dictionary(Of String, List(Of String))
    ' Partition details (for displaying info about each partition)
    Private ReadOnly partitionDetails As New Dictionary(Of String, String)
    ' Board configuration options (CPU freq, flash freq, etc.)
    Private ReadOnly boardOptions As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))
    ' Board option details and descriptions
    Private ReadOnly boardOptionDetails As New Dictionary(Of String, Dictionary(Of String, String))
    ' Default values for board options
    Private ReadOnly boardOptionDefaults As New Dictionary(Of String, Dictionary(Of String, String))
    ' Actual values that Arduino IDE resolves defaults to
    Private ReadOnly boardOptionActualValues As New Dictionary(Of String, Dictionary(Of String, String))
    ' Flag and path for boards.txt if being used
    Private _boardsTxtPath As String = ""
    ' Custom partition CSV file path if loaded
    Private _customPartitionPath As String = ""

    ' Public property for boards.txt path
    Public ReadOnly Property BoardsTxtPath As String
        Get
            Return _boardsTxtPath
        End Get
    End Property

    ' Public flag for whether a custom partition file is loaded
    Public ReadOnly Property HasCustomPartition As Boolean
        Get
            Return Not String.IsNullOrEmpty(_customPartitionPath) AndAlso File.Exists(_customPartitionPath)
        End Get
    End Property

    Public Sub New()
        ' Initialize default boards
        InitDefaultBoards()
    End Sub

    ' Check if boards.txt is being used
    Public Function IsUsingBoardsTxt() As Boolean
        Return Not String.IsNullOrEmpty(_boardsTxtPath) AndAlso File.Exists(_boardsTxtPath)
    End Function

    ' Add a board with its FQBN pattern
    Public Sub AddBoard(name As String, fqbnPattern As String)
        Dim boardConfig = New Dictionary(Of String, String)
        boardConfig.Add("fqbnPattern", fqbnPattern)
        boards(name) = boardConfig
    End Sub

    ' Add partition option for a specific board
    Public Sub AddPartitionOption(boardName As String, partitionSchemeName As String)
        If Not partitionSchemes.ContainsKey(boardName) Then
            partitionSchemes(boardName) = New List(Of String)()
        End If
        If Not partitionSchemes(boardName).Contains(partitionSchemeName) Then
            partitionSchemes(boardName).Add(partitionSchemeName)
        End If
    End Sub

    ' Add a partition detail entry
    Public Sub AddPartitionDetail(partitionSchemeName As String, details As String)
        partitionDetails(partitionSchemeName) = details
    End Sub

    ' Get the details for a specific partition
    Public Function GetPartitionDetails(partitionSchemeName As String) As String
        If partitionDetails.ContainsKey(partitionSchemeName) Then
            Return partitionDetails(partitionSchemeName)
        End If
        Return ""
    End Function

    ' Initialize default boards and partitions
    Private Sub InitDefaultBoards()
        ' Add ESP32 boards with exact FQBN patterns from Arduino IDE
        AddBoard("ESP32 Dev Module", "esp32:esp32:esp32")
        AddBoard("ESP32-S2", "esp32:esp32:esp32s2")
        AddBoard("ESP32-C3", "esp32:esp32:esp32c3")
        AddBoard("ESP32-S3", "esp32:esp32:esp32s3")
        AddBoard("ESP32 Wrover Kit", "esp32:esp32:esp32wrover")
        AddBoard("ESP32 Pico Kit", "esp32:esp32:pico32")
        AddBoard("ESP32-C6", "esp32:esp32:esp32c6")
        AddBoard("ESP32-H2", "esp32:esp32:esp32h2")

        ' Add default partition schemes for each board
        For Each boardName In boards.Keys
            AddPartitionOption(boardName, "default")
            AddPartitionOption(boardName, "huge_app")
            AddPartitionOption(boardName, "min_spiffs")
            AddPartitionOption(boardName, "no_ota")
        Next

        ' Add partition details
        AddPartitionDetail("default", "Default with balanced app/SPIFFS (1.2MB App/1.5MB SPIFFS)")
        AddPartitionDetail("huge_app", "Huge App with minimal SPIFFS (3MB App/190KB SPIFFS)")
        AddPartitionDetail("min_spiffs", "Minimal SPIFFS (1.9MB APP/190KB SPIFFS)")
        AddPartitionDetail("no_ota", "No OTA (2MB APP/2MB SPIFFS)")

        ' Check for saved custom partition
        LoadCustomPartitions()

        ' Add board-specific options based on ACTUAL Arduino IDE configurations
        AddBoardSpecificOptions()
        AddOptionDetails()
    End Sub

    ' Add board-specific options based on actual Arduino IDE configurations
    Private Sub AddBoardSpecificOptions()
        ' ESP32 Dev Module - Full featured ESP32 with all options
        AddESP32DevModuleOptions()

        ' ESP32-S2 - Single core with CORRECTED options
        AddESP32S2Options()

        ' ESP32-S3 - Dual core with USB support
        AddESP32S3Options()

        ' ESP32-C3 - Single core RISC-V
        AddESP32C3Options()

        ' ESP32 Wrover Kit - CORRECTED OPTIONS based on actual boards.txt
        AddESP32WroverOptions()

        ' ESP32 Pico Kit - VERY LIMITED OPTIONS as specified
        AddESP32PicoOptions()

        ' ESP32-C6 - Similar to C3
        AddESP32C6Options()

        ' ESP32-H2 - CORRECTED Zigbee/Thread with specific options
        AddESP32H2Options()
    End Sub

    ' ESP32 Dev Module - Full featured (esp32:esp32:esp32)
    Private Sub AddESP32DevModuleOptions()
        Dim esp32Options As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency
        Dim cpuFreq As New Dictionary(Of String, String)
        cpuFreq.Add("240", "240MHz (WiFi/BT)")
        cpuFreq.Add("160", "160MHz")
        cpuFreq.Add("80", "80MHz")
        esp32Options.Add("CPUFreq", cpuFreq)

        ' Flash Frequency
        Dim flashFreq As New Dictionary(Of String, String)
        flashFreq.Add("80", "80MHz")
        flashFreq.Add("40", "40MHz")
        esp32Options.Add("FlashFreq", flashFreq)

        ' Flash Mode
        Dim flashMode As New Dictionary(Of String, String)
        flashMode.Add("qio", "QIO")
        flashMode.Add("dio", "DIO")
        flashMode.Add("qout", "QOUT")
        flashMode.Add("dout", "DOUT")
        esp32Options.Add("FlashMode", flashMode)

        ' Flash Size
        Dim flashSize As New Dictionary(Of String, String)
        flashSize.Add("4M", "4MB (32Mb)")
        flashSize.Add("8M", "8MB (64Mb)")
        flashSize.Add("2M", "2MB (16Mb)")
        flashSize.Add("16M", "16MB (128Mb)")
        esp32Options.Add("FlashSize", flashSize)

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        uploadSpeed.Add("460800", "460800")
        uploadSpeed.Add("512000", "512000")
        esp32Options.Add("UploadSpeed", uploadSpeed)

        ' Core Debug Level
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        debugLevel.Add("verbose", "Verbose")
        esp32Options.Add("DebugLevel", debugLevel)

        ' PSRAM
        Dim psram As New Dictionary(Of String, String)
        psram.Add("disabled", "Disabled")
        psram.Add("enabled", "Enabled")
        esp32Options.Add("PSRAM", psram)

        ' Erase All Flash
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        esp32Options.Add("EraseFlash", eraseFlash)

        ' Events Run On Core
        Dim eventsCore As New Dictionary(Of String, String)
        eventsCore.Add("1", "Core 1")
        eventsCore.Add("0", "Core 0")
        esp32Options.Add("EventsCore", eventsCore)

        ' Arduino Runs On Core
        Dim loopCore As New Dictionary(Of String, String)
        loopCore.Add("1", "Core 1")
        loopCore.Add("0", "Core 0")
        esp32Options.Add("LoopCore", loopCore)

        boardOptions.Add("ESP32 Dev Module", esp32Options)

        ' Set defaults
        Dim esp32Defaults As New Dictionary(Of String, String)
        esp32Defaults.Add("CPUFreq", "240")
        esp32Defaults.Add("FlashFreq", "80")
        esp32Defaults.Add("FlashMode", "qio")
        esp32Defaults.Add("FlashSize", "4M")
        esp32Defaults.Add("UploadSpeed", "921600")
        esp32Defaults.Add("DebugLevel", "none")
        esp32Defaults.Add("PSRAM", "disabled")
        esp32Defaults.Add("EraseFlash", "none")
        esp32Defaults.Add("EventsCore", "1")
        esp32Defaults.Add("LoopCore", "1")
        boardOptionDefaults.Add("ESP32 Dev Module", esp32Defaults)
        boardOptionActualValues.Add("ESP32 Dev Module", esp32Defaults.ToDictionary(Function(x) x.Key, Function(x) x.Value))
    End Sub

    ' ESP32-S2 - Single core - CORRECTED based on actual Arduino IDE boards.txt
    ' --------------------------------------------------------------------
    ' ❶  ESP32-S2  – bring menu in line with boards.txt (Arduino IDE)
    ' --------------------------------------------------------------------
    Private Sub AddESP32S2Options()
        Dim s2 As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency ---------------------------------------------------
        s2.Add("CPUFreq", New Dictionary(Of String, String) From {
        {"240", "240 MHz"},
        {"160", "160 MHz"},
        {"80", "80 MHz"},
        {"40", "40 MHz"},
        {"20", "20 MHz"},
        {"10", "10 MHz"}
    })

        ' Flash Frequency -------------------------------------------------
        s2.Add("FlashFreq", New Dictionary(Of String, String) From {
        {"80", "80 MHz"},
        {"40", "40 MHz"}
    })

        ' Flash Mode ------------------------------------------------------
        s2.Add("FlashMode", New Dictionary(Of String, String) From {
        {"qio", "QIO"},
        {"dio", "DIO"},
        {"qout", "QOUT"},
        {"dout", "DOUT"}
    })

        ' Flash Size ------------------------------------------------------
        s2.Add("FlashSize", New Dictionary(Of String, String) From {
        {"2M", "2 MB (16 Mb)"},
        {"4M", "4 MB (32 Mb)"},
        {"8M", "8 MB (64 Mb)"},
        {"16M", "16 MB (128 Mb)"}
    })

        ' Upload Speed ----------------------------------------------------
        s2.Add("UploadSpeed", New Dictionary(Of String, String) From {
        {"921600", "921 600"},
        {"460800", "460 800"},
        {"230400", "230 400"},
        {"115200", "115 200"}
    })

        ' Core Debug Level ------------------------------------------------
        s2.Add("DebugLevel", New Dictionary(Of String, String) From {
        {"none", "None"},
        {"error", "Error"},
        {"warn", "Warning"},
        {"info", "Info"},
        {"debug", "Debug"},
        {"verbose", "Verbose"}
    })

        ' PSRAM -----------------------------------------------------------
        s2.Add("PSRAM", New Dictionary(Of String, String) From {
        {"disabled", "Disabled"},
        {"enabled", "Enabled"}
    })

        ' Erase All Flash -------------------------------------------------
        s2.Add("EraseFlash", New Dictionary(Of String, String) From {
        {"none", "Disabled"},
        {"all", "Enabled"}
    })

        ' USB CDC On Boot -------------------------------------------------
        s2.Add("USBCDCOnBoot", New Dictionary(Of String, String) From {
        {"default", "Default"},
        {"enabled", "Enabled"}
    })

        ' USB DFU On Boot -------------------------------------------------
        s2.Add("USBDFUOnBoot", New Dictionary(Of String, String) From {
        {"default", "Default"},
        {"enabled", "Enabled"}
    })

        ' USB MSC Firmware On Boot ---------------------------------------
        s2.Add("USBMSCOnBoot", New Dictionary(Of String, String) From {
        {"default", "Default"},
        {"enabled", "Enabled"}
    })

        ' JTAG Adapter ----------------------------------------------------
        s2.Add("JTAGAdapter", New Dictionary(Of String, String) From {
        {"default", "Disabled"},
        {"external", "FTDI Adapter"},
        {"bridge", "ESP-USB Bridge"}
    })

        ' Upload Mode -----------------------------------------------------
        s2.Add("UploadMode", New Dictionary(Of String, String) From {
        {"default", "UART0"},
        {"dfu", "Internal USB"}
    })

        ' Zigbee Mode -----------------------------------------------------
        s2.Add("ZigbeeMode", New Dictionary(Of String, String) From {
        {"default", "Disabled"},
        {"zczr", "Zigbee (Coordinator/Router)"}
    })

        boardOptions("ESP32-S2") = s2

        ' ---------- Defaults exactly as Arduino IDE resolves them --------
        boardOptionDefaults("ESP32-S2") = New Dictionary(Of String, String) From {
        {"CPUFreq", "240"},
        {"FlashFreq", "80"},
        {"FlashMode", "qio"},
        {"FlashSize", "4M"},
        {"UploadSpeed", "921600"},
        {"DebugLevel", "none"},
        {"PSRAM", "disabled"},
        {"EraseFlash", "none"},
        {"USBCDCOnBoot", "default"},
        {"USBDFUOnBoot", "default"},
        {"USBMSCOnBoot", "default"},
        {"JTAGAdapter", "default"},
        {"UploadMode", "default"},
        {"ZigbeeMode", "default"}
    }

        ' The IDE’s “actual” value mapping is 1-to-1 here
        boardOptionActualValues("ESP32-S2") =
        boardOptionDefaults("ESP32-S2").ToDictionary(Function(k) k.Key,
                                                     Function(k) k.Value)
    End Sub
    ' --------------------------------------------------------------------

    ' ESP32-S3 - Dual core with USB
    Private Sub AddESP32S3Options()
        Dim esp32S3Options As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency
        Dim cpuFreq As New Dictionary(Of String, String)
        cpuFreq.Add("240", "240MHz (WiFi/BT)")
        cpuFreq.Add("160", "160MHz")
        cpuFreq.Add("80", "80MHz")
        cpuFreq.Add("20", "20MHz")
        cpuFreq.Add("10", "10MHz")
        esp32S3Options.Add("CPUFreq", cpuFreq)


        ' Flash Mode
        Dim flashMode As New Dictionary(Of String, String)
        flashMode.Add("qio", "QIO 80MHz")
        flashMode.Add("qio120", "QIO 120MHz")
        flashMode.Add("dio", "DIO 80MHz")
        flashMode.Add("opi", "OPI 80MHz")
        esp32S3Options.Add("FlashMode", flashMode)

        ' Flash Size
        Dim flashSize As New Dictionary(Of String, String)
        flashSize.Add("8M", "8MB (64Mb)")
        flashSize.Add("4M", "4MB (32Mb)")
        flashSize.Add("16M", "16MB (128Mb)")
        flashSize.Add("2M", "2MB (16Mb)")
        esp32S3Options.Add("FlashSize", flashSize)

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        uploadSpeed.Add("460800", "460800")
        esp32S3Options.Add("UploadSpeed", uploadSpeed)

        ' Core Debug Level
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        debugLevel.Add("verbose", "Verbose")
        esp32S3Options.Add("DebugLevel", debugLevel)

        ' CDC On Boot
        Dim cdcOnBoot As New Dictionary(Of String, String)
        cdcOnBoot.Add("default", "Disabled")
        cdcOnBoot.Add("cdc", "Enabled")
        esp32S3Options.Add("CDCOnBoot", cdcOnBoot)

        ' DFU On Boot
        Dim dfuOnBoot As New Dictionary(Of String, String)
        dfuOnBoot.Add("default", "Disabled")
        dfuOnBoot.Add("dfu", "Enabled (Requires USB-OTG Mode)")
        esp32S3Options.Add("DFUOnBoot", dfuOnBoot)

        ' USB Firmware MSC On Boot
        Dim mscOnBoot As New Dictionary(Of String, String)
        mscOnBoot.Add("default", "Disabled")
        mscOnBoot.Add("msc", "Enabled (Requires USB-OTG Mode)")
        esp32S3Options.Add("MSCOnBoot", mscOnBoot)

        ' PSRAM
        Dim psram As New Dictionary(Of String, String)
        psram.Add("disabled", "Disabled")
        psram.Add("enabled", "QSPI PSRAM")
        psram.Add("opi", "OPI PSRAM")
        esp32S3Options.Add("PSRAM", psram)

        ' Upload Mode
        Dim upload As New Dictionary(Of String, String)
        upload.Add("default", "UART0 / Hardware CDC")
        upload.Add("cdc", "USB-OTG CDC (TinyUSB)")
        esp32S3Options.Add("UploadMode", upload)


        ' Erase Flash
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        esp32S3Options.Add("EraseFlash", eraseFlash)

        ' Events Run On Core
        Dim eventsCore As New Dictionary(Of String, String)
        eventsCore.Add("1", "Core 1")
        eventsCore.Add("0", "Core 0")
        esp32S3Options.Add("EventsCore", eventsCore)

        ' Arduino Runs On Core
        Dim loopCore As New Dictionary(Of String, String)
        loopCore.Add("1", "Core 1")
        loopCore.Add("0", "Core 0")
        esp32S3Options.Add("LoopCore", loopCore)

        ' USB Mode - ESP32-S3 uses USBMode
        Dim usbMode As New Dictionary(Of String, String)
        usbMode.Add("default", "USB-OTG (TinyUSB)")
        usbMode.Add("hwcdc", "Hardware CDC and JTAG")
        esp32S3Options.Add("USBMode", usbMode)

        ' JTAG Adapter
        Dim jtagAdapter As New Dictionary(Of String, String)
        jtagAdapter.Add("default", "Disabled")
        jtagAdapter.Add("builtin", "Integrated USB JTAG")
        jtagAdapter.Add("external", "FTDI Adapter")
        jtagAdapter.Add("bridge", "ESP USB Bridge")
        esp32S3Options.Add("JTAGAdapter", jtagAdapter)

        ' Zigbee Mode - CORRECTED values
        Dim zigbeeMode As New Dictionary(Of String, String)
        zigbeeMode.Add("default", "Disabled")
        zigbeeMode.Add("zczr", "Zigbee ZCZR (coordinator/router)")
        esp32S3Options.Add("ZigbeeMode", zigbeeMode)

        boardOptions.Add("ESP32-S3", esp32S3Options)

        ' Set defaults
        Dim esp32S3Defaults As New Dictionary(Of String, String)
        esp32S3Defaults.Add("CPUFreq", "240")
        esp32S3Defaults.Add("FlashMode", "qio")
        esp32S3Defaults.Add("FlashSize", "8M")
        esp32S3Defaults.Add("UploadSpeed", "921600")
        esp32S3Defaults.Add("DebugLevel", "none")
        esp32S3Defaults.Add("PSRAM", "disabled")
        esp32S3Defaults.Add("EraseFlash", "none")
        esp32S3Defaults.Add("EventsCore", "1")
        esp32S3Defaults.Add("LoopCore", "1")
        esp32S3Defaults.Add("ZigbeeMode", "hwcdc")
        esp32S3Defaults.Add("USBMode", "hwcdc")
        esp32S3Defaults.Add("UploadMode", "default")
        boardOptionDefaults.Add("ESP32-S3", esp32S3Defaults)
        boardOptionActualValues.Add("ESP32-S3", esp32S3Defaults.ToDictionary(Function(x) x.Key, Function(x) x.Value))
    End Sub

    ' ESP32-C3 - RISC-V single core
    Private Sub AddESP32C3Options()
        Dim esp32C3Options As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency
        Dim cpuFreq As New Dictionary(Of String, String)
        cpuFreq.Add("160", "160MHz")
        cpuFreq.Add("80", "80MHz")
        cpuFreq.Add("40", "40MHz")
        cpuFreq.Add("20", "20MHz")
        cpuFreq.Add("10", "10MHz")
        esp32C3Options.Add("CPUFreq", cpuFreq)

        ' Flash Frequency
        Dim flashFreq As New Dictionary(Of String, String)
        flashFreq.Add("80", "80MHz")
        flashFreq.Add("40", "40MHz")
        esp32C3Options.Add("FlashFreq", flashFreq)

        ' Flash Mode
        Dim flashMode As New Dictionary(Of String, String)
        flashMode.Add("qio", "QIO")
        flashMode.Add("dio", "DIO")
        esp32C3Options.Add("FlashMode", flashMode)

        ' Flash Size
        Dim flashSize As New Dictionary(Of String, String)
        flashSize.Add("4M", "4MB (32Mb)")
        flashSize.Add("2M", "2MB (16Mb)")
        flashSize.Add("8M", "8MB (64Mb)")
        flashSize.Add("16M", "16MB (128Mb)")
        esp32C3Options.Add("FlashSize", flashSize)

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        uploadSpeed.Add("460800", "460800")
        esp32C3Options.Add("UploadSpeed", uploadSpeed)

        ' Core Debug Level
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        debugLevel.Add("verbose", "Verbose")
        esp32C3Options.Add("DebugLevel", debugLevel)

        ' Erase Flash
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        esp32C3Options.Add("EraseFlash", eraseFlash)

        ' CDC On Boot
        Dim cdcOnBoot As New Dictionary(Of String, String)
        cdcOnBoot.Add("default", "Disabled")
        cdcOnBoot.Add("cdc", "Enabled")
        esp32C3Options.Add("CDCOnBoot", cdcOnBoot)


        ' JTAG Adapter
        Dim jtagAdapter As New Dictionary(Of String, String)
        jtagAdapter.Add("default", "Disabled")
        jtagAdapter.Add("builtin", "Integrated USB JTAG")
        jtagAdapter.Add("external", "FTDI Adapter")
        jtagAdapter.Add("bridge", "ESP USB Bridge")
        esp32C3Options.Add("JTAGAdapter", jtagAdapter)


        boardOptions.Add("ESP32-C3", esp32C3Options)

        ' Set defaults
        Dim esp32C3Defaults As New Dictionary(Of String, String)
        esp32C3Defaults.Add("CPUFreq", "160")
        esp32C3Defaults.Add("FlashFreq", "80")
        esp32C3Defaults.Add("FlashMode", "qio")
        esp32C3Defaults.Add("FlashSize", "4M")
        esp32C3Defaults.Add("UploadSpeed", "921600")
        esp32C3Defaults.Add("DebugLevel", "none")
        esp32C3Defaults.Add("EraseFlash", "none")
        esp32C3Defaults.Add("CDCOnBoot", "default")
        esp32C3Defaults.Add("JTAGAdapter", "default")
        boardOptionDefaults.Add("ESP32-C3", esp32C3Defaults)
        boardOptionActualValues.Add("ESP32-C3", esp32C3Defaults.ToDictionary(Function(x) x.Key, Function(x) x.Value))
    End Sub

    ' ESP32 Wrover Kit - CORRECTED based on actual boards.txt
    Private Sub AddESP32WroverOptions()
        Dim esp32WroverOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' Flash Frequency
        Dim flashFreq As New Dictionary(Of String, String)
        flashFreq.Add("80", "80MHz")
        flashFreq.Add("40", "40MHz")
        esp32WroverOptions.Add("FlashFreq", flashFreq)

        ' Flash Mode
        Dim flashMode As New Dictionary(Of String, String)
        flashMode.Add("qio", "QIO")
        flashMode.Add("dio", "DIO")
        esp32WroverOptions.Add("FlashMode", flashMode)

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        uploadSpeed.Add("460800", "460800")
        esp32WroverOptions.Add("UploadSpeed", uploadSpeed)

        ' Core Debug Level
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        debugLevel.Add("verbose", "Verbose")
        esp32WroverOptions.Add("DebugLevel", debugLevel)

        ' PSRAM
        Dim psram As New Dictionary(Of String, String)
        psram.Add("disabled", "Disabled")
        psram.Add("enabled", "Enabled")
        esp32WroverOptions.Add("PSRAM", psram)

        ' Erase Flash
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        esp32WroverOptions.Add("EraseFlash", eraseFlash)

        boardOptions.Add("ESP32 Wrover Kit", esp32WroverOptions)

        ' Set defaults
        Dim esp32WroverDefaults As New Dictionary(Of String, String)
        esp32WroverDefaults.Add("FlashFreq", "80")
        esp32WroverDefaults.Add("FlashMode", "qio")
        esp32WroverDefaults.Add("UploadSpeed", "921600")
        esp32WroverDefaults.Add("DebugLevel", "none")
        esp32WroverDefaults.Add("PSRAM", "enabled")
        esp32WroverDefaults.Add("EraseFlash", "none")
        boardOptionDefaults.Add("ESP32 Wrover Kit", esp32WroverDefaults)
        boardOptionActualValues.Add("ESP32 Wrover Kit", esp32WroverDefaults.ToDictionary(Function(x) x.Key, Function(x) x.Value))
    End Sub

    ' ESP32 Pico Kit - VERY LIMITED OPTIONS
    Private Sub AddESP32PicoOptions()
        Dim esp32PicoOptions As New Dictionary(Of String, Dictionary(Of String, String))

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        uploadSpeed.Add("460800", "460800")
        esp32PicoOptions.Add("UploadSpeed", uploadSpeed)

        ' Core Debug Level
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        debugLevel.Add("verbose", "Verbose")
        esp32PicoOptions.Add("DebugLevel", debugLevel)

        ' Erase Flash
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        esp32PicoOptions.Add("EraseFlash", eraseFlash)

        boardOptions.Add("ESP32 Pico Kit", esp32PicoOptions)

        ' Set defaults
        Dim esp32PicoDefaults As New Dictionary(Of String, String)
        esp32PicoDefaults.Add("UploadSpeed", "921600")
        esp32PicoDefaults.Add("DebugLevel", "none")
        esp32PicoDefaults.Add("EraseFlash", "none")
        boardOptionDefaults.Add("ESP32 Pico Kit", esp32PicoDefaults)
        boardOptionActualValues.Add("ESP32 Pico Kit", esp32PicoDefaults.ToDictionary(Function(x) x.Key, Function(x) x.Value))
    End Sub

    ' ESP32-C6 - Similar to C3
    Private Sub AddESP32C6Options()
        Dim esp32C6Options As New Dictionary(Of String, Dictionary(Of String, String))

        ' CPU Frequency
        Dim cpuFreq As New Dictionary(Of String, String)
        cpuFreq.Add("160", "160MHz")
        cpuFreq.Add("80", "80MHz")
        cpuFreq.Add("40", "40MHz")
        esp32C6Options.Add("CPUFreq", cpuFreq)

        ' Flash Frequency
        Dim flashFreq As New Dictionary(Of String, String)
        flashFreq.Add("80", "80MHz")
        flashFreq.Add("40", "40MHz")
        esp32C6Options.Add("FlashFreq", flashFreq)

        ' Flash Mode
        Dim flashMode As New Dictionary(Of String, String)
        flashMode.Add("qio", "QIO")
        flashMode.Add("dio", "DIO")
        flashMode.Add("qout", "QOUT")
        flashMode.Add("dout", "DOUT")
        esp32C6Options.Add("FlashMode", flashMode)

        ' Flash Size
        Dim flashSize As New Dictionary(Of String, String)
        flashSize.Add("4M", "4MB (32Mb)")
        flashSize.Add("8M", "8MB (64Mb)")
        flashSize.Add("2M", "2MB (16Mb)")
        flashSize.Add("16M", "16MB (128Mb)")
        esp32C6Options.Add("FlashSize", flashSize)

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        uploadSpeed.Add("460800", "460800")
        esp32C6Options.Add("UploadSpeed", uploadSpeed)

        ' Core Debug Level
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        esp32C6Options.Add("DebugLevel", debugLevel)

        ' Erase Flash
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        esp32C6Options.Add("EraseFlash", eraseFlash)

        boardOptions.Add("ESP32-C6", esp32C6Options)

        ' Set defaults
        Dim esp32C6Defaults As New Dictionary(Of String, String)
        esp32C6Defaults.Add("CPUFreq", "160")
        esp32C6Defaults.Add("FlashFreq", "80")
        esp32C6Defaults.Add("FlashMode", "qio")
        esp32C6Defaults.Add("FlashSize", "4M")
        esp32C6Defaults.Add("UploadSpeed", "921600")
        esp32C6Defaults.Add("DebugLevel", "none")
        esp32C6Defaults.Add("EraseFlash", "none")
        boardOptionDefaults.Add("ESP32-C6", esp32C6Defaults)
        boardOptionActualValues.Add("ESP32-C6", esp32C6Defaults.ToDictionary(Function(x) x.Key, Function(x) x.Value))
    End Sub

    ' ESP32-H2 - CORRECTED Zigbee/Thread options
    Private Sub AddESP32H2Options()
        Dim esp32H2Options As New Dictionary(Of String, Dictionary(Of String, String))

        ' Flash Frequency - 64MHz and 16MHz
        Dim flashFreq As New Dictionary(Of String, String)
        flashFreq.Add("64", "64MHz")
        flashFreq.Add("16", "16MHz")
        esp32H2Options.Add("FlashFreq", flashFreq)

        ' Flash Mode
        Dim flashMode As New Dictionary(Of String, String)
        flashMode.Add("qio", "QIO")
        flashMode.Add("dio", "DIO")
        esp32H2Options.Add("FlashMode", flashMode)

        ' Flash Size
        Dim flashSize As New Dictionary(Of String, String)
        flashSize.Add("4M", "4MB (32Mb)")
        flashSize.Add("8M", "8MB (64Mb)")
        flashSize.Add("2M", "2MB (16Mb)")
        flashSize.Add("16M", "16MB (128Mb)")
        esp32H2Options.Add("FlashSize", flashSize)

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
        esp32H2Options.Add("UploadSpeed", uploadSpeed)

        ' Core Debug Level
        Dim debugLevel As New Dictionary(Of String, String)
        debugLevel.Add("none", "None")
        debugLevel.Add("error", "Error")
        debugLevel.Add("warn", "Warning")
        debugLevel.Add("info", "Info")
        debugLevel.Add("debug", "Debug")
        esp32H2Options.Add("DebugLevel", debugLevel)

        ' Erase Flash
        Dim eraseFlash As New Dictionary(Of String, String)
        eraseFlash.Add("none", "Disabled")
        eraseFlash.Add("all", "Enabled")
        esp32H2Options.Add("EraseFlash", eraseFlash)

        ' USB CDC On Boot
        Dim usbCDC As New Dictionary(Of String, String)
        usbCDC.Add("default", "Default")
        usbCDC.Add("enabled", "Enabled")
        esp32H2Options.Add("USBCDCOnBoot", usbCDC)

        ' JTAG Adapter
        Dim jtagAdapter As New Dictionary(Of String, String)
        jtagAdapter.Add("default", "Disabled")
        jtagAdapter.Add("builtin", "Integrated USB JTAG")
        jtagAdapter.Add("external", "FTDI Adapter")
        jtagAdapter.Add("bridge", "ESP USB Bridge")
        esp32H2Options.Add("JTAGAdapter", jtagAdapter)

        ' Zigbee Mode - CORRECTED values
        Dim zigbeeMode As New Dictionary(Of String, String)
        zigbeeMode.Add("none", "Disabled")
        zigbeeMode.Add("ed", "Zigbee ED (end device)")
        zigbeeMode.Add("zczr", "Zigbee ZCZR (coordinator/router)")
        zigbeeMode.Add("ed_debug", "Zigbee ED (end device)-Debug")
        zigbeeMode.Add("zczr_debug", "Zigbee ZCZR (coordinator/router)-Debug")
        esp32H2Options.Add("ZigbeeMode", zigbeeMode)

        boardOptions.Add("ESP32-H2", esp32H2Options)

        ' Set defaults
        Dim esp32H2Defaults As New Dictionary(Of String, String)
        esp32H2Defaults.Add("FlashFreq", "64")
        esp32H2Defaults.Add("FlashMode", "qio")
        esp32H2Defaults.Add("FlashSize", "4M")
        esp32H2Defaults.Add("UploadSpeed", "921600")
        esp32H2Defaults.Add("DebugLevel", "none")
        esp32H2Defaults.Add("EraseFlash", "none")
        esp32H2Defaults.Add("USBCDCOnBoot", "default")
        esp32H2Defaults.Add("JTAGAdapter", "default")
        esp32H2Defaults.Add("ZigbeeMode", "none")
        boardOptionDefaults.Add("ESP32-H2", esp32H2Defaults)
        boardOptionActualValues.Add("ESP32-H2", esp32H2Defaults.ToDictionary(Function(x) x.Key, Function(x) x.Value))
    End Sub

    ' Add option descriptions and details
    Private Sub AddOptionDetails()
        Dim cpuFreqDetails As New Dictionary(Of String, String)
        cpuFreqDetails.Add("name", "CPU Frequency")
        cpuFreqDetails.Add("description", "The clock frequency for the ESP32 CPU")
        boardOptionDetails.Add("CPUFreq", cpuFreqDetails)

        Dim flashFreqDetails As New Dictionary(Of String, String)
        flashFreqDetails.Add("name", "Flash Frequency")
        flashFreqDetails.Add("description", "The frequency of the flash memory chip")
        boardOptionDetails.Add("FlashFreq", flashFreqDetails)

        Dim flashModeDetails As New Dictionary(Of String, String)
        flashModeDetails.Add("name", "Flash Mode")
        flashModeDetails.Add("description", "The communication protocol with the flash chip")
        boardOptionDetails.Add("FlashMode", flashModeDetails)

        Dim flashSizeDetails As New Dictionary(Of String, String)
        flashSizeDetails.Add("name", "Flash Size")
        flashSizeDetails.Add("description", "The size of the flash memory chip")
        boardOptionDetails.Add("FlashSize", flashSizeDetails)

        Dim uploadSpeedDetails As New Dictionary(Of String, String)
        uploadSpeedDetails.Add("name", "Upload Speed")
        uploadSpeedDetails.Add("description", "The serial speed for uploading code")
        boardOptionDetails.Add("UploadSpeed", uploadSpeedDetails)

        Dim debugLevelDetails As New Dictionary(Of String, String)
        debugLevelDetails.Add("name", "Core Debug Level")
        debugLevelDetails.Add("description", "The debug verbosity level")
        boardOptionDetails.Add("DebugLevel", debugLevelDetails)

        Dim psramDetails As New Dictionary(Of String, String)
        psramDetails.Add("name", "PSRAM")
        psramDetails.Add("description", "External SPI RAM support")
        boardOptionDetails.Add("PSRAM", psramDetails)

        Dim eraseFlashDetails As New Dictionary(Of String, String)
        eraseFlashDetails.Add("name", "Erase All Flash")
        eraseFlashDetails.Add("description", "Erase all flash before sketch upload")
        boardOptionDetails.Add("EraseFlash", eraseFlashDetails)

        Dim eventsCoreDetails As New Dictionary(Of String, String)
        eventsCoreDetails.Add("name", "Events Run On Core")
        eventsCoreDetails.Add("description", "Which core to run FreeRTOS events on")
        boardOptionDetails.Add("EventsCore", eventsCoreDetails)

        Dim loopCoreDetails As New Dictionary(Of String, String)
        loopCoreDetails.Add("name", "Arduino Runs On Core")
        loopCoreDetails.Add("description", "Which core to run Arduino setup/loop on")
        boardOptionDetails.Add("LoopCore", loopCoreDetails)

        Dim usbModeDetails As New Dictionary(Of String, String)
        usbModeDetails.Add("name", "USB Mode")
        usbModeDetails.Add("description", "USB stack configuration")
        boardOptionDetails.Add("USBMode", usbModeDetails)

        Dim cdcOnBootDetails As New Dictionary(Of String, String)
        cdcOnBootDetails.Add("name", "CDC On Boot")
        cdcOnBootDetails.Add("description", "Enable USB CDC on boot")
        boardOptionDetails.Add("CDCOnBoot", cdcOnBootDetails)

        Dim usbCDCDetails As New Dictionary(Of String, String)
        usbCDCDetails.Add("name", "USB CDC On Boot")
        usbCDCDetails.Add("description", "Enable USB CDC on boot")
        boardOptionDetails.Add("USBCDCOnBoot", usbCDCDetails)

        Dim jtagAdapterDetails As New Dictionary(Of String, String)
        jtagAdapterDetails.Add("name", "JTAG Adapter")
        jtagAdapterDetails.Add("description", "JTAG debugging adapter configuration")
        boardOptionDetails.Add("JTAGAdapter", jtagAdapterDetails)

        Dim zigbeeModeDetails As New Dictionary(Of String, String)
        zigbeeModeDetails.Add("name", "Zigbee Mode")
        zigbeeModeDetails.Add("description", "Zigbee protocol configuration")
        boardOptionDetails.Add("ZigbeeMode", zigbeeModeDetails)

        If Not boardOptionDetails.ContainsKey("USBDFUOnBoot") Then
            boardOptionDetails("USBDFUOnBoot") = New Dictionary(Of String, String) From {
            {"name", "USB DFU On Boot"},
            {"description", "Enable device-firmware-update interface at boot"}
        }
        End If

        If Not boardOptionDetails.ContainsKey("USBMSCOnBoot") Then
            boardOptionDetails("USBMSCOnBoot") = New Dictionary(Of String, String) From {
            {"name", "USB MSC On Boot"},
            {"description", "Expose firmware storage over USB mass-storage"}
        }
        End If

        If Not boardOptionDetails.ContainsKey("UploadMode") Then
            boardOptionDetails("UploadMode") = New Dictionary(Of String, String) From {
            {"name", "Upload Mode"},
            {"description", "Transport used for flashing the sketch"}
        }
        End If

        ' USBCDCOnBoot, JTAGAdapter, ZigbeeMode were already present
    End Sub

    ' Get option name with description
    Public Function GetOptionName(optionKey As String) As String
        If boardOptionDetails.ContainsKey(optionKey) Then
            Return boardOptionDetails(optionKey)("name")
        End If
        Return FormatOptionName(optionKey)
    End Function

    ' Get option description
    Public Function GetOptionDescription(optionKey As String) As String
        If boardOptionDetails.ContainsKey(optionKey) Then
            Return boardOptionDetails(optionKey)("description")
        End If
        Return ""
    End Function

    ' Get default value for an option for specific board
    Public Function GetOptionDefault(boardName As String, optionKey As String) As String
        If boardOptionDefaults.ContainsKey(boardName) AndAlso boardOptionDefaults(boardName).ContainsKey(optionKey) Then
            Return boardOptionDefaults(boardName)(optionKey)
        End If
        Return "default"
    End Function

    ' Get the actual value that a default option resolves to
    Public Function GetOptionActualValue(boardName As String, optionKey As String, optionValue As String) As String
        If optionValue = "default" Then
            If boardOptionActualValues.ContainsKey(boardName) AndAlso
                boardOptionActualValues(boardName).ContainsKey(optionKey) Then
                Return boardOptionActualValues(boardName)(optionKey)
            End If
        End If
        Return optionValue
    End Function

    ' Format option name for display
    Public Function FormatOptionName(name As String) As String
        Dim result = ""
        Dim lastWasUpper = False
        For i As Integer = 0 To name.Length - 1
            Dim c = name(i)
            If Char.IsUpper(c) Then
                If i > 0 AndAlso Not lastWasUpper Then
                    result += " " + c
                Else
                    result += c
                End If
                lastWasUpper = True
            Else
                If i = 0 Then
                    result += Char.ToUpper(c)
                Else
                    result += c
                End If
                lastWasUpper = False
            End If
        Next
        Return result
    End Function

    ' Load any previously saved custom partition
    Public Sub LoadCustomPartitions()
        For Each boardName In boards.Keys
            AddPartitionOption(boardName, "custom")
        Next

        Dim appDataPath = GetAppDataPath()
        If Not String.IsNullOrEmpty(appDataPath) Then
            Dim customFiles = Directory.GetFiles(appDataPath, "custom_partitions_*.csv")
            If customFiles.Length > 0 Then
                _customPartitionPath = customFiles.OrderByDescending(Function(f) f).FirstOrDefault()
                AddPartitionDetail("custom", "Custom partition from partitions.csv")
            End If
        End If
    End Sub

    ' Set a custom partitions.csv file
    Public Function SetCustomPartitionsCsvFile(filePath As String) As Boolean
        If Not File.Exists(filePath) Then
            Return False
        End If

        Try
            Dim appDataPath = GetAppDataPath()
            If String.IsNullOrEmpty(appDataPath) Then
                Return False
            End If

            Dim destPath = Path.Combine(appDataPath, String.Format("custom_partitions_{0}.csv", DateTime.Now.Ticks))
            File.Copy(filePath, destPath, True)
            _customPartitionPath = destPath

            For Each boardName In boards.Keys
                AddPartitionOption(boardName, "custom")
            Next

            AddPartitionDetail("custom", "Custom partition from partitions.csv")
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' Check if a partition scheme exists
    Public Function PartitionExists(partitionSchemeName As String) As Boolean
        If partitionSchemeName.ToLower() = "custom" Then
            Return HasCustomPartition
        End If

        For Each boardPartitions In partitionSchemes.Values
            If boardPartitions.Contains(partitionSchemeName) Then
                Return True
            End If
        Next

        Return False
    End Function

    ' Get the App_Data path
    Private Function GetAppDataPath() As String
        Try
            Dim appPath = System.Web.HttpContext.Current.Server.MapPath("~/App_Data")
            If Not Directory.Exists(appPath) Then
                Directory.CreateDirectory(appPath)
            End If
            Return appPath
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ' Get a list of all board names
    Public Function GetBoardNames() As List(Of String)
        Return boards.Keys.ToList()
    End Function

    ' Get available partition options for a board
    Public Function GetPartitionOptions(boardName As String) As List(Of String)
        If partitionSchemes.ContainsKey(boardName) Then
            Return partitionSchemes(boardName).ToList()
        End If
        Return New List(Of String)({"default"})
    End Function

    ' Get partition count and info as a string
    Public Function GetPartitionCount() As String
        Dim totalCount = 0
        Dim customCount = 0
        Dim boardsTxtCount = 0

        For Each partScheme In partitionSchemes.Values
            totalCount += partScheme.Count
        Next

        If HasCustomPartition Then
            customCount = 1
        End If

        If IsUsingBoardsTxt() Then
            boardsTxtCount = totalCount - customCount
        End If

        Dim countText = String.Format("Available partition schemes: {0}", totalCount)

        If boardsTxtCount > 0 OrElse customCount > 0 Then
            countText += " ("
            Dim parts = New List(Of String)()
            If boardsTxtCount > 0 Then
                parts.Add(String.Format("{0} from boards.txt", boardsTxtCount))
            End If
            If customCount > 0 Then
                parts.Add(String.Format("{0} custom", customCount))
            End If
            countText += String.Join(", ", parts) + ")"
        End If

        Return countText
    End Function

    ' Get FQBN for specific board and partition
    Public Function GetFQBN(boardName As String, partitionSchemeName As String, sketchFolder As String, Optional boardOptions As Dictionary(Of String, String) = Nothing) As String
        If Not boards.ContainsKey(boardName) Then
            Return ""
        End If

        Dim fqbnPattern = boards(boardName)("fqbnPattern")
        Dim optionsList As New List(Of String)()

        If partitionSchemeName.ToLower() = "custom" AndAlso HasCustomPartition Then
            optionsList.Add("PartitionScheme=custom")
        ElseIf Not String.IsNullOrEmpty(partitionSchemeName) AndAlso partitionSchemeName.ToLower() <> "default" Then
            optionsList.Add(String.Format("PartitionScheme={0}", partitionSchemeName))
        End If

        If boardOptions IsNot Nothing AndAlso boardOptions.Count > 0 Then
            For Each kvp As KeyValuePair(Of String, String) In boardOptions
                Dim defaultValue = GetOptionDefault(boardName, kvp.Key)
                Dim actualDefaultValue = GetOptionActualValue(boardName, kvp.Key, defaultValue)

                If kvp.Value <> actualDefaultValue AndAlso kvp.Value <> "default" Then
                    optionsList.Add(String.Format("{0}={1}", kvp.Key, kvp.Value))
                End If
            Next
        End If

        If optionsList.Count > 0 Then
            Return String.Format("{0}:{1}", fqbnPattern, String.Join(",", optionsList))
        Else
            Return fqbnPattern
        End If
    End Function

    ' Get available board options for a specific board
    Public Function GetBoardOptions(boardName As String) As Dictionary(Of String, Dictionary(Of String, String))
        If boardOptions.ContainsKey(boardName) Then
            Return boardOptions(boardName)
        End If
        Return New Dictionary(Of String, Dictionary(Of String, String))()
    End Function

    ' Import partition schemes and board options from boards.txt
    Public Function ImportFromBoardsTxt(boardsTxtPath As String) As ImportResult
        Dim result = New ImportResult()
        result.Success = False

        If Not File.Exists(boardsTxtPath) Then
            result.Message = "boards.txt file not found."
            Return result
        End If

        Try
            Dim lines = File.ReadAllLines(boardsTxtPath)
            Dim foundPartitions = New Dictionary(Of String, List(Of String))()
            Dim foundOptions = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))()
            Dim foundOptionDefaults = New Dictionary(Of String, Dictionary(Of String, String))()
            Dim foundOptionActualValues = New Dictionary(Of String, Dictionary(Of String, String))()
            Dim foundPartitionCount = 0
            Dim foundOptionCount = 0

            For Each line In lines
                If String.IsNullOrWhiteSpace(line) OrElse line.TrimStart().StartsWith("#") Then
                    Continue For
                End If

                Dim partMatch = Regex.Match(line, "^([^.]+)\.menu\.PartitionScheme\.([^=]+)=(.*)")
                If partMatch.Success Then
                    Dim boardId = partMatch.Groups(1).Value
                    Dim partitionId = partMatch.Groups(2).Value
                    Dim partitionName = partMatch.Groups(3).Value
                    Dim matchedBoard = FindMatchingBoardName(boardId)

                    If Not String.IsNullOrEmpty(matchedBoard) Then
                        If Not foundPartitions.ContainsKey(matchedBoard) Then
                            foundPartitions(matchedBoard) = New List(Of String)()
                        End If
                        foundPartitions(matchedBoard).Add(partitionId)
                        foundPartitionCount += 1
                        AddPartitionDetail(partitionId, partitionName)
                        If String.IsNullOrEmpty(result.FirstPartitionName) Then
                            result.FirstPartitionName = partitionId
                        End If
                    Else
                        result.Warnings.Add(String.Format("No matching board for '{0}'", boardId))
                    End If
                    Continue For
                End If

                Dim defaultMatch = Regex.Match(line, "^([^.]+)\.menu\.([^.]+)\.default=(.*)")
                If defaultMatch.Success Then
                    Dim boardId = defaultMatch.Groups(1).Value
                    Dim optionName = defaultMatch.Groups(2).Value
                    Dim defaultValue = defaultMatch.Groups(3).Value

                    If optionName.ToLower() = "partitionscheme" Then
                        Continue For
                    End If

                    Dim matchedBoard = FindMatchingBoardName(boardId)
                    If Not String.IsNullOrEmpty(matchedBoard) Then
                        If Not foundOptionDefaults.ContainsKey(matchedBoard) Then
                            foundOptionDefaults(matchedBoard) = New Dictionary(Of String, String)()
                        End If
                        If Not foundOptionActualValues.ContainsKey(matchedBoard) Then
                            foundOptionActualValues(matchedBoard) = New Dictionary(Of String, String)()
                        End If
                        foundOptionDefaults(matchedBoard)(optionName) = defaultValue
                        foundOptionActualValues(matchedBoard)(optionName) = defaultValue
                    End If
                    Continue For
                End If

                Dim optMatch = Regex.Match(line, "^([^.]+)\.menu\.([^.]+)\.([^=]+)=(.*)")
                If optMatch.Success Then
                    Dim boardId = optMatch.Groups(1).Value
                    Dim optionName = optMatch.Groups(2).Value
                    Dim optionValue = optMatch.Groups(3).Value
                    Dim optionDisplay = optMatch.Groups(4).Value

                    If optionName.ToLower() = "partitionscheme" OrElse optionValue.ToLower() = "default" Then
                        Continue For
                    End If

                    Dim matchedBoard = FindMatchingBoardName(boardId)
                    If Not String.IsNullOrEmpty(matchedBoard) Then
                        If Not foundOptions.ContainsKey(matchedBoard) Then
                            foundOptions(matchedBoard) = New Dictionary(Of String, Dictionary(Of String, String))()
                        End If
                        If Not foundOptions(matchedBoard).ContainsKey(optionName) Then
                            foundOptions(matchedBoard)(optionName) = New Dictionary(Of String, String)()
                        End If
                        foundOptions(matchedBoard)(optionName)(optionValue) = optionDisplay
                        foundOptionCount += 1
                    End If
                End If
            Next

            If foundPartitionCount > 0 Then
                partitionSchemes.Clear()
                For Each kvp In foundPartitions
                    partitionSchemes(kvp.Key) = kvp.Value
                    If HasCustomPartition Then
                        AddPartitionOption(kvp.Key, "custom")
                    End If
                Next
                _boardsTxtPath = boardsTxtPath

                If foundOptionCount > 0 Then
                    boardOptions.Clear()
                    For Each boardKvp In foundOptions
                        boardOptions(boardKvp.Key) = boardKvp.Value
                    Next

                    If foundOptionDefaults.Count > 0 Then
                        boardOptionDefaults.Clear()
                        For Each boardKvp In foundOptionDefaults
                            boardOptionDefaults(boardKvp.Key) = boardKvp.Value
                        Next
                    End If

                    If foundOptionActualValues.Count > 0 Then
                        boardOptionActualValues.Clear()
                        For Each boardKvp In foundOptionActualValues
                            boardOptionActualValues(boardKvp.Key) = boardKvp.Value
                        Next
                    End If
                    result.FoundBoardOptions = True
                End If

                result.Success = True
                result.Message = String.Format("Successfully imported {0} partition schemes and {1} configuration options from boards.txt", foundPartitionCount, foundOptionCount)
            Else
                result.Message = "No partition schemes found in boards.txt"
            End If

        Catch ex As Exception
            result.Message = String.Format("Error parsing boards.txt: {0}", ex.Message)
        End Try

        Return result
    End Function

    ' Find matching board name from boardId
    Private Function FindMatchingBoardName(boardId As String) As String
        For Each board In boards.Keys
            Dim boardName = board.ToLower()
            If boardName.Contains(boardId.ToLower()) Then
                Return board
            End If
        Next

        For Each board In boards.Keys
            If boards(board)("fqbnPattern").Contains(String.Format(":{0}", boardId)) Then
                Return board
            End If
        Next

        Return ""
    End Function

    ' Import a custom partition from CSV file
    Public Function ImportCustomPartition(csvPath As String) As ImportResult
        Dim result = New ImportResult()
        result.Success = False

        If Not File.Exists(csvPath) Then
            result.Message = "CSV file not found."
            Return result
        End If

        Try
            Dim lines = File.ReadAllLines(csvPath)
            Dim isValidFormat = False
            For Each line In lines
                If line.Contains("app") OrElse line.Contains("data") OrElse line.Contains("spiffs") Then
                    isValidFormat = True
                    Exit For
                End If
            Next

            If Not isValidFormat Then
                result.Message = "File does not appear to be a valid partitions.csv file"
                Return result
            End If

            If SetCustomPartitionsCsvFile(csvPath) Then
                result.Success = True
                result.Message = "Custom partition scheme imported successfully"
                result.FirstPartitionName = "custom"
            Else
                result.Message = "Failed to import custom partition scheme"
            End If

        Catch ex As Exception
            result.Message = String.Format("Error importing CSV: {0}", ex.Message)
        End Try

        Return result
    End Function

    ' Get FQBN specifically for compilation/upload with forced settings
    Public Function GetFQBNForCompilation(boardName As String, partitionSchemeName As String, sketchFolder As String, Optional boardOptions As Dictionary(Of String, String) = Nothing) As String
        If Not boards.ContainsKey(boardName) Then
            Return ""
        End If

        Dim fqbnPattern = boards(boardName)("fqbnPattern")
        Dim optionsList As New List(Of String)()

        If partitionSchemeName.ToLower() = "custom" AndAlso HasCustomPartition Then
            optionsList.Add("PartitionScheme=custom")
        ElseIf Not String.IsNullOrEmpty(partitionSchemeName) AndAlso partitionSchemeName.ToLower() <> "default" Then
            optionsList.Add(String.Format("PartitionScheme={0}", partitionSchemeName))
        End If

        If boardOptions IsNot Nothing AndAlso boardOptions.Count > 0 Then
            For Each kvp As KeyValuePair(Of String, String) In boardOptions
                If kvp.Value <> "default" Then
                    optionsList.Add(String.Format("{0}={1}", kvp.Key, kvp.Value))
                End If
            Next
        End If

        If optionsList.Count > 0 Then
            Return String.Format("{0}:{1}", fqbnPattern, String.Join(",", optionsList))
        Else
            Return fqbnPattern
        End If
    End Function

    ' Clear custom partition
    Public Sub ClearCustomPartition()
        If Not String.IsNullOrEmpty(_customPartitionPath) AndAlso File.Exists(_customPartitionPath) Then
            Try
                File.Delete(_customPartitionPath)
            Catch
            End Try
        End If
        _customPartitionPath = ""
    End Sub

    ' Clear boards.txt partitions
    Public Sub ClearBoardsTxtPartitions()
        _boardsTxtPath = ""
        partitionSchemes.Clear()
        boardOptions.Clear()
        InitDefaultBoards()
    End Sub
End Class

' Result class for import operations
Public Class ImportResult
    Public Property Success As Boolean = False
    Public Property Message As String = ""
    Public Property FirstPartitionName As String = ""
    Public Property Warnings As New List(Of String)()
    Public Property FoundBoardOptions As Boolean = False
End Class
