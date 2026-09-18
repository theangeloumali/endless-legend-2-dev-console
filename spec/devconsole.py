"""Declarative spec of the Dev Console: pages -> buttons -> effects. Edit this file to add a button.

Each page is an independent event that appears every turn for human empires (max 4 buttons per event, the
base game's ceiling). Effects are (kind, *args) tuples interpreted by tools/build_devconsole.py.
Amounts are game units; the ×1000 fixed point is applied by the builder. All RawValues must stay < 2^31.
"""

MOD = {
    "display_name": "Dev Console",
    "description": "In-game developer console for feature testing: add Dust, Influence, Research and every "
                   "resource on demand; toggle yield multipliers, instant build and instant research; unlock "
                   "eras; combat cheats. Human empires only. Data-only mod (official modding path).",
    "author": "Angelo",
    "version": "0.3.0",
    "game_version": "1.0.116",
}

# Resource indexes = the definitions' ResourceType field (never the 1-based UIMapper names).
STRATEGIC = list(range(0, 6))
LUXURY = list(range(10, 26))
SPECIALS = [26, 27]  # Corpse/Cadavers (Necrophage), Spirit

# Empire stock caps are 40 per resource (Tag_Empire_Major ResourceNNMaxStock); this descriptor lifts them.
# Property names are 1-based: strategic 0-5 -> 01-06, luxury 10-25 -> 11-26, Spirit 27 -> 28 (Corpse is uncapped).
RESOURCE_CAP = {
    "name": "Descriptor_Dev_ResourceCap",
    "title": "Dev Console: resource caps lifted",
    "properties": [f"Resource{i + 1:02d}MaxStock" for i in STRATEGIC + LUXURY + [27]],
    "add": 1_000_000,
}

# Yield multipliers: Percent on the empire's cities. x1000 is the ceiling that keeps a big late-game city
# under the Int32 fixed-point limit (2,147,483 units).
YIELD_PROPERTIES = ["MoneyGain", "IndustryGain", "ScienceGain", "InfluenceGain", "FoodGain"]
YIELD_STEPS = [10, 100, 1000]

INSTANT = {
    "Status_Dev_InstantBuild": ("Dev Console: instant build", "IndustryGain", 1000),
    "Status_Dev_InstantResearch": ("Dev Console: instant research", "ScienceGain", 1000),
}

# Unit statuses for the combat page (StartingType Unit, applied to every unit of the empire, N turns).
COMBAT = {
    "Status_Dev_Invulnerable": ("Dev Console: invulnerable", [("HealthPoints", 4, 99_900)], 10),
    "Status_Dev_OneHit": ("Dev Console: one-hit kills", [("DamageBonusFlat", 0, 9_999)], 10),
}

PAGES = [
    {
        "name": "DevConsole_Economy", "title": "Dev Console: Economy", "prompt": "Economy - pick an action.",
        "buttons": [
            ("+10,000 Dust", "Adds 10,000 Dust to the treasury.", [("money", 10_000)]),
            ("+100,000 Dust", "Adds 100,000 Dust to the treasury.", [("money", 100_000)]),
            ("+10,000 Influence", "Adds 10,000 Influence.", [("influence", 10_000)]),
            ("+100,000 Influence", "Adds 100,000 Influence.", [("influence", 100_000)]),
        ],
    },
    {
        "name": "DevConsole_Research", "title": "Dev Console: Research", "prompt": "Research - pick an action.",
        "buttons": [
            ("+10,000 Research", "Adds 10,000 Science to the current research.", [("research", 10_000)]),
            ("+100,000 Research", "Adds 100,000 Science to the current research.", [("research", 100_000)]),
            ("Unlock Era I-III techs", "Unlocks every technology of Eras I, II and III.",
             [("unlock_era", 0), ("unlock_era", 1), ("unlock_era", 2)]),
            ("Unlock Era IV-VII techs", "Unlocks every technology of Eras IV to VII.",
             [("unlock_era", 3), ("unlock_era", 4), ("unlock_era", 5), ("unlock_era", 6)]),
        ],
    },
    {
        "name": "DevConsole_Resources", "title": "Dev Console: Resources", "prompt": "Resources - pick an action.",
        "buttons": [
            ("+10,000 all Strategic", "Lifts stock caps and adds 10,000 of each strategic resource.",
             [("lift_caps",), ("resources", STRATEGIC, 10_000)]),
            ("+10,000 all Luxury", "Lifts stock caps and adds 10,000 of each luxury resource.",
             [("lift_caps",), ("resources", LUXURY, 10_000)]),
            ("+10,000 Cadavers & Spirits", "Adds 10,000 of each faction-specific resource.",
             [("lift_caps",), ("resources", SPECIALS, 10_000)]),
            ("+1,000 of everything", "Lifts stock caps and adds 1,000 of every resource.",
             [("lift_caps",), ("resources", STRATEGIC + LUXURY + SPECIALS, 1_000)]),
        ],
    },
    {
        "name": "DevConsole_Yields", "title": "Dev Console: Yields", "prompt": "Yield multiplier - pick a level.",
        "buttons": [
            ("Yields x10", "Dust, Industry, Science, Influence and Food x10 in every city.", [("yields", 10)]),
            ("Yields x100", "Dust, Industry, Science, Influence and Food x100 in every city.", [("yields", 100)]),
            ("Yields x1000", "Dust, Industry, Science, Influence and Food x1000 in every city.", [("yields", 1000)]),
            ("Yields x1 (off)", "Removes every yield multiplier.", [("yields_off",)]),
        ],
    },
    {
        "name": "DevConsole_Instant", "title": "Dev Console: Instant", "prompt": "Instant build / research toggles.",
        "buttons": [
            ("Instant build ON", "Industry x1000 in every city: anything completes next turn.",
             [("apply_status", "Status_Dev_InstantBuild")]),
            ("Instant build OFF", "Removes the industry multiplier.", [("remove_status", "Status_Dev_InstantBuild")]),
            ("Instant research ON", "Science x1000 in every city: a technology per turn.",
             [("apply_status", "Status_Dev_InstantResearch")]),
            ("Instant research OFF", "Removes the science multiplier.", [("remove_status", "Status_Dev_InstantResearch")]),
        ],
    },
    {
        "name": "DevConsole_Combat", "title": "Dev Console: Combat", "prompt": "Combat cheats - 10 turns each.",
        "buttons": [
            ("Invulnerable (10 turns)", "All your units get x1000 health for 10 turns.",
             [("apply_status_on_units", "Status_Dev_Invulnerable")]),
            ("One-hit kills (10 turns)", "All your units deal +9,999 damage for 10 turns.",
             [("apply_status_on_units", "Status_Dev_OneHit")]),
            ("Both (10 turns)", "Invulnerable and one-hit kills for 10 turns.",
             [("apply_status_on_units", "Status_Dev_Invulnerable"), ("apply_status_on_units", "Status_Dev_OneHit")]),
            ("+10,000 Dust", "Adds 10,000 Dust to the treasury.", [("money", 10_000)]),
        ],
    },
]
