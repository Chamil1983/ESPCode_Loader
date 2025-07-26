Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Threading.Tasks
Imports System.Web
Imports System.Web.Services
Imports System.Web.UI

Public Class Compilation
    Inherits System.Web.UI.Page

    Private Shared _compiler As ArduinoCompiler
    Private Shared _compilerStarted As Boolean = False
    Private Shared _projectInfo As Dictionary(Of String, String)
    Private Shared _resourceStats As Dictionary(Of String, Object)

    Private _boardManager As BoardManager
    Private _projectManager As ProjectManager

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ' Initialize managers
        _boardManager = New BoardManager()
        _projectManager = New ProjectManager()

        If Not IsPostBack Then
            ' Initialize compiler
            _compiler = New ArduinoCompiler(My.Settings.ArduinoCliPath)
            _compilerStarted = False
            _resourceStats = New Dictionary(Of String, Object)()
            _projectInfo = New Dictionary(Of String, String)()

            ' Get project name from query string or session
            Dim projectName As String = ""
            If Request.QueryString("project") IsNot Nothing Then
                projectName = Request.QueryString("project")
            ElseIf Session("SelectedProject") IsNot Nothing Then
                projectName = Session("SelectedProject").ToString()
            End If

            ' Get board and partition from session
            Dim boardName As String = "ESP32 Dev Module"
            Dim partitionScheme As String = "default"

            If Session("SelectedBoard") IsNot Nothing Then
                boardName = Session("SelectedBoard").ToString()
            End If

            If Session("SelectedPartition") IsNot Nothing Then
                partitionScheme = Session("SelectedPartition").ToString()
            End If

            ' Store info for shared use
            _projectInfo("ProjectName") = projectName
            _projectInfo("BoardName") = boardName
            _projectInfo("PartitionScheme") = partitionScheme

            ' Update UI with project info
            lblProjectName.Text = projectName
            lblBoardName.Text = boardName
            lblPartition.Text = partitionScheme

            ' Get project path
            Dim projectPath = Path.Combine(Server.MapPath("~/App_Data/Projects"), projectName)
            _projectInfo("ProjectPath") = projectPath

            ' Check if project exists
            If Not Directory.Exists(projectPath) Then
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                    "alert('Project not found. Please select a valid project.');", True)
                Response.Redirect("Default.aspx")
                Return
            End If

            ' Get FQBN for selected board
            Dim fqbn = _boardManager.GetFQBN(boardName)

            ' Apply partition scheme if specified
            If partitionScheme <> "default" AndAlso partitionScheme <> "custom" Then
                fqbn = _boardManager.ApplyPartitionScheme(fqbn, partitionScheme)
            ElseIf partitionScheme = "custom" Then
                fqbn = _boardManager.ApplyCustomPartitionFile(fqbn)
            End If

            _projectInfo("FQBN") = fqbn
        End If
    End Sub

    Protected Sub btnStartCompile_Click(sender As Object, e As EventArgs)
        ' Disable UI during compilation
        btnStartCompile.Enabled = False
        btnStartCompile.Text = "Compiling..."
        btnCancel.Enabled = True
        btnDownloadBinary.Enabled = False

        ' Start compilation in background
        StartCompilation()

        ' Register script to start progress checking
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "startProgressCheck",
            "startProgressCheck();", True)
    End Sub

    Protected Sub btnCancel_Click(sender As Object, e As EventArgs)
        ' Cancel compilation if running
        _compiler?.CancelCompilation()

        ' Update UI
        btnStartCompile.Enabled = True
        btnStartCompile.Text = "Start Compilation"
        btnCancel.Enabled = False

        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "stopProgressCheck",
            "stopProgressCheck(); $('#statusLabel').text('Cancelled');", True)
    End Sub

    Protected Sub btnDownloadBinary_Click(sender As Object, e As EventArgs)
        ' Find compiled binary file
        Dim projectPath = _projectInfo("ProjectPath")
        Dim projectName = _projectInfo("ProjectName")

        ' The binary is typically stored in the build directory
        Dim buildDir = Path.Combine(projectPath, "build")
        Dim binaryFilePath = String.Empty

        If Directory.Exists(buildDir) Then
            ' Try to find the binary (.bin) file
            Dim binFiles = Directory.GetFiles(buildDir, "*.bin", SearchOption.AllDirectories)

            If binFiles.Length > 0 Then
                binaryFilePath = binFiles(0)
            Else
                ' Try to find ELF file if no BIN file
                Dim elfFiles = Directory.GetFiles(buildDir, "*.elf", SearchOption.AllDirectories)
                If elfFiles.Length > 0 Then
                    binaryFilePath = elfFiles(0)
                End If
            End If
        End If

        If String.IsNullOrEmpty(binaryFilePath) OrElse Not File.Exists(binaryFilePath) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Binary file not found. Please compile the project first.');", True)
            Return
        End If

        ' Download the binary file
        Response.Clear()
        Response.ContentType = "application/octet-stream"
        Response.AddHeader("Content-Disposition", $"attachment; filename={Path.GetFileName(binaryFilePath)}")
        Response.TransmitFile(binaryFilePath)
        Response.Flush()
        Response.End()
    End Sub

    Protected Sub btnBack_Click(sender As Object, e As EventArgs)
        Response.Redirect("Default.aspx")
    End Sub

    Private Async Sub StartCompilation()
        ' Set compilation started flag
        _compilerStarted = True

        ' Reset resource stats
        _resourceStats = New Dictionary(Of String, Object)() From {
            {"FlashSize", 0},
            {"FlashPercentage", 0},
            {"RAMSize", 0},
            {"RAMPercentage", 0}
        }

        ' Start compilation in background
        Await Task.Run(Async Function()
                           Dim projectPath = _projectInfo("ProjectPath")
                           Dim fqbn = _projectInfo("FQBN")

                           ' Run the compilation
                           Await _compiler.CompileProjectAsync(projectPath, fqbn)

                           ' Extract compilation statistics
                           Dim stats = _compiler.ExtractCompilationStats()

                           ' Update resource stats for UI
                           _resourceStats = stats
                       End Function)
    End Sub

    <WebMethod>
    Public Shared Function GetCompilationProgress() As Dictionary(Of String, Object)
        ' Create result object
        Dim result As New Dictionary(Of String, Object)()

        ' Add project info
        If _projectInfo IsNot Nothing Then
            For Each key In _projectInfo.Keys
                result(key) = _projectInfo(key)
            Next
        End If

        ' Add resource stats
        If _resourceStats IsNot Nothing Then
            For Each key In _resourceStats.Keys
                result(key) = _resourceStats(key)
            Next
        End If

        ' Add compilation progress info if compiler exists
        If _compiler IsNot Nothing Then
            result("Status") = _compiler.Status.ToString()
            result("Progress") = _compiler.Progress
            result("Phase") = _compiler.Phase
            result("OutputLog") = _compiler.OutputLog
        Else
            result("Status") = "NotStarted"
            result("Progress") = 0
            result("Phase") = "Not started"
            result("OutputLog") = ""
        End If

        ' Add flag to indicate if compilation has been started
        result("Started") = _compilerStarted

        Return result
    End Function
End Class
