<%@ Page Language="VB" AutoEventWireup="true" CodeBehind="ProjectUpload.aspx.vb" Inherits="KC_LINK_WebUploader.ProjectUpload" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Upload Project - KC-Link Arduino ESP32 Uploader</title>
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
                                    <a class="nav-link active" href="ProjectUpload.aspx">Upload Project</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" href="BoardConfig.aspx">Board Configuration</a>
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
                    <h1>Upload Arduino Project</h1>
                    <p>Upload your Arduino project files. You can either upload a ZIP archive containing all project files, or upload individual files.</p>
                </div>
            </div>

            <div class="row">
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h4>Upload ZIP Archive</h4>
                        </div>
                        <div class="card-body">
                            <p>Upload a ZIP file containing your entire Arduino project. The ZIP should include at least one .ino file.</p>
                            <div class="form-group mb-3">
                                <label for="fileProject">Select ZIP File:</label>
                                <asp:FileUpload ID="fileProject" runat="server" CssClass="form-control" />
                            </div>
                            <div class="form-group mb-3">
                                <label for="txtProjectName">Project Name (optional):</label>
                                <asp:TextBox ID="txtProjectName" runat="server" CssClass="form-control" 
                                    placeholder="Leave blank to use the ZIP filename"></asp:TextBox>
                            </div>
                            <div class="form-group">
                                <asp:Button ID="btnUploadZip" runat="server" Text="Upload Project" 
                                    CssClass="btn btn-primary" OnClick="btnUploadZip_Click" />
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h4>Create New Project</h4>
                        </div>
                        <div class="card-body">
                            <p>Create a new project from scratch or use a template.</p>
                            <div class="form-group mb-3">
                                <label for="txtNewProjectName">New Project Name:</label>
                                <asp:TextBox ID="txtNewProjectName" runat="server" CssClass="form-control" 
                                    placeholder="Enter project name"></asp:TextBox>
                            </div>
                            <div class="form-group mb-3">
                                <label for="ddlTemplate">Template (optional):</label>
                                <asp:DropDownList ID="ddlTemplate" runat="server" CssClass="form-control">
                                    <asp:ListItem Value="">-- Select a template --</asp:ListItem>
                                    <asp:ListItem Value="basic">Basic Sketch</asp:ListItem>
                                    <asp:ListItem Value="wifi">WiFi Example</asp:ListItem>
                                    <asp:ListItem Value="bluetooth">Bluetooth Example</asp:ListItem>
                                    <asp:ListItem Value="kclink">KC-Link Starter</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="form-group">
                                <asp:Button ID="btnCreateNew" runat="server" Text="Create Project" 
                                    CssClass="btn btn-success" OnClick="btnCreateNew_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row mt-4">
                <div class="col-md-12">
                    <div class="card">
                        <div class="card-header">
                            <h4>Board Configuration</h4>
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
                                        <label for="ddlPartitionScheme">Partition Scheme:</label>
                                        <asp:DropDownList ID="ddlPartitionScheme" runat="server" CssClass="form-control"></asp:DropDownList>
                                    </div>
                                </div>
                            </div>
                        </div>
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