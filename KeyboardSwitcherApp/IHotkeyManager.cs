namespace KeyboardLayoutSwitcher;

public interface IHotkeyManager
{
    void RegisterHotkey(int id, uint modifiers, uint key, Action<int> onHotkeyPressed);
    void UnregisterHotkey(int id);
}
