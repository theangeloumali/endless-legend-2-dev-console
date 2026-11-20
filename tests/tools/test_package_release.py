import json
import sys
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools"))

import package_release  # noqa: E402


def test_package_writes_nexus_zip_and_workshop_folder(tmp_path):
    dll = tmp_path / "DevConsole.dll"
    dll.write_bytes(b"stand-in plugin")
    dist = tmp_path / "dist"

    package_release.package(dll, dist)

    with zipfile.ZipFile(dist / "DevConsole.zip") as archive:
        assert sorted(archive.namelist()) == ["DevConsole.dll", "INSTALL.txt", "LICENSE"]
        assert archive.read("DevConsole.dll") == b"stand-in plugin"
    workshop = dist / "workshop" / "GeloDGreatDevConsole"
    assert (workshop / "DevConsole.dll").read_bytes() == b"stand-in plugin"
    info = json.loads((workshop / "mod-info.json").read_text(encoding="utf-8"))
    assert info["Version"] == package_release.version()
    assert info["DisplayName"] == "GeloDGreat Dev Console: Endless Forge"
    assert "Hazardous BepInEx Mod Loader" in info["Description"]


def test_package_replaces_a_stale_workshop_folder(tmp_path):
    dll = tmp_path / "DevConsole.dll"
    dll.write_bytes(b"new")
    stale = tmp_path / "dist" / "workshop" / "GeloDGreatDevConsole" / "Old.dll"
    stale.parent.mkdir(parents=True)
    stale.write_bytes(b"old")

    package_release.package(dll, tmp_path / "dist")

    assert not stale.exists()
