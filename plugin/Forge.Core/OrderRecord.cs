using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Forge.Core
{
    /// <summary>A game order as plain data: what the edit log, undo and recipes keep instead of the live DTO,
    /// so none of them depend on a game type that a patch might rename.</summary>
    public sealed class OrderRecord
    {
        /// <summary>Simple type name, e.g. EditorOrderSpawnObservatoryAt.</summary>
        public string Type;

        /// <summary>Every field that is neither a tile nor the local empire: numbers, names, enums by name.</summary>
        public Dictionary<string, JToken> Scalars = new Dictionary<string, JToken>();

        /// <summary>Absolute tile indexes, for every int field whose name ends in TileIndex.</summary>
        public Dictionary<string, int> Tiles = new Dictionary<string, int>();

        /// <summary>Empire fields that pointed at the player when recorded; replay substitutes the current player.</summary>
        public HashSet<string> LocalEmpireFields = new HashSet<string>();

        /// <summary>Set when a field cannot survive a new game (entity GUIDs, arrays), so recipes leave it out.</summary>
        public bool Unrecordable;

        public OrderRecord Clone() =>
            new OrderRecord
            {
                Type = Type,
                Scalars = new Dictionary<string, JToken>(Scalars),
                Tiles = new Dictionary<string, int>(Tiles),
                LocalEmpireFields = new HashSet<string>(LocalEmpireFields),
                Unrecordable = Unrecordable,
            };

        public static bool IsTileField(string fieldName) => fieldName.EndsWith("TileIndex");
    }
}
