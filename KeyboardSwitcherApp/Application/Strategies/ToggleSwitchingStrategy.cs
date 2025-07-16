using KeyboardLayoutSwitcher.Core.Entities;
using KeyboardLayoutSwitcher.Core.Interfaces;

namespace KeyboardLayoutSwitcher.Application.Strategies;

public class ToggleSwitchingStrategy : ISwitchingStrategy
{
    private KeyboardLayout? _lastUsedLayout;

    public KeyboardLayout? GetNextLayout(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? currentLayout)
    {
        var layoutList = layouts.ToList();
        if (!layoutList.Any())
            return null;

        if (currentLayout == null)
        {
            _lastUsedLayout = layoutList.First();
            return _lastUsedLayout;
        }

        // If we have a last used layout and it's different from current, toggle to it
        if (_lastUsedLayout != null && _lastUsedLayout.Handle != currentLayout.Handle)
        {
            var temp = _lastUsedLayout;
            _lastUsedLayout = currentLayout;
            return temp;
        }

        // Otherwise, switch to the next layout in the list
        var currentIndex = layoutList.FindIndex(l => l.Handle == currentLayout.Handle);
        if (currentIndex == -1)
        {
            _lastUsedLayout = layoutList.First();
            return _lastUsedLayout;
        }

        var nextIndex = (currentIndex + 1) % layoutList.Count;
        _lastUsedLayout = currentLayout;
        return layoutList[nextIndex];
    }
} 