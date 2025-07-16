using KeyboardLayoutSwitcher.Core.Entities;

namespace KeyboardLayoutSwitcher.Core.Interfaces;

public interface ISwitchingStrategy
{
    KeyboardLayout? GetNextLayout(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? currentLayout);
} 