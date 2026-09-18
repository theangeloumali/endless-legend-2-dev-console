"""Spike A: hand-shaped Dev Console hub + pages, cloned from live base-game exemplars.

Proves load / render / recur / human-only / instant effects / paging before any generator exists.
    python spike/make_spike_a.py <export-dir> [--six]
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
# raised when any narrative choice is made — the context in which a TriggerNarrativeEventConsequence page opens
CHOICE_MADE = "Amplitude.Mercury.Simulation.SimulationEvent_NarrativeEventChoice, Amplitude.Mercury.Firstpass"

EX = {
    "event": "NarrativeEvents_Council_CityManagement/Council_CityManagement_Event001.json",
    "dialog": "Common_Tidefall_Events_DialogDefinition/Common_Tidefall_Event003.json",
    "ack": "NarrativeEventDialog/Dialog_NarrativeEvent_Placeholder.json",
    "category": "NarrativeEventCategoryDefinition/NarrativeEventCategory_Collectible.json",
    "money": "SimulationEventEffectsDefinition/AftermathBattleReward_Dust_10.json",
    "influence": "AwakeningQuest_Aspect_ChoiceDefinition/AwakeningQuest_Aspect_01_Step01_Choice.json",
    "consequence": "MinorFactionQuestChoiceDefinition/MinorFaction_GenericQuest_01_ChoiceDefinition.json",
    "human": "NarrativeEvents_MoodMessages/NarrativeEvent_MoodMessage_AttackedByPlayer.json",
}


def load(export: Path, key: str) -> Asset:
    return Asset.load(export / EX[key])


def element_name(key: str) -> str:
    return Path(EX[key]).stem


# ---- effects (each returns a fresh subtree grafted from an exemplar) ------------------------------

def amount_effect(export: Path, key: str, type_suffix: str, amount: int) -> Node:
    effect = copy.deepcopy(find_node(load(export, key).nodes, type_suffix))
    effect.set("TargetID", "Empire")
    cost = effect.child("Amount")
    cost.child("Constant").set("RawValue", amount * ONE)
    cost.child("RpnDefinitionReference").set("serializableElementName", None)
    cost.set("SourceID", "Empire")
    return effect


def page_effect(export: Path, event: str) -> Node:
    """Open another narrative event directly. The category-based TriggerNarrativeEvent is rejected by
    DataController.IsSimulationEventEffectValid inside a choice; this consequence form is the allowed one."""
    effect = copy.deepcopy(find_node(load(export, "consequence").nodes, "SimulationEventEffect_TriggerNarrativeEventConsequence"))
    effect.set("SimulationEffectDescriptionOverride", None)
    effect.set("ChancesToTriggerAConsequence", 100)
    effect.set("Delay", -1)
    consequences = effect.child("PossibleConsequences").array
    consequences.children = consequences.children[:1]
    consequence = consequences.children[0]
    consequence.child("NarrativeEventDefinition").set("serializableElementName", event)
    consequence.child("Fallback").set("serializableElementName", None)
    consequence.set("Weight", 100)
    consequence.child("Stack").array.children = []
    return effect


def human_only_prerequisite(export: Path) -> Node:
    prereq = copy.deepcopy(top(load(export, "event").nodes, "Trigger").child("SimulationEventTrigger").child("Prerequisites").array.children[0])
    prereq.set("EntityID", "Empire")
    human = copy.deepcopy(find_node(load(export, "human").nodes, "SimulationVariableFilterEmpireIsPlayedByAI"))
    human.name = "Filter"
    human.set("IsInverted", True)
    prereq.children = [prereq.child("EntityID"), human]
    return prereq


# ---- assets --------------------------------------------------------------------------------------

def make_category(export: Path, name: str, manual: bool) -> None:
    asset = load(export, "category").as_clone_of(element_name("category"))
    asset.raw.update({
        "IsObsolete": False, "Priority": 100, "Global": False, "IsMandatorySkippable": False,
        "IsOptional": False, "NarrativeEventDefinitionDistribution": 1, "NeedManualTrigger": manual,
        "RefillPoolWhenEmpty": True, "MonsoonPrerequisite": 0, "NumberOfTurnsInDeadZoneAfterTrigger": 0,
        "DeadZoneAffectedByGameSpeed": False, "InhibitedNarrativeEventCategories": [], "NoUI": False,
    })
    asset.save(OUT / "Categories" / f"{name}.json")


def make_dialog(export: Path, name: str, prompt: str, choice_titles: list[str]) -> None:
    asset = load(export, "dialog").as_clone_of(element_name("dialog"))
    steps = top(asset.nodes, "Steps")
    focus = copy.deepcopy(find_node(asset.nodes, "DialogCameraFocus"))
    choice = copy.deepcopy(find_node(asset.nodes, "DialogChoice"))
    choice.set("LeftCharacterVariable", "Leader")
    choice.set("RightCharacterVariable", "None")
    choice.set("SpeakerPosition", 0)
    choice.set("LocalizationKey", prompt)
    choice.child("LocalizedChoices").array.children = [Value("", 1, title) for title in choice_titles]
    steps.array.children = [focus, choice]
    asset.save(OUT / "Dialogs" / f"{name}.json")


def make_ack(export: Path) -> None:
    asset = load(export, "ack").as_clone_of(element_name("ack"))
    find_node(asset.nodes, "DialogLine").set("LocalizationKey", "Applied.")
    asset.save(OUT / "Dialogs" / "DevConsole_Ack.json")


def make_choice(template: Node, title: str, description: str, dialog: str | None, effects: list[Node]) -> Node:
    choice = copy.deepcopy(template)
    choice.set("Title", title)
    choice.set("Description", description)
    choice.child("ChoiceDialog").set("serializableElementName", dialog)
    choice.set("Instant", True)
    choice.child("Prerequisites").array.children = []
    for effect in effects:
        effect.name = ""  # array elements are unnamed; exemplars may come from named fields
    choice.child("SimulationEventEffects").array.children = effects
    choice.set("Notes", None)
    return choice


def make_event(export: Path, name: str, category: str, dialog: str, title: str, description: str,
               choices: list[Node], sim_event: str, human_only: bool) -> None:
    asset = load(export, "event").as_clone_of(element_name("event"))
    asset.raw.update({
        "IsObsolete": False, "Category": {"serializableElementName": category},
        "EventDialog": {"serializableElementName": dialog}, "Title": title, "Description": description,
        "Notes": "", "GeoLocalizationID": "Empire",
    })
    trigger = top(asset.nodes, "Trigger")
    trigger.set("ConsumeEventUponTrigger", False)
    trigger.set("PreventEventRemoval", False)
    sim = trigger.child("SimulationEventTrigger")
    sim.set("SimulationEvent", sim_event)
    sim.child("Prerequisites").array.children = [human_only_prerequisite(export)] if human_only else []
    sim.child("Variables").array.children = []
    top(asset.nodes, "Choices").array.children = choices
    asset.save(OUT / "Events" / f"{name}.json")


def make_mod_info() -> None:
    info = {
        "DisplayName": "Dev Console", "Description": "In-game developer console for feature testing.",
        "Author": "Angelo", "Version": "0.1.0-spike", "GameVersion": "1.0.116",
        "Guid": str(uuid.uuid5(uuid.NAMESPACE_DNS, "devconsole.el2.spike")), "SteamWorkshopId": "",
    }
    (OUT / "mod-info.json").write_text(json.dumps(info, indent=4) + "\n", encoding="utf-8")


def hub_choices(export: Path, template: Node, six: bool) -> list[Node]:
    choices = [
        make_choice(template, "+10,000 Dust", "Adds 10,000 Dust to your treasury.", "DevConsole_Ack",
                    [amount_effect(export, "money", "SimulationEventEffect_AddOrRemoveMoney", 10_000)]),
        make_choice(template, "Open test page", "Opens a second console page immediately.", None,
                    [page_effect(export, "DevConsole_Test")]),
        make_choice(template, "+10,000 Influence", "Adds 10,000 Influence.", None,
                    [amount_effect(export, "influence", "SimulationEventEffect_AddOrRemoveInfluence", 10_000)]),
        make_choice(template, "Close", "Closes the console until next turn.", "DevConsole_Ack", []),
    ]
    if six:  # measurement run: how many buttons does the dialog render?
        choices[3:3] = [make_choice(template, "Spare 5", "Fifth button (render test).", None, []),
                        make_choice(template, "Spare 6", "Sixth button (render test).", None, [])]
    return choices


def main() -> None:
    export = Path(sys.argv[1])
    six = "--six" in sys.argv
    template = copy.deepcopy(top(load(export, "event").nodes, "Choices").array.children[0])
    titles = lambda choices: [c.child("Title").data for c in choices]  # noqa: E731

    make_category(export, "NarrativeEventCategory_DevConsole", manual=False)
    make_category(export, "NarrativeEventCategory_DevConsole_Pages", manual=True)
    make_ack(export)

    # Hub: fires every TurnBegin for human empires.
    hub = hub_choices(export, template, six)
    make_dialog(export, "DevConsole_Hub_Dialog", "Dev Console - pick an action.", titles(hub))
    make_event(export, "DevConsole_Hub", "NarrativeEventCategory_DevConsole", "DevConsole_Hub_Dialog",
               "Dev Console", "Developer test console.", hub, TURN_BEGIN, human_only=True)

    # Hub twin: same buttons, reachable from a page's Back button (opens in the NarrativeEventChoice context).
    hub_page = hub_choices(export, template, six)
    make_dialog(export, "DevConsole_HubPage_Dialog", "Dev Console - pick an action.", titles(hub_page))
    make_event(export, "DevConsole_HubPage", "NarrativeEventCategory_DevConsole_Pages", "DevConsole_HubPage_Dialog",
               "Dev Console", "Developer test console.", hub_page, CHOICE_MADE, human_only=False)

    # Test page: an action that re-opens itself (multiple presses per turn) and Back.
    test = [
        make_choice(template, "+10,000 Influence (again)", "Adds 10,000 Influence and stays on this page.", None,
                    [amount_effect(export, "influence", "SimulationEventEffect_AddOrRemoveInfluence", 10_000),
                     page_effect(export, "DevConsole_Test")]),
        make_choice(template, "Back", "Return to the console.", None, [page_effect(export, "DevConsole_HubPage")]),
    ]
    make_dialog(export, "DevConsole_Test_Dialog", "Test page - paging works if you can read this.", titles(test))
    make_event(export, "DevConsole_Test", "NarrativeEventCategory_DevConsole_Pages", "DevConsole_Test_Dialog",
               "Dev Console - Test page", "Second page.", test, CHOICE_MADE, human_only=False)
    make_mod_info()
    print(f"wrote {sum(1 for _ in OUT.rglob('*.json'))} files to {OUT}")


if __name__ == "__main__":
    main()
