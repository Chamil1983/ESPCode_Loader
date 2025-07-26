Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Web
Imports System.Web.UI

Public Class ProjectUpload
    Inherits System.Web.UI.Page

    Private _projectManager As ProjectManager
    Private _boardManager As BoardManager

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        ' Initialize managers
        _projectManager = New ProjectManager()
        _boardManager = New BoardManager()

        If Not IsPostBack Then
            ' Populate board types dropdown
            PopulateBoardTypes()

            ' Populate partition schemes dropdown
            PopulatePartitionSchemes()
        End If
    End Sub

    Private Sub PopulateBoardTypes()
        ddlBoardType.Items.Clear()

        ' Add board types from board manager
        For Each boardName In _boardManager.GetBoardNames()
            ddlBoardType.Items.Add(New ListItem(boardName, boardName))
        Next

        ' Set default selection from settings
        If Not String.IsNullOrEmpty(My.Settings.DefaultBoard) Then
            Dim item = ddlBoardType.Items.FindByValue(My.Settings.DefaultBoard)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If
    End Sub

    Private Sub PopulatePartitionSchemes()
        ddlPartitionScheme.Items.Clear()

        ' Add default partition schemes
        ddlPartitionScheme.Items.Add(New ListItem("Default", "default"))
        ddlPartitionScheme.Items.Add(New ListItem("Minimal SPIFFS", "min_spiffs"))
        ddlPartitionScheme.Items.Add(New ListItem("Minimal OTA", "min_ota"))
        ddlPartitionScheme.Items.Add(New ListItem("Huge App", "huge_app"))
        ddlPartitionScheme.Items.Add(New ListItem("Custom", "custom"))

        ' Add any custom partition schemes
        For Each scheme In _boardManager.GetCustomPartitions()
            If scheme <> "default" AndAlso scheme <> "min_spiffs" AndAlso scheme <> "min_ota" _
                AndAlso scheme <> "huge_app" AndAlso scheme <> "custom" Then
                ddlPartitionScheme.Items.Add(New ListItem(scheme, scheme))
            End If
        Next

        ' Set default selection from settings
        If Not String.IsNullOrEmpty(My.Settings.DefaultPartition) Then
            Dim item = ddlPartitionScheme.Items.FindByValue(My.Settings.DefaultPartition)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If
    End Sub

    Protected Sub ddlBoardType_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Update default partition scheme based on selected board
        Dim selectedBoard = ddlBoardType.SelectedValue
        Dim defaultPartition = _boardManager.GetDefaultPartitionForBoard(selectedBoard)

        If Not String.IsNullOrEmpty(defaultPartition) Then
            Dim item = ddlPartitionScheme.Items.FindByValue(defaultPartition)
            If item IsNot Nothing Then
                item.Selected = True
            End If
        End If
    End Sub

    Protected Sub btnUploadZip_Click(sender As Object, e As EventArgs)
        If Not fileProject.HasFile Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Please select a file to upload.');", True)
            Return
        End If

        ' Check file extension
        Dim ext = Path.GetExtension(fileProject.FileName).ToLower()
        If ext <> ".zip" Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Please select a ZIP file.');", True)
            Return
        End If

        ' Get project name
        Dim projectName = txtProjectName.Text.Trim()
        If String.IsNullOrEmpty(projectName) Then
            projectName = Path.GetFileNameWithoutExtension(fileProject.FileName)
        End If

        ' Upload the project
        Dim projectPath = _projectManager.UploadProject(fileProject.PostedFile, projectName)

        If String.IsNullOrEmpty(projectPath) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Failed to upload project. Please check if the ZIP contains at least one .ino file.');", True)
            Return
        End If

        ' Save selected board and partition to session
        Session("SelectedBoard") = ddlBoardType.SelectedValue
        Session("SelectedPartition") = ddlPartitionScheme.SelectedValue
        Session("SelectedProject") = projectName

        ' Redirect to compilation page
        Response.Redirect($"Compilation.aspx?project={projectName}")
    End Sub

    Protected Sub btnCreateNew_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(txtNewProjectName.Text.Trim()) Then
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "alert",
                "alert('Please enter a project name.');", True)
            Return
        End If

        ' Create a clean project name
        Dim projectName = txtNewProjectName.Text.Trim()
        projectName = projectName.Replace(" ", "_")
        projectName = Path.GetFileNameWithoutExtension(projectName)

        ' Add timestamp for uniqueness
        projectName = $"{projectName}_{DateTime.Now:yyyyMMddHHmmss}"

        ' Create project directory
        Dim projectDir = Path.Combine(Server.MapPath("~/App_Data/Projects"), projectName)
        If Not Directory.Exists(projectDir) Then
            Directory.CreateDirectory(projectDir)
        End If

        ' Create basic sketch file
        Dim sketchFile = Path.Combine(projectDir, $"{projectName}.ino")
        Dim sketchContent As String = ""

        ' Use selected template or default basic template
        Select Case ddlTemplate.SelectedValue
            Case "basic"
                sketchContent = GetBasicTemplate(projectName)
            Case "wifi"
                sketchContent = GetWiFiTemplate(projectName)
            Case "bluetooth"
                sketchContent = GetBluetoothTemplate(projectName)
            Case "kclink"
                sketchContent = GetKCLinkTemplate(projectName)
            Case Else
                sketchContent = GetBasicTemplate(projectName)
        End Select

        ' Write sketch content
        File.WriteAllText(sketchFile, sketchContent)

        ' Save selected board and partition to session
        Session("SelectedBoard") = ddlBoardType.SelectedValue
        Session("SelectedPartition") = ddlPartitionScheme.SelectedValue
        Session("SelectedProject") = projectName

        ' Redirect to the project edit page
        Response.Redirect($"ProjectEdit.aspx?project={projectName}")
    End Sub

    ' Template methods
    Private Function GetBasicTemplate(projectName As String) As String
        Return $"/*
 * Project: {projectName}
 * Created by: KC-Link Arduino ESP32 Uploader
 * Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
 */

void setup() {{
  // Initialize serial communication
  Serial.begin(115200);
  
  // Wait for serial to be ready
  delay(1000);
  
  Serial.println(""Hello from ESP32!"");
}}

void loop() {{
  // Main loop code
  delay(1000);
  Serial.println(""ESP32 is running..."");
}}
"
    End Function

    Private Function GetWiFiTemplate(projectName As String) As String
        Return $"/*
 * Project: {projectName}
 * Created by: KC-Link Arduino ESP32 Uploader
 * Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
 * 
 * WiFi Example for ESP32
 */

#include <WiFi.h>

const char* ssid = ""YourNetworkName"";
const char* password = ""YourNetworkPassword"";

void setup() {{
  // Initialize serial communication
  Serial.begin(115200);
  delay(1000);
  
  // Connect to WiFi network
  Serial.println();
  Serial.print(""Connecting to "");
  Serial.println(ssid);
  
  WiFi.begin(ssid, password);
  
  while (WiFi.status() != WL_CONNECTED) {{
    delay(500);
    Serial.print(""."");
  }}
  
  Serial.println("""");
  Serial.println(""WiFi connected"");
  Serial.println(""IP address: "");
  Serial.println(WiFi.localIP());
}}

void loop() {{
  // Print WiFi signal strength
  Serial.print(""Signal strength (RSSI): "");
  Serial.println(WiFi.RSSI());
  delay(5000);
}}
"
    End Function

    Private Function GetBluetoothTemplate(projectName As String) As String
        Return $"/*
 * Project: {projectName}
 * Created by: KC-Link Arduino ESP32 Uploader
 * Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
 * 
 * Bluetooth Example for ESP32
 */

#include ""BluetoothSerial.h""

// Check if Bluetooth is available
#if !defined(CONFIG_BT_ENABLED) || !defined(CONFIG_BLUEDROID_ENABLED)
#error Bluetooth is not enabled! Please run `make menuconfig` to enable it
#endif

BluetoothSerial SerialBT;

void setup() {{
  Serial.begin(115200);
  SerialBT.begin(""{projectName}""); // Bluetooth device name
  Serial.println(""Bluetooth device started, you can pair with it now!"");
}}

void loop() {{
  // Forward data from Serial to Bluetooth
  if (Serial.available()) {{
    SerialBT.write(Serial.read());
  }}
  
  // Forward data from Bluetooth to Serial
  if (SerialBT.available()) {{
    Serial.write(SerialBT.read());
  }}
  
  delay(20);
}}
"
    End Function

    Private Function GetKCLinkTemplate(projectName As String) As String
        Return $"/*
 * Project: {projectName}
 * Created by: KC-Link Arduino ESP32 Uploader
 * Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
 * 
 * KC-Link Starter Template for ESP32
 */

#include <WiFi.h>
#include <EEPROM.h>
#include <ESPmDNS.h>

// WiFi credentials
const char* ssid = ""YourNetworkName"";
const char* password = ""YourNetworkPassword"";

// Device settings
#define DEVICE_NAME ""{projectName}""
#define LED_BUILTIN 2  // Most ESP32 boards have this LED on GPIO2

// Function prototypes
void setupWiFi();
void blinkLED(int times, int delayMs);

void setup() {{
  // Initialize serial communication
  Serial.begin(115200);
  delay(1000);
  
  // Initialize LED pin
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);
  
  // Initialize EEPROM
  EEPROM.begin(512);
  
  // Welcome message
  Serial.println(""========================================"");
  Serial.println(""KC-Link ESP32 Device Starting..."");
  Serial.println(""Device Name: "" + String(DEVICE_NAME));
  Serial.println(""========================================"");
  
  // Connect to WiFi
  setupWiFi();
  
  // Initialize mDNS
  if (MDNS.begin(DEVICE_NAME)) {{
    Serial.println(""mDNS responder started"");
  }} else {{
    Serial.println(""Error setting up mDNS responder"");
  }}
  
  // Indicate setup complete
  blinkLED(3, 200);
  Serial.println(""Setup complete. System running."");
}}

void loop() {{
  // Main loop code
  delay(5000);
  
  // Blink LED once to show activity
  blinkLED(1, 100);
  
  // Print system status
  Serial.println(""System is running. WiFi status: "" + 
    String(WiFi.status() == WL_CONNECTED ? ""Connected"" : ""Disconnected""));
}}

void setupWiFi() {{
  Serial.println();
  Serial.print(""Connecting to WiFi network: "");
  Serial.println(ssid);
  
  WiFi.begin(ssid, password);
  
  // Wait for connection with timeout
  int timeout = 0;
  while (WiFi.status() != WL_CONNECTED && timeout < 20) {{
    delay(500);
    Serial.print(""."");
    timeout++;
    blinkLED(1, 50);  // Quick blink while connecting
  }}
  
  if (WiFi.status() == WL_CONNECTED) {{
    Serial.println("""");
    Serial.println(""WiFi connected"");
    Serial.println(""IP address: "" + WiFi.localIP().toString());
  }} else {{
    Serial.println("""");
    Serial.println(""WiFi connection failed. Running without network."");
  }}
}}

void blinkLED(int times, int delayMs) {{
  for (int i = 0; i < times; i++) {{
    digitalWrite(LED_BUILTIN, HIGH);
    delay(delayMs);
    digitalWrite(LED_BUILTIN, LOW);
    delay(delayMs);
  }}
}}
"
    End Function
End Class