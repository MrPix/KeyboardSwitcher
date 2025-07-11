using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KeyboardLayoutSwitcher;

public class WindowsKeyboardLayoutManager : IKeyboardLayoutManager
{
    [DllImport("user32.dll")]
    private static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);
    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    private const uint INPUTLANGCHANGE_SYSCHARSET = 0x0001;

    public IReadOnlyList<IntPtr> GetAvailableLayouts()
    {
        int layoutCount = GetKeyboardLayoutList(0, null);
        IntPtr[] layouts = new IntPtr[layoutCount];
        GetKeyboardLayoutList(layoutCount, layouts);
        return layouts;
    }

    public IntPtr GetCurrentLayout()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            uint threadId = GetWindowThreadProcessId(foregroundWindow, out uint processId);
            return GetKeyboardLayout(threadId);
        }
        return IntPtr.Zero;
    }

    public void ActivateLayout(IntPtr hkl)
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, new IntPtr(INPUTLANGCHANGE_SYSCHARSET), hkl);
        }
    }

    public string GetLayoutName(IntPtr hkl)
    {
        try
        {
            uint layoutId = (uint)hkl.ToInt32() & 0xFFFF;
            var culture = new CultureInfo((int)layoutId);
            return culture.DisplayName;
        }
        catch
        {
            return $"Layout {hkl:X8}";
        }
    }
}
