using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KeyboardLayoutSwitcher;

public class KeyboardSwitcher
{
    // Add at the top of the class (inside KeyboardSwitcher)
    private IntPtr hookId = IntPtr.Zero;
    private LowLevelKeyboardProc hookCallback;

    // Windows API for keyboard hook
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);


    // Windows API imports
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
    
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    // Import ActivateKeyboardLayout
    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    // Constants
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
    private const uint INPUTLANGCHANGE_SYSCHARSET = 0x0001;
    private const uint KLF_ACTIVATE = 0x00000001;
    private const int VK_SHIFT = 0x10;
    private const int LEFT_SHIFT = 0xA0;
    private const int LEFT_SHIFT_ALT = 0xA4;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    // Hotkey ID
    private const int HOTKEY_ID_ALT_SHIFT = 1;

    private NotifyIcon trayIcon;
    private ContextMenuStrip contextMenu;
    private HotkeyForm hiddenForm;
    private List<IntPtr> availableLayouts;
    private SwitchingAlgorithm algorithm = SwitchingAlgorithm.Toggle; // Default to toggle
    private bool isShiftReleased = true; // Track if Shift is released

    private System.Windows.Forms.Timer hotkeyReleaseTimer;
    private bool hotkeyActive = false;

    public enum SwitchingAlgorithm
    {
        Cycle,          // Cycle through all layouts
        Toggle,         // Toggle between two most recent
        MostRecent,     // Switch to most recently used
        Frequency       // Switch based on usage frequency
    }

    public KeyboardSwitcher()
    {
        InitializeComponent();
        LoadAvailableLayouts();
        SetupKeyboardHook();
        RegisterHotkeys();
    }

    private void InitializeComponent()
    {
        // Create hidden form to receive hotkey messages
        hiddenForm = new HotkeyForm();
        hiddenForm.HotkeyPressed += OnHotkeyPressed;

        // Create system tray icon
        trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Keyboard Layout Switcher"
        };

        // Create context menu
        contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, Exit);

        trayIcon.ContextMenuStrip = contextMenu;
        trayIcon.DoubleClick += (s, e) => SwitchKeyboardLayout();
    }

    private void LoadAvailableLayouts()
    {
        availableLayouts = new List<IntPtr>();

        // Get number of keyboard layouts
        int layoutCount = GetKeyboardLayoutList(0, null);
        IntPtr[] layouts = new IntPtr[layoutCount];

        // Get all keyboard layouts
        GetKeyboardLayoutList(layoutCount, layouts);
        availableLayouts.AddRange(layouts);

        Console.WriteLine($"Found {availableLayouts.Count} keyboard layouts:");
        for (int i = 0; i < availableLayouts.Count; i++)
        {
            string layoutName = GetLayoutName(availableLayouts[i]);
            Console.WriteLine($"  {i}: {layoutName}");
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

    // Call this in your constructor or InitializeComponent
    private void SetupKeyboardHook()
    {
        hookCallback = HookCallback;
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            hookId = SetWindowsHookEx(WH_KEYBOARD_LL, hookCallback, GetModuleHandle(curModule.ModuleName), 0);
        }
    }
    // Unhook when exiting
    private void RemoveKeyboardHook()
    {
        if (hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }
    }

    // The actual hook callback
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == LEFT_SHIFT)
            {
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    // Shift pressed
                    //Trace.WriteLine("Shift pressed");
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    // Shift released
                    //Trace.WriteLine("Shift released");
                    isShiftReleased = true; // Set flag for release
                    Trace.WriteLine("Shift released detected in hook callback.");
                    Trace.WriteLine(vkCode);
                }
            }
        }
        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private void RegisterHotkeys()
    {
        // Register Alt+Shift combination
        RegisterHotKey(hiddenForm.Handle, HOTKEY_ID_ALT_SHIFT, MOD_SHIFT | MOD_ALT, (uint)Keys.Menu); // Keys.Menu is the Alt key
    }

    private void OnHotkeyPressed(int hotkeyId)
    {
        if (hotkeyId == HOTKEY_ID_ALT_SHIFT)
        {
            hotkeyActive = true;
            if (isShiftReleased)
            {
                isShiftReleased = false; // Reset flag after handling
                Trace.WriteLine("Shift released state reset.");
                // Shift was released - toggle between recent layouts
                SwitchWithToggleAlgorithm();
                Trace.WriteLine("Using toggle algorithm for Alt+Shift hotkey.");
            }
            else
            {
                // Shift is still held - cycle through layouts
                SwitchWithCycleAlgorithm();
                Trace.WriteLine("Using cycle algorithm for Alt+Shift hotkey.");
            }
            StartHotkeyReleaseTimer();
        }
    }

    private void StartHotkeyReleaseTimer()
    {
        if (hotkeyReleaseTimer == null)
        {
            hotkeyReleaseTimer = new System.Windows.Forms.Timer();
            hotkeyReleaseTimer.Interval = 200; // ms
            hotkeyReleaseTimer.Tick += (s, e) =>
            {
                // Check if Shift and Alt are up
                bool shiftUp = (GetAsyncKeyState(VK_SHIFT) & 0x8000) == 0;
                bool altUp = (GetAsyncKeyState((int)Keys.Menu) & 0x8000) == 0;

                if (shiftUp && hotkeyActive)
                {
                    isShiftReleased = true;
                    // Simulate hotkey release event here
                    Trace.WriteLine("Shift released by timer.");
                    hotkeyReleaseTimer.Stop();
                }
                if (shiftUp && altUp && hotkeyActive)
                {
                    // Reset hotkey active state
                    hotkeyActive = false;
                    MoveSelectedLayoutToFront();
                    TraceLayouts();
                    Trace.WriteLine("Hotkey released - moving selected layout to front.");
                }
            };
        }
        hotkeyReleaseTimer.Stop();
        hotkeyReleaseTimer.Start();
    }

    private void SwitchWithToggleAlgorithm()
    {
        // Always use toggle algorithm for this hotkey
        SwitchByToggle();
    }

    private void SwitchWithCycleAlgorithm()
    {
        // Always use cycle algorithm for this hotkey
        SwitchByCycle();
    }

    private void SwitchKeyboardLayout()
    {
        // Use the currently selected algorithm
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
        IntPtr currentLayout = GetCurrentLayout();
        int currentIndex = availableLayouts.IndexOf(currentLayout);
        // If current layout is not in the list, fallback to first
        if (currentIndex == -1)
            currentIndex = 0;
        // Move the selected layout to the front
        availableLayouts.RemoveAt(currentIndex);
        availableLayouts.Insert(0, currentLayout);
    }

    private void TraceLayouts()
    {
        Console.WriteLine("Available layouts:");
        foreach (var layout in availableLayouts)
        {
            string layoutName = GetLayoutName(layout);
            Trace.WriteLine($" {layoutName} - {layout:X8}");
            //Trace.WriteLine(layoutName);
        }
    }

    // Switch to the second layout in the list, then move it to the front
    private void SwitchByToggle()
    {
        if (availableLayouts.Count < 2)
            return;

        IntPtr currentLayout = GetCurrentLayout();
        int currentIndex = availableLayouts.IndexOf(currentLayout);

        // If current layout is not in the list, fallback to first
        if (currentIndex == -1)
            currentIndex = 0;

        int targetIndex = (currentIndex == 0) ? 1 : 0;
        IntPtr targetLayout = availableLayouts[targetIndex];

        ActivateLayout(targetLayout);

        // Move the selected layout to the front
        //availableLayouts.Remove(targetLayout);
        //availableLayouts.Insert(0, targetLayout);
    }

    // Move the current layout to the end, activate the new first layout
    private void SwitchByCycle()
    {
        if (availableLayouts.Count < 2)
            return;

        IntPtr currentLayout = GetCurrentLayout();
        int currentIndex = availableLayouts.IndexOf(currentLayout);

        // If current layout is not in the list, fallback to first
        if (currentIndex == -1)
            currentIndex = 0;

        currentIndex++;
        if (currentIndex >= availableLayouts.Count)
        {
            currentIndex = 0; // Wrap around
        }
        ActivateLayout(availableLayouts[currentIndex]);
    }

    private void ActivateLayout(IntPtr hkl)
    {
        try
        {
            IntPtr currentLayout = GetCurrentLayout();
            string currentLayoutName = GetLayoutName(currentLayout);
            Trace.WriteLine($"Current layout: {currentLayoutName}");

            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return;
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            uint currentThreadId = GetCurrentThreadId();
            bool attached = AttachThreadInput(currentThreadId, foregroundThreadId, true);
            try
            {
                ActivateKeyboardLayout(hkl, KLF_ACTIVATE);
            }
            finally
            {
                if (attached)
                    AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }


            string layoutName = GetLayoutName(hkl);
            Console.WriteLine($"Switched to: {layoutName}");

            // Update tray icon tooltip
            trayIcon.Text = $"Keyboard Layout Switcher - {layoutName}";

            // Show balloon tip
            /*trayIcon.ShowBalloonTip(1000, "Layout Changed",
                $"Switched to {layoutName}", ToolTipIcon.Info);*/
            Trace.WriteLine($"Switched to layout: {layoutName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error switching layout: {ex.Message}");
        }
    }

    private IntPtr GetCurrentLayout()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            uint threadId = GetWindowThreadProcessId(foregroundWindow, out uint processId);
            return GetKeyboardLayout(threadId);
        }
        return IntPtr.Zero;
    }

    private void Exit(object sender, EventArgs e)
    {
        // Unregister hotkey
        UnregisterHotKey(hiddenForm.Handle, HOTKEY_ID_ALT_SHIFT);
        RemoveKeyboardHook();

        // Clean up
        trayIcon.Visible = false;
        trayIcon.Dispose();
        hiddenForm.Dispose();

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
}