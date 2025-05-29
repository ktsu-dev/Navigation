# Design Decisions

This document explains the key design decisions and architectural choices made in the Navigation Stack Library.

## Core Design Principles

### 1. SOLID Principles

#### Single Responsibility Principle (SRP)

-   **INavigationStack**: Manages only navigation operations
-   **IUndoRedoProvider**: Handles only undo/redo functionality
-   **IPersistenceProvider**: Responsible only for state persistence
-   **INavigationItem**: Represents only navigation data

#### Open/Closed Principle (OCP)

-   Interfaces allow extension without modification
-   Custom persistence providers can be added without changing core code
-   External undo/redo systems can be integrated through the provider pattern

#### Liskov Substitution Principle (LSP)

-   All implementations of interfaces are interchangeable
-   `NavigationItem` can be replaced with custom implementations
-   Providers can be swapped without affecting navigation behavior

#### Interface Segregation Principle (ISP)

-   Small, focused interfaces prevent dependency on unused functionality
-   `INavigationItem` doesn't include navigation logic
-   Persistence and undo/redo are separate concerns

#### Dependency Inversion Principle (DIP)

-   High-level modules depend on abstractions, not concretions
-   Navigation stack depends on provider interfaces, not implementations
-   Dependency injection enables flexible configuration

### 2. Contracts, Models, Services (CMS) Architecture

#### Why CMS over Traditional Layered Architecture?

-   **Clearer separation of concerns**: Contracts define what the system does, Models define data structures, Services implement behavior
-   **Better testability**: Each layer can be tested independently
-   **Reduced coupling**: Services depend on contracts, not other services
-   **Enhanced maintainability**: Changes to implementation don't affect contracts

#### Benefits over MVC/MVP/MVVM

-   **Framework agnostic**: Can be used in any presentation pattern
-   **Focused responsibility**: Each layer has a clear, single responsibility
-   **Easier to understand**: Straightforward mapping from business concepts to code structure

## Interface Design Decisions

### Generic Type Constraints

```csharp
public interface INavigationStack<T> where T : INavigationItem
```

**Decision**: Use generic type constraints for navigation items.

**Rationale**:

-   Type safety at compile time
-   Allows specialized navigation items for different scenarios
-   Enables strong typing in persistence and events
-   Prevents runtime type errors

**Alternative Considered**: Using `object` or non-generic interfaces
**Why Rejected**: Would lose type safety and require runtime casting

### Event-Driven Architecture

```csharp
public event NavigationEventHandler<T>? NavigationChanged;
```

**Decision**: Use events for navigation notifications.

**Rationale**:

-   Loose coupling between navigation and UI
-   Multiple subscribers can react to navigation changes
-   Standard .NET event pattern
-   Supports reactive programming patterns

**Alternative Considered**: Callback delegates or observer pattern
**Why Chosen**: Events are the standard .NET approach and provide better decoupling

### Async/Await for Persistence

```csharp
Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default);
```

**Decision**: Make persistence operations asynchronous.

**Rationale**:

-   File I/O and database operations are naturally async
-   Prevents UI blocking
-   Supports cancellation for long-running operations
-   Future-proof for cloud storage scenarios

**Alternative Considered**: Synchronous methods with background tasks
**Why Rejected**: Would hide async nature and make cancellation harder

## Provider Pattern Implementation

### Why Provider Pattern?

**Decision**: Use provider pattern for undo/redo and persistence.

**Rationale**:

-   **Extensibility**: Easy to add new storage backends
-   **Testing**: Simple to mock for unit tests
-   **Separation of Concerns**: Navigation logic separate from storage details
-   **Configuration**: Different providers for different environments

### Optional Providers

```csharp
public NavigationStack(IUndoRedoProvider? undoRedoProvider = null,
                      IPersistenceProvider<T>? persistenceProvider = null)
```

**Decision**: Make both providers optional.

**Rationale**:

-   Not all scenarios need undo/redo functionality
-   Some applications don't require persistence
-   Simpler setup for basic use cases
-   Progressive enhancement approach

**Alternative Considered**: Separate interfaces for different feature combinations
**Why Rejected**: Would create interface explosion and complexity

## Navigation State Management

### Immutable vs Mutable State

**Decision**: Use mutable navigation items with immutable state snapshots.

**Rationale**:

-   Navigation items may need updates (display name, metadata)
-   State snapshots for persistence should be immutable
-   Balances usability with data integrity
-   Aligns with common UI patterns

### Forward Stack Behavior

**Decision**: Clear forward stack when navigating to new item.

**Rationale**:

-   Matches browser navigation behavior
-   Prevents confusing navigation states
-   Simplifies implementation
-   Consistent with user expectations

```csharp
public void NavigateTo(T item)
{
    // Clear forward stack - standard behavior
    while (_currentIndex < _items.Count - 1)
    {
        _items.RemoveAt(_items.Count - 1);
    }

    _items.Add(item);
    _currentIndex = _items.Count - 1;
}
```

## Error Handling Strategy

### Exception vs Return Value Approach

**Decision**: Use exceptions for invalid operations, return values for state queries.

**Rationale**:

-   Exceptions for programming errors (null arguments)
-   Return values for valid state queries (CanGoBack, GoBack)
-   Follows .NET Framework conventions
-   Clear distinction between errors and normal flow

```csharp
// Returns null for valid "no previous item" state
public T? GoBack()
{
    if (!CanGoBack) return null;
    // ...
}

// Throws for invalid input
public void NavigateTo(T item)
{
    ArgumentNullException.ThrowIfNull(item, nameof(item));
    // ...
}
```

### Graceful Degradation

**Decision**: Continue operation when optional features fail.

**Rationale**:

-   Navigation continues if persistence fails
-   Core functionality remains available
-   Logging can capture issues without blocking user
-   Better user experience

## Performance Considerations

### Lazy Loading vs Eager Loading

**Decision**: Eager loading for navigation history, lazy loading for persistence.

**Rationale**:

-   Navigation operations need fast access to history
-   Persistence can be deferred and batched
-   UI responsiveness is priority
-   Memory usage is manageable for typical navigation scenarios

### Event Handler Performance

**Decision**: Synchronous event handling with async extension points.

**Rationale**:

-   Events should be fast and not block navigation
-   Async work can be done in event handlers using fire-and-forget
-   Keeps navigation stack responsive
-   Allows for async logging, analytics, etc.

## Serialization Strategy

### JSON over Binary

**Decision**: Use JSON for default serialization.

**Rationale**:

-   Human readable for debugging
-   Cross-platform compatibility
-   Web API friendly
-   Schema evolution support
-   Tool support (viewers, editors)

**Alternative Considered**: Binary serialization
**Why Rejected**: Platform specific, harder to debug, version compatibility issues

### Custom Serialization Support

```csharp
public JsonFilePersistenceProvider(string filePath, JsonSerializerOptions? jsonOptions = null)
```

**Decision**: Allow custom serialization options.

**Rationale**:

-   Different applications have different needs
-   Custom type converters may be required
-   Performance tuning options
-   Backward compatibility scenarios

## Testing Strategy

### Dependency Injection Friendly

**Decision**: Design all components to support dependency injection.

**Rationale**:

-   Easy to mock for unit testing
-   Supports integration testing
-   Framework agnostic
-   Modern .NET best practices

### Interface-Based Design

**Decision**: Program against interfaces, not implementations.

**Rationale**:

-   Easy to create test doubles
-   Supports multiple implementations
-   Cleaner test code
-   Better separation of concerns

## Factory Pattern Usage

### Why Factory over Direct Instantiation?

**Decision**: Provide factory for common scenarios while allowing direct instantiation.

**Rationale**:

-   Simplifies common use cases
-   Encapsulates provider creation logic
-   Supports dependency injection scenarios
-   Still allows advanced configuration

```csharp
// Simple factory usage
var stack = factory.CreateBasicNavigationStack<NavigationItem>();

// Advanced direct usage
var stack = new NavigationStack<NavigationItem>(customUndoProvider, customPersistenceProvider);
```

## Metadata Design

### Dictionary vs Strongly Typed Properties

**Decision**: Use flexible dictionary-based metadata with strongly typed core properties.

**Rationale**:

-   Core properties (Id, DisplayName) are always needed
-   Metadata allows extensibility without interface changes
-   Different navigation items need different additional data
-   Balances type safety with flexibility

```csharp
public interface INavigationItem
{
    string Id { get; }                    // Always needed
    string DisplayName { get; set; }     // Always needed
    DateTime CreatedAt { get; }          // Always needed
    IReadOnlyDictionary<string, object> Metadata { get; } // Flexible
}
```

## Thread Safety

### Single-Threaded Design

**Decision**: Navigation stack is not thread-safe by design.

**Rationale**:

-   Navigation is typically UI-bound (single thread)
-   Thread safety adds complexity and performance overhead
-   Most UI frameworks are single-threaded
-   Applications needing multi-threading can add synchronization

**Note**: Persistence operations are async but called from UI thread context.

## Versioning Strategy

### Semantic Versioning

**Decision**: Use semantic versioning for the library.

**Rationale**:

-   Clear communication of breaking changes
-   Follows .NET ecosystem standards
-   Tool support for dependency management
-   Predictable upgrade paths

### Interface Stability

**Decision**: Interfaces are part of the public contract and avoid breaking changes.

**Rationale**:

-   Consumer code depends on interface stability
-   Breaking interface changes require major version bump
-   Extension through new interfaces preferred
-   Backward compatibility is important

## Memory Management

### Reference Management

**Decision**: Navigation stack holds references to navigation items.

**Rationale**:

-   Items may be referenced elsewhere in application
-   GC handles cleanup when references are released
-   Simpler than weak references
-   Acceptable memory usage for typical scenarios

### History Limits

**Decision**: Optional history limits in undo/redo providers.

**Rationale**:

-   Prevents unbounded memory growth
-   Configurable based on application needs
-   Default limits provide reasonable behavior
-   Can be disabled for scenarios requiring full history

## Extension Points

### Plugin Architecture

**Decision**: Extensible through interface implementation rather than plugin loading.

**Rationale**:

-   Simpler security model
-   Compile-time type checking
-   Better performance
-   Easier debugging and deployment

### Custom Navigation Items

**Decision**: Support custom navigation item implementations.

**Rationale**:

-   Different applications have different data needs
-   Base implementation covers common scenarios
-   Interface allows complete customization
-   Enables domain-specific navigation items

This design document serves as a reference for understanding the architectural decisions and can guide future development and modifications to the library.
