using KeyboardLayoutSwitcher.Domain.Entities;
using KeyboardLayoutSwitcher.Domain.Interfaces;

namespace KeyboardLayoutSwitcher.Infrastructure.Services;

public class SystemTrayNotificationService : INotificationService, IDisposable
{
    private readonly NotifyIcon _trayIcon;
    private bool _isDisposed;

    public SystemTrayNotificationService()
    {
        _trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Keyboard Layout Switcher"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, (s, e) => ExitRequested?.Invoke());
        _trayIcon.ContextMenuStrip = contextMenu;
    }

    public event Action? ExitRequested;

    public void ShowLayoutChanged(KeyboardLayout layout)
    {
        _trayIcon.Text = $"Keyboard Layout Switcher - {layout.DisplayName}";
        
        // Optional: Show balloon tip
        // _trayIcon.ShowBalloonTip(1000, "Layout Changed", 
        //     $"Switched to {layout.DisplayName}", ToolTipIcon.Info);
    }

    public void ShowError(string message)
    {
        _trayIcon.ShowBalloonTip(3000, "Error", message, ToolTipIcon.Error);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _isDisposed = true;
        }
    }
}