Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text.RegularExpressions

Public Class BoardConfigManager
    ' Properties for storing board configuration data
    Public Property Boards As New Dictionary(Of String, Dictionary(Of String, String))
    Public Property BoardsFilePath As String = ""
    Public Property PartitionSchemes As New Dictionary(Of String, String)

    ' Selected configurations
    Private selectedBoard As String = ""
    Private selectedPartition As String = ""
    Private customPartitionFile As String = ""

    ' Constants for configuration keys
    Private Const FQBN_PREFIX As String = "espressif:esp32:"
    Private Const DEFAULT_PARTITION As String = "default"

    ' Constructor
    Public Sub New()
        ' Initialize with default values
    End Sub

    ' Load board configurations from the boards.txt file
    Public Sub LoadBoardConfigurations()
        ' Clear existing configurations
        Boards.Clear()
        PartitionSchemes.Clear()

        If String.IsNullOrEmpty(BoardsFilePath) OrElse Not File.Exists(BoardsFilePath) Then
            ' Try to find boards.txt in common locations
            FindBoardsFile()

            If String.IsNullOrEmpty(BoardsFilePath) OrElse Not File.Exists(BoardsFilePath) Then
                Throw New FileNotFoundException("boards.txt file not found. Please configure path to Arduino ESP32 core.")
            End If
        End If

        ' Read and parse the boards.txt file
        Dim lines = File.ReadAllLines(BoardsFilePath)

        ' First parse all ESP32 boards
        For Each line In lines
            ' Skip comments and empty lines
            If line.Trim().StartsWith("#") OrElse String.IsNullOrWhiteSpace(line) Then
                Continue For
            End If

            ' Parse key-value pairs
            Dim parts = line.Split(New Char() {"="c}, 2)
            If parts.Length = 2 Then
                Dim key = parts(0).Trim()
                Dim value = parts(1).Trim()

                ' Check if this is a board name line
                If key.Contains(".name") AndAlso Not key.Contains("menu.") Then
                    Dim boardId = key.Substring(0, key.IndexOf("."))

                    ' Check if this is an ESP32 board
                    If boardId.StartsWith("esp32") Then
                        ' Add the board if it's not already in the dictionary
                        If Not Boards.ContainsKey(value) Then
                            Boards.Add(value, New Dictionary(Of String, String)())
                            Boards(value).Add("id", boardId)
                            Boards(value).Add("name", value)
                        End If
                    End If
                End If
            End If
        Next

        ' Then load all partition schemes
        Dim partitionPattern As New Regex("esp32\.menu\.PartitionScheme\.([^.]+)\.name=(.*)")
        For Each line In lines
            Dim match = partitionPattern.Match(line)
            If match.Success Then
                Dim schemeId = match.Groups(1).Value
                Dim schemeName = match.Groups(2).Value

                ' Add to partition schemes dictionary
                If Not PartitionSchemes.ContainsKey(schemeName) Then
                    PartitionSchemes.Add(schemeName, schemeId)
                End If
            End If
        Next

        ' Now load detailed configuration for each board
        For Each boardName In Boards.Keys.ToList()
            Dim boardId = Boards(boardName)("id")

            ' Find all properties for this board
            For Each line In lines
                ' Skip comments and empty lines
                If line.Trim().StartsWith("#") OrElse String.IsNullOrWhiteSpace(line) Then
                    Continue For
                End If

                ' Parse key-value pairs
                Dim parts = line.Split(New Char() {"="c}, 2)
                If parts.Length = 2 Then
                    Dim key = parts(0).Trim()
                    Dim value = parts(1).Trim()

                    ' Check if this property belongs to our board
                    If key.StartsWith(boardId & ".") Then
                        ' Extract the property name
                        Dim propKey = key.Substring(boardId.Length + 1)

                        ' Add the property to the board dictionary
                        If Not Boards(boardName).ContainsKey(propKey) Then
                            Boards(boardName).Add(propKey, value)
                        End If
                    End If
                End If
            Next
        Next

        ' Add custom partition scheme option
        PartitionSchemes.Add("Custom Partition File", "custom")
    End Sub

    ' Find boards.txt in common locations
    Private Sub FindBoardsFile()
        Dim commonLocations As New List(Of String)

        ' Check Arduino IDE locations
        Dim arduinoPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)

        ' Arduino 2.x location
        commonLocations.Add(Path.Combine(arduinoPath, "Arduino15", "packages", "esp32", "hardware", "esp32", "2.0.14", "boards.txt"))
        commonLocations.Add(Path.Combine(arduinoPath, "Arduino15", "packages", "esp32", "hardware", "esp32", "2.0.13", "boards.txt"))
        commonLocations.Add(Path.Combine(arduinoPath, "Arduino15", "packages", "esp32", "hardware", "esp32", "2.0.12", "boards.txt"))

        ' Arduino 1.8.x location
        commonLocations.Add(Path.Combine(arduinoPath, "Arduino15", "packages", "esp32", "hardware", "esp32", "1.0.6", "boards.txt"))
        commonLocations.Add(Path.Combine(arduinoPath, "Arduino15", "packages", "esp32", "hardware", "esp32", "1.0.5", "boards.txt"))

        ' Look for latest version if specific versions not found
        Dim esp32HardwarePath = Path.Combine(arduinoPath, "Arduino15", "packages", "esp32", "hardware", "esp32")
        If Directory.Exists(esp32HardwarePath) Then
            Dim versionDirs = Directory.GetDirectories(esp32HardwarePath)
            If versionDirs.Length > 0 Then
                ' Sort to get the latest version
                Array.Sort(versionDirs)
                commonLocations.Add(Path.Combine(versionDirs(versionDirs.Length - 1), "boards.txt"))
            End If
        End If

        ' Check Arduino IDE program files location
        Dim programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        commonLocations.Add(Path.Combine(programFiles, "Arduino", "hardware", "espressif", "esp32", "boards.txt"))

        ' Check if Arduino IDE is installed in the user's Documents folder
        Dim documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        commonLocations.Add(Path.Combine(documentsPath, "Arduino", "hardware", "espressif", "esp32", "boards.txt"))

        ' Try each location
        For Each location In commonLocations
            If File.Exists(location) Then
                BoardsFilePath = location
                Return
            End If
        Next

        ' If not found in common locations, check if there's any boards.txt in the Arduino15 folder
        If Directory.Exists(Path.Combine(arduinoPath, "Arduino15")) Then
            Dim boardsFiles = Directory.GetFiles(Path.Combine(arduinoPath, "Arduino15"), "boards.txt", SearchOption.AllDirectories)
            If boardsFiles.Length > 0 Then
                ' Use the first one found
                BoardsFilePath = boardsFiles(0)
            End If
        End If
    End Sub

    ' Get a list of available board names
    Public Function GetBoardNames() As List(Of String)
        Dim boardNames = New List(Of String)

        For Each board In Boards.Keys
            boardNames.Add(board)
        Next

        ' Sort alphabetically for better UI
        boardNames.Sort()

        Return boardNames
    End Function

    ' Get a list of available partition schemes
    Public Function GetPartitionSchemes() As List(Of String)
        Dim schemeNames = New List(Of String)

        For Each scheme In PartitionSchemes.Keys
            schemeNames.Add(scheme)
        Next

        ' Sort alphabetically but put "default" at the top
        schemeNames.Sort()

        ' Move "default" to the top if it exists
        If schemeNames.Contains("Default") Then
            schemeNames.Remove("Default")
            schemeNames.Insert(0, "Default")
        End If

        ' Move "Custom Partition File" to the end
        If schemeNames.Contains("Custom Partition File") Then
            schemeNames.Remove("Custom Partition File")
            schemeNames.Add("Custom Partition File")
        End If

        Return schemeNames
    End Function

    ' Get FQBN (Fully Qualified Board Name) for Arduino CLI
    Public Function GetFQBN(boardName As String) As String
        If Not Boards.ContainsKey(boardName) Then
            Return ""
        End If

        Dim boardId = Boards(boardName)("id")
        Dim fqbn = FQBN_PREFIX & boardId

        ' Add partition scheme if set
        If Not String.IsNullOrEmpty(selectedPartition) Then
            Dim partitionId = GetPartitionId(selectedPartition)
            If Not String.IsNullOrEmpty(partitionId) Then
                fqbn &= ":PartitionScheme=" & partitionId
            End If
        End If

        ' Add other board-specific parameters here if needed
        ' Example: CPU frequency, Flash size, etc.

        Return fqbn
    End Function

    ' Get the partition scheme ID from the name
    Private Function GetPartitionId(partitionName As String) As String
        If PartitionSchemes.ContainsKey(partitionName) Then
            Return PartitionSchemes(partitionName)
        End If

        Return DEFAULT_PARTITION
    End Function

    ' Set the selected board
    Public Sub SetSelectedBoard(boardName As String)
        selectedBoard = boardName
    End Sub

    ' Get the selected board
    Public Function GetSelectedBoard() As String
        Return selectedBoard
    End Function

    ' Set the selected partition scheme
    Public Sub SetSelectedPartition(partitionName As String)
        selectedPartition = partitionName
    End Sub

    ' Get the selected partition scheme
    Public Function GetSelectedPartition() As String
        Return selectedPartition
    End Function

    ' Set the custom partition file path
    Public Sub SetCustomPartitionFile(filePath As String)
        customPartitionFile = filePath
    End Sub

    ' Get the custom partition file path
    Public Function GetCustomPartitionFile() As String
        Return customPartitionFile
    End Function

    ' Get all configuration options for a specific board
    Public Function GetBoardConfigOptions(boardName As String) As Dictionary(Of String, String)
        If Boards.ContainsKey(boardName) Then
            Return Boards(boardName)
        End If

        Return New Dictionary(Of String, String)
    End Function

    ' Check if a board has a specific property
    Public Function HasBoardProperty(boardName As String, propertyName As String) As Boolean
        If Boards.ContainsKey(boardName) Then
            Return Boards(boardName).ContainsKey(propertyName)
        End If

        Return False
    End Function

    ' Get a specific property value for a board
    Public Function GetBoardProperty(boardName As String, propertyName As String) As String
        If Boards.ContainsKey(boardName) AndAlso Boards(boardName).ContainsKey(propertyName) Then
            Return Boards(boardName)(propertyName)
        End If

        Return ""
    End Function

    ' Get all available menu options for a specific board (CPU, Flash, etc.)
    Public Function GetBoardMenuOptions(boardName As String) As Dictionary(Of String, Dictionary(Of String, String))
        Dim menuOptions = New Dictionary(Of String, Dictionary(Of String, String))

        If Not Boards.ContainsKey(boardName) Then
            Return menuOptions
        End If

        ' Extract all menu options
        Dim boardId = Boards(boardName)("id")
        Dim menuPattern = New Regex(boardId & "\.menu\.([^.]+)\.([^.]+)\.name=(.*)")

        For Each kvp In Boards(boardName)
            Dim key = kvp.Key
            Dim value = kvp.Value

            ' Check if this is a menu option
            If key.StartsWith("menu.") Then
                Dim parts = key.Split(New Char() {"."c}, 4)
                If parts.Length >= 4 Then
                    Dim menuCategory = parts(1)
                    Dim menuOption = parts(2)
                    Dim menuProperty = parts(3)

                    ' Create category if it doesn't exist
                    If Not menuOptions.ContainsKey(menuCategory) Then
                        menuOptions.Add(menuCategory, New Dictionary(Of String, String)())
                    End If

                    ' Add the option if it's a name property
                    If menuProperty = "name" Then
                        menuOptions(menuCategory).Add(menuOption, value)
                    End If
                End If
            End If
        Next

        Return menuOptions
    End Function

    ' Get compile/upload flags for the selected board and partition
    Public Function GetCompileFlags() As Dictionary(Of String, String)
        Dim flags = New Dictionary(Of String, String)

        ' Add board-specific flags
        If Not String.IsNullOrEmpty(selectedBoard) AndAlso Boards.ContainsKey(selectedBoard) Then
            Dim boardId = Boards(selectedBoard)("id")
            flags.Add("board", boardId)

            ' Add FQBN
            flags.Add("fqbn", GetFQBN(selectedBoard))
        End If

        ' Add partition scheme flag
        If Not String.IsNullOrEmpty(selectedPartition) Then
            Dim partitionId = GetPartitionId(selectedPartition)
            flags.Add("partition", partitionId)

            ' Add custom partition file if needed
            If partitionId = "custom" AndAlso Not String.IsNullOrEmpty(customPartitionFile) Then
                flags.Add("partitionFile", customPartitionFile)
            End If
        End If

        Return flags
    End Function
End Class