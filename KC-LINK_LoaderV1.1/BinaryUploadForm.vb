Imports System
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Diagnostics
Imports System.Threading
Imports System.IO.Ports


Namespace KC_LINK_LoaderV1._1
    Public Class BinaryUploadForm
        Inherits Form

        ' UI Controls
        Private WithEvents lblBootloader As Label
        Private WithEvents txtBootloaderPath As TextBox
        Private WithEvents btnBrowseBootloader As Button
        Private WithEvents lblBootloaderAddr As Label
        Private WithEvents txtBootloaderAddr As TextBox

        Private WithEvents lblPartition As Label
        Private WithEvents txtPartitionPath As TextBox
        Private WithEvents btnBrowsePartition As Button
        Private WithEvents lblPartitionAddr As Label
        Private WithEvents txtPartitionAddr As TextBox

        Private WithEvents lblBootApp0 As Label
        Private WithEvents txtBootApp0Path As TextBox
        Private WithEvents btnBrowseBootApp0 As Button
        Private WithEvents lblBootApp0Addr As Label
        Private WithEvents txtBootApp0Addr As TextBox

        Private WithEvents lblApplication As Label
        Private WithEvents txtApplicationPath As TextBox
        Private WithEvents btnBrowseApplication As Button
        Private WithEvents lblApplicationAddr As Label
        Private WithEvents txtApplicationAddr As TextBox

        Private WithEvents cmbSerialPort As ComboBox
        Private WithEvents lblSerialPort As Label
        Private WithEvents btnRefreshPorts As Button

        Private WithEvents btnUpload As Button
        Private WithEvents btnCancel As Button
        Private WithEvents progressBar As ProgressBar
        Private WithEvents txtOutput As RichTextBox

        Private WithEvents bgWorker As System.ComponentModel.BackgroundWorker
        Private isUploading As Boolean = False
        Private esptoolPath As String = ""
        Private esptoolVersion As String = ""
        Private serialPortsInUse As New List(Of String)
        Private serialPortOpener As SerialPort = Nothing

        ' Constructor
        Public Sub New()
            MyBase.New()
            InitializeComponent()
            RefreshPortList()
            LoadDefaultAddresses()
            FindEsptoolPath()
        End Sub

        Private Sub InitializeComponent()
            Me.mainLayout = New System.Windows.Forms.TableLayoutPanel()
            Me.binaryFilesPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.lblBootloader = New System.Windows.Forms.Label()
            Me.txtBootloaderPath = New System.Windows.Forms.TextBox()
            Me.btnBrowseBootloader = New System.Windows.Forms.Button()
            Me.txtBootloaderAddr = New System.Windows.Forms.TextBox()
            Me.lblPartition = New System.Windows.Forms.Label()
            Me.txtPartitionPath = New System.Windows.Forms.TextBox()
            Me.btnBrowsePartition = New System.Windows.Forms.Button()
            Me.txtPartitionAddr = New System.Windows.Forms.TextBox()
            Me.lblBootApp0 = New System.Windows.Forms.Label()
            Me.txtBootApp0Path = New System.Windows.Forms.TextBox()
            Me.btnBrowseBootApp0 = New System.Windows.Forms.Button()
            Me.txtBootApp0Addr = New System.Windows.Forms.TextBox()
            Me.lblApplication = New System.Windows.Forms.Label()
            Me.txtApplicationPath = New System.Windows.Forms.TextBox()
            Me.btnBrowseApplication = New System.Windows.Forms.Button()
            Me.txtApplicationAddr = New System.Windows.Forms.TextBox()
            Me.serialPortPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.lblSerialPort = New System.Windows.Forms.Label()
            Me.cmbSerialPort = New System.Windows.Forms.ComboBox()
            Me.btnRefreshPorts = New System.Windows.Forms.Button()
            Me.buttonsPanel = New System.Windows.Forms.FlowLayoutPanel()
            Me.btnCancel = New System.Windows.Forms.Button()
            Me.btnUpload = New System.Windows.Forms.Button()
            Me.progressBar = New System.Windows.Forms.ProgressBar()
            Me.txtOutput = New System.Windows.Forms.RichTextBox()
            Me.lblBootloaderAddr = New System.Windows.Forms.Label()
            Me.lblPartitionAddr = New System.Windows.Forms.Label()
            Me.lblBootApp0Addr = New System.Windows.Forms.Label()
            Me.lblApplicationAddr = New System.Windows.Forms.Label()
            Me.bgWorker = New System.ComponentModel.BackgroundWorker()
            Me.mainLayout.SuspendLayout()
            Me.binaryFilesPanel.SuspendLayout()
            Me.serialPortPanel.SuspendLayout()
            Me.buttonsPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'mainLayout
            '
            Me.mainLayout.ColumnCount = 1
            Me.mainLayout.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.mainLayout.Controls.Add(Me.binaryFilesPanel, 0, 0)
            Me.mainLayout.Controls.Add(Me.serialPortPanel, 0, 2)
            Me.mainLayout.Controls.Add(Me.buttonsPanel, 0, 4)
            Me.mainLayout.Controls.Add(Me.progressBar, 0, 5)
            Me.mainLayout.Controls.Add(Me.txtOutput, 0, 6)
            Me.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill
            Me.mainLayout.Location = New System.Drawing.Point(0, 0)
            Me.mainLayout.Name = "mainLayout"
            Me.mainLayout.Padding = New System.Windows.Forms.Padding(10)
            Me.mainLayout.RowCount = 7
            Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200.0!))
            Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 10.0!))
            Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 10.0!))
            Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40.0!))
            Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
            Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.mainLayout.Size = New System.Drawing.Size(684, 561)
            Me.mainLayout.TabIndex = 0
            '
            'binaryFilesPanel
            '
            Me.binaryFilesPanel.ColumnCount = 4
            Me.binaryFilesPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100.0!))
            Me.binaryFilesPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.binaryFilesPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80.0!))
            Me.binaryFilesPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100.0!))
            Me.binaryFilesPanel.Controls.Add(Me.lblBootloader, 0, 0)
            Me.binaryFilesPanel.Controls.Add(Me.txtBootloaderPath, 1, 0)
            Me.binaryFilesPanel.Controls.Add(Me.btnBrowseBootloader, 2, 0)
            Me.binaryFilesPanel.Controls.Add(Me.txtBootloaderAddr, 3, 0)
            Me.binaryFilesPanel.Controls.Add(Me.lblPartition, 0, 1)
            Me.binaryFilesPanel.Controls.Add(Me.txtPartitionPath, 1, 1)
            Me.binaryFilesPanel.Controls.Add(Me.btnBrowsePartition, 2, 1)
            Me.binaryFilesPanel.Controls.Add(Me.txtPartitionAddr, 3, 1)
            Me.binaryFilesPanel.Controls.Add(Me.lblBootApp0, 0, 2)
            Me.binaryFilesPanel.Controls.Add(Me.txtBootApp0Path, 1, 2)
            Me.binaryFilesPanel.Controls.Add(Me.btnBrowseBootApp0, 2, 2)
            Me.binaryFilesPanel.Controls.Add(Me.txtBootApp0Addr, 3, 2)
            Me.binaryFilesPanel.Controls.Add(Me.lblApplication, 0, 3)
            Me.binaryFilesPanel.Controls.Add(Me.txtApplicationPath, 1, 3)
            Me.binaryFilesPanel.Controls.Add(Me.btnBrowseApplication, 2, 3)
            Me.binaryFilesPanel.Controls.Add(Me.txtApplicationAddr, 3, 3)
            Me.binaryFilesPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me.binaryFilesPanel.Location = New System.Drawing.Point(13, 13)
            Me.binaryFilesPanel.Name = "binaryFilesPanel"
            Me.binaryFilesPanel.RowCount = 4
            Me.binaryFilesPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0!))
            Me.binaryFilesPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0!))
            Me.binaryFilesPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0!))
            Me.binaryFilesPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.0!))
            Me.binaryFilesPanel.Size = New System.Drawing.Size(658, 194)
            Me.binaryFilesPanel.TabIndex = 0
            '
            'lblBootloader
            '
            Me.lblBootloader.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblBootloader.Location = New System.Drawing.Point(3, 0)
            Me.lblBootloader.Name = "lblBootloader"
            Me.lblBootloader.Size = New System.Drawing.Size(94, 48)
            Me.lblBootloader.TabIndex = 0
            Me.lblBootloader.Text = "Bootloader:"
            Me.lblBootloader.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'txtBootloaderPath
            '
            Me.txtBootloaderPath.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtBootloaderPath.Location = New System.Drawing.Point(103, 3)
            Me.txtBootloaderPath.Name = "txtBootloaderPath"
            Me.txtBootloaderPath.Size = New System.Drawing.Size(372, 20)
            Me.txtBootloaderPath.TabIndex = 1
            '
            'btnBrowseBootloader
            '
            Me.btnBrowseBootloader.Dock = System.Windows.Forms.DockStyle.Fill
            Me.btnBrowseBootloader.Location = New System.Drawing.Point(481, 3)
            Me.btnBrowseBootloader.Name = "btnBrowseBootloader"
            Me.btnBrowseBootloader.Size = New System.Drawing.Size(74, 42)
            Me.btnBrowseBootloader.TabIndex = 2
            Me.btnBrowseBootloader.Text = "Browse..."
            '
            'txtBootloaderAddr
            '
            Me.txtBootloaderAddr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtBootloaderAddr.Location = New System.Drawing.Point(561, 3)
            Me.txtBootloaderAddr.Name = "txtBootloaderAddr"
            Me.txtBootloaderAddr.Size = New System.Drawing.Size(94, 20)
            Me.txtBootloaderAddr.TabIndex = 3
            Me.txtBootloaderAddr.Text = "0x1000"
            '
            'lblPartition
            '
            Me.lblPartition.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblPartition.Location = New System.Drawing.Point(3, 48)
            Me.lblPartition.Name = "lblPartition"
            Me.lblPartition.Size = New System.Drawing.Size(94, 48)
            Me.lblPartition.TabIndex = 4
            Me.lblPartition.Text = "Partition Table:"
            Me.lblPartition.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'txtPartitionPath
            '
            Me.txtPartitionPath.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtPartitionPath.Location = New System.Drawing.Point(103, 51)
            Me.txtPartitionPath.Name = "txtPartitionPath"
            Me.txtPartitionPath.Size = New System.Drawing.Size(372, 20)
            Me.txtPartitionPath.TabIndex = 5
            '
            'btnBrowsePartition
            '
            Me.btnBrowsePartition.Dock = System.Windows.Forms.DockStyle.Fill
            Me.btnBrowsePartition.Location = New System.Drawing.Point(481, 51)
            Me.btnBrowsePartition.Name = "btnBrowsePartition"
            Me.btnBrowsePartition.Size = New System.Drawing.Size(74, 42)
            Me.btnBrowsePartition.TabIndex = 6
            Me.btnBrowsePartition.Text = "Browse..."
            '
            'txtPartitionAddr
            '
            Me.txtPartitionAddr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtPartitionAddr.Location = New System.Drawing.Point(561, 51)
            Me.txtPartitionAddr.Name = "txtPartitionAddr"
            Me.txtPartitionAddr.Size = New System.Drawing.Size(94, 20)
            Me.txtPartitionAddr.TabIndex = 7
            Me.txtPartitionAddr.Text = "0x8000"
            '
            'lblBootApp0
            '
            Me.lblBootApp0.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblBootApp0.Location = New System.Drawing.Point(3, 96)
            Me.lblBootApp0.Name = "lblBootApp0"
            Me.lblBootApp0.Size = New System.Drawing.Size(94, 48)
            Me.lblBootApp0.TabIndex = 8
            Me.lblBootApp0.Text = "Boot App 0:"
            Me.lblBootApp0.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'txtBootApp0Path
            '
            Me.txtBootApp0Path.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtBootApp0Path.Location = New System.Drawing.Point(103, 99)
            Me.txtBootApp0Path.Name = "txtBootApp0Path"
            Me.txtBootApp0Path.Size = New System.Drawing.Size(372, 20)
            Me.txtBootApp0Path.TabIndex = 9
            '
            'btnBrowseBootApp0
            '
            Me.btnBrowseBootApp0.Dock = System.Windows.Forms.DockStyle.Fill
            Me.btnBrowseBootApp0.Location = New System.Drawing.Point(481, 99)
            Me.btnBrowseBootApp0.Name = "btnBrowseBootApp0"
            Me.btnBrowseBootApp0.Size = New System.Drawing.Size(74, 42)
            Me.btnBrowseBootApp0.TabIndex = 10
            Me.btnBrowseBootApp0.Text = "Browse..."
            '
            'txtBootApp0Addr
            '
            Me.txtBootApp0Addr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtBootApp0Addr.Location = New System.Drawing.Point(561, 99)
            Me.txtBootApp0Addr.Name = "txtBootApp0Addr"
            Me.txtBootApp0Addr.Size = New System.Drawing.Size(94, 20)
            Me.txtBootApp0Addr.TabIndex = 11
            Me.txtBootApp0Addr.Text = "0xe000"
            '
            'lblApplication
            '
            Me.lblApplication.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblApplication.Location = New System.Drawing.Point(3, 144)
            Me.lblApplication.Name = "lblApplication"
            Me.lblApplication.Size = New System.Drawing.Size(94, 50)
            Me.lblApplication.TabIndex = 12
            Me.lblApplication.Text = "Application:"
            Me.lblApplication.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'txtApplicationPath
            '
            Me.txtApplicationPath.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtApplicationPath.Location = New System.Drawing.Point(103, 147)
            Me.txtApplicationPath.Name = "txtApplicationPath"
            Me.txtApplicationPath.Size = New System.Drawing.Size(372, 20)
            Me.txtApplicationPath.TabIndex = 13
            '
            'btnBrowseApplication
            '
            Me.btnBrowseApplication.Dock = System.Windows.Forms.DockStyle.Fill
            Me.btnBrowseApplication.Location = New System.Drawing.Point(481, 147)
            Me.btnBrowseApplication.Name = "btnBrowseApplication"
            Me.btnBrowseApplication.Size = New System.Drawing.Size(74, 44)
            Me.btnBrowseApplication.TabIndex = 14
            Me.btnBrowseApplication.Text = "Browse..."
            '
            'txtApplicationAddr
            '
            Me.txtApplicationAddr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtApplicationAddr.Location = New System.Drawing.Point(561, 147)
            Me.txtApplicationAddr.Name = "txtApplicationAddr"
            Me.txtApplicationAddr.Size = New System.Drawing.Size(94, 20)
            Me.txtApplicationAddr.TabIndex = 15
            Me.txtApplicationAddr.Text = "0x10000"
            '
            'serialPortPanel
            '
            Me.serialPortPanel.ColumnCount = 3
            Me.serialPortPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100.0!))
            Me.serialPortPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.serialPortPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100.0!))
            Me.serialPortPanel.Controls.Add(Me.lblSerialPort, 0, 0)
            Me.serialPortPanel.Controls.Add(Me.cmbSerialPort, 1, 0)
            Me.serialPortPanel.Controls.Add(Me.btnRefreshPorts, 2, 0)
            Me.serialPortPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me.serialPortPanel.Location = New System.Drawing.Point(13, 223)
            Me.serialPortPanel.Name = "serialPortPanel"
            Me.serialPortPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.serialPortPanel.Size = New System.Drawing.Size(658, 34)
            Me.serialPortPanel.TabIndex = 1
            '
            'lblSerialPort
            '
            Me.lblSerialPort.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblSerialPort.Location = New System.Drawing.Point(3, 0)
            Me.lblSerialPort.Name = "lblSerialPort"
            Me.lblSerialPort.Size = New System.Drawing.Size(94, 34)
            Me.lblSerialPort.TabIndex = 0
            Me.lblSerialPort.Text = "Serial Port:"
            Me.lblSerialPort.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'cmbSerialPort
            '
            Me.cmbSerialPort.Dock = System.Windows.Forms.DockStyle.Fill
            Me.cmbSerialPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cmbSerialPort.Location = New System.Drawing.Point(103, 3)
            Me.cmbSerialPort.Name = "cmbSerialPort"
            Me.cmbSerialPort.Size = New System.Drawing.Size(452, 21)
            Me.cmbSerialPort.TabIndex = 1
            '
            'btnRefreshPorts
            '
            Me.btnRefreshPorts.Dock = System.Windows.Forms.DockStyle.Fill
            Me.btnRefreshPorts.Location = New System.Drawing.Point(561, 3)
            Me.btnRefreshPorts.Name = "btnRefreshPorts"
            Me.btnRefreshPorts.Size = New System.Drawing.Size(94, 28)
            Me.btnRefreshPorts.TabIndex = 2
            Me.btnRefreshPorts.Text = "Refresh"
            '
            'buttonsPanel
            '
            Me.buttonsPanel.Controls.Add(Me.btnCancel)
            Me.buttonsPanel.Controls.Add(Me.btnUpload)
            Me.buttonsPanel.Dock = System.Windows.Forms.DockStyle.Fill
            Me.buttonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft
            Me.buttonsPanel.Location = New System.Drawing.Point(13, 273)
            Me.buttonsPanel.Name = "buttonsPanel"
            Me.buttonsPanel.Size = New System.Drawing.Size(658, 34)
            Me.buttonsPanel.TabIndex = 2
            Me.buttonsPanel.WrapContents = False
            '
            'btnCancel
            '
            Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
            Me.btnCancel.Location = New System.Drawing.Point(553, 5)
            Me.btnCancel.Margin = New System.Windows.Forms.Padding(5)
            Me.btnCancel.Name = "btnCancel"
            Me.btnCancel.Size = New System.Drawing.Size(100, 30)
            Me.btnCancel.TabIndex = 0
            Me.btnCancel.Text = "Cancel"
            '
            'btnUpload
            '
            Me.btnUpload.BackColor = System.Drawing.Color.LightGreen
            Me.btnUpload.Location = New System.Drawing.Point(443, 5)
            Me.btnUpload.Margin = New System.Windows.Forms.Padding(5)
            Me.btnUpload.Name = "btnUpload"
            Me.btnUpload.Size = New System.Drawing.Size(100, 30)
            Me.btnUpload.TabIndex = 1
            Me.btnUpload.Text = "Upload"
            Me.btnUpload.UseVisualStyleBackColor = False
            '
            'progressBar
            '
            Me.progressBar.Dock = System.Windows.Forms.DockStyle.Fill
            Me.progressBar.Location = New System.Drawing.Point(13, 313)
            Me.progressBar.Name = "progressBar"
            Me.progressBar.Size = New System.Drawing.Size(658, 24)
            Me.progressBar.TabIndex = 3
            '
            'txtOutput
            '
            Me.txtOutput.BackColor = System.Drawing.Color.Black
            Me.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill
            Me.txtOutput.Font = New System.Drawing.Font("Consolas", 9.0!)
            Me.txtOutput.ForeColor = System.Drawing.Color.LightGreen
            Me.txtOutput.Location = New System.Drawing.Point(13, 343)
            Me.txtOutput.Name = "txtOutput"
            Me.txtOutput.ReadOnly = True
            Me.txtOutput.Size = New System.Drawing.Size(658, 205)
            Me.txtOutput.TabIndex = 4
            Me.txtOutput.Text = ""
            '
            'lblBootloaderAddr
            '
            Me.lblBootloaderAddr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblBootloaderAddr.Location = New System.Drawing.Point(0, 0)
            Me.lblBootloaderAddr.Name = "lblBootloaderAddr"
            Me.lblBootloaderAddr.Size = New System.Drawing.Size(100, 23)
            Me.lblBootloaderAddr.TabIndex = 0
            Me.lblBootloaderAddr.Text = "Address:"
            Me.lblBootloaderAddr.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'lblPartitionAddr
            '
            Me.lblPartitionAddr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblPartitionAddr.Location = New System.Drawing.Point(0, 0)
            Me.lblPartitionAddr.Name = "lblPartitionAddr"
            Me.lblPartitionAddr.Size = New System.Drawing.Size(100, 23)
            Me.lblPartitionAddr.TabIndex = 0
            Me.lblPartitionAddr.Text = "Address:"
            Me.lblPartitionAddr.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'lblBootApp0Addr
            '
            Me.lblBootApp0Addr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblBootApp0Addr.Location = New System.Drawing.Point(0, 0)
            Me.lblBootApp0Addr.Name = "lblBootApp0Addr"
            Me.lblBootApp0Addr.Size = New System.Drawing.Size(100, 23)
            Me.lblBootApp0Addr.TabIndex = 0
            Me.lblBootApp0Addr.Text = "Address:"
            Me.lblBootApp0Addr.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'lblApplicationAddr
            '
            Me.lblApplicationAddr.Dock = System.Windows.Forms.DockStyle.Fill
            Me.lblApplicationAddr.Location = New System.Drawing.Point(0, 0)
            Me.lblApplicationAddr.Name = "lblApplicationAddr"
            Me.lblApplicationAddr.Size = New System.Drawing.Size(100, 23)
            Me.lblApplicationAddr.TabIndex = 0
            Me.lblApplicationAddr.Text = "Address:"
            Me.lblApplicationAddr.TextAlign = System.Drawing.ContentAlignment.MiddleRight
            '
            'bgWorker
            '
            Me.bgWorker.WorkerReportsProgress = True
            Me.bgWorker.WorkerSupportsCancellation = True
            '
            'BinaryUploadForm
            '
            Me.AcceptButton = Me.btnUpload
            Me.CancelButton = Me.btnCancel
            Me.ClientSize = New System.Drawing.Size(684, 561)
            Me.Controls.Add(Me.mainLayout)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "BinaryUploadForm"
            Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
            Me.Text = "ESP32 Binary Upload"
            Me.mainLayout.ResumeLayout(False)
            Me.binaryFilesPanel.ResumeLayout(False)
            Me.binaryFilesPanel.PerformLayout()
            Me.serialPortPanel.ResumeLayout(False)
            Me.buttonsPanel.ResumeLayout(False)
            Me.ResumeLayout(False)

        End Sub

        Private Sub BinaryUploadForm_FormClosing(sender As Object, e As FormClosingEventArgs)
            ' Close any open serial ports
            CloseSerialPort()
        End Sub

        Private Sub BrowseBootloader_Click(sender As Object, e As EventArgs) Handles btnBrowseBootloader.Click
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Bootloader Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtBootloaderPath.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub BrowsePartition_Click(sender As Object, e As EventArgs) Handles btnBrowsePartition.Click
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Partition Table Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtPartitionPath.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub BrowseBootApp0_Click(sender As Object, e As EventArgs) Handles btnBrowseBootApp0.Click
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Boot App 0 Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtBootApp0Path.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub BrowseApplication_Click(sender As Object, e As EventArgs) Handles btnBrowseApplication.Click
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*"
                openFileDialog.Title = "Select Application Binary File"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    txtApplicationPath.Text = openFileDialog.FileName
                End If
            End Using
        End Sub

        Private Sub RefreshPorts_Click(sender As Object, e As EventArgs) Handles btnRefreshPorts.Click
            RefreshPortList()
        End Sub

        Private Sub RefreshPortList()
            ' Remember current selection
            Dim currentSelection As String = ""
            If cmbSerialPort.SelectedItem IsNot Nothing Then
                currentSelection = cmbSerialPort.SelectedItem.ToString()
            End If

            cmbSerialPort.Items.Clear()
            serialPortsInUse.Clear()

            ' Get all available COM ports
            For Each port As String In My.Computer.Ports.SerialPortNames
                cmbSerialPort.Items.Add(port)

                ' Check if port is in use
                Try
                    Using testPort As New SerialPort(port)
                        testPort.Open()
                        testPort.Close()
                    End Using
                Catch ex As Exception
                    ' Port is likely in use
                    serialPortsInUse.Add(port)
                    AppendToOutput("Note: Port " & port & " appears to be in use by another application")
                End Try
            Next

            ' Try to restore previous selection
            If Not String.IsNullOrEmpty(currentSelection) AndAlso cmbSerialPort.Items.Contains(currentSelection) Then
                cmbSerialPort.SelectedItem = currentSelection
            ElseIf cmbSerialPort.Items.Count > 0 Then
                cmbSerialPort.SelectedIndex = 0
            End If

            AppendToOutput("Found " & cmbSerialPort.Items.Count & " serial ports")
            If serialPortsInUse.Count > 0 Then
                AppendToOutput("Note: " & serialPortsInUse.Count & " ports are currently in use")
            End If
        End Sub

        Private Sub LoadDefaultAddresses()
            ' Standard ESP32 flash addresses - updated based on ESP32 documentation
            txtBootloaderAddr.Text = KC_LINK_LoaderV1._1.BinaryExporter.DefaultBootloaderAddress    ' 0x1000
            txtPartitionAddr.Text = KC_LINK_LoaderV1._1.BinaryExporter.DefaultPartitionAddress      ' 0x8000
            txtBootApp0Addr.Text = KC_LINK_LoaderV1._1.BinaryExporter.DefaultBootApp0Address        ' 0xe000
            txtApplicationAddr.Text = KC_LINK_LoaderV1._1.BinaryExporter.DefaultApplicationAddress  ' 0x10000

            AppendToOutput("Using standard ESP32 flash addresses:")
            AppendToOutput("Bootloader: " & txtBootloaderAddr.Text)
            AppendToOutput("Partition Table: " & txtPartitionAddr.Text)
            AppendToOutput("Boot App 0: " & txtBootApp0Addr.Text)
            AppendToOutput("Application: " & txtApplicationAddr.Text)
        End Sub

        Private Sub FindEsptoolPath()
            ' Try to find esptool.py or esptool.exe in common locations
            esptoolPath = ""
            esptoolVersion = ""

            ' First check in Arduino ESP32 package (most reliable for ESP32 uploads)
            Dim esp32PackageDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "arduino15", "packages", "esp32")

            If Directory.Exists(esp32PackageDir) Then
                Dim esptoolFiles = Directory.GetFiles(esp32PackageDir, "esptool*", SearchOption.AllDirectories)
                If esptoolFiles.Length > 0 Then
                    For Each toolPath In esptoolFiles
                        If toolPath.EndsWith(".exe") OrElse toolPath.EndsWith(".py") Then
                            ' Found esptool - check version
                            esptoolPath = toolPath
                            AppendToOutput("Found ESP32 esptool at: " & esptoolPath)

                            ' Get version info
                            Try
                                Dim versionProcess As New Process()
                                versionProcess.StartInfo.FileName = esptoolPath
                                versionProcess.StartInfo.Arguments = "version"
                                versionProcess.StartInfo.UseShellExecute = False
                                versionProcess.StartInfo.CreateNoWindow = True
                                versionProcess.StartInfo.RedirectStandardOutput = True
                                versionProcess.StartInfo.RedirectStandardError = True

                                versionProcess.Start()
                                Dim versionOutput = versionProcess.StandardOutput.ReadToEnd()
                                versionProcess.WaitForExit()

                                If versionProcess.ExitCode = 0 Then
                                    esptoolVersion = versionOutput.Trim()
                                    AppendToOutput("Esptool version: " & esptoolVersion)
                                    Return ' Found working esptool
                                End If
                            Catch ex As Exception
                                ' Continue searching
                                AppendToOutput("Error checking esptool version: " & ex.Message)
                            End Try
                        End If
                    Next
                End If
            End If

            ' Look in typical Arduino CLI installation paths
            Dim arduinoCliPath As String = My.Settings.ArduinoCliPath
            If Not String.IsNullOrEmpty(arduinoCliPath) Then
                Dim arduinoDir = Path.GetDirectoryName(arduinoCliPath)

                ' Look for the ESP32 Arduino core
                Dim possibleEspToolPaths = New String() {
                    Path.Combine(arduinoDir, "esptool.exe"),
                    Path.Combine(arduinoDir, "esptool.py"),
                    Path.Combine(Path.GetDirectoryName(arduinoDir), "hardware", "espressif", "esp32", "tools", "esptool.py"),
                    Path.Combine(Path.GetDirectoryName(arduinoDir), "hardware", "espressif", "esp32", "tools", "esptool", "esptool.py")
                }

                For Each toolPath In possibleEspToolPaths
                    If File.Exists(toolPath) Then
                        esptoolPath = toolPath
                        AppendToOutput("Found esptool at: " & esptoolPath)
                        Return
                    End If
                Next
            End If

            ' If still not found, try to use the Python esptool module
            Try
                Dim pythonPath As String = ""
                Dim pythonProcess As New Process()
                pythonProcess.StartInfo.FileName = "python"
                pythonProcess.StartInfo.Arguments = "--version"
                pythonProcess.StartInfo.UseShellExecute = False
                pythonProcess.StartInfo.CreateNoWindow = True
                pythonProcess.StartInfo.RedirectStandardOutput = True

                Try
                    pythonProcess.Start()
                    pythonProcess.WaitForExit()
                    pythonPath = "python"
                    AppendToOutput("Found Python in PATH")
                Catch ex As Exception
                    ' Python not in PATH, try python3
                    Try
                        pythonProcess.StartInfo.FileName = "python3"
                        pythonProcess.Start()
                        pythonProcess.WaitForExit()
                        pythonPath = "python3"
                        AppendToOutput("Found Python3 in PATH")
                    Catch ex2 As Exception
                        ' Neither python nor python3 found
                        AppendToOutput("Python not found in PATH. Please install Python and esptool.")
                    End Try
                End Try

                If Not String.IsNullOrEmpty(pythonPath) Then
                    ' Check if esptool.py is installed
                    Dim esptoolProcess As New Process()
                    esptoolProcess.StartInfo.FileName = pythonPath
                    esptoolProcess.StartInfo.Arguments = "-m esptool version"
                    esptoolProcess.StartInfo.UseShellExecute = False
                    esptoolProcess.StartInfo.CreateNoWindow = True
                    esptoolProcess.StartInfo.RedirectStandardOutput = True

                    Try
                        esptoolProcess.Start()
                        Dim output = esptoolProcess.StandardOutput.ReadToEnd()
                        esptoolProcess.WaitForExit()

                        If esptoolProcess.ExitCode = 0 Then
                            esptoolPath = pythonPath & " -m esptool"
                            esptoolVersion = output.Trim()
                            AppendToOutput("Using esptool.py module: " & esptoolPath)
                            AppendToOutput("Esptool version: " & esptoolVersion)
                            Return
                        End If
                    Catch ex As Exception
                        ' esptool.py not installed as module
                        AppendToOutput("esptool.py module not found")
                    End Try
                End If
            Catch ex As Exception
                AppendToOutput("Error checking for Python and esptool: " & ex.Message)
            End Try

            ' Last attempt - try to find in Arduino package directories directly
            Try
                Dim packagesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "arduino15", "packages")

                If Directory.Exists(packagesDir) Then
                    ' Search for esptool in all packages
                    Dim esptoolFiles = Directory.GetFiles(packagesDir, "esptool*.exe", SearchOption.AllDirectories)
                    If esptoolFiles.Length > 0 Then
                        esptoolPath = esptoolFiles(0)
                        AppendToOutput("Found esptool in packages: " & esptoolPath)
                        Return
                    End If

                    ' Try Python scripts
                    Dim esptoolPyFiles = Directory.GetFiles(packagesDir, "esptool*.py", SearchOption.AllDirectories)
                    If esptoolPyFiles.Length > 0 Then
                        esptoolPath = esptoolPyFiles(0)
                        AppendToOutput("Found esptool.py in packages: " & esptoolPath)
                        Return
                    End If
                End If
            Catch ex As Exception
                AppendToOutput("Error searching package directories: " & ex.Message)
            End Try

            ' If we get here, no esptool found
            esptoolPath = ""
            AppendToOutput("WARNING: Could not find esptool. Upload may fail.")
            AppendToOutput("Please install esptool via 'pip install esptool' or specify path manually.")
        End Sub

        Private Sub CloseSerialPort()
            ' Close any open serial port
            If serialPortOpener IsNot Nothing Then
                Try
                    If serialPortOpener.IsOpen Then
                        serialPortOpener.Close()
                    End If
                    serialPortOpener.Dispose()
                    serialPortOpener = Nothing
                Catch ex As Exception
                    ' Ignore errors during close
                End Try
            End If
        End Sub

        Private Function IsPortAvailable(portName As String) As Boolean
            Try
                ' Close any previous serialPortOpener
                CloseSerialPort()

                ' Try to open the port to see if it's available
                serialPortOpener = New SerialPort(portName)
                serialPortOpener.Open()
                Return True
            Catch ex As Exception
                AppendToOutput("Port " & portName & " is busy: " & ex.Message)
                Return False
            End Try
        End Function

        Private Sub Upload_Click(sender As Object, e As EventArgs) Handles btnUpload.Click
            If cmbSerialPort.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a serial port for uploading.",
                           "No Serial Port Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If isUploading Then
                ' Cancel upload
                If bgWorker.IsBusy Then
                    bgWorker.CancelAsync()
                End If
                btnUpload.Text = "Upload"
                btnUpload.BackColor = Color.LightGreen
                isUploading = False
                AppendToOutput("Upload cancelled")
            Else
                ' Validate input files
                If Not ValidateRequiredFiles() Then
                    Return
                End If

                ' Check if esptool was found
                If String.IsNullOrEmpty(esptoolPath) Then
                    Dim result = MessageBox.Show(
                        "esptool was not found automatically. Upload may fail." & Environment.NewLine &
                        "Do you want to continue anyway?" & Environment.NewLine &
                        "(You may need to install esptool via 'pip install esptool')",
                        "esptool Not Found",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning)

                    If result = DialogResult.No Then
                        Return
                    End If
                End If

                ' Check if port is available
                Dim selectedPort = cmbSerialPort.SelectedItem.ToString()
                If Not IsPortAvailable(selectedPort) Then
                    Dim result = MessageBox.Show(
                        "The selected COM port (" & selectedPort & ") appears to be in use by another application." & Environment.NewLine &
                        "Would you like to try to use it anyway?" & Environment.NewLine &
                        "Note: Close any serial monitors or other programs using this port first.",
                        "Port In Use",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning)

                    If result = DialogResult.No Then
                        Return
                    End If
                Else
                    ' Close the port now that we've verified it works
                    CloseSerialPort()
                End If

                ' Start upload
                txtOutput.Clear()
                progressBar.Value = 0
                AppendToOutput("Starting binary upload...")

                btnUpload.Text = "Cancel"
                btnUpload.BackColor = Color.IndianRed
                isUploading = True

                ' Start upload in background
                bgWorker.RunWorkerAsync(cmbSerialPort.SelectedItem.ToString())
            End If
        End Sub

        Private Function ValidateRequiredFiles() As Boolean
            ' At minimum, we need the application binary file
            If String.IsNullOrEmpty(txtApplicationPath.Text) OrElse Not File.Exists(txtApplicationPath.Text) Then
                MessageBox.Show("Please select a valid application binary file.",
                           "Missing Required File", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            ' Validate addresses (must be in hex format)
            Try
                If Not String.IsNullOrEmpty(txtBootloaderPath.Text) Then
                    Convert.ToInt32(txtBootloaderAddr.Text, 16)
                End If
                If Not String.IsNullOrEmpty(txtPartitionPath.Text) Then
                    Convert.ToInt32(txtPartitionAddr.Text, 16)
                End If
                If Not String.IsNullOrEmpty(txtBootApp0Path.Text) Then
                    Convert.ToInt32(txtBootApp0Addr.Text, 16)
                End If
                Convert.ToInt32(txtApplicationAddr.Text, 16)
            Catch ex As Exception
                MessageBox.Show("One or more addresses are not in valid hexadecimal format (e.g., 0x1000).",
                           "Invalid Address Format", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End Try

            Return True
        End Function

        Private Sub Cancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
            If isUploading AndAlso bgWorker.IsBusy Then
                bgWorker.CancelAsync()
                isUploading = False
                btnUpload.Text = "Upload"
                btnUpload.BackColor = Color.LightGreen
            Else
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            End If
        End Sub

        Private Sub BgWorker_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles bgWorker.DoWork
            Dim worker As System.ComponentModel.BackgroundWorker = CType(sender, System.ComponentModel.BackgroundWorker)
            Dim port As String = CType(e.Argument, String)
            Dim uploadSuccess As Boolean = True

            ' Get the file paths and addresses from the UI thread
            Dim bootloaderPath As String = ""
            Dim bootloaderAddr As String = ""
            Dim partitionPath As String = ""
            Dim partitionAddr As String = ""
            Dim bootApp0Path As String = ""
            Dim bootApp0Addr As String = ""
            Dim applicationPath As String = ""
            Dim applicationAddr As String = ""

            Me.Invoke(Sub()
                          bootloaderPath = txtBootloaderPath.Text
                          bootloaderAddr = txtBootloaderAddr.Text
                          partitionPath = txtPartitionPath.Text
                          partitionAddr = txtPartitionAddr.Text
                          bootApp0Path = txtBootApp0Path.Text
                          bootApp0Addr = txtBootApp0Addr.Text
                          applicationPath = txtApplicationPath.Text
                          applicationAddr = txtApplicationAddr.Text
                      End Sub)

            ' Add file list for upload
            Dim fileList As String = ""

            If Not String.IsNullOrEmpty(bootloaderPath) AndAlso File.Exists(bootloaderPath) Then
                fileList += bootloaderAddr & " """ & bootloaderPath & """ "
                worker.ReportProgress(15, "Using bootloader: " & Path.GetFileName(bootloaderPath))
            End If

            If Not String.IsNullOrEmpty(partitionPath) AndAlso File.Exists(partitionPath) Then
                fileList += partitionAddr & " """ & partitionPath & """ "
                worker.ReportProgress(15, "Using partition table: " & Path.GetFileName(partitionPath))
            End If

            If Not String.IsNullOrEmpty(bootApp0Path) AndAlso File.Exists(bootApp0Path) Then
                fileList += bootApp0Addr & " """ & bootApp0Path & """ "
                worker.ReportProgress(15, "Using boot_app0: " & Path.GetFileName(bootApp0Path))
            End If

            If Not String.IsNullOrEmpty(applicationPath) AndAlso File.Exists(applicationPath) Then
                fileList += applicationAddr & " """ & applicationPath & """"
                worker.ReportProgress(15, "Using application: " & Path.GetFileName(applicationPath))
            End If

            worker.ReportProgress(10, "Preparing to flash binaries...")
            worker.ReportProgress(15, "Command: write_flash --chip esp32 --port " & port & " --baud 460800 --flash_mode dio --flash_freq 40m --flash_size detect " & fileList)

            ' Make sure any serial monitors or other applications release the port
            worker.ReportProgress(15, "Ensuring port " & port & " is available...")

            ' Close any applications that might be using the port
            ClosePortProcesses(port)

            ' Allow a moment for the port to be released
            Thread.Sleep(500)

            ' Execute the esptool command with the correct format
            Try
                Dim processInfo As New ProcessStartInfo()
                Dim cmdArgs As String = ""

                ' Check if this is the Arduino ESP32 esptool.exe
                If esptoolPath.EndsWith(".exe") Then
                    ' Standalone executable - for ESP32 we need to put chip parameter before write_flash
                    processInfo.FileName = esptoolPath
                    cmdArgs = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                    worker.ReportProgress(20, "Using Arduino ESP32 esptool.exe")
                ElseIf esptoolPath.Contains(" -m ") Then
                    ' Python module
                    Dim parts = esptoolPath.Split(New String() {" -m "}, StringSplitOptions.None)
                    processInfo.FileName = parts(0) ' python or python3
                    cmdArgs = "-m esptool --chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                    worker.ReportProgress(20, "Using Python esptool.py module")
                ElseIf esptoolPath.EndsWith(".py") Then
                    ' Python script - need to find Python
                    Try
                        processInfo.FileName = "python"
                        cmdArgs = """" & esptoolPath & """ --chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                        worker.ReportProgress(20, "Using Python script esptool.py")
                    Catch ex As Exception
                        ' Try with python3
                        processInfo.FileName = "python3"
                        worker.ReportProgress(20, "Trying with python3 instead of python")
                    End Try
                Else
                    ' Unknown format - try direct execution
                    processInfo.FileName = esptoolPath
                    cmdArgs = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                    worker.ReportProgress(20, "Using direct execution of esptool")
                End If

                processInfo.Arguments = cmdArgs
                processInfo.UseShellExecute = False
                processInfo.CreateNoWindow = True
                processInfo.RedirectStandardOutput = True
                processInfo.RedirectStandardError = True

                Dim process As New Process()
                process.StartInfo = processInfo
                process.EnableRaisingEvents = True

                ' Setup output handlers
                AddHandler process.OutputDataReceived, Sub(s, outputData)
                                                           If Not String.IsNullOrEmpty(outputData.Data) Then
                                                               worker.ReportProgress(0, outputData.Data)

                                                               ' Update progress based on output
                                                               If outputData.Data.Contains("Writing at") Then
                                                                   worker.ReportProgress(30, Nothing)
                                                               ElseIf outputData.Data.Contains("Written ") Then
                                                                   worker.ReportProgress(50, Nothing)
                                                               ElseIf outputData.Data.Contains("Verifying ") Then
                                                                   worker.ReportProgress(70, Nothing)
                                                               ElseIf outputData.Data.Contains("Hash of data verified") Then
                                                                   worker.ReportProgress(90, Nothing)
                                                               ElseIf outputData.Data.Contains("Hard resetting") Then
                                                                   worker.ReportProgress(95, Nothing)
                                                               End If
                                                           End If
                                                       End Sub

                AddHandler process.ErrorDataReceived, Sub(s, errorData)
                                                          If Not String.IsNullOrEmpty(errorData.Data) Then
                                                              worker.ReportProgress(0, "ERROR: " & errorData.Data)

                                                              ' Check for specific errors
                                                              If errorData.Data.Contains("Could not open") AndAlso
                                                                 errorData.Data.Contains("the port is busy or doesn't exist") Then
                                                                  worker.ReportProgress(0, "Port is busy. Try closing any serial monitors or other applications using the port.")
                                                                  worker.ReportProgress(0, "Note: Sometimes you need to restart the IDE to fully release the port.")
                                                              End If

                                                              uploadSuccess = False
                                                          End If
                                                      End Sub

                worker.ReportProgress(20, "Starting upload process...")
                worker.ReportProgress(20, "Command: " & processInfo.FileName & " " & processInfo.Arguments)

                process.Start()
                process.BeginOutputReadLine()
                process.BeginErrorReadLine()

                ' Wait for process to exit or cancellation
                While Not process.HasExited
                    If worker.CancellationPending Then
                        process.Kill()
                        e.Cancel = True
                        Return
                    End If
                    Thread.Sleep(100)
                End While

                ' Process completed, check exit code
                If process.ExitCode <> 0 Then
                    worker.ReportProgress(0, "Upload failed with exit code: " & process.ExitCode)
                    uploadSuccess = False

                    ' Try to find what's using the port
                    If process.ExitCode = 2 Then
                        worker.ReportProgress(0, "Checking what process is using the port...")
                        Dim portUserInfo = GetPortUserProcess(port)
                        If Not String.IsNullOrEmpty(portUserInfo) Then
                            worker.ReportProgress(0, "Port " & port & " is used by: " & portUserInfo)
                            worker.ReportProgress(0, "Please close this application before uploading.")
                        End If
                    End If

                    ' Try one more time with a different command format as fallback
                    If uploadSuccess = False Then
                        worker.ReportProgress(0, "Trying alternative command format...")

                        Dim altProcessInfo As New ProcessStartInfo()
                        altProcessInfo.FileName = processInfo.FileName

                        ' Swap the parameter order for write_flash
                        If processInfo.Arguments.Contains("write_flash --flash_mode") Then
                            ' Move chip/port parameters after write_flash
                            altProcessInfo.Arguments = "write_flash --chip esp32 --port " & port & " --baud 460800 --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                        Else
                            ' Move flash mode parameters before write_flash
                            altProcessInfo.Arguments = "--chip esp32 --port " & port & " --baud 460800 write_flash --flash_mode dio --flash_freq 40m --flash_size detect " & fileList
                        End If

                        worker.ReportProgress(20, "Alternative command: " & altProcessInfo.FileName & " " & altProcessInfo.Arguments)

                        altProcessInfo.UseShellExecute = False
                        altProcessInfo.CreateNoWindow = True
                        altProcessInfo.RedirectStandardOutput = True
                        altProcessInfo.RedirectStandardError = True

                        Dim altProcess As New Process()
                        altProcess.StartInfo = altProcessInfo
                        altProcess.EnableRaisingEvents = True

                        ' Setup output handlers
                        AddHandler altProcess.OutputDataReceived, Sub(s, outputData)
                                                                      If Not String.IsNullOrEmpty(outputData.Data) Then
                                                                          worker.ReportProgress(0, outputData.Data)

                                                                          ' Update progress based on output
                                                                          If outputData.Data.Contains("Writing at") Then
                                                                              worker.ReportProgress(30, Nothing)
                                                                          ElseIf outputData.Data.Contains("Written ") Then
                                                                              worker.ReportProgress(50, Nothing)
                                                                          ElseIf outputData.Data.Contains("Verifying ") Then
                                                                              worker.ReportProgress(70, Nothing)
                                                                          ElseIf outputData.Data.Contains("Hash of data verified") Then
                                                                              worker.ReportProgress(90, Nothing)
                                                                          ElseIf outputData.Data.Contains("Hard resetting") Then
                                                                              worker.ReportProgress(95, Nothing)
                                                                          End If
                                                                      End If
                                                                  End Sub

                        AddHandler altProcess.ErrorDataReceived, Sub(s, errorData)
                                                                     If Not String.IsNullOrEmpty(errorData.Data) Then
                                                                         worker.ReportProgress(0, "ERROR: " & errorData.Data)
                                                                     End If
                                                                 End Sub

                        worker.ReportProgress(20, "Starting alternative upload attempt...")

                        altProcess.Start()
                        altProcess.BeginOutputReadLine()
                        altProcess.BeginErrorReadLine()

                        ' Wait for process to exit or cancellation
                        While Not altProcess.HasExited
                            If worker.CancellationPending Then
                                altProcess.Kill()
                                e.Cancel = True
                                Return
                            End If
                            Thread.Sleep(100)
                        End While

                        ' Process completed, check exit code
                        If altProcess.ExitCode = 0 Then
                            worker.ReportProgress(100, "Alternative upload completed successfully!")
                            uploadSuccess = True
                        Else
                            worker.ReportProgress(0, "Alternative upload failed with exit code: " & altProcess.ExitCode)
                        End If
                    End If
                Else
                    worker.ReportProgress(100, "Upload completed successfully!")
                End If

            Catch ex As Exception
                worker.ReportProgress(0, "Error during upload: " & ex.Message)
                uploadSuccess = False
            End Try

            e.Result = uploadSuccess
        End Sub

        Private Function GetPortUserProcess(portName As String) As String
            Try
                ' Use Windows Management Instrumentation (WMI) to find processes using the port
                Dim startInfo As New ProcessStartInfo()
                startInfo.FileName = "powershell"
                startInfo.Arguments = "-Command ""Get-Process | Where-Object {$_.Modules.FileName -like '*serial*' -or $_.Modules.FileName -like '*com*'} | Select-Object ProcessName, Id | Format-Table -AutoSize"""
                startInfo.UseShellExecute = False
                startInfo.RedirectStandardOutput = True
                startInfo.CreateNoWindow = True

                Dim process As New Process()
                process.StartInfo = startInfo
                process.Start()

                Dim output As String = process.StandardOutput.ReadToEnd()
                process.WaitForExit()

                If Not String.IsNullOrEmpty(output) Then
                    Return output
                End If

                Return "Could not determine which process is using the port"
            Catch ex As Exception
                Return "Error checking port usage: " & ex.Message
            End Try
        End Function

        Private Sub ClosePortProcesses(portName As String)
            Try
                ' Close any known port-using processes
                Dim knownProcesses As String() = {"Serial Monitor", "putty", "terminal", "serialmon", "coolterm", "terraterm"}

                For Each procName In knownProcesses
                    Try
                        Dim procs = Process.GetProcessesByName(procName)
                        For Each proc In procs
                            Try
                                proc.CloseMainWindow()
                                ' Give it a moment to close
                                Thread.Sleep(100)
                                If Not proc.HasExited Then
                                    proc.Kill()
                                End If
                            Catch ex As Exception
                                ' Ignore errors trying to close processes
                            End Try
                        Next
                    Catch ex As Exception
                        ' Ignore errors if a process type isn't found
                    End Try
                Next
            Catch ex As Exception
                ' Ignore any errors in the cleanup process
            End Try
        End Sub

        Private Sub BgWorker_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles bgWorker.ProgressChanged
            ' Update progress bar for percentage changes
            If e.ProgressPercentage > 0 Then
                progressBar.Value = Math.Min(e.ProgressPercentage, 100)
            End If

            ' Update output with message if provided
            If e.UserState IsNot Nothing Then
                AppendToOutput(e.UserState.ToString())
            End If
        End Sub

        Private Sub BgWorker_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles bgWorker.RunWorkerCompleted
            ' Reset UI state
            btnUpload.Text = "Upload"
            btnUpload.BackColor = Color.LightGreen
            isUploading = False

            If e.Cancelled Then
                AppendToOutput("Upload cancelled by user")
                progressBar.Value = 0
            ElseIf e.Error IsNot Nothing Then
                AppendToOutput("Error during upload: " & e.Error.Message)
                MessageBox.Show("Error during upload: " & e.Error.Message, "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ElseIf CBool(e.Result) Then
                AppendToOutput("Binary upload completed successfully!")
                progressBar.Value = 100
            Else
                AppendToOutput("Binary upload failed. Check output for details.")

                ' Show more helpful error message
                Dim extraHelp = "Common issues:" & Environment.NewLine &
                                "1. Serial port is in use by another application" & Environment.NewLine &
                                "2. You need to reset your board manually before upload" & Environment.NewLine &
                                "3. A serial monitor is still open" & Environment.NewLine & Environment.NewLine &
                                "Try closing all other applications that might be using the serial port."

                MessageBox.Show("Binary upload failed. " & extraHelp, "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            ' Close any opened serial port
            CloseSerialPort()
        End Sub

        Private Sub AppendToOutput(text As String)
            If txtOutput.InvokeRequired Then
                txtOutput.Invoke(Sub() AppendToOutput(text))
            Else
                txtOutput.AppendText(text & Environment.NewLine)
                txtOutput.SelectionStart = txtOutput.Text.Length
                txtOutput.ScrollToCaret()
            End If
        End Sub

        Friend WithEvents mainLayout As TableLayoutPanel
        Friend WithEvents binaryFilesPanel As TableLayoutPanel
        Friend WithEvents serialPortPanel As TableLayoutPanel
        Friend WithEvents buttonsPanel As FlowLayoutPanel
    End Class
End Namespace