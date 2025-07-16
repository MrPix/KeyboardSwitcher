using KeyboardLayoutSwitcher.Application.Services;
using KeyboardLayoutSwitcher.Core.Interfaces;
using System.Windows.Forms;

namespace KeyboardLayoutSwitcher.Presentation.Forms;

public class MainApplication : IDisposable
{
    private readonly KeyboardSwitchingService _switchingService;
    private readonly IHotkeyService _hotkeyService;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private bool _disposed = false;

    public MainApplication(
        KeyboardSwitchingService switchingService,
        IHotkeyService hotkeyService)
    {
        _switchingService = switchingService;
        _hotkeyService = hotkeyService;

        // Create system tray icon
        _trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Keyboard Layout Switcher"
        };

        // Create context menu
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Switch Layout", null, (s, e) => SwitchLayout());
        _contextMenu.Items.Add("-"); // Separator
        _contextMenu.Items.Add("Exit", null, this.Exit);

        _trayIcon.ContextMenuStrip = _contextMenu;
        _trayIcon.DoubleClick += (s, e) => SwitchLayout();

        // Subscribe to hotkey events
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
    }

    public void Start()
    {
        _hotkeyService.Start();
    }

    public void Stop()
    {
        _hotkeyService.Stop();
    }

    private void OnHotkeyPressed(int hotkeyId)
    {
        SwitchLayout();
    }

    private void SwitchLayout()
    {
        _switchingService.SwitchLayout();
        
        // Show notification
        var currentLayout = _switchingService.GetCurrentLayout();
        if (currentLayout != null)
        {
            _trayIcon.ShowBalloonTip(1000, "Keyboard Layout", 
                $"Switched to: {currentLayout.Name}", ToolTipIcon.Info);
        }
    }

    private void Exit(object? sender, EventArgs e)
    {
        Stop();
        Dispose();
        System.Windows.Forms.Application.Exit();
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
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
            _disposed = true;
        }
    }
} 