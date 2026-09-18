# Dev Console — ENDLESS LEGEND 2

An in-game developer console for **ENDLESS LEGEND 2**. Press **Insert**, get a window with eight tabs: set your
Dust to whatever you like, finish anything in a build queue instantly, hand your heroes levels and Legendary gear,
spawn armies on the tile you're pointing at, force a war, or switch to playing another empire entirely.

It works by using **the game's own developer tools**. Amplitude shipped their in-house map editor in the retail
build — about 110 editor commands — and left it public. The console asks the simulation to do things the same way
their editor did, instead of writing values into memory behind the game's back.

> Unofficial and not affiliated with Amplitude Studios or SEGA. Single-player testing tool — don't use it in
> multiplayer against people who haven't agreed to it.

![Game](https://img.shields.io/badge/game-V1.0.116-blue) ![License](https://img.shields.io/badge/license-MIT-green)

---

## Install

1. **Install BepInEx 5** (x64, Mono) if you don't already have it — [download it here](https://github.com/BepInEx/BepInEx/releases).
   Extract it into the game folder, the one containing `Endless Legend 2.exe`:
   ```
   .../steamapps/common/ENDLESS Legend 2/
   ```
2. **Run the game once, then quit.** This makes BepInEx create its folders.
3. **Download `DevConsole.zip`** from the [latest release](../../releases/latest).
4. **Extract `DevConsole.dll`** into:
   ```
   .../ENDLESS Legend 2/BepInEx/plugins/
   ```
5. Start the game, load a save, and press **Insert**.

If the window doesn't appear, open `BepInEx/LogOutput.log` and look for `Dev Console` — it logs a line on load and
a line for every action, which is the fastest way to see what happened.

> **Remove any other EL2 cheat plugin first.** The Nexus "EL2 Resource Manager" patches the same income methods and
> running both doubles your multipliers. This plugin refuses to load alongside it.

---

## Using it

**Insert** opens and closes the window. Drag it by the title bar. Everything applies the moment you click — there
is no confirm step.

### Economy

Type a number and press **Set**. These set an absolute value rather than adding, so `1000000` in the Dust box means
you end up with exactly one million.

Resources give you every strategic, luxury, Cadaver and Spirit in one press. Technologies unlock a whole era, or
search for a single one by name.

### Build — instant build and instant recruit

Press **Refresh settlements**, pick one, and complete anything in its queue **free and immediately**. Units are
constructions in EL2, so this is how you instantly recruit an army. There is also a button to clear every
settlement's queue at once.

### Heroes

Press **Refresh roster** first. Pick a hero from the selector, or tick **apply to all** to hit your whole roster
with the same action.

- **Skill points → Give** tops them up (stat increases spend them, so do this first)
- Fill in the four stat boxes and press **Apply stats**
- **Give XP** to level them, plus **Heal** and **Dismiss**
- **Create draw** generates any number of new heroes at a level range you choose, then **Recruit all in draw**
- **Spawn** any named hero in the game onto the tile your mouse is over

### Equipment

All 177 items, **sorted rarest first** and coloured by rarity. Add one to your stash, or use the bulk filter to add
everything matching a word. Then equip it to the selected hero, or clear the empire's equipment out.

### World

This tab targets **the tile your mouse is hovering over** — the tile number updates live as you move across the
map. Spawn any unit there, found a city or camp, teleport a selected army to it, give an army absurd movement,
reveal the entire map, or collect every curiosity on it.

### Diplomacy

Declare war, force peace, force all treaties, make an empire offer surrender, change war score, meet everybody, and
pick your victory path. **Switch local empire** hands you control of another empire — the console follows you, so
it is a genuine way to inspect what the AI is sitting on.

### Battle

The game's own seven battle cheats: infinite movement, infinite action tokens, infinite battle skills, ignore zone
of control, ignore round count, ignore empire playing, and line-of-sight debug. They last only for the session —
restarting the game clears them.

### Yields

Per-turn multipliers (×2 to ×1000) for Dust, Industry, Science and Influence, plus instant build and instant
research. Unlike everything else these are applied as income arrives, so they show up on your **next turn**, not
immediately. For a one-off jump use the Economy tab.

---

## Settings

Edit `BepInEx/config/angelo.el2.devconsole.cfg`, or delete it to reset:

| Setting         | Default  | What it does                                                                          |
| --------------- | -------- | ------------------------------------------------------------------------------------- |
| `Hotkey`        | `Insert` | Key that opens and closes the window                                                  |
| `UiScale`       | `0`      | Window scale. `0` picks one from your resolution (×2 at 4K); set a number to override |
| `WindowHeight`  | `520`    | Height of the scrolling area                                                          |
| `NativeOverlay` | `true`   | Unlocks the game's built-in debug overlay (see Known limits)                          |
| `Yields`        | `1`      | The four per-turn multipliers, also on the Yields tab                                 |

---

## Achievements

**This plugin does not disable achievements** — the game cannot see BepInEx plugins at all. Its modded check
(`ModificationManager`) only ever looks at the Steam Workshop directory and the `Modding/` folder.

**Any Workshop or data mod does disable them, though.** When `ModdingUtils.IsModdingEnabled` is true,
`StatisticsReporterAncillary.InitializeOnLoad` returns before registering a single statistic reporter — and EL2
achievements are statistic-driven, so none can ever fire. Unsubscribe from Workshop mods (and check the content
folder actually emptied) if you want achievements. `Player.log` tells you which state you are in:

```
[Modification] Modding is not enabled because no modifications were registered.   ← achievements work
[Modification] Modding is enabled with N active modifications.                    ← achievements off
```

---

## Known limits

- **The Yields multipliers affect console actions too.** Orders are processed asynchronously, so if Science is at
  ×1000 a console "+10,000 research" gets multiplied as well. Set the multipliers to `off` when you want exact
  numbers.
- **Everything caps around 2,147,483.** The simulation uses 32-bit fixed-point maths, so stocks and multiplied
  yields cannot exceed that without overflowing.
- **No item icons.** The game keeps them in a streamed virtual-texture atlas only its own UI shader can draw. Names,
  rarities and rarity colours all work.
- **The native debug overlay does not open.** The plugin unlocks Amplitude's own overlay — `DebugOverlayManager`
  reports itself disabled in retail and the plugin flips that — but **F2 still does not raise it** on V1.0.116,
  because the manager that registers the key never starts. Set `NativeOverlay = false` to skip the patch. The
  Insert window is unaffected either way.
- **Built against V1.0.116.** Every game member is resolved by name and null-checked, so a future patch that renames
  something disables that one control and logs a warning instead of breaking the plugin.
- Cadavers and Spirits exist only for the factions that use them; those buttons are harmless elsewhere.

---

## Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) and a copy of the game — it references the game's
assemblies at compile time, and none of them are redistributed here.

```bash
python tools/build_plugin.py                 # build and install into BepInEx/plugins
python tools/build_plugin.py --no-install    # build only
```

If the game is not at the default path:

```bash
python tools/build_plugin.py --game-dir "D:/SteamLibrary/steamapps/common/ENDLESS Legend 2"
```

### How it is put together

**Writes go through the game's order system.** `Amplitude.Mercury.Interop` holds ~110 public `EditorOrder*` types
plus the god/cheat orders, and `SandboxManager.PostOrder` enqueues them onto a thread-safe queue the simulation
drains and validates. That is the whole of `Orders.cs`.

**Reads use reflection**, because `Sandbox`, `Empire`, `Hero`, `Settlement` and `Army` are all `internal`. `Sim.cs`
is that bridge.

Two things are not orders. The yield multipliers are Harmony patches on the `Gain*` methods, because no order scales
per-turn income. The battle cheats call `BattleDebug.SetCheat` directly with `writeRegistry: false`.

Names and rarities come from the live datatables: each definition is paired with its `UIMapper` by element name and
the mapper's `%Key` title resolved through `ILocalizationService`, so lists follow your language and pick up
anything a data mod adds.

```
plugin/DevConsole/
  Plugin.cs        entry point, hotkey
  Orders.cs        order dispatch          Sim.cs      reflection reads
  Catalog.cs       names, rarities, sort   Patches.cs  yield multipliers
  DebugOverlay.cs  native overlay unlock
  Cheats/          one file per tab's actions
  Ui/              Theme.cs, Widgets.cs, Window.cs, one Tab*.cs per tab
tools/             build and install scripts
mod/DevConsole/    a separate data-only mod (see below)
docs/plans/        design notes and the research behind it
```

---

## The data-mod variant

`mod/DevConsole/` is a **separate** version built only from the official JSON modding path — no code injection, so
it could be shared on the Steam Workshop. It is far more limited: the console appears as an every-turn event dialog
with at most four buttons per page, and each action needs a confirm click.

The plugin replaces it. Use this only if you specifically need a no-injection mod:

```bash
python tools/install_mod.py --source mod/DevConsole    # install
python tools/install_mod.py --uninstall                # remove
```

Start a **new game** after installing or removing it — saves keep references to its elements.

---

## License

MIT — see [LICENSE](LICENSE). Unofficial; ENDLESS LEGEND is a trademark of Amplitude Studios / SEGA. No game assets
are redistributed in this repository.
