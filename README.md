# ktsu.Navigation

A robust .NET library for implementing navigation stacks with undo/redo support and persistence capabilities.

## Overview

**ktsu.Navigation** provides a complete navigation stack implementation that supports:

- **Forward/Backward Navigation**: Navigate through items with full browser-like back/forward functionality
- **Undo/Redo Operations**: Complete undo/redo support for all navigation actions
- **Persistence**: Save and restore navigation state to various storage backends
- **Event-Driven Architecture**: React to navigation changes with comprehensive event notifications
- **Generic Design**: Works with any navigation item type implementing `INavigationItem`
- **Factory Pattern**: Easy creation of navigation stacks with different configurations

Perfect for applications that need robust navigation management, such as:
- Code editors with file navigation
- Image viewers with browsing history
- Document management systems
- Any application requiring navigation state management

## Features

### Core Navigation
- ✅ NavigateTo, GoBack, GoForward operations
- ✅ Current item tracking with CanGoBack/CanGoForward status
- ✅ Complete navigation history management
- ✅ Forward history clearing when navigating to new items
- ✅ Thread-safe operations

### Undo/Redo System
- ✅ Full undo/redo support for navigation operations
- ✅ Configurable history size limits
- ✅ State change notifications
- ✅ Error handling during undo/redo operations

### Persistence
- ✅ JSON file persistence provider
- ✅ In-memory persistence for testing
- ✅ Async operations with cancellation token support
- ✅ Custom persistence provider support

### Events & Extensibility
- ✅ Navigation change events (NavigateTo, GoBack, GoForward, Clear)
- ✅ Metadata support for navigation items
- ✅ Generic design for custom navigation item types
- ✅ Factory pattern for easy configuration

## Installation

Add the NuGet package:

```bash
dotnet add package ktsu.Navigation
```

## Quick Start

### Basic Navigation

```csharp
using ktsu.Navigation.Core.Models;
using ktsu.Navigation.Core.Services;

// Create navigation items
var page1 = new NavigationItem("page1", "Home Page");
var page2 = new NavigationItem("page2", "About Page");
var page3 = new NavigationItem("page3", "Contact Page");

// Create a basic navigation stack
var navigation = new Navigation<NavigationItem>();

// Navigate through items
navigation.NavigateTo(page1);  // Current: Home Page
navigation.NavigateTo(page2);  // Current: About Page
navigation.NavigateTo(page3);  // Current: Contact Page

// Go back and forward
var previous = navigation.GoBack();    // Current: About Page
var next = navigation.GoForward();     // Current: Contact Page

// Check navigation state
Console.WriteLine($"Current: {navigation.Current?.DisplayName}");
Console.WriteLine($"Can go back: {navigation.CanGoBack}");
Console.WriteLine($"Can go forward: {navigation.CanGoForward}");
```

### With Undo/Redo Support

```csharp
using ktsu.Navigation.Core.Services;

// Create navigation with undo/redo support
var undoRedoProvider = new SimpleUndoRedoProvider(maxHistorySize: 50);
var navigation = new Navigation<NavigationItem>(undoRedoProvider);

navigation.NavigateTo(page1);
navigation.NavigateTo(page2);

// Undo the last navigation
undoRedoProvider.Undo();  // Back to page1

// Redo the navigation
undoRedoProvider.Redo();  // Forward to page2 again
```

### With Persistence

```csharp
using ktsu.Navigation.Core.Services;

// Create navigation with JSON file persistence
var persistenceProvider = new JsonFilePersistenceProvider<NavigationItem>("navigation-state.json");
var navigation = new Navigation<NavigationItem>(persistenceProvider: persistenceProvider);

navigation.NavigateTo(page1);
navigation.NavigateTo(page2);

// Save navigation state
await navigation.SaveStateAsync();

// Later... restore navigation state
await navigation.LoadStateAsync();
Console.WriteLine($"Restored to: {navigation.Current?.DisplayName}");
```

### Complete Example with All Features

```csharp
using ktsu.Navigation.Core.Services;

// Create providers
var undoRedoProvider = new SimpleUndoRedoProvider();
var persistenceProvider = new JsonFilePersistenceProvider<NavigationItem>("app-navigation.json");

// Create navigation stack with all features
var navigation = new Navigation<NavigationItem>(undoRedoProvider, persistenceProvider);

// Subscribe to navigation events
navigation.NavigationChanged += (sender, e) =>
{
    Console.WriteLine($"Navigation: {e.NavigationType}");
    Console.WriteLine($"From: {e.PreviousItem?.DisplayName ?? "None"}");
    Console.WriteLine($"To: {e.CurrentItem?.DisplayName ?? "None"}");
};

// Use factory for easier creation
var factory = new NavigationStackFactory(undoRedoProvider);
var anotherNavigation = factory.CreateNavigationStack<NavigationItem>(persistenceProvider);
```

### Custom Navigation Items

```csharp
public class DocumentNavigationItem : INavigationItem
{
    public string Id { get; }
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();
    
    private readonly Dictionary<string, object> _metadata = new();
    
    public DocumentNavigationItem(string filePath, string displayName)
    {
        Id = filePath;
        DisplayName = displayName;
        CreatedAt = DateTime.UtcNow;
        SetMetadata("FilePath", filePath);
        SetMetadata("FileSize", new FileInfo(filePath).Length);
    }
    
    public void SetMetadata(string key, object value) => _metadata[key] = value;
    public bool RemoveMetadata(string key) => _metadata.Remove(key);
}

// Use with navigation
var navigation = new Navigation<DocumentNavigationItem>();
var document = new DocumentNavigationItem("/path/to/file.txt", "My Document");
navigation.NavigateTo(document);
```

## Architecture

The library follows clean architecture principles with well-separated concerns:

- **Models**: `NavigationItem`, `NavigationState` - Core data structures
- **Contracts**: Interfaces defining the contracts for all components
- **Services**: Implementation classes for navigation, persistence, and undo/redo

### Key Interfaces

- `INavigation<T>`: Main navigation stack operations
- `INavigationItem`: Contract for items that can be navigated to
- `IPersistenceProvider<T>`: Contract for persistence implementations
- `IUndoRedoProvider`: Contract for undo/redo implementations

## Extension Points

Create custom persistence providers:

```csharp
public class DatabasePersistenceProvider<T> : IPersistenceProvider<T> where T : INavigationItem
{
    public async Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default)
    {
        // Save to database
    }
    
    public async Task<INavigationState<T>?> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        // Load from database
        return null;
    }
    
    // Implement other interface methods...
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License. Copyright (c) ktsu.dev
