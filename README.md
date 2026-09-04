# Miniscuplter v0.9.9 — Launcher, Installation and Update Management

This preserved branch turns the source project into an installable application architecture. It is a historical milestone; current development is on `v1.0`.

## Added in v0.9.9

### Launcher

- native CPU/RAM/GPU/VRAM hardware probe independent of Python
- installed/missing AI model status
- explicit install/remove/update actions for managed models
- upstream Hugging Face/Git revision checks
- model-update warnings without automatic updates
- AI runtime repair
- application update checks and staged updater
- normal application start button

### Installation/storage

- self-contained .NET launcher and updater
- Inno Setup installer with user-selectable writable install location
- application, backend runtime and AIData stored under the selected installation root
- independent in-app locations for Projects, reusable Model/Parts Library and STL Exports
- compatibility migration/linking for the older `user://parts_library` location

### Release/update pipeline

- `build_release.ps1`
- Godot Windows export preset
- portable `Miniscuplter-win-x64.zip`
- staged application updater preserving managed/user data
- GitHub Release integration for update discovery

## Normal installed layout

```text
Miniscuplter/
├── Miniscuplter.Launcher.exe
├── Miniscuplter.Updater.exe
├── launcher.settings.json
├── App/
│   ├── Miniscuplter.exe
│   └── ai_backend/.venv/
└── AIData/
    ├── models/
    ├── tools/
    └── components.json
```

## Runtime requirement at this milestone

Python 3.10 x64 is still required to create the local AI virtual environment. The launcher/editor themselves do not require Python to open.

## Historical status

Frozen after code/packaging CI validation. `v1.0` performs the full release-candidate audit, transactional model updates, dependency hardening, updater/runtime preservation fixes, improved geometry scalability, extra tests and reproducible full Windows installer artifacts.
