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
            Widgets.ValueButton("Dust", ref dust, "Set", Economy.SetDust);
            Widgets.ValueButton("Influence", ref influence, "Set", Economy.SetInfluence);
            Widgets.ValueButton("City cap", ref cityCap, "Set", Economy.SetCityCap);

            Widgets.ValueButton("Research", ref research, "Add", Economy.AddResearch);

            Widgets.Section("Resources");
            GUILayout.BeginHorizontal();
            var hasAmount = Widgets.IntField("Amount each", ref resources, out var amount);
            GUILayout.EndHorizontal();
            GUI.enabled = hasAmount;
            Widgets.Row(("All strategic", () => Economy.AddResources(Economy.Strategic, amount)),
                        ("All luxury", () => Economy.AddResources(Economy.Luxury, amount)),
                        ("Cadavers & Spirits", () => Economy.AddResources(Economy.Specials, amount)));
            Widgets.Button("Everything", () => Economy.AddResources(Economy.All, amount), hasAmount);
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
            var tech = technology.Selected(Catalog.Technologies);
            Widgets.Button(tech == null ? "Pick a technology" : "Complete " + tech.Display,
                           () => Economy.CompleteTechnology(tech.Name), tech != null);
        }

        private static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII" };

    }
}
