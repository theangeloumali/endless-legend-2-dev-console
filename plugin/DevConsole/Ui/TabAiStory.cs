namespace DevConsole.Ui
{
    internal sealed class TabAiStory : ITab
    {
        public string Title => "AI & Story";

        public void Draw() => Widgets.Hint("Coming in this release.");
    }
}
