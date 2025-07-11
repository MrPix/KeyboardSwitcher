using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardLayoutSwitcher
{
    // Custom form class to handle WndProc
    public class HotkeyForm : Form
    {
        public event Action<int> HotkeyPressed;

        public HotkeyForm()
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Visible = false;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                HotkeyPressed?.Invoke(hotkeyId);
            }

            base.WndProc(ref m);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }
    }

    public class KeyboardSwitcher
    {
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

        // Constants
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
        private const uint INPUTLANGCHANGE_SYSCHARSET = 0x0001;
        private const uint KLF_ACTIVATE = 0x00000001;

        // Hotkey IDs
        private const int HOTKEY_ID_TOGGLE = 1;
        private const int HOTKEY_ID_CYCLE = 2;

        private NotifyIcon trayIcon;
        private ContextMenuStrip contextMenu;
        private HotkeyForm hiddenForm;
        private List<IntPtr> availableLayouts;
        private List<IntPtr> recentLayouts = new List<IntPtr>(); // Track recently used layouts
        private int currentLayoutIndex = 0;
        private SwitchingAlgorithm algorithm = SwitchingAlgorithm.Toggle; // Default to toggle

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
            contextMenu.Items.Add("Current Layout", null, ShowCurrentLayout);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Cycle Algorithm", null, (s, e) => SetAlgorithm(SwitchingAlgorithm.Cycle));
            contextMenu.Items.Add("Toggle Algorithm", null, (s, e) => SetAlgorithm(SwitchingAlgorithm.Toggle));
            contextMenu.Items.Add("Most Recent Algorithm", null, (s, e) => SetAlgorithm(SwitchingAlgorithm.MostRecent));
            contextMenu.Items.Add(new ToolStripSeparator());
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

        private void RegisterHotkeys()
        {
            // Register Alt+Left Shift for toggle algorithm
            RegisterHotKey(hiddenForm.Handle, HOTKEY_ID_TOGGLE,
                MOD_ALT | MOD_SHIFT, (uint)Keys.LShiftKey);

            // Register Shift+Alt for cycle switching
            RegisterHotKey(hiddenForm.Handle, HOTKEY_ID_CYCLE,
                MOD_SHIFT | MOD_ALT, (uint)Keys.Menu); // Keys.Menu is the Alt key
        }

        private void OnHotkeyPressed(int hotkeyId)
        {
            switch (hotkeyId)
            {
                case HOTKEY_ID_TOGGLE:
                    SwitchWithToggleAlgorithm();
                    break;
                case HOTKEY_ID_CYCLE:
                    SwitchWithCycleAlgorithm();
                    break;
            }
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
                case SwitchingAlgorithm.MostRecent:
                    SwitchByMostRecent();
                    break;
                case SwitchingAlgorithm.Frequency:
                    SwitchByFrequency();
                    break;
            }
        }

        private void SwitchByCycle()
        {
            currentLayoutIndex = (currentLayoutIndex + 1) % availableLayouts.Count;
            IntPtr targetLayout = availableLayouts[currentLayoutIndex];
            ActivateLayout(targetLayout);
            UpdateRecentLayouts(targetLayout);
        }

        private void UpdateRecentLayouts(IntPtr layout)
        {
            // Remove if already in list
            recentLayouts.Remove(layout);

            // Add to front
            recentLayouts.Insert(0, layout);

            // Keep only last 2 layouts
            if (recentLayouts.Count > 2)
            {
                recentLayouts.RemoveAt(2);
            }
        }

        private void SwitchByToggle()
        {
            // Enhanced toggle that remembers the last two used layouts
            if (recentLayouts.Count == 0)
            {
                // First time - add current layout to recent list
                IntPtr currentLayout = GetCurrentLayout();
                if (currentLayout != IntPtr.Zero)
                {
                    recentLayouts.Add(currentLayout);
                }
            }

            if (availableLayouts.Count >= 2)
            {
                IntPtr targetLayout;

                if (recentLayouts.Count >= 2)
                {
                    // Toggle between the two most recent layouts
                    IntPtr currentLayout = GetCurrentLayout();
                    if (currentLayout == recentLayouts[0])
                    {
                        targetLayout = recentLayouts[1];
                    }
                    else
                    {
                        targetLayout = recentLayouts[0];
                    }
                }
                else
                {
                    // Fallback to simple toggle between first two available layouts
                    currentLayoutIndex = currentLayoutIndex == 0 ? 1 : 0;
                    targetLayout = availableLayouts[currentLayoutIndex];
                }

                ActivateLayout(targetLayout);
                UpdateRecentLayouts(targetLayout);
            }
        }

        private void SwitchByMostRecent()
        {
            // For simplicity, this cycles through layouts
            // In a full implementation, you'd track usage history
            SwitchByCycle();
        }

        private void SwitchByFrequency()
        {
            // For simplicity, this cycles through layouts
            // In a full implementation, you'd track usage frequency
            SwitchByCycle();
        }

        private void SwitchToNextLayout()
        {
            currentLayoutIndex = (currentLayoutIndex + 1) % availableLayouts.Count;
            ActivateLayout(availableLayouts[currentLayoutIndex]);
        }

        private void SwitchToPreviousLayout()
        {
            currentLayoutIndex = (currentLayoutIndex - 1 + availableLayouts.Count) % availableLayouts.Count;
            ActivateLayout(availableLayouts[currentLayoutIndex]);
        }

        private void ActivateLayout(IntPtr hkl)
        {
            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();

                if (foregroundWindow != IntPtr.Zero)
                {
                    // Send language change request to the foreground window
                    PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST,
                        new IntPtr(INPUTLANGCHANGE_SYSCHARSET), hkl);

                    string layoutName = GetLayoutName(hkl);
                    Console.WriteLine($"Switched to: {layoutName}");

                    // Update tray icon tooltip
                    trayIcon.Text = $"Keyboard Layout Switcher - {layoutName}";

                    // Show balloon tip
                    trayIcon.ShowBalloonTip(1000, "Layout Changed",
                        $"Switched to {layoutName}", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error switching layout: {ex.Message}");
            }
        }

        private void SetAlgorithm(SwitchingAlgorithm newAlgorithm)
        {
            algorithm = newAlgorithm;
            Console.WriteLine($"Switching algorithm set to: {algorithm}");

            // Update menu checkmarks
            foreach (ToolStripMenuItem item in contextMenu.Items.OfType<ToolStripMenuItem>())
            {
                if (item.Text.Contains("Algorithm"))
                {
                    item.Checked = item.Text.StartsWith(algorithm.ToString());
                }
            }
        }

        private void ShowCurrentLayout(object sender, EventArgs e)
        {
            IntPtr currentLayout = GetCurrentLayout();
            string layoutName = GetLayoutName(currentLayout);
            MessageBox.Show($"Current Layout: {layoutName}", "Keyboard Layout Info");
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
            // Unregister hotkeys
            UnregisterHotKey(hiddenForm.Handle, HOTKEY_ID_TOGGLE);
            UnregisterHotKey(hiddenForm.Handle, HOTKEY_ID_CYCLE);

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
            Console.WriteLine("  Alt+Left Shift: Toggle between two most recent layouts");
            Console.WriteLine("  Shift+Alt: Cycle through all available layouts");
            Console.WriteLine("Right-click the tray icon for options.");

            Application.Run();
        }
    }



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
}