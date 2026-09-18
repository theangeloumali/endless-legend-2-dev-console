namespace DevConsole.Ui
{
    /// <summary>One screen of the console.</summary>
    internal interface ITab
    {
        string Title { get; }

        void Draw();
    }
}
