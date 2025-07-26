Imports System
Imports System.Web
Imports System.IO

Namespace KC_LINK_WebUploader
    Public Class GlobalApplication
        Inherits HttpApplication

        Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
            ' Initialize application settings
            InitializeSettings()

            ' Create required directories
            CreateRequiredDirectories()

            ' Log application start
            LogMessage($"Application started at 2025-07-25 00:30:36 UTC by Chamil1983")
        End Sub

        Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
            ' Log application end
            LogMessage($"Application ended at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
        End Sub

        Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
            ' Get the exception
            Dim ex As Exception = Server.GetLastError()

            ' Log the error
            LogMessage($"Unhandled error: {ex.Message}")
            LogMessage($"Stack trace: {ex.StackTrace}")
        End Sub

        Private Sub InitializeSettings()
            ' Ensure default settings are set
            If String.IsNullOrEmpty(My.Settings.ArduinoCliPath) Then
                My.Settings.ArduinoCliPath = "C:\arduino-cli\arduino-cli.exe"
            End If

            If String.IsNullOrEmpty(My.Settings.DefaultSketchDir) Then
                My.Settings.DefaultSketchDir = "C:\Temp\ArduinoSketches"
            End If

            If My.Settings.CompileTimeout <= 0 Then
                My.Settings.CompileTimeout = 300
            End If

            If String.IsNullOrEmpty(My.Settings.DefaultBoard) Then
                My.Settings.DefaultBoard = "KC-Link PRO A8 (Default)"
            End If

            If String.IsNullOrEmpty(My.Settings.DefaultPartition) Then
                My.Settings.DefaultPartition = "default"
            End If

            My.Settings.Save()
        End Sub

        Private Sub CreateRequiredDirectories()
            Try
                ' Create application directories
                Dim appDataPath As String = Server.MapPath("~/App_Data")
                Dim projectsPath As String = Path.Combine(appDataPath, "Projects")
                Dim tempPath As String = Path.Combine(appDataPath, "Temp")
                Dim logsPath As String = Path.Combine(appDataPath, "logs")
                Dim partitionsPath As String = Path.Combine(appDataPath, "partitions")

                EnsureDirectoryExists(appDataPath)
                EnsureDirectoryExists(projectsPath)
                EnsureDirectoryExists(tempPath)
                EnsureDirectoryExists(logsPath)
                EnsureDirectoryExists(partitionsPath)
            Catch ex As Exception
                ' Log the error
                System.Diagnostics.Debug.WriteLine($"Error creating directories: {ex.Message}")
            End Try
        End Sub

        Private Sub EnsureDirectoryExists(path As String)
            If Not Directory.Exists(path) Then
                Directory.CreateDirectory(path)
            End If
        End Sub

        Private Sub LogMessage(message As String)
            Try
                ' We need to check My.Settings.EnableLogging, but if it's not initialized yet, default to true
                Dim loggingEnabled As Boolean = True

                ' Try to get the setting if it's available
                Try
                    loggingEnabled = My.Settings.EnableLogging
                Catch
                    ' Use default if setting is not available
                End Try

                If loggingEnabled Then
                    Dim logFile As String = Server.MapPath("~/App_Data/logs/app.log")
                    Dim logDir As String = Path.GetDirectoryName(logFile)

                    If Not Directory.Exists(logDir) Then
                        Directory.CreateDirectory(logDir)
                    End If

                    Using writer As New StreamWriter(logFile, True)
                        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}")
                    End Using
                End If
            Catch ex As Exception
                ' Last resort error logging
                System.Diagnostics.Debug.WriteLine($"Error writing to log: {ex.Message}")
            End Try
        End Sub
    End Class
End Namespace