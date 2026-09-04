#define MyAppName "Miniscuplter"
#define MyAppVersion "0.9.9"
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

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "airuntime"; Description: "Set up the local AI Python environment after installation (requires Python 3.10 x64)"; GroupDescription: "Local AI:"; Flags: checkedonce

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

[Code]
function JsonEscapePath(Value: string): string;
begin
  StringChangeEx(Value, '\', '\\', True);
  StringChangeEx(Value, '"', '\"', True);
  Result := Value;
end;

function InstallFolderWritable(Folder: string): Boolean;
var
  Probe: string;
begin
  Result := False;
  if not ForceDirectories(Folder) then
    exit;

  Probe := AddBackslash(Folder) + '.miniscuplter_write_test.tmp';
  if SaveStringToFile(Probe, 'write-test', False) then
  begin
    DeleteFile(Probe);
    Result := True;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Folder: string;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    Folder := ExpandConstant(WizardDirValue);
    if not InstallFolderWritable(Folder) then
    begin
      MsgBox(
        'Miniscuplter must be installed in a folder your normal Windows account can write to. ' +
        'The launcher downloads and removes AI models there and applies application updates without administrator access.' + #13#10 + #13#10 +
        'Choose another folder, such as the default per-user location or a writable folder on another drive.',
        mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectDir then
    WizardForm.SelectDirLabel.Caption :=
      'Choose where Miniscuplter, its local AI runtime, and downloaded AI models will live. ' +
      'This folder must stay writable by your Windows account so the launcher can manage models and application updates.';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Settings: string;
begin
  if CurStep = ssPostInstall then
  begin
    Settings :=
      '{' + #13#10 +
      '  "InstallRoot": "' + JsonEscapePath(ExpandConstant('{app}')) + '",' + #13#10 +
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
  if not InstallFolderWritable(ExpandConstant('{app}')) then
  begin
    Result := 'The selected Miniscuplter installation folder is not writable by the current Windows account. Choose a different installation folder.';
    exit;
  end;

  if DirExists(ExpandConstant('{app}\AIData')) then
    Log('Existing AIData folder detected; downloaded AI models will be preserved.');
end;
