using KeyboardLayoutSwitcher.Domain.Entities;
using KeyboardLayoutSwitcher.Domain.Interfaces;

namespace KeyboardLayoutSwitcher.Domain.Services;

public class ToggleLayoutStrategy : ILayoutSwitchingStrategy
{
    public Task<KeyboardLayout?> GetNextLayoutAsync(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? current)
    {
        var layoutList = layouts.ToList();
        
        if (layoutList.Count < 2)
            return Task.FromResult<KeyboardLayout?>(null);

        var currentIndex = current != null ? layoutList.IndexOf(current) : -1;
        
        // If current layout is not in the list, fallback to first
        if (currentIndex == -1)
            currentIndex = 0;

        var targetIndex = (currentIndex == 0) ? 1 : 0;
        return Task.FromResult<KeyboardLayout?>(layoutList[targetIndex]);
    }
}

public class CycleLayoutStrategy : ILayoutSwitchingStrategy
{
    public Task<KeyboardLayout?> GetNextLayoutAsync(IEnumerable<KeyboardLayout> layouts, KeyboardLayout? current)
    {
        var layoutList = layouts.ToList();
        
        if (layoutList.Count < 2)
            return Task.FromResult<KeyboardLayout?>(null);

        var currentIndex = current != null ? layoutList.IndexOf(current) : -1;
        
        // If current layout is not in the list, fallback to first
        if (currentIndex == -1)
            currentIndex = 0;

        currentIndex++;
        if (currentIndex >= layoutList.Count)
        {
            currentIndex = 0; // Wrap around
        }
        
        return Task.FromResult<KeyboardLayout?>(layoutList[currentIndex]);
    }
}