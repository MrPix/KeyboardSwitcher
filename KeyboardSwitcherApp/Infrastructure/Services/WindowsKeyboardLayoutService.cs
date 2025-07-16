using System.Globalization;
using System.Runtime.InteropServices;
using KeyboardLayoutSwitcher.Core.Entities;
using KeyboardLayoutSwitcher.Core.Interfaces;

namespace KeyboardLayoutSwitcher.Infrastructure.Services;

public class WindowsKeyboardLayoutService : IKeyboardLayoutService
{
    private List<KeyboardLayout> _availableLayouts = new();

    // Windows API imports
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint flags);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // Constants
    private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    private const uint INPUTLANGCHANGE_SYSCHARSET = 0x0001;
    private const uint KLF_ACTIVATE = 0x00000001;

    public WindowsKeyboardLayoutService()
    {
        RefreshLayouts();
    }

    public IEnumerable<KeyboardLayout> GetAvailableLayouts()
    {
        return _availableLayouts;
    }

    public KeyboardLayout? GetCurrentLayout()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
            return null;
            
        var threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        if (threadId == 0)
            return null;
            
        var currentLayoutHandle = GetKeyboardLayout(threadId);

        return _availableLayouts.FirstOrDefault(l => l.Handle == currentLayoutHandle);
    }

    public void ActivateLayout(KeyboardLayout layout)
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, (IntPtr)(int)INPUTLANGCHANGE_SYSCHARSET, layout.Handle);
        }
    }

    public void RefreshLayouts()
    {
        _availableLayouts.Clear();

        // Get number of keyboard layouts
        int layoutCount = GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        if (layoutCount == 0)
            return;

        IntPtr[] layouts = new IntPtr[layoutCount];

        // Get all keyboard layouts
        GetKeyboardLayoutList(layoutCount, layouts);

        foreach (var layoutHandle in layouts)
        {
            var layoutName = GetLayoutName(layoutHandle);
            var layoutId = (int)layoutHandle.ToInt32() & 0xFFFF;
            _availableLayouts.Add(new KeyboardLayout(layoutHandle, layoutName, layoutId));
        }
    }

    private string GetLayoutName(IntPtr hkl)
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