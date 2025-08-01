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

        ' Only add if it doesn't exist
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
        ' Add ESP32 boards
        AddBoard("ESP32 Dev Module", "esp32:esp32:esp32")
        AddBoard("ESP32-S2", "esp32:esp32:esp32s2")
        AddBoard("ESP32-C3", "esp32:esp32:esp32c3")
        AddBoard("ESP32-S3", "esp32:esp32:esp32s3")
        AddBoard("ESP32 Wrover Kit", "esp32:esp32:esp32wrover")
        AddBoard("ESP32 Pico Kit", "esp32:esp32:pico32")
        AddBoard("ESP32-C6", "esp32:esp32:esp32c6")
        AddBoard("ESP32-H2", "esp32:esp32:esp32h2")

        ' Add some default partition schemes
        AddPartitionOption("ESP32 Dev Module", "default")
        AddPartitionOption("ESP32 Dev Module", "huge_app")
        AddPartitionOption("ESP32 Dev Module", "min_spiffs")
        AddPartitionOption("ESP32 Dev Module", "no_ota")

        ' Add partition details
        AddPartitionDetail("default", "Default with balanced app/SPIFFS (1.2MB App/1.5MB SPIFFS)")
        AddPartitionDetail("huge_app", "Huge App with minimal SPIFFS (3MB App/190KB SPIFFS)")
        AddPartitionDetail("min_spiffs", "Minimal SPIFFS (1.9MB APP/190KB SPIFFS)")
        AddPartitionDetail("no_ota", "No OTA (2MB APP/2MB SPIFFS)")

        ' Check for saved custom partition
        LoadCustomPartitions()

        ' Add default board options for common boards
        AddDefaultBoardOptions()
    End Sub

    ' Add default board options
    Private Sub AddDefaultBoardOptions()
        ' ESP32 Dev Module default options
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
        flashSize.Add("2M", "2MB (16Mb)")
        flashSize.Add("16M", "16MB (128Mb)")
        esp32Options.Add("FlashSize", flashSize)

        ' Upload Speed
        Dim uploadSpeed As New Dictionary(Of String, String)
        uploadSpeed.Add("921600", "921600")
        uploadSpeed.Add("115200", "115200")
        uploadSpeed.Add("230400", "230400")
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

        ' Add to the board options dictionary
        boardOptions.Add("ESP32 Dev Module", esp32Options)

        ' Add similar defaults for other ESP32 variants
        boardOptions.Add("ESP32-S2", esp32Options)
        boardOptions.Add("ESP32-C3", esp32Options)
        boardOptions.Add("ESP32-S3", esp32Options)
        boardOptions.Add("ESP32 Wrover Kit", esp32Options)
        boardOptions.Add("ESP32 Pico Kit", esp32Options)

        ' Add option details/descriptions
        AddOptionDetails()

        ' Set default values for options
        AddOptionDefaults()
    End Sub

    ' Add option descriptions and details
    Private Sub AddOptionDetails()
        ' Create descriptions dictionary for each option type
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
    End Sub

    ' Add default values for options
    Private Sub AddOptionDefaults()
        ' ESP32 Dev Module defaults
        Dim esp32Defaults As New Dictionary(Of String, String)
        esp32Defaults.Add("CPUFreq", "240")
        esp32Defaults.Add("FlashFreq", "80")
        esp32Defaults.Add("FlashMode", "qio")
        esp32Defaults.Add("FlashSize", "4M")
        esp32Defaults.Add("UploadSpeed", "921600")
        esp32Defaults.Add("DebugLevel", "none")
        esp32Defaults.Add("PSRAM", "disabled")
        boardOptionDefaults.Add("ESP32 Dev Module", esp32Defaults)

        ' Copy the same defaults to other boards for now
        boardOptionDefaults.Add("ESP32-S2", esp32Defaults)
        boardOptionDefaults.Add("ESP32-C3", esp32Defaults)
        boardOptionDefaults.Add("ESP32-S3", esp32Defaults)
        boardOptionDefaults.Add("ESP32 Wrover Kit", esp32Defaults)
        boardOptionDefaults.Add("ESP32 Pico Kit", esp32Defaults)
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

    ' Format option name for display (e.g., "CPUFreq" -> "CPU Frequency")
    Public Function FormatOptionName(name As String) As String
        Dim result = ""
        Dim lastWasUpper = False

        For i As Integer = 0 To name.Length - 1
            Dim c = name(i)

            If Char.IsUpper(c) Then
                If i > 0 AndAlso Not lastWasUpper Then
                    ' Add space before new uppercase letter that follows lowercase
                    result += " " + c
                Else
                    result += c
                End If
                lastWasUpper = True
            Else
                If i = 0 Then
                    ' Capitalize first letter
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
        ' Add custom partition option to all boards
        For Each boardName In boards.Keys
            AddPartitionOption(boardName, "custom")
        Next

        ' Try to find custom partition CSV in App_Data
        Dim appDataPath = GetAppDataPath()
        If Not String.IsNullOrEmpty(appDataPath) Then
            ' Look for any custom partition files
            Dim customFiles = Directory.GetFiles(appDataPath, "custom_partitions_*.csv")
            If customFiles.Length > 0 Then
                ' Use the most recent one (highest timestamp)
                _customPartitionPath = customFiles.OrderByDescending(Function(f) f).FirstOrDefault()
                ' Add partition details
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
            ' Copy the file to App_Data
            Dim appDataPath = GetAppDataPath()
            If String.IsNullOrEmpty(appDataPath) Then
                Return False
            End If

            ' Create a unique name using timestamp
            Dim destPath = Path.Combine(appDataPath, String.Format("custom_partitions_{0}.csv", DateTime.Now.Ticks))
            File.Copy(filePath, destPath, True)

            ' Update path and add option
            _customPartitionPath = destPath

            ' Add to all boards
            For Each boardName In boards.Keys
                AddPartitionOption(boardName, "custom")
            Next

            ' Add partition details
            AddPartitionDetail("custom", "Custom partition from partitions.csv")

            Return True
        Catch ex As Exception
            ' Handle any errors
            Return False
        End Try
    End Function

    ' Check if a partition scheme exists
    Public Function PartitionExists(partitionSchemeName As String) As Boolean
        ' Custom partition is handled specially
        If partitionSchemeName.ToLower() = "custom" Then
            Return HasCustomPartition
        End If

        ' Check if any board has this partition
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

        ' Return a default if no specific schemes found
        Return New List(Of String)({"default"})
    End Function

    ' Get partition count and info as a string
    Public Function GetPartitionCount() As String
        Dim totalCount = 0
        Dim customCount = 0
        Dim boardsTxtCount = 0

        ' Count across all boards
        For Each partScheme In partitionSchemes.Values
            totalCount += partScheme.Count
        Next

        ' Determine how many came from custom/boards.txt
        If HasCustomPartition Then
            customCount = 1  ' Just one custom partition scheme
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

        ' Handle custom partition scheme
        If partitionSchemeName.ToLower() = "custom" AndAlso HasCustomPartition Then
            optionsList.Add("PartitionScheme=custom")
            ' For normal partition schemes
        ElseIf partitionSchemeName.ToLower() <> "default" Then
            optionsList.Add(String.Format("PartitionScheme={0}", partitionSchemeName))
        End If

        ' Add any other board options
        If boardOptions IsNot Nothing AndAlso boardOptions.Count > 0 Then
            For Each kvp As KeyValuePair(Of String, String) In boardOptions
                If kvp.Value <> "default" Then
                    optionsList.Add(String.Format("{0}={1}", kvp.Key, kvp.Value))
                End If
            Next
        End If

        ' Construct the full FQBN
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
        ' Return empty dictionary if no options found
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
            ' Read all lines from the file
            Dim lines = File.ReadAllLines(boardsTxtPath)

            ' Track found partitions for each board
            Dim foundPartitions = New Dictionary(Of String, List(Of String))()
            Dim foundOptions = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))()
            Dim foundOptionDefaults = New Dictionary(Of String, Dictionary(Of String, String))()
            Dim foundPartitionCount = 0
            Dim foundOptionCount = 0

            ' Parse the file to find partition schemes
            For Each line In lines
                ' Skip empty lines and comments
                If String.IsNullOrWhiteSpace(line) OrElse line.TrimStart().StartsWith("#") Then
                    Continue For
                End If

                ' Find partition schemes (lines like: esp32.menu.PartitionScheme.default=Default 4MB with spiffs)
                Dim partMatch = Regex.Match(line, "^([^.]+)\.menu\.PartitionScheme\.([^=]+)=(.*)")
                If partMatch.Success Then
                    Dim boardId = partMatch.Groups(1).Value
                    Dim partitionId = partMatch.Groups(2).Value
                    Dim partitionName = partMatch.Groups(3).Value

                    ' Find the matching board in our config
                    Dim matchedBoard = FindMatchingBoardName(boardId)

                    ' If we found a matching board, add this partition
                    If Not String.IsNullOrEmpty(matchedBoard) Then
                        ' Initialize if needed
                        If Not foundPartitions.ContainsKey(matchedBoard) Then
                            foundPartitions(matchedBoard) = New List(Of String)()
                        End If

                        ' Add the partition
                        foundPartitions(matchedBoard).Add(partitionId)
                        foundPartitionCount += 1

                        ' Add partition details
                        AddPartitionDetail(partitionId, partitionName)

                        ' Track first partition for return value
                        If String.IsNullOrEmpty(result.FirstPartitionName) Then
                            result.FirstPartitionName = partitionId
                        End If
                    Else
                        ' Couldn't find matching board
                        result.Warnings.Add(String.Format("No matching board for '{0}'", boardId))
                    End If

                    Continue For
                End If

                ' Find default option values (lines like: esp32.menu.FlashFreq.default=80)
                Dim defaultMatch = Regex.Match(line, "^([^.]+)\.menu\.([^.]+)\.default=(.*)")
                If defaultMatch.Success Then
                    Dim boardId = defaultMatch.Groups(1).Value
                    Dim optionName = defaultMatch.Groups(2).Value
                    Dim defaultValue = defaultMatch.Groups(3).Value

                    ' Skip PartitionScheme as we handle it separately
                    If optionName.ToLower() = "partitionscheme" Then
                        Continue For
                    End If

                    ' Find the matching board
                    Dim matchedBoard = FindMatchingBoardName(boardId)

                    If Not String.IsNullOrEmpty(matchedBoard) Then
                        ' Initialize defaults dictionary if needed
                        If Not foundOptionDefaults.ContainsKey(matchedBoard) Then
                            foundOptionDefaults(matchedBoard) = New Dictionary(Of String, String)()
                        End If

                        ' Add default value
                        foundOptionDefaults(matchedBoard)(optionName) = defaultValue
                    End If

                    Continue For
                End If

                ' Find other board options (lines like: esp32.menu.CPUFreq.240=240MHz (WiFi/BT))
                Dim optMatch = Regex.Match(line, "^([^.]+)\.menu\.([^.]+)\.([^=]+)=(.*)")
                If optMatch.Success Then
                    Dim boardId = optMatch.Groups(1).Value
                    Dim optionName = optMatch.Groups(2).Value
                    Dim optionValue = optMatch.Groups(3).Value
                    Dim optionDisplay = optMatch.Groups(4).Value

                    ' Skip PartitionScheme as we handle it separately
                    If optionName.ToLower() = "partitionscheme" Then
                        Continue For
                    End If

                    ' Skip default values as we handle them separately
                    If optionValue.ToLower() = "default" Then
                        Continue For
                    End If

                    ' Find the matching board
                    Dim matchedBoard = FindMatchingBoardName(boardId)

                    If Not String.IsNullOrEmpty(matchedBoard) Then
                        ' Initialize board options if needed
                        If Not foundOptions.ContainsKey(matchedBoard) Then
                            foundOptions(matchedBoard) = New Dictionary(Of String, Dictionary(Of String, String))()
                        End If

                        ' Initialize option dictionary if needed
                        If Not foundOptions(matchedBoard).ContainsKey(optionName) Then
                            foundOptions(matchedBoard)(optionName) = New Dictionary(Of String, String)()
                        End If

                        ' Add the option
                        foundOptions(matchedBoard)(optionName)(optionValue) = optionDisplay
                        foundOptionCount += 1
                    End If
                End If
            Next

            ' If we found partitions, update our configuration
            If foundPartitionCount > 0 Then
                ' Clear existing partitions and replace with found ones
                partitionSchemes.Clear()

                ' Copy found partitions
                For Each kvp In foundPartitions
                    partitionSchemes(kvp.Key) = kvp.Value

                    ' Make sure custom is still available if needed
                    If HasCustomPartition Then
                        AddPartitionOption(kvp.Key, "custom")
                    End If
                Next

                ' Store the path
                _boardsTxtPath = boardsTxtPath

                ' Update board options if we found any
                If foundOptionCount > 0 Then
                    ' Clear existing board options and replace with found ones
                    boardOptions.Clear()

                    ' Copy found options
                    For Each boardKvp In foundOptions
                        boardOptions(boardKvp.Key) = boardKvp.Value
                    Next

                    ' Update option defaults
                    If foundOptionDefaults.Count > 0 Then
                        boardOptionDefaults.Clear()
                        For Each boardKvp In foundOptionDefaults
                            boardOptionDefaults(boardKvp.Key) = boardKvp.Value
                        Next
                    End If

                    result.FoundBoardOptions = True
                End If

                ' Success!
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
        ' First try direct match
        For Each board In boards.Keys
            Dim boardName = board.ToLower()

            ' Try direct match based on board naming conventions
            If boardName.Contains(boardId.ToLower()) Then
                Return board
            End If
        Next

        ' Try by FQBN pattern
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
            ' Read the file to validate
            Dim lines = File.ReadAllLines(csvPath)

            ' Check if it looks like a partitions.csv file
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

            ' Save as the custom partition file
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

    ' Clear custom partition
    Public Sub ClearCustomPartition()
        If Not String.IsNullOrEmpty(_customPartitionPath) AndAlso File.Exists(_customPartitionPath) Then
            Try
                ' Delete the file
                File.Delete(_customPartitionPath)
            Catch
                ' Ignore errors on deletion
            End Try
        End If

        _customPartitionPath = ""
    End Sub

    ' Clear boards.txt partitions
    Public Sub ClearBoardsTxtPartitions()
        _boardsTxtPath = ""

        ' Reset to default boards and partitions
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