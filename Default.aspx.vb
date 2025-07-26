Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Compression
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Class [Default]
    Inherits System.Web.UI.Page

    Private _projectManager As ProjectManager
    Private _boardManager As BoardManager

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ' Initialize managers
        _projectManager = New ProjectManager()
        _boardManager = New BoardManager()

        If Not IsPostBack Then
            ' Populate board types dropdown
            PopulateBoardTypes()

            ' Populate partition schemes dropdown
            PopulatePartitionSchemes()

            ' Load project list
            LoadProjects()
        End If
    End Sub

    Private Sub PopulateBoardTypes()
        ddlBoardType.Items.Clear()

        ' Add board types from board manager
        For Each boardName In _boardManager.GetBoardNames()
            ddlBoardType.Items.Add(New ListItem(boardName, boardName))
        Next

        ' Set default selection from settings
        If Not String.IsNullOrEmpty(My.Settings.DefaultBoard) Then
            Dim item = ddlBoardType.Items.FindByValue(My.Settings.DefaultBoard)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If
    End Sub

    Private Sub PopulatePartitionSchemes()
        ddlPartitionScheme.Items.Clear()

        ' Add default partition schemes
        ddlPartitionScheme.Items.Add(New ListItem("Default", "default"))
        ddlPartitionScheme.Items.Add(New ListItem("Minimal SPIFFS", "min_spiffs"))
        ddlPartitionScheme.Items.Add(New ListItem("Minimal OTA", "min_ota"))
        ddlPartitionScheme.Items.Add(New ListItem("Huge App", "huge_app"))
        ddlPartitionScheme.Items.Add(New ListItem("Custom", "custom"))

        ' Add any custom partition schemes
        For Each scheme In _boardManager.GetCustomPartitions()
            If scheme <> "default" AndAlso scheme <> "min_spiffs" AndAlso scheme <> "min_ota" _
                AndAlso scheme <> "huge_app" AndAlso scheme <> "custom" Then
                ddlPartitionScheme.Items.Add(New ListItem(scheme, scheme))
            End If
        Next

        ' Set default selection from settings
        If Not String.IsNullOrEmpty(My.Settings.DefaultPartition) Then
            Dim item = ddlPartitionScheme.Items.FindByValue(My.Settings.DefaultPartition)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If
    End Sub

    Private Sub LoadProjects()
        ' Get list of projects
        Dim projects = _projectManager.GetProjects()

        ' Bind to grid view
        gvProjects.DataSource = projects
        gvProjects.DataBind()
    End Sub

    Protected Sub ddlBoardType_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Update default partition scheme based on selected board
        Dim selectedBoard = ddlBoardType.SelectedValue
        Dim defaultPartition = _boardManager.GetDefaultPartitionForBoard(selectedBoard)

        If Not String.IsNullOrEmpty(defaultPartition) Then
            Dim item = ddlPartitionScheme.Items.FindByValue(defaultPartition)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If

        ' Save selected board to session
        Session("SelectedBoard") = selectedBoard
    End Sub

    Protected Sub btnUploadNewProject_Click(sender As Object, e As EventArgs)
        Response.Redirect("ProjectUpload.aspx")
    End Sub

    Protected Sub btnConfigureBoard_Click(sender As Object, e As EventArgs)
        ' Save selected board and partition scheme to session
        Session("SelectedBoard") = ddlBoardType.SelectedValue
        Session("SelectedPartition") = ddlPartitionScheme.SelectedValue

        ' Redirect to board configuration page
        Response.Redirect("BoardConfig.aspx")
    End Sub

    Protected Sub btnQuickUpload_Click(sender As Object, e As EventArgs)
        If Not fileQuickUpload.HasFile Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
            "alert('Please select a file to upload.');", True)
            Return
        End If

        ' Check file extension
        Dim ext = Path.GetExtension(fileQuickUpload.FileName).ToLower()
        If ext <> ".ino" Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
            "alert('Please select an Arduino sketch file (.ino).');", True)
            Return
        End If

        ' Generate a project name from the file name
        Dim projectName = Path.GetFileNameWithoutExtension(fileQuickUpload.FileName)
        projectName = $"{projectName}_{DateTime.Now:yyyyMMddHHmmss}"

        ' Create temporary directory
        Dim tempDir = Path.Combine(Server.MapPath("~/App_Data/Temp"), projectName)
        Directory.CreateDirectory(tempDir)

        ' Save file to temp directory
        Dim filePath = Path.Combine(tempDir, fileQuickUpload.FileName)
        fileQuickUpload.SaveAs(filePath)

        ' Create a zip file
        Dim zipPath = Path.Combine(Server.MapPath("~/App_Data/Temp"), $"{projectName}.zip")
        Using zipArchive = System.IO.Compression.ZipFile.Open(zipPath, IO.Compression.ZipArchiveMode.Create)
            zipArchive.CreateEntryFromFile(filePath, fileQuickUpload.FileName)
        End Using

        ' Clean up temp file
        File.Delete(filePath)
        Directory.Delete(tempDir, True)

        ' Upload the zip file as a project
        Using fileStream = File.OpenRead(zipPath)
            ' Use the new method that accepts a stream
            Dim uploadedProjectPath = _projectManager.UploadProjectFromStream(fileStream, Path.GetFileName(zipPath), projectName)

            If String.IsNullOrEmpty(uploadedProjectPath) Then
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Failed to upload project. Please try again.');", True)
                Return
            End If
        End Using

        ' Clean up zip file
        File.Delete(zipPath)

        ' Save selected board and partition to session
        Session("SelectedBoard") = ddlBoardType.SelectedValue
        Session("SelectedPartition") = ddlPartitionScheme.SelectedValue
        Session("SelectedProject") = projectName

        ' Redirect to compilation page
        Response.Redirect($"Compilation.aspx?project={projectName}")
    End Sub

    Private Function CreateHttpPostedFileFromStream(stream As Stream, contentType As String, fileName As String) As HttpPostedFile
        ' Use reflection or other means to create a proper HttpPostedFile
        ' This is just a placeholder - the actual implementation depends on your environment
        ' You might need to use a mock object or a specific implementation for your framework
    End Function

    Protected Sub gvProjects_RowCommand(sender As Object, e As GridViewCommandEventArgs)
        Dim projectName = e.CommandArgument.ToString()

        Select Case e.CommandName
            Case "Compile"
                ' Save selected board and partition to session
                Session("SelectedBoard") = ddlBoardType.SelectedValue
                Session("SelectedPartition") = ddlPartitionScheme.SelectedValue
                Session("SelectedProject") = projectName

                ' Redirect to compilation page
                Response.Redirect($"Compilation.aspx?project={projectName}")

            Case "Edit"
                ' Redirect to edit page
                Response.Redirect($"ProjectEdit.aspx?project={projectName}")

            Case "Download"
                ' Create zip archive of project
                Dim zipPath = _projectManager.CreateProjectArchive(projectName)
                If Not String.IsNullOrEmpty(zipPath) Then
                    ' Download the file
                    Response.ContentType = "application/zip"
                    Response.AddHeader("Content-Disposition", $"attachment; filename={projectName}.zip")
                    Response.TransmitFile(zipPath)
                    Response.Flush()
                    Response.End()
                Else
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                        "alert('Failed to download project.');", True)
                End If

            Case "Delete"
                ' Delete the project
                If _projectManager.DeleteProject(projectName) Then
                    ' Reload projects list
                    LoadProjects()
                Else
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                        "alert('Failed to delete project.');", True)
                End If
        End Select
    End Sub
End Class

