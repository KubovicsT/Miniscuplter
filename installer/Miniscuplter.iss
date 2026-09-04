#define MyAppName "Miniscuplter"
#define MyAppVersion "0.9.9"
#define MyAppPublisher "Miniscuplter"
#define MyAppExeName "Miniscuplter.Launcher.exe"

[Setup]
AppId={{F92F4BC8-1F51-4FA9-BD43-A6D0BA0C0999}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Miniscuplter
DefaultGroupName=Miniscuplter
DisableProgramGroupPage=yes
OutputDir=..\dist\installer
OutputBaseFilename=Miniscuplter-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesEnvironment=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "airuntime"; Description: "Set up the local AI Python environment after installation"; GroupDescription: "Local AI:"; Flags: checkedonce

[Dirs]
Name: "{app}\AIData"

[Files]
Source: "..\dist\package\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Miniscuplter"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Miniscuplter"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\setup_ai_backend.bat"; Parameters: "/quiet"; WorkingDir: "{app}"; StatusMsg: "Preparing local AI runtime..."; Flags: runhidden waituntilterminated skipifsilent; Tasks: airuntime
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Miniscuplter"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Program files are removed by the normal uninstaller. AIData is intentionally NOT listed here:
; downloaded models may be tens of gigabytes and are user-managed from the launcher.

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  Settings: string;
begin
  if CurStep = ssPostInstall then
  begin
    Settings :=
      '{' + #13#10 +
      '  "InstallRoot": "' + StringChangeEx(ExpandConstant('{app}'), '\', '\\', True) + '",' + #13#10 +
      '  "AppExecutable": "App\\Miniscuplter.exe",' + #13#10 +
      '  "DataRoot": "AIData",' + #13#10 +
      '  "CheckApplicationUpdates": true,' + #13#10 +
      '  "CheckModelUpdates": true,' + #13#10 +
      '  "ReleaseRepository": "KubovicsT/Miniscuplter"' + #13#10 +
      '}' + #13#10;
    SaveStringToFile(ExpandConstant('{app}\launcher.settings.json'), Settings, False);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if DirExists(ExpandConstant('{app}\AIData')) then
    Log('Existing AIData folder detected and will be preserved.');
end;
