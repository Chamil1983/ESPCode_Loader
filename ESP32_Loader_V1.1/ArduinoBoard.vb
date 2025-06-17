Imports System.IO

Public Class ArduinoBoard
    Public Property Name As String
    Public Property FQBN As String
    Public Property MenuOptions As New Dictionary(Of String, Dictionary(Of String, String))

    Public Shared Function ParseBoardsTxt(path As String) As List(Of ArduinoBoard)
        Dim boards As New List(Of ArduinoBoard)

        Try
            If Not File.Exists(path) Then
                Throw New FileNotFoundException($"Boards.txt file not found at {path}")
            End If

            Dim lines = File.ReadAllLines(path)
            Dim currentBoard As ArduinoBoard = Nothing

            For Each line In lines
                Try
                    If String.IsNullOrWhiteSpace(line) OrElse line.StartsWith("#") Then Continue For

                    Dim parts = line.Split(New Char() {"="}, 2)
                    If parts.Length < 2 Then Continue For

                    Dim key = parts(0).Trim()
                    Dim value = parts(1).Trim()

                    If Not key.Contains(".") Then Continue For

                    Dim boardParts = key.Split("."c)
                    If boardParts Is Nothing OrElse boardParts.Length < 2 Then Continue For

                    Dim boardId = boardParts(0)
                    If String.IsNullOrEmpty(boardId) Then Continue For

                    ' Find or create board
                    If currentBoard Is Nothing OrElse Not boardId.Equals(currentBoard.FQBN) Then
                        Dim foundBoard = False
                        For Each board In boards
                            If board.FQBN.Equals(boardId) Then
                                currentBoard = board
                                foundBoard = True
                                Exit For
                            End If
                        Next

                        If Not foundBoard Then
                            currentBoard = New ArduinoBoard With {.FQBN = boardId}
                            boards.Add(currentBoard)
                        End If
                    End If

                    ' Parse menu options
                    If boardParts.Length >= 3 AndAlso boardParts(1) = "menu" Then
                        Dim menuCategory = boardParts(2)
                        If String.IsNullOrEmpty(menuCategory) Then Continue For

                        If boardParts.Length = 3 Then
                            ' This is a menu category
                            If Not currentBoard.MenuOptions.ContainsKey(menuCategory) Then
                                currentBoard.MenuOptions.Add(menuCategory, New Dictionary(Of String, String)())
                            End If
                        ElseIf boardParts.Length >= 4 Then
                            ' This is a menu option
                            Dim menuOption = boardParts(3)
                            If String.IsNullOrEmpty(menuOption) Then Continue For

                            If Not currentBoard.MenuOptions.ContainsKey(menuCategory) Then
                                currentBoard.MenuOptions.Add(menuCategory, New Dictionary(Of String, String)())
                            End If

                            If Not currentBoard.MenuOptions(menuCategory).ContainsKey(menuOption) Then
                                currentBoard.MenuOptions(menuCategory).Add(menuOption, value)
                            End If
                        End If
                    ElseIf boardParts.Length = 2 AndAlso boardParts(1) = "name" Then
                        ' Board name
                        currentBoard.Name = value
                    End If
                Catch innerEx As Exception
                    ' Skip problematic lines instead of failing the whole parse
                    System.Diagnostics.Debug.WriteLine($"Warning: Error parsing line: {line}. {innerEx.Message}")
                    Continue For
                End Try
            Next

            ' Final validation - remove boards without names or FQBNs
            For i As Integer = boards.Count - 1 To 0 Step -1
                If String.IsNullOrEmpty(boards(i).Name) OrElse String.IsNullOrEmpty(boards(i).FQBN) Then
                    boards.RemoveAt(i)
                End If
            Next

            If boards.Count = 0 Then
                Throw New InvalidOperationException("No valid boards found in the boards.txt file.")
            End If

            Return boards

        Catch ex As Exception
            Throw New Exception($"Error parsing boards.txt file: {ex.Message}", ex)
        End Try
    End Function
End Class