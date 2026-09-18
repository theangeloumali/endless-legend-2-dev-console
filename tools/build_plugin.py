r"""Build the BepInEx plugin and install it into the game.

    python tools/build_plugin.py [--no-install] [--game-dir <path>]

Uses the per-user .NET SDK (%USERPROFILE%\.dotnet) if `dotnet` is not on PATH. Installing parks any other plugin
(the Nexus Resource Manager patches the same Gain methods and would double the multipliers).
"""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "plugin" / "DevConsole" / "DevConsole.csproj"
GAME_DIR = Path(r"D:\SteamLibrary\steamapps\common\ENDLESS Legend 2")


def dotnet() -> str:
    on_path = shutil.which("dotnet")
    if on_path and "dotnet-sdk" not in on_path:
        try:
            if subprocess.run([on_path, "--list-sdks"], capture_output=True, text=True).stdout.strip():
                return on_path
        except OSError:
            pass
    user = Path(os.environ["USERPROFILE"]) / ".dotnet" / "dotnet.exe"
    if not user.exists():
        sys.exit("no .NET SDK found: run the dotnet-install.ps1 step from the plan (B0)")
    return str(user)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--no-install", action="store_true")
    parser.add_argument("--game-dir", type=Path, default=GAME_DIR)
    args = parser.parse_args()

    build = subprocess.run([dotnet(), "build", str(PROJECT), "-c", "Release", "-nologo", f"-p:GameDir={args.game_dir}"],
                           capture_output=True, text=True)
    print(build.stdout[-3000:], build.stderr[-2000:])
    if build.returncode != 0:
        return build.returncode
    dll = PROJECT.parent / "bin" / "Release" / "net472" / "DevConsole.dll"
    if args.no_install:
        print(f"built {dll}")
        return 0

    plugins = args.game_dir / "BepInEx" / "plugins"
    parked = args.game_dir / "BepInEx" / "plugins.disabled"
    parked.mkdir(exist_ok=True)
    for other in plugins.glob("*.dll"):
        if other.name != dll.name:
            shutil.move(str(other), parked / other.name)
            print(f"parked {other.name}")
    shutil.copy2(dll, plugins / dll.name)
    print(f"installed {plugins / dll.name}; restart the game and check BepInEx/LogOutput.log for 'Dev Console'")
    return 0


if __name__ == "__main__":
    sys.exit(main())
