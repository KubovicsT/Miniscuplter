#define MyAppName "Miniscuplter"
#define MyAppVersion "1.0.5"
#define MyAppPublisher "Miniscuplter"
#define MyAppExeName "Miniscuplter.Launcher.exe"

[Setup]
AppId={{F92F4BC8-1F51-4FA9-BD43-A6D0BA0C0999}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Miniscuplter
DefaultGroupName=Miniscuplter
DisableProgramGroupPage=yes
OutputDir=..\dist\installer
OutputBaseFilename=Miniscuplter-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesEnvironment=no
CloseApplications=yes
RestartApplications=no

[Files]
Source: "..\dist\package\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Miniscuplter"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Miniscuplter"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Miniscuplter"; Flags: nowait postinstall skipifsilent
