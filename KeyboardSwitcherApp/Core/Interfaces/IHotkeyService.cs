namespace KeyboardLayoutSwitcher.Core.Interfaces;

public interface IHotkeyService
{
    event Action<int> HotkeyPressed;
    void RegisterHotkey(int id, uint modifiers, uint key);
    void UnregisterHotkey(int id);
    void Start();
    void Stop();
} 