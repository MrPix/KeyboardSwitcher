using System.Windows.Forms;

namespace KeyboardLayoutSwitcher;

public class WindowsSystemTray : ISystemTray
{
    private NotifyIcon _trayIcon;
    private ContextMenuStrip _contextMenu;

    public void SetIcon(string tooltip, Action onDoubleClick)
    {
        _trayIcon = new NotifyIcon()
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = tooltip
        };
        _trayIcon.DoubleClick += (s, e) => onDoubleClick();
    }

    public void SetContextMenu(IEnumerable<(string label, Action onClick)> items)
    {
        _contextMenu = new ContextMenuStrip();
        foreach (var (label, onClick) in items)
        {
            _contextMenu.Items.Add(label, null, (s, e) => onClick());
        }
        _trayIcon.ContextMenuStrip = _contextMenu;
    }

    public void Show() => _trayIcon.Visible = true;
    public void Hide() => _trayIcon.Visible = false;
    public void Dispose() { _trayIcon?.Dispose(); _contextMenu?.Dispose(); }
    public void UpdateTooltip(string tooltip) { if (_trayIcon != null) _trayIcon.Text = tooltip; }
}
