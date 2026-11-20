r"""Package a built DevConsole.dll for release.

    python tools/package_release.py

Writes, from plugin/DevConsole/bin/Release/net472/DevConsole.dll (build it first with tools/build_plugin.py):
  dist/DevConsole.zip                     Nexus / GitHub release: DLL, INSTALL.txt, LICENSE
  dist/workshop/GeloDGreatDevConsole/     the folder the Mod Authoring Toolkit uploads to the Workshop; the
                                          community BepInEx loaders load the DLL from a subscriber's Workshop folder
"""
from __future__ import annotations

import json
import re
import shutil
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / "plugin" / "DevConsole" / "bin" / "Release" / "net472" / "DevConsole.dll"
RELEASE = ROOT / "tools" / "release"
WORKSHOP_FOLDER = "GeloDGreatDevConsole"


def version() -> str:
    source = (ROOT / "plugin" / "DevConsole" / "Plugin.cs").read_text(encoding="utf-8")
    match = re.search(r'Version = "([^"]+)"', source)
    if not match:
        sys.exit("could not read Plugin.Version")
    return match.group(1)


def package(dll: Path, dist: Path) -> None:
    dist.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(dist / "DevConsole.zip", "w", zipfile.ZIP_DEFLATED) as archive:
        archive.write(dll, "DevConsole.dll")
        archive.write(RELEASE / "INSTALL.txt", "INSTALL.txt")
        archive.write(ROOT / "LICENSE", "LICENSE")

    workshop = dist / "workshop" / WORKSHOP_FOLDER
    if workshop.exists():
        shutil.rmtree(workshop)  # a rebuilt folder must not upload files from an older release
    workshop.mkdir(parents=True)
    info = json.loads((RELEASE / "mod-info.json").read_text(encoding="utf-8"))
    info["Version"] = version()
    info["Description"] = (RELEASE / "workshop-description.txt").read_text(encoding="utf-8").strip()
    (workshop / "mod-info.json").write_text(json.dumps(info, indent=4, ensure_ascii=False) + "\n", encoding="utf-8")
    shutil.copy2(dll, workshop / "DevConsole.dll")
    preview = RELEASE / "mod-preview-image.jpg"
    if preview.exists():
        shutil.copy2(preview, workshop / preview.name)


def main() -> int:
    if not DLL.exists():
        sys.exit(f"{DLL} not found: run tools/build_plugin.py first")
    package(DLL, ROOT / "dist")
    print(f"wrote {ROOT / 'dist' / 'DevConsole.zip'} and {ROOT / 'dist' / 'workshop' / WORKSHOP_FOLDER}")
    if not (RELEASE / "mod-preview-image.jpg").exists():
        print("note: add tools/release/mod-preview-image.jpg before uploading to the Workshop")
    return 0


if __name__ == "__main__":
    sys.exit(main())
