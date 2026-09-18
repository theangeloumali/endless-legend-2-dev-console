"""Whole-asset builders: descriptors, statuses, narrative events (with their dialog + category), mod-info.

Every plain field the spec owns is written explicitly; everything else stays byte-identical to the exemplar.
Odin node lists are emitted in full because JsonUtility.FromJsonOverwrite replaces them wholesale.
"""
from __future__ import annotations

import copy
import json
import uuid
from pathlib import Path

from odin import Asset, Node, Value, find_node, top

from .exemplars import INT32_MAX, TURN_BEGIN, Exemplars

PERMANENT = INT32_MAX
# The loader adds files in alphabetical path order and validates each element as it lands; a visible
# StatusDefinition needs its UIMapper already present, so mappers live in a folder that sorts first.
MAPPERS = "0_UIMappers"
MAJOR_EMPIRE = "Amplitude.Mercury.Simulation.MajorEmpire, Amplitude.Mercury.Firstpass"
UNIT = "Amplitude.Mercury.Simulation.Unit, Amplitude.Mercury.Firstpass"


def property_effect(target: str, operation: int, raw_value: int) -> dict:
    """One Descriptor.PropertyEffects entry. operation: 0 Add, 1 Sub, 2 Mult, 3 Div, 4 Percent, 5 Pow, 6 Max."""
    return {"Note": "", "TargetProperty": target, "ToTargetOperation": operation, "RpnOperationStack": [],
            "ConstantStack": [{"RawValue": raw_value}], "PropertyLocalName": []}


class Assets:
    def __init__(self, exemplars: Exemplars, out_dir: Path):
        self.ex = exemplars
        self.out = Path(out_dir)
        self.elements: dict[str, str] = {}  # element name -> AssetTypeName (for the validator)

    def _save(self, asset: Asset, folder: str, name: str) -> None:
        asset.save(self.out / folder / f"{name}.json")
        self.elements[name] = asset.type_name

    # ---- descriptors + UI ---------------------------------------------------------------------------
    def descriptor(self, name: str, title: str, effects: list[dict], starting_type: str = MAJOR_EMPIRE,
                   exemplar: str = "empire_descriptor") -> str:
        """`effects` = list of {"Path": [...], "PropertyEffects": [property_effect(...)]}."""
        asset = self.ex.clone(exemplar)
        template = asset.raw["Effects"][0]
        asset.raw["Effects"] = []
        for effect in effects:
            entry = copy.deepcopy(template)
            entry["Path"] = {"PropertyToFollow": effect["Path"], "Validations": [], "specificTargetType": ""}
            entry["PropertyEffects"] = effect["PropertyEffects"]
            asset.raw["Effects"].append(entry)
        asset.raw["startingType"] = starting_type
        self._save(asset, "Descriptors", name)
        ui = self.ex.clone("descriptor_ui")
        ui.raw.update({"RawTitle": title, "Description": "", "Lore": "", "OptionalTag": ""})
        self._save(ui, MAPPERS + "/Descriptors", name)
        return name

    # ---- statuses -----------------------------------------------------------------------------------
    def status(self, name: str, title: str, descriptor: str, *, on_units: bool = False,
               duration: int = PERMANENT, cancels: list[str] = (), hidden: bool = True) -> str:
        """Hidden by default: DataController.CheckStatusDefinitions demands a StatusUIMapper for visible statuses
        and does not see mod-added mappers (startup error). Hidden only drops the status icon; effects still apply."""
        kind = "unit" if on_units else "empire"
        asset = self.ex.clone(f"{kind}_status")
        asset.raw.update({
            "Hidden": hidden, "Descriptor": {"serializableElementName": descriptor},
            "CostModifier": {"serializableElementName": ""}, "InhibitedByStatus": [],
            "CancelOnApplyStatus": [{"serializableElementName": c} for c in cancels],
            "DefaultDuration": duration, "IgnoreGameSpeed": True,
            "StartingType": UNIT if on_units else MAJOR_EMPIRE,
        })
        self._save(asset, "Statuses", name)
        ui = self.ex.clone(f"{kind}_status_ui")
        ui.raw.update({"RawTitle": title, "Description": "", "Lore": "", "OptionalTag": "", "LocalizationLines": []})
        self._save(ui, MAPPERS + "/Statuses", name)
        return name

    # ---- narrative events ---------------------------------------------------------------------------
    def human_only(self) -> Node:
        prereq = top(self.ex.get("event").nodes, "Trigger").child("SimulationEventTrigger").child("Prerequisites").array.children[0]
        prereq.set("EntityID", "Empire")
        human = find_node(self.ex.get("human").nodes, "SimulationVariableFilterEmpireIsPlayedByAI")
        human.name = "Filter"
        human.set("IsInverted", True)
        prereq.children = [prereq.child("EntityID"), human]
        return prereq

    def choice(self, title: str, description: str, effects: list[Node]) -> Node:
        node = copy.deepcopy(top(self.ex.get("event").nodes, "Choices").array.children[0])
        node.set("Title", title)
        node.set("Description", description)
        node.child("ChoiceDialog").set("serializableElementName", None)  # no extra ack dialog
        node.set("Instant", True)
        node.child("Prerequisites").array.children = []
        node.child("SimulationEventEffects").array.children = effects
        node.set("Notes", None)
        return node

    def event(self, name: str, title: str, prompt: str, choices: list[Node]) -> str:
        """A visible, human-only event that fires every TurnBegin (category dead zone 0) with `choices` buttons."""
        category = self.ex.clone("category")
        category.raw.update({
            "IsObsolete": False, "Priority": 100, "Global": False, "IsMandatorySkippable": False,
            "IsOptional": False, "NarrativeEventDefinitionDistribution": 1, "NeedManualTrigger": False,
            "RefillPoolWhenEmpty": True, "MonsoonPrerequisite": 0, "NumberOfTurnsInDeadZoneAfterTrigger": 0,
            "DeadZoneAffectedByGameSpeed": False, "InhibitedNarrativeEventCategories": [], "NoUI": False,
        })
        self._save(category, "Categories", f"NarrativeEventCategory_{name}")

        dialog = self.ex.clone("dialog")
        focus = copy.deepcopy(find_node(dialog.nodes, "DialogCameraFocus"))
        step = copy.deepcopy(find_node(dialog.nodes, "DialogChoice"))
        step.set("LeftCharacterVariable", "Leader")
        step.set("RightCharacterVariable", "None")
        step.set("SpeakerPosition", 0)
        step.set("LocalizationKey", prompt)
        step.child("LocalizedChoices").array.children = [Value("", 1, c.child("Title").data) for c in choices]
        top(dialog.nodes, "Steps").array.children = [focus, step]
        top(dialog.nodes, "OptionalVariables").array.children = []  # exemplar leftovers spam the log
        self._save(dialog, "Dialogs", f"{name}_Dialog")

        asset = self.ex.clone("event")
        asset.raw.update({
            "IsObsolete": False, "Category": {"serializableElementName": f"NarrativeEventCategory_{name}"},
            "EventDialog": {"serializableElementName": f"{name}_Dialog"}, "Title": title, "Description": prompt,
            "Notes": "", "GeoLocalizationID": "Empire",
        })
        trigger = top(asset.nodes, "Trigger")
        trigger.set("ConsumeEventUponTrigger", False)
        trigger.set("PreventEventRemoval", False)
        sim = trigger.child("SimulationEventTrigger")
        sim.set("SimulationEvent", TURN_BEGIN)
        sim.child("Prerequisites").array.children = [self.human_only()]
        sim.child("Variables").array.children = []
        top(asset.nodes, "Choices").array.children = choices
        self._save(asset, "Events", name)
        return name

    # ---- manifest -----------------------------------------------------------------------------------
    def mod_info(self, display_name: str, description: str, author: str, version: str, game_version: str) -> None:
        info = {"DisplayName": display_name, "Description": description, "Author": author, "Version": version,
                "GameVersion": game_version, "Guid": str(uuid.uuid5(uuid.NAMESPACE_DNS, "devconsole.el2")),
                "SteamWorkshopId": ""}
        self.out.mkdir(parents=True, exist_ok=True)
        (self.out / "mod-info.json").write_text(json.dumps(info, indent=4) + "\n", encoding="utf-8")
