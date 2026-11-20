namespace DevConsole.Ui
{
    internal sealed class TabSettlements : ITab
    {
        public string Title => "Settlements";

        public void Draw() => Widgets.Hint("Coming in this release.");
    }
}
