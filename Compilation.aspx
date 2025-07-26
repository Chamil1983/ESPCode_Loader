<%@ Page Language="VB" AutoEventWireup="true" CodeBehind="Compilation.aspx.vb" Inherits="KC_LINK_WebUploader.Compilation" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Compilation - KC-Link Arduino ESP32 Uploader</title>
    <link href="Content/bootstrap.min.css" rel="stylesheet" />
    <link href="Content/Site.css" rel="stylesheet" />
    <script src="Scripts/jquery-3.6.0.min.js"></script>
    <script src="Scripts/bootstrap.bundle.min.js"></script>
    <style type="text/css">
        .output-console {
            font-family: 'Consolas', monospace;
            background-color: #000;
            color: #0F0;
            padding: 10px;
            height: 400px;
            overflow-y: auto;
            white-space: pre-wrap;
            border-radius: 5px;
        }
        .progress-large {
            height: 25px;
        }
        .resource-bar {
            height: 20px;
            margin-bottom: 10px;
        }
        .flash-bar {
            background-color: royalblue;
        }
        .ram-bar {
            background-color: mediumpurple;
        }
    </style>
    <script type="text/javascript">
        var intervalId;
        function startProgressCheck() {
            // Check compilation progress every 2 seconds
            intervalId = setInterval(function() {
                // Make AJAX request to get progress
                $.ajax({
                    type: "POST",
                    url: "Compilation.aspx/GetCompilationProgress",
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function(response) {
                        var result = response.d;
                        
                        // Update progress bar
                        $("#compilationProgress").css("width", result.Progress + "%").attr("aria-valuenow", result.Progress);
                        $("#progressLabel").text(result.Phase + " (" + result.Progress + "%)");
                        
                        // Update output text
                        $("#outputConsole").text(result.OutputLog);
                        $("#outputConsole").scrollTop($("#outputConsole")[0].scrollHeight);
                        
                        // Update status text
                        $("#statusLabel").text(result.Status);
                        
                        // If compilation is complete or failed, stop checking and update UI
                        if (result.Status === "Completed" || result.Status === "Failed" || result.Status === "Cancelled") {
                            clearInterval(intervalId);
                            $("#btnStartCompile").prop("disabled", false).text("Compile Again");
                            $("#btnCancel").prop("disabled", true);
                            
                            if (result.Status === "Completed") {
                                $("#statusLabel").text("Compilation Successful");
                                $("#compilationProgress").removeClass("bg-info").addClass("bg-success");
                                
                                // Update resource usage bars
                                $("#flashBar").css("width", result.FlashPercentage + "%");
                                $("#flashLabel").text("Flash: " + result.FlashSize + " bytes (" + result.FlashPercentage + "%)");
                                
                                $("#ramBar").css("width", result.RAMPercentage + "%");
                                $("#ramLabel").text("RAM: " + result.RAMSize + " bytes (" + result.RAMPercentage + "%)");
                                
                                // Enable download button if binary was generated
                                $("#btnDownloadBinary").prop("disabled", false);
                            } else if (result.Status === "Failed") {
                                $("#statusLabel").text("Compilation Failed");
                                $("#compilationProgress").removeClass("bg-info").addClass("bg-danger");
                            } else {
                                $("#statusLabel").text("Compilation Cancelled");
                                $("#compilationProgress").removeClass("bg-info").addClass("bg-warning");
                            }
                        }
                    },
                    error: function() {
                        // Handle errors or connection issues
                        $("#statusLabel").text("Error checking progress");
                    }
                });
            }, 2000);
        }
        
        function stopProgressCheck() {
            clearInterval(intervalId);
        }
    </script>
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
                                    <a class="nav-link" href="Settings.aspx">Settings</a>
                                </li>
                            </ul>
                        </div>
                    </div>
                </nav>
            </div>

            <div class="row mt-3">
                <div class="col-md-12">
                    <h1>Compilation</h1>
                    <div class="alert alert-info" role="alert">
                        <strong>Project:</strong> <asp:Label ID="lblProjectName" runat="server" Text=""></asp:Label> &nbsp;|&nbsp;
                        <strong>Board:</strong> <asp:Label ID="lblBoardName" runat="server" Text=""></asp:Label> &nbsp;|&nbsp;
                        <strong>Partition:</strong> <asp:Label ID="lblPartition" runat="server" Text=""></asp:Label>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="card mb-3">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <h4>Compilation Output</h4>
                            <span class="badge bg-primary" id="statusLabel">Ready</span>
                        </div>
                        <div class="card-body">
                            <div class="progress progress-large mb-3">
                                <div id="compilationProgress" class="progress-bar bg-info progress-bar-striped progress-bar-animated"
                                    role="progressbar" style="width: 0%;" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
                                </div>
                            </div>
                            <p id="progressLabel">Ready to compile</p>

                            <div id="outputConsole" class="output-console"></div>

                            <div class="mt-3">
                                <asp:Button ID="btnStartCompile" runat="server" Text="Start Compilation" 
                                    CssClass="btn btn-primary" OnClick="btnStartCompile_Click" />
                                <asp:Button ID="btnCancel" runat="server" Text="Cancel" 
                                    CssClass="btn btn-danger" OnClick="btnCancel_Click" Enabled="false" />
                                <asp:Button ID="btnDownloadBinary" runat="server" Text="Download Binary" 
                                    CssClass="btn btn-success" OnClick="btnDownloadBinary_Click" Enabled="false" />
                                <asp:Button ID="btnBack" runat="server" Text="Back to Home" 
                                    CssClass="btn btn-secondary" OnClick="btnBack_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-12">
                    <div class="card mb-3">
                        <div class="card-header">
                            <h4>Hardware Resources</h4>
                        </div>
                        <div class="card-body">
                            <div class="row">
                                <div class="col-md-6">
                                    <p id="flashLabel">Flash: 0 bytes (0%)</p>
                                    <div class="progress resource-bar">
                                        <div id="flashBar" class="progress-bar flash-bar" role="progressbar" 
                                            style="width: 0%;" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <p id="ramLabel">RAM: 0 bytes (0%)</p>
                                    <div class="progress resource-bar">
                                        <div id="ramBar" class="progress-bar ram-bar" role="progressbar" 
                                            style="width: 0%;" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
                                        </div>
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
        
        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />
    </form>
</body>
</html>