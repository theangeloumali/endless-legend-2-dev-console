using System.Collections.Generic;
using Amplitude;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;

namespace DevConsole.Cheats
{
    /// <summary>
    /// The empire's equipment stash and what a hero wears. EquipmentInfo is a public struct inside a public
    /// ListOfStruct, so only the field that holds the stash needs reflection.
    /// </summary>
    internal static class Equipment
    {
        public static readonly HeroEquipmentSlot[] Slots =
        {
            HeroEquipmentSlot.Weapon, HeroEquipmentSlot.Armor, HeroEquipmentSlot.Accessory,
            HeroEquipmentSlot.Consumable, HeroEquipmentSlot.Pet,
        };

        public sealed class Item
        {
            public ulong UniqueId;
            public string Definition;
        }

        public static List<Item> Stash()
        {
            var items = new List<Item>();
            foreach (var entry in Sim.Structs(Sim.LocalEmpire, "EquipmentStash"))
            {
                if (!(entry is EquipmentInfo info))
                {
                    continue;
                }
                items.Add(new Item
                {
                    UniqueId = info.UniqueID,
                    Definition = info.EquipmentDefinition == null ? "(unknown)" : info.EquipmentDefinition.Name.ToString(),
                });
            }
            return items;
        }

        public static void Add(string definitionName) =>
            Orders.Post(new OrderForceAddEquipment { HeroEquipmentName = new StaticString(definitionName) }, $"add {definitionName}");

        public static void AddAll(IEnumerable<string> definitionNames)
        {
            foreach (var name in definitionNames)
            {
                Add(name);
            }
        }

        public static void Equip(Item item, Heroes.Entry hero) =>
            Orders.Post(new OrderHeroEquip { EquipmentUniqueID = item.UniqueId, HeroIndex = hero.Index },
                        $"equip {item.Definition} on {hero.Name}");

        public static void Unequip(HeroEquipmentSlot slot, Heroes.Entry hero) =>
            Orders.Post(new OrderHeroUnequip { TargetEquipmentSlot = slot, HeroIndex = hero.Index },
                        $"unequip {slot} from {hero.Name}");

        public static void ClearAll() =>
            Orders.Post(new EditorOrderClearAllEquipment { EmpireIndex = Sim.LocalEmpireIndex }, "clear all equipment");
    }
}
