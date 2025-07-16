using KeyboardLayoutSwitcher.Domain.Entities;
using KeyboardLayoutSwitcher.Domain.Interfaces;

namespace KeyboardLayoutSwitcher.Application.Services;

public interface IKeyboardLayoutService
{
    Task<IEnumerable<KeyboardLayout>> GetAvailableLayoutsAsync();
    Task<KeyboardLayout?> GetCurrentLayoutAsync();
    Task StartAsync();
    Task StopAsync();
    void Dispose();
}

public class KeyboardLayoutService : IKeyboardLayoutService, IDisposable
{
    private readonly IKeyboardLayoutRepository _repository;
    private readonly IHotkeyService _hotkeyService;
    private readonly UseCases.SwitchKeyboardLayoutUseCase _switchLayoutUseCase;
    private readonly Domain.Enums.SwitchingAlgorithm _defaultAlgorithm;
    private bool _isDisposed;

    public KeyboardLayoutService(
        IKeyboardLayoutRepository repository,
        IHotkeyService hotkeyService,
        UseCases.SwitchKeyboardLayoutUseCase switchLayoutUseCase)
    {
        _repository = repository;
        _hotkeyService = hotkeyService;
        _switchLayoutUseCase = switchLayoutUseCase;
        _defaultAlgorithm = Domain.Enums.SwitchingAlgorithm.Toggle;
    }

    public async Task<IEnumerable<KeyboardLayout>> GetAvailableLayoutsAsync()
    {
        return await _repository.GetAvailableLayoutsAsync();
    }

    public async Task<KeyboardLayout?> GetCurrentLayoutAsync()
    {
        return await _repository.GetCurrentLayoutAsync();
    }

    public Task StartAsync()
    {
        const int HOTKEY_ID_ALT_SHIFT = 1;
        const uint MOD_ALT = 0x0001;
        const uint MOD_SHIFT = 0x0004;

        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.RegisterHotkey(HOTKEY_ID_ALT_SHIFT, MOD_SHIFT | MOD_ALT, (uint)Keys.Menu);

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        const int HOTKEY_ID_ALT_SHIFT = 1;
        _hotkeyService.UnregisterHotkey(HOTKEY_ID_ALT_SHIFT);
        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;

        return Task.CompletedTask;
    }

    private async void OnHotkeyPressed(int hotkeyId)
    {
        const int HOTKEY_ID_ALT_SHIFT = 1;
        
        if (hotkeyId == HOTKEY_ID_ALT_SHIFT)
        {
            await _switchLayoutUseCase.ExecuteAsync(_defaultAlgorithm);
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            StopAsync().Wait();
            _hotkeyService?.Dispose();
            _isDisposed = true;
        }
    }
}