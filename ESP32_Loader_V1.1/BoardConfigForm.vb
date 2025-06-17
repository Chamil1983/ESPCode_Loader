Imports System.Windows.Forms
Imports System.Drawing

Public Class BoardConfigForm
    Inherits Form

    Private cmbOptions As New Dictionary(Of String, ComboBox)
    Private board As ArduinoBoard

    Public Property SelectedConfig As New ArduinoBoardConfig

    Public Sub New(board As ArduinoBoard)
        Me.board = board
        InitializeComponents()
    End Sub

    Private Sub InitializeComponents()
        Text = $"Board Configuration: {board.Name}"
        Size = New Size(400, 500)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        StartPosition = FormStartPosition.CenterParent

        Dim panel As New Panel With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True
        }

        Dim y As Integer = 10

        ' Add option controls - with additional null checks
        If board.MenuOptions IsNot Nothing Then
            For Each menuCategory In board.MenuOptions
                If menuCategory.Key Is Nothing OrElse String.IsNullOrEmpty(menuCategory.Key) Then Continue For

                Dim categoryName = menuCategory.Key

                ' Label for category
                Dim lblCategory As New Label With {
                    .Text = categoryName,
                    .Location = New Point(10, y),
                    .Size = New Size(panel.Width - 20, 20),
                    .Font = New Font(Font, FontStyle.Bold)
                }
                panel.Controls.Add(lblCategory)
                y += 25

                ' ComboBox for options
                Dim cmbOption As New ComboBox With {
                    .DropDownStyle = ComboBoxStyle.DropDownList,
                    .Location = New Point(20, y),
                    .Size = New Size(panel.Width - 40, 25)
                }

                ' Add options to combo - with validation and error handling
                If menuCategory.Value IsNot Nothing Then
                    Try
                        For Each entry In menuCategory.Value
                            If entry.Key IsNot Nothing Then
                                cmbOption.Items.Add(entry.Key)
                            End If
                        Next
                    Catch ex As Exception
                        ' Handle potential dictionary errors
                        MessageBox.Show($"Error loading options for {categoryName}: {ex.Message}",
                                       "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End Try
                End If

                If cmbOption.Items.Count > 0 Then
                    cmbOption.SelectedIndex = 0
                End If

                panel.Controls.Add(cmbOption)
                cmbOptions.Add(categoryName, cmbOption)
                y += 30
            Next
        End If

        ' Buttons
        Dim btnOK As New Button With {
            .Text = "OK",
            .DialogResult = DialogResult.OK,
            .Location = New Point(panel.Width - 160, y + 10),
            .Size = New Size(70, 30)
        }
        AddHandler btnOK.Click, AddressOf btnOK_Click
        panel.Controls.Add(btnOK)

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .DialogResult = DialogResult.Cancel,
            .Location = New Point(panel.Width - 80, y + 10),
            .Size = New Size(70, 30)
        }
        panel.Controls.Add(btnCancel)

        Controls.Add(panel)
        AcceptButton = btnOK
        CancelButton = btnCancel
    End Sub

    Private Sub btnOK_Click(sender As Object, e As EventArgs)
        SelectedConfig = New ArduinoBoardConfig()

        For Each kvp In cmbOptions
            Try
                Dim categoryName = kvp.Key
                Dim comboBox = kvp.Value

                If comboBox.SelectedItem IsNot Nothing Then
                    Dim optionKey = comboBox.SelectedItem.ToString()

                    ' Validate that the selection is valid
                    If board.MenuOptions.ContainsKey(categoryName) AndAlso
                       board.MenuOptions(categoryName).ContainsKey(optionKey) Then

                        Dim optionValue = board.MenuOptions(categoryName)(optionKey)
                        SelectedConfig.Options.Add(categoryName, optionKey)
                    End If
                End If
            Catch ex As Exception
                ' Skip this option if there's an error
                System.Diagnostics.Debug.WriteLine($"Warning: Error processing option: {ex.Message}")
            End Try
        Next

        DialogResult = DialogResult.OK
        Close()
    End Sub
End Class