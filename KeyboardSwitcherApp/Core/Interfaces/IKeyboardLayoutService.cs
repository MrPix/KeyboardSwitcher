using KeyboardLayoutSwitcher.Core.Entities;

namespace KeyboardLayoutSwitcher.Core.Interfaces;

public interface IKeyboardLayoutService
{
    IEnumerable<KeyboardLayout> GetAvailableLayouts();
    KeyboardLayout? GetCurrentLayout();
    void ActivateLayout(KeyboardLayout layout);
    void RefreshLayouts();
} 