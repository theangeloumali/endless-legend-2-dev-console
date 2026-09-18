using System.Collections.Generic;
using Amplitude;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;
using HarmonyLib;

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

            public override string ToString() => Definition;
        }

        public static List<Item> Stash()
        {
            var items = new List<Item>();
            var stash = Traverse.Create(Sim.LocalEmpire).Field("EquipmentStash").GetValue();
            if (stash == null)
            {
                return items;
            }
            var traverse = Traverse.Create(stash);
            var length = traverse.Field<int>("Length").Value;
            if (!(traverse.Field("Data").GetValue() is System.Array data))
            {
                return items;
            }
            for (var i = 0; i < length && i < data.Length; i++)
            {
                if (!(data.GetValue(i) is EquipmentInfo info))
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
