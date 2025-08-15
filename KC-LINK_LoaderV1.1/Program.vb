Imports System
Imports System.Windows.Forms

Namespace KC_LINK_LoaderV1
    Friend Class Program
        ''' <summary>
        ''' The main entry point for the application.
        ''' </summary>
        <STAThread()>
        Shared Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            ' Log application startup
            Try
                Dim logMessage As String = "2025-08-12 04:38:01 Application started by Chamil1983"
                Dim logPath As String = System.IO.Path.Combine(Application.StartupPath, "application_log.txt")
                System.IO.File.AppendAllText(logPath, logMessage & Environment.NewLine)
            Catch ex As Exception
                ' Ignore logging errors
            End Try

            ' Explicitly run the MainForm
            Application.Run(New MainForm())
        End Sub
    End Class
End Namespace