using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardLayoutSwitcher;

public class KeyboardSwitcher
{
    private readonly IKeyboardLayoutManager _layoutManager;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly ISystemTray _systemTray;
    private List<IntPtr> availableLayouts;
    private SwitchingAlgorithm algorithm = SwitchingAlgorithm.Toggle;
    private bool isShiftReleased = true;
    private System.Windows.Forms.Timer hotkeyReleaseTimer;
    private bool hotkeyActive = false;
    private const int HOTKEY_ID_ALT_SHIFT = 1;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private const int VK_SHIFT = 0x10;

    public enum SwitchingAlgorithm
    {
        Cycle,
        Toggle,
        MostRecent,
        Frequency
    }

    public KeyboardSwitcher(
        IKeyboardLayoutManager layoutManager,
        IHotkeyManager hotkeyManager,
        ISystemTray systemTray)
    {
        _layoutManager = layoutManager;
        _hotkeyManager = hotkeyManager;
        _systemTray = systemTray;
        InitializeComponent();
        LoadAvailableLayouts();
        RegisterHotkeys();
    }

    private void InitializeComponent()
    {
        _systemTray.SetIcon("Keyboard Layout Switcher", SwitchKeyboardLayout);
        _systemTray.SetContextMenu(new List<(string, Action)> { ("Exit", Exit) });
    }

    private void LoadAvailableLayouts()
    {
        availableLayouts = _layoutManager.GetAvailableLayouts().ToList();
        Console.WriteLine($"Found {availableLayouts.Count} keyboard layouts:");
        for (int i = 0; i < availableLayouts.Count; i++)
        {
            string layoutName = _layoutManager.GetLayoutName(availableLayouts[i]);
            Console.WriteLine($"  {i}: {layoutName}");
        }
    }

    private void RegisterHotkeys()
    {
        _hotkeyManager.RegisterHotkey(HOTKEY_ID_ALT_SHIFT, MOD_SHIFT | MOD_ALT, (uint)Keys.Menu, OnHotkeyPressed);
    }

    private void OnHotkeyPressed(int hotkeyId)
    {
        if (hotkeyId == HOTKEY_ID_ALT_SHIFT)
        {
            hotkeyActive = true;
            if (isShiftReleased)
            {
                isShiftReleased = false;
                SwitchWithToggleAlgorithm();
            }
            else
            {
                SwitchWithCycleAlgorithm();
            }
            StartHotkeyReleaseTimer();
        }
    }

    private void StartHotkeyReleaseTimer()
    {
        if (hotkeyReleaseTimer == null)
        {
            hotkeyReleaseTimer = new System.Windows.Forms.Timer();
            hotkeyReleaseTimer.Interval = 200;
            hotkeyReleaseTimer.Tick += (s, e) =>
            {
                bool shiftUp = (NativeMethods.GetAsyncKeyState(VK_SHIFT) & 0x8000) == 0;
                bool altUp = (NativeMethods.GetAsyncKeyState((int)Keys.Menu) & 0x8000) == 0;
                if (shiftUp && hotkeyActive)
                {
                    isShiftReleased = true;
                    hotkeyReleaseTimer.Stop();
                }
                if (shiftUp && altUp && hotkeyActive)
                {
                    hotkeyActive = false;
                    MoveSelectedLayoutToFront();
                    TraceLayouts();
                }
            };
        }
        hotkeyReleaseTimer.Stop();
        hotkeyReleaseTimer.Start();
    }

    private void SwitchWithToggleAlgorithm() => SwitchByToggle();
    private void SwitchWithCycleAlgorithm() => SwitchByCycle();

    private void SwitchKeyboardLayout()
    {
        switch (algorithm)
        {
            case SwitchingAlgorithm.Cycle:
                SwitchByCycle();
                break;
            case SwitchingAlgorithm.Toggle:
                SwitchByToggle();
                break;
        }
    }

    private void MoveSelectedLayoutToFront()
    {
        if (availableLayouts.Count < 2)
            return;
        IntPtr currentLayout = _layoutManager.GetCurrentLayout();
        int currentIndex = availableLayouts.IndexOf(currentLayout);
        if (currentIndex == -1)
            currentIndex = 0;
        availableLayouts.RemoveAt(currentIndex);
        availableLayouts.Insert(0, currentLayout);
    }

    private void TraceLayouts()
    {
        Console.WriteLine("Available layouts:");
        foreach (var layout in availableLayouts)
        {
            string layoutName = _layoutManager.GetLayoutName(layout);
            Trace.WriteLine(layoutName);
        }
    }

    private void SwitchByToggle()
    {
        if (availableLayouts.Count < 2)
            return;
        IntPtr currentLayout = _layoutManager.GetCurrentLayout();
        int currentIndex = availableLayouts.IndexOf(currentLayout);
        if (currentIndex == -1)
            currentIndex = 0;
        int targetIndex = (currentIndex == 0) ? 1 : 0;
        IntPtr targetLayout = availableLayouts[targetIndex];
        _layoutManager.ActivateLayout(targetLayout);
    }

    private void SwitchByCycle()
    {
        if (availableLayouts.Count < 2)
            return;
        IntPtr currentLayout = _layoutManager.GetCurrentLayout();
        int currentIndex = availableLayouts.IndexOf(currentLayout);
        if (currentIndex == -1)
            currentIndex = 0;
        currentIndex++;
        if (currentIndex >= availableLayouts.Count)
            currentIndex = 0;
        _layoutManager.ActivateLayout(availableLayouts[currentIndex]);
    }

    private void Exit()
    {
        _hotkeyManager.UnregisterHotkey(HOTKEY_ID_ALT_SHIFT);
        _systemTray.Hide();
        _systemTray.Dispose();
        Application.Exit();
    }

    public void Run()
    {
        Console.WriteLine("Keyboard Layout Switcher started.");
        Console.WriteLine("Hotkeys:");
        Console.WriteLine("  Alt+Shift (release quickly): Toggle between two most recent layouts");
        Console.WriteLine("  Alt+Shift (hold Shift): Cycle through all available layouts");
        Console.WriteLine("Right-click the tray icon for options.");
        Application.Run();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }
}