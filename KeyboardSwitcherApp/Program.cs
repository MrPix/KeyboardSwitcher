namespace KeyboardLayoutSwitcher;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var switcher = new KeyboardSwitcher();
        switcher.Run();
    }
}