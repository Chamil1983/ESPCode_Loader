Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.IO
Imports System.Diagnostics

Namespace KC_LINK_LoaderV1._1
    Public Class ExportCompleteDialog
        Inherits Form

        Private WithEvents lblTitle As Label
        Private WithEvents lblDescription As Label
        Private WithEvents lstFiles As ListView
        Private WithEvents btnOpenFolder As Button
        Private WithEvents btnZipUpload As Button
        Private WithEvents btnBinaryUpload As Button
        Private WithEvents btnClose As Button

        Private exportPath As String
        Private zipPath As String

        Public Sub New(exportPath As String, files As List(Of String), zipPath As String)
            MyBase.New()
            Me.exportPath = exportPath
            Me.zipPath = zipPath
            InitializeComponent()
            PopulateFilesList(files)
        End Sub

        Private Sub InitializeComponent()
            ' Form setup
            Me.Text = "Export Complete"
            Me.Size = New Size(500, 400)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            ' Main layout
            Dim mainLayout As New TableLayoutPanel()
            mainLayout.Dock = DockStyle.Fill
            mainLayout.RowCount = 4
            mainLayout.ColumnCount = 1
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))  ' Title
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))  ' Description
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Files list
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))  ' Buttons
            mainLayout.Padding = New Padding(10)

            ' Title label
            lblTitle = New Label()
            lblTitle.Text = "Binary Export Complete"
            lblTitle.Font = New Font(lblTitle.Font, FontStyle.Bold)
            lblTitle.Dock = DockStyle.Fill
            lblTitle.TextAlign = ContentAlignment.MiddleLeft

            ' Description label
            lblDescription = New Label()
            lblDescription.Text = "The following binary files have been exported to:" & Environment.NewLine & exportPath
            lblDescription.Dock = DockStyle.Fill
            lblDescription.TextAlign = ContentAlignment.MiddleLeft

            ' Files list
            lstFiles = New ListView()
            lstFiles.View = View.Details
            lstFiles.FullRowSelect = True
            lstFiles.Dock = DockStyle.Fill
            lstFiles.Columns.Add("File", 250)
            lstFiles.Columns.Add("Size", 80)
            lstFiles.Columns.Add("Address", 80)

            ' Buttons panel
            Dim buttonsPanel As New FlowLayoutPanel()
            buttonsPanel.Dock = DockStyle.Fill
            buttonsPanel.FlowDirection = FlowDirection.RightToLeft
            buttonsPanel.WrapContents = False

            btnClose = New Button()
            btnClose.Text = "Close"
            btnClose.Size = New Size(80, 30)
            btnClose.Margin = New Padding(5)
            btnClose.DialogResult = DialogResult.Cancel

            btnBinaryUpload = New Button()
            btnBinaryUpload.Text = "Binary Upload"
            btnBinaryUpload.Size = New Size(100, 30)
            btnBinaryUpload.Margin = New Padding(5)

            btnZipUpload = New Button()
            btnZipUpload.Text = "Zip Upload"
            btnZipUpload.Size = New Size(100, 30)
            btnZipUpload.Margin = New Padding(5)
            btnZipUpload.Enabled = Not String.IsNullOrEmpty(zipPath)

            btnOpenFolder = New Button()
            btnOpenFolder.Text = "Open Folder"
            btnOpenFolder.Size = New Size(100, 30)
            btnOpenFolder.Margin = New Padding(5)

            buttonsPanel.Controls.Add(btnClose)
            buttonsPanel.Controls.Add(btnBinaryUpload)
            buttonsPanel.Controls.Add(btnZipUpload)
            buttonsPanel.Controls.Add(btnOpenFolder)

            ' Add controls to main layout
            mainLayout.Controls.Add(lblTitle, 0, 0)
            mainLayout.Controls.Add(lblDescription, 0, 1)
            mainLayout.Controls.Add(lstFiles, 0, 2)
            mainLayout.Controls.Add(buttonsPanel, 0, 3)

            ' Add main layout to form
            Me.Controls.Add(mainLayout)

            ' Wire up events
            AddHandler btnOpenFolder.Click, AddressOf btnOpenFolder_Click
            AddHandler btnZipUpload.Click, AddressOf btnZipUpload_Click
            AddHandler btnBinaryUpload.Click, AddressOf btnBinaryUpload_Click
            AddHandler btnClose.Click, AddressOf btnClose_Click

            ' Set accept and cancel buttons
            Me.AcceptButton = btnClose
            Me.CancelButton = btnClose
        End Sub

        Private Sub PopulateFilesList(files As List(Of String))
            lstFiles.Items.Clear()

            ' Read the flash_addresses.txt file if it exists
            Dim addressMap As New Dictionary(Of String, String)()
            Dim addressFilePath As String = Path.Combine(exportPath, "flash_addresses.txt")

            If File.Exists(addressFilePath) Then
                Try
                    Dim lines As String() = File.ReadAllLines(addressFilePath)
                    For Each line As String In lines
                        ' Skip empty lines and comments
                        If String.IsNullOrWhiteSpace(line) OrElse line.TrimStart().StartsWith("#") Then
                            Continue For
                        End If

                        ' Parse line in format "filename: address"
                        Dim parts As String() = line.Split(New Char() {":"c}, 2)
                        If parts.Length = 2 Then
                            Dim fileName As String = parts(0).Trim()
                            Dim address As String = parts(1).Trim()
                            addressMap(fileName) = address
                        End If
                    Next
                Catch ex As Exception
                    ' Ignore errors reading address file
                End Try
            End If

            ' Add files to the list
            For Each filePath As String In files
                If File.Exists(filePath) Then
                    Dim fileName As String = Path.GetFileName(filePath)
                    Dim fileInfo As New FileInfo(filePath)
                    Dim fileSize As String = GetFileSizeString(fileInfo.Length)

                    ' Get flash address from map or use default
                    Dim address As String = "N/A"
                    If addressMap.ContainsKey(fileName) Then
                        address = addressMap(fileName)
                    End If

                    Dim item As New ListViewItem(fileName)
                    item.SubItems.Add(fileSize)
                    item.SubItems.Add(address)
                    item.Tag = filePath

                    lstFiles.Items.Add(item)
                End If
            Next

            ' Auto-size columns
            For Each column As ColumnHeader In lstFiles.Columns
                column.Width = -2  ' Auto-size
            Next
        End Sub

        Private Function GetFileSizeString(bytes As Long) As String
            If bytes < 1024 Then
                Return $"{bytes} bytes"
            ElseIf bytes < 1024 * 1024 Then
                Return $"{bytes / 1024:F1} KB"
            Else
                Return $"{bytes / (1024 * 1024):F1} MB"
            End If
        End Function

        Private Sub btnOpenFolder_Click(sender As Object, e As EventArgs)
            Try
                Process.Start("explorer.exe", exportPath)
            Catch ex As Exception
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnZipUpload_Click(sender As Object, e As EventArgs)
            Try
                If File.Exists(zipPath) Then
                    ' Open ZipUploadForm with the zip file pre-selected
                    Dim zipUploadForm As New ZipUploadForm()

                    ' Set the zip path via reflection since there's no public property
                    Dim txtZipPathField = zipUploadForm.GetType().GetField("txtZipPath", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                    If txtZipPathField IsNot Nothing Then
                        Dim txtZipPath As TextBox = DirectCast(txtZipPathField.GetValue(zipUploadForm), TextBox)
                        txtZipPath.Text = zipPath
                    End If

                    zipUploadForm.ShowDialog()
                Else
                    MessageBox.Show("ZIP file not found or has not been created.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Catch ex As Exception
                MessageBox.Show($"Error opening ZIP upload: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnBinaryUpload_Click(sender As Object, e As EventArgs)
            Try
                ' Get selected file or use the first application binary
                Dim applicationFile As String = String.Empty
                Dim bootloaderFile As String = String.Empty
                Dim partitionFile As String = String.Empty
                Dim bootAppFile As String = String.Empty

                ' Find each type of file based on its name
                For Each item As ListViewItem In lstFiles.Items
                    Dim filePath As String = item.Tag.ToString()
                    Dim fileName As String = Path.GetFileName(filePath).ToLower()

                    If fileName.Contains("bootloader") Then
                        bootloaderFile = filePath
                    ElseIf fileName.Contains("partition") Then
                        partitionFile = filePath
                    ElseIf fileName.Contains("boot_app0") Then
                        bootAppFile = filePath
                    ElseIf fileName.EndsWith(".bin") AndAlso Not (fileName.Contains("bootloader") OrElse fileName.Contains("partition") OrElse fileName.Contains("boot_app0")) Then
                        applicationFile = filePath
                    End If
                Next

                If String.IsNullOrEmpty(applicationFile) Then
                    ' If no application file is found, try to use the selected file
                    If lstFiles.SelectedItems.Count > 0 Then
                        applicationFile = lstFiles.SelectedItems(0).Tag.ToString()
                    End If
                End If

                If String.IsNullOrEmpty(applicationFile) Then
                    MessageBox.Show("Please select a binary file to upload.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                ' Open BinaryUploadForm with pre-selected files
                Dim binaryUploadForm As New BinaryUploadForm()

                ' Set file paths via reflection
                If Not String.IsNullOrEmpty(applicationFile) Then
                    Dim txtApplicationPathField = binaryUploadForm.GetType().GetField("txtApplicationPath", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                    If txtApplicationPathField IsNot Nothing Then
                        Dim txtApplicationPath As TextBox = DirectCast(txtApplicationPathField.GetValue(binaryUploadForm), TextBox)
                        txtApplicationPath.Text = applicationFile
                    End If
                End If

                If Not String.IsNullOrEmpty(bootloaderFile) Then
                    Dim txtBootloaderPathField = binaryUploadForm.GetType().GetField("txtBootloaderPath", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                    If txtBootloaderPathField IsNot Nothing Then
                        Dim txtBootloaderPath As TextBox = DirectCast(txtBootloaderPathField.GetValue(binaryUploadForm), TextBox)
                        txtBootloaderPath.Text = bootloaderFile
                    End If
                End If

                If Not String.IsNullOrEmpty(partitionFile) Then
                    Dim txtPartitionPathField = binaryUploadForm.GetType().GetField("txtPartitionPath", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                    If txtPartitionPathField IsNot Nothing Then
                        Dim txtPartitionPath As TextBox = DirectCast(txtPartitionPathField.GetValue(binaryUploadForm), TextBox)
                        txtPartitionPath.Text = partitionFile
                    End If
                End If

                If Not String.IsNullOrEmpty(bootAppFile) Then
                    Dim txtBootApp0PathField = binaryUploadForm.GetType().GetField("txtBootApp0Path", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                    If txtBootApp0PathField IsNot Nothing Then
                        Dim txtBootApp0Path As TextBox = DirectCast(txtBootApp0PathField.GetValue(binaryUploadForm), TextBox)
                        txtBootApp0Path.Text = bootAppFile
                    End If
                End If

                binaryUploadForm.ShowDialog()
            Catch ex As Exception
                MessageBox.Show($"Error opening binary upload: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnClose_Click(sender As Object, e As EventArgs)
            Me.Close()
        End Sub
    End Class
End Namespace