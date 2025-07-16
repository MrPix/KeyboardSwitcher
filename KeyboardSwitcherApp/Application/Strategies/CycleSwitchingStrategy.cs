using KeyboardLayoutSwitcher.Core.Entities;
using KeyboardLayoutSwitcher.Core.Interfaces;

namespace KeyboardLayoutSwitcher.Application.Strategies;

public class CycleSwitchingStrategy : ISwitchingStrategy
{
    public KeyboardLayout? GetNextLayout(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? currentLayout)
    {
        var layoutList = layouts.ToList();
        if (!layoutList.Any())
            return null;

        if (currentLayout == null)
            return layoutList.First();

        var currentIndex = layoutList.FindIndex(l => l.Handle == currentLayout.Handle);
        if (currentIndex == -1)
            return layoutList.First();

        var nextIndex = (currentIndex + 1) % layoutList.Count;
        return layoutList[nextIndex];
    }
} 