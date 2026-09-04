# Miniscuplter v1.0 Build and Release

## Preferred build: GitHub Actions artifact

The `v1.0` workflow produces a real Windows x64 installer artifact after the normal compile/test/package jobs pass.

The full-release job:

1. checks out the exact commit
2. installs .NET 8 and Inno Setup
3. downloads the official Godot 4.7.2 .NET Windows x64 editor
4. downloads the matching Godot 4.7.2 mono export templates
5. verifies both downloads against pinned SHA-256 hashes
6. installs the templates into the runner's version-specific Godot template directory
7. runs the real Godot Windows release export
8. publishes the self-contained launcher and updater
9. copies the Python backend source without model weights or `.venv`
10. builds `Miniscuplter-win-x64.zip`
11. compiles `Miniscuplter-Setup-1.0.0.exe`
12. validates all required outputs are present and non-empty
13. uploads the installer and portable ZIP as a GitHub Actions artifact

The artifact name includes the exact commit SHA and is retained for 30 days.

## Pinned Godot release

v1.0 targets Godot **4.7.2 stable .NET**. CI pins the official editor and mono export-template archives and their SHA-256 values so a future upstream release cannot silently change the build environment.

## Local release build

Requirements:

- Windows x64
- .NET 8 SDK
- Godot 4.7.2 .NET x64
- matching Godot 4.7.2 mono export templates
- Inno Setup 6 for installer compilation

Run from the repository root:

```powershell
./build_release.ps1 -GodotExe "C:\path\to\Godot_v4.7.2-stable_mono_win64.exe" -BuildInstaller
```

Outputs:

```text
dist/
├── package/
│   ├── Miniscuplter.Launcher.exe
│   ├── Miniscuplter.Updater.exe
│   ├── App/Miniscuplter.exe
│   └── App/ai_backend/...
├── Miniscuplter-win-x64.zip
└── installer/Miniscuplter-Setup-1.0.0.exe
```

`-SkipGodotExport` exists for fast package-layout CI and development checks, but a package created with that option is **not** a complete distributable editor build because `App/Miniscuplter.exe` is intentionally absent.

## CI validation layers

The v1.0 workflow has four relevant jobs:

- **dotnet** — builds the Godot C# project, launcher and updater
- **python-syntax** — compiles backend Python, runs dependency-free core logic tests and release audit
- **packaging** — verifies self-contained launcher/updater/package/installer mechanics without the expensive Godot export
- **full-windows-release** — performs the actual Godot export and uploads the installable artifact

The full release job depends on all three earlier jobs succeeding.

## Public release publishing

GitHub Actions artifacts are intended for testing. Application self-update checks GitHub **Releases**, so a public/updateable version must eventually publish `Miniscuplter-win-x64.zip` as a release asset with that exact name. The launcher only applies an update after explicit user approval and verifies the GitHub-provided SHA-256 digest when available.

## Signing

v1.0 does not yet require a code-signing certificate in CI. Before broad public commercial distribution, Windows code signing is recommended to reduce SmartScreen friction and establish publisher identity.
