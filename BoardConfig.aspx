<%@ Page Language="VB" AutoEventWireup="true" CodeBehind="BoardConfig.aspx.vb" Inherits="KC_LINK_WebUploader.BoardConfig" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Board Configuration - KC-Link Arduino ESP32 Uploader</title>
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
                                    <a class="nav-link active" href="BoardConfig.aspx">Board Configuration</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" href="Settings.aspx">Settings</a>
                                </li>
                            </ul>
                        </div>
                    </div>
                </nav>
            </div>

            <div class="row mt-3">
                <div class="col-md-12">
                    <h1>Board Configuration</h1>
                    <p>Configure settings for your ESP32 board.</p>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="card mb-3">
                        <div class="card-header">
                            <h4>Board Selection</h4>
                        </div>
                        <div class="card-body">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group mb-3">
                                        <label for="ddlBoardType">Board Type:</label>
                                        <asp:DropDownList ID="ddlBoardType" runat="server" CssClass="form-control" AutoPostBack="true"
                                            OnSelectedIndexChanged="ddlBoardType_SelectedIndexChanged"></asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group mb-3">
                                        <asp:Button ID="btnReload" runat="server" Text="Reload Boards" 
                                            CssClass="btn btn-secondary mt-4" OnClick="btnReload_Click" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="card mb-3">
                        <div class="card-header">
                            <h4>Configuration Parameters</h4>
                        </div>
                        <div class="card-body">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group mb-3">
                                        <label for="ddlCpuFreq">CPU Frequency:</label>
                                        <asp:DropDownList ID="ddlCpuFreq" runat="server" CssClass="form-control"></asp:DropDownList>
                                    </div>
                                    <div class="form-group mb-3">
                                        <label for="ddlFlashMode">Flash Mode:</label>
                                        <asp:DropDownList ID="ddlFlashMode" runat="server" CssClass="form-control"></asp:DropDownList>
                                    </div>
                                    <div class="form-group mb-3">
                                        <label for="ddlFlashFreq">Flash Frequency:</label>
                                        <asp:DropDownList ID="ddlFlashFreq" runat="server" CssClass="form-control"></asp:DropDownList>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group mb-3">
                                        <label for="ddlPartitionScheme">Partition Scheme:</label>
                                        <asp:DropDownList ID="ddlPartitionScheme" runat="server" CssClass="form-control"></asp:DropDownList>
                                    </div>
                                    <div class="form-group mb-3">
                                        <label for="ddlUploadSpeed">Upload Speed:</label>
                                        <asp:DropDownList ID="ddlUploadSpeed" runat="server" CssClass="form-control"></asp:DropDownList>
                                    </div>
                                    <div class="form-group mb-3">
                                        <label for="ddlDebugLevel">Debug Level:</label>
                                        <asp:DropDownList ID="ddlDebugLevel" runat="server" CssClass="form-control"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="form-check mb-3">
                                        <asp:CheckBox ID="chkPSRAM" runat="server" CssClass="form-check-input" />
                                        <label class="form-check-label" for="chkPSRAM">Enable PSRAM</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="card mb-3">
                        <div class="card-header">
                            <h4>FQBN (Fully Qualified Board Name)</h4>
                        </div>
                        <div class="card-body">
                            <div class="form-group mb-3">
                                <asp:TextBox ID="txtFQBN" runat="server" CssClass="form-control" TextMode="MultiLine" 
                                    Rows="3" ReadOnly="true"></asp:TextBox>
                            </div>
                            <div class="form-group">
                                <asp:Button ID="btnCopyFQBN" runat="server" Text="Copy FQBN" CssClass="btn btn-secondary"
                                    OnClientClick="copyToClipboard(); return false;" />
                                <script type="text/javascript">
                                    function copyToClipboard() {
                                        var textBox = document.getElementById('<%= txtFQBN.ClientID %>');
                                        textBox.select();
                                        document.execCommand('copy');
                                        alert('FQBN copied to clipboard');
                                    }
                                </script>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="form-group mb-3">
                        <asp:Button ID="btnSave" runat="server" Text="Save Configuration" 
                            CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        <asp:Button ID="btnReset" runat="server" Text="Reset to Defaults" 
                            CssClass="btn btn-warning" OnClick="btnReset_Click" />
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