Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Web
Imports System.IO.Compression

Public Class ProjectManager
    ' Private fields
    Private _projectsDirectory As String
    Private _tempDirectory As String
    Private _logFile As String

    ' Constructor
    Public Sub New()
        ' Set up directories
        _projectsDirectory = HttpContext.Current.Server.MapPath("~/App_Data/Projects")
        _tempDirectory = HttpContext.Current.Server.MapPath("~/App_Data/Temp")
        _logFile = HttpContext.Current.Server.MapPath("~/App_Data/logs/project_manager.log")

        ' Create directories if they don't exist
        If Not Directory.Exists(_projectsDirectory) Then
            Directory.CreateDirectory(_projectsDirectory)
        End If

        If Not Directory.Exists(_tempDirectory) Then
            Directory.CreateDirectory(_tempDirectory)
        End If

        Dim logDir = Path.GetDirectoryName(_logFile)
        If Not Directory.Exists(logDir) Then
            Directory.CreateDirectory(logDir)
        End If
    End Sub

    ' Upload and extract a project archive (ZIP) from HttpPostedFile
    Public Function UploadProject(fileUpload As HttpPostedFile, Optional projectName As String = "") As String
        Try
            If fileUpload Is Nothing OrElse fileUpload.ContentLength = 0 Then
                LogMessage("Upload failed: No file was uploaded")
                Return ""
            End If

            ' Generate a unique project name if none provided
            If String.IsNullOrEmpty(projectName) Then
                projectName = Path.GetFileNameWithoutExtension(fileUpload.FileName)

                ' Make sure the name is valid for a directory
                projectName = MakeValidDirectoryName(projectName)

                ' Add timestamp to ensure uniqueness
                projectName = $"{projectName}_{DateTime.Now:yyyyMMddHHmmss}"
            Else
                projectName = MakeValidDirectoryName(projectName)
            End If

            ' Create project directory
            Dim projectPath = Path.Combine(_projectsDirectory, projectName)
            If Directory.Exists(projectPath) Then
                ' Delete existing directory if it exists
                Directory.Delete(projectPath, True)
            End If
            Directory.CreateDirectory(projectPath)

            ' Save the uploaded file to temp directory
            Dim tempZipPath = Path.Combine(_tempDirectory, $"{projectName}.zip")
            fileUpload.SaveAs(tempZipPath)

            ' Extract the zip file to project directory
            ZipFile.ExtractToDirectory(tempZipPath, projectPath)

            ' Delete the temp file
            File.Delete(tempZipPath)

            ' Find the .ino file
            Dim inoFiles = Directory.GetFiles(projectPath, "*.ino", SearchOption.AllDirectories)
            If inoFiles.Length = 0 Then
                LogMessage($"Error: No .ino file found in uploaded project {projectName}")
                Directory.Delete(projectPath, True)
                Return ""
            End If

            ' If the .ino file is not in the root of the project directory, restructure
            If Path.GetDirectoryName(inoFiles(0)) <> projectPath Then
                RestructureProject(projectPath, inoFiles(0))
            End If

            LogMessage($"Project {projectName} uploaded successfully by Chamil1983 at 2025-07-24 23:26:12")
            Return projectPath

        Catch ex As Exception
            LogMessage($"Error uploading project: {ex.Message}")
            Return ""
        End Try
    End Function

    ' Upload project from file stream (for files created on server)
    Public Function UploadProjectFromStream(stream As Stream, fileName As String, Optional projectName As String = "") As String
        Try
            If stream Is Nothing OrElse stream.Length = 0 Then
                LogMessage("Upload failed: Empty stream provided")
                Return ""
            End If

            ' Generate a unique project name if none provided
            If String.IsNullOrEmpty(projectName) Then
                projectName = Path.GetFileNameWithoutExtension(fileName)

                ' Make sure the name is valid for a directory
                projectName = MakeValidDirectoryName(projectName)

                ' Add timestamp to ensure uniqueness
                projectName = $"{projectName}_{DateTime.Now:yyyyMMddHHmmss}"
            Else
                projectName = MakeValidDirectoryName(projectName)
            End If

            ' Create project directory
            Dim projectPath = Path.Combine(_projectsDirectory, projectName)
            If Directory.Exists(projectPath) Then
                ' Delete existing directory if it exists
                Directory.Delete(projectPath, True)
            End If
            Directory.CreateDirectory(projectPath)

            ' Save the stream to temp directory
            Dim tempZipPath = Path.Combine(_tempDirectory, $"{projectName}.zip")
            Using fileStream As New FileStream(tempZipPath, FileMode.Create)
                ' Reset stream position and copy to file
                stream.Position = 0
                stream.CopyTo(fileStream)
            End Using

            ' Extract the zip file to project directory
            ZipFile.ExtractToDirectory(tempZipPath, projectPath)

            ' Delete the temp file
            File.Delete(tempZipPath)

            ' Find the .ino file
            Dim inoFiles = Directory.GetFiles(projectPath, "*.ino", SearchOption.AllDirectories)
            If inoFiles.Length = 0 Then
                LogMessage($"Error: No .ino file found in uploaded project {projectName}")
                Directory.Delete(projectPath, True)
                Return ""
            End If

            ' If the .ino file is not in the root of the project directory, restructure
            If Path.GetDirectoryName(inoFiles(0)) <> projectPath Then
                RestructureProject(projectPath, inoFiles(0))
            End If

            LogMessage($"Project {projectName} created from stream by Chamil1983 at 2025-07-24 23:26:12")
            Return projectPath

        Catch ex As Exception
            LogMessage($"Error creating project from stream: {ex.Message}")
            Return ""
        End Try
    End Function

    ' Rest of the methods remain the same...
    ' ... (Get projects, GetProjectInfo, DeleteProject, etc.)

    ' Helper method to make a valid directory name
    Private Function MakeValidDirectoryName(name As String) As String
        ' Remove invalid characters
        Dim invalidChars = Path.GetInvalidFileNameChars()
        Dim result = String.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))

        ' Ensure the name is not empty
        If String.IsNullOrEmpty(result) Then
            result = "ArduinoProject"
        End If

        Return result
    End Function

    ' Helper method to restructure a project if the .ino file is in a subdirectory
    Private Sub RestructureProject(projectPath As String, inoFilePath As String)
        Dim inoFileName = Path.GetFileName(inoFilePath)
        Dim inoDirectory = Path.GetDirectoryName(inoFilePath)

        ' If the .ino file is already in the root, nothing to do
        If inoDirectory = projectPath Then
            Return
        End If

        ' Get all files in the .ino file's directory
        Dim filesToMove = Directory.GetFiles(inoDirectory)

        ' Move all files to the project root
        For Each filePath In filesToMove
            Dim fileName = Path.GetFileName(filePath)
            Dim targetPath = Path.Combine(projectPath, fileName)

            ' Skip if target already exists
            If File.Exists(targetPath) Then
                Continue For
            End If

            File.Move(filePath, targetPath)
        Next

        ' Now check if the directory is empty and delete it if it is
        If Directory.GetFiles(inoDirectory).Length = 0 AndAlso Directory.GetDirectories(inoDirectory).Length = 0 Then
            Directory.Delete(inoDirectory)
        End If
    End Sub

    ' Log messages to file
    Private Sub LogMessage(message As String)
        Try
            Using writer As New StreamWriter(_logFile, True)
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}")
            End Using
        Catch
            ' Ignore logging errors
        End Try
    End Sub

    ' Get list of projects
    ' Get list of projects
    Public Function GetProjects() As List(Of ProjectInfo)
        Dim projects As New List(Of ProjectInfo)

        Try
            ' Check if directory exists
            If Not Directory.Exists(_projectsDirectory) Then
                Directory.CreateDirectory(_projectsDirectory)
                Return projects
            End If

            ' Get all subdirectories in the projects directory
            Dim projectDirs = Directory.GetDirectories(_projectsDirectory)

            For Each projectDir As String In projectDirs
                Dim projectName = Path.GetFileName(projectDir)

                ' Look for .ino files in the project directory
                Dim inoFiles = Directory.GetFiles(projectDir, "*.ino", SearchOption.TopDirectoryOnly)

                If inoFiles.Length > 0 Then
                    Dim info As New ProjectInfo()
                    info.Name = projectName
                    info.Path = projectDir
                    info.InoFile = inoFiles(0)
                    info.DateCreated = Directory.GetCreationTime(projectDir)

                    ' Count files by type
                    info.InoCount = Directory.GetFiles(projectDir, "*.ino", SearchOption.AllDirectories).Length
                    info.CppCount = Directory.GetFiles(projectDir, "*.cpp", SearchOption.AllDirectories).Length
                    info.HeaderCount = Directory.GetFiles(projectDir, "*.h", SearchOption.AllDirectories).Length

                    ' Count lines of code in main .ino file
                    If File.Exists(info.InoFile) Then
                        info.MainFileLines = File.ReadAllLines(info.InoFile).Length
                    End If

                    projects.Add(info)
                End If
            Next

            ' Sort by date created (newest first)
            projects.Sort(Function(a, b) b.DateCreated.CompareTo(a.DateCreated))

            ' Log that we retrieved the projects list
            LogMessage($"Retrieved list of {projects.Count} projects by Chamil1983 at 2025-07-25 00:33:23")

        Catch ex As Exception
            LogMessage($"Error getting projects: {ex.Message}")
        End Try

        Return projects
    End Function

    ' Get a list of project files
    Public Function GetProjectFiles(projectName As String) As List(Of ProjectFile)
        Dim files As New List(Of ProjectFile)

        Try
            Dim projectPath = Path.Combine(_projectsDirectory, projectName)

            If Not Directory.Exists(projectPath) Then
                Return files
            End If

            ' Get all files recursively
            Dim allFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)

            For Each filePath In allFiles
                Dim file As New ProjectFile()
                file.Path = filePath
                file.Name = Path.GetFileName(filePath)
                file.Extension = Path.GetExtension(filePath)
                file.Size = New FileInfo(filePath).Length
                file.RelativePath = filePath.Replace(projectPath, "").TrimStart(Path.DirectorySeparatorChar)

                ' Determine file type for display purposes
                Select Case file.Extension.ToLower()
                    Case ".ino"
                        file.Type = "Arduino Sketch"
                    Case ".h"
                        file.Type = "Header File"
                    Case ".cpp"
                        file.Type = "C++ Source"
                    Case ".c"
                        file.Type = "C Source"
                    Case ".txt"
                        file.Type = "Text File"
                    Case ".json"
                        file.Type = "JSON File"
                    Case Else
                        file.Type = "Other File"
                End Select

                files.Add(file)
            Next

            ' Sort by extension then name
            files.Sort(Function(a, b)
                           Dim extCompare = a.Extension.CompareTo(b.Extension)
                           If extCompare = 0 Then
                               Return a.Name.CompareTo(b.Name)
                           End If
                           Return extCompare
                       End Function)

            LogMessage($"Retrieved {files.Count} files from project {projectName} by Chamil1983 at 2025-07-25 00:11:58")

        Catch ex As Exception
            LogMessage($"Error getting project files: {ex.Message}")
        End Try

        Return files
    End Function

    ' Get project info
    Public Function GetProjectInfo(projectName As String) As ProjectInfo
        Try
            Dim projectPath = Path.Combine(_projectsDirectory, projectName)

            If Directory.Exists(projectPath) Then
                Dim inoFiles = Directory.GetFiles(projectPath, "*.ino", SearchOption.TopDirectoryOnly)

                If inoFiles.Length > 0 Then
                    Dim info As New ProjectInfo()
                    info.Name = projectName
                    info.Path = projectPath
                    info.InoFile = inoFiles(0)
                    info.DateCreated = Directory.GetCreationTime(projectPath)

                    ' Count files by type
                    info.InoCount = Directory.GetFiles(projectPath, "*.ino", SearchOption.AllDirectories).Length
                    info.CppCount = Directory.GetFiles(projectPath, "*.cpp", SearchOption.AllDirectories).Length
                    info.HeaderCount = Directory.GetFiles(projectPath, "*.h", SearchOption.AllDirectories).Length

                    ' Count lines of code in main .ino file
                    If File.Exists(info.InoFile) Then
                        info.MainFileLines = File.ReadAllLines(info.InoFile).Length
                    End If

                    Return info
                End If
            End If

            Return Nothing
        Catch ex As Exception
            LogMessage($"Error getting project info: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ' Delete a project
    Public Function DeleteProject(projectName As String) As Boolean
        Try
            Dim projectPath = Path.Combine(_projectsDirectory, projectName)

            If Directory.Exists(projectPath) Then
                Directory.Delete(projectPath, True)
                LogMessage($"Project {projectName} deleted by Chamil1983 at 2025-07-25 00:17:08")
                Return True
            End If

            Return False
        Catch ex As Exception
            LogMessage($"Error deleting project: {ex.Message}")
            Return False
        End Try
    End Function

    ' Get file content as text
    Public Function GetFileContent(filePath As String) As String
        Try
            If File.Exists(filePath) Then
                ' Check if the file is likely a binary file
                Dim ext = Path.GetExtension(filePath).ToLower()
                Dim isBinary = (ext = ".bin" OrElse ext = ".elf" OrElse ext = ".hex" _
                               OrElse ext = ".o" OrElse ext = ".a" OrElse ext = ".so" _
                               OrElse ext = ".dll" OrElse ext = ".exe")

                If isBinary Then
                    Return "[Binary file content not shown]"
                End If

                Return File.ReadAllText(filePath)
            End If

            Return ""
        Catch ex As Exception
            LogMessage($"Error reading file content: {ex.Message}")
            Return "[Error reading file]"
        End Try
    End Function

    ' Save file content
    Public Function SaveFileContent(filePath As String, content As String) As Boolean
        Try
            If String.IsNullOrEmpty(filePath) Then
                Return False
            End If

            File.WriteAllText(filePath, content)
            LogMessage($"File saved: {Path.GetFileName(filePath)} by Chamil1983 at 2025-07-25 00:17:08")
            Return True
        Catch ex As Exception
            LogMessage($"Error saving file content: {ex.Message}")
            Return False
        End Try
    End Function

    ' Create zip archive of a project
    Public Function CreateProjectArchive(projectName As String) As String
        Try
            Dim projectPath = Path.Combine(_projectsDirectory, projectName)

            If Not Directory.Exists(projectPath) Then
                LogMessage($"Error creating archive: Project {projectName} not found")
                Return ""
            End If

            Dim zipPath = Path.Combine(_tempDirectory, $"{projectName}.zip")

            ' Delete existing zip if it exists
            If File.Exists(zipPath) Then
                File.Delete(zipPath)
            End If

            ' Create zip file
            ZipFile.CreateFromDirectory(projectPath, zipPath)

            LogMessage($"Archive created for project {projectName} by Chamil1983 at 2025-07-25 00:17:08")
            Return zipPath
        Catch ex As Exception
            LogMessage($"Error creating project archive: {ex.Message}")
            Return ""
        End Try
    End Function

End Class

Public Class ProjectInfo
    Public Property Name As String = ""
    Public Property Path As String = ""
    Public Property InoFile As String = ""
    Public Property DateCreated As DateTime = DateTime.MinValue
    Public Property InoCount As Integer = 0
    Public Property CppCount As Integer = 0
    Public Property HeaderCount As Integer = 0
    Public Property MainFileLines As Integer = 0
End Class

' Class to hold file information
Public Class ProjectFile
    Public Property Name As String = ""
    Public Property Path As String = ""
    Public Property RelativePath As String = ""
    Public Property Extension As String = ""
    Public Property Size As Long = 0
    Public Property Type As String = ""
End Class