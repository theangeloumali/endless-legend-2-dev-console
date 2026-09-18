"""Generate mod/DevConsole from spec/devconsole.py using base-game exemplars.

    python tools/build_devconsole.py <export-dir> [--out mod/DevConsole]
"""
from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
sys.path.insert(0, str(ROOT / "spec"))

import devconsole as spec  # noqa: E402
from el2mod import ONE, Assets, Effects, Exemplars  # noqa: E402
from el2mod.assets import property_effect  # noqa: E402

PERCENT, ADD = 4, 0


def build_state(assets: Assets) -> None:
    """Descriptors and statuses the buttons reference."""
    cap = spec.RESOURCE_CAP
    assets.descriptor(cap["name"], cap["title"], [{
        "Path": [], "PropertyEffects": [property_effect(p, ADD, cap["add"] * ONE) for p in cap["properties"]],
    }], exemplar="empire_descriptor_self")

    yield_statuses = [f"Status_Dev_Yields_x{n}" for n in spec.YIELD_STEPS]
    for n, status in zip(spec.YIELD_STEPS, yield_statuses):
        descriptor = assets.descriptor(f"Descriptor_Dev_Yields_x{n}", f"Dev Console: yields x{n}", [{
            "Path": ["Cities"], "PropertyEffects": [property_effect(p, PERCENT, (n - 1) * ONE) for p in spec.YIELD_PROPERTIES],
        }])
        assets.status(status, f"Dev Console: yields x{n}", descriptor, cancels=[s for s in yield_statuses if s != status])

    for status, (title, prop, factor) in spec.INSTANT.items():
        descriptor = assets.descriptor(status.replace("Status_", "Descriptor_"), title, [{
            "Path": ["Cities"], "PropertyEffects": [property_effect(prop, PERCENT, (factor - 1) * ONE)],
        }])
        assets.status(status, title, descriptor)

    for status, (title, props, turns) in spec.COMBAT.items():
        descriptor = assets.descriptor(status.replace("Status_", "Descriptor_"), title, [{
            "Path": [], "PropertyEffects": [property_effect(p, op, v * ONE) for p, op, v in props],
        }], starting_type="Amplitude.Mercury.Simulation.Unit, Amplitude.Mercury.Firstpass", exemplar="unit_descriptor")
        assets.status(status, title, descriptor, on_units=True, duration=turns)


def effect_nodes(effects: Effects, kind_args: tuple) -> list:
    kind, *args = kind_args
    match kind:
        case "money":
            return [effects.money(args[0])]
        case "influence":
            return [effects.influence(args[0])]
        case "research":
            return [effects.research(args[0])]
        case "unlock_era":
            return [effects.unlock_era(args[0])]
        case "lift_caps":
            return [effects.apply_descriptor(spec.RESOURCE_CAP["name"])]
        case "resources":
            return [effects.resource(index, args[1]) for index in args[0]]
        case "yields":
            return [effects.apply_status(f"Status_Dev_Yields_x{args[0]}")]
        case "yields_off":
            return [effects.remove_status(f"Status_Dev_Yields_x{n}") for n in spec.YIELD_STEPS]
        case "apply_status":
            return [effects.apply_status(args[0])]
        case "remove_status":
            return [effects.remove_status(args[0])]
        case "apply_status_on_units":
            return [effects.apply_status_on_units(args[0])]
    raise ValueError(f"unknown effect kind {kind!r}")


def build(export_dir: Path, out_dir: Path) -> Assets:
    if out_dir.exists():
        shutil.rmtree(out_dir)
    exemplars = Exemplars(export_dir)
    effects = Effects(exemplars)
    assets = Assets(exemplars, out_dir)
    build_state(assets)
    for page in spec.PAGES:
        if len(page["buttons"]) > 4:
            raise ValueError(f"{page['name']}: the event dialog shows at most 4 buttons")
        choices = [assets.choice(title, description, [n for e in effect_list for n in effect_nodes(effects, e)])
                   for title, description, effect_list in page["buttons"]]
        assets.event(page["name"], page["title"], page["prompt"], choices)
    assets.mod_info(**spec.MOD)
    return assets


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("export_dir", type=Path)
    parser.add_argument("--out", type=Path, default=ROOT / "mod" / "DevConsole")
    args = parser.parse_args()
    assets = build(args.export_dir, args.out)
    print(f"wrote {sum(1 for _ in args.out.rglob('*.json'))} files ({len(assets.elements)} elements) to {args.out}")


if __name__ == "__main__":
    main()
