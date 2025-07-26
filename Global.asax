<%@ Application Language="VB" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Configuration" %>

<script runat="server">
    ' Default settings
    Private Const DEFAULT_ARDUINO_CLI_PATH As String = "C:\arduino-cli\arduino-cli.exe"
    Private Const DEFAULT_SKETCH_DIR As String = "C:\Temp\ArduinoSketches"
    Private Const DEFAULT_COMPILE_TIMEOUT As Integer = 300
    Private Const DEFAULT_BOARD As String = "KC-Link PRO A8 (Default)"
    Private Const DEFAULT_PARTITION As String = "default"
    
    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Initialize application settings
        InitializeSettings()
        
        ' Create required directories
        CreateRequiredDirectories()
        
        ' Log application start
        LogMessage("Application started at 2025-07-25 00:46:31 UTC by Chamil1983")
    End Sub
    
    Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Log application end
        LogMessage("Application ended at " & DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") & " UTC")
    End Sub
    
    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        ' Get the exception
        Dim ex As Exception = Server.GetLastError()
        
        ' Log the error
        LogMessage("Unhandled error: " & ex.Message)
        LogMessage("Stack trace: " & ex.StackTrace)
    End Sub
    
    Private Sub InitializeSettings()
        ' Store default settings in Application state if not already set
        If Application("ArduinoCliPath") Is Nothing Then
            Application("ArduinoCliPath") = GetAppSetting("ArduinoCliPath", DEFAULT_ARDUINO_CLI_PATH)
        End If
        
        If Application("DefaultSketchDir") Is Nothing Then
            Application("DefaultSketchDir") = GetAppSetting("DefaultSketchDir", DEFAULT_SKETCH_DIR)
        End If
        
        If Application("CompileTimeout") Is Nothing Then
            Dim timeoutStr As String = GetAppSetting("CompileTimeout", DEFAULT_COMPILE_TIMEOUT.ToString())
            Dim timeout As Integer
            If Integer.TryParse(timeoutStr, timeout) Then
                Application("CompileTimeout") = timeout
            Else
                Application("CompileTimeout") = DEFAULT_COMPILE_TIMEOUT
            End If
        End If
        
        If Application("DefaultBoard") Is Nothing Then
            Application("DefaultBoard") = GetAppSetting("DefaultBoard", DEFAULT_BOARD)
        End If
        
        If Application("DefaultPartition") Is Nothing Then
            Application("DefaultPartition") = GetAppSetting("DefaultPartition", DEFAULT_PARTITION)
        End If
        
        ' Enable logging by default
        If Application("EnableLogging") Is Nothing Then
            Dim enableLoggingStr As String = GetAppSetting("EnableLogging", "true")
            Application("EnableLogging") = (enableLoggingStr.ToLower() = "true")
        End If
    End Sub
    
    ' Helper function to get app settings with default values
    Private Function GetAppSetting(key As String, defaultValue As String) As String
        Dim value As String = ConfigurationManager.AppSettings(key)
        If String.IsNullOrEmpty(value) Then
            Return defaultValue
        End If
        Return value
    End Function
    
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
            System.Diagnostics.Debug.WriteLine("Error creating directories: " & ex.Message)
        End Try
    End Sub
    
    Private Sub EnsureDirectoryExists(path As String)
        If Not Directory.Exists(path) Then
            Directory.CreateDirectory(path)
        End If
    End Sub
    
    Private Sub LogMessage(message As String)
        Try
            ' Check if logging is enabled
            Dim loggingEnabled As Boolean = True
            
            ' Try to get the setting if it's available
            If Application("EnableLogging") IsNot Nothing Then
                loggingEnabled = CBool(Application("EnableLogging"))
            End If
            
            If loggingEnabled Then
                Dim logFile As String = Server.MapPath("~/App_Data/logs/app.log")
                Dim logDir As String = Path.GetDirectoryName(logFile)
                
                If Not Directory.Exists(logDir) Then
                    Directory.CreateDirectory(logDir)
                End If
                
                Using writer As New StreamWriter(logFile, True)
                    writer.WriteLine("[" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "] " & message)
                End Using
            End If
        Catch ex As Exception
            ' Last resort error logging
            System.Diagnostics.Debug.WriteLine("Error writing to log: " & ex.Message)
        End Try
    End Sub
</script>