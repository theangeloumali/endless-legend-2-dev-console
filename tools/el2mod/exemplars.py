"""Base-game exemplar files every generated asset is cloned from (all live, IsObsolete=false)."""
from __future__ import annotations

import copy
from pathlib import Path

from odin import Asset

ONE = 1000  # Amplitude.FixedPoint.OneRaw: every RawValue is the game value x 1000
INT32_MAX = 2_147_483_647
TURN_BEGIN = "Amplitude.Mercury.Simulation.SimulationEvent_TurnBegin, Amplitude.Mercury.Firstpass"

# key -> path inside the export. The stem is also the SourceElementName the game clones from.
PATHS = {
    "event": "NarrativeEvents_Council_CityManagement/Council_CityManagement_Event001.json",
    "dialog": "Common_Tidefall_Events_DialogDefinition/Common_Tidefall_Event003.json",
    "category": "NarrativeEventCategoryDefinition/NarrativeEventCategory_Collectible.json",
    "human": "NarrativeEvents_MoodMessages/NarrativeEvent_MoodMessage_AttackedByPlayer.json",
    "money": "SimulationEventEffectsDefinition/AftermathBattleReward_Dust_10.json",
    "influence": "AwakeningQuest_Aspect_ChoiceDefinition/AwakeningQuest_Aspect_01_Step01_Choice.json",
    "research": "SimulationEventEffectsDefinition/AftermathBattleReward_Research_5.json",
    "resource": "SimulationEventEffectsDefinition/AftermathBattleReward_Resource01_1.json",
    "unlock_era": "FactionTraitCustom/FactionTrait_Custom_Specific01.json",
    "apply_descriptor": "FactionTrait_LastLord/FactionTrait_LastLord_Chapter06AChoice02_FactionQuest.json",
    "apply_status": "AwakeningQuest_Aspect_ChoiceDefinition/AwakeningQuest_Aspect_01_Step03_Choice.json",
    "remove_status": "Collectible_Quest_ChoiceDefinition/Collectible_Quest_003_ChoiceDefinition.json",
    "apply_status_units": "NarrativeEvents_Common/Common_Event001.json",
    "empire_descriptor": "EmpireStatusDescriptor/Effect_Status_Empire_DustLossPerCouncilor.json",
    "empire_descriptor_self": "EmpireStatusDescriptor/Effect_Status_Empire_CurioLootImprove_High.json",
    "empire_status": "EmpireStatusDefinition/Status_Empire_Approval_High.json",
    "empire_status_ui": "EmpireStatusDefinitionUIMappers/Status_Empire_Approval_High.json",
    "unit_descriptor": "UnitStatusDescriptor_Map/StatusDescriptor_Unit_Map_Damage_Gain01.json",
    "unit_status": "UnitStatusDefinition_Map/Status_Unit_Map_Damage_Gain01.json",
    "unit_status_ui": "UnitStatusDefinition_MapUIMappers/Status_Unit_Map_Damage_Gain01.json",
    "descriptor_ui": "FactionTraitDescriptorUIMappers/Effect_LastLord_NoRebellion.json",
}


class Exemplars:
    """Loads exemplar assets from the extracted export; every call returns a fresh deep copy."""

    def __init__(self, export_dir: Path):
        self.export_dir = Path(export_dir)
        self._cache: dict[str, Asset] = {}

    def get(self, key: str) -> Asset:
        if key not in self._cache:
            self._cache[key] = Asset.load(self.export_dir / PATHS[key])
        return copy.deepcopy(self._cache[key])

    def clone(self, key: str) -> Asset:
        """Exemplar copy whose header names the exemplar as SourceElementName (the game clones it)."""
        return self.get(key).as_clone_of(self.source_name(key))

    @staticmethod
    def source_name(key: str) -> str:
        return Path(PATHS[key]).stem
