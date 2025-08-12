Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.IO.Compression
Imports System.Windows.Forms

Namespace KC_LINK_LoaderV1._1
    Public Class BinaryManagerForm
        Inherits Form

        ' UI Controls
        Private WithEvents lstProjects As ListView
        Private WithEvents lstBinaries As ListView
        Private WithEvents btnRefresh As Button
        Private WithEvents btnZipUpload As Button
        Private WithEvents btnBinaryUpload As Button
        Private WithEvents btnClose As Button
        Private WithEvents btnOpenFolder As Button
        Private WithEvents btnCreateZip As Button

        ' Current selection
        Private selectedProjectPath As String = String.Empty

        Public Sub New()
            MyBase.New()
            InitializeComponent()
            LoadProjects()
        End Sub

        Private Sub InitializeComponent()
            ' Form setup
            Me.Text = "Binary Manager"
            Me.Size = New Size(800, 600)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.MinimizeBox = False
            Me.MaximizeBox = False

            ' Main layout
            Dim mainLayout As New TableLayoutPanel()
            mainLayout.Dock = DockStyle.Fill
            mainLayout.RowCount = 3
            mainLayout.ColumnCount = 2
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))  ' Header
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Content
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))  ' Buttons
            mainLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))  ' Project list
            mainLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70))  ' Binary list
            mainLayout.Padding = New Padding(10)

            ' Headers
            Dim lblProjects As New Label()
            lblProjects.Text = "Projects"
            lblProjects.Font = New Font(lblProjects.Font, FontStyle.Bold)
            lblProjects.Dock = DockStyle.Fill
            lblProjects.TextAlign = ContentAlignment.MiddleLeft

            Dim lblBinaries As New Label()
            lblBinaries.Text = "Binary Files"
            lblBinaries.Font = New Font(lblBinaries.Font, FontStyle.Bold)
            lblBinaries.Dock = DockStyle.Fill
            lblBinaries.TextAlign = ContentAlignment.MiddleLeft

            ' Project list
            lstProjects = New ListView()
            lstProjects.View = View.Details
            lstProjects.FullRowSelect = True
            lstProjects.Dock = DockStyle.Fill
            lstProjects.Columns.Add("Project", 250)

            ' Binary list
            lstBinaries = New ListView()
            lstBinaries.View = View.Details
            lstBinaries.FullRowSelect = True
            lstBinaries.Dock = DockStyle.Fill
            lstBinaries.Columns.Add("File", 250)
            lstBinaries.Columns.Add("Size", 80)
            lstBinaries.Columns.Add("Last Modified", 150)

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
            btnBinaryUpload.Enabled = False

            btnZipUpload = New Button()
            btnZipUpload.Text = "Zip Upload"
            btnZipUpload.Size = New Size(100, 30)
            btnZipUpload.Margin = New Padding(5)
            btnZipUpload.Enabled = False

            btnCreateZip = New Button()
            btnCreateZip.Text = "Create ZIP"
            btnCreateZip.Size = New Size(100, 30)
            btnCreateZip.Margin = New Padding(5)
            btnCreateZip.Enabled = False

            btnOpenFolder = New Button()
            btnOpenFolder.Text = "Open Folder"
            btnOpenFolder.Size = New Size(100, 30)
            btnOpenFolder.Margin = New Padding(5)
            btnOpenFolder.Enabled = False

            btnRefresh = New Button()
            btnRefresh.Text = "Refresh"
            btnRefresh.Size = New Size(80, 30)
            btnRefresh.Margin = New Padding(5)

            buttonsPanel.Controls.Add(btnClose)
            buttonsPanel.Controls.Add(btnBinaryUpload)
            buttonsPanel.Controls.Add(btnZipUpload)
            buttonsPanel.Controls.Add(btnCreateZip)
            buttonsPanel.Controls.Add(btnOpenFolder)
            buttonsPanel.Controls.Add(btnRefresh)

            ' Add controls to main layout
            mainLayout.Controls.Add(lblProjects, 0, 0)
            mainLayout.Controls.Add(lblBinaries, 1, 0)
            mainLayout.Controls.Add(lstProjects, 0, 1)
            mainLayout.Controls.Add(lstBinaries, 1, 1)
            mainLayout.Controls.Add(buttonsPanel, 0, 2)
            mainLayout.SetColumnSpan(buttonsPanel, 2)

            ' Add main layout to form
            Me.Controls.Add(mainLayout)

            ' Wire up events
            AddHandler lstProjects.SelectedIndexChanged, AddressOf lstProjects_SelectedIndexChanged
            AddHandler lstBinaries.SelectedIndexChanged, AddressOf lstBinaries_SelectedIndexChanged
            AddHandler btnRefresh.Click, AddressOf btnRefresh_Click
            AddHandler btnOpenFolder.Click, AddressOf btnOpenFolder_Click
            AddHandler btnCreateZip.Click, AddressOf btnCreateZip_Click
            AddHandler btnZipUpload.Click, AddressOf btnZipUpload_Click
            AddHandler btnBinaryUpload.Click, AddressOf btnBinaryUpload_Click

            ' Set accept and cancel buttons
            Me.AcceptButton = btnClose
            Me.CancelButton = btnClose
        End Sub

        Private Sub LoadProjects()
            lstProjects.Items.Clear()

            ' Get recent projects from settings
            If My.Settings.RecentProjects IsNot Nothing Then
                For i As Integer = My.Settings.RecentProjects.Count - 1 To 0 Step -1
                    Dim projectPath = My.Settings.RecentProjects(i)
                    If Directory.Exists(projectPath) Then
                        Dim projectName = Path.GetFileName(projectPath)
                        Dim item As New ListViewItem(projectName)
                        item.Tag = projectPath
                        lstProjects.Items.Add(item)
                    End If
                Next
            End If

            ' Auto-size columns
            For Each column As ColumnHeader In lstProjects.Columns
                column.Width = -2  ' Auto-size
            Next
        End Sub

        Private Sub LoadBinaries(projectPath As String)
            lstBinaries.Items.Clear()

            ' Check if export folder exists
            Dim exportPath As String = Path.Combine(projectPath, "export")
            If Not Directory.Exists(exportPath) Then
                Return
            End If

            ' Load binary files
            For Each filePath In Directory.GetFiles(exportPath)
                Dim fileInfo As New FileInfo(filePath)
                Dim fileName As String = Path.GetFileName(filePath)

                Dim item As New ListViewItem(fileName)
                item.SubItems.Add(GetFileSizeString(fileInfo.Length))
                item.SubItems.Add(fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"))
                item.Tag = filePath

                lstBinaries.Items.Add(item)
            Next

            ' Auto-size columns
            For Each column As ColumnHeader In lstBinaries.Columns
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

        Private Sub lstProjects_SelectedIndexChanged(sender As Object, e As EventArgs)
            If lstProjects.SelectedItems.Count > 0 Then
                selectedProjectPath = lstProjects.SelectedItems(0).Tag.ToString()
                LoadBinaries(selectedProjectPath)
                btnOpenFolder.Enabled = True
                btnCreateZip.Enabled = HasBinaryFiles(selectedProjectPath)
            Else
                selectedProjectPath = String.Empty
                lstBinaries.Items.Clear()
                btnOpenFolder.Enabled = False
                btnCreateZip.Enabled = False
            End If
        End Sub

        Private Sub lstBinaries_SelectedIndexChanged(sender As Object, e As EventArgs)
            btnZipUpload.Enabled = False
            btnBinaryUpload.Enabled = False

            If lstBinaries.SelectedItems.Count > 0 Then
                Dim selectedFile As String = lstBinaries.SelectedItems(0).Tag.ToString()

                If selectedFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) Then
                    btnZipUpload.Enabled = True
                    btnBinaryUpload.Enabled = False
                ElseIf selectedFile.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) Then
                    btnBinaryUpload.Enabled = True
                    btnZipUpload.Enabled = False
                End If
            End If
        End Sub

        Private Sub btnRefresh_Click(sender As Object, e As EventArgs)
            ' Reload projects
            LoadProjects()

            ' Reload binaries for selected project
            If Not String.IsNullOrEmpty(selectedProjectPath) Then
                LoadBinaries(selectedProjectPath)
            End If
        End Sub

        Private Sub btnOpenFolder_Click(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(selectedProjectPath) Then
                Return
            End If

            Dim exportPath As String = Path.Combine(selectedProjectPath, "export")
            If Not Directory.Exists(exportPath) Then
                Directory.CreateDirectory(exportPath)
            End If

            Try
                Process.Start("explorer.exe", exportPath)
            Catch ex As Exception
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnCreateZip_Click(sender As Object, e As EventArgs)
            If String.IsNullOrEmpty(selectedProjectPath) Then
                Return
            End If

            Dim exportPath As String = Path.Combine(selectedProjectPath, "export")
            If Not Directory.Exists(exportPath) Then
                MessageBox.Show("No binary files found to create ZIP package.", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Check if there are any .bin files
            If Not HasBinaryFiles(selectedProjectPath) Then
                MessageBox.Show("No binary files found to create ZIP package.", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                ' Get project name
                Dim projectName As String = Path.GetFileName(selectedProjectPath)

                ' Create ZIP file
                Dim zipPath As String = Path.Combine(exportPath, $"{projectName}_firmware.zip")

                ' Delete existing ZIP if it exists
                If File.Exists(zipPath) Then
                    File.Delete(zipPath)
                End If

                ' Get all binary files
                Dim files As New List(Of String)()
                For Each filePath In Directory.GetFiles(exportPath, "*.bin")
                    files.Add(filePath)
                Next

                ' Add manifest file if it exists
                Dim manifestPath = Path.Combine(exportPath, "flash_addresses.txt")
                If File.Exists(manifestPath) Then
                    files.Add(manifestPath)
                End If

                ' Create the ZIP file
                Using zipArchive As IO.Compression.ZipArchive = IO.Compression.ZipFile.Open(zipPath, IO.Compression.ZipArchiveMode.Create)
                    For Each filePath In files
                        zipArchive.CreateEntryFromFile(filePath, Path.GetFileName(filePath))
                    Next
                End Using

                MessageBox.Show($"ZIP package created successfully: {zipPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Refresh binary list
                LoadBinaries(selectedProjectPath)
            Catch ex As Exception
                MessageBox.Show($"Error creating ZIP package: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnZipUpload_Click(sender As Object, e As EventArgs)
            If lstBinaries.SelectedItems.Count = 0 Then
                Return
            End If

            Dim zipPath As String = lstBinaries.SelectedItems(0).Tag.ToString()

            If Not zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("Please select a ZIP file for upload.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                ' Open ZipUploadForm with the zip file pre-selected
                Dim zipUploadForm As New ZipUploadForm()

                ' Set the zip path via reflection since there's no public property
                Dim txtZipPathField = zipUploadForm.GetType().GetField("txtZipPath", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                If txtZipPathField IsNot Nothing Then
                    Dim txtZipPath As TextBox = DirectCast(txtZipPathField.GetValue(zipUploadForm), TextBox)
                    txtZipPath.Text = zipPath
                End If

                zipUploadForm.ShowDialog()
            Catch ex As Exception
                MessageBox.Show($"Error opening ZIP upload: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub btnBinaryUpload_Click(sender As Object, e As EventArgs)
            If lstBinaries.SelectedItems.Count = 0 Then
                Return
            End If

            Dim selectedFile As String = lstBinaries.SelectedItems(0).Tag.ToString()

            If Not selectedFile.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("Please select a binary file for upload.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                ' Get all binary files in the same directory
                Dim exportPath As String = Path.GetDirectoryName(selectedFile)
                Dim applicationFile As String = selectedFile
                Dim bootloaderFile As String = String.Empty
                Dim partitionFile As String = String.Empty
                Dim bootAppFile As String = String.Empty

                ' If the selected file is not the main application, find the right files
                Dim fileName As String = Path.GetFileName(selectedFile).ToLower()
                If fileName.Contains("bootloader") OrElse fileName.Contains("partition") OrElse fileName.Contains("boot_app0") Then
                    ' Find application file and other binary files
                    For Each filePath In Directory.GetFiles(exportPath, "*.bin")
                        Dim fileNameLower As String = Path.GetFileName(filePath).ToLower()

                        If fileNameLower.Contains("bootloader") Then
                            bootloaderFile = filePath
                        ElseIf fileNameLower.Contains("partition") Then
                            partitionFile = filePath
                        ElseIf fileNameLower.Contains("boot_app0") Then
                            bootAppFile = filePath
                        ElseIf Not (fileNameLower.Contains("bootloader") OrElse fileNameLower.Contains("partition") OrElse fileNameLower.Contains("boot_app0")) Then
                            applicationFile = filePath
                        End If
                    Next
                Else
                    ' Find bootloader, partition, and boot_app0 files
                    For Each filePath In Directory.GetFiles(exportPath, "*.bin")
                        Dim fileNameLower As String = Path.GetFileName(filePath).ToLower()

                        If fileNameLower.Contains("bootloader") Then
                            bootloaderFile = filePath
                        ElseIf fileNameLower.Contains("partition") Then
                            partitionFile = filePath
                        ElseIf fileNameLower.Contains("boot_app0") Then
                            bootAppFile = filePath
                        End If
                    Next
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

        Private Function HasBinaryFiles(projectPath As String) As Boolean
            Dim exportPath As String = Path.Combine(projectPath, "export")
            If Not Directory.Exists(exportPath) Then
                Return False
            End If

            Return Directory.GetFiles(exportPath, "*.bin").Length > 0
        End Function
    End Class
End Namespace