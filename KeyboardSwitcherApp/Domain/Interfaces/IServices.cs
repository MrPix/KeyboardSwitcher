using KeyboardLayoutSwitcher.Domain.Entities;

namespace KeyboardLayoutSwitcher.Domain.Interfaces;

public interface IKeyboardLayoutRepository
{
    Task<IEnumerable<KeyboardLayout>> GetAvailableLayoutsAsync();
    Task<KeyboardLayout?> GetCurrentLayoutAsync();
    Task<bool> SetCurrentLayoutAsync(KeyboardLayout layout);
}

public interface ILayoutSwitchingStrategy
{
    Task<KeyboardLayout?> GetNextLayoutAsync(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? current);
}

public interface INotificationService
{
    void ShowLayoutChanged(KeyboardLayout layout);
    void ShowError(string message);
}

public interface IHotkeyService
{
    event Action<int>? HotkeyPressed;
    bool RegisterHotkey(int id, uint modifiers, uint virtualKey);
    bool UnregisterHotkey(int id);
    void Dispose();
}