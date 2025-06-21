Imports System
Imports System.Collections.Generic

Public Class HardwareStats
    ' Statistics properties
    Public Property SketchSize As Long = 0
    Public Property SketchSizePercentage As Integer = 0
    Public Property RAMSize As Long = 0
    Public Property RAMPercentage As Integer = 0
    Public Property LastCompileDuration As TimeSpan = TimeSpan.Zero
    Public Property CompilationCount As Integer = 0
    Public Property SuccessfulCompilations As Integer = 0

    ' History of compilations
    Private compilationHistory As New List(Of CompilationRecord)

    ' Constructor
    Public Sub New()
        ' Initialize with default values
    End Sub

    ' Add a new compilation record
    Public Sub AddCompilation(projectName As String, success As Boolean, duration As TimeSpan)
        Dim record As New CompilationRecord() With {
            .ProjectName = projectName,
            .Success = success,
            .CompilationDate = DateTime.Now,  ' Changed from Date to CompilationDate
            .Duration = duration
        }

        compilationHistory.Add(record)

        ' Update statistics
        CompilationCount += 1
        If success Then
            SuccessfulCompilations += 1
        End If

        LastCompileDuration = duration

        ' Keep only the last 50 compilations in history
        If compilationHistory.Count > 50 Then
            compilationHistory.RemoveAt(0)
        End If
    End Sub

    ' Update sketch size information
    Public Sub UpdateSketchSize(size As Long, percentage As Integer)
        SketchSize = size
        SketchSizePercentage = percentage
    End Sub

    ' Update RAM usage information
    Public Sub UpdateRAMUsage(size As Long, percentage As Integer)
        RAMSize = size
        RAMPercentage = percentage
    End Sub

    ' Get compilation success rate as percentage
    Public ReadOnly Property CompilationSuccessRate() As Integer
        Get
            If CompilationCount = 0 Then
                Return 100
            End If

            Return CInt((SuccessfulCompilations / CompilationCount) * 100)
        End Get
    End Property

    ' Get average compilation duration
    Public ReadOnly Property AverageCompilationTime() As TimeSpan
        Get
            If compilationHistory.Count = 0 Then
                Return TimeSpan.Zero
            End If

            Dim totalSeconds As Double = 0

            For Each record In compilationHistory
                totalSeconds += record.Duration.TotalSeconds
            Next

            Return TimeSpan.FromSeconds(totalSeconds / compilationHistory.Count)
        End Get
    End Property

    ' Get list of recent compilations
    Public ReadOnly Property RecentCompilations(count As Integer) As List(Of CompilationRecord)
        Get
            Dim result As New List(Of CompilationRecord)
            Dim startIndex = Math.Max(0, compilationHistory.Count - count)

            For i As Integer = startIndex To compilationHistory.Count - 1
                result.Add(compilationHistory(i))
            Next

            Return result
        End Get
    End Property

    ' Inner class for compilation records
    Public Class CompilationRecord
        Public Property ProjectName As String
        Public Property Success As Boolean
        Public Property CompilationDate As DateTime  ' Renamed from Date to CompilationDate
        Public Property Duration As TimeSpan
    End Class
End Class