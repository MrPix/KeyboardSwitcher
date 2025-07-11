using System.Windows.Forms;

namespace KeyboardLayoutSwitcher;

public class WindowsHotkeyManager : IHotkeyManager
{
    private readonly HotkeyForm _form;
    public WindowsHotkeyManager()
    {
        _form = new HotkeyForm();
        Application.ApplicationExit += (s, e) => _form.Dispose();
    }

    public void RegisterHotkey(int id, uint modifiers, uint key, Action<int> onHotkeyPressed)
    {
        _form.HotkeyPressed += onHotkeyPressed;
        NativeMethods.RegisterHotKey(_form.Handle, id, modifiers, key);
    }

    public void UnregisterHotkey(int id)
    {
        NativeMethods.UnregisterHotKey(_form.Handle, id);
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
