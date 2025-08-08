<%@ Page Language="VB" AutoEventWireup="false" CodeBehind="Default.aspx.vb" Inherits="ArduinoWeb.Default" ValidateRequest="false" %>
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
        
        .btn-info {
            background-color: var(--info);
            color: white;
        }
        
        .btn-info:hover {
            background-color: #0098B7;
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
            position: relative;
            transition: all 0.3s ease;
            border: 1px solid var(--gray-200);
            border-radius: var(--border-radius);
            overflow: hidden;
        }
        
        .board-option-card:hover {
            transform: translateY(-3px);
            box-shadow: var(--shadow);
        }
        
        .board-option-header {
            padding: 12px 16px;
            background-color: var(--gray-100);
            border-bottom: 1px solid var(--gray-200);
            display: flex;
            flex-direction: column;
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
            position: relative;
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
        
        /* Notification system */
        .notifications-container {
            position: fixed;
            top: 20px;
            right: 20px;
            width: 300px;
            z-index: 9999;
        }
        
        .notification {
            padding: 15px;
            margin-bottom: 10px;
            border-radius: var(--border-radius);
            box-shadow: var(--shadow-lg);
            display: flex;
            align-items: flex-start;
            animation: slideIn 0.3s ease-out;
        }
        
        .notification.success {
            background-color: var(--success-light);
            border-left: 4px solid var(--success);
        }
        
        .notification.error {
            background-color: var(--danger-light);
            border-left: 4px solid var(--danger);
        }
        
        .notification.info {
            background-color: var(--info-light);
            border-left: 4px solid var(--info);
        }
        
        .notification-icon {
            margin-right: 10px;
            font-size: 18px;
        }
        
        .notification-content {
            flex: 1;
        }
        
        .notification-title {
            font-weight: 600;
            margin-bottom: 4px;
        }
        
        .notification-message {
            font-size: 13px;
        }
        
        .notification-close {
            font-size: 16px;
            cursor: pointer;
            margin-left: 10px;
        }
        
        @keyframes slideIn {
            from { transform: translateX(100%); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
        
        @keyframes slideOut {
            from { transform: translateX(0); opacity: 1; }
            to { transform: translateX(100%); opacity: 0; }
        }
        
        /* Enhanced board options styling */
        .board-option-card {
            position: relative;
            transition: all 0.3s ease;
            border: 1px solid var(--gray-200);
            border-radius: var(--border-radius);
            overflow: hidden;
        }

        .board-option-card:hover {
            transform: translateY(-3px);
            box-shadow: var(--shadow);
        }

        .board-option-header {
            padding: 12px 16px;
            background-color: var(--gray-100);
            border-bottom: 1px solid var(--gray-200);
            display: flex;
            flex-direction: column;
        }

        .board-option-content {
            padding: 12px 16px;
            position: relative;
        }

        .board-option-dropdown {
            border-radius: var(--border-radius);
            padding: 8px 10px;
            font-size: 14px;
            width: 100%;
            transition: all 0.3s ease;
        }

        .board-option-dropdown option {
            padding: 8px;
        }

        /* Default indicator styling */
        .default-indicator {
            position: absolute;
            right: 16px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--success);
            font-size: 16px;
        }

        /* Arduino IDE default option styling */
        .arduino-default {
            font-weight: bold;
            color: #0052cc;
            background-color: rgba(76, 154, 255, 0.1);
        }

        /* Board option card with Arduino defaults */
        .board-option-card.has-arduino-default {
            border-left: 3px solid #0052cc;
        }

        /* Board option card with custom values */
        .board-option-card.custom-value {
            border-left: 3px solid var(--warning);
        }

        /* Animation for board option changes */
        @keyframes highlight-change {
            0% { background-color: rgba(255, 204, 0, 0.3); }
            100% { background-color: transparent; }
        }

        .highlight-change {
            animation: highlight-change 1s ease;
        }
    </style>

    <style type="text/css">
    .fqbn-preview-value, .actual-fqbn-value {
        word-break: break-all;
        max-width: 100%;
        overflow-wrap: break-word;
        white-space: normal;
    }

        /* Binary export button styling */
    .btn-export {
        background-color: #6f42c1;
        color: white;
    }
    
    .btn-export:hover {
        background-color: #5a32a3;
    }
    
    /* Animation for successful export */
    @keyframes highlight-export {
        0% { background-color: rgba(111, 66, 193, 0.3); }
        100% { background-color: transparent; }
    }
    
    .export-file-item {
        padding: 4px 8px;
        border-radius: 3px;
        margin-bottom: 4px;
        font-family: var(--font-mono);
        background-color: var(--gray-100);
    }

     /* Binary upload button styling */
    .btn-warning {
        background-color: #FF9800;
        color: white;
    }
    
    .btn-warning:hover {
        background-color: #F57C00;
    }
    
    /* Binary upload container styling */
    #binaryUploadContainer {
        transition: all 0.3s ease;
    }
    
    #binaryUploadContainer:hover {
        border-color: #FF9800;
        box-shadow: 0 0 5px rgba(255, 152, 0, 0.3);
    }
    
    /* Binary upload verification icon styling */
    .verification-success {
        color: var(--success);
        font-weight: bold;
    }
    
    .verification-error {
        color: var(--danger);
        font-weight: bold;
    }
    
    /* Animation for verification progress */
    @keyframes verification-pulse {
        0% { opacity: 0.6; }
        50% { opacity: 1; }
        100% { opacity: 0.6; }
    }
    
    .verification-progress {
        animation: verification-pulse 1.5s infinite;
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
                            <label for="ddlBoard">Processor Name:</label>
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
    <!-- Export Binary button -->
    <asp:Button ID="btnExportBinary" runat="server" Text="Export Binary" CssClass="btn btn-info" ClientIDMode="Static" 
               OnClick="btnExportBinary_Click" OnClientClick="resetAndStartProgress(); return true;" />
    <!-- Add the new Upload Binary button -->
    <asp:Button ID="btnUploadBinary" runat="server" Text="Upload Binary" CssClass="btn btn-warning" ClientIDMode="Static" 
               OnClick="btnUploadBinary_Click" OnClientClick="resetAndStartProgress(); return true;" />
    <button type="button" id="btnShowStats" class="btn btn-info" onclick="showCurrentStats(); return false;">
    <i class="fas fa-chart-bar"></i> Show Stats
    </button>
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

<div id="binaryUploadContainer" class="form-group" style="margin-top: 16px; padding: 15px; border: 1px dashed #ccc; border-radius: 4px; background-color: #f8f9fa;">
    <label for="fuBinaryZip">Binary File for Upload:</label>
    <div class="file-input-wrapper" style="margin-bottom: 10px;">
        <asp:FileUpload ID="fuBinaryZip" runat="server" CssClass="file-input" accept=".bin,.hex,.elf,.zip" ValidateRequestMode="Disabled" />
        <span class="file-input-label">
            <i class="fas fa-file-archive"></i> Choose Binary File or ZIP
        </span>
    </div>
    <div class="form-row">
        <div class="form-col">
            <asp:CheckBox ID="chkVerifyUpload" runat="server" Checked="true" Text="Verify upload" />
        </div>
        <div class="form-col">
            <asp:TextBox ID="txtBinaryPath" runat="server" ReadOnly="true" placeholder="Selected binary file path will appear here" CssClass="form-control" />
        </div>
    </div>
    <div class="tip">
        <i class="fas fa-info-circle"></i> Upload a .bin, .hex, .elf, or .zip file containing binary files to flash directly to your device.
    </div>
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
            ESP32 Arduino Web Loader v1.5.0 | Last Updated: 2025-08-02 11:39:42 | User: Chamil1983
        </div>
    </div>

    <!-- Notifications container -->
    <div id="notificationsContainer" class="notifications-container"></div>
    
    <!-- Hidden fields for board configuration -->
    <asp:HiddenField ID="hidBaseFQBN" runat="server" />
    <asp:HiddenField ID="hidFullFQBN" runat="server" />
    <!-- New hidden field to store current compile output -->
    <asp:HiddenField ID="hidCurrentCompileOutput" runat="server" />
    </form>

    <!-- Main JavaScript -->
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
        let setupAttemptsRemaining = 5;      // Number of attempts for setting up board options
        let lastCompileOutput = "";          // Store the last successful compilation output

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
        // Document loaded - initialize everything with retry
        document.addEventListener('DOMContentLoaded', function () {
            debug("Document loaded - Enhanced script with retries");

            // Initialize charts immediately
            initializeStatsAndCharts();

            // Setup with retry mechanism
            setupWithRetry();
        });

        // Setup with retry for better reliability
        function setupWithRetry() {
            if (setupAttemptsRemaining <= 0) {
                debug("Gave up setting up board options after multiple attempts");
                return;
            }

            setupAttemptsRemaining--;

            try {
                setupBoardOptionListeners();
                setupCompileTracking();

                // If this succeeds, we're done
                debug("Setup completed successfully");
            } catch (e) {
                debug(`Setup attempt failed: ${e.message}, will retry in 500ms`);
                setTimeout(setupWithRetry, 500);
            }
        }

        // Set up event listeners for board option changes - with robust error handling
        function setupBoardOptionListeners() {
            try {
                debug("Setting up board option listeners");

                // First find all dropdowns directly
                const optionSelects = document.querySelectorAll('[id^=ddlOption_]');
                debug(`Found ${optionSelects.length} board option dropdowns directly`);

                // Set up listeners for each dropdown
                for (const select of optionSelects) {
                    select.addEventListener('change', function () {
                        debug(`Board option changed: ${select.id} = ${select.value}`);
                        updateFQBNPreview();
                        highlightChange(select);
                    });
                }

                // If none found, try again with a different approach
                if (optionSelects.length === 0) {
                    debug("No options found initially, trying alternative approach");
                    // Try finding by class name
                    const classSelects = document.querySelectorAll('.board-option-dropdown');
                    debug(`Found ${classSelects.length} board option dropdowns by class`);

                    for (const select of classSelects) {
                        select.addEventListener('change', function () {
                            debug(`Board option changed by class: ${select.id} = ${select.value}`);
                            updateFQBNPreview();
                            highlightChange(select);
                        });
                    }

                    // If still none, we'll set up a mutation observer to catch dynamically added options
                    if (classSelects.length === 0) {
                        setupMutationObserver();
                    }
                }

                // Also try to update FQBN preview now
                setTimeout(updateFQBNPreview, 200);

            } catch (e) {
                debug(`Error setting up board option listeners: ${e.message}`);
                // We'll retry via setupWithRetry
            }
        }

        // Setup mutation observer to detect dynamically added board options
        function setupMutationObserver() {
            debug("Setting up mutation observer for dynamic board options");

            const targetNode = document.getElementById('form1');
            if (!targetNode) {
                debug("No form1 element found for mutation observer");
                return;
            }

            const observer = new MutationObserver(function (mutations) {
                for (const mutation of mutations) {
                    if (mutation.type === 'childList') {
                        const addedNodes = mutation.addedNodes;
                        for (let i = 0; i < addedNodes.length; i++) {
                            const node = addedNodes[i];

                            // Check if added node is or contains our board options
                            if (node.querySelectorAll) {
                                const options = node.querySelectorAll('[id^=ddlOption_], .board-option-dropdown');
                                if (options.length > 0) {
                                    debug(`Mutation observer found ${options.length} new board options`);

                                    // Set up listeners for these new options
                                    for (const select of options) {
                                        select.addEventListener('change', function () {
                                            debug(`Dynamic board option changed: ${select.id} = ${select.value}`);
                                            updateFQBNPreview();
                                            highlightChange(select);
                                        });
                                    }

                                    // Try to update FQBN preview
                                    setTimeout(updateFQBNPreview, 200);

                                    // Style the options
                                    setTimeout(styleBoardOptions, 300);
                                }
                            }
                        }
                    }
                }
            });

            observer.observe(targetNode, { childList: true, subtree: true });
            debug("Mutation observer setup complete");
        }

        // Update the FQBN preview based on selected options - with enhanced error handling
        function updateFQBNPreview() {
            debug("Updating FQBN preview");

            try {
                // Get base board FQBN and partition
                const board = document.getElementById('ddlBoard')?.value || "esp32";
                const partitionScheme = document.getElementById('ddlPartition')?.value || "";

                // Collect all option values from dropdowns
                const options = [];

                // Add partition scheme if not default
                if (partitionScheme && partitionScheme !== 'default') {
                    options.push(`PartitionScheme=${partitionScheme}`);
                }

                // Get all board option dropdowns - try multiple approaches
                let optionDropdowns = document.querySelectorAll('[id^=ddlOption_]');
                if (!optionDropdowns || optionDropdowns.length === 0) {
                    optionDropdowns = document.querySelectorAll('.board-option-dropdown');
                }

                debug(`Found ${optionDropdowns.length} board option dropdowns for FQBN update`);

                // Add each non-default option
                for (const dropdown of optionDropdowns) {
                    if (dropdown.value && dropdown.value !== 'default') {
                        const optionName = dropdown.id.replace('ddlOption_', '');
                        options.push(`${optionName}=${dropdown.value}`);
                    }
                }

                // Find the preview element using multiple approaches
                let fqbnPreview = document.getElementById('fqbnPreview');
                if (!fqbnPreview) {
                    // Try to find by class
                    fqbnPreview = document.querySelector('.fqbn-preview-value');
                    debug("FQBN preview element found by class: " + (fqbnPreview ? "Yes" : "No"));
                }

                // Get the base FQBN for this board - with fallback
                const baseFQBN = document.getElementById('hidBaseFQBN')?.value ||
                    `esp32:esp32:${board}`;

                // Build the full FQBN
                let fullFQBN = options.length > 0 ?
                    `${baseFQBN}:${options.join(',')}` :
                    baseFQBN;

                debug(`Generated FQBN: ${fullFQBN}`);

                // Update the preview if found
                if (fqbnPreview) {
                    fqbnPreview.textContent = fullFQBN;
                    debug("Updated FQBN preview element");
                } else {
                    debug("FQBN preview element not found for update");
                }

                // Update hidden field if it exists
                const hidFQBN = document.getElementById('hidFullFQBN');
                if (hidFQBN) {
                    hidFQBN.value = fullFQBN;
                    debug("Updated hidden FQBN field");
                }

                return true;
            } catch (e) {
                debug(`Error updating FQBN preview: ${e.message}`);
                return false;
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

            // Clear debug panel
            const debugPanel = document.getElementById('debugProgress');
            if (debugPanel) {
                debugPanel.innerHTML = "";
            }

            // Hide stats panel
            const statsPanel = document.getElementById('statsPanel');
            if (statsPanel) {
                statsPanel.style.display = 'none';
            }

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
                // Save the current output for statistics processing
                lastCompileOutput = outputText;

                // Store in hidden field for server-side access
                const hidOutput = document.getElementById('hidCurrentCompileOutput');
                if (hidOutput) {
                    hidOutput.value = outputText;
                }

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

        // Handle a completed operation (success or failure) with retry for statistics
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

            // If successful, show the statistics with retry
            if (success && outputText.includes("Compiled OK")) {
                // Initial attempt with delay
                setTimeout(() => {
                    debug("First attempt to process compilation output");
                    if (!processCompilationOutput(outputText)) {
                        // If first attempt fails, try again
                        debug("First attempt failed, will retry statistics processing");
                        setTimeout(() => {
                            processCompilationOutput(outputText, true);
                        }, 1000);
                    }
                }, 500);
            }
        }

        // Set up compile button tracking
        function setupCompileTracking() {
            try {
                // Set up compile button listener
                const compileBtn = document.getElementById('btnCompile');
                if (compileBtn) {
                    debug("Setting up compile button tracking");

                    compileBtn.addEventListener('click', function () {
                        window.compileStartTime = Date.now();
                        debug('Compilation started at: ' + new Date(window.compileStartTime).toLocaleTimeString());

                        // Reset UI state for new compilation
                        resetCompilationState();
                    });
                } else {
                    debug("Compile button not found");
                }

                // Similarly for upload button
                const uploadBtn = document.getElementById('btnUpload');
                if (uploadBtn) {
                    debug("Setting up upload button tracking");

                    uploadBtn.addEventListener('click', function () {
                        window.compileStartTime = Date.now();
                        debug('Upload started at: ' + new Date(window.compileStartTime).toLocaleTimeString());

                        // Reset UI state for new operation
                        resetCompilationState();
                    });
                }
            } catch (e) {
                debug(`Error setting up compile tracking: ${e.message}`);
            }
        }

        // Reset the compilation state
        function resetCompilationState() {
            try {
                debug("Resetting compilation state");

                // Hide stats panel
                const statsPanel = document.getElementById('statsPanel');
                if (statsPanel) {
                    statsPanel.style.display = 'none';
                    debug("Stats panel hidden");
                }

                // Reset stats values
                if (document.getElementById('statSketchSize')) document.getElementById('statSketchSize').innerText = '--';
                if (document.getElementById('statFlashUsage')) document.getElementById('statFlashUsage').innerText = '--';
                if (document.getElementById('statRamUsage')) document.getElementById('statRamUsage').innerText = '--';
                if (document.getElementById('statCompileTime')) document.getElementById('statCompileTime').innerText = '--';

                // Reset chart if it exists
                if (window.statsChart) {
                    window.statsChart.data.datasets[0].data = [0, 0, 0];
                    window.statsChart.update();
                    debug("Chart reset");
                } else {
                    debug("Chart not initialized, will initialize now");
                    initializeStatsAndCharts();
                }
            } catch (e) {
                debug(`Error resetting compilation state: ${e.message}`);
            }
        }

        // -------------------- STATISTICS HANDLING --------------------
        // Initialize statistics and charts
        function initializeStatsAndCharts() {
            try {
                debug("Initializing stats and charts");

                // Create an empty chart to be populated later
                const ctx = document.getElementById('compilationStatsChart');
                if (ctx) {
                    // Check if Chart.js is loaded
                    if (typeof Chart === 'undefined') {
                        debug("ERROR: Chart.js not loaded! Charts will not work.");
                        return false;
                    }

                    try {
                        window.statsChart = new Chart(ctx, {
                            type: 'bar',
                            data: {
                                labels: ['Sketch Size (KB)', 'Flash Usage (%)', 'RAM Usage (KB)'],
                                datasets: [{
                                    label: 'Compilation Statistics',
                                    data: [0, 0, 0],
                                    backgroundColor: [
                                        'rgba(54, 162, 235, 0.7)',
                                        'rgba(255, 99, 132, 0.7)',
                                        'rgba(75, 192, 192, 0.7)'
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
                                    },
                                    title: {
                                        display: true,
                                        text: 'ESP32 Compilation Statistics'
                                    }
                                }
                            }
                        });
                        debug("Stats chart initialized");
                    } catch (chartError) {
                        debug(`Error creating chart: ${chartError.message}`);
                    }
                } else {
                    debug("Chart canvas element not found");
                }

                return true;
            } catch (e) {
                debug(`Error initializing stats and charts: ${e.message}`);
                return false;
            }
        }

        // Show statistics panel with current compilation data, not from session storage
        function showCurrentStats() {
            debug("Show Stats button clicked - using current compilation output");

            try {
                // Get the output text directly from the textbox
                const outputElem = document.getElementById('txtOutput');
                if (!outputElem || !outputElem.value) {
                    debug("No compilation output found in text area");
                    showNotification('No Output Available', 'No compilation output found. Please compile your sketch first.', 'info');
                    return false;
                }

                // Use the current output from the textarea, not from session storage
                const outputText = outputElem.value;
                debug(`Processing current compilation output: ${outputText.length} characters`);

                // Process the compilation output directly
                return processCompilationOutput(outputText, true);
            } catch (e) {
                debug(`Error showing current stats: ${e.message}`);
                showNotification('Error', 'Could not process compilation statistics.', 'error');
                return false;
            }
        }

        // Process compilation output to extract statistics
        function processCompilationOutput(outputText, isManualTrigger = false) {
            try {
                debug(`Processing compilation output for statistics: ${outputText.length} chars, manual trigger: ${isManualTrigger}`);

                if (!outputText || outputText.trim() === "") {
                    debug("Empty output text, can't process statistics");
                    if (isManualTrigger) {
                        showNotification('No Data', 'No compilation data found. Please compile your sketch first.', 'info');
                    }
                    return false;
                }

                // Make sure we have compile information in the output
                if (!outputText.includes("Sketch uses") && !outputText.includes("bytes")) {
                    debug("Output doesn't contain sketch size information");
                    if (isManualTrigger) {
                        showNotification('Incomplete Data', 'The output does not contain compilation statistics.', 'info');
                    }
                    return false;
                }

                // Define regex patterns for extracting statistics
                const sketchSizeRegex = /Sketch uses ([0-9,]+) bytes/;
                const flashUsageRegex = /\(([0-9.]+)%\)/;
                const ramUsageRegex = /Global variables use ([0-9,]+) bytes/;

                // Extract values using regex
                const sketchSizeMatch = sketchSizeRegex.exec(outputText);
                const flashUsageMatch = flashUsageRegex.exec(outputText);
                const ramUsageMatch = ramUsageRegex.exec(outputText);

                debug("Stats matches: " +
                    (sketchSizeMatch ? sketchSizeMatch[1] : "not found") + ", " +
                    (flashUsageMatch ? flashUsageMatch[1] : "not found") + ", " +
                    (ramUsageMatch ? ramUsageMatch[1] : "not found"));

                // Get the values or use placeholders
                const sketchSize = sketchSizeMatch ? sketchSizeMatch[1] + " bytes" : "N/A";
                const flashUsage = flashUsageMatch ? flashUsageMatch[1] + "%" : "N/A";
                const ramUsage = ramUsageMatch ? ramUsageMatch[1] + " bytes" : "N/A";

                // Calculate compilation time
                const compileDuration = ((Date.now() - (window.compileStartTime || Date.now())) / 1000).toFixed(1);
                const compileTime = compileDuration + " sec";

                // Update the stats UI
                const sketchSizeElem = document.getElementById('statSketchSize');
                const flashUsageElem = document.getElementById('statFlashUsage');
                const ramUsageElem = document.getElementById('statRamUsage');
                const compileTimeElem = document.getElementById('statCompileTime');

                if (sketchSizeElem) sketchSizeElem.innerText = sketchSize;
                if (flashUsageElem) flashUsageElem.innerText = flashUsage;
                if (ramUsageElem) ramUsageElem.innerText = ramUsage;
                if (compileTimeElem) compileTimeElem.innerText = compileTime;

                // Parse numeric values for the chart
                let sketchSizeKB = 0;
                let flashUsagePercent = 0;
                let ramUsageKB = 0;

                try {
                    if (sketchSizeMatch) {
                        sketchSizeKB = parseFloat(sketchSizeMatch[1].replace(/,/g, '')) / 1024;
                    }
                    if (flashUsageMatch) {
                        flashUsagePercent = parseFloat(flashUsageMatch[1]);
                    }
                    if (ramUsageMatch) {
                        ramUsageKB = parseFloat(ramUsageMatch[1].replace(/,/g, '')) / 1024;
                    }

                    // Update the chart with the new values
                    if (window.statsChart) {
                        window.statsChart.data.datasets[0].data = [sketchSizeKB, flashUsagePercent, ramUsageKB];
                        window.statsChart.update();
                        debug("Chart updated with new values");
                    } else {
                        debug("Chart not available, trying to initialize");
                        initializeStatsAndCharts();
                    }

                    // SAVE STATS TO SESSION STORAGE
                    const statsData = {
                        sketchSize: sketchSize,
                        flashUsage: flashUsage,
                        ramUsage: ramUsage,
                        compileTime: compileTime,
                        sketchSizeKB: sketchSizeKB,
                        flashUsagePercent: flashUsagePercent,
                        ramUsageKB: ramUsageKB,
                        timestamp: new Date().toISOString(),
                        outputText: outputText // Store the full output for reference
                    };

                    try {
                        sessionStorage.setItem('compilationStats', JSON.stringify(statsData));
                        debug("Saved compilation stats to session storage");
                    } catch (storageError) {
                        debug(`Error saving stats to session storage: ${storageError.message}`);
                    }

                } catch (e) {
                    debug(`Error updating chart: ${e.message}`);
                }

                // Show the statistics panel with animation
                const statsPanel = document.getElementById('statsPanel');
                if (statsPanel) {
                    statsPanel.style.display = 'block';
                    debug("Statistics panel displayed");

                    // Scroll to the stats panel with smooth animation
                    setTimeout(function () {
                        statsPanel.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }, 300);

                    if (isManualTrigger) {
                        showNotification('Statistics Loaded', 'Showing current compilation statistics.', 'info');
                    }
                } else {
                    debug("ERROR: Statistics panel element not found!");
                }

                return true;

            } catch (e) {
                debug(`Error processing compilation output: ${e.message}`);
                return false;
            }
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
            try {
                // Hide all tabs
                const tabs = document.querySelectorAll('.stat-tabs-content > div');
                tabs.forEach(tab => tab.classList.remove('active'));

                // Deactivate all buttons
                const buttons = document.querySelectorAll('.stat-tab');
                buttons.forEach(button => button.classList.remove('active'));

                // Show selected tab
                const selectedTab = document.getElementById(tabId);
                if (selectedTab) {
                    selectedTab.classList.add('active');
                }

                // Activate button
                const button = document.querySelector(`.stat-tab[data-tab="${tabId}"]`);
                if (button) button.classList.add('active');

                // Resize chart if needed
                if (tabId === 'chartTab' && window.statsChart) {
                    try {
                        window.statsChart.resize();
                        debug("Chart resized for display");
                    } catch (e) {
                        debug(`Error resizing chart: ${e.message}`);
                    }
                }
            } catch (e) {
                debug(`Error switching statistic tabs: ${e.message}`);
            }
        }

        // Show the selected main tab
        function showTab(tabId) {
            try {
                // Hide all tabs
                const tabs = document.querySelectorAll('.tab-content');
                tabs.forEach(tab => tab.classList.remove('active'));

                // Deactivate all buttons
                const buttons = document.querySelectorAll('.tab-btn');
                buttons.forEach(button => button.classList.remove('active'));

                // Show selected tab
                const selectedTab = document.getElementById(tabId);
                if (selectedTab) {
                    selectedTab.classList.add('active');
                }

                // Activate button
                const button = document.getElementById('btn-' + tabId);
                if (button) {
                    button.classList.add('active');
                }
            } catch (e) {
                debug(`Error switching main tabs: ${e.message}`);
            }
        }

        // Show notification
        function showNotification(title, message, type = 'info') {
            try {
                const container = document.getElementById('notificationsContainer');
                if (!container) {
                    debug("Notification container not found");
                    return;
                }

                const notification = document.createElement('div');
                notification.className = `notification ${type}`;

                let iconClass = 'fa-info-circle';
                if (type === 'success') iconClass = 'fa-check-circle';
                if (type === 'error') iconClass = 'fa-exclamation-circle';

                notification.innerHTML = `
                    <div class="notification-icon">
                        <i class="fas ${iconClass}"></i>
                    </div>
                    <div class="notification-content">
                        <div class="notification-title">${title}</div>
                        <div class="notification-message">${message}</div>
                    </div>
                    <div class="notification-close" onclick="this.parentElement.remove()">
                        <i class="fas fa-times"></i>
                    </div>
                `;

                container.appendChild(notification);
                debug(`Showed ${type} notification: ${title}`);

                // Auto-remove after 5 seconds
                setTimeout(() => {
                    notification.style.animation = 'slideOut 0.3s ease-out forwards';
                    setTimeout(() => {
                        if (notification.parentNode) {
                            notification.parentNode.removeChild(notification);
                        }
                    }, 300);
                }, 5000);
            } catch (e) {
                debug(`Error showing notification: ${e.message}`);
            }
        }

        // Update the version info in the UI
        function updateVersionInfo() {
            try {
                const versionElement = document.querySelector('.version-info');
                if (versionElement) {
                    versionElement.innerHTML = 'ESP32 Arduino Web Loader v1.5.0 | Last Updated: 2025-08-02 11:43:58 | User: Chamil1983';
                }
            } catch (e) {
                debug(`Error updating version info: ${e.message}`);
            }
        }

        // Enhanced board options styling
        function styleBoardOptions() {
            // Get all option dropdowns
            const dropdowns = document.querySelectorAll('[id^="ddlOption_"]');

            dropdowns.forEach(function (dropdown) {
                // Get parent card
                const optionCard = dropdown.closest('.board-option-card');
                if (!optionCard) return;

                // Check if dropdown has data attribute for default value
                const defaultVal = dropdown.getAttribute('data-default-value');
                if (!defaultVal) return;

                // Get actual selected value
                const selectedVal = dropdown.value;

                // Find all options and style them
                Array.from(dropdown.options).forEach(function (option) {
                    // Style Arduino IDE default
                    if (option.value === defaultVal) {
                        option.className = 'arduino-default';
                        option.innerHTML = option.text + ' ★';
                    }

                    // Add special styling for custom values (non-default values)
                    if (option.value !== 'default' && option.value !== defaultVal) {
                        option.className = 'custom-value';
                    }
                });

                // Style the dropdown based on selection
                if (selectedVal === defaultVal) {
                    // Using Arduino IDE default
                    dropdown.style.borderColor = '#0052cc';
                    dropdown.style.boxShadow = '0 0 0 1px rgba(0, 82, 204, 0.3)';
                    optionCard.classList.add('has-arduino-default');

                    // Add indicator icon
                    if (!optionCard.querySelector('.default-indicator')) {
                        const indicator = document.createElement('div');
                        indicator.className = 'default-indicator';
                        indicator.innerHTML = '<i class="fas fa-check-circle" title="Arduino IDE default value"></i>';
                        optionCard.querySelector('.board-option-content').appendChild(indicator);
                    }
                } else if (selectedVal !== 'default') {
                    // Using custom value
                    optionCard.classList.add('custom-value');
                    dropdown.style.borderColor = '#ff991f';
                    dropdown.style.boxShadow = '0 0 0 1px rgba(255, 153, 31, 0.3)';
                }
            });

            console.log("Board option styling completed");
        }

        // Highlight changes when a board option is modified
        function highlightChange(element) {
            const card = element.closest('.board-option-card');
            if (card) {
                card.classList.remove('highlight-change');
                // Force reflow
                void card.offsetWidth;
                card.classList.add('highlight-change');
            }
            styleBoardOptions();
        }

        // Setup a backup polling for changes if MutationObserver doesn't work
        // This is especially important for ASP.NET TextBox control value changes
        setInterval(function () {
            if (compileActive) {
                checkForOutputChanges();
            }
        }, 100);

        // Call updateVersionInfo once on page load to ensure date is current
        updateVersionInfo();

        // Set up periodic check for DOM readiness and board options
        setInterval(function () {
            // If there are board options but no listeners, set them up
            const options = document.querySelectorAll('[id^=ddlOption_], .board-option-dropdown');
            if (options.length > 0) {
                const hasListener = options[0].getAttribute('data-has-listener') === 'true';

                if (!hasListener) {
                    debug(`Found ${options.length} board options without listeners, setting them up`);
                    for (const option of options) {
                        option.addEventListener('change', function () {
                            debug(`Board option changed: ${option.id} = ${option.value}`);
                            updateFQBNPreview();
                            highlightChange(option);
                        });
                        option.setAttribute('data-has-listener', 'true');
                    }

                    // Update FQBN preview now that we have options
                    updateFQBNPreview();

                    // Style board options
                    styleBoardOptions();
                }
            }
        }, 2000);
    </script>
    
    <!-- Board Options Script -->
    <script type="text/javascript">
        /**
         * Enhanced Board Options Handler
         * Provides better visualization and interaction with ESP32 board settings
         */

        // Style the board option dropdowns with visual indicators for defaults
        function styleBoardOptions() {
            setTimeout(function () {
                // Get all option dropdowns
                var dropdowns = document.querySelectorAll('[id^="ddlOption_"]');

                dropdowns.forEach(function (dropdown) {
                    // Get parent card
                    var optionCard = dropdown.closest('.board-option-card');
                    if (!optionCard) return;

                    // Check if dropdown has data attribute for default value
                    var defaultVal = dropdown.getAttribute('data-default-value');
                    if (!defaultVal) return;

                    // Get actual selected value
                    var selectedVal = dropdown.value;

                    // Find all options and style them
                    Array.from(dropdown.options).forEach(function (option) {
                        // Style Arduino IDE default
                        if (option.value === defaultVal) {
                            option.className = 'arduino-default';
                            option.innerHTML = option.text + ' ★';
                        }

                        // Add special styling for custom values (non-default values)
                        if (option.value !== 'default' && option.value !== defaultVal) {
                            option.className = 'custom-value';
                        }
                    });

                    // Style the dropdown based on selection
                    if (selectedVal === defaultVal) {
                        // Using Arduino IDE default
                        dropdown.style.borderColor = '#0052cc';
                        dropdown.style.boxShadow = '0 0 0 1px rgba(0, 82, 204, 0.3)';
                        optionCard.classList.add('has-arduino-default');

                        // Add indicator icon
                        if (!optionCard.querySelector('.default-indicator')) {
                            var indicator = document.createElement('div');
                            indicator.className = 'default-indicator';
                            indicator.innerHTML = '<i class="fas fa-check-circle" title="Arduino IDE default value"></i>';
                            optionCard.querySelector('.board-option-content').appendChild(indicator);
                        }
                    } else if (selectedVal !== 'default') {
                        // Using custom value
                        optionCard.classList.add('custom-value');
                        dropdown.style.borderColor = '#ff991f';
                        dropdown.style.boxShadow = '0 0 0 1px rgba(255, 153, 31, 0.3)';
                    }
                });

                console.log("Board option styling completed");
            }, 500);
        }

        // Set up event listeners for board option changes
        function setupBoardOptionListeners() {
            try {
                var optionSelects = document.querySelectorAll('[id^="ddlOption_"]');
                console.log(`Found ${optionSelects.length} board option dropdowns directly`);

                // Set up listeners for each dropdown
                for (const select of optionSelects) {
                    select.addEventListener('change', function () {
                        console.log(`Board option changed: ${select.id} = ${select.value}`);
                        updateFQBNPreview();
                        highlightChange(select);
                    });
                }

                // If no options found, try setting up a mutation observer
                if (optionSelects.length === 0) {
                    setupMutationObserver();
                }

                // Update FQBN preview initially
                setTimeout(updateFQBNPreview, 200);
            } catch (e) {
                console.error(`Error setting up board option listeners: ${e.message}`);
            }
        }

        // Set up mutation observer to detect dynamically added board options
        function setupMutationObserver() {
            console.log("Setting up mutation observer for dynamic board options");

            const targetNode = document.getElementById('form1');
            if (!targetNode) {
                console.log("No form1 element found for mutation observer");
                return;
            }

            const observer = new MutationObserver(function (mutations) {
                for (const mutation of mutations) {
                    if (mutation.type === 'childList') {
                        const addedNodes = mutation.addedNodes;
                        for (let i = 0; i < addedNodes.length; i++) {
                            const node = addedNodes[i];

                            // Check if added node is or contains our board options
                            if (node.querySelectorAll) {
                                const options = node.querySelectorAll('[id^=ddlOption_], .board-option-dropdown');
                                if (options.length > 0) {
                                    console.log(`Mutation observer found ${options.length} new board options`);

                                    // Set up listeners for these new options
                                    for (const select of options) {
                                        select.addEventListener('change', function () {
                                            console.log(`Dynamic board option changed: ${select.id} = ${select.value}`);
                                            updateFQBNPreview();
                                            highlightChange(select);
                                        });
                                    }

                                    // Update FQBN preview
                                    setTimeout(updateFQBNPreview, 200);

                                    // Style the options
                                    setTimeout(styleBoardOptions, 300);
                                }
                            }
                        }
                    }
                }
            });

            observer.observe(targetNode, { childList: true, subtree: true });
            console.log("Mutation observer setup complete");
        }

        // Wait for document to be ready
        document.addEventListener('DOMContentLoaded', function () {
            // Setup board option handling
            setupBoardOptionListeners();

            // Style board options initially
            styleBoardOptions();

            // Add listener to board dropdown to update options when board changes
            const boardSelect = document.getElementById('ddlBoard');
            if (boardSelect) {
                boardSelect.addEventListener('change', function () {
                    // Board options will be repopulated via postback
                    // Apply styling after short delay to allow for page update
                    setTimeout(styleBoardOptions, 1000);
                });
            }
        });

        // Periodic check for any dynamically added elements
        setInterval(function () {
            const options = document.querySelectorAll('[id^=ddlOption_]');
            const fqbnPreview = document.getElementById('fqbnPreview');

            if (options.length > 0 && !document.querySelector('.styled-options-marker')) {
                console.log('Found unstyled options, applying styling');
                styleBoardOptions();

                // Add marker to avoid repeated styling
                const marker = document.createElement('div');
                marker.className = 'styled-options-marker';
                marker.style.display = 'none';
                document.body.appendChild(marker);
            }

            if (fqbnPreview && !fqbnPreview.innerText && options.length > 0) {
                console.log('FQBN preview empty but options exist, updating FQBN');
                updateFQBNPreview();
            }
        }, 2000);
    </script>

    <script type="text/javascript">
        // Binary upload verification handling
        function startVerification() {
            var verifyStatusElem = document.getElementById('verificationStatus');
            if (verifyStatusElem) {
                verifyStatusElem.innerHTML = '<i class="fas fa-sync fa-spin"></i> Verifying...';
                verifyStatusElem.className = 'verification-progress';
            }
        }

        function completeVerification(success) {
            var verifyStatusElem = document.getElementById('verificationStatus');
            if (verifyStatusElem) {
                if (success) {
                    verifyStatusElem.innerHTML = '<i class="fas fa-check-circle"></i> Verified';
                    verifyStatusElem.className = 'verification-success';
                } else {
                    verifyStatusElem.innerHTML = '<i class="fas fa-exclamation-triangle"></i> Verification Failed';
                    verifyStatusElem.className = 'verification-error';
                }
            }
        }

        // Show binary upload UI when button is clicked
        function showBinaryUploadUI() {
            var container = document.getElementById('binaryUploadContainer');
            if (container) {
                container.style.display = 'block';
                container.scrollIntoView({ behavior: 'smooth' });
            }
        }
    </script>
</body>
</html>
