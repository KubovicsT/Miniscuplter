from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> None:
    installer = (ROOT / "Scripts/ExtrasInstaller.cs").read_text(encoding="utf-8")
    density = (ROOT / "Scripts/Main.WorkspaceDensity.cs").read_text(encoding="utf-8")

    check("InstallWorkspaceDensity();" in installer, "workspace density installer missing")
    check(
        installer.index("InstallResourceTelemetry();") < installer.index("InstallWorkspaceDensity();"),
        "density cleanup must compose after the current MS-027 workspace surfaces",
    )
    check("2D Canvas Editing" in density and "Context Aware Image Editing" in density,
          "bounded instructional-copy targets missing")
    check("instruction.Visible = false" in density,
          "superseded instructional copy is not retired")
    check("TooltipText" in density and "V1023SetCompactTooltip" in density,
          "retired instructions must remain discoverable through hover help")
    check("_v1015CanvasHint.Text" in density,
          "empty 2D workspace hint was not compacted")
    check("ProjectStore" not in density and "V1020Generate3DAsync" not in density,
          "density layer must not acquire project or Stage-C generation authority")
    check("_v1015EditStatus.Visible" not in density and "_v1017ImageJobStatus.Visible" not in density,
          "important workflow/job status must remain visibly owned by existing surfaces")

    print("workspace density regression tests passed")


if __name__ == "__main__":
    main()
