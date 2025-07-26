<%@ Page Language="VB" AutoEventWireup="true" CodeBehind="ProjectEdit.aspx.vb" Inherits="KC_LINK_WebUploader.ProjectEdit" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Project Editor - KC-Link Arduino ESP32 Uploader</title>
    <link href="Content/bootstrap.min.css" rel="stylesheet" />
    <link href="Content/Site.css" rel="stylesheet" />
    <script src="Scripts/jquery-3.6.0.min.js"></script>
    <script src="Scripts/bootstrap.bundle.min.js"></script>
    <style type="text/css">
        .file-list {
            height: 400px;
            overflow-y: auto;
            border: 1px solid #ddd;
            border-radius: 4px;
        }
        .editor {
            height: 550px;
            width: 100%;
            font-family: 'Consolas', monospace;
            font-size: 14px;
        }
        .file-item {
            padding: 4px 8px;
            cursor: pointer;
        }
        .file-item:hover {
            background-color: #f5f5f5;
        }
        .file-item.active {
            background-color: #007bff;
            color: white;
        }
        .ino-file {
            color: #007bff;
            font-weight: bold;
        }
        .cpp-file {
            color: #28a745;
        }
        .h-file {
            color: #fd7e14;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container-fluid">
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
                                    <a class="nav-link" href="Settings.aspx">Settings</a>
                                </li>
                            </ul>
                        </div>
                    </div>
                </nav>
            </div>

            <div class="row mt-3">
                <div class="col-md-12">
                    <h1>Project Editor</h1>
                    <div class="alert alert-info" role="alert">
                        Project: <strong><asp:Label ID="lblProjectName" runat="server" Text=""></asp:Label></strong>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-3">
                    <div class="card mb-3">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <h5 class="mb-0">Files</h5>
                            <div>
                                <asp:LinkButton ID="btnAddNewFile" runat="server" CssClass="btn btn-sm btn-primary"
                                    OnClick="btnAddNewFile_Click">
                                    <i class="bi bi-plus"></i> Add
                                </asp:LinkButton>
                            </div>
                        </div>
                        <div class="card-body p-0">
                            <div class="file-list">
                                <asp:Repeater ID="rptFiles" runat="server" OnItemCommand="rptFiles_ItemCommand">
                                    <ItemTemplate>
                                        <div class="file-item <%# GetFileClass(Container.DataItem) %>">
                                            <asp:LinkButton ID="lnkSelectFile" runat="server" CommandName="SelectFile" 
                                                CommandArgument='<%# Eval("Path") %>' Text='<%# Eval("Name") %>' 
                                                CssClass="d-block text-decoration-none"></asp:LinkButton>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>
                    </div>

                    <div class="card mb-3">
                        <div class="card-header">
                            <h5 class="mb-0">Actions</h5>
                        </div>
                        <div class="card-body">
                            <div class="d-grid gap-2">
                                <asp:Button ID="btnCompile" runat="server" Text="Compile Project" 
                                    CssClass="btn btn-primary" OnClick="btnCompile_Click" />
                                <asp:Button ID="btnDownload" runat="server" Text="Download Project" 
                                    CssClass="btn btn-success" OnClick="btnDownload_Click" />
                                <asp:Button ID="btnDelete" runat="server" Text="Delete Project" 
                                    CssClass="btn btn-danger" OnClientClick="return confirm('Are you sure you want to delete this project?');"
                                    OnClick="btnDelete_Click" />
                                <asp:Button ID="btnBack" runat="server" Text="Back to Home" 
                                    CssClass="btn btn-secondary" OnClick="btnBack_Click" />
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-9">
                    <div class="card mb-3">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <h5 class="mb-0">
                                <asp:Label ID="lblCurrentFile" runat="server" Text="No file selected"></asp:Label>
                            </h5>
                            <div>
                                <asp:Button ID="btnSave" runat="server" Text="Save" CssClass="btn btn-primary" 
                                    OnClick="btnSave_Click" Enabled="false" />
                                <asp:Button ID="btnRename" runat="server" Text="Rename" CssClass="btn btn-outline-secondary" 
                                    OnClick="btnRename_Click" Enabled="false" />
                                <asp:Button ID="btnDelete2" runat="server" Text="Delete" CssClass="btn btn-outline-danger" 
                                    OnClientClick="return confirm('Are you sure you want to delete this file?');"
                                    OnClick="btnDelete2_Click" Enabled="false" />
                            </div>
                        </div>
                        <div class="card-body">
                            <asp:TextBox ID="txtEditor" runat="server" TextMode="MultiLine" CssClass="editor"></asp:TextBox>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Modal for adding new file -->
            <div class="modal fade" id="addFileModal" tabindex="-1" aria-labelledby="addFileModalLabel" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="addFileModalLabel">Add New File</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-group mb-3">
                                <label for="txtNewFileName">File Name:</label>
                                <asp:TextBox ID="txtNewFileName" runat="server" CssClass="form-control" placeholder="e.g., MyFile.ino"></asp:TextBox>
                            </div>
                            <div class="form-group mb-3">
                                <label for="ddlFileType">File Type:</label>
                                <asp:DropDownList ID="ddlFileType" runat="server" CssClass="form-control">
                                    <asp:ListItem Value=".ino">Arduino Sketch (.ino)</asp:ListItem>
                                    <asp:ListItem Value=".h">Header File (.h)</asp:ListItem>
                                    <asp:ListItem Value=".cpp">C++ Source File (.cpp)</asp:ListItem>
                                    <asp:ListItem Value=".c">C Source File (.c)</asp:ListItem>
                                    <asp:ListItem Value=".txt">Text File (.txt)</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <asp:Button ID="btnCreateNewFile" runat="server" Text="Create File" 
                                CssClass="btn btn-primary" OnClick="btnCreateNewFile_Click" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- Modal for renaming file -->
            <div class="modal fade" id="renameFileModal" tabindex="-1" aria-labelledby="renameFileModalLabel" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="renameFileModalLabel">Rename File</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-group">
                                <label for="txtRenameFile">New File Name:</label>
                                <asp:TextBox ID="txtRenameFile" runat="server" CssClass="form-control"></asp:TextBox>
                                <asp:HiddenField ID="hidCurrentFilePath" runat="server" />
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <asp:Button ID="btnRenameFile" runat="server" Text="Rename" 
                                CssClass="btn btn-primary" OnClick="btnRenameFile_Click" />
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <asp:ScriptManager ID="ScriptManager1" runat="server" />

        <script type="text/javascript">
            // Function to show Add File modal
            function showAddFileModal() {
                var addFileModal = new bootstrap.Modal(document.getElementById('addFileModal'));
                addFileModal.show();
            }

            // Function to show Rename File modal
            function showRenameFileModal() {
                var renameFileModal = new bootstrap.Modal(document.getElementById('renameFileModal'));
                renameFileModal.show();
            }
        </script>
    </form>
</body>
</html>