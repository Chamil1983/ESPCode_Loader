Namespace My
    Partial Friend NotInheritable Class MySettings
        Public Property ArduinoCliPath As String = "C:\arduino-cli\arduino-cli.exe"
        Public Property DefaultSketchDir As String = "C:\Temp\ArduinoSketches"
        Public Property HardwarePath As String = ""
        Public Property BoardsFilePath As String = ""
        Public Property EnableLogging As Boolean = True
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
        End Sub
    End Class
End Namespace