# Architecture Overview

The Navigation Stack Library follows a clean, modular architecture based on SOLID principles and the Contracts, Models, Services (CMS) pattern.

## Architectural Patterns

### Contracts, Models, Services (CMS)

The library is organized into three main layers:

```
Navigation.Core/
├── Contracts/          # Interfaces and abstractions
├── Models/            # Data structures and entities
└── Services/          # Business logic and implementations
```

#### Contracts Layer

-   **Purpose**: Defines abstractions and contracts
-   **Contains**: Interfaces, delegates, enums
-   **Dependencies**: None (pure abstractions)

Key interfaces:

-   `INavigationStack<T>` - Core navigation functionality
-   `INavigationItem` - Contract for navigable items
-   `IUndoRedoProvider` - External undo/redo integration
-   `IPersistenceProvider<T>` - Persistence abstraction
-   `INavigationState<T>` - Serializable state representation

#### Models Layer

-   **Purpose**: Data structures and domain entities
-   **Contains**: POCOs, value objects, data transfer objects
-   **Dependencies**: Contracts only

Key models:

-   `NavigationItem` - Base implementation of navigation items
-   `NavigationState<T>` - Serializable navigation state
-   `NavigationEventArgs<T>` - Event data for navigation changes

#### Services Layer

-   **Purpose**: Business logic and concrete implementations
-   **Contains**: Core algorithms, default implementations, utilities
-   **Dependencies**: Contracts and Models

Key services:

-   `NavigationStack<T>` - Core navigation implementation
-   `SimpleUndoRedoProvider` - Built-in undo/redo functionality
-   `InMemoryPersistenceProvider<T>` - Memory-based persistence
-   `JsonFilePersistenceProvider<T>` - File-based persistence
-   `NavigationStackFactory` - Factory for creating configured instances

## SOLID Principles Implementation

### Single Responsibility Principle (SRP)

Each class has a single, well-defined responsibility:

-   `NavigationStack<T>` - Manages navigation state and operations
-   `SimpleUndoRedoProvider` - Handles undo/redo operations
-   `JsonFilePersistenceProvider<T>` - Manages JSON file persistence
-   `NavigationItem` - Represents a single navigation item

### Open/Closed Principle (OCP)

The library is open for extension but closed for modification:

-   New persistence providers can be added by implementing `IPersistenceProvider<T>`
-   Custom undo/redo providers can be created via `IUndoRedoProvider`
-   Navigation items can be extended by implementing `INavigationItem`

### Liskov Substitution Principle (LSP)

All implementations can be substituted for their abstractions:

-   Any `IPersistenceProvider<T>` implementation works with `NavigationStack<T>`
-   Any `IUndoRedoProvider` implementation integrates seamlessly
-   Custom `INavigationItem` implementations are fully supported

### Interface Segregation Principle (ISP)

Interfaces are focused and minimal:

-   `INavigationItem` contains only navigation-relevant members
-   `IPersistenceProvider<T>` focuses solely on persistence operations
-   `IUndoRedoProvider` contains only undo/redo functionality

### Dependency Inversion Principle (DIP)

High-level modules depend on abstractions:

-   `NavigationStack<T>` depends on `IUndoRedoProvider` and `IPersistenceProvider<T>`
-   No direct dependencies on concrete implementations
-   Factory pattern enables dependency injection

## Core Components

### Navigation Stack

The central component managing navigation state:

```csharp
public class NavigationStack<T> : INavigationStack<T> where T : INavigationItem
{
    private readonly List<T> _items;
    private readonly IUndoRedoProvider? _undoRedoProvider;
    private readonly IPersistenceProvider<T>? _persistenceProvider;
    private int _currentIndex;

    // Implementation...
}
```

**Key Features:**

-   Generic type parameter for flexibility
-   Optional provider dependencies
-   Thread-safe event handling
-   Immutable history views

### Provider Architecture

External integrations are handled through provider interfaces:

```csharp
// Undo/Redo Provider
public interface IUndoRedoProvider
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void RegisterAction(IUndoableAction action, string description);
    bool Undo();
    bool Redo();
}

// Persistence Provider
public interface IPersistenceProvider<T> where T : INavigationItem
{
    Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken);
    Task<INavigationState<T>?> LoadStateAsync(CancellationToken cancellationToken);
    Task<bool> HasSavedStateAsync(CancellationToken cancellationToken);
    Task ClearSavedStateAsync(CancellationToken cancellationToken);
}
```

### Event System

Type-safe event notifications for navigation changes:

```csharp
public delegate void NavigationEventHandler<T>(object sender, NavigationEventArgs<T> e)
    where T : INavigationItem;

public class NavigationEventArgs<T> : EventArgs where T : INavigationItem
{
    public NavigationType NavigationType { get; }
    public T? PreviousItem { get; }
    public T? CurrentItem { get; }
}
```

## Data Flow

### Navigation Operation Flow

1. User calls `NavigateTo(item)`
2. `NavigationStack<T>` validates the operation
3. Undo action is created and registered with provider
4. Navigation state is updated
5. Events are raised to notify listeners
6. State can be persisted asynchronously

### Undo/Redo Flow

1. Undo/Redo provider receives action registration
2. Provider manages action history and state
3. When undo/redo is triggered, provider calls action methods
4. Actions restore previous navigation state
5. Navigation events are raised for state changes

### Persistence Flow

1. Navigation state changes trigger save operations
2. State is serialized to `INavigationState<T>`
3. Persistence provider handles storage mechanism
4. On application restart, state can be restored
5. Restored state rebuilds navigation stack

## Extension Points

### Custom Navigation Items

```csharp
public class CustomNavigationItem : INavigationItem
{
    // Custom properties and behavior
    public string CustomProperty { get; set; }

    // Required INavigationItem implementation
    public string Id { get; }
    public string DisplayName { get; set; }
    // ... other required members
}
```

### Custom Persistence Providers

```csharp
public class DatabasePersistenceProvider<T> : IPersistenceProvider<T>
    where T : INavigationItem
{
    // Database-specific persistence implementation
}
```

### Custom Undo/Redo Providers

```csharp
public class ExternalUndoRedoProvider : IUndoRedoProvider
{
    // Integration with external undo/redo systems
}
```

## Performance Considerations

-   **Memory Efficiency**: Uses indexed lists for O(1) navigation operations
-   **Event Optimization**: Minimal allocations in event handling
-   **Lazy Loading**: Persistence operations are async and on-demand
-   **History Management**: Configurable history limits prevent memory leaks
-   **Immutable Views**: History views are read-only to prevent corruption

## Thread Safety

-   **Navigation Operations**: Not thread-safe by design for performance
-   **Event Handling**: Thread-safe event invocation
-   **Provider Calls**: Providers responsible for their own thread safety
-   **Recommended Pattern**: Use single-threaded access or external synchronization
