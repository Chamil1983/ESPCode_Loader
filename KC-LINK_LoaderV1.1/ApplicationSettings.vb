Namespace My
    Partial Friend NotInheritable Class MySettings
        Public Property ArduinoCliPath As String = ""
        Public Property DefaultSketchDir As String = ""
        Public Property HardwarePath As String = ""
        Public Property BoardsFilePath As String = ""  ' New property for boards.txt path
        Public Property EnableLogging As Boolean = False
        Public Property VerboseOutput As Boolean = True
        Public Property CompileTimeout As Integer = 300
        Public Property RecentProjects As Collections.Specialized.StringCollection
        Public Property DefaultBoard As String = "KC-Link PRO A8 (Default)"
        Public Property DefaultPartition As String = "default"
        Public Property LastUsedBoard As String = "KC-Link PRO A8 (Default)"
        Public Property LastUsedPartition As String = "default"

        ' Initialize collections when settings are loaded
        Private Sub MySettings_SettingsLoaded(sender As Object, e As System.Configuration.SettingsLoadedEventArgs) Handles Me.SettingsLoaded
            If RecentProjects Is Nothing Then
                RecentProjects = New Collections.Specialized.StringCollection()
            End If

            ' Add current user and time information to the log
            Debug.WriteLine($"[2025-06-20 02:52:18] Settings loaded by Chamil1983")
        End Sub
    End Class
End Namespace