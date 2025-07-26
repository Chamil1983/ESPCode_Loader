Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports System.Web

Public Class BoardManager
    ' Private fields
    Private boardConfigurations As Dictionary(Of String, String) = New Dictionary(Of String, String)()
    Private boardParameters As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))()
    Private customPartitionFile As String = String.Empty

    ' Properties
    Public Property BoardsFilePath As String = String.Empty

    ' Constructor
    Public Sub New()
        ' Set default boards file location
        Dim appPath = HttpContext.Current.Server.MapPath("~/")
        BoardsFilePath = Path.Combine(appPath, "App_Data", "boards.txt")

        ' Load configurations
        LoadBoardConfigurations()
    End Sub

    ' Public methods
    Public Sub LoadBoardConfigurations()
        ' Clear existing configurations
        boardConfigurations.Clear()
        boardParameters.Clear()

        ' Add default configurations
        AddDefaultConfigurations()

        ' Load custom configurations if file exists
        If File.Exists(BoardsFilePath) Then
            Try
                Dim lines As String() = File.ReadAllLines(BoardsFilePath)
                ParseBoardsFile(lines)
                LogMessage($"Loaded {boardConfigurations.Count} board configurations")
            Catch ex As Exception
                LogMessage($"Error loading boards: {ex.Message}")
            End Try
        Else
            LogMessage($"Boards file not found: {BoardsFilePath}")
        End If
    End Sub

    ' Parse boards.txt file to extract all board configurations
    Private Sub ParseBoardsFile(lines As String())
        ' First pass: find all board IDs
        Dim boardIDs As New Dictionary(Of String, String)()

        For Each line In lines
            ' Look for board name entries (e.g., "esp32.name=ESP32 Dev Module")
            If line.Contains(".name=") Then
                Try
                    Dim parts = line.Split(New Char() {"."c}, 2)
                    If parts.Length >= 2 Then
                        Dim boardId = parts(0)
                        parts = parts(1).Split(New Char() {"="c}, 2)
                        If parts.Length >= 2 Then
                            Dim boardName = parts(1).Trim()
                            boardIDs.Add(boardId, boardName)

                            ' Initialize parameter dictionary for this board
                            boardParameters.Add(boardName, New Dictionary(Of String, String)())
                        End If
                    End If
                Catch ex As Exception
                    LogMessage($"Error parsing board name: {line}, {ex.Message}")
                End Try
            End If
        Next

        ' Second pass: extract all parameters for each board
        For Each boardId In boardIDs.Keys
            Dim boardName = boardIDs(boardId)
            Dim parameters = boardParameters(boardName)

            ' Default parameters
            parameters("menu.PartitionScheme") = "Partition Scheme"
            parameters("menu.CPUFreq") = "CPU Frequency"
            parameters("menu.FlashMode") = "Flash Mode"
            parameters("menu.FlashFreq") = "Flash Frequency"
            parameters("menu.UploadSpeed") = "Upload Speed"
            parameters("menu.DebugLevel") = "Debug Level"

            ' Extract all parameters for this board
            For Each line In lines
                If line.StartsWith(boardId & ".") Then
                    Try
                        ' Skip the board name entry already processed
                        If line.Contains(".name=") Then Continue For

                        Dim lineParts = line.Substring(boardId.Length + 1).Split(New Char() {"="c}, 2)
                        If lineParts.Length >= 2 Then
                            Dim key = lineParts(0)
                            Dim value = lineParts(1).Trim()

                            ' Store the parameter
                            parameters(key) = value
                        End If
                    Catch ex As Exception
                        LogMessage($"Error parsing parameter: {line}, {ex.Message}")
                    End Try
                End If
            Next

            ' Build FQBN with default parameters
            BuildBoardFQBN(boardId, boardName, parameters)
        Next
    End Sub

    ' Build FQBN for a board with its default parameters
    Private Sub BuildBoardFQBN(boardId As String, boardName As String, parameters As Dictionary(Of String, String))
        ' Default configuration
        Dim vendor = "esp32"
        Dim architecture = "esp32"
        Dim paramList As New Dictionary(Of String, String)()

        ' Parse build parameters for default values
        For Each key In parameters.Keys
            ' Look for default menu selections
            If key.StartsWith("menu.") AndAlso key.Contains(".") Then Continue For

            ' Extract default values for important parameters
            If key = "build.flash_mode" Then
                paramList("FlashMode") = parameters(key)
            ElseIf key = "build.f_flash" Then
                ' Convert Hz to MHz (e.g., 80000000L -> 80)
                Dim flashFreq = parameters(key).Replace("L", "").Replace("UL", "")
                If flashFreq.EndsWith("000000") Then
                    flashFreq = (Long.Parse(flashFreq) / 1000000).ToString()
                End If
                paramList("FlashFreq") = flashFreq
            ElseIf key = "build.f_cpu" Then
                ' Convert Hz to MHz
                Dim cpuFreq = parameters(key).Replace("L", "").Replace("UL", "")
                If cpuFreq.EndsWith("000000") Then
                    cpuFreq = (Long.Parse(cpuFreq) / 1000000).ToString()
                End If
                paramList("CPUFreq") = cpuFreq
            ElseIf key = "build.partitions" Then
                paramList("PartitionScheme") = parameters(key)
            ElseIf key = "upload.speed" Then
                paramList("UploadSpeed") = parameters(key)
            ElseIf key = "build.core" AndAlso parameters.ContainsKey("build.variant") Then
                ' Determine board architecture
                architecture = parameters(key).Split(New Char() {":"c})(0)
            End If
        Next

        ' Set defaults if not found
        If Not paramList.ContainsKey("CPUFreq") Then paramList("CPUFreq") = "240"
        If Not paramList.ContainsKey("FlashMode") Then paramList("FlashMode") = "dio"
        If Not paramList.ContainsKey("FlashFreq") Then paramList("FlashFreq") = "80"
        If Not paramList.ContainsKey("PartitionScheme") Then paramList("PartitionScheme") = "default"

        ' Build parameter string
        Dim paramStrings As New List(Of String)
        For Each kvp In paramList
            paramStrings.Add($"{kvp.Key}={kvp.Value}")
        Next

        Dim paramStr = String.Join(",", paramStrings)
        Dim fqbn = $"{vendor}:{architecture}:{boardId}:{paramStr}"

        ' Add to configurations
        boardConfigurations(boardName) = fqbn
    End Sub

    ' Add default ESP32 board configurations
    Private Sub AddDefaultConfigurations()
        ' KC-Link boards
        boardConfigurations.Add("KC-Link PRO A8 (Default)", "esp32:esp32:esp32:PartitionScheme=default,CPUFreq=240,FlashMode=qio,FlashFreq=80")
        boardConfigurations.Add("KC-Link PRO A8 (Minimal)", "esp32:esp32:esp32:PartitionScheme=min_spiffs,CPUFreq=240,FlashMode=qio,FlashFreq=80")
        boardConfigurations.Add("KC-Link PRO A8 (OTA)", "esp32:esp32:esp32:PartitionScheme=min_ota,CPUFreq=240,FlashMode=qio,FlashFreq=80")

        ' Add these to parameters dictionary as well
        AddDefaultBoardParameters("KC-Link PRO A8 (Default)")
        AddDefaultBoardParameters("KC-Link PRO A8 (Minimal)")
        AddDefaultBoardParameters("KC-Link PRO A8 (OTA)")

        ' Standard ESP32 boards
        boardConfigurations.Add("ESP32 Dev Module", "esp32:esp32:esp32:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80")
        boardConfigurations.Add("ESP32 Wrover Kit", "esp32:esp32:esp32wrover:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80")
        boardConfigurations.Add("ESP32 Pico Kit", "esp32:esp32:pico32:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80")
        boardConfigurations.Add("ESP32-S2 Dev Module", "esp32:esp32:esp32s2:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80")
        boardConfigurations.Add("ESP32-S3 Dev Module", "esp32:esp32:esp32s3:PartitionScheme=default,CPUFreq=240,FlashMode=dio,FlashFreq=80")
        boardConfigurations.Add("ESP32-C3 Dev Module", "esp32:esp32:esp32c3:PartitionScheme=default,CPUFreq=160,FlashMode=dio,FlashFreq=80")

        AddDefaultBoardParameters("ESP32 Dev Module")
        AddDefaultBoardParameters("ESP32 Wrover Kit")
        AddDefaultBoardParameters("ESP32 Pico Kit")
        AddDefaultBoardParameters("ESP32-S2 Dev Module")
        AddDefaultBoardParameters("ESP32-S3 Dev Module")
        AddDefaultBoardParameters("ESP32-C3 Dev Module")
    End Sub

    ' Add default parameters for a board
    Private Sub AddDefaultBoardParameters(boardName As String)
        Dim parameters As New Dictionary(Of String, String)()

        ' Common menu parameters
        parameters("menu.PartitionScheme") = "Partition Scheme"
        parameters("menu.CPUFreq") = "CPU Frequency"
        parameters("menu.FlashMode") = "Flash Mode"
        parameters("menu.FlashFreq") = "Flash Frequency"
        parameters("menu.UploadSpeed") = "Upload Speed"
        parameters("menu.DebugLevel") = "Debug Level"

        ' Default values
        parameters("menu.PartitionScheme.default") = "Default"
        parameters("menu.PartitionScheme.min_spiffs") = "Minimal SPIFFS"
        parameters("menu.PartitionScheme.min_ota") = "Minimal OTA"
        parameters("menu.PartitionScheme.huge_app") = "Huge App"
        parameters("menu.PartitionScheme.custom") = "Custom"

        parameters("menu.CPUFreq.240") = "240MHz"
        parameters("menu.CPUFreq.160") = "160MHz"
        parameters("menu.CPUFreq.80") = "80MHz"

        parameters("menu.FlashMode.qio") = "QIO"
        parameters("menu.FlashMode.dio") = "DIO"
        parameters("menu.FlashMode.qout") = "QOUT"
        parameters("menu.FlashMode.dout") = "DOUT"

        parameters("menu.FlashFreq.80") = "80MHz"
        parameters("menu.FlashFreq.40") = "40MHz"

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

        ' Add to board parameters dictionary
        boardParameters(boardName) = parameters
    End Sub

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

    Public Function GetBoardParameters(boardName As String) As Dictionary(Of String, String)
        ' Return parameters for the given board
        If boardParameters.ContainsKey(boardName) Then
            Return boardParameters(boardName)
        Else
            ' Return empty dictionary if board not found
            Return New Dictionary(Of String, String)()
        End If
    End Function

    Public Function GetParameterOptions(boardName As String, paramName As String) As Dictionary(Of String, String)
        Dim result As New Dictionary(Of String, String)()

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
                    result.Add("240", "240MHz")
                    result.Add("160", "160MHz")
                    result.Add("80", "80MHz")
                Case "FlashMode"
                    result.Add("qio", "QIO")
                    result.Add("dio", "DIO")
                    result.Add("qout", "QOUT")
                    result.Add("dout", "DOUT")
                Case "FlashFreq"
                    result.Add("80", "80MHz")
                    result.Add("40", "40MHz")
                Case "PartitionScheme"
                    result.Add("default", "Default")
                    result.Add("min_spiffs", "Minimal SPIFFS")
                    result.Add("min_ota", "Minimal OTA")
                    result.Add("huge_app", "Huge App")
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

        ' Copy the partition file to the App_Data directory
        Try
            ' Ensure the partition file has a proper name
            Dim fileName = Path.GetFileName(filePath)
            Dim partitionName = Path.GetFileNameWithoutExtension(fileName).ToLower()

            ' Copy to App_Data directory
            Dim appDataPath = HttpContext.Current.Server.MapPath("~/App_Data")
            Dim partitionsDir = Path.Combine(appDataPath, "partitions")

            ' Create partitions directory if it doesn't exist
            If Not Directory.Exists(partitionsDir) Then
                Directory.CreateDirectory(partitionsDir)
            End If

            ' Copy the custom partition file to the partitions directory
            Dim destFile = Path.Combine(partitionsDir, "custom.csv")
            File.Copy(filePath, destFile, True)

            LogMessage($"Custom partition file copied to {destFile}")
        Catch ex As Exception
            ' Log error but continue
            LogMessage($"Error copying custom partition file: {ex.Message}")
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

            ' Parse the FQBN into parts
            Dim parts = fqbn.Split(New Char() {":"c})

            ' Check if we have enough parts for a valid FQBN
            If parts.Length >= 3 Then
                ' Extract vendor, architecture, board ID
                Dim vendor = parts(0)
                Dim architecture = parts(1)
                Dim boardId = parts(2)

                ' Build parameter string
                Dim paramList As New List(Of String)
                For Each kvp In parameters
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
                LogMessage($"Updated board configuration for {boardName}: {newFqbn}")
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
            ElseIf boardName.Contains("C3") Then
                boardId = "esp32c3"
            ElseIf boardName.Contains("Wrover") Then
                boardId = "esp32wrover"
            ElseIf boardName.Contains("Pico") Then
                boardId = "pico32"
            End If

            ' Build parameter string
            Dim paramList As New List(Of String)
            For Each kvp In parameters
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
            LogMessage($"Added new board configuration for {boardName}: {newFqbn}")
        End If
    End Sub

    Public Function GetCustomPartitions() As List(Of String)
        ' Return a list of available custom partition schemes
        Dim partitionsList As New List(Of String)

        ' Check for partition files in expected location
        Dim partitionsDir = HttpContext.Current.Server.MapPath("~/App_Data/partitions")

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
                LogMessage($"Error reading partition files: {ex.Message}")
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

        Return partitionsList.Distinct().ToList()
    End Function

    Private Sub LogMessage(message As String)
        ' Log to application log
        Dim logFilePath = HttpContext.Current.Server.MapPath("~/App_Data/logs/app.log")
        Dim logDir = Path.GetDirectoryName(logFilePath)

        If Not Directory.Exists(logDir) Then
            Directory.CreateDirectory(logDir)
        End If

        Try
            Using writer As New StreamWriter(logFilePath, True)
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}")
            End Using
        Catch ex As Exception
            ' Cannot log the log error
        End Try
    End Sub
End Class