"""Static checks of a generated mod against the game's export (no game launch needed).

    python tools/validate_mod.py <export-dir> [--mod mod/DevConsole]

Rules: asset type exists in the export · SourceElementName exists, same type, not obsolete · every
serializableElementName resolves (export or mod) · Odin node stream round-trips · every RawValue < 2^31 ·
events have <= 4 choices whose titles equal the dialog's LocalizedChoices · each element name is unique.
"""
from __future__ import annotations

import argparse
import copy
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from odin import Asset, Node, Value, top, walk  # noqa: E402

INT32_MAX = 2_147_483_647
HEADER = re.compile(r'"AssetTypeName":\s*"([^"]+)"')


def export_index(export_dir: Path) -> dict[str, list[dict]]:
    """name -> [{type, obsolete}, ...]; names are scoped per database, so an asset and its UIMapper share one.
    Cached next to the export because 31k files take ~20 s to scan."""
    cache = export_dir.parent / "export-index.json"
    if cache.exists():
        return json.loads(cache.read_text(encoding="utf-8"))
    index: dict[str, list[dict]] = {}
    for path in export_dir.rglob("*.json"):
        head = path.read_text(encoding="utf-8-sig", errors="replace")
        match = HEADER.search(head[:600])
        if not match:
            continue
        obsolete = bool(re.search(r'"IsObsolete":\s*true', head))
        index.setdefault(path.stem, []).append({"type": match.group(1), "obsolete": obsolete})
    cache.write_text(json.dumps(index), encoding="utf-8")
    return index


def references(raw: dict, nodes) -> set[str]:
    refs: set[str] = set()

    def plain(value) -> None:
        if isinstance(value, dict):
            for key, inner in value.items():
                if key == "serializableElementName" and inner:
                    refs.add(inner)
                plain(inner)
        elif isinstance(value, list):
            for inner in value:
                plain(inner)

    plain({k: v for k, v in raw.items() if k != "AMP_SERIALIZATION_HEADER"})
    for item in walk(nodes):
        if isinstance(item, Value) and item.name == "serializableElementName" and item.entry == 1 and item.data:
            refs.add(item.data)
    return refs


def check(mod_dir: Path, export: dict[str, list[dict]]) -> list[str]:
    errors: list[str] = []
    files = [p for p in mod_dir.rglob("*.json") if p.name != "mod-info.json"]
    seen: dict[tuple[str, str], str] = {}
    for path in files:
        type_name = HEADER.search(path.read_text(encoding="utf-8-sig")[:600]).group(1)
        if (path.stem, type_name) in seen:
            errors.append(f"{path}: duplicate element {path.stem} of type {type_name} (also {seen[path.stem, type_name]})")
        seen[path.stem, type_name] = str(path)
    known = set(export) | {name for name, _ in seen}
    types = {info["type"] for infos in export.values() for info in infos}
    dialogs: dict[str, list[str]] = {}
    events: dict[str, tuple[str, list[str]]] = {}

    for path in files:
        raw = json.loads(path.read_text(encoding="utf-8-sig"))
        header = raw["AMP_SERIALIZATION_HEADER"]
        where = path.relative_to(mod_dir).as_posix()
        if header["AssetTypeName"] not in types:
            errors.append(f"{where}: unknown AssetTypeName {header['AssetTypeName']}")
        source = header.get("SourceElementName")
        if source:
            info = next((i for i in export.get(source, []) if i["type"] == header["AssetTypeName"]), None)
            if not info:
                errors.append(f"{where}: SourceElementName {source} not in the {header['AssetTypeName'].split(',')[0]} database")
            elif info["obsolete"]:
                errors.append(f"{where}: SourceElementName {source} is obsolete")
        asset = Asset(copy.deepcopy(raw))
        if asset.to_json() != raw:
            errors.append(f"{where}: Odin node stream does not round-trip")
        for ref in references(raw, asset.nodes) - known:
            errors.append(f"{where}: unresolved reference {ref}")
        for match in re.finditer(r'"RawValue":\s*(-?\d+)', json.dumps(raw)):
            if abs(int(match.group(1))) > INT32_MAX:
                errors.append(f"{where}: RawValue {match.group(1)} exceeds Int32")
        for item in walk(asset.nodes):
            if isinstance(item, Value) and item.entry == 3 and item.data.lstrip("-").isdigit() and abs(int(item.data)) > INT32_MAX:
                errors.append(f"{where}: int node {item.name}={item.data} exceeds Int32")
        if header["AssetTypeName"].startswith("Amplitude.Mercury.Data.Simulation.DialogDefinition,"):
            choice_steps = [n for n in walk(asset.nodes) if isinstance(n, Node) and n.type.split(",")[0].endswith("DialogChoice")]
            if choice_steps:
                dialogs[path.stem] = [v.data for v in choice_steps[-1].child("LocalizedChoices").array.children]
        if header["AssetTypeName"].startswith("Amplitude.Mercury.Data.Simulation.NarrativeEventDefinition,"):
            titles = [c.child("Title").data for c in top(asset.nodes, "Choices").array.children]
            events[path.stem] = (raw["EventDialog"]["serializableElementName"], titles)

    for name, (dialog, titles) in events.items():
        if len(titles) > 4:
            errors.append(f"{name}: {len(titles)} choices, the dialog renders at most 4")
        if dialogs.get(dialog) != titles:
            errors.append(f"{name}: choice titles {titles} != dialog {dialog} LocalizedChoices {dialogs.get(dialog)}")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("export_dir", type=Path)
    parser.add_argument("--mod", type=Path, default=Path("mod") / "DevConsole")
    args = parser.parse_args()
    errors = check(args.mod, export_index(args.export_dir))
    files = sum(1 for _ in args.mod.rglob("*.json"))
    print(f"{args.mod}: {files} files, {len(errors)} error(s)")
    for error in errors:
        print("  ERROR", error)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
