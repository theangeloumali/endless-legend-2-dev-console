namespace DevConsole.Ui
{
    internal sealed class TabRecipes : ITab
    {
        public string Title => "Recipes";

        public void Draw() => Widgets.Hint("Coming in this release.");
    }
}
