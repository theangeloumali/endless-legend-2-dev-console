namespace DevConsole.Ui
{
    internal sealed class TabUnits : ITab
    {
        public string Title => "Units";

        public void Draw() => Widgets.Hint("Coming in this release.");
    }
}
