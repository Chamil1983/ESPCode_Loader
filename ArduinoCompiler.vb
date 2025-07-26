Imports System
Imports System.IO
Imports System.Diagnostics
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web

Public Class ArduinoCompiler
    ' Private fields
    Private _compilerPath As String
    Private _projectPath As String
    Private _outputLog As New System.Text.StringBuilder()
    Private _processExitCode As Integer = -1
    Private _compilationStatus As CompilationStatus = CompilationStatus.NotStarted
    Private _currentProgress As Integer = 0
    Private _currentPhase As String = ""
    Private _cancellationTokenSource As CancellationTokenSource = Nothing

    ' Public properties
    Public ReadOnly Property OutputLog As String
        Get
            Return _outputLog.ToString()
        End Get
    End Property

    Public ReadOnly Property Status As CompilationStatus
        Get
            Return _compilationStatus
        End Get
    End Property

    Public ReadOnly Property ExitCode As Integer
        Get
            Return _processExitCode
        End Get
    End Property

    Public ReadOnly Property Progress As Integer
        Get
            Return _currentProgress
        End Get
    End Property

    Public ReadOnly Property Phase As String
        Get
            Return _currentPhase
        End Get
    End Property

    ' Compilation status enum
    Public Enum CompilationStatus
        NotStarted
        Running
        Completed
        Failed
        Cancelled
    End Enum

    ' Constructor
    Public Sub New(compilerPath As String)
        _compilerPath = compilerPath
    End Sub

    ' Compile the project
    Public Async Function CompileProjectAsync(projectPath As String, fqbn As String) As Task(Of Boolean)
        If String.IsNullOrEmpty(_compilerPath) OrElse Not File.Exists(_compilerPath) Then
            _outputLog.AppendLine("Error: Arduino CLI not found.")
            _compilationStatus = CompilationStatus.Failed
            Return False
        End If

        If String.IsNullOrEmpty(projectPath) OrElse Not Directory.Exists(projectPath) Then
            _outputLog.AppendLine("Error: Project directory not found.")
            _compilationStatus = CompilationStatus.Failed
            Return False
        End If

        _projectPath = projectPath
        _outputLog.Clear()
        _currentProgress = 0
        _currentPhase = "Initializing"
        _compilationStatus = CompilationStatus.Running

        ' Create cancellation token source
        _cancellationTokenSource = New CancellationTokenSource()

        Try
            ' Build command arguments
            Dim arguments = $"compile -v --fqbn {fqbn} ""{projectPath}"""

            ' Log the command
            _outputLog.AppendLine($"Running: {_compilerPath} {arguments}")

            ' Create process
            Dim process As New Process()
            process.StartInfo.FileName = _compilerPath
            process.StartInfo.Arguments = arguments
            process.StartInfo.UseShellExecute = False
            process.StartInfo.RedirectStandardOutput = True
            process.StartInfo.RedirectStandardError = True
            process.StartInfo.CreateNoWindow = True

            ' Set up output handlers
            AddHandler process.OutputDataReceived, AddressOf OnOutputDataReceived
            AddHandler process.ErrorDataReceived, AddressOf OnErrorDataReceived

            ' Start process
            process.Start()
            process.BeginOutputReadLine()
            process.BeginErrorReadLine()

            ' Wait for process to complete or be cancelled
            Await Task.Run(Sub()
                               Try
                                   ' Check for cancellation while waiting
                                   While Not process.HasExited
                                       If _cancellationTokenSource.Token.IsCancellationRequested Then
                                           Try
                                               process.Kill()
                                           Catch ex As Exception
                                               ' Process may have exited already
                                           End Try
                                           _compilationStatus = CompilationStatus.Cancelled
                                           _outputLog.AppendLine("Compilation cancelled.")
                                           Exit While
                                       End If
                                       Thread.Sleep(100)
                                   End While

                                   If Not _cancellationTokenSource.Token.IsCancellationRequested Then
                                       process.WaitForExit()
                                       _processExitCode = process.ExitCode

                                       If _processExitCode = 0 Then
                                           _compilationStatus = CompilationStatus.Completed
                                           _currentProgress = 100
                                           _currentPhase = "Completed"
                                           _outputLog.AppendLine("Compilation completed successfully.")
                                       Else
                                           _compilationStatus = CompilationStatus.Failed
                                           _currentPhase = "Failed"
                                           _outputLog.AppendLine($"Compilation failed with exit code: {_processExitCode}")
                                       End If
                                   End If
                               Catch ex As Exception
                                   _compilationStatus = CompilationStatus.Failed
                                   _currentPhase = "Error"
                                   _outputLog.AppendLine($"Error during compilation: {ex.Message}")
                               End Try
                           End Sub, _cancellationTokenSource.Token)

            ' Clean up event handlers
            RemoveHandler process.OutputDataReceived, AddressOf OnOutputDataReceived
            RemoveHandler process.ErrorDataReceived, AddressOf OnErrorDataReceived

            Return _processExitCode = 0

        Catch ex As Exception
            _compilationStatus = CompilationStatus.Failed
            _currentPhase = "Error"
            _outputLog.AppendLine($"Error starting compilation: {ex.Message}")
            Return False
        End Try
    End Function

    ' Cancel the running compilation
    Public Sub CancelCompilation()
        If _compilationStatus = CompilationStatus.Running Then
            _cancellationTokenSource?.Cancel()
        End If
    End Sub

    ' Extract compile statistics from output
    Public Function ExtractCompilationStats() As Dictionary(Of String, Object)
        Dim stats As New Dictionary(Of String, Object)

        ' Default values
        stats("SketchSize") = 0
        stats("SketchSizePercentage") = 0
        stats("RAMSize") = 0
        stats("RAMPercentage") = 0
        stats("CompilationSuccess") = (_compilationStatus = CompilationStatus.Completed)

        ' Extract binary size statistics from output log
        ' Example: "Sketch uses 345678 bytes (26%) of program storage space."
        Try
            Dim sketchSizeMatch As Match = Regex.Match(OutputLog, "Sketch uses (\d+) bytes \((\d+)%\) of program storage space")
            If sketchSizeMatch.Success Then
                stats("SketchSize") = Long.Parse(sketchSizeMatch.Groups(1).Value)
                stats("SketchSizePercentage") = Integer.Parse(sketchSizeMatch.Groups(2).Value)
            End If

            ' Extract RAM usage if available
            Dim ramMatch As Match = Regex.Match(OutputLog, "Global variables use (\d+) bytes \((\d+)%\) of dynamic memory")
            If ramMatch.Success Then
                stats("RAMSize") = Long.Parse(ramMatch.Groups(1).Value)
                stats("RAMPercentage") = Integer.Parse(ramMatch.Groups(2).Value)
            End If
        Catch ex As Exception
            ' Ignore parsing errors
        End Try

        Return stats
    End Function

    ' Event handlers for process output
    Private Sub OnOutputDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            _outputLog.AppendLine(e.Data)
            UpdateProgressFromOutput(e.Data)
        End If
    End Sub

    Private Sub OnErrorDataReceived(sender As Object, e As DataReceivedEventArgs)
        If Not String.IsNullOrEmpty(e.Data) Then
            _outputLog.AppendLine($"ERROR: {e.Data}")
        End If
    End Sub

    ' Update progress based on output
    Private Sub UpdateProgressFromOutput(line As String)
        ' Detect compilation phases from output
        Dim lowerLine = line.ToLower()

        ' Determine which phase we're in based on output
        If lowerLine.Contains("verifying") Then
            _currentPhase = "Verifying sketch"
            _currentProgress = 10
        ElseIf lowerLine.Contains("detecting libraries") Then
            _currentPhase = "Detecting libraries"
            _currentProgress = 20
        ElseIf lowerLine.Contains("sketch uses") OrElse lowerLine.Contains("compiling") Then
            _currentPhase = "Compiling"
            _currentProgress = 30
        ElseIf lowerLine.Contains("linking") Then
            _currentPhase = "Linking"
            _currentProgress = 50
        ElseIf lowerLine.Contains("generating binary") Then
            _currentPhase = "Generating binary"
            _currentProgress = 70
        ElseIf lowerLine.Contains("flash size") Then
            _currentPhase = "Preparing flash"
            _currentProgress = 80
        ElseIf lowerLine.Contains("done compiling") Then
            _currentPhase = "Compilation complete"
            _currentProgress = 100
        End If

        ' Extract explicit progress percentage if available
        If lowerLine.Contains("%") Then
            Try
                Dim match As Match = Regex.Match(lowerLine, "(\d+)%")
                If match.Success Then
                    Dim percentage As Integer = Integer.Parse(match.Groups(1).Value)
                    _currentProgress = percentage
                End If
            Catch
                ' Ignore parsing errors
            End Try
        End If
    End Sub
End Class