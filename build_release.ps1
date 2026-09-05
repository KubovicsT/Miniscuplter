param(
    [string]$Configuration = "Release",
    [string]$GodotExe = $env:GODOT_EXE,
    [switch]$SkipGodotExport,
    [switch]$BuildInstaller
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'
$package = Join-Path $dist 'package'
$appDir = Join-Path $package 'App'

[xml]$launcherProject = Get-Content (Join-Path $root 'Launcher/Miniscuplter.Launcher.csproj') -Raw
$versionNode = $launcherProject.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1
$version = [string]$versionNode.Version
if ([string]::IsNullOrWhiteSpace($version)) { throw 'Launcher project does not declare a release version.' }

if (Test-Path $package) { Remove-Item $package -Recurse -Force }
New-Item $appDir -ItemType Directory -Force | Out-Null

Write-Host "Building Miniscuplter v$version release package..."
Write-Host 'Publishing self-contained launcher...'
dotnet publish (Join-Path $root 'Launcher/Miniscuplter.Launcher.csproj') -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o (Join-Path $dist 'launcher')
if ($LASTEXITCODE -ne 0) { throw 'Launcher publish failed.' }
Copy-Item (Join-Path $dist 'launcher/Miniscuplter.Launcher.exe') $package -Force

Write-Host 'Publishing self-contained staged updater...'
dotnet publish (Join-Path $root 'Updater/Miniscuplter.Updater.csproj') -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o (Join-Path $dist 'updater')
if ($LASTEXITCODE -ne 0) { throw 'Updater publish failed.' }
Copy-Item (Join-Path $dist 'updater/Miniscuplter.Updater.exe') $package -Force

if (-not $SkipGodotExport) {
    if ([string]::IsNullOrWhiteSpace($GodotExe) -or -not (Test-Path $GodotExe)) {
        throw 'Godot .NET executable not found. Pass -GodotExe or set GODOT_EXE to Godot 4.7.2 .NET.'
    }
    $godotCommand = $GodotExe
    if ($GodotExe -notmatch '_console\.exe$') {
        $consoleCandidate = [System.IO.Path]::Combine(
            [System.IO.Path]::GetDirectoryName($GodotExe),
            ([System.IO.Path]::GetFileNameWithoutExtension($GodotExe) + '_console.exe'))
        if (Test-Path $consoleCandidate) { $godotCommand = $consoleCandidate }
    }
    Write-Host "Using Godot exporter: $godotCommand"

    $solution = Join-Path $root 'Miniscuplter.sln'
    if (-not (Test-Path $solution)) { throw 'Miniscuplter.sln is required for Godot .NET export.' }
    Write-Host 'Building C# solution before Godot export...'
    dotnet build $solution -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Miniscuplter C# solution build failed.' }
    Write-Host 'Exporting Godot application...'
    & $godotCommand --headless --path $root --export-release 'Windows Desktop' (Join-Path $appDir 'Miniscuplter.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Godot Windows export failed.' }
    $exportedExe = Join-Path $appDir 'Miniscuplter.exe'
    if (-not (Test-Path $exportedExe) -or (Get-Item $exportedExe).Length -eq 0) { throw 'Godot reported success but did not produce a usable Miniscuplter.exe.' }
}

# Backend source and setup live with the exported app but model weights never ship in releases.
Copy-Item (Join-Path $root 'ai_backend') (Join-Path $appDir 'ai_backend') -Recurse -Force
Get-ChildItem (Join-Path $appDir 'ai_backend') -Directory -Filter '__pycache__' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
if (Test-Path (Join-Path $appDir 'ai_backend/.venv')) { Remove-Item (Join-Path $appDir 'ai_backend/.venv') -Recurse -Force }
Copy-Item (Join-Path $root 'setup_ai_backend.bat') $package -Force

# Existing installed settings are preserved by the updater. This relative fallback also makes
# the ZIP portable when run without the installer.
@'
{
  "InstallRoot": ".",
  "AppExecutable": "App\\Miniscuplter.exe",
  "DataRoot": "AIData",
  "CheckApplicationUpdates": true,
  "CheckModelUpdates": true,
  "ReleaseRepository": "KubovicsT/Miniscuplter"
}
'@ | Set-Content (Join-Path $package 'launcher.settings.json') -Encoding UTF8
New-Item (Join-Path $package 'AIData') -ItemType Directory -Force | Out-Null

@{
    schema = 1
    version = $version
    asset = 'Miniscuplter-win-x64.zip'
    architecture = 'win-x64'
    preserves = @('AIData','configured DataRoot','Runtime','Projects','PartsLibrary','Exports','UserData','App/ai_backend/.venv','runtime caches')
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $package 'release.json') -Encoding UTF8

$zip = Join-Path $dist 'Miniscuplter-win-x64.zip'
$shaFile = "$zip.sha256"
if (Test-Path $zip) { Remove-Item $zip -Force }
if (Test-Path $shaFile) { Remove-Item $shaFile -Force }
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  Miniscuplter-win-x64.zip" | Set-Content $shaFile -Encoding ASCII
Write-Host "Release ZIP: $zip"
Write-Host "Release SHA-256: $hash"

if ($BuildInstaller) {
    $candidates = @()
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $candidates += $cmd.Source }
    if (${env:ProgramFiles(x86)}) { $candidates += (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe') }
    if ($env:ProgramFiles) { $candidates += (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe') }
    if ($env:ChocolateyInstall) { $candidates += (Join-Path $env:ChocolateyInstall 'bin\ISCC.exe') }
    $iscc = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if (-not $iscc) { throw 'Inno Setup 6 is required to build the installer.' }
    Write-Host "Using Inno Setup compiler: $iscc"
    & $iscc (Join-Path $root 'installer/Miniscuplter.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
}
