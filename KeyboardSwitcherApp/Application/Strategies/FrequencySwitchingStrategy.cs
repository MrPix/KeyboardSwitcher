using KeyboardLayoutSwitcher.Core.Entities;
using KeyboardLayoutSwitcher.Core.Interfaces;

namespace KeyboardLayoutSwitcher.Application.Strategies;

public class FrequencySwitchingStrategy : ISwitchingStrategy
{
    public KeyboardLayout? GetNextLayout(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? currentLayout)
    {
        var layoutList = layouts.ToList();
        if (!layoutList.Any())
            return null;

        if (currentLayout == null)
            return layoutList.First();

        // Find the most frequently used layout that's not the current one
        var mostFrequent = layoutList
            .Where(l => l.Handle != currentLayout.Handle)
            .OrderByDescending(l => l.UsageCount)
            .ThenByDescending(l => l.LastUsed)
            .FirstOrDefault();

        // If no frequent layout found, cycle to next
        if (mostFrequent == null)
        {
            var currentIndex = layoutList.FindIndex(l => l.Handle == currentLayout.Handle);
            if (currentIndex == -1)
                return layoutList.First();

            var nextIndex = (currentIndex + 1) % layoutList.Count;
            return layoutList[nextIndex];
        }

        return mostFrequent;
    }
} 