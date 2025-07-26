<%@ Page Language="VB" AutoEventWireup="true" CodeBehind="Default.aspx.vb" Inherits="KC_LINK_WebUploader.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>KC-Link Arduino ESP32 Uploader</title>
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
                                    <a class="nav-link active" href="Default.aspx">Home</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" href="ProjectUpload.aspx">Upload Project</a>
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

            <div class="jumbotron">
                <h1>KC-Link Arduino ESP32 Uploader</h1>
                <p class="lead">Upload and compile your Arduino sketches for ESP32 boards.</p>
                <p>
                    <asp:LinkButton ID="btnUploadNewProject" runat="server" CssClass="btn btn-primary btn-lg" 
                        OnClick="btnUploadNewProject_Click">Upload New Project</asp:LinkButton>
                </p>
            </div>

            <div class="row">
                <div class="col-lg-12">
                    <h2>Recent Projects</h2>
                    <asp:GridView ID="gvProjects" runat="server" AutoGenerateColumns="False" 
                        CssClass="table table-striped table-hover" OnRowCommand="gvProjects_RowCommand">
                        <Columns>
                            <asp:BoundField DataField="Name" HeaderText="Project Name" />
                            <asp:BoundField DataField="DateCreated" HeaderText="Date Created" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
                            <asp:TemplateField HeaderText="Files">
                                <ItemTemplate>
                                    <%# Eval("InoCount") %> .ino, <%# Eval("CppCount") %> .cpp, <%# Eval("HeaderCount") %> .h
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="MainFileLines" HeaderText="Main Lines" />
                            <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lnkCompile" runat="server" CommandName="Compile" 
                                        CommandArgument='<%# Eval("Name") %>' CssClass="btn btn-primary btn-sm">
                                        Compile
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="lnkEdit" runat="server" CommandName="Edit" 
                                        CommandArgument='<%# Eval("Name") %>' CssClass="btn btn-info btn-sm">
                                        Edit
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="lnkDownload" runat="server" CommandName="Download" 
                                        CommandArgument='<%# Eval("Name") %>' CssClass="btn btn-success btn-sm">
                                        Download
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="lnkDelete" runat="server" CommandName="Delete" 
                                        CommandArgument='<%# Eval("Name") %>' CssClass="btn btn-danger btn-sm"
                                        OnClientClick="return confirm('Are you sure you want to delete this project?');">
                                        Delete
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

            <div class="row mt-4">
                <div class="col-lg-6">
                    <div class="card">
                        <div class="card-header">
                            <h4>Board Configuration</h4>
                        </div>
                        <div class="card-body">
                            <div class="form-group mb-3">
                                <label for="ddlBoardType">Board Type:</label>
                                <asp:DropDownList ID="ddlBoardType" runat="server" CssClass="form-control" AutoPostBack="true"
                                    OnSelectedIndexChanged="ddlBoardType_SelectedIndexChanged"></asp:DropDownList>
                            </div>
                            <div class="form-group mb-3">
                                <label for="ddlPartitionScheme">Partition Scheme:</label>
                                <asp:DropDownList ID="ddlPartitionScheme" runat="server" CssClass="form-control"></asp:DropDownList>
                            </div>
                            <div class="form-group">
                                <asp:Button ID="btnConfigureBoard" runat="server" Text="Configure Board" 
                                    CssClass="btn btn-secondary" OnClick="btnConfigureBoard_Click" />
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-lg-6">
                    <div class="card">
                        <div class="card-header">
                            <h4>Quick Upload</h4>
                        </div>
                        <div class="card-body">
                            <p>Upload a new Arduino sketch file (.ino) directly:</p>
                            <div class="form-group mb-3">
                                <label for="fileQuickUpload">Select a .ino file:</label>
                                <asp:FileUpload ID="fileQuickUpload" runat="server" CssClass="form-control" />
                            </div>
                            <div class="form-group">
                            <asp:Button ID="btnQuickUpload" runat="server" Text="Upload & Compile" 
                                    CssClass="btn btn-primary" OnClick="btnQuickUpload_Click" />
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