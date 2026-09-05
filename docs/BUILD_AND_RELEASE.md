# Miniscuplter v1.0.6 Build and Release

## Release outputs

A full Windows x64 release produces:

```text
Miniscuplter-Setup-1.0.6.exe
Miniscuplter-win-x64.zip
Miniscuplter-win-x64.zip.sha256
```

The portable ZIP also contains `release.json` with the package version, architecture and expected asset identity. The self-updater requires that manifest to match the GitHub Release being installed.

## Full release CI

For `v1.x` version branches, the full-release job runs only after the normal compile/test/package jobs pass.

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
10. writes package `release.json`
11. builds `Miniscuplter-win-x64.zip`
12. writes `Miniscuplter-win-x64.zip.sha256`
13. compiles the versioned Inno Setup installer
14. validates all required outputs are present and non-empty
15. smoke-installs the generated installer
16. uploads the installer, ZIP and SHA sidecar as the run's release artifact

After those checks pass on a `v1.x` version branch, the publish job creates the corresponding GitHub Release **only if that version has not already been published**. Published release versions are treated as immutable; changes after publication should move to a new version branch rather than silently replacing the package behind an existing version number.

## Why GitHub Releases are required

GitHub Actions artifacts are intended for CI/testing and expire. Installed launchers also should not need a GitHub account or API token merely to update.

The launcher therefore discovers updates from public GitHub **Releases**. CI publishes the exact asset names used by the updater:

```text
Miniscuplter-win-x64.zip
Miniscuplter-win-x64.zip.sha256
```

The launcher prefers GitHub's release-asset SHA-256 digest and falls back to the published SHA sidecar. If neither is available, automatic update is disabled rather than accepting an unverifiable ZIP.

## Self-update safety model

On launcher startup, update checks are enabled by default. If a newer stable semantic-versioned release exists, the launcher offers it to the user; code is never replaced without user approval.

The download path is resumable:

- update ZIPs are cached under `<DataRoot>/update-cache`
- interrupted downloads retain a `.partial` file
- retries use HTTP range requests when the release server supports them
- the exact release asset byte size is enforced
- the completed file must match SHA-256 before the updater starts

The staged updater independently re-checks the SHA-256 and `release.json` version, extracts the new package outside the active install tree, validates required files, backs up managed application files, and only then replaces them.

Persistent data is preserved. This includes the configured `DataRoot` (normally `AIData`), model weights, interrupted model stages, update cache, `Runtime`, projects, parts library, exports, user data and launcher settings. Nested `App/ai_backend/.venv`, `.runtime-cache`, and legacy backend `data` are parked outside the destructive update area with same-volume directory moves so multi-gigabyte runtimes are not needlessly copied. If application replacement fails, the old managed tree is restored and the parked runtime is returned before exit.

## Pinned Godot release

v1.0.6 targets Godot **4.7.2 stable .NET**. CI pins the official editor and mono export-template archives and their SHA-256 values so a future upstream release cannot silently change the build environment.

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
│   ├── release.json
│   ├── App/Miniscuplter.exe
│   └── App/ai_backend/...
├── Miniscuplter-win-x64.zip
├── Miniscuplter-win-x64.zip.sha256
└── installer/Miniscuplter-Setup-1.0.6.exe
```

`-SkipGodotExport` exists for fast package-layout CI and development checks, but a package created with that option is **not** a complete distributable editor build because `App/Miniscuplter.exe` is intentionally absent.

## CI validation layers

The v1.x workflow has five relevant layers:

- **dotnet** — builds the Godot C# project, launcher and updater
- **python-syntax** — compiles backend Python, resolves runtime dependencies, runs core logic tests and the release audit
- **packaging** — verifies self-contained launcher/updater/package/installer mechanics without the expensive Godot export
- **full-windows-release** — performs the real Godot export and smoke-installs the generated installer
- **publish-release** — publishes the verified immutable GitHub Release assets when that version has not already been published

## Signing

v1.0.6 does not yet require a code-signing certificate in CI. Before broad public commercial distribution, Windows code signing is recommended to reduce SmartScreen friction and establish publisher identity.
