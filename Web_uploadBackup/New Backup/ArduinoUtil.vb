Imports System.Diagnostics
Imports System.IO
Imports System.Text

Public Class ArduinoUtil
    Public Class ExecResult
        Public Property Output As String
        Public Property Success As Boolean
    End Class

    ' Delegate for real-time output event
    Public Delegate Sub OutputHandler(outputLine As String)

    ' Real-time compile with output callback
    Public Shared Function RunCompileRealtime(cliPath As String, projDir As String, fqbn As String, outputCb As OutputHandler) As ExecResult
        Dim args = $"compile --fqbn {fqbn} -v ""{projDir}"""
        Return RunArduinoCliRealtime(cliPath, args, projDir, outputCb)
    End Function

    ' Real-time upload with output callback
    Public Shared Function RunUploadRealtime(cliPath As String, projDir As String, fqbn As String, port As String, outputCb As OutputHandler) As ExecResult
        Dim args = $"compile --fqbn {fqbn} --upload --port {port} -v ""{projDir}"""
        Return RunArduinoCliRealtime(cliPath, args, projDir, outputCb)
    End Function

    ' Core function for real-time output
    Private Shared Function RunArduinoCliRealtime(cliPath As String, args As String, workingDir As String, outputCb As OutputHandler) As ExecResult
        Dim result As New ExecResult()
        Dim sb As New StringBuilder()
        Try
            Dim psi As New ProcessStartInfo(cliPath, args)
            psi.UseShellExecute = False
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.CreateNoWindow = True
            psi.WorkingDirectory = workingDir

            Using proc As New Process()
                proc.StartInfo = psi

                AddHandler proc.OutputDataReceived, Sub(sender, e)
                                                        If e.Data IsNot Nothing Then
                                                            sb.AppendLine(e.Data)
                                                            If outputCb IsNot Nothing Then outputCb(e.Data)
                                                        End If
                                                    End Sub
                AddHandler proc.ErrorDataReceived, Sub(sender, e)
                                                       If e.Data IsNot Nothing Then
                                                           sb.AppendLine(e.Data)
                                                           If outputCb IsNot Nothing Then outputCb(e.Data)
                                                       End If
                                                   End Sub

                proc.Start()
                proc.BeginOutputReadLine()
                proc.BeginErrorReadLine()
                proc.WaitForExit()
                result.Success = (proc.ExitCode = 0)
            End Using
            result.Output = sb.ToString()
        Catch ex As Exception
            result.Output = "Error: " & ex.Message
            result.Success = False
        End Try
        Return result
    End Function

    ' Additional method to support binary export
    ' Binary export with output callback - using the correct flags
    Public Shared Function RunBinaryExportRealtime(cliPath As String, projDir As String, fqbn As String, exportDir As String, outputCb As OutputHandler) As ExecResult
        ' Use the -e flag (--export-binaries) and --build-path options which are supported
        Dim args = $"compile --fqbn {fqbn} -v -e --build-path ""{exportDir}"" ""{projDir}"""
        Return RunArduinoCliRealtime(cliPath, args, projDir, outputCb)
    End Function

    ' Upload binary file directly with output callback
    Public Shared Function RunBinaryUploadRealtime(cliPath As String, binaryFile As String, fqbn As String, port As String, verify As Boolean, outputCb As OutputHandler) As ExecResult
        ' arduino-cli upload command with binary file
        Dim verifyFlag = If(verify, " -t", "")
        Dim args = $"upload -i ""{binaryFile}"" --fqbn {fqbn} --port {port}{verifyFlag} -v"
        Return RunArduinoCliRealtime(cliPath, args, Path.GetDirectoryName(binaryFile), outputCb)
    End Function
End Class
