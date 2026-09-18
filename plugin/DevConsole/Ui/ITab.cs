namespace DevConsole.Ui
{
    /// <summary>One screen of the console. Tabs keep each file small and the window one screenful.</summary>
    internal interface ITab
    {
        string Title { get; }

        void Draw();
    }
}
