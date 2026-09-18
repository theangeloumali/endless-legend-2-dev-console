"""SimulationEventEffect builders. Each returns a fresh Odin subtree grafted from a base-game exemplar,
so field order and struct/class ids always match what the game deserializes."""
from __future__ import annotations

from odin import Node, find_node

from .exemplars import ONE, Exemplars


class Effects:
    def __init__(self, exemplars: Exemplars):
        self.ex = exemplars

    def _effect(self, key: str, type_suffix: str) -> Node:
        effect = find_node(self.ex.get(key).nodes, type_suffix)
        effect.name = ""  # array elements are unnamed; some exemplars hold the effect in a named field
        effect.set("TargetID", "Empire")
        return effect

    def _amount(self, key: str, type_suffix: str, amount: int) -> Node:
        effect = self._effect(key, type_suffix)
        cost = effect.child("Amount")
        cost.child("Constant").set("RawValue", amount * ONE)
        cost.child("RpnDefinitionReference").set("serializableElementName", None)
        cost.set("SourceID", "Empire")
        return effect

    # ---- instant gains ----------------------------------------------------------------------------
    def money(self, amount: int) -> Node:
        return self._amount("money", "SimulationEventEffect_AddOrRemoveMoney", amount)

    def influence(self, amount: int) -> Node:
        return self._amount("influence", "SimulationEventEffect_AddOrRemoveInfluence", amount)

    def research(self, amount: int) -> Node:
        return self._amount("research", "SimulationEventEffect_AddResearch", amount)

    def resource(self, index: int, amount: int) -> Node:
        """`index` is the definition's ResourceType: strategic 0-5, luxury 10-25, Corpse 26, Spirit 27."""
        effect = self._amount("resource", "SimulationEventEffect_AddOrRemoveResource", amount)
        effect.set("Resource", index)
        return effect

    def unlock_era(self, era_index: int) -> Node:
        effect = self._effect("unlock_era", "SimulationEventEffect_UnlockEraTechnologies")
        effect.set("EraIndex", era_index)
        return effect

    # ---- state -------------------------------------------------------------------------------------
    def apply_descriptor(self, descriptor: str) -> Node:
        """Permanent: the game has no remove-descriptor effect. Use statuses for anything reversible."""
        effect = self._effect("apply_descriptor", "SimulationEventEffect_ApplyDescriptor")
        effect.set("Hidden", True)
        effect.child("Descriptor").set("serializableElementName", descriptor)
        return effect

    def apply_status(self, status: str) -> Node:
        effect = self._effect("apply_status", "SimulationEventEffect_ApplyStatus")
        effect.child("StatusDefinition").set("serializableElementName", status)
        effect.set("Duration", -1)  # the status' DefaultDuration applies; -1 on both sides is rejected
        return effect

    def remove_status(self, status: str) -> Node:
        effect = self._effect("remove_status", "SimulationEventEffect_RemoveStatus")
        effect.child("StatusDefinition").set("serializableElementName", status)
        return effect

    def apply_status_on_units(self, status: str) -> Node:
        effect = self._effect("apply_status_units", "SimulationEventEffect_ApplyStatusOnEmpireArmiesUnits")
        effect.child("StatusDefinition").set("serializableElementName", status)
        effect.set("Duration", -1)
        return effect
