"""Spike A/B: Dev Console as a flag-driven state machine, cloned from live base-game exemplars.

Hub fires every TurnBegin (human only). Every action also sets a "console open" flag (a faction trait
carrying an empty descriptor); the console twin triggers on SimulationEvent_NarrativeEventChoice while the
flag is present, so each press re-opens the console. Pages use their own flag; Back swaps flags; Close clears.
    python spike/make_spike_a.py <export-dir>
"""
from __future__ import annotations

import copy
import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
from odin import Asset, Node, Value, find_node, top  # noqa: E402

OUT = Path(__file__).resolve().parent / "DevConsole"
ONE = 1000  # FixedPoint.OneRaw
TURN_BEGIN = "Amplitude.Mercury.Simulation.SimulationEvent_TurnBegin, Amplitude.Mercury.Firstpass"
CHOICE_MADE = "Amplitude.Mercury.Simulation.SimulationEvent_NarrativeEventChoice, Amplitude.Mercury.Firstpass"
MONEY_CHANGED = "Amplitude.Mercury.Simulation.SimulationEvent_MoneyStockChanged, Amplitude.Mercury.Firstpass"
END_DIALOGUE = "Amplitude.Mercury.Simulation.SimulationEvent_EndDialogue, Amplitude.Mercury.Firstpass"

EX = {
    "event": "NarrativeEvents_Council_CityManagement/Council_CityManagement_Event001.json",
    "dialog": "Common_Tidefall_Events_DialogDefinition/Common_Tidefall_Event003.json",
    "category": "NarrativeEventCategoryDefinition/NarrativeEventCategory_Collectible.json",
    "money": "SimulationEventEffectsDefinition/AftermathBattleReward_Dust_10.json",
    "influence": "AwakeningQuest_Aspect_ChoiceDefinition/AwakeningQuest_Aspect_01_Step01_Choice.json",
    "human": "NarrativeEvents_MoodMessages/NarrativeEvent_MoodMessage_AttackedByPlayer.json",
    "status": "EmpireStatusDefinition/Status_Empire_Approval_Aspect_AwakeningQuest.json",
    "status_ui": "EmpireStatusDefinitionUIMappers/Status_Empire_Approval_High.json",
    "apply_status": "AwakeningQuest_Aspect_ChoiceDefinition/AwakeningQuest_Aspect_01_Step03_Choice.json",
    "remove_status": "Collectible_Quest_ChoiceDefinition/Collectible_Quest_003_ChoiceDefinition.json",
    "descriptor": "FactionTrait_LastLordDescriptors/Effect_LastLord_NoRebellion.json",
    "descriptor_ui": "FactionTraitDescriptorUIMappers/Effect_LastLord_NoRebellion.json",
    "has_descriptor": "NarrativeEvents_AwakeningQuest/NarrativeEvent_NarrativeEvents_AwakeningQuest_Custom01_Step01.json",
}
EXPORT = Path(".")
_cache: dict[str, Asset] = {}


def exemplar(key: str) -> Asset:
    """Fresh deep copy of an exemplar asset (cached parse)."""
    if key not in _cache:
        _cache[key] = Asset.load(EXPORT / EX[key])
    return copy.deepcopy(_cache[key])


def element_name(key: str) -> str:
    return Path(EX[key]).stem


def flag_descriptor(flag: str) -> str:
    return f"Descriptor_Dev_Flag_{flag}"


def flag_status(flag: str) -> str:
    return f"Status_Dev_Flag_{flag}"


# ---- effects -------------------------------------------------------------------------------------

def amount_effect(key: str, type_suffix: str, amount: int) -> Node:
    effect = find_node(exemplar(key).nodes, type_suffix)
    effect.set("TargetID", "Empire")
    cost = effect.child("Amount")
    cost.child("Constant").set("RawValue", amount * ONE)
    cost.child("RpnDefinitionReference").set("serializableElementName", None)
    cost.set("SourceID", "Empire")
    return effect


def money(amount: int) -> Node:
    return amount_effect("money", "SimulationEventEffect_AddOrRemoveMoney", amount)


def influence(amount: int) -> Node:
    return amount_effect("influence", "SimulationEventEffect_AddOrRemoveInfluence", amount)


def set_flag(flag: str) -> Node:
    """Statuses are the removable carrier: permanent traits never record their effects (MajorEmpire.AddFactionTrait
    skips the bookkeeping when isPermanent), so RemoveEmpireFactionTrait cannot revert them."""
    effect = find_node(exemplar("apply_status").nodes, "SimulationEventEffect_ApplyStatus")
    effect.set("TargetID", "Empire")
    effect.set("Hidden", True)
    effect.child("StatusDefinition").set("serializableElementName", flag_status(flag))
    effect.set("Duration", -1)  # use the status' own DefaultDuration (validator rejects -1 on both sides)
    return effect


def clear_flag(flag: str) -> Node:
    effect = find_node(exemplar("remove_status").nodes, "SimulationEventEffect_RemoveStatus")
    effect.set("TargetID", "Empire")
    effect.set("Hidden", True)
    effect.child("StatusDefinition").set("serializableElementName", flag_status(flag))
    return effect


# ---- prerequisites -------------------------------------------------------------------------------

def prerequisite_template() -> Node:
    return top(exemplar("event").nodes, "Trigger").child("SimulationEventTrigger").child("Prerequisites").array.children[0]


def human_only() -> Node:
    prereq = prerequisite_template()
    prereq.set("EntityID", "Empire")
    human = find_node(exemplar("human").nodes, "SimulationVariableFilterEmpireIsPlayedByAI")
    human.name = "Filter"
    human.set("IsInverted", True)
    prereq.children = [prereq.child("EntityID"), human]
    return prereq


def has_flag(flag: str) -> Node:
    prereq = prerequisite_template()
    prereq.set("EntityID", "Empire")
    filt = find_node(exemplar("has_descriptor").nodes, "SimulationVariableFilterEntityDescriptor")
    filt.name = "Filter"
    filt.set("IsInverted", False)
    refs = filt.child("MustHaveOneOfDescriptors").array
    refs.children = refs.children[:1]
    refs.children[0].set("serializableElementName", flag_descriptor(flag))
    prereq.children = [prereq.child("EntityID"), filt]
    return prereq


# ---- assets --------------------------------------------------------------------------------------

def make_flag(flag: str, title: str) -> None:
    """Empty descriptor + hidden permanent empire status carrying it (+ UIMappers): a per-empire boolean."""
    descriptor = exemplar("descriptor").as_clone_of(element_name("descriptor"))
    descriptor.raw["Effects"] = []
    descriptor.save(OUT / "Descriptors" / f"{flag_descriptor(flag)}.json")
    descriptor_ui = exemplar("descriptor_ui").as_clone_of(element_name("descriptor_ui"))
    descriptor_ui.raw.update({"RawTitle": title, "Description": "", "Lore": "", "OptionalTag": ""})
    descriptor_ui.save(OUT / "UIMappers" / "Descriptors" / f"{flag_descriptor(flag)}.json")

    status = exemplar("status").as_clone_of(element_name("status"))
    status.raw.update({
        "Hidden": True, "Descriptor": {"serializableElementName": flag_descriptor(flag)},
        "CostModifier": {"serializableElementName": ""}, "InhibitedByStatus": [], "CancelOnApplyStatus": [],
        "DefaultDuration": 2147483647, "IgnoreGameSpeed": True,
    })
    status.save(OUT / "Statuses" / f"{flag_status(flag)}.json")
    status_ui = exemplar("status_ui").as_clone_of(element_name("status_ui"))
    for key in ("RawTitle", "Description", "Lore", "OptionalTag"):
        if key in status_ui.raw:
            status_ui.raw[key] = title if key == "RawTitle" else ""
    status_ui.save(OUT / "UIMappers" / "Statuses" / f"{flag_status(flag)}.json")


def make_category(name: str) -> None:
    asset = exemplar("category").as_clone_of(element_name("category"))
    asset.raw.update({
        "IsObsolete": False, "Priority": 100, "Global": False, "IsMandatorySkippable": False,
        "IsOptional": False, "NarrativeEventDefinitionDistribution": 1, "NeedManualTrigger": False,
        "RefillPoolWhenEmpty": True, "MonsoonPrerequisite": 0, "NumberOfTurnsInDeadZoneAfterTrigger": 0,
        "DeadZoneAffectedByGameSpeed": False, "InhibitedNarrativeEventCategories": [], "NoUI": False,
    })
    asset.save(OUT / "Categories" / f"{name}.json")


def make_dialog(name: str, prompt: str, choice_titles: list[str]) -> None:
    asset = exemplar("dialog").as_clone_of(element_name("dialog"))
    steps = top(asset.nodes, "Steps")
    focus = copy.deepcopy(find_node(asset.nodes, "DialogCameraFocus"))
    choice = copy.deepcopy(find_node(asset.nodes, "DialogChoice"))
    choice.set("LeftCharacterVariable", "Leader")
    choice.set("RightCharacterVariable", "None")
    choice.set("SpeakerPosition", 0)
    choice.set("LocalizationKey", prompt)
    choice.child("LocalizedChoices").array.children = [Value("", 1, title) for title in choice_titles]
    steps.array.children = [focus, choice]
    top(asset.nodes, "OptionalVariables").array.children = []  # exemplar leftovers (Advisor/MukagEmpire) spam the log
    asset.save(OUT / "Dialogs" / f"{name}.json")


def make_choice(title: str, description: str, effects: list[Node]) -> Node:
    choice = copy.deepcopy(top(exemplar("event").nodes, "Choices").array.children[0])
    choice.set("Title", title)
    choice.set("Description", description)
    choice.child("ChoiceDialog").set("serializableElementName", None)
    choice.set("Instant", True)
    choice.child("Prerequisites").array.children = []
    for effect in effects:
        effect.name = ""  # array elements are unnamed; exemplars may come from named fields
    choice.child("SimulationEventEffects").array.children = effects
    choice.set("Notes", None)
    return choice


def make_event(name: str, title: str, prompt: str, choices: list[Node], sim_event: str,
               prerequisites: list[Node], repeatable: bool) -> None:
    make_category(f"NarrativeEventCategory_{name}")
    make_dialog(f"{name}_Dialog", prompt, [c.child("Title").data for c in choices])
    asset = exemplar("event").as_clone_of(element_name("event"))
    asset.raw.update({
        "IsObsolete": False, "Category": {"serializableElementName": f"NarrativeEventCategory_{name}"},
        "EventDialog": {"serializableElementName": f"{name}_Dialog"}, "Title": title, "Description": prompt,
        "Notes": "", "GeoLocalizationID": "Empire",
    })
    trigger = top(asset.nodes, "Trigger")
    trigger.set("ConsumeEventUponTrigger", False)
    trigger.set("PreventEventRemoval", repeatable)  # keep in pool so it can fire again within the turn
    sim = trigger.child("SimulationEventTrigger")
    sim.set("SimulationEvent", sim_event)
    sim.child("Prerequisites").array.children = prerequisites
    sim.child("Variables").array.children = []
    top(asset.nodes, "Choices").array.children = choices
    asset.save(OUT / "Events" / f"{name}.json")


def make_mod_info() -> None:
    info = {
        "DisplayName": "Dev Console", "Description": "In-game developer console for feature testing.",
        "Author": "Angelo", "Version": "0.2.0-spike", "GameVersion": "1.0.116",
        "Guid": str(uuid.uuid5(uuid.NAMESPACE_DNS, "devconsole.el2")), "SteamWorkshopId": "",
    }
    (OUT / "mod-info.json").write_text(json.dumps(info, indent=4) + "\n", encoding="utf-8")


def console_choices(tag: str) -> list[Node]:
    """Hub and twins share buttons; actions keep the flag set, Close clears it."""
    return [
        make_choice("+10,000 Dust", f"Adds 10,000 Dust. [{tag}]", [money(10_000), set_flag("Console")]),
        make_choice("+10,000 Influence", f"Adds 10,000 Influence. [{tag}]", [influence(10_000), set_flag("Console")]),
        make_choice("Close", "Closes the console until next turn.", [clear_flag("Console")]),
    ]


def main() -> None:
    global EXPORT
    EXPORT = Path(sys.argv[1])
    make_flag("Console", "Dev Console open")

    make_event("DevConsole_Hub", "Dev Console", "Dev Console - pick an action.", console_choices("hub"),
               TURN_BEGIN, [human_only()], repeatable=False)
    # three candidate re-open triggers, all gated by the flag; the one that fires wins
    for tag, sim_event in (("choice", CHOICE_MADE), ("money", MONEY_CHANGED), ("enddialog", END_DIALOGUE)):
        make_event(f"DevConsole_Twin_{tag}", f"Dev Console [{tag}]", f"Re-opened by {tag}.", console_choices(tag),
                   sim_event, [has_flag("Console")], repeatable=True)
    # probe: if the flag persists, this shows at the next turn begin
    make_event("DevConsole_Probe", "Dev Console [probe]", "Flag is set - the trait mechanism works.",
               [make_choice("OK", "Dismiss.", [])], TURN_BEGIN, [has_flag("Console")], repeatable=False)
    make_mod_info()
    print(f"wrote {sum(1 for _ in OUT.rglob('*.json'))} files to {OUT}")


if __name__ == "__main__":
    main()
