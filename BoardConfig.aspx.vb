Imports System
Imports System.Collections.Generic
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Class BoardConfig
    Inherits System.Web.UI.Page

    Private _boardManager As BoardManager
    Private _currentParameters As Dictionary(Of String, String)
    Private _boardName As String = ""

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ' Initialize board manager
        _boardManager = New BoardManager()

        If Not IsPostBack Then
            ' Load board types
            PopulateBoardTypes()

            ' Set selected board from session or query string
            Dim selectedBoard As String = ""

            If Request.QueryString("board") IsNot Nothing Then
                selectedBoard = Request.QueryString("board")
            ElseIf Session("SelectedBoard") IsNot Nothing Then
                selectedBoard = Session("SelectedBoard").ToString()
            End If

            If Not String.IsNullOrEmpty(selectedBoard) Then
                Dim item = ddlBoardType.Items.FindByValue(selectedBoard)
                If item IsNot Nothing Then
                    item.Selected = True
                End If
            ElseIf Not String.IsNullOrEmpty(My.Settings.DefaultBoard) Then
                Dim item = ddlBoardType.Items.FindByValue(My.Settings.DefaultBoard)
                If item IsNot Nothing Then
                    item.Selected = True
                End If
            End If

            ' Load board configuration
            LoadBoardConfiguration()
        End If
    End Sub

    Private Sub PopulateBoardTypes()
        ddlBoardType.Items.Clear()

        ' Add board types from board manager
        For Each boardName In _boardManager.GetBoardNames()
            ddlBoardType.Items.Add(New ListItem(boardName, boardName))
        Next

        ' Select first item if available
        If ddlBoardType.Items.Count > 0 Then
            ddlBoardType.SelectedIndex = 0
        End If
    End Sub

    Private Sub LoadBoardConfiguration()
        If ddlBoardType.SelectedItem Is Nothing Then
            Return
        End If

        ' Get the current board name
        _boardName = ddlBoardType.SelectedItem.ToString()

        ' Get the FQBN for this board
        Dim currentFQBN = _boardManager.GetFQBN(_boardName)

        ' Update the FQBN text
        txtFQBN.Text = currentFQBN

        ' Parse FQBN to extract parameters
        ExtractParametersFromFQBN(currentFQBN)

        ' Populate parameter dropdowns
        PopulateParameterDropdowns()
    End Sub

    Private Sub ExtractParametersFromFQBN(fqbn As String)
        _currentParameters = New Dictionary(Of String, String)()

        ' Default values
        _currentParameters("CPUFreq") = "240"
        _currentParameters("FlashMode") = "dio"
        _currentParameters("FlashFreq") = "80"
        _currentParameters("PartitionScheme") = "default"
        _currentParameters("UploadSpeed") = "921600"
        _currentParameters("DebugLevel") = "none"
        _currentParameters("PSRAM") = "disabled"

        ' Parse parameters from FQBN
        If fqbn.Contains(":") Then
            Dim parts = fqbn.Split(New Char() {":"c})
            If parts.Length >= 4 Then
                Dim paramPart = parts(3)
                Dim paramPairs = paramPart.Split(New Char() {","c})

                For Each pair In paramPairs
                    If pair.Contains("=") Then
                        Dim keyValue = pair.Split(New Char() {"="c}, 2)
                        If keyValue.Length = 2 Then
                            _currentParameters(keyValue(0)) = keyValue(1)
                        End If
                    End If
                Next
            End If
        End If
    End Sub

    Private Sub PopulateParameterDropdowns()
        ' Populate CPU Frequency dropdown
        PopulateDropdown(ddlCpuFreq, _boardManager.GetParameterOptions(_boardName, "CPUFreq"), _currentParameters("CPUFreq"))

        ' Populate Flash Mode dropdown
        PopulateDropdown(ddlFlashMode, _boardManager.GetParameterOptions(_boardName, "FlashMode"), _currentParameters("FlashMode"))

        ' Populate Flash Frequency dropdown
        PopulateDropdown(ddlFlashFreq, _boardManager.GetParameterOptions(_boardName, "FlashFreq"), _currentParameters("FlashFreq"))

        ' Populate Partition Scheme dropdown
        PopulateDropdown(ddlPartitionScheme, _boardManager.GetParameterOptions(_boardName, "PartitionScheme"), _currentParameters("PartitionScheme"))

        ' Populate Upload Speed dropdown
        PopulateDropdown(ddlUploadSpeed, _boardManager.GetParameterOptions(_boardName, "UploadSpeed"), _currentParameters("UploadSpeed"))

        ' Populate Debug Level dropdown
        PopulateDropdown(ddlDebugLevel, _boardManager.GetParameterOptions(_boardName, "DebugLevel"), _currentParameters("DebugLevel"))

        ' Set PSRAM checkbox
        chkPSRAM.Checked = (_currentParameters("PSRAM") = "enabled")
    End Sub

    Private Sub PopulateDropdown(dropDown As DropDownList, options As Dictionary(Of String, String), selectedValue As String)
        dropDown.Items.Clear()

        ' Add options to dropdown
        For Each kvp In options
            Dim item = New ListItem(kvp.Value, kvp.Key)
            dropDown.Items.Add(item)

            ' Select the current value
            If kvp.Key = selectedValue Then
                item.Selected = True
            End If
        Next

        ' If nothing is selected, select the first item if available
        If dropDown.SelectedIndex < 0 AndAlso dropDown.Items.Count > 0 Then
            dropDown.SelectedIndex = 0
        End If
    End Sub

    Protected Sub ddlBoardType_SelectedIndexChanged(sender As Object, e As EventArgs)
        LoadBoardConfiguration()
    End Sub

    Protected Sub btnReload_Click(sender As Object, e As EventArgs)
        ' Reload board configurations
        _boardManager.LoadBoardConfigurations()

        ' Refresh board list
        Dim currentBoard = If(ddlBoardType.SelectedItem IsNot Nothing, ddlBoardType.SelectedItem.ToString(), "")
        PopulateBoardTypes()

        ' Try to reselect the current board
        If Not String.IsNullOrEmpty(currentBoard) Then
            Dim item = ddlBoardType.Items.FindByValue(currentBoard)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If

        ' Load board configuration
        LoadBoardConfiguration()

        ' Show success message
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
            "alert('Board configurations reloaded successfully.');", True)
    End Sub

    Protected Sub btnSave_Click(sender As Object, e As EventArgs)
        ' Update parameters from UI controls
        UpdateParametersFromUI()

        ' Save board configuration
        _boardManager.UpdateBoardConfiguration(_boardName, _currentParameters)

        ' Save as default board
        My.Settings.DefaultBoard = _boardName
        My.Settings.Save()

        ' Store in session
        Session("SelectedBoard") = _boardName

        ' Update FQBN text
        txtFQBN.Text = _boardManager.GetFQBN(_boardName)

        ' Show success message
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
            $"alert('Board configuration for {_boardName} has been saved successfully.');", True)
    End Sub

    Protected Sub btnReset_Click(sender As Object, e As EventArgs)
        ' Reset parameters to defaults
        _currentParameters("CPUFreq") = "240"
        _currentParameters("FlashMode") = "dio"
        _currentParameters("FlashFreq") = "80"
        _currentParameters("PartitionScheme") = "default"
        _currentParameters("UploadSpeed") = "921600"
        _currentParameters("DebugLevel") = "none"
        _currentParameters("PSRAM") = "disabled"

        ' Update UI
        PopulateParameterDropdowns()

        ' Update FQBN
        UpdateParametersFromUI()
        txtFQBN.Text = _boardManager.GetFQBN(_boardName)

        ' Show success message
        ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
            "alert('Board configuration has been reset to default values.');", True)
    End Sub

    Protected Sub btnCancel_Click(sender As Object, e As EventArgs)
        ' Redirect back to previous page or home
        If Session("PreviousPage") IsNot Nothing Then
            Response.Redirect(Session("PreviousPage").ToString())
        Else
            Response.Redirect("Default.aspx")
        End If
    End Sub

    Private Sub UpdateParametersFromUI()
        ' Update parameters from UI controls
        If ddlCpuFreq.SelectedItem IsNot Nothing Then
            _currentParameters("CPUFreq") = ddlCpuFreq.SelectedValue
        End If

        If ddlFlashMode.SelectedItem IsNot Nothing Then
            _currentParameters("FlashMode") = ddlFlashMode.SelectedValue
        End If

        If ddlFlashFreq.SelectedItem IsNot Nothing Then
            _currentParameters("FlashFreq") = ddlFlashFreq.SelectedValue
        End If

        If ddlPartitionScheme.SelectedItem IsNot Nothing Then
            _currentParameters("PartitionScheme") = ddlPartitionScheme.SelectedValue
        End If

        If ddlUploadSpeed.SelectedItem IsNot Nothing Then
            _currentParameters("UploadSpeed") = ddlUploadSpeed.SelectedValue
        End If

        If ddlDebugLevel.SelectedItem IsNot Nothing Then
            _currentParameters("DebugLevel") = ddlDebugLevel.SelectedValue
        End If

        _currentParameters("PSRAM") = If(chkPSRAM.Checked, "enabled", "disabled")
    End Sub
End Class