Imports System
Imports System.IO
Imports System.IO.Ports
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Text
Imports System.Threading

Public Class SerialMonitor
    ' Private fields
    Private WithEvents serialPort As SerialPort
    Private WithEvents monitorForm As Form
    Private WithEvents outputTextBox As RichTextBox
    Private WithEvents inputTextBox As TextBox
    Private WithEvents sendButton As Button
    Private WithEvents clearButton As Button
    Private WithEvents autoScrollCheckBox As CheckBox
    Private WithEvents baudRateComboBox As ComboBox
    Private WithEvents lineEndingComboBox As ComboBox
    Private WithEvents displayModeButton As Button
    Private WithEvents saveToFileButton As Button
    Private WithEvents showTimestampsCheckBox As CheckBox

    ' Changed name to avoid ambiguity with property
    Private portIsOpen As Boolean = False
    Private isTextMode As Boolean = True
    Private showTimestamps As Boolean = False
    Private portName As String = String.Empty
    Private outputBuffer As New StringBuilder()
    Private receivedBytes As Integer = 0
    Private sentBytes As Integer = 0
    Private lastActivity As DateTime = DateTime.Now

    ' Properties
    Public ReadOnly Property IsOpen() As Boolean
        Get
            ' Reference private field with different name
            Return portIsOpen
        End Get
    End Property

    ' Methods
    Public Sub OpenMonitor(portName As String)
        If String.IsNullOrEmpty(portName) Then
            MessageBox.Show("Please specify a valid COM port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        If portIsOpen Then
            ' Close the current connection first
            CloseMonitor()
        End If

        Me.portName = portName

        ' Try to open the serial port
        Try
            serialPort = New SerialPort(portName)
            serialPort.BaudRate = 115200  ' Default baud rate
            serialPort.DataBits = 8
            serialPort.Parity = Parity.None
            serialPort.StopBits = StopBits.One
            serialPort.Handshake = Handshake.None
            serialPort.ReadTimeout = 500
            serialPort.WriteTimeout = 500
            serialPort.Open()

            ' Create and show the monitor form
            ShowMonitorForm()

            portIsOpen = True
            outputTextBox.AppendText($"Connected to {portName} at {serialPort.BaudRate} baud{Environment.NewLine}")

            ' Start reading from the port
            BeginAsyncRead()

            lastActivity = DateTime.Now

        Catch ex As Exception
            MessageBox.Show($"Error opening port {portName}: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Sub CloseMonitor()
        If Not portIsOpen Then
            Return
        End If

        Try
            ' Stop reading
            If serialPort IsNot Nothing AndAlso serialPort.IsOpen Then
                serialPort.Close()
            End If

            portIsOpen = False

            ' Close the monitor form if it's open
            If monitorForm IsNot Nothing AndAlso Not monitorForm.IsDisposed Then
                monitorForm.Close()
            End If

        Catch ex As Exception
            MessageBox.Show($"Error closing port: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowMonitorForm()
        monitorForm = New Form()
        monitorForm.Text = $"Serial Monitor - {portName}"
        monitorForm.Size = New Size(650, 500)
        monitorForm.StartPosition = FormStartPosition.CenterScreen

        ' Create layout
        Dim mainLayout As New TableLayoutPanel()
        mainLayout.Dock = DockStyle.Fill
        mainLayout.RowCount = 3
        mainLayout.ColumnCount = 1
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))
        mainLayout.Padding = New Padding(10)

        ' Output text box
        outputTextBox = New RichTextBox()
        outputTextBox.Dock = DockStyle.Fill
        outputTextBox.ReadOnly = True
        outputTextBox.BackColor = Color.Black
        outputTextBox.ForeColor = Color.LightGreen
        outputTextBox.Font = New Font("Consolas", 9.75F)
        outputTextBox.ScrollBars = RichTextBoxScrollBars.Vertical
        outputTextBox.Text = ""

        ' Input panel
        Dim inputPanel As New TableLayoutPanel()
        inputPanel.Dock = DockStyle.Fill
        inputPanel.ColumnCount = 3
        inputPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        inputPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
        inputPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))

        inputTextBox = New TextBox()
        inputTextBox.Dock = DockStyle.Fill

        sendButton = New Button()
        sendButton.Text = "Send"
        sendButton.Dock = DockStyle.Fill

        clearButton = New Button()
        clearButton.Text = "Clear"
        clearButton.Dock = DockStyle.Fill

        inputPanel.Controls.Add(inputTextBox, 0, 0)
        inputPanel.Controls.Add(sendButton, 1, 0)
        inputPanel.Controls.Add(clearButton, 2, 0)

        ' Options panel
        Dim optionsPanel As New TableLayoutPanel()
        optionsPanel.Dock = DockStyle.Fill
        optionsPanel.ColumnCount = 7
        optionsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
        optionsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        optionsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
        optionsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
        optionsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        optionsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 110))
        optionsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))

        Dim lblBaud As New Label()
        lblBaud.Text = "Baud Rate:"
        lblBaud.TextAlign = ContentAlignment.MiddleRight
        lblBaud.Dock = DockStyle.Fill

        baudRateComboBox = New ComboBox()
        baudRateComboBox.Dock = DockStyle.Fill
        baudRateComboBox.DropDownStyle = ComboBoxStyle.DropDownList
        baudRateComboBox.Items.AddRange({"9600", "19200", "38400", "57600", "74880", "115200", "230400", "460800", "921600"})
        baudRateComboBox.SelectedItem = "115200"

        Dim lblLineEnding As New Label()
        lblLineEnding.Text = "Line Ending:"
        lblLineEnding.TextAlign = ContentAlignment.MiddleRight
        lblLineEnding.Dock = DockStyle.Fill

        lineEndingComboBox = New ComboBox()
        lineEndingComboBox.Dock = DockStyle.Fill
        lineEndingComboBox.DropDownStyle = ComboBoxStyle.DropDownList
        lineEndingComboBox.Items.AddRange({"No Line Ending", "Newline", "Carriage Return", "Both NL & CR"})
        lineEndingComboBox.SelectedIndex = 1

        autoScrollCheckBox = New CheckBox()
        autoScrollCheckBox.Text = "Auto Scroll"
        autoScrollCheckBox.Checked = True
        autoScrollCheckBox.Dock = DockStyle.Fill

        showTimestampsCheckBox = New CheckBox()
        showTimestampsCheckBox.Text = "Show Timestamps"
        showTimestampsCheckBox.Checked = False
        showTimestampsCheckBox.Dock = DockStyle.Fill

        displayModeButton = New Button()
        displayModeButton.Text = "HEX Mode"
        displayModeButton.Dock = DockStyle.Fill

        saveToFileButton = New Button()
        saveToFileButton.Text = "Save to File"
        saveToFileButton.Dock = DockStyle.Fill

        optionsPanel.Controls.Add(lblBaud, 0, 0)
        optionsPanel.Controls.Add(baudRateComboBox, 1, 0)
        optionsPanel.Controls.Add(lblLineEnding, 2, 0)
        optionsPanel.Controls.Add(lineEndingComboBox, 3, 0)
        optionsPanel.Controls.Add(autoScrollCheckBox, 4, 0)
        optionsPanel.Controls.Add(displayModeButton, 5, 0)
        optionsPanel.Controls.Add(saveToFileButton, 6, 0)

        ' Add controls to main layout
        mainLayout.Controls.Add(outputTextBox, 0, 0)
        mainLayout.Controls.Add(inputPanel, 0, 1)
        mainLayout.Controls.Add(optionsPanel, 0, 2)

        ' Add main layout to form
        monitorForm.Controls.Add(mainLayout)

        ' Wire up events
        AddHandler sendButton.Click, AddressOf SendButton_Click
        AddHandler clearButton.Click, AddressOf ClearButton_Click
        AddHandler displayModeButton.Click, AddressOf DisplayModeButton_Click
        AddHandler saveToFileButton.Click, AddressOf SaveToFileButton_Click
        AddHandler baudRateComboBox.SelectedIndexChanged, AddressOf BaudRateComboBox_SelectedIndexChanged
        AddHandler monitorForm.FormClosing, AddressOf MonitorForm_FormClosing
        AddHandler inputTextBox.KeyPress, AddressOf InputTextBox_KeyPress
        AddHandler showTimestampsCheckBox.CheckedChanged, AddressOf ShowTimestampsCheckBox_CheckedChanged

        ' Show the form
        monitorForm.Show()
    End Sub

    Private Sub BeginAsyncRead()
        If serialPort Is Nothing OrElse Not serialPort.IsOpen Then
            Return
        End If

        Try
            Dim buffer(4096) As Byte
            serialPort.BaseStream.BeginRead(buffer, 0, buffer.Length,
                Sub(ar)
                    Try
                        If serialPort Is Nothing OrElse Not serialPort.IsOpen Then
                            Return
                        End If

                        Dim bytesRead = serialPort.BaseStream.EndRead(ar)
                        If bytesRead > 0 Then
                            receivedBytes += bytesRead
                            DisplayData(buffer, bytesRead)
                            lastActivity = DateTime.Now
                        End If

                        ' Continue reading
                        BeginAsyncRead()
                    Catch ex As Exception
                        ' Handle disconnect or other error
                        If monitorForm IsNot Nothing AndAlso Not monitorForm.IsDisposed Then
                            monitorForm.Invoke(Sub()
                                                   outputTextBox.AppendText($"Error reading from port: {ex.Message}{Environment.NewLine}")
                                               End Sub)
                        End If
                    End Try
                End Sub, Nothing)
        Catch ex As Exception
            If monitorForm IsNot Nothing AndAlso Not monitorForm.IsDisposed Then
                monitorForm.Invoke(Sub()
                                       outputTextBox.AppendText($"Error setting up async read: {ex.Message}{Environment.NewLine}")
                                   End Sub)
            End If
        End Try
    End Sub

    Private Sub DisplayData(data() As Byte, length As Integer)
        ' Convert the received data based on the current display mode
        If isTextMode Then
            ' Text mode - display as ASCII
            Dim text = Encoding.ASCII.GetString(data, 0, length)

            If showTimestamps Then
                ' Split the text by newlines to add timestamps to each line
                Dim lines = text.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
                For Each line In lines
                    If Not String.IsNullOrEmpty(line) Then
                        outputBuffer.Append($"[{DateTime.Now:HH:mm:ss.fff}] {line}{Environment.NewLine}")
                    End If
                Next
            Else
                outputBuffer.Append(text)
            End If
        Else
            ' HEX mode - display as hexadecimal values
            Dim hexText = New StringBuilder(length * 3)

            If showTimestamps Then
                hexText.Append($"[{DateTime.Now:HH:mm:ss.fff}] ")
            End If

            For i As Integer = 0 To length - 1
                hexText.Append(data(i).ToString("X2") & " ")

                ' Add newline every 16 bytes for readability
                If (i + 1) Mod 16 = 0 AndAlso i < length - 1 Then
                    hexText.Append(Environment.NewLine)

                    If showTimestamps Then
                        hexText.Append($"[{DateTime.Now:HH:mm:ss.fff}] ")
                    End If
                End If
            Next

            hexText.Append(Environment.NewLine)
            outputBuffer.Append(hexText.ToString())
        End If

        ' Update the output textbox on the UI thread
        If monitorForm IsNot Nothing AndAlso Not monitorForm.IsDisposed Then
            monitorForm.Invoke(Sub()
                                   outputTextBox.AppendText(outputBuffer.ToString())
                                   outputBuffer.Clear()

                                   ' Auto-scroll if enabled
                                   If autoScrollCheckBox.Checked Then
                                       outputTextBox.SelectionStart = outputTextBox.Text.Length
                                       outputTextBox.ScrollToCaret()
                                   End If

                                   ' Update the form title with statistics
                                   monitorForm.Text = $"Serial Monitor - {portName} (Rx: {receivedBytes}, Tx: {sentBytes} bytes)"
                               End Sub)
        End If
    End Sub

    Private Sub SendButton_Click(sender As Object, e As EventArgs)
        SendText()
    End Sub

    Private Sub ClearButton_Click(sender As Object, e As EventArgs)
        outputTextBox.Clear()
    End Sub

    Private Sub DisplayModeButton_Click(sender As Object, e As EventArgs)
        isTextMode = Not isTextMode
        displayModeButton.Text = If(isTextMode, "HEX Mode", "Text Mode")

        ' Clear the output when switching modes
        outputTextBox.Clear()
    End Sub

    Private Sub SaveToFileButton_Click(sender As Object, e As EventArgs)
        Using saveDialog As New SaveFileDialog()
            saveDialog.Filter = "Text Files|*.txt|Log Files|*.log|All Files|*.*"
            saveDialog.Title = "Save Serial Output"
            saveDialog.FileName = $"SerialLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"

            If saveDialog.ShowDialog() = DialogResult.OK Then
                Try
                    File.WriteAllText(saveDialog.FileName, outputTextBox.Text)
                    MessageBox.Show($"Output saved to {saveDialog.FileName}", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show($"Error saving file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Sub BaudRateComboBox_SelectedIndexChanged(sender As Object, e As EventArgs)
        If serialPort Is Nothing OrElse Not serialPort.IsOpen Then
            Return
        End If

        Try
            Dim baudRate = Integer.Parse(baudRateComboBox.SelectedItem.ToString())
            serialPort.BaudRate = baudRate
            outputTextBox.AppendText($"Baud rate changed to {baudRate}{Environment.NewLine}")
        Catch ex As Exception
            MessageBox.Show($"Error changing baud rate: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub InputTextBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        ' Send text when Enter key is pressed
        If e.KeyChar = Convert.ToChar(Keys.Enter) Then
            SendText()
            e.Handled = True
        End If
    End Sub

    Private Sub ShowTimestampsCheckBox_CheckedChanged(sender As Object, e As EventArgs)
        showTimestamps = showTimestampsCheckBox.Checked
    End Sub

    Private Sub MonitorForm_FormClosing(sender As Object, e As FormClosingEventArgs)
        CloseMonitor()
    End Sub

    Private Sub SendText()
        If String.IsNullOrEmpty(inputTextBox.Text) Then
            Return
        End If

        If serialPort Is Nothing OrElse Not serialPort.IsOpen Then
            MessageBox.Show("Serial port is not open.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        Try
            Dim textToSend = inputTextBox.Text

            ' Add line endings based on selection
            Select Case lineEndingComboBox.SelectedIndex
                Case 0  ' No Line Ending
                    ' Do nothing
                Case 1  ' Newline
                    textToSend += Convert.ToChar(10)
                Case 2  ' Carriage Return
                    textToSend += Convert.ToChar(13)
                Case 3  ' Both NL & CR
                    textToSend += Convert.ToChar(13) & Convert.ToChar(10)
            End Select

            ' Send the text
            Dim dataToSend = Encoding.ASCII.GetBytes(textToSend)
            serialPort.Write(dataToSend, 0, dataToSend.Length)
            sentBytes += dataToSend.Length

            ' Echo to output if in text mode
            If isTextMode Then
                If showTimestamps Then
                    outputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] TX: {textToSend}{Environment.NewLine}")
                Else
                    outputTextBox.AppendText($"TX: {textToSend}{Environment.NewLine}")
                End If
            Else
                ' Show as hex in hex mode
                Dim hexText = New StringBuilder(dataToSend.Length * 3 + 4)

                If showTimestamps Then
                    hexText.Append($"[{DateTime.Now:HH:mm:ss.fff}] ")
                End If

                hexText.Append("TX: ")

                For Each b As Byte In dataToSend
                    hexText.Append(b.ToString("X2") & " ")
                Next

                hexText.Append(Environment.NewLine)
                outputTextBox.AppendText(hexText.ToString())
            End If

            ' Auto-scroll if enabled
            If autoScrollCheckBox.Checked Then
                outputTextBox.SelectionStart = outputTextBox.Text.Length
                outputTextBox.ScrollToCaret()
            End If

            ' Update the form title with statistics
            monitorForm.Text = $"Serial Monitor - {portName} (Rx: {receivedBytes}, Tx: {sentBytes} bytes)"

            ' Clear input textbox
            inputTextBox.Clear()
            inputTextBox.Focus()

        Catch ex As Exception
            MessageBox.Show($"Error sending data: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class