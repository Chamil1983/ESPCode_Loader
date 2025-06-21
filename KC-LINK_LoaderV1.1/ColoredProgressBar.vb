Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class ColoredProgressBar
    Inherits ProgressBar

    Public Property BarColor As Color = Color.RoyalBlue

    Public Sub New()
        MyBase.New()
        ' Enable user paint
        Me.SetStyle(ControlStyles.UserPaint, True)
        Me.Style = ProgressBarStyle.Continuous
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        ' Draw background
        Dim rect As New Rectangle(0, 0, Me.Width, Me.Height)
        e.Graphics.Clear(Me.BackColor)

        ' Draw bar
        Dim percent As Double = (Me.Value - Me.Minimum) / (Me.Maximum - Me.Minimum)
        Dim fillRect As New Rectangle(0, 0, CInt(rect.Width * percent), rect.Height)
        Using b As New SolidBrush(Me.BarColor)
            e.Graphics.FillRectangle(b, fillRect)
        End Using

        ' Optional: Draw border
        ControlPaint.DrawBorder(e.Graphics, rect, Color.Gray, ButtonBorderStyle.Solid)

        ' Optional: Draw percent text
        Dim txt As String = $"{Me.Value}%"
        Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
        e.Graphics.DrawString(txt, Me.Font, Brushes.Black, rect, sf)
    End Sub
End Class