# API Reference

Complete API documentation for the Navigation Stack Library.

## Core Interfaces

### INavigationStack&lt;T&gt;

Main interface for navigation stack functionality.

```csharp
public interface INavigationStack<T> where T : INavigationItem
```

#### Properties

| Property       | Type   | Description                                         |
| -------------- | ------ | --------------------------------------------------- |
| `Current`      | `T?`   | Gets the current item in the navigation stack       |
| `CanGoBack`    | `bool` | Gets whether there are items to navigate back to    |
| `CanGoForward` | `bool` | Gets whether there are items to navigate forward to |
| `Count`        | `int`  | Gets the number of items in the stack               |

#### Methods

| Method               | Return Type        | Description                                     |
| -------------------- | ------------------ | ----------------------------------------------- |
| `NavigateTo(T item)` | `void`             | Navigates to a new item, adding it to the stack |
| `GoBack()`           | `T?`               | Navigates back to the previous item             |
| `GoForward()`        | `T?`               | Navigates forward to the next item              |
| `Clear()`            | `void`             | Clears all items from the navigation stack      |
| `GetHistory()`       | `IReadOnlyList<T>` | Gets a read-only view of the navigation history |
| `GetBackStack()`     | `IReadOnlyList<T>` | Gets the back stack (items before current)      |
| `GetForwardStack()`  | `IReadOnlyList<T>` | Gets the forward stack (items after current)    |

#### Events

| Event               | Type                         | Description                   |
| ------------------- | ---------------------------- | ----------------------------- |
| `NavigationChanged` | `NavigationEventHandler<T>?` | Raised when navigation occurs |

---

### INavigationItem

Contract for items that can be stored in a navigation stack.

```csharp
public interface INavigationItem
```

#### Properties

| Property      | Type                                  | Description                                                   |
| ------------- | ------------------------------------- | ------------------------------------------------------------- |
| `Id`          | `string`                              | Gets the unique identifier for this navigation item           |
| `DisplayName` | `string`                              | Gets or sets the display name for this navigation item        |
| `CreatedAt`   | `DateTime`                            | Gets the timestamp when this navigation item was created      |
| `Metadata`    | `IReadOnlyDictionary<string, object>` | Gets additional metadata associated with this navigation item |

#### Methods

| Method                                  | Return Type | Description                                       |
| --------------------------------------- | ----------- | ------------------------------------------------- |
| `SetMetadata(string key, object value)` | `void`      | Adds or updates metadata for this navigation item |
| `RemoveMetadata(string key)`            | `bool`      | Removes metadata from this navigation item        |

---

### IUndoRedoProvider

Contract for external undo/redo providers.

```csharp
public interface IUndoRedoProvider
```

#### Properties

| Property  | Type   | Description                                |
| --------- | ------ | ------------------------------------------ |
| `CanUndo` | `bool` | Gets whether undo operations are available |
| `CanRedo` | `bool` | Gets whether redo operations are available |

#### Methods

| Method                                                       | Return Type | Description                                    |
| ------------------------------------------------------------ | ----------- | ---------------------------------------------- |
| `RegisterAction(IUndoableAction action, string description)` | `void`      | Registers an undoable action with the provider |
| `Undo()`                                                     | `bool`      | Performs an undo operation                     |
| `Redo()`                                                     | `bool`      | Performs a redo operation                      |
| `Clear()`                                                    | `void`      | Clears all undo/redo history                   |

#### Events

| Event          | Type            | Description                             |
| -------------- | --------------- | --------------------------------------- |
| `StateChanged` | `EventHandler?` | Raised when the undo/redo state changes |

---

### IPersistenceProvider&lt;T&gt;

Contract for persistence providers that can save and restore navigation state.

```csharp
public interface IPersistenceProvider<T> where T : INavigationItem
```

#### Methods

| Method                                                                                     | Return Type                  | Description                                        |
| ------------------------------------------------------------------------------------------ | ---------------------------- | -------------------------------------------------- |
| `SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default)` | `Task`                       | Saves the navigation state to persistent storage   |
| `LoadStateAsync(CancellationToken cancellationToken = default)`                            | `Task<INavigationState<T>?>` | Loads the navigation state from persistent storage |
| `HasSavedStateAsync(CancellationToken cancellationToken = default)`                        | `Task<bool>`                 | Checks if saved state exists                       |
| `ClearSavedStateAsync(CancellationToken cancellationToken = default)`                      | `Task`                       | Clears any saved state                             |

---

### INavigationState&lt;T&gt;

Represents the state of a navigation stack that can be persisted.

```csharp
public interface INavigationState<T> where T : INavigationItem
```

#### Properties

| Property       | Type               | Description                                    |
| -------------- | ------------------ | ---------------------------------------------- |
| `Items`        | `IReadOnlyList<T>` | Gets the items in the navigation stack         |
| `CurrentIndex` | `int`              | Gets the index of the current item             |
| `CreatedAt`    | `DateTime`         | Gets the timestamp when this state was created |

---

### IUndoableAction

Contract for undoable actions.

```csharp
public interface IUndoableAction
```

#### Properties

| Property      | Type     | Description                         |
| ------------- | -------- | ----------------------------------- |
| `Description` | `string` | Gets the description of this action |

#### Methods

| Method      | Return Type | Description         |
| ----------- | ----------- | ------------------- |
| `Execute()` | `void`      | Executes the action |
| `Undo()`    | `void`      | Undoes the action   |

## Core Classes

### NavigationStack&lt;T&gt;

Main implementation of the navigation stack.

```csharp
public class NavigationStack<T> : INavigationStack<T> where T : INavigationItem
```

#### Constructors

```csharp
public NavigationStack(IUndoRedoProvider? undoRedoProvider = null,
                      IPersistenceProvider<T>? persistenceProvider = null)
```

#### Additional Methods

| Method                                                          | Return Type  | Description                                                       |
| --------------------------------------------------------------- | ------------ | ----------------------------------------------------------------- |
| `SaveStateAsync(CancellationToken cancellationToken = default)` | `Task`       | Saves the current navigation state using the persistence provider |
| `LoadStateAsync(CancellationToken cancellationToken = default)` | `Task<bool>` | Loads navigation state using the persistence provider             |

---

### NavigationItem

Base implementation of a navigation item.

```csharp
public class NavigationItem : INavigationItem
```

#### Constructors

```csharp
public NavigationItem(string id, string displayName)
```

#### Method Overrides

| Method                | Return Type | Description             |
| --------------------- | ----------- | ----------------------- |
| `ToString()`          | `string`    | Returns the DisplayName |
| `Equals(object? obj)` | `bool`      | Compares based on Id    |
| `GetHashCode()`       | `int`       | Hash code based on Id   |

---

### NavigationState&lt;T&gt;

Implementation of navigation state for persistence.

```csharp
public class NavigationState<T> : INavigationState<T> where T : INavigationItem
```

#### Constructors

```csharp
public NavigationState(IEnumerable<T> items, int currentIndex)
```

#### Additional Properties

| Property  | Type | Description                                   |
| --------- | ---- | --------------------------------------------- |
| `Current` | `T?` | Gets the current item in the navigation state |

#### Static Methods

| Method                                                     | Return Type          | Description                                            |
| ---------------------------------------------------------- | -------------------- | ------------------------------------------------------ |
| `FromNavigationStack(INavigationStack<T> navigationStack)` | `NavigationState<T>` | Creates a new navigation state from a navigation stack |

---

### SimpleUndoRedoProvider

Built-in implementation of undo/redo functionality.

```csharp
public class SimpleUndoRedoProvider : IUndoRedoProvider
```

#### Constructors

```csharp
public SimpleUndoRedoProvider(int maxHistorySize = 100)
```

#### Additional Methods

| Method           | Return Type                      | Description                               |
| ---------------- | -------------------------------- | ----------------------------------------- |
| `GetUndoStack()` | `IReadOnlyList<IUndoableAction>` | Gets the current undo stack for debugging |
| `GetRedoStack()` | `IReadOnlyList<IUndoableAction>` | Gets the current redo stack for debugging |

---

### InMemoryPersistenceProvider&lt;T&gt;

In-memory persistence provider for testing and simple scenarios.

```csharp
public class InMemoryPersistenceProvider<T> : IPersistenceProvider<T> where T : INavigationItem
```

#### Constructors

```csharp
public InMemoryPersistenceProvider()
```

---

### JsonFilePersistenceProvider&lt;T&gt;

JSON file-based persistence provider.

```csharp
public class JsonFilePersistenceProvider<T> : IPersistenceProvider<T> where T : INavigationItem
```

#### Constructors

```csharp
public JsonFilePersistenceProvider(string filePath, JsonSerializerOptions? jsonOptions = null)
```

---

### NavigationStackFactory

Factory for creating navigation stacks with configured providers.

```csharp
public class NavigationStackFactory
```

#### Constructors

```csharp
public NavigationStackFactory(IUndoRedoProvider? defaultUndoRedoProvider = null,
                             Func<Type, object?>? serviceProvider = null)
```

#### Methods

| Method                                                                   | Return Type           | Description                                                 |
| ------------------------------------------------------------------------ | --------------------- | ----------------------------------------------------------- |
| `CreateNavigationStack<T>()`                                             | `INavigationStack<T>` | Creates a navigation stack with default providers           |
| `CreateNavigationStack<T>(IUndoRedoProvider?, IPersistenceProvider<T>?)` | `INavigationStack<T>` | Creates a navigation stack with specific providers          |
| `CreateNavigationStack<T>(IUndoRedoProvider)`                            | `INavigationStack<T>` | Creates a navigation stack with only an undo/redo provider  |
| `CreateNavigationStack<T>(IPersistenceProvider<T>)`                      | `INavigationStack<T>` | Creates a navigation stack with only a persistence provider |
| `CreateBasicNavigationStack<T>()`                                        | `INavigationStack<T>` | Creates a basic navigation stack without any providers      |

## Events and Delegates

### NavigationEventHandler&lt;T&gt;

Delegate for navigation events.

```csharp
public delegate void NavigationEventHandler<T>(object sender, NavigationEventArgs<T> e)
    where T : INavigationItem;
```

### NavigationEventArgs&lt;T&gt;

Event arguments for navigation events.

```csharp
public class NavigationEventArgs<T> : EventArgs where T : INavigationItem
```

#### Constructors

```csharp
public NavigationEventArgs(NavigationType navigationType, T? previousItem, T? currentItem)
```

#### Properties

| Property         | Type             | Description                               |
| ---------------- | ---------------- | ----------------------------------------- |
| `NavigationType` | `NavigationType` | Gets the type of navigation that occurred |
| `PreviousItem`   | `T?`             | Gets the previous navigation item         |
| `CurrentItem`    | `T?`             | Gets the current navigation item          |

## Enumerations

### NavigationType

Defines the types of navigation operations.

```csharp
public enum NavigationType
{
    NavigateTo,    // Navigation to a new item
    GoBack,        // Navigation backward in the stack
    GoForward,     // Navigation forward in the stack
    Clear          // The navigation stack was cleared
}
```

## Usage Examples

### Basic Usage

```csharp
var stack = new NavigationStack<NavigationItem>();
var item = new NavigationItem("page1", "Page 1");
stack.NavigateTo(item);
```

### With Providers

```csharp
var undoProvider = new SimpleUndoRedoProvider();
var persistenceProvider = new JsonFilePersistenceProvider<NavigationItem>("nav.json");
var stack = new NavigationStack<NavigationItem>(undoProvider, persistenceProvider);
```

### Using Factory

```csharp
var factory = new NavigationStackFactory();
var stack = factory.CreateNavigationStack<NavigationItem>();
```
