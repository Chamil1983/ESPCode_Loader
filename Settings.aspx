<%@ Page Language="VB" AutoEventWireup="true" CodeBehind="Settings.aspx.vb" Inherits="KC_LINK_WebUploader.Settings" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Settings - KC-Link Arduino ESP32 Uploader</title>
    <link href="Content/bootstrap.min.css" rel="stylesheet" />
    <link href="Content/Site.css" rel="stylesheet" />
    <script src="Scripts/jquery-3.6.0.min.js"></script>
    <script src="Scripts/bootstrap.bundle.min.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="header clearfix">
                <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
                    <div class="container-fluid">
                        <a class="navbar-brand" href="#">KC-Link ESP32 Uploader</a>
                        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                            <span class="navbar-toggler-icon"></span>
                        </button>
                        <div class="collapse navbar-collapse" id="navbarNav">
                            <ul class="navbar-nav">
                                <li class="nav-item">
                                    <a class="nav-link" href="Default.aspx">Home</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" href="ProjectUpload.aspx">Upload Project</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" href="BoardConfig.aspx">Board Configuration</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link active" href="Settings.aspx">Settings</a>
                                </li>
                            </ul>
                        </div>
                    </div>
                </nav>
            </div>

            <div class="row mt-3">
                <div class="col-md-12">
                    <h1>Application Settings</h1>
                    <p>Configure settings for Arduino ESP32 compilation and uploading.</p>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="card mb-3">
                        <div class="card-header">
                            <h4>Arduino CLI Configuration</h4>
                        </div>
                        <div class="card-body">
                            <div class="form-group mb-3">
                                <label for="txtArduinoCliPath">Arduino CLI Path:</label>
                                <div class="input-group">
                                    <asp:TextBox ID="txtArduinoCliPath" runat="server" CssClass="form-control"></asp:TextBox>
                                    <asp:Button ID="btnBrowseArduinoCli" runat="server" Text="Browse..." 
                                        CssClass="btn btn-outline-secondary" OnClick="btnBrowseArduinoCli_Click" />
                                </div>
                                <small class="form-text text-muted">Path to the Arduino CLI executable</small>
                            </div>
                            <div class="form-group mb-3">
                                <label for="txtDefaultSketchDir">Default Sketch Directory:</label>
                                <div class="input-group">
                                    <asp:TextBox ID="txtDefaultSketchDir" runat="server" CssClass="form-control"></asp:TextBox>
                                    <asp:Button ID="btnBrowseSketchDir" runat="server" Text="Browse..." 
                                        CssClass="btn btn-outline-secondary" OnClick="btnBrowseSketchDir_Click" />
                                </div>
                                <small class="form-text text-muted">Default directory for Arduino sketches</small>
                            </div>
                            <div class="form-group mb-3">
                                <label for="txtHardwarePath">ESP32 Hardware Path:</label>
                                <div class="input-group">
                                    <asp:TextBox ID="txtHardwarePath" runat="server" CssClass="form-control"></asp:TextBox>
                                    <asp:Button ID="btnBrowseHardwarePath" runat="server" Text="Browse..." 
                                        CssClass="btn btn-outline-secondary" OnClick="btnBrowseHardwarePath_Click" />
                                </div>
                                <small class="form-text text-muted">Path to ESP32 hardware files</small>
                            </div>
                            <div class="form-group mb-3">
                                <label for="txtBoardsFilePath">boards.txt File Path:</label>
                                <div class="input-group">
                                    <asp:TextBox ID="txtBoardsFilePath" runat="server" CssClass="form-control"></asp:TextBox>
                                    <asp:Button ID="btnBrowseBoardsFile" runat="server" Text="Browse..." 
                                        CssClass="btn btn-outline-secondary" OnClick="btnBrowseBoardsFile_Click" />
                                </div>
                                <small class="form-text text-muted">Path to the boards.txt file for ESP32</small>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-6">
                    <div class="card mb-3">
                        <div class="card-header">
                            <h4>Compilation Settings</h4>
                        </div>
                        <div class="card-body">
                            <div class="form-group mb-3">
                                <label for="txtCompileTimeout">Compile Timeout (seconds):</label>
                                <asp:TextBox ID="txtCompileTimeout" runat="server" CssClass="form-control" 
                                    TextMode="Number" min="30" max="600"></asp:TextBox>
                                <small class="form-text text-muted">Maximum time allowed for compilation (30-600 seconds)</small>
                            </div>
                            <div class="form-check mb-3">
                                <asp:CheckBox ID="chkVerboseOutput" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="chkVerboseOutput">Enable Verbose Output</label>
                                <small class="d-block form-text text-muted">Show detailed compilation information</small>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="card mb-3">
                        <div class="card-header">
                            <h4>Other Settings</h4>
                        </div>
                        <div class="card-body">
                            <div class="form-check mb-3">
                                <asp:CheckBox ID="chkEnableLogging" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="chkEnableLogging">Enable Application Logging</label>
                                <small class="d-block form-text text-muted">Log application actions to file</small>
                            </div>
                            <div class="form-group mb-3">
                                <label for="ddlDefaultBoard">Default Board:</label>
                                <asp:DropDownList ID="ddlDefaultBoard" runat="server" CssClass="form-control"></asp:DropDownList>
                                <small class="form-text text-muted">Default board to use for new projects</small>
                            </div>
                            <div class="form-group mb-3">
                                <label for="ddlDefaultPartition">Default Partition Scheme:</label>
                                <asp:DropDownList ID="ddlDefaultPartition" runat="server" CssClass="form-control"></asp:DropDownList>
                                <small class="form-text text-muted">Default partition scheme to use</small>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="form-group mb-3">
                        <asp:Button ID="btnSaveSettings" runat="server" Text="Save Settings" 
                            CssClass="btn btn-primary" OnClick="btnSaveSettings_Click" />
                        <asp:Button ID="btnResetDefaults" runat="server" Text="Reset to Defaults" 
                            CssClass="btn btn-warning" OnClick="btnResetDefaults_Click" />
                        <asp:Button ID="btnCancel" runat="server" Text="Cancel" 
                            CssClass="btn btn-secondary" OnClick="btnCancel_Click" />
                    </div>
                </div>
            </div>

            <footer class="footer mt-4">
                <p>&copy; <%: DateTime.Now.Year %> - KC-Link Arduino ESP32 Uploader - Last login: Chamil1983 at <%= DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") %> UTC</p>
            </footer>
        </div>
    </form>
</body>
</html>