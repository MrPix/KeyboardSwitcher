using System.Globalization;
using System.Runtime.InteropServices;
using KeyboardLayoutSwitcher.Domain.Entities;
using KeyboardLayoutSwitcher.Domain.Interfaces;

namespace KeyboardLayoutSwitcher.Infrastructure.Repositories;

public class WindowsKeyboardLayoutRepository : IKeyboardLayoutRepository
{
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
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // Constants
    private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    private const uint INPUTLANGCHANGE_SYSCHARSET = 0x0001;

    public Task<IEnumerable<KeyboardLayout>> GetAvailableLayoutsAsync()
    {
        var layouts = new List<KeyboardLayout>();

        // Get number of keyboard layouts
        int layoutCount = GetKeyboardLayoutList(0, null);
        IntPtr[] layoutHandles = new IntPtr[layoutCount];

        // Get all keyboard layouts
        GetKeyboardLayoutList(layoutCount, layoutHandles);

        foreach (var handle in layoutHandles)
        {
            uint layoutId = (uint)handle.ToInt32() & 0xFFFF;
            string name = GetLayoutName(handle);
            string displayName = GetLayoutDisplayName(handle);

            layouts.Add(new KeyboardLayout(handle, name, displayName, layoutId));
        }

        return Task.FromResult<IEnumerable<KeyboardLayout>>(layouts);
    }

    public Task<KeyboardLayout?> GetCurrentLayoutAsync()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            uint threadId = GetWindowThreadProcessId(foregroundWindow, out uint processId);
            IntPtr handle = GetKeyboardLayout(threadId);
            
            if (handle != IntPtr.Zero)
            {
                uint layoutId = (uint)handle.ToInt32() & 0xFFFF;
                string name = GetLayoutName(handle);
                string displayName = GetLayoutDisplayName(handle);

                return Task.FromResult<KeyboardLayout?>(
                    new KeyboardLayout(handle, name, displayName, layoutId));
            }
        }

        return Task.FromResult<KeyboardLayout?>(null);
    }

    public Task<bool> SetCurrentLayoutAsync(KeyboardLayout layout)
    {
        try
        {
            IntPtr foregroundWindow = GetForegroundWindow();

            if (foregroundWindow != IntPtr.Zero)
            {
                // Send language change request to the foreground window
                bool result = PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST,
                    new IntPtr(INPUTLANGCHANGE_SYSCHARSET), layout.Handle);

                return Task.FromResult(result);
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string GetLayoutName(IntPtr hkl)
    {
        try
        {
            uint layoutId = (uint)hkl.ToInt32() & 0xFFFF;
            return $"Layout_{layoutId:X4}";
        }
        catch
        {
            return $"Layout_{hkl:X8}";
        }
    }

    private string GetLayoutDisplayName(IntPtr hkl)
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