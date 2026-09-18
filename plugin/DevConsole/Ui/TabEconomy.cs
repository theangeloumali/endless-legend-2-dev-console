using System.Linq;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Stocks are SET (the orders take an absolute value), resources and eras are added.</summary>
    internal sealed class TabEconomy : ITab
    {
        private readonly Widgets.Dropdown technology = new Widgets.Dropdown("Technology");
        private string dust = "1000000";
        private string influence = "1000000";
        private string research = "10000";
        private string cityCap = "99";
        private string resources = "10000";

        public string Title => "Economy";

        public void Draw()
        {
            Widgets.Section("Stocks  (the order sets an absolute value)");
            Stock("Dust", ref dust, Economy.SetDust);
            Stock("Influence", ref influence, Economy.SetInfluence);
            Stock("City cap", ref cityCap, Economy.SetCityCap);

            GUILayout.BeginHorizontal();
            var hasResearch = Widgets.IntField("Research", ref research, out var researchValue);
            Widgets.Button("Add", () => Economy.AddResearch(researchValue), hasResearch);
            GUILayout.EndHorizontal();

            Widgets.Section("Resources");
            GUILayout.BeginHorizontal();
            var hasAmount = Widgets.IntField("Amount each", ref resources, out var amount);
            GUILayout.EndHorizontal();
            GUI.enabled = hasAmount;
            Widgets.Row(("All strategic", () => Economy.AddResources(Economy.Strategic, amount)),
                        ("All luxury", () => Economy.AddResources(Economy.Luxury, amount)),
                        ("Cadavers & Spirits", () => Economy.AddResources(Economy.Specials, amount)));
            Widgets.Button("Everything", () => Economy.AddResources(
                Economy.Strategic.Concat(Economy.Luxury).Concat(Economy.Specials), amount), hasAmount);
            GUI.enabled = true;

            Widgets.Section("Technologies");
            GUILayout.BeginHorizontal();
            for (var era = 0; era < Economy.EraCount; era++)
            {
                var index = era;
                Widgets.Button("Era " + Roman[era], () => Economy.UnlockEra(index));
            }
            Widgets.Button("All", Economy.UnlockAllEras);
            GUILayout.EndHorizontal();
            technology.Draw(Catalog.Technologies);
            Widgets.Button("Complete selected technology",
                           () => Economy.CompleteTechnology(technology.Selected(Catalog.Technologies)),
                           technology.Selected(Catalog.Technologies) != null);
        }

        private static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII" };

        private static void Stock(string label, ref string buffer, System.Action<int> apply)
        {
            GUILayout.BeginHorizontal();
            var parsed = Widgets.IntField(label, ref buffer, out var value);
            var captured = value;
            Widgets.Button("Set", () => apply(captured), parsed);
            GUILayout.EndHorizontal();
        }
    }
}
