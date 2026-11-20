using System.Collections.Generic;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Record a run of edits, save it, and replay it on another game around that game's capital.</summary>
    internal sealed class TabRecipes : ITab
    {
        private List<Recipes.Listing> saved;
        private string name = "my-start";

        public string Title => "Recipes";

        public void Draw()
        {
            Widgets.Section("Record");
            Widgets.Row((Recipes.Recording ? "● Recording… press to stop" : "● Start recording",
                         () => { if (Recipes.Recording) Recipes.StopRecording(); else Recipes.StartRecording(); }));
            Widgets.Hint(Recipes.Recording
                ? $"{Recipes.RecordedSteps} edits captured. Every edit the game accepts is saved; undos and entity-specific edits (heroes, seats, dungeons) are left out."
                : "While recording, every edit the game accepts is captured. Tiles are stored relative to your capital, so the recipe lands in the same shape on any map.");
            name = Widgets.TextRow("Save as", name);
            Widgets.Button("Save recipe", () => { Recipes.Save(name); saved = null; }, Recipes.Recording && Recipes.RecordedSteps > 0);

            Widgets.Section("Saved recipes");
            if (Recipes.Busy)
            {
                Widgets.Hint("Replaying… one step every few frames, so each step sees the ones before it.");
            }
            if (saved == null)
            {
                saved = Recipes.List();
            }
            Widgets.Row(("Refresh list", () => saved = Recipes.List()),
                        ("Open folder", () => Application.OpenURL("file:///" + Recipes.Folder.Replace('\\', '/'))));
            if (saved.Count == 0)
            {
                Widgets.Hint("No recipes yet. They are saved to BepInEx/config/forge-recipes and can be shared as files.");
            }
            foreach (var listing in saved)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{listing.Name}   ·   {listing.Steps} edits   ·   made on {listing.Build}", Theme.Label);
                Widgets.Button("Replay", () => Recipes.Replay(listing, atSameTiles: false), listing.Steps > 0 && !Recipes.Busy);
                Widgets.Button("Same tiles", () => Recipes.Replay(listing, atSameTiles: true), listing.Steps > 0 && !Recipes.Busy);
                GUILayout.EndHorizontal();
                foreach (var problem in listing.Problems)
                {
                    Widgets.Hint("   " + problem);
                }
            }
            Widgets.Hint("Replay places each edit around this game's capital. 'Same tiles' reuses the recorded tile numbers, for the same map only. "
                         + "Edits that fall off the map or use something this game build lacks are skipped and listed in the edit log.");
        }
    }
}
