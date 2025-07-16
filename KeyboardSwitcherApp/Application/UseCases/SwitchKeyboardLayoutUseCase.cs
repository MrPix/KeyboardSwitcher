using KeyboardLayoutSwitcher.Domain.Entities;
using KeyboardLayoutSwitcher.Domain.Enums;
using KeyboardLayoutSwitcher.Domain.Interfaces;

namespace KeyboardLayoutSwitcher.Application.UseCases;

public class SwitchKeyboardLayoutUseCase
{
    private readonly IKeyboardLayoutRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly Dictionary<SwitchingAlgorithm, ILayoutSwitchingStrategy> _strategies;

    public SwitchKeyboardLayoutUseCase(
        IKeyboardLayoutRepository repository,
        INotificationService notificationService,
        IEnumerable<ILayoutSwitchingStrategy> strategies)
    {
        _repository = repository;
        _notificationService = notificationService;
        _strategies = new Dictionary<SwitchingAlgorithm, ILayoutSwitchingStrategy>();
        
        foreach (var strategy in strategies)
        {
            if (strategy is Domain.Services.ToggleLayoutStrategy)
                _strategies[SwitchingAlgorithm.Toggle] = strategy;
            else if (strategy is Domain.Services.CycleLayoutStrategy)
                _strategies[SwitchingAlgorithm.Cycle] = strategy;
        }
    }

    public async Task<bool> ExecuteAsync(SwitchingAlgorithm algorithm)
    {
        try
        {
            var layouts = await _repository.GetAvailableLayoutsAsync();
            var current = await _repository.GetCurrentLayoutAsync();

            if (!_strategies.TryGetValue(algorithm, out var strategy))
            {
                _notificationService.ShowError($"Strategy for {algorithm} not found");
                return false;
            }

            var nextLayout = await strategy.GetNextLayoutAsync(layouts, current);
            
            if (nextLayout == null)
            {
                _notificationService.ShowError("No suitable layout found to switch to");
                return false;
            }

            var success = await _repository.SetCurrentLayoutAsync(nextLayout);
            
            if (success)
            {
                _notificationService.ShowLayoutChanged(nextLayout);
            }
            else
            {
                _notificationService.ShowError("Failed to switch keyboard layout");
            }

            return success;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error switching layout: {ex.Message}");
            return false;
        }
    }
}