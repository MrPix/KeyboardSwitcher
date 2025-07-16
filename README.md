# KeyboardSwitcher

KeyboardSwitcher is a Windows utility that allows you to quickly switch between installed keyboard layouts using customizable hotkeys. It runs in the system tray and provides several switching algorithms, such as cycling through all layouts or toggling between the two most recent ones.

## Features
- Switch keyboard layouts with Alt+Shift hotkey
- Cycle or toggle between layouts
- System tray icon with context menu
- .NET 8, C# 12

## Limitations
- Does not support Microsoft Teams and other Electron apps

## Usage
- Alt+Shift (release quickly): Toggle between two most recent layouts
- Alt+Shift (hold Shift): Cycle through all available layouts
- Right-click the tray icon for more options

# Keyboard Layout Switcher - Clean Architecture

This application has been refactored to follow Clean Architecture principles, providing better separation of concerns, testability, and maintainability.

## Architecture Overview

The application is organized into the following layers:

### 1. Domain Layer (`Domain/`)
Contains the core business logic and entities, independent of any external concerns.

- **Entities**: `KeyboardLayout` - Core business entity representing a keyboard layout
- **Enums**: `SwitchingAlgorithm` - Defines different layout switching strategies
- **Interfaces**: Core abstractions for repositories and services
- **Services**: `ToggleLayoutStrategy`, `CycleLayoutStrategy` - Business logic for switching algorithms

### 2. Application Layer (`Application/`)
Contains use cases and application-specific business rules.

- **UseCases**: `SwitchKeyboardLayoutUseCase` - Orchestrates keyboard layout switching
- **Services**: `KeyboardLayoutService` - Application service coordinating operations

### 3. Infrastructure Layer (`Infrastructure/`)
Contains implementations of external concerns like Windows API calls and system tray.

- **Repositories**: `WindowsKeyboardLayoutRepository` - Windows-specific keyboard layout operations
- **Services**: 
  - `WindowsHotkeyService` - Windows hotkey registration and handling
  - `SystemTrayNotificationService` - System tray icon and notifications

### 4. Presentation Layer (`Presentation/`)
Contains the application startup and dependency injection configuration.

- **ApplicationBootstrapper** - Configures dependencies and starts the application

## Key Benefits of Clean Architecture

1. **Separation of Concerns**: Each layer has a single responsibility
2. **Dependency Inversion**: Higher layers depend on abstractions, not concrete implementations
3. **Testability**: Business logic can be tested independently of UI and external dependencies
4. **Maintainability**: Changes in one layer don't affect others
5. **Flexibility**: Easy to swap implementations (e.g., different notification systems)

## Dependencies Flow
Presentation ? Application ? Domain
    ?
Infrastructure ? Domain (through interfaces)
The Domain layer has no dependencies on other layers. All dependencies point inward toward the Domain layer.

## Usage

The application starts with the `ApplicationBootstrapper` which:
1. Creates all necessary dependencies
2. Wires up the services
3. Starts the keyboard layout monitoring
4. Manages the system tray icon

Hotkeys:
- **Alt+Shift**: Toggle between keyboard layouts

Right-click the system tray icon to exit the application.

## Extending the Application

To add new features:

1. **New switching algorithm**: Implement `ILayoutSwitchingStrategy` in the Domain layer
2. **Different notification system**: Implement `INotificationService` in the Infrastructure layer
3. **New hotkey combinations**: Extend the `IHotkeyService` interface and implementation
4. **Additional use cases**: Add new use case classes in the Application layer

The clean architecture makes it easy to extend functionality without modifying existing code.