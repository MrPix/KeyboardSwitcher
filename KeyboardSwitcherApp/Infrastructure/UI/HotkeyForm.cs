namespace KeyboardLayoutSwitcher.Infrastructure.UI;

// Custom form class to handle WndProc
public class HotkeyForm : Form
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