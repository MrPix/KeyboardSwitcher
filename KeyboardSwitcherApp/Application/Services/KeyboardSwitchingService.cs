using KeyboardLayoutSwitcher.Core.Entities;
using KeyboardLayoutSwitcher.Core.Enums;
using KeyboardLayoutSwitcher.Core.Interfaces;

namespace KeyboardLayoutSwitcher.Application.Services;

public class KeyboardSwitchingService
{
    private readonly IKeyboardLayoutService _layoutService;
    private readonly Dictionary<SwitchingAlgorithm, ISwitchingStrategy> _strategies;
    private SwitchingAlgorithm _currentAlgorithm = SwitchingAlgorithm.Toggle;

    public KeyboardSwitchingService(
        IKeyboardLayoutService layoutService,
        IEnumerable<ISwitchingStrategy> strategies)
    {
        _layoutService = layoutService;
        _strategies = strategies.ToDictionary(s => GetStrategyType(s));
    }

    public void SetAlgorithm(SwitchingAlgorithm algorithm)
    {
        _currentAlgorithm = algorithm;
    }

    public void SwitchLayout()
    {
        var layouts = _layoutService.GetAvailableLayouts().ToList();
        var currentLayout = _layoutService.GetCurrentLayout();

        if (!layouts.Any())
            return;

        if (_strategies.TryGetValue(_currentAlgorithm, out var strategy))
        {
            var nextLayout = strategy.GetNextLayout(layouts, currentLayout);
            if (nextLayout != null)
            {
                _layoutService.ActivateLayout(nextLayout);
                nextLayout.MarkAsUsed();
            }
        }
    }

    public IEnumerable<KeyboardLayout> GetAvailableLayouts()
    {
        return _layoutService.GetAvailableLayouts();
    }

    public KeyboardLayout? GetCurrentLayout()
    {
        return _layoutService.GetCurrentLayout();
    }

    private static SwitchingAlgorithm GetStrategyType(ISwitchingStrategy strategy)
    {
        return strategy.GetType().Name switch
        {
            "CycleSwitchingStrategy" => SwitchingAlgorithm.Cycle,
            "ToggleSwitchingStrategy" => SwitchingAlgorithm.Toggle,
            "MostRecentSwitchingStrategy" => SwitchingAlgorithm.MostRecent,
            "FrequencySwitchingStrategy" => SwitchingAlgorithm.Frequency,
            _ => SwitchingAlgorithm.Toggle
        };
    }
} 