Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.ComponentModel

' Use a unique namespace for your project
Namespace KC_LINK_LoaderV1
    Public Class ColoredProgressBar
        Inherits Control

        ' Private fields
        Private m_value As Integer = 0
        Private m_maximum As Integer = 100
        Private m_minimum As Integer = 0
        Private m_barColor As Color = Color.RoyalBlue
        Private m_showText As Boolean = True

        Public Sub New()
            MyBase.New()
            Me.SetStyle(ControlStyles.UserPaint Or
                        ControlStyles.AllPaintingInWmPaint Or
                        ControlStyles.OptimizedDoubleBuffer Or
                        ControlStyles.ResizeRedraw Or
                        ControlStyles.SupportsTransparentBackColor, True)

            Me.BackColor = SystemColors.Control
            Me.ForeColor = Color.Black
            Me.Height = 20
        End Sub

        <Category("Appearance")>
        <DefaultValue(0)>
        Public Property Value As Integer
            Get
                Return m_value
            End Get
            Set(value As Integer)
                If value < m_minimum Then
                    value = m_minimum
                ElseIf value > m_maximum Then
                    value = m_maximum
                End If
                If m_value <> value Then
                    m_value = value
                    Invalidate(False)
                End If
            End Set
        End Property

        <Category("Appearance")>
        <DefaultValue(100)>
        Public Property Maximum As Integer
            Get
                Return m_maximum
            End Get
            Set(value As Integer)
                If value < m_minimum Then value = m_minimum
                If m_maximum <> value Then
                    m_maximum = value
                    If m_value > m_maximum Then m_value = m_maximum
                    Invalidate(False)
                End If
            End Set
        End Property

        <Category("Appearance")>
        <DefaultValue(0)>
        Public Property Minimum As Integer
            Get
                Return m_minimum
            End Get
            Set(value As Integer)
                If value > m_maximum Then value = m_maximum
                If m_minimum <> value Then
                    m_minimum = value
                    If m_value < m_minimum Then m_value = m_minimum
                    Invalidate(False)
                End If
            End Set
        End Property

        <Category("Appearance")>
        Public Property BarColor As Color
            Get
                Return m_barColor
            End Get
            Set(value As Color)
                If m_barColor <> value Then
                    m_barColor = value
                    Invalidate(False)
                End If
            End Set
        End Property

        <Category("Appearance")>
        <DefaultValue(True)>
        Public Property ShowPercentText As Boolean
            Get
                Return m_showText
            End Get
            Set(value As Boolean)
                If m_showText <> value Then
                    m_showText = value
                    Invalidate(False)
                End If
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim g As Graphics = e.Graphics
            Dim rect As Rectangle = ClientRectangle

            g.SmoothingMode = SmoothingMode.AntiAlias
            g.InterpolationMode = InterpolationMode.HighQualityBicubic

            ' Draw background
            Using bgBrush As New SolidBrush(Me.BackColor)
                g.FillRectangle(bgBrush, rect)
            End Using

            ' Calculate progress width
            Dim range As Integer = m_maximum - m_minimum
            If range <= 0 Then range = 1

            Dim percentage As Double = CDbl(m_value - m_minimum) / range
            Dim progressWidth As Integer = CInt(Math.Floor(rect.Width * percentage))

            If progressWidth > 0 Then
                Dim progressRect As New Rectangle(rect.X, rect.Y, progressWidth, rect.Height)

                ' Draw gradient fill
                Using barBrush As New LinearGradientBrush(
                    progressRect,
                    Color.FromArgb(m_barColor.R, m_barColor.G, m_barColor.B, 230),
                    m_barColor,
                    LinearGradientMode.Vertical)
                    g.FillRectangle(barBrush, progressRect)
                End Using

                Using lightPen As New Pen(Color.FromArgb(60, 255, 255, 255), 1)
                    g.DrawLine(lightPen, progressRect.X, progressRect.Y, progressRect.Right, progressRect.Y)
                    g.DrawLine(lightPen, progressRect.X, progressRect.Y, progressRect.X, progressRect.Bottom)
                End Using

                Using shadowPen As New Pen(Color.FromArgb(40, 0, 0, 0), 1)
                    g.DrawLine(shadowPen, progressRect.X, progressRect.Bottom - 1, progressRect.Right - 1, progressRect.Bottom - 1)
                    g.DrawLine(shadowPen, progressRect.Right - 1, progressRect.Y, progressRect.Right - 1, progressRect.Bottom - 1)
                End Using
            End If

            ' Draw border
            Using borderPen As New Pen(Color.Gray, 1)
                g.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1)
            End Using

            ' Draw text if enabled
            If m_showText Then
                Dim percentValue As Integer = CInt(percentage * 100)
                Dim text As String = $"{percentValue}%"
                Dim textSize As SizeF = g.MeasureString(text, Me.Font)

                Dim textRect As New RectangleF(
                    (rect.Width - textSize.Width) / 2,
                    (rect.Height - textSize.Height) / 2,
                    textSize.Width,
                    textSize.Height)

                Using shadowBrush As New SolidBrush(Color.FromArgb(80, 0, 0, 0))
                    g.DrawString(text, Me.Font, shadowBrush, textRect.X + 1, textRect.Y + 1)
                End Using

                Using textBrush As New SolidBrush(Me.ForeColor)
                    g.DrawString(text, Me.Font, textBrush, textRect.X, textRect.Y)
                End Using
            End If
        End Sub
    End Class
End Namespace