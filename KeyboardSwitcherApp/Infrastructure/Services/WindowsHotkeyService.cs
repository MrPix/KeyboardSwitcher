using System.Runtime.InteropServices;
using KeyboardLayoutSwitcher.Domain.Interfaces;

namespace KeyboardLayoutSwitcher.Infrastructure.Services;

public class WindowsHotkeyService : IHotkeyService, IDisposable
{
    // Windows API imports
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Form _hiddenForm;
    private bool _isDisposed;

    public event Action<int>? HotkeyPressed;

    public WindowsHotkeyService()
    {
        _hiddenForm = new HotkeyForm();
        ((HotkeyForm)_hiddenForm).HotkeyPressed += OnHotkeyPressed;
    }

    public bool RegisterHotkey(int id, uint modifiers, uint virtualKey)
    {
        return RegisterHotKey(_hiddenForm.Handle, id, modifiers, virtualKey);
    }

    public bool UnregisterHotkey(int id)
    {
        return UnregisterHotKey(_hiddenForm.Handle, id);
    }

    private void OnHotkeyPressed(int hotkeyId)
    {
        HotkeyPressed?.Invoke(hotkeyId);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _hiddenForm?.Dispose();
            _isDisposed = true;
        }
    }

    // Custom form class to handle WndProc
    private class HotkeyForm : Form
    {
        public event Action<int>? HotkeyPressed;

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
}