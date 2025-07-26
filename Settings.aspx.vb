Imports System
Imports System.IO
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Class Settings
    Inherits System.Web.UI.Page

    Private _boardManager As BoardManager

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ' Initialize board manager
        _boardManager = New BoardManager()

        If Not IsPostBack Then
            ' Load current settings
            LoadSettings()

            ' Populate board types dropdown
            PopulateBoardTypes()

            ' Populate partition schemes dropdown
            PopulatePartitionSchemes()
        End If
    End Sub

    Private Sub LoadSettings()
        ' Load application settings
        txtArduinoCliPath.Text = My.Settings.ArduinoCliPath
        txtDefaultSketchDir.Text = My.Settings.DefaultSketchDir
        txtHardwarePath.Text = My.Settings.HardwarePath
        txtBoardsFilePath.Text = My.Settings.BoardsFilePath
        txtCompileTimeout.Text = My.Settings.CompileTimeout.ToString()
        chkEnableLogging.Checked = My.Settings.EnableLogging
        chkVerboseOutput.Checked = My.Settings.VerboseOutput
    End Sub

    Private Sub PopulateBoardTypes()
        ddlDefaultBoard.Items.Clear()

        ' Add board types from board manager
        For Each boardName In _boardManager.GetBoardNames()
            ddlDefaultBoard.Items.Add(New ListItem(boardName, boardName))
        Next

        ' Set default selection from settings
        If Not String.IsNullOrEmpty(My.Settings.DefaultBoard) Then
            Dim item = ddlDefaultBoard.Items.FindByValue(My.Settings.DefaultBoard)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If
    End Sub

    Private Sub PopulatePartitionSchemes()
        ddlDefaultPartition.Items.Clear()

        ' Add default partition schemes
        ddlDefaultPartition.Items.Add(New ListItem("Default", "default"))
        ddlDefaultPartition.Items.Add(New ListItem("Minimal SPIFFS", "min_spiffs"))
        ddlDefaultPartition.Items.Add(New ListItem("Minimal OTA", "min_ota"))
        ddlDefaultPartition.Items.Add(New ListItem("Huge App", "huge_app"))
        ddlDefaultPartition.Items.Add(New ListItem("Custom", "custom"))

        ' Add any custom partition schemes
        For Each scheme In _boardManager.GetCustomPartitions()
            If scheme <> "default" AndAlso scheme <> "min_spiffs" AndAlso scheme <> "min_ota" _
                AndAlso scheme <> "huge_app" AndAlso scheme <> "custom" Then
                ddlDefaultPartition.Items.Add(New ListItem(scheme, scheme))
            End If
        Next

        ' Set default selection from settings
        If Not String.IsNullOrEmpty(My.Settings.DefaultPartition) Then
            Dim item = ddlDefaultPartition.Items.FindByValue(My.Settings.DefaultPartition)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If
    End Sub

    Protected Sub btnBrowseArduinoCli_Click(sender As Object, e As EventArgs)
        ' Server-side file browsing is not directly possible in web applications
        ' This would typically be done through a JavaScript file picker 
        ' or providing a predefined list of options

        ' For this example, we'll just show a message that this would be handled client-side
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "browseMessage",
            "alert('In a real application, this would open a file browser dialog. " &
            "For security reasons, web applications cannot directly browse the server file system. " &
            "Please manually enter the path to arduino-cli.exe.');", True)
    End Sub

    Protected Sub btnBrowseSketchDir_Click(sender As Object, e As EventArgs)
        ' Same as above - would need client-side handling
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "browseMessage",
            "alert('In a real application, this would open a directory browser dialog. " &
            "Please manually enter the sketch directory path.');", True)
    End Sub

    Protected Sub btnBrowseHardwarePath_Click(sender As Object, e As EventArgs)
        ' Same as above - would need client-side handling
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "browseMessage",
            "alert('In a real application, this would open a directory browser dialog. " &
            "Please manually enter the ESP32 hardware directory path.');", True)
    End Sub

    Protected Sub btnBrowseBoardsFile_Click(sender As Object, e As EventArgs)
        ' Same as above - would need client-side handling
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "browseMessage",
            "alert('In a real application, this would open a file browser dialog. " &
            "Please manually enter the path to boards.txt file.');", True)
    End Sub

    Protected Sub btnSaveSettings_Click(sender As Object, e As EventArgs)
        ' Validate inputs
        If String.IsNullOrEmpty(txtArduinoCliPath.Text) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Arduino CLI path cannot be empty.');", True)
            Return
        End If

        ' Parse timeout value
        Dim timeout As Integer
        If Not Integer.TryParse(txtCompileTimeout.Text, timeout) OrElse timeout < 30 OrElse timeout > 600 Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Compile timeout must be a number between 30 and 600 seconds.');", True)
            Return
        End If

        ' Save settings
        My.Settings.ArduinoCliPath = txtArduinoCliPath.Text
        My.Settings.DefaultSketchDir = txtDefaultSketchDir.Text
        My.Settings.HardwarePath = txtHardwarePath.Text
        My.Settings.BoardsFilePath = txtBoardsFilePath.Text
        My.Settings.CompileTimeout = timeout
        My.Settings.EnableLogging = chkEnableLogging.Checked
        My.Settings.VerboseOutput = chkVerboseOutput.Checked

        ' Save board settings
        If ddlDefaultBoard.SelectedItem IsNot Nothing Then
            My.Settings.DefaultBoard = ddlDefaultBoard.SelectedValue
        End If

        If ddlDefaultPartition.SelectedItem IsNot Nothing Then
            My.Settings.DefaultPartition = ddlDefaultPartition.SelectedValue
        End If

        ' Save settings
        My.Settings.Save()

        ' Update boards.txt path in board manager if changed
        If _boardManager.BoardsFilePath <> txtBoardsFilePath.Text AndAlso File.Exists(txtBoardsFilePath.Text) Then
            _boardManager.BoardsFilePath = txtBoardsFilePath.Text
            _boardManager.LoadBoardConfigurations()
        End If

        ' Show success message
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
            "alert('Settings saved successfully.');", True)

        ' Log the settings update
        LogMessage($"Settings updated by Chamil1983 at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
    End Sub

    Protected Sub btnResetDefaults_Click(sender As Object, e As EventArgs)
        ' Confirm reset
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "confirmReset",
            "if(confirm('Are you sure you want to reset all settings to default values?')) {" &
            "   window.location.href = 'Settings.aspx?reset=true';" &
            "}", True)
    End Sub

    Protected Sub btnCancel_Click(sender As Object, e As EventArgs)
        ' Go back to home page
        Response.Redirect("Default.aspx")
    End Sub

    Protected Sub Page_PreRender(sender As Object, e As EventArgs) Handles Me.PreRender
        ' Check if we need to reset settings
        If Not IsPostBack AndAlso Request.QueryString("reset") = "true" Then
            ResetToDefaults()
        End If
    End Sub

    Private Sub ResetToDefaults()
        ' Set default paths
        txtArduinoCliPath.Text = "C:\arduino-cli\arduino-cli.exe"
        txtDefaultSketchDir.Text = "C:\Temp\ArduinoSketches"
        txtHardwarePath.Text = ""
        txtBoardsFilePath.Text = ""
        txtCompileTimeout.Text = "300"
        chkEnableLogging.Checked = True
        chkVerboseOutput.Checked = True

        ' Set default board and partition
        If ddlDefaultBoard.Items.FindByValue("KC-Link PRO A8 (Default)") IsNot Nothing Then
            ddlDefaultBoard.SelectedValue = "KC-Link PRO A8 (Default)"
        End If

        If ddlDefaultPartition.Items.FindByValue("default") IsNot Nothing Then
            ddlDefaultPartition.SelectedValue = "default"
        End If

        ' Show message
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
            "alert('Settings have been reset to default values. Click Save Settings to apply.');", True)

        ' Log the reset
        LogMessage($"Settings reset to defaults by Chamil1983 at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
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