using KeyboardLayoutSwitcher.Application.Services;
using KeyboardLayoutSwitcher.Application.Strategies;
using KeyboardLayoutSwitcher.Core.Interfaces;
using KeyboardLayoutSwitcher.Infrastructure.Services;
using KeyboardLayoutSwitcher.Presentation.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace KeyboardLayoutSwitcher.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeyboardSwitcherServices(this IServiceCollection services)
    {
        // Core services
        services.AddScoped<IKeyboardLayoutService, WindowsKeyboardLayoutService>();
        services.AddScoped<IHotkeyService, WindowsHotkeyService>();

        // Application services
        services.AddScoped<KeyboardSwitchingService>();
        services.AddScoped<MainApplication>();

        // Strategies
        services.AddScoped<ISwitchingStrategy, CycleSwitchingStrategy>();
        services.AddScoped<ISwitchingStrategy, ToggleSwitchingStrategy>();
        services.AddScoped<ISwitchingStrategy, MostRecentSwitchingStrategy>();
        services.AddScoped<ISwitchingStrategy, FrequencySwitchingStrategy>();

        return services;
    }
} 