using KeyboardLayoutSwitcher.Core.Entities;
using KeyboardLayoutSwitcher.Core.Interfaces;

namespace KeyboardLayoutSwitcher.Application.Strategies;

public class MostRecentSwitchingStrategy : ISwitchingStrategy
{
    public KeyboardLayout? GetNextLayout(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? currentLayout)
    {
        var layoutList = layouts.ToList();
        if (!layoutList.Any())
            return null;

        if (currentLayout == null)
            return layoutList.First();

        // Find the most recently used layout that's not the current one
        var mostRecent = layoutList
            .Where(l => l.Handle != currentLayout.Handle)
            .OrderByDescending(l => l.LastUsed)
            .FirstOrDefault();

        // If no recent layout found, cycle to next
        if (mostRecent == null)
        {
            var currentIndex = layoutList.FindIndex(l => l.Handle == currentLayout.Handle);
            if (currentIndex == -1)
                return layoutList.First();

            var nextIndex = (currentIndex + 1) % layoutList.Count;
            return layoutList[nextIndex];
        }

        return mostRecent;
    }
} 