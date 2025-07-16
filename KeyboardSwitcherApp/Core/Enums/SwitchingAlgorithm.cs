namespace KeyboardLayoutSwitcher.Core.Enums;

public enum SwitchingAlgorithm
{
    Cycle,          // Cycle through all layouts
    Toggle,         // Toggle between two most recent
    MostRecent,     // Switch to most recently used
    Frequency       // Switch based on usage frequency
} 