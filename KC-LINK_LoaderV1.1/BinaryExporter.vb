Imports System
Imports System.IO
Imports System.IO.Compression
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Text.RegularExpressions

Namespace KC_LINK_LoaderV1._1
    ''' <summary>
    ''' Class to handle exporting of binary files and creating ZIP packages
    ''' </summary>
    Public Class BinaryExporter
        ' Default ESP32 memory addresses - verified against ESP32 documentation and Arduino ESP32 core
        Public Shared ReadOnly DefaultBootloaderAddress As String = "0x1000"    ' Bootloader starts at 0x1000
        Public Shared ReadOnly DefaultPartitionAddress As String = "0x8000"     ' Partition table at 0x8000
        Public Shared ReadOnly DefaultBootApp0Address As String = "0xe000"      ' boot_app0 at 0xe000
        Public Shared ReadOnly DefaultApplicationAddress As String = "0x10000"  ' Application at 0x10000

        ' Main binary export method
        Public Shared Function ExportBinaries(buildPath As String, projectName As String, exportPath As String, createZip As Boolean) As Boolean
            Try
                Debug.WriteLine("[2025-08-11 23:24:24] Exporting binaries from " & buildPath)
                If String.IsNullOrEmpty(buildPath) OrElse Not Directory.Exists(buildPath) Then
                    Throw New DirectoryNotFoundException("Build directory not found: " & buildPath)
                End If

                ' Find binary files
                Dim applicationBin As String = Path.Combine(buildPath, projectName & ".ino.bin")
                Dim bootloaderBin As String = Path.Combine(buildPath, projectName & ".ino.bootloader.bin")
                Dim partitionsBin As String = Path.Combine(buildPath, projectName & ".ino.partitions.bin")
                Dim bootAppBin As String = ""
                Dim mergedBin As String = Path.Combine(buildPath, projectName & ".ino.merged.bin")

                ' Check for boot_app0.bin in the hardware directory
                Dim bootApp0Path = FindBootApp0Bin(buildPath)
                If Not String.IsNullOrEmpty(bootApp0Path) Then
                    bootAppBin = bootApp0Path
                End If

                ' Verify the main application binary exists
                If Not File.Exists(applicationBin) Then
                    Debug.WriteLine("[2025-08-11 23:24:24] Application binary not found: " & applicationBin)

                    ' Try to find any .bin files
                    Dim binFiles = Directory.GetFiles(buildPath, "*.bin")
                    If binFiles.Length > 0 Then
                        For Each binFile In binFiles
                            Debug.WriteLine("[2025-08-11 23:24:24] Found binary file: " & binFile)
                        Next

                        ' If we found any bin files, use the first one as the application binary
                        If Not File.Exists(applicationBin) AndAlso binFiles.Length > 0 Then
                            Debug.WriteLine("[2025-08-11 23:24:24] Using first found binary as application: " & binFiles(0))
                            applicationBin = binFiles(0)
                        End If
                    Else
                        Throw New FileNotFoundException("No binary files found in build directory: " & buildPath)
                    End If
                End If

                ' Create export directory if it doesn't exist
                If Not Directory.Exists(exportPath) Then
                    Directory.CreateDirectory(exportPath)
                    Debug.WriteLine("[2025-08-11 23:24:24] Created export directory: " & exportPath)
                End If

                ' Copy binary files to export directory
                Dim exportedFiles As New List(Of String)

                ' Copy application binary
                Dim destAppBin = Path.Combine(exportPath, projectName & ".bin")
                File.Copy(applicationBin, destAppBin, True)
                exportedFiles.Add(destAppBin)
                Debug.WriteLine("[2025-08-11 23:24:24] Copied application binary to: " & destAppBin)

                ' Copy bootloader if it exists
                If File.Exists(bootloaderBin) Then
                    Dim destBootBin = Path.Combine(exportPath, "bootloader.bin")
                    File.Copy(bootloaderBin, destBootBin, True)
                    exportedFiles.Add(destBootBin)
                    Debug.WriteLine("[2025-08-11 23:24:24] Copied bootloader binary to: " & destBootBin)
                End If

                ' Copy partition table if it exists
                If File.Exists(partitionsBin) Then
                    Dim destPartBin = Path.Combine(exportPath, "partitions.bin")
                    File.Copy(partitionsBin, destPartBin, True)
                    exportedFiles.Add(destPartBin)
                    Debug.WriteLine("[2025-08-11 23:24:24] Copied partitions binary to: " & destPartBin)
                End If

                ' Copy boot_app0 if it exists
                If File.Exists(bootAppBin) Then
                    Dim destBootAppBin = Path.Combine(exportPath, "boot_app0.bin")
                    File.Copy(bootAppBin, destBootAppBin, True)
                    exportedFiles.Add(destBootAppBin)
                    Debug.WriteLine("[2025-08-11 23:24:24] Copied boot_app0 binary to: " & destBootAppBin)
                End If

                ' Copy merged binary if it exists
                If File.Exists(mergedBin) Then
                    Dim destMergedBin = Path.Combine(exportPath, projectName & "_merged.bin")
                    File.Copy(mergedBin, destMergedBin, True)
                    exportedFiles.Add(destMergedBin)
                    Debug.WriteLine("[2025-08-11 23:24:24] Copied merged binary to: " & destMergedBin)
                End If

                ' Create a manifest file with flash addresses
                Dim manifestPath = Path.Combine(exportPath, "flash_addresses.txt")
                Using writer As New StreamWriter(manifestPath)
                    writer.WriteLine("# Flash addresses for " & projectName)
                    writer.WriteLine("# Generated on " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    writer.WriteLine("")

                    If File.Exists(Path.Combine(exportPath, "bootloader.bin")) Then
                        writer.WriteLine("bootloader.bin: " & DefaultBootloaderAddress)
                    End If

                    If File.Exists(Path.Combine(exportPath, "partitions.bin")) Then
                        writer.WriteLine("partitions.bin: " & DefaultPartitionAddress)
                    End If

                    If File.Exists(Path.Combine(exportPath, "boot_app0.bin")) Then
                        writer.WriteLine("boot_app0.bin: " & DefaultBootApp0Address)
                    End If

                    writer.WriteLine(projectName & ".bin: " & DefaultApplicationAddress)

                    If File.Exists(Path.Combine(exportPath, projectName & "_merged.bin")) Then
                        writer.WriteLine(projectName & "_merged.bin: 0x0")
                    End If
                End Using

                exportedFiles.Add(manifestPath)
                Debug.WriteLine("[2025-08-11 23:24:24] Created flash address manifest: " & manifestPath)

                ' Create ZIP archive if requested
                If createZip Then
                    Dim zipPath = Path.Combine(exportPath, projectName & "_firmware.zip")

                    ' Delete existing ZIP if it exists
                    If File.Exists(zipPath) Then
                        File.Delete(zipPath)
                    End If

                    ' Create the ZIP file
                    Using zipArchive As ZipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create)
                        For Each filePath In exportedFiles
                            zipArchive.CreateEntryFromFile(filePath, Path.GetFileName(filePath))
                        Next
                    End Using

                    Debug.WriteLine("[2025-08-11 23:24:24] Created firmware ZIP package: " & zipPath)

                    ' Display success message
                    MessageBox.Show(
                        "Binary files and ZIP package have been successfully created in:" & Environment.NewLine & exportPath,
                        "Export Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

                    Return True
                Else
                    ' Display success message without ZIP
                    MessageBox.Show(
                        "Binary files have been successfully exported to:" & Environment.NewLine & exportPath,
                        "Export Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

                    Return True
                End If
            Catch ex As Exception
                Debug.WriteLine("[2025-08-11 23:24:24] Error exporting binaries: " & ex.Message)
                MessageBox.Show("Error exporting binary files: " & ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End Try
        End Function

        ' Helper method to find boot_app0.bin file
        Private Shared Function FindBootApp0Bin(buildPath As String) As String
            ' Try to find in the build directory first
            Dim bootAppBin = Path.Combine(buildPath, "boot_app0.bin")
            If File.Exists(bootAppBin) Then
                Return bootAppBin
            End If

            ' Try to find in the Arduino esp32 hardware directory
            Try
                ' Parse output to find the location of the ESP32 hardware directory
                Dim arduinoDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                Dim esp32HardwarePath = Path.Combine(arduinoDir, "arduino15", "packages", "esp32", "hardware", "esp32")

                If Directory.Exists(esp32HardwarePath) Then
                    ' Find the latest version directory
                    Dim versionDirs = Directory.GetDirectories(esp32HardwarePath)
                    If versionDirs.Length > 0 Then
                        Array.Sort(versionDirs)
                        Dim latestVersion = versionDirs(versionDirs.Length - 1)

                        ' Check for boot_app0.bin in standard locations
                        Dim bootAppLocations = New String() {
                            Path.Combine(latestVersion, "tools", "partitions", "boot_app0.bin"),
                            Path.Combine(latestVersion, "tools", "sdk", "bin", "boot_app0.bin")
                        }

                        For Each location In bootAppLocations
                            If File.Exists(location) Then
                                Return location
                            End If
                        Next
                    End If
                End If
            Catch ex As Exception
                Debug.WriteLine("[2025-08-11 23:24:24] Error finding boot_app0.bin: " & ex.Message)
            End Try

            Return String.Empty
        End Function

        ' Extract build folder path from compilation output
        Public Shared Function ExtractBuildPathFromOutput(compilationOutput As String) As String
            Try
                Debug.WriteLine("[2025-08-11 23:24:24] Attempting to extract build path from compilation output")

                ' Various patterns to match build path in different formats
                Dim patterns As String() = {
                    "Using build (?:folder|directory): ([^\r\n]+)",
                    "--build-path\s+(?:""|')?(.*?)(?:""|')?\s",
                    "Sketch uses \d+ bytes.*?[\r\n]",
                    "-o\s+(?:""|')?([^""'\r\n]+?[\\\/][^\\\/\r\n]+?\.ino\.[^\\\/\r\n]+)(?:""|')?"
                }

                For Each pattern In patterns
                    Dim matches = Regex.Matches(compilationOutput, pattern, RegexOptions.IgnoreCase)

                    For Each match As Match In matches
                        If match.Groups.Count > 1 Then
                            Dim filepath = match.Groups(1).Value.Trim()
                            Debug.WriteLine("[2025-08-11 23:24:24] Found potential build path: " & filepath)

                            ' If the path contains a .bin file, extract the directory
                            If filepath.EndsWith(".bin") OrElse filepath.EndsWith(".elf") Then
                                Dim dirpath = Path.GetDirectoryName(filepath)
                                Debug.WriteLine("[2025-08-11 23:24:24] Extracted directory: " & dirpath)

                                If Directory.Exists(dirpath) Then
                                    Debug.WriteLine("[2025-08-11 23:24:24] Verified build path exists: " & dirpath)
                                    Return dirpath
                                End If
                            ElseIf Directory.Exists(filepath) Then
                                Debug.WriteLine("[2025-08-11 23:24:24] Verified build path exists: " & filepath)
                                Return filepath
                            End If
                        End If
                    Next
                Next

                ' More specific search for Arduino build paths in terminal output
                Dim arduinoBuildPathPattern = "C:\\\\Users\\\\[^\\\\]+\\\\AppData\\\\Local\\\\arduino\\\\sketches\\\\[^\\\\]+\\/[\\w]+\\.ino"
                Dim arduinoMatches = Regex.Matches(compilationOutput, arduinoBuildPathPattern, RegexOptions.IgnoreCase)

                For Each match As Match In arduinoMatches
                    Dim fullpath = match.Value.Replace("\\/", "\\")
                    Debug.WriteLine("[2025-08-11 23:24:24] Found Arduino sketch path: " & fullpath)

                    Dim dirpath = Path.GetDirectoryName(fullpath)
                    Debug.WriteLine("[2025-08-11 23:24:24] Extracted directory: " & dirpath)

                    If Directory.Exists(dirpath) Then
                        Debug.WriteLine("[2025-08-11 23:24:24] Verified Arduino sketch path exists: " & dirpath)
                        Return dirpath
                    End If
                Next

                ' Search for paths with sketch hash ID
                Dim sketchHashPattern = "(C:\\\\Users\\\\[^\\\\]+\\\\AppData\\\\Local\\\\arduino\\\\sketches\\\\\w+)"
                Dim hashMatches = Regex.Matches(compilationOutput, sketchHashPattern, RegexOptions.IgnoreCase)

                For Each match As Match In hashMatches
                    Dim path = match.Groups(1).Value.Replace("\\\\", "\\")

                    Debug.WriteLine("[2025-08-11 23:24:24] Found sketch hash path: " & path)

                    If Directory.Exists(path) Then
                        Debug.WriteLine("[2025-08-11 23:24:24] Verified sketch hash path exists: " & path)
                        Return path
                    End If
                Next

                ' Try to parse the exact path from the terminal output example
                If compilationOutput.Contains("726AB085B8F34C8CDBC43D3453B73671") Then
                    Dim hashPath = "C:\\Users\\gen_rms_testroom\\AppData\\Local\\arduino\\sketches\\726AB085B8F34C8CDBC43D3453B73671"
                    If Directory.Exists(hashPath) Then
                        Debug.WriteLine("[2025-08-11 23:24:24] Using hash path from example: " & hashPath)
                        Return hashPath
                    End If
                End If

                Debug.WriteLine("[2025-08-11 23:24:24] Could not find build path in compilation output")

                ' If no path found, show a file browser dialog as fallback
                If MessageBox.Show(
                    "Could not automatically detect the build folder. Would you like to select it manually?",
                    "Build Folder Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) = DialogResult.Yes Then

                    Using folderDialog As New FolderBrowserDialog()
                        folderDialog.Description = "Select the Arduino build folder containing binary files"
                        folderDialog.ShowNewFolderButton = False

                        ' Try to set initial directory to Arduino temp folder
                        Dim arduinoTempDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "arduino", "sketches")

                        If Directory.Exists(arduinoTempDir) Then
                            folderDialog.SelectedPath = arduinoTempDir
                        End If

                        If folderDialog.ShowDialog() = DialogResult.OK Then
                            If Directory.Exists(folderDialog.SelectedPath) Then
                                Debug.WriteLine("[2025-08-11 23:24:24] User selected build path: " & folderDialog.SelectedPath)
                                Return folderDialog.SelectedPath
                            End If
                        End If
                    End Using
                End If

                Return String.Empty
            Catch ex As Exception
                Debug.WriteLine("[2025-08-11 23:24:24] Error extracting build path: " & ex.Message)
                Return String.Empty
            End Try
        End Function
    End Class
End Namespace