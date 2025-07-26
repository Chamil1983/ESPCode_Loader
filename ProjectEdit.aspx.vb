Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Class ProjectEdit
    Inherits System.Web.UI.Page

    Private _projectManager As ProjectManager
    Private _projectName As String = ""
    Private _projectPath As String = ""
    Private _selectedFilePath As String = ""

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ' Initialize project manager
        _projectManager = New ProjectManager()

        ' Get project name from query string
        If Request.QueryString("project") IsNot Nothing Then
            _projectName = Request.QueryString("project")
        ElseIf Session("SelectedProject") IsNot Nothing Then
            _projectName = Session("SelectedProject").ToString()
        End If

        ' If no project specified, redirect to home
        If String.IsNullOrEmpty(_projectName) Then
            Response.Redirect("Default.aspx")
            Return
        End If

        ' Set project path
        _projectPath = Path.Combine(Server.MapPath("~/App_Data/Projects"), _projectName)

        ' Check if project exists
        If Not Directory.Exists(_projectPath) Then
            Response.Redirect("Default.aspx")
            Return
        End If

        ' Update UI with project name
        lblProjectName.Text = _projectName

        If Not IsPostBack Then
            ' Load project files
            LoadProjectFiles()

            ' Select first file if available
            SelectFirstFile()
        End If
    End Sub

    Private Sub LoadProjectFiles()
        ' Get project files
        Dim files = _projectManager.GetProjectFiles(_projectName)

        ' Bind to repeater
        rptFiles.DataSource = files
        rptFiles.DataBind()
    End Sub

    Private Sub SelectFirstFile()
        ' Find the first .ino file and select it
        Dim files = _projectManager.GetProjectFiles(_projectName)

        If files.Count > 0 Then
            ' First try to find an .ino file
            Dim inoFile = files.Find(Function(f) f.Extension.ToLower() = ".ino")

            If inoFile IsNot Nothing Then
                ' Select the .ino file
                SelectFile(inoFile.Path)
            Else
                ' Otherwise select the first file
                SelectFile(files(0).Path)
            End If
        End If
    End Sub

    Protected Function GetFileClass(file As Object) As String
        Dim projectFile = DirectCast(file, ProjectFile)

        Select Case projectFile.Extension.ToLower()
            Case ".ino"
                Return "ino-file"
            Case ".cpp"
                Return "cpp-file"
            Case ".h"
                Return "h-file"
            Case Else
                Return ""
        End Select
    End Function

    Protected Sub rptFiles_ItemCommand(source As Object, e As RepeaterCommandEventArgs)
        If e.CommandName = "SelectFile" Then
            SelectFile(e.CommandArgument.ToString())
        End If
    End Sub

    Private Sub SelectFile(filePath As String)
        _selectedFilePath = filePath

        ' Get file content
        Dim content = _projectManager.GetFileContent(filePath)

        ' Update UI
        txtEditor.Text = content
        lblCurrentFile.Text = Path.GetFileName(filePath)

        ' Enable buttons
        btnSave.Enabled = True
        btnRename.Enabled = True
        btnDelete2.Enabled = True

        ' Set hidden field for current file path
        hidCurrentFilePath.Value = filePath
    End Sub

    Protected Sub btnSave_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(_selectedFilePath) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('No file is selected.');", True)
            Return
        End If

        ' Save file content
        If _projectManager.SaveFileContent(_selectedFilePath, txtEditor.Text) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('File saved successfully.');", True)

            ' Log the save action
            LogMessage($"File {Path.GetFileName(_selectedFilePath)} saved by Chamil1983 at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
        Else
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Failed to save file.');", True)
        End If
    End Sub

    Protected Sub btnRename_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(_selectedFilePath) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('No file is selected.');", True)
            Return
        End If

        ' Set current file name in rename text box
        txtRenameFile.Text = Path.GetFileName(_selectedFilePath)

        ' Show the rename modal
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showModal",
            "showRenameFileModal();", True)
    End Sub

    Protected Sub btnRenameFile_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(hidCurrentFilePath.Value) OrElse String.IsNullOrEmpty(txtRenameFile.Text) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('File name cannot be empty.');", True)
            Return
        End If

        Try
            ' Get the current and new file paths
            Dim currentPath = hidCurrentFilePath.Value
            Dim newFileName = txtRenameFile.Text.Trim()

            ' Ensure file name has an extension
            If Not Path.HasExtension(newFileName) Then
                newFileName += Path.GetExtension(currentPath)
            End If

            ' Create new path
            Dim newPath = Path.Combine(Path.GetDirectoryName(currentPath), newFileName)

            ' Check if new file already exists
            If File.Exists(newPath) And newPath.ToLower() <> currentPath.ToLower() Then
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                    "alert('A file with that name already exists.');", True)
                Return
            End If

            ' Save content to ensure it's not lost
            Dim content = txtEditor.Text

            ' Rename the file
            File.Move(currentPath, newPath)

            ' Ensure content is preserved (some file systems might not preserve content on move)
            File.WriteAllText(newPath, content)

            ' Update UI
            _selectedFilePath = newPath
            lblCurrentFile.Text = Path.GetFileName(newPath)
            hidCurrentFilePath.Value = newPath

            ' Reload file list
            LoadProjectFiles()

            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('File renamed successfully.');", True)

            ' Log the rename action
            LogMessage($"File renamed from {Path.GetFileName(currentPath)} to {newFileName} by Chamil1983 at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")

        Catch ex As Exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                $"alert('Error renaming file: {ex.Message}');", True)
        End Try
    End Sub

    Protected Sub btnDelete2_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(_selectedFilePath) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('No file is selected.');", True)
            Return
        End If

        Try
            ' Get file name for logging
            Dim fileName = Path.GetFileName(_selectedFilePath)

            ' Delete the file
            File.Delete(_selectedFilePath)

            ' Clear selection
            _selectedFilePath = ""
            txtEditor.Text = ""
            lblCurrentFile.Text = "No file selected"

            ' Disable buttons
            btnSave.Enabled = False
            btnRename.Enabled = False
            btnDelete2.Enabled = False

            ' Reload file list
            LoadProjectFiles()

            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('File deleted successfully.');", True)

            ' Log the delete action
            LogMessage($"File {fileName} deleted by Chamil1983 at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")

        Catch ex As Exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                $"alert('Error deleting file: {ex.Message}');", True)
        End Try
    End Sub

    Protected Sub btnAddNewFile_Click(sender As Object, e As EventArgs)
        ' Clear the new file name text box
        txtNewFileName.Text = ""

        ' Show the add file modal
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "showModal",
            "showAddFileModal();", True)
    End Sub

    Protected Sub btnCreateNewFile_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(txtNewFileName.Text) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('File name cannot be empty.');", True)
            Return
        End If

        Try
            ' Get the file name and ensure it has an extension
            Dim fileName = txtNewFileName.Text.Trim()
            Dim fileType = ddlFileType.SelectedValue

            If Not Path.HasExtension(fileName) Then
                fileName += fileType
            End If

            ' Create full path
            Dim newFilePath = Path.Combine(_projectPath, fileName)

            ' Check if file already exists
            If File.Exists(newFilePath) Then
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                    "alert('A file with that name already exists.');", True)
                Return
            End If

            ' Create file with template content
            Dim content = GetTemplateForFileType(fileName, fileType)
            File.WriteAllText(newFilePath, content)

            ' Reload file list
            LoadProjectFiles()

            ' Select the new file
            SelectFile(newFilePath)

            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('File created successfully.');", True)

            ' Log the create action
            LogMessage($"New file {fileName} created by Chamil1983 at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")

        Catch ex As Exception
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                $"alert('Error creating file: {ex.Message}');", True)
        End Try
    End Sub

    Private Function GetTemplateForFileType(fileName As String, fileType As String) As String
        Dim baseFileName = Path.GetFileNameWithoutExtension(fileName)

        Select Case fileType.ToLower()
            Case ".ino"
                Return $"/*
 * {fileName}
 * Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
 * Author: Chamil1983
 * Project: {_projectName}
 */

void setup() {{
  // Initialize serial communication
  Serial.begin(115200);
  
  // Wait for serial to be ready
  delay(1000);
  
  Serial.println(""Setup complete"");
}}

void loop() {{
  // Main loop code
  delay(1000);
}}
"
            Case ".h"
                Dim guardName = $"{baseFileName.ToUpper()}_H"
                Return $"/*
 * {fileName}
 * Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
 * Author: Chamil1983
 * Project: {_projectName}
 */

#ifndef {guardName}
#define {guardName}

// Header declarations go here

class {baseFileName} {{
  public:
    {baseFileName}();
    
  private:
    // Private members
}};

#endif // {guardName}
"
            Case ".cpp"
                Return $"/*
 * {fileName}
 * Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
 * Author: Chamil1983
 * Project: {_projectName}
 */

#include ""{baseFileName}.h""

// Implementation code goes here

{baseFileName}::{baseFileName}() {{
  // Constructor implementation
}}
"
            Case ".c"
                Return $"/*
 * {fileName}
 * Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
 * Author: Chamil1983
 * Project: {_projectName}
 */

#include <stdio.h>

// Implementation code goes here
"
            Case Else
                Return $"// {fileName}
// Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
// Author: Chamil1983
// Project: {_projectName}
"
        End Select
    End Function

    Protected Sub btnCompile_Click(sender As Object, e As EventArgs)
        ' Save current file if one is selected
        If Not String.IsNullOrEmpty(_selectedFilePath) Then
            _projectManager.SaveFileContent(_selectedFilePath, txtEditor.Text)
        End If

        ' Store project name in session
        Session("SelectedProject") = _projectName

        ' Redirect to compilation page
        Response.Redirect($"Compilation.aspx?project={_projectName}")
    End Sub

    Protected Sub btnDownload_Click(sender As Object, e As EventArgs)
        ' Create zip archive of project
        Dim zipPath = _projectManager.CreateProjectArchive(_projectName)

        If String.IsNullOrEmpty(zipPath) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Failed to create project archive.');", True)
            Return
        End If

        ' Download the zip file
        Response.ContentType = "application/zip"
        Response.AddHeader("Content-Disposition", $"attachment; filename={_projectName}.zip")
        Response.TransmitFile(zipPath)
        Response.Flush()
        Response.End()
    End Sub

    Protected Sub btnDelete_Click(sender As Object, e As EventArgs)
        If _projectManager.DeleteProject(_projectName) Then
            ' Log the deletion
            LogMessage($"Project {_projectName} deleted by Chamil1983 at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")

            ' Redirect to home page
            Response.Redirect("Default.aspx")
        Else
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Failed to delete project.');", True)
        End If
    End Sub

    Protected Sub btnBack_Click(sender As Object, e As EventArgs)
        Response.Redirect("Default.aspx")
    End Sub

    Private Sub LogMessage(message As String)
        If My.Settings.EnableLogging Then
            Try
                Dim logFile = Server.MapPath("~/App_Data/logs/app.log")
                Dim logDir = Path.GetDirectoryName(logFile)

                If Not Directory.Exists(logDir) Then
                    Directory.CreateDirectory(logDir)
                End If

                Using writer As New StreamWriter(logFile, True)
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}")
                End Using
            Catch ex As Exception
                ' Cannot log the log error
            End Try
        End If
    End Sub
End Class