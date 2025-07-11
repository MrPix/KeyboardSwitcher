namespace KeyboardLayoutSwitcher;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var layoutManager = new WindowsKeyboardLayoutManager();
        var hotkeyManager = new WindowsHotkeyManager();
        var systemTray = new WindowsSystemTray();
        var switcher = new KeyboardSwitcher(layoutManager, hotkeyManager, systemTray);
        switcher.Run();
    }
}