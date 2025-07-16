using KeyboardLayoutSwitcher.Application.Services;
using KeyboardLayoutSwitcher.Application.UseCases;
using KeyboardLayoutSwitcher.Domain.Interfaces;
using KeyboardLayoutSwitcher.Domain.Services;
using KeyboardLayoutSwitcher.Infrastructure.Repositories;
using KeyboardLayoutSwitcher.Infrastructure.Services;
using WinFormsApp = System.Windows.Forms.Application;

namespace KeyboardLayoutSwitcher.Presentation;

public class ApplicationBootstrapper : IDisposable
{
    private readonly IKeyboardLayoutService _keyboardLayoutService;
    private readonly SystemTrayNotificationService _notificationService;
    private bool _isDisposed;

    public ApplicationBootstrapper()
    {
        // Infrastructure layer
        var keyboardLayoutRepository = new WindowsKeyboardLayoutRepository();
        var hotkeyService = new WindowsHotkeyService();
        _notificationService = new SystemTrayNotificationService();

        // Domain layer
        var strategies = new List<ILayoutSwitchingStrategy>
        {
            new ToggleLayoutStrategy(),
            new CycleLayoutStrategy()
        };

        // Application layer
        var switchLayoutUseCase = new SwitchKeyboardLayoutUseCase(
            keyboardLayoutRepository,
            _notificationService,
            strategies);

        _keyboardLayoutService = new KeyboardLayoutService(
            keyboardLayoutRepository,
            hotkeyService,
            switchLayoutUseCase);

        // Wire up events
        _notificationService.ExitRequested += OnExitRequested;
    }

    public async Task StartAsync()
    {
        Console.WriteLine("Keyboard Layout Switcher started.");
        Console.WriteLine("Hotkeys:");
        Console.WriteLine("  Alt+Shift: Toggle between layouts");
        Console.WriteLine("Right-click the tray icon for options.");

        await _keyboardLayoutService.StartAsync();
    }

    public async Task StopAsync()
    {
        await _keyboardLayoutService.StopAsync();
    }

    private void OnExitRequested()
    {
        WinFormsApp.Exit();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _keyboardLayoutService?.Dispose();
            _notificationService?.Dispose();
            _isDisposed = true;
        }
    }
}