; =====================================================================
; OmniForge Utility Suite - Production Inno Setup Script
; =====================================================================

#define MyAppName "OmniForge Utility Suite"
#define MyAppVersion "0.1.0-alpha"
#define MyAppPublisher "OmniForge Contributors"
#define MyAppURL "https://github.com/OmniForge/OmniForge"
#define MyAppExeName "OmniForge.App.exe"

[Setup]
; Unique application GUID for clean upgrade / uninstall identification
AppId={{8B264E2A-98C1-4D05-87B7-6577E4515E80}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Destination installation folder
DefaultDirName={autopf}\OmniForge
DefaultGroupName=OmniForge
DisableProgramGroupPage=yes

; Permissions: Allow both per-user and per-machine installation
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

; Architecture: 64-bit Windows requirement for WinUI 3
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Output configuration
OutputDir=..\artifacts\installer
OutputBaseFilename=OmniForge-Setup-v{#MyAppVersion}

; High-DPI Multi-Layer Application Icon
SetupIconFile=..\src\OmniForge.App\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression and Packaging
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Safe Application Closing during Upgrade
CloseApplications=yes
CloseApplicationsFilter=*.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Package all published self-contained application binaries and assets
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\OmniForge\logs"
Type: filesandordirs; Name: "{localappdata}\OmniForge\temp"

