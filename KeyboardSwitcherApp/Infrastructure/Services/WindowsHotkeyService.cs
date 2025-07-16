using System.Diagnostics;
using System.Runtime.InteropServices;
using KeyboardLayoutSwitcher.Core.Interfaces;
using KeyboardLayoutSwitcher.Infrastructure.UI;

namespace KeyboardLayoutSwitcher.Infrastructure.Services;

public class WindowsHotkeyService : IHotkeyService, IDisposable
{
    private readonly HotkeyForm _hiddenForm;
    private bool _disposed = false;

    // Windows API imports
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    // Constants
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const int VK_SHIFT = 0x10;
    private const int LEFT_SHIFT = 0xA0;
    private const int LEFT_SHIFT_ALT = 0xA4;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    // Hotkey ID
    private const int HOTKEY_ID_ALT_SHIFT = 1;

    // Keyboard hook
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc _hookCallback = null!;
    private bool _isShiftReleased = true;
    private System.Windows.Forms.Timer? _hotkeyReleaseTimer;
    private bool _hotkeyActive = false;

    // Delegate for keyboard hook
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public event Action<int>? HotkeyPressed;

    public WindowsHotkeyService()
    {
        _hiddenForm = new HotkeyForm();
        _hiddenForm.HotkeyPressed += OnHotkeyPressed;
        SetupKeyboardHook();
    }

    public void RegisterHotkey(int id, uint modifiers, uint key)
    {
        RegisterHotKey(_hiddenForm.Handle, id, modifiers, key);
    }

    public void UnregisterHotkey(int id)
    {
        UnregisterHotKey(_hiddenForm.Handle, id);
    }

    public void Start()
    {
        // Register Alt+Shift combination
        RegisterHotkey(HOTKEY_ID_ALT_SHIFT, MOD_SHIFT | MOD_ALT, (uint)Keys.Menu);
    }

    public void Stop()
    {
        UnregisterHotkey(HOTKEY_ID_ALT_SHIFT);
        RemoveKeyboardHook();
    }

    private void SetupKeyboardHook()
    {
        _hookCallback = HookCallback;
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            if (curModule?.ModuleName != null)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
    }

    private void RemoveKeyboardHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == LEFT_SHIFT)
            {
                if (wParam == (IntPtr)WM_KEYUP)
                {
                    _isShiftReleased = true;
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void OnHotkeyPressed(int hotkeyId)
    {
        if (hotkeyId == HOTKEY_ID_ALT_SHIFT)
        {
            _hotkeyActive = true;
            if (_isShiftReleased)
            {
                _isShiftReleased = false;
                HotkeyPressed?.Invoke(hotkeyId);
            }
            StartHotkeyReleaseTimer();
        }
    }

    private void StartHotkeyReleaseTimer()
    {
        if (_hotkeyReleaseTimer == null)
        {
            _hotkeyReleaseTimer = new System.Windows.Forms.Timer();
            _hotkeyReleaseTimer.Interval = 200;
            _hotkeyReleaseTimer.Tick += (s, e) =>
            {
                bool shiftUp = (GetAsyncKeyState(VK_SHIFT) & 0x8000) == 0;
                bool altUp = (GetAsyncKeyState((int)Keys.Menu) & 0x8000) == 0;

                if (shiftUp && _hotkeyActive)
                {
                    _hotkeyActive = false;
                    _hotkeyReleaseTimer.Stop();
                }
            };
        }

        if (!_hotkeyReleaseTimer.Enabled)
        {
            _hotkeyReleaseTimer.Start();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Stop();
            _hiddenForm?.Dispose();
            _hotkeyReleaseTimer?.Dispose();
            _disposed = true;
        }
    }
} 