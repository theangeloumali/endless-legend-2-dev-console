"""Install the Dev Console mod into the game's Modding directory and verify the load.

    python tools/install_mod.py [--source mod/DevConsole] [--park-bepinex | --restore-bepinex] [--verify]
    python tools/install_mod.py --uninstall     # remove the data mod (the plugin replaces its every-turn dialog)

The game reads `<GameDirectory>/Modding/mod-configuration.json` (a JSON array of mod folder names) at startup
and logs `[Modification] Modding is enabled with N active modifications.` to Player.log; UI/data errors land in
the newest `Temporary Files/Diagnostics*.html`.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
from pathlib import Path

LOCAL = Path(os.environ["LOCALAPPDATA"])
GAME_DATA_DIR = LOCAL / "Amplitude Studios" / "Endless Legend 2"
PLAYER_LOG = LOCAL.parent / "LocalLow" / "Amplitude Studios" / "Endless Legend 2" / "Player.log"
GAME_DIR = Path(r"D:\SteamLibrary\steamapps\common\ENDLESS Legend 2")
MOD_NAME = "DevConsole"
LOG_ERROR_PATTERNS = ("LoadModificationException", "could not find source element", "could not resolve type",
                      "Failed to load modification", "Failed to create modification", "Invalid configuration file")


def set_active(modding_dir: Path, enabled: bool) -> list[str]:
    """Add or drop MOD_NAME in mod-configuration.json (the JSON array the game reads at startup)."""
    config = modding_dir / "mod-configuration.json"
    active = json.loads(config.read_text(encoding="utf-8-sig")) if config.exists() else []
    active = [name for name in active if name != MOD_NAME] + ([MOD_NAME] if enabled else [])
    config.write_text(json.dumps(active, indent=4) + "\n", encoding="utf-8")
    return active


def install(source: Path, modding_dir: Path) -> None:
    target = modding_dir / MOD_NAME
    if target.exists():
        shutil.rmtree(target)
    shutil.copytree(source, target)
    active = set_active(modding_dir, enabled=True)
    files = sorted(p.relative_to(target).as_posix() for p in target.rglob("*.json"))
    print(f"installed {len(files)} files -> {target}\nmod-configuration.json = {active}")


def uninstall(modding_dir: Path) -> None:
    target = modding_dir / MOD_NAME
    if target.exists():
        shutil.rmtree(target)
    print(f"removed {target}\nmod-configuration.json = {set_active(modding_dir, enabled=False)}")


def move_plugins(game_dir: Path, park: bool, keep: tuple[str, ...] = ()) -> None:
    src, dst = game_dir / "BepInEx" / "plugins", game_dir / "BepInEx" / "plugins.disabled"
    if not park:
        src, dst = dst, src
    dst.mkdir(exist_ok=True)
    moved = [p.name for p in src.glob("*.dll") if p.name not in keep and shutil.move(str(p), dst / p.name)]
    print(f"{'parked' if park else 'restored'} BepInEx plugins: {moved or 'none'}")


def bepinex_state(game_dir: Path) -> str:
    active = [p.name for p in (game_dir / "BepInEx" / "plugins").glob("*.dll")]
    parked = [p.name for p in (game_dir / "BepInEx" / "plugins.disabled").glob("*.dll")]
    return f"BepInEx plugins active={active or 'none'} parked={parked or 'none'}"


def verify(modding_dir: Path) -> bool:
    ok = True
    log = PLAYER_LOG.read_text(encoding="utf-8", errors="replace") if PLAYER_LOG.exists() else ""
    hits = [line for line in log.splitlines() if "[Modification]" in line]
    print("Player.log:", *(hits or ["(no [Modification] line - game not started since install?)"]), sep="\n  ")
    errors = [line for line in log.splitlines() if any(p in line for p in LOG_ERROR_PATTERNS)]
    if errors:
        ok = False
        print("Player.log ERRORS:", *errors[:20], sep="\n  ")
    if not any("Modding is enabled with" in h for h in hits):
        ok = False

    diagnostics = sorted((GAME_DATA_DIR / "Temporary Files").glob("Diagnostics*.html"), key=lambda p: p.stat().st_mtime)
    if diagnostics:
        newest = diagnostics[-1]
        text = re.sub(r"<[^>]+>", "", newest.read_text(encoding="utf-8", errors="replace"))
        elements = [p.stem for p in (modding_dir / MOD_NAME).rglob("*.json") if p.name != "mod-info.json"]
        suspicious = [line.strip() for line in text.splitlines()
                      if "Could not find" in line or any(e in line for e in elements)]
        print(f"Diagnostics ({newest.name}):", *(suspicious[:30] or ["clean - no 'Could not find' and no mod element mentioned"]), sep="\n  ")
        ok = ok and not suspicious
    else:
        print("Diagnostics: no Diagnostics*.html found")
    return ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", type=Path, default=Path("mod") / MOD_NAME)
    parser.add_argument("--modding-dir", type=Path, default=GAME_DATA_DIR / "Modding")
    parser.add_argument("--game-dir", type=Path, default=GAME_DIR)
    parser.add_argument("--park-bepinex", action="store_true")
    parser.add_argument("--restore-bepinex", action="store_true")
    parser.add_argument("--verify", action="store_true", help="only check Player.log + Diagnostics, no install")
    parser.add_argument("--uninstall", action="store_true", help="remove the mod folder and its configuration entry")
    args = parser.parse_args()

    if args.uninstall:
        uninstall(args.modding_dir)
        return 0

    if args.park_bepinex:
        move_plugins(args.game_dir, park=True)
    if args.restore_bepinex:
        move_plugins(args.game_dir, park=False)
    if not args.verify:
        args.modding_dir.mkdir(parents=True, exist_ok=True)
        install(args.source, args.modding_dir)
    print(bepinex_state(args.game_dir))
    if args.verify:
        return 0 if verify(args.modding_dir) else 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
