<%@ Page Language="VB" AutoEventWireup="false" CodeBehind="Default.aspx.vb" Inherits="ArduinoWeb.Default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>ESP32 Arduino Web Loader</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
    <script src="https://cdn.jsdelivr.net/npm/chart.js@3.9.1/dist/chart.min.js"></script>
    <style>
        :root {
            --primary: #0052cc;
            --primary-light: #2684FF;
            --primary-dark: #0747A6;
            --success: #36B37E;
            --success-light: #ABF5D1;
            --warning: #FF991F;
            --danger: #FF5630;
            --danger-light: #FFE2DD;
            --info: #00B8D9;
            --info-light: #B3F5FF;
            --gray-100: #F4F5F7;
            --gray-200: #EBECF0;
            --gray-300: #DFE1E6;
            --gray-400: #C1C7D0;
            --gray-500: #97A0AF;
            --gray-600: #6B778C;
            --gray-700: #42526E;
            --gray-800: #253858;
            --gray-900: #172B4D;
            --terminal-bg: #1E1E1E;
            --terminal-text: #5fff5c;
            --shadow-sm: 0 1px 2px rgba(9, 30, 66, 0.1);
            --shadow: 0 3px 5px rgba(9, 30, 66, 0.2);
            --shadow-lg: 0 8px 16px rgba(9, 30, 66, 0.25);
            --shadow-xl: 0 12px 24px rgba(9, 30, 66, 0.25);
            --border-radius: 4px;
            --transition: all 0.2s ease;
            --font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
            --font-mono: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, Courier, monospace;
        }
        
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }
        
        body {
            font-family: var(--font-family);
            background-color: var(--gray-100);
            color: var(--gray-900);
            line-height: 1.5;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }
        
        .container {
            max-width: 1000px;
            margin: 24px auto;
            padding: 0 16px;
        }
        
        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 24px;
        }
        
        .header-title {
            display: flex;
            align-items: center;
            gap: 12px;
            color: var(--primary-dark);
        }
        
        .header-title h1 {
            font-weight: 600;
            font-size: 24px;
        }
        
        .header-logo {
            width: 40px;
            height: 40px;
            display: flex;
            align-items: center;
            justify-content: center;
            background-color: var(--primary);
            border-radius: 10px;
            color: white;
            font-size: 20px;
        }
        
        .card {
            background-color: white;
            border-radius: var(--border-radius);
            box-shadow: var(--shadow);
            margin-bottom: 16px;
            overflow: hidden;
            transition: var(--transition);
        }
        
        .card:hover {
            box-shadow: var(--shadow-lg);
        }
        
        .card-header {
            padding: 16px 20px;
            background-color: white;
            border-bottom: 1px solid var(--gray-200);
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        
        .card-header h2 {
            font-size: 16px;
            font-weight: 600;
            color: var(--gray-800);
            display: flex;
            align-items: center;
            gap: 8px;
        }
        
        .card-header h2 i {
            color: var(--primary);
        }
        
        .card-body {
            padding: 20px;
        }
        
        .form-group {
            margin-bottom: 16px;
        }
        
        .form-row {
            display: flex;
            gap: 16px;
            margin-bottom: 16px;
        }
        
        .form-col {
            flex: 1;
        }
        
        label {
            display: block;
            font-size: 13px;
            font-weight: 500;
            color: var(--gray-700);
            margin-bottom: 6px;
        }
        
        input[type="text"],
        select,
        textarea {
            width: 100%;
            padding: 8px 10px;
            font-size: 14px;
            border: 1px solid var(--gray-300);
            border-radius: var(--border-radius);
            background-color: white;
            transition: var(--transition);
        }
        
        input[type="text"]:focus,
        select:focus,
        textarea:focus {
            border-color: var(--primary-light);
            box-shadow: 0 0 0 2px rgba(0, 82, 204, 0.2);
            outline: none;
        }
        
        .btn-group {
            display: flex;
            gap: 8px;
            margin: 16px 0;
        }
        
        .btn {
            padding: 8px 16px;
            font-size: 14px;
            font-weight: 500;
            border: none;
            border-radius: var(--border-radius);
            cursor: pointer;
            transition: var(--transition);
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            min-width: 80px;
        }
        
        .btn-primary {
            background-color: var(--primary);
            color: white;
        }
        
        .btn-primary:hover {
            background-color: var(--primary-dark);
        }
        
        .btn-success {
            background-color: var(--success);
            color: white;
        }
        
        .btn-success:hover {
            background-color: #2D9969;
        }
        
        .btn-danger {
            background-color: var(--danger);
            color: white;
        }
        
        .btn-danger:hover {
            background-color: #E34C26;
        }
        
        .btn-sm {
            padding: 4px 10px;
            font-size: 12px;
            min-width: auto;
        }
        
        .file-input-wrapper {
            position: relative;
            display: inline-block;
            margin-top: 8px;
        }
        
        .file-input {
            position: absolute;
            left: 0;
            top: 0;
            opacity: 0;
            cursor: pointer;
            width: 100%;
            height: 100%;
            z-index: 2;
        }
        
        .file-input-label {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 12px;
            background-color: var(--gray-200);
            color: var(--gray-700);
            border-radius: var(--border-radius);
            font-size: 13px;
            transition: var(--transition);
            cursor: pointer;
        }
        
        .file-input:hover + .file-input-label {
            background-color: var(--gray-300);
        }
        
        .file-input:focus + .file-input-label {
            box-shadow: 0 0 0 2px rgba(0, 82, 204, 0.2);
        }
        
        .tab-nav {
            display: flex;
            border-bottom: 1px solid var(--gray-300);
            margin-bottom: 20px;
        }
        
        .tab-btn {
            padding: 10px 16px;
            font-size: 14px;
            font-weight: 500;
            color: var(--gray-700);
            border: none;
            background: none;
            border-bottom: 2px solid transparent;
            cursor: pointer;
            transition: var(--transition);
        }
        
        .tab-btn:hover {
            color: var(--primary);
        }
        
        .tab-btn.active {
            color: var(--primary);
            border-bottom-color: var(--primary);
        }
        
        .tab-content {
            display: none;
        }
        
        .tab-content.active {
            display: block;
        }
        
        .terminal {
            background-color: var(--terminal-bg);
            color: var(--terminal-text);
            font-family: var(--font-mono);
            font-size: 13px;
            padding: 12px;
            border-radius: var(--border-radius);
            overflow-y: auto;
            min-height: 240px;
            max-height: 400px;
            white-space: pre-wrap;
            word-wrap: break-word;
        }
        
        .badge {
            display: inline-flex;
            align-items: center;
            padding: 2px 8px;
            font-size: 12px;
            font-weight: 500;
            border-radius: 12px;
        }
        
        .badge-success {
            background-color: var(--success-light);
            color: var(--success);
        }
        
        .badge-info {
            background-color: var(--info-light);
            color: var(--info);
        }
        
        .badge-warning {
            background-color: #FFFAE6;
            color: var(--warning);
        }
        
        .badge-danger {
            background-color: var(--danger-light);
            color: var(--danger);
        }
        
        .alert {
            padding: 12px 16px;
            margin-bottom: 16px;
            border-radius: var(--border-radius);
            display: flex;
            align-items: flex-start;
            gap: 12px;
        }
        
        .alert i {
            font-size: 16px;
            margin-top: 2px;
        }
        
        .alert-success {
            background-color: var(--success-light);
            color: var(--success);
        }
        
        .alert-info {
            background-color: var(--info-light);
            color: var(--info);
        }
        
        .alert-warning {
            background-color: #FFFAE6;
            color: var(--warning);
        }
        
        .alert-danger {
            background-color: var(--danger-light);
            color: var(--danger);
        }
        
        .progress-container {
            margin: 12px 0;
        }
        
        .progress-header {
            display: flex;
            justify-content: space-between;
            margin-bottom: 6px;
        }
        
        .progress-label {
            font-size: 13px;
            font-weight: 500;
            color: var(--gray-700);
        }
        
        .progress-bar {
            height: 8px;
            background-color: var(--gray-200);
            border-radius: 4px;
            overflow: hidden;
        }
        
        .progress-value {
            height: 100%;
            background-color: var(--primary);
            border-radius: 4px;
            transition: width 0.3s ease;
        }
        
        .status-indicator {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            font-size: 14px;
            font-weight: 500;
        }
        
        .status-indicator.success {
            color: var(--success);
        }
        
        .status-indicator.error {
            color: var(--danger);
        }
        
        .status-indicator.pending {
            color: var(--gray-600);
        }
        
        .section-title {
            margin-bottom: 16px;
            color: var(--gray-700);
            font-weight: 600;
            font-size: 16px;
        }
        
        .info-box {
            background-color: var(--gray-100);
            border-radius: var(--border-radius);
            padding: 16px;
            margin-bottom: 16px;
        }
        
        .upload-result {
            margin-top: 8px;
            font-size: 13px;
        }
        
        .version-info {
            text-align: center;
            font-size: 12px;
            color: var(--gray-600);
            margin-top: 24px;
            padding-top: 16px;
            border-top: 1px solid var(--gray-200);
        }
        
        /* Statistics panel */
        .stats-panel {
            display: none;
            margin-top: 16px;
            animation: fadeIn 0.5s ease-in-out;
        }
        
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
            gap: 16px;
            margin-bottom: 24px;
        }
        
        .stat-card {
            background-color: white;
            padding: 16px;
            border-radius: var(--border-radius);
            box-shadow: var(--shadow-sm);
            border-left: 3px solid var(--primary);
        }
        
        .stat-title {
            font-size: 12px;
            color: var(--gray-600);
            margin-bottom: 4px;
        }
        
        .stat-value {
            font-size: 20px;
            font-weight: 600;
            color: var(--gray-900);
        }
        
        .stat-subtitle {
            font-size: 12px;
            color: var(--gray-500);
            margin-top: 4px;
        }
        
        .chart-container {
            margin-top: 20px;
            height: 300px;
            position: relative;
        }
        
        .chart-card {
            border-radius: var(--border-radius);
            background: white;
            box-shadow: var(--shadow-sm);
            padding: 20px;
        }
        
        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(10px); }
            to { opacity: 1; transform: translateY(0); }
        }
        
        /* Custom switch for status panel visibility */
        .switch {
            position: relative;
            display: inline-block;
            width: 40px;
            height: 20px;
            margin-left: 8px;
            vertical-align: middle;
        }
        
        .switch input {
            opacity: 0;
            width: 0;
            height: 0;
        }
        
        .slider {
            position: absolute;
            cursor: pointer;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background-color: var(--gray-300);
            transition: .4s;
            border-radius: 34px;
        }
        
        .slider:before {
            position: absolute;
            content: "";
            height: 16px;
            width: 16px;
            left: 2px;
            bottom: 2px;
            background-color: white;
            transition: .4s;
            border-radius: 50%;
        }
        
        input:checked + .slider {
            background-color: var(--primary);
        }
        
        input:checked + .slider:before {
            transform: translateX(20px);
        }
        
        /* Debug panel - made visible for troubleshooting */
        .debug-panel {
            display: block;
            font-size: 11px;
            color: #666;
            background: #f5f5f5;
            border: 1px solid #ddd;
            padding: 5px;
            margin-top: 5px;
            height: 100px;
            overflow: auto;
        }
        
        .partition-details {
            margin-top: 8px;
            font-size: 13px;
            color: var(--gray-700);
        }
        
        .status-panel {
            margin-bottom: 16px;
            padding: 12px;
            border-radius: var(--border-radius);
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        
        .status-panel.boards-txt {
            background-color: rgba(54, 179, 126, 0.1);
            border-left: 3px solid var(--success);
        }
        
        .status-panel.custom-partition {
            background-color: rgba(0, 184, 217, 0.1);
            border-left: 3px solid var(--info);
        }
        
        .tip {
            font-size: 13px;
            color: var(--gray-600);
            margin-top: 4px;
        }
        
        .tabs-row {
            display: flex;
            gap: 12px;
            margin-top: 16px;
        }
        
        .tabs-row button {
            background: var(--gray-200);
            border: none;
            padding: 8px 16px;
            border-radius: var(--border-radius);
            cursor: pointer;
            font-weight: 500;
            color: var(--gray-700);
        }
        
        .tabs-row button.active {
            background: var(--primary);
            color: white;
        }
        
        .stat-tabs-content > div {
            display: none;
        }
        
        .stat-tabs-content > div.active {
            display: block;
            animation: fadeIn 0.3s ease-in-out;
        }
        
        /* Enhanced board config styles */
        .board-config-container {
            margin-top: 24px;
            padding-top: 20px;
            border-top: 1px dashed var(--gray-300);
        }
        
        .board-options-title {
            font-size: 18px;
            font-weight: 600;
            color: var(--gray-800);
            margin-bottom: 12px;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        
        .board-options-description {
            font-size: 14px;
            color: var(--gray-600);
            margin-bottom: 20px;
        }
        
        .board-options-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
            gap: 16px;
            margin-bottom: 20px;
        }
        
        .board-option-card {
            background-color: white;
            border-radius: var(--border-radius);
            box-shadow: var(--shadow-sm);
            overflow: hidden;
            transition: var(--transition);
            border: 1px solid var(--gray-200);
        }
        
        .board-option-card:hover {
            box-shadow: var(--shadow);
            transform: translateY(-2px);
        }
        
        .board-option-header {
            padding: 12px 16px;
            background-color: var(--gray-100);
            border-bottom: 1px solid var(--gray-200);
        }
        
        .board-option-header h4 {
            margin: 0;
            font-size: 15px;
            font-weight: 500;
            color: var(--gray-800);
        }
        
        .board-option-desc {
            font-size: 12px;
            color: var(--gray-600);
            margin-top: 4px;
        }
        
        .board-option-content {
            padding: 12px 16px;
        }
        
        .fqbn-preview-container {
            margin-top: 24px;
            padding: 16px;
            background-color: var(--gray-100);
            border-radius: var(--border-radius);
            border-left: 3px solid var(--primary);
        }
        
        .fqbn-preview-label {
            font-size: 14px;
            margin-bottom: 8px;
            color: var(--gray-700);
        }
        
        .fqbn-preview-value {
            font-family: var(--font-mono);
            font-size: 13px;
            background-color: white;
            padding: 8px 12px;
            border-radius: var(--border-radius);
            border: 1px solid var(--gray-300);
            color: var(--gray-800);
            word-break: break-all;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />
    
    <div class="container">
        <!-- Header -->
        <div class="header">
            <div class="header-title">
                <div class="header-logo">
                    <i class="fas fa-microchip"></i>
                </div>
                <h1>ESP32 Arduino Web Loader</h1>
            </div>
        </div>
        
        <!-- Tab navigation -->
        <div class="tab-nav">
            <button type="button" id="btn-mainTab" class="tab-btn active" onclick="showTab('mainTab')">
                <i class="fas fa-code"></i> Development
            </button>
            <button type="button" id="btn-configTab" class="tab-btn" onclick="showTab('configTab')">
                <i class="fas fa-cog"></i> Configuration
            </button>
        </div>
        
        <!-- Main Tab Content -->
        <div id="mainTab" class="tab-content active">
            <!-- Project Setup Card -->
            <div class="card">
                <div class="card-header">
                    <h2><i class="fas fa-folder-open"></i> Project Setup</h2>
                </div>
                <div class="card-body">
                    <div class="form-group">
                        <label for="txtProjectDir">Project Folder (on server):</label>
                        <asp:TextBox ID="txtProjectDir" runat="server" placeholder="Enter the path to your sketch folder" />
                    </div>
                    
                    <div class="form-group">
                        <label>Upload Sketch as ZIP:</label>
                        <div class="file-input-wrapper">
                            <asp:FileUpload ID="fuSketchZip" runat="server" CssClass="file-input" />
                            <span class="file-input-label">
                                <i class="fas fa-upload"></i> Choose ZIP File
                            </span>
                        </div>
                        <asp:Button ID="btnUploadSketch" runat="server" Text="Upload Sketch ZIP" CssClass="btn btn-primary" OnClick="btnUploadSketch_Click" />
                        <div class="upload-result">
                            <asp:Label ID="lblUploadResult" runat="server" />
                        </div>
                    </div>
                    
                    <div class="form-group">
                        <label>Upload Single Sketch File (.ino):</label>
                        <div class="file-input-wrapper">
                            <asp:FileUpload ID="fuSingleSketch" runat="server" CssClass="file-input" />
                            <span class="file-input-label">
                                <i class="fas fa-file-code"></i> Choose .ino File
                            </span>
                        </div>
                        <asp:Button ID="btnUploadSingleSketch" runat="server" Text="Upload File" CssClass="btn btn-primary" OnClick="btnUploadSingleSketch_Click" />
                        <div class="form-group" style="margin-top: 8px;">
                            <asp:TextBox ID="txtSketchFile" runat="server" ReadOnly="true" placeholder="Uploaded sketch path will appear here" />
                        </div>
                        <div class="upload-result">
                            <asp:Label ID="lblSketchFileResult" runat="server" />
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- CLI Settings Card -->
            <div class="card">
                <div class="card-header">
                    <h2><i class="fas fa-terminal"></i> Arduino CLI Settings</h2>
                </div>
                <div class="card-body">
                    <div class="form-group">
                        <label for="txtCliPath">Arduino CLI Executable Path:</label>
                        <asp:TextBox ID="txtCliPath" runat="server" placeholder="Full path to arduino-cli executable (e.g., C:\path\to\arduino-cli.exe)" />
                        <div class="tip">
                            <i class="fas fa-info-circle"></i> This should be the path to arduino-cli on the server.
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- Status Panels -->
            <div id="boardsTxtStatusPanel" runat="server" class="status-panel boards-txt">
                <asp:Label ID="lblBoardsTxtStatus" runat="server" />
                <asp:Button ID="btnClearBoardsTxt" runat="server" Text="Clear" CssClass="btn btn-sm btn-danger" OnClick="btnClearBoardsTxt_Click" />
            </div>
            
            <div id="customPartitionStatusPanel" runat="server" class="status-panel custom-partition">
                <asp:Label ID="lblCustomPartitionStatus" runat="server" />
                <asp:Button ID="btnClearCustom" runat="server" Text="Clear" CssClass="btn btn-sm btn-danger" OnClick="btnClearCustom_Click" />
            </div>
            
            <!-- Build Settings Card -->
            <div class="card">
                <div class="card-header">
                    <h2><i class="fas fa-cogs"></i> Build Settings</h2>
                </div>
                <div class="card-body">
                    <div class="form-row">
                        <div class="form-col">
                            <label for="ddlBoard">Board Type:</label>
                            <asp:DropDownList ID="ddlBoard" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlBoard_SelectedIndexChanged" />
                        </div>
                        <div class="form-col">
                            <label for="ddlPartition">Partition Scheme:</label>
                            <asp:DropDownList ID="ddlPartition" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlPartition_SelectedIndexChanged" />
                            <div class="partition-details">
                                <asp:Literal ID="litPartitionCount" runat="server" />
                            </div>
                        </div>
                    </div>
                    
                    <div class="form-row">
                        <div class="form-col">
                            <label for="ddlSerial">Serial Port:</label>
                            <asp:DropDownList ID="ddlSerial" runat="server" />
                        </div>
                        <div class="form-col" style="display: flex; align-items: flex-end;">
                            <asp:Button ID="btnRefreshSerial" runat="server" Text="Refresh Ports" CssClass="btn btn-primary" OnClick="btnRefreshSerial_Click" />
                        </div>
                    </div>
                    
                    <!-- Board configuration options -->
                    <asp:PlaceHolder ID="plhBoardOptions" runat="server" />

                    <!-- Add the missing Literal control -->
                    <div style="display:none;">
                        <asp:Literal ID="litFQBN" runat="server" />
                    </div>
                </div>
            </div>
            
            <!-- Compile & Upload Card -->
            <div class="card">
                <div class="card-header">
                    <h2><i class="fas fa-play-circle"></i> Build & Deploy</h2>
                </div>
                <div class="card-body">
                    <div class="btn-group">
                        <asp:Button ID="btnCompile" runat="server" Text="Compile" CssClass="btn btn-primary" ClientIDMode="Static" 
                                   OnClick="btnCompile_Click" OnClientClick="resetAndStartProgress(); return true;" />
                        <asp:Button ID="btnUpload" runat="server" Text="Upload" CssClass="btn btn-success" ClientIDMode="Static" 
                                   OnClick="btnUpload_Click" OnClientClick="resetAndStartProgress(); return true;" />
                    </div>
                    
                    <!-- Progress bar -->
                    <div id="progressContainer" class="progress-container">
                        <div class="progress-header">
                            <div class="progress-label" id="progressText">Ready to compile/upload</div>
                            <div class="progress-percent" id="progressPercent">0%</div>
                        </div>
                        <div class="progress-bar">
                            <div id="progressBar" class="progress-value" style="width: 0%"></div>
                        </div>
                        <div id="debugProgress" class="debug-panel"></div>
                    </div>
                    
                    <!-- Statistics Panel with Chart -->
                    <div id="statsPanel" class="stats-panel">
                        <h3 class="section-title">Compilation Statistics</h3>
                        
                        <div class="tabs-row">
                            <button type="button" class="stat-tab active" data-tab="cardsTab" onclick="showStatTab('cardsTab')">
                                <i class="fas fa-th-large"></i> Cards
                            </button>
                            <button type="button" class="stat-tab" data-tab="chartTab" onclick="showStatTab('chartTab')">
                                <i class="fas fa-chart-bar"></i> Chart
                            </button>
                        </div>
                        
                        <div class="stat-tabs-content">
                            <!-- Cards View -->
                            <div id="cardsTab" class="active">
                                <div class="stats-grid">
                                    <div class="stat-card">
                                        <div class="stat-title">Sketch Size</div>
                                        <div id="statSketchSize" class="stat-value">--</div>
                                        <div class="stat-subtitle">Total compiled size</div>
                                    </div>
                                    <div class="stat-card">
                                        <div class="stat-title">Flash Usage</div>
                                        <div id="statFlashUsage" class="stat-value">--</div>
                                        <div class="stat-subtitle">Percentage of available flash</div>
                                    </div>
                                    <div class="stat-card">
                                        <div class="stat-title">RAM Usage</div>
                                        <div id="statRamUsage" class="stat-value">--</div>
                                        <div class="stat-subtitle">Global variables</div>
                                    </div>
                                    <div class="stat-card">
                                        <div class="stat-title">Compilation Time</div>
                                        <div id="statCompileTime" class="stat-value">--</div>
                                        <div class="stat-subtitle">Total build time</div>
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Chart View -->
                            <div id="chartTab">
                                <div class="chart-card">
                                    <canvas id="compilationStatsChart" height="240"></canvas>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="form-group" style="margin-top: 16px;">
                        <label for="txtOutput">Compilation Output:</label>
                        <asp:TextBox ID="txtOutput" runat="server" ClientIDMode="Static" TextMode="MultiLine" CssClass="terminal" ReadOnly="true" Rows="8" />
                        <div style="margin-top: 8px; display: flex; justify-content: space-between; align-items: center;">
                            <asp:Label ID="lblStatus" runat="server" Text="Status: Ready" CssClass="status-indicator pending" />
                            <button type="button" class="btn btn-sm" onclick="clearOutput()">Clear Output</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        
        <!-- Configuration Tab Content -->
        <div id="configTab" class="tab-content">
            <!-- Custom Partition Import Card -->
            <div class="card">
                <div class="card-header">
                    <h2><i class="fas fa-puzzle-piece"></i> Custom Partition Import</h2>
                </div>
                <div class="card-body">
                    <div class="info-box">
                        Upload a custom <strong>partitions.csv</strong> file to use with the "custom" partition scheme.
                        This is required for using the "custom" partition option in the dropdown.
                    </div>
                    
                    <div class="form-group">
                        <label>Import Partitions CSV:</label>
                        <div class="file-input-wrapper">
                            <asp:FileUpload ID="fuPartitionsCsv" runat="server" CssClass="file-input" accept=".csv" />
                            <span class="file-input-label">
                                <i class="fas fa-file-csv"></i> Choose CSV File
                            </span>
                        </div>
                        <asp:Button ID="btnImportPartitionsCsv" runat="server" Text="Import Partitions CSV" CssClass="btn btn-primary" OnClick="btnImportPartitionsCsv_Click" />
                        <div class="upload-result">
                            <asp:Label ID="lblCsvResult" runat="server" />
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- Boards.txt Import Card -->
            <div class="card">
                <div class="card-header">
                    <h2><i class="fas fa-file-alt"></i> Import from boards.txt</h2>
                </div>
                <div class="card-body">
                    <div class="info-box">
                        <strong>Import partition schemes from Arduino boards.txt file.</strong> When a boards.txt file is set,
                        only basic partitions matching the Arduino IDE will be shown in the dropdown menu.
                    </div>
                    
                    <div class="form-group">
                        <label for="txtBoardsTxtPath">boards.txt Path:</label>
                        <asp:TextBox ID="txtBoardsTxtPath" runat="server" placeholder="C:\Users\[User]\AppData\Local\Arduino15\packages\esp32\hardware\esp32\2.0.9\boards.txt" />
                    </div>
                    
                    <div class="form-group">
                        <label>Or upload boards.txt file:</label>
                        <div class="file-input-wrapper">
                            <asp:FileUpload ID="fuBoardsTxt" runat="server" CssClass="file-input" accept=".txt" />
                            <span class="file-input-label">
                                <i class="fas fa-file-alt"></i> Choose boards.txt
                            </span>
                        </div>
                        <asp:Button ID="btnImportBoardsTxt" runat="server" Text="Import from boards.txt" CssClass="btn btn-primary" OnClick="btnImportBoardsTxt_Click" />
                        <div class="upload-result">
                            <asp:Label ID="lblBoardsTxtResult" runat="server" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
        
        <div class="version-info">
            ESP32 Arduino Web Loader v1.5.0 | Last Updated: 2025-07-31 23:54:39 | User: Chamil1983
        </div>
    </div>
    
    <script type="text/javascript">
        // -------------------- CORE VARIABLES --------------------
        let compileActive = false;           // Flag to indicate active compilation
        let compileStartTime = 0;            // Track when compile started
        let progressUpdateTimer = null;      // Timer for progress updates
        let lastOutputLength = 0;            // Last length of output text
        let lastOutputText = "";             // Last output text content
        let statsChart = null;               // Chart.js reference
        let currentProgress = 0;             // Current progress percentage
        let autoProgressTimer = null;        // Timer for auto-progress
        let outputCheckInterval = 50;        // Check output every 50ms for responsiveness
        let lastUpdateTime = 0;              // Last time progress was updated

        // Progress tracking patterns - each represents a compilation phase
        const progressMarkers = [
            { pattern: "Executing task: arduino-cli", progress: 5, phase: "Initialize" },
            { pattern: "Using FQBN", progress: 10, phase: "Configure" },
            { pattern: "Loading", progress: 15, phase: "Loading" },
            { pattern: "Resolving", progress: 20, phase: "Resolving Dependencies" },
            { pattern: "Library", progress: 25, phase: "Loading Libraries" },
            { pattern: "Using library", progress: 30, phase: "Using Libraries" },
            { pattern: "Building in", progress: 40, phase: "Building" },
            { pattern: "Archiving", progress: 45, phase: "Archiving" },
            { pattern: "Compiling", progress: 50, phase: "Compiling" },
            { pattern: "Linking", progress: 75, phase: "Linking" },
            { pattern: "Creating bin", progress: 85, phase: "Creating binary" },
            { pattern: "Sketch uses", progress: 90, phase: "Finalizing" },
            { pattern: "Global variables use", progress: 95, phase: "Finalizing" }
        ];

        // -------------------- INITIALIZATION --------------------
        // Document loaded - initialize everything
        document.addEventListener('DOMContentLoaded', function () {
            debug("Document loaded");
            initializeChart();
            setupBoardOptionListeners();
        });

        // Set up event listeners for board option changes
        function setupBoardOptionListeners() {
            setTimeout(() => {
                debug("Setting up board option listeners");
                // Find all dropdowns in the board options section
                const optionSelects = document.querySelectorAll('[id^=ddlOption_]');

                for (const select of optionSelects) {
                    select.addEventListener('change', function () {
                        debug(`Board option changed: ${select.id} = ${select.value}`);
                        updateFQBNPreview();
                    });
                }
                debug(`Set up listeners for ${optionSelects.length} board options`);
            }, 500); // Small delay to ensure DOM is ready
        }

        // Update the FQBN preview based on selected options
        function updateFQBNPreview() {
            debug("Updating FQBN preview");

            // Get base board FQBN and partition
            const board = document.getElementById('ddlBoard').value;
            const partitionScheme = document.getElementById('ddlPartition').value;

            // Collect all option values from dropdowns
            const options = [];

            // Add partition scheme if not default
            if (partitionScheme && partitionScheme !== 'default') {
                options.push(`PartitionScheme=${partitionScheme}`);
            }

            // Get all board option dropdowns
            const optionDropdowns = document.querySelectorAll('[id^=ddlOption_]');

            // Add each non-default option
            for (const dropdown of optionDropdowns) {
                if (dropdown.value && dropdown.value !== 'default') {
                    const optionName = dropdown.id.replace('ddlOption_', '');
                    options.push(`${optionName}=${dropdown.value}`);
                }
            }

            // Find the preview element
            const fqbnPreview = document.getElementById('fqbnPreview');
            if (!fqbnPreview) {
                debug("FQBN preview element not found");
                return;
            }

            // Get the base FQBN for this board
            const baseFQBN = document.getElementById('hidBaseFQBN') ?
                document.getElementById('hidBaseFQBN').value :
                `esp32:esp32:${board}`;

            // Build the full FQBN
            let fullFQBN = options.length > 0 ?
                `${baseFQBN}:${options.join(',')}` :
                baseFQBN;

            // Update the preview
            fqbnPreview.textContent = fullFQBN;

            debug(`Updated FQBN: ${fullFQBN}`);

            // Update hidden field if it exists
            const hidFQBN = document.getElementById('hidFullFQBN');
            if (hidFQBN) {
                hidFQBN.value = fullFQBN;
            }
        }

        // -------------------- PROGRESS TRACKING --------------------
        // Reset all progress state and start tracking for a new operation
        function resetAndStartProgress() {
            debug("Resetting progress tracking state");

            // Reset all state variables
            compileActive = true;
            compileStartTime = Date.now();
            lastOutputLength = 0;
            lastOutputText = "";
            currentProgress = 0;
            lastUpdateTime = 0;

            // Reset UI elements
            setProgressUI(0, "Starting...");
            document.getElementById('debugProgress').innerHTML = "";
            document.getElementById('statsPanel').style.display = 'none';

            // Clear any existing timers
            if (progressUpdateTimer) clearInterval(progressUpdateTimer);
            if (autoProgressTimer) clearInterval(autoProgressTimer);

            // Start monitoring for output changes
            progressUpdateTimer = setInterval(checkForOutputChanges, outputCheckInterval);

            // Set up auto-progress for responsiveness
            autoProgressTimer = setInterval(autoAdvanceProgress, 300);

            debug("Progress tracking started");
            return true;
        }

        // Check for changes in the output text
        function checkForOutputChanges() {
            if (!compileActive) return;

            const outputElem = document.getElementById('txtOutput');
            if (!outputElem) return;

            const currentText = outputElem.value || "";
            const currentLength = currentText.length;

            // Check if output has changed
            if (currentLength !== lastOutputLength || currentText !== lastOutputText) {
                debug(`Output changed: ${lastOutputLength} → ${currentLength} chars`);

                // Store new values
                lastOutputLength = currentLength;
                lastOutputText = currentText;

                // Process the new output
                processOutput(currentText);

                // Update last change time
                lastUpdateTime = Date.now();
            }
        }

        // Process the output text to update progress
        function processOutput(outputText) {
            if (!outputText || !compileActive) return;

            // Check for completion markers first
            if (outputText.includes("Compiled OK") || outputText.includes("Upload OK")) {
                handleCompletedOperation(outputText, true);
                return;
            } else if (outputText.includes("Compile failed") || outputText.includes("Upload failed")) {
                handleCompletedOperation(outputText, false);
                return;
            }

            // Check for progress markers
            for (let marker of progressMarkers) {
                if (outputText.includes(marker.pattern) && marker.progress > currentProgress) {
                    // Found a marker that indicates progress beyond our current state
                    currentProgress = marker.progress;
                    setProgressUI(currentProgress, marker.phase);
                    debug(`Progress: ${marker.pattern} -> ${currentProgress}% (${marker.phase})`);
                }
            }
        }

        // Automatically advance progress slightly if no updates
        function autoAdvanceProgress() {
            if (!compileActive) return;

            // Don't advance past 95% automatically
            if (currentProgress >= 95) return;

            const timeSinceUpdate = Date.now() - lastUpdateTime;

            // If it's been more than 2 seconds since a real update, simulate a small advance
            if (timeSinceUpdate > 2000 && currentProgress < 90) {
                const increment = 0.3; // Small increment
                currentProgress += increment;
                setProgressUI(currentProgress, "Processing...");
            }
        }

        // Update the progress UI elements
        function setProgressUI(percent, message) {
            const progressBar = document.getElementById('progressBar');
            const progressText = document.getElementById('progressText');
            const progressPercent = document.getElementById('progressPercent');

            if (progressBar && progressText && progressPercent) {
                progressBar.style.width = `${Math.min(100, Math.round(percent))}%`;
                progressText.innerText = message;
                progressPercent.innerText = `${Math.min(100, Math.round(percent))}%`;
            }
        }

        // Handle a completed operation (success or failure)
        function handleCompletedOperation(outputText, success) {
            debug(`Operation ${success ? "succeeded" : "failed"}`);

            // Stop all monitoring
            compileActive = false;
            if (progressUpdateTimer) {
                clearInterval(progressUpdateTimer);
                progressUpdateTimer = null;
            }
            if (autoProgressTimer) {
                clearInterval(autoProgressTimer);
                autoProgressTimer = null;
            }

            // Update the UI to 100%
            setProgressUI(100, success ? "Complete!" : "Failed");

            // If successful, show the statistics
            if (success) {
                // Small delay to ensure output is fully processed
                setTimeout(() => {
                    extractAndShowStats(outputText);
                }, 500);
            }
        }

        // -------------------- STATISTICS HANDLING --------------------
        // Extract statistics from output and show the stats panel
        function extractAndShowStats(output) {
            debug("Extracting statistics from output");

            // Calculate compilation duration
            const compileDuration = ((Date.now() - compileStartTime) / 1000).toFixed(1);

            // Extract statistics using regular expressions
            const sketchSize = extractWithPattern(output, /Sketch uses (\d+,?\d*) bytes/, "--");
            const flashUsage = extractWithPattern(output, /\((\d+\.?\d*%)\) of program storage space/, "--");
            const ramUsage = extractWithPattern(output, /Global variables use (\d+,?\d*) bytes/, "--");

            debug(`Stats - Size: ${sketchSize}, Flash: ${flashUsage}, RAM: ${ramUsage}, Time: ${compileDuration}s`);

            // Update the statistics UI
            document.getElementById('statSketchSize').innerText = sketchSize + " bytes";
            document.getElementById('statFlashUsage').innerText = flashUsage;
            document.getElementById('statRamUsage').innerText = ramUsage;
            document.getElementById('statCompileTime').innerText = compileDuration + " sec";

            // Update the chart
            updateStatsChart(sketchSize, flashUsage, ramUsage);

            // Show the statistics panel with animation
            var statsPanel = document.getElementById('statsPanel');
            statsPanel.style.display = 'block';

            // Scroll to make the statistics visible
            setTimeout(function () {
                statsPanel.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }, 200);

            debug("Statistics panel displayed");
        }

        // Extract value using regex pattern
        function extractWithPattern(text, pattern, defaultValue) {
            const match = text.match(pattern);
            return match ? match[1] : defaultValue;
        }

        // Update the statistics chart
        function updateStatsChart(sketchSize, flashUsage, ramUsage) {
            if (!statsChart) {
                debug("Chart not initialized, creating new chart");
                initializeChart();
            }

            try {
                // Parse the values for the chart
                let sketchSizeKB = parseFloat((parseInt(sketchSize.replace(/,/g, '')) / 1024).toFixed(2));
                if (isNaN(sketchSizeKB)) sketchSizeKB = 0;

                let flashUsagePercent = parseFloat(flashUsage.replace('%', ''));
                if (isNaN(flashUsagePercent)) flashUsagePercent = 0;

                let ramUsageKB = parseFloat((parseInt(ramUsage.replace(/,/g, '').replace(' bytes', '')) / 1024).toFixed(2));
                if (isNaN(ramUsageKB)) ramUsageKB = 0;

                // Update the chart data
                statsChart.data.datasets[0].data = [sketchSizeKB, flashUsagePercent, ramUsageKB];
                statsChart.update();

                debug(`Chart updated: [${sketchSizeKB}KB, ${flashUsagePercent}%, ${ramUsageKB}KB]`);
            } catch (error) {
                debug(`Error updating chart: ${error.message}`);
            }
        }

        // Initialize the chart
        function initializeChart() {
            debug("Initializing chart");
            const ctx = document.getElementById('compilationStatsChart');
            if (!ctx) {
                debug("Chart canvas not found!");
                return;
            }

            statsChart = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: ['Sketch Size (KB)', 'Flash Usage (%)', 'RAM Usage (KB)'],
                    datasets: [{
                        label: 'Compilation Statistics',
                        data: [0, 0, 0],
                        backgroundColor: [
                            'rgba(54, 162, 235, 0.7)',  // Blue
                            'rgba(255, 99, 132, 0.7)',  // Red
                            'rgba(75, 192, 192, 0.7)'   // Green
                        ],
                        borderColor: [
                            'rgba(54, 162, 235, 1)',
                            'rgba(255, 99, 132, 1)',
                            'rgba(75, 192, 192, 1)'
                        ],
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    },
                    plugins: {
                        legend: {
                            display: false
                        }
                    }
                }
            });

            debug("Chart initialized");
        }

        // -------------------- UTILITY FUNCTIONS --------------------
        // Log debugging messages
        function debug(message) {
            const debugPanel = document.getElementById('debugProgress');
            if (debugPanel) {
                const timestamp = new Date().toLocaleTimeString();
                debugPanel.innerHTML += `<div>[${timestamp}] ${message}</div>`;
                debugPanel.scrollTop = debugPanel.scrollHeight;
            }
            console.log(`[DEBUG] ${message}`);
        }

        // Clear the output text
        function clearOutput() {
            const outputElem = document.getElementById('txtOutput');
            if (outputElem) {
                outputElem.value = "Output cleared.";
                lastOutputText = outputElem.value;
                lastOutputLength = lastOutputText.length;
            }
        }

        // Show the selected statistics tab
        function showStatTab(tabId) {
            // Hide all tabs
            const tabs = document.querySelectorAll('.stat-tabs-content > div');
            tabs.forEach(tab => tab.classList.remove('active'));

            // Deactivate all buttons
            const buttons = document.querySelectorAll('.stat-tab');
            buttons.forEach(button => button.classList.remove('active'));

            // Show selected tab
            document.getElementById(tabId).classList.add('active');

            // Activate button
            const button = document.querySelector(`.stat-tab[data-tab="${tabId}"]`);
            if (button) button.classList.add('active');

            // Resize chart if needed
            if (tabId === 'chartTab' && statsChart) {
                statsChart.resize();
            }
        }

        // Show the selected main tab
        function showTab(tabId) {
            // Hide all tabs
            const tabs = document.querySelectorAll('.tab-content');
            tabs.forEach(tab => tab.classList.remove('active'));

            // Deactivate all buttons
            const buttons = document.querySelectorAll('.tab-btn');
            buttons.forEach(button => button.classList.remove('active'));

            // Show selected tab
            document.getElementById(tabId).classList.add('active');

            // Activate button
            document.getElementById('btn-' + tabId).classList.add('active');
        }

        // Setup a backup polling for changes if MutationObserver doesn't work
        // This is especially important for ASP.NET TextBox control value changes
        setInterval(function () {
            if (compileActive) {
                checkForOutputChanges();
            }
        }, 100);
    </script>
    
    <!-- Hidden fields for board configuration -->
    <asp:HiddenField ID="hidBaseFQBN" runat="server" />
    <asp:HiddenField ID="hidFullFQBN" runat="server" />
    </form>
</body>
</html>