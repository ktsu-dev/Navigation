# Usage Guide

This guide covers common usage patterns, best practices, and real-world scenarios for the Navigation Stack Library.

## Getting Started

### Basic Navigation Stack

The simplest way to use the library is with a basic navigation stack:

```csharp
using ktsu.Navigation.Core.Contracts;
using ktsu.Navigation.Core.Models;
using ktsu.Navigation.Core.Services;

// Create a navigation stack
var navigationStack = new NavigationStack<NavigationItem>();

// Create and navigate to items
var homeItem = new NavigationItem("home", "Home Page");
var aboutItem = new NavigationItem("about", "About Page");
var contactItem = new NavigationItem("contact", "Contact Page");

navigationStack.NavigateTo(homeItem);
navigationStack.NavigateTo(aboutItem);
navigationStack.NavigateTo(contactItem);

// Navigate back and forward
var previous = navigationStack.GoBack(); // Returns to About Page
var forward = navigationStack.GoForward(); // Returns to Contact Page
```

### With Event Handling

Listen to navigation changes to update your UI:

```csharp
navigationStack.NavigationChanged += (sender, e) =>
{
    Console.WriteLine($"Navigation changed: {e.NavigationType}");
    Console.WriteLine($"Previous: {e.PreviousItem?.DisplayName ?? "None"}");
    Console.WriteLine($"Current: {e.CurrentItem?.DisplayName ?? "None"}");

    // Update UI here
    UpdateNavigationUI(e.CurrentItem);
};
```

## Advanced Scenarios

### With Undo/Redo Support

Add undo/redo functionality to your navigation:

```csharp
// Create an undo/redo provider
var undoProvider = new SimpleUndoRedoProvider(maxHistorySize: 50);

// Create navigation stack with undo support
var navigationStack = new NavigationStack<NavigationItem>(undoProvider);

// Navigate normally - operations are automatically undoable
navigationStack.NavigateTo(homeItem);
navigationStack.NavigateTo(aboutItem);

// Undo the last navigation
if (undoProvider.CanUndo)
{
    undoProvider.Undo(); // Returns to Home Page
}

// Redo the navigation
if (undoProvider.CanRedo)
{
    undoProvider.Redo(); // Returns to About Page
}

// Listen to undo/redo state changes
undoProvider.StateChanged += (sender, e) =>
{
    UpdateUndoRedoButtons(undoProvider.CanUndo, undoProvider.CanRedo);
};
```

### With Persistence

Save and restore navigation state across application sessions:

```csharp
// Create a file-based persistence provider
var persistenceProvider = new JsonFilePersistenceProvider<NavigationItem>("navigation.json");

// Create navigation stack with persistence
var navigationStack = new NavigationStack<NavigationItem>(
    undoRedoProvider: null,
    persistenceProvider: persistenceProvider);

// Load previous state on application startup
if (await navigationStack.LoadStateAsync())
{
    Console.WriteLine("Navigation state restored from previous session");
}

// Navigation operations automatically trigger persistence
navigationStack.NavigateTo(homeItem);
navigationStack.NavigateTo(aboutItem);

// Manually save state if needed
await navigationStack.SaveStateAsync();
```

### Full-Featured Setup

Combine all features for a complete navigation solution:

```csharp
// Create providers
var undoProvider = new SimpleUndoRedoProvider();
var persistenceProvider = new JsonFilePersistenceProvider<NavigationItem>("nav.json");

// Create navigation stack with all features
var navigationStack = new NavigationStack<NavigationItem>(undoProvider, persistenceProvider);

// Load previous state
await navigationStack.LoadStateAsync();

// Set up event handling
navigationStack.NavigationChanged += OnNavigationChanged;
undoProvider.StateChanged += OnUndoRedoStateChanged;

// Your application is ready for navigation!
```

## Custom Navigation Items

Create specialized navigation items for your application:

```csharp
public class PageNavigationItem : INavigationItem
{
    public string Id { get; }
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();

    // Custom properties
    public string Url { get; }
    public string PageTitle { get; set; }
    public Dictionary<string, string> QueryParameters { get; }

    private readonly Dictionary<string, object> _metadata = new();

    public PageNavigationItem(string id, string displayName, string url)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Url = url ?? throw new ArgumentNullException(nameof(url));
        CreatedAt = DateTime.UtcNow;
        QueryParameters = new Dictionary<string, string>();
    }

    public void SetMetadata(string key, object value)
    {
        _metadata[key] = value;
    }

    public bool RemoveMetadata(string key)
    {
        return _metadata.Remove(key);
    }

    // Custom methods
    public void AddQueryParameter(string key, string value)
    {
        QueryParameters[key] = value;
    }
}

// Usage
var pageItem = new PageNavigationItem("product-123", "Product Details", "/products/123");
pageItem.AddQueryParameter("tab", "reviews");
pageItem.SetMetadata("category", "electronics");

navigationStack.NavigateTo(pageItem);
```

## Working with Metadata

Navigation items support flexible metadata for storing additional information:

```csharp
var item = new NavigationItem("dashboard", "Dashboard");

// Add metadata
item.SetMetadata("section", "analytics");
item.SetMetadata("filters", new { dateRange = "last30days", category = "sales" });
item.SetMetadata("permissions", new[] { "read", "write" });

// Access metadata
if (item.Metadata.TryGetValue("section", out var section))
{
    Console.WriteLine($"Section: {section}");
}

// Remove metadata
item.RemoveMetadata("filters");
```

## Factory Pattern Usage

Use the factory for consistent navigation stack creation:

```csharp
// Set up factory with default providers
var defaultUndoProvider = new SimpleUndoRedoProvider();
var factory = new NavigationStackFactory(defaultUndoProvider);

// Create different types of navigation stacks
var basicStack = factory.CreateBasicNavigationStack<NavigationItem>();
var undoStack = factory.CreateNavigationStack<NavigationItem>(new SimpleUndoRedoProvider());
var persistentStack = factory.CreateNavigationStack<NavigationItem>(
    new JsonFilePersistenceProvider<NavigationItem>("nav.json"));

// With dependency injection
var factory = new NavigationStackFactory(
    defaultUndoProvider,
    serviceType => serviceProvider.GetService(serviceType));
```

## Best Practices

### Navigation Management

```csharp
public class NavigationManager<T> where T : INavigationItem
{
    private readonly INavigationStack<T> _navigationStack;

    public NavigationManager(INavigationStack<T> navigationStack)
    {
        _navigationStack = navigationStack;
        _navigationStack.NavigationChanged += OnNavigationChanged;
    }

    public async Task<bool> CanNavigateToAsync(T item)
    {
        // Implement business logic for navigation validation
        return await ValidatePermissions(item);
    }

    public async Task NavigateToAsync(T item)
    {
        if (await CanNavigateToAsync(item))
        {
            _navigationStack.NavigateTo(item);
            await SaveNavigationStateAsync();
        }
    }

    private async Task SaveNavigationStateAsync()
    {
        if (_navigationStack is NavigationStack<T> stack)
        {
            await stack.SaveStateAsync();
        }
    }

    private void OnNavigationChanged(object sender, NavigationEventArgs<T> e)
    {
        // Update UI, analytics, etc.
        LogNavigationEvent(e);
        UpdateBrowserHistory(e);
    }
}
```

### Error Handling

```csharp
public async Task<bool> SafeNavigateAsync<T>(INavigationStack<T> stack, T item)
    where T : INavigationItem
{
    try
    {
        stack.NavigateTo(item);

        if (stack is NavigationStack<T> persistentStack)
        {
            await persistentStack.SaveStateAsync();
        }

        return true;
    }
    catch (ArgumentNullException ex)
    {
        logger.LogError(ex, "Invalid navigation item");
        return false;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Navigation failed");
        return false;
    }
}
```

### Memory Management

```csharp
// Limit navigation history to prevent memory leaks
var undoProvider = new SimpleUndoRedoProvider(maxHistorySize: 100);

// Clear history when appropriate
public void OnUserLogout()
{
    navigationStack.Clear();
    undoProvider.Clear();
}

// Dispose of event handlers
public void Dispose()
{
    navigationStack.NavigationChanged -= OnNavigationChanged;
    undoProvider.StateChanged -= OnUndoRedoStateChanged;
}
```

## Integration Examples

### Web Application Integration

```csharp
public class WebNavigationService
{
    private readonly INavigationStack<PageNavigationItem> _navigationStack;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task NavigateToPageAsync(string path, Dictionary<string, string> query = null)
    {
        var pageId = GeneratePageId(path, query);
        var displayName = GetPageDisplayName(path);

        var item = new PageNavigationItem(pageId, displayName, path);

        if (query != null)
        {
            foreach (var kvp in query)
            {
                item.AddQueryParameter(kvp.Key, kvp.Value);
            }
        }

        _navigationStack.NavigateTo(item);
        await UpdateBrowserHistoryAsync(item);
    }

    public async Task HandleBrowserBackAsync()
    {
        var previous = _navigationStack.GoBack();
        if (previous != null)
        {
            await RedirectToPageAsync(previous);
        }
    }
}
```

### Desktop Application Integration

```csharp
public partial class MainWindow : Window
{
    private readonly INavigationStack<ViewNavigationItem> _navigationStack;

    public MainWindow()
    {
        InitializeComponent();

        var undoProvider = new SimpleUndoRedoProvider();
        var persistenceProvider = new JsonFilePersistenceProvider<ViewNavigationItem>(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "MyApp", "navigation.json"));

        _navigationStack = new NavigationStack<ViewNavigationItem>(undoProvider, persistenceProvider);
        _navigationStack.NavigationChanged += OnNavigationChanged;

        SetupCommands(undoProvider);
    }

    private void SetupCommands(IUndoRedoProvider undoProvider)
    {
        BackCommand = new RelayCommand(() => _navigationStack.GoBack(), () => _navigationStack.CanGoBack);
        ForwardCommand = new RelayCommand(() => _navigationStack.GoForward(), () => _navigationStack.CanGoForward);
        UndoCommand = new RelayCommand(() => undoProvider.Undo(), () => undoProvider.CanUndo);
        RedoCommand = new RelayCommand(() => undoProvider.Redo(), () => undoProvider.CanRedo);
    }

    private void OnNavigationChanged(object sender, NavigationEventArgs<ViewNavigationItem> e)
    {
        // Update content area
        ContentFrame.Navigate(e.CurrentItem?.ViewType);

        // Update navigation buttons
        CommandManager.InvalidateRequerySuggested();
    }
}
```

## Performance Tips

1. **Use appropriate history limits**: Set reasonable max history sizes for undo providers
2. **Lazy persistence**: Only save state when necessary, not on every navigation
3. **Efficient event handling**: Avoid heavy operations in navigation event handlers
4. **Memory cleanup**: Clear navigation history when no longer needed
5. **Async operations**: Use async methods for persistence operations

## Common Patterns

### Breadcrumb Navigation

```csharp
public class BreadcrumbViewModel
{
    private readonly INavigationStack<NavigationItem> _navigationStack;

    public ObservableCollection<BreadcrumbItem> Breadcrumbs { get; } = new();

    public BreadcrumbViewModel(INavigationStack<NavigationItem> navigationStack)
    {
        _navigationStack = navigationStack;
        _navigationStack.NavigationChanged += UpdateBreadcrumbs;
    }

    private void UpdateBreadcrumbs(object sender, NavigationEventArgs<NavigationItem> e)
    {
        Breadcrumbs.Clear();
        var backStack = _navigationStack.GetBackStack();

        foreach (var item in backStack)
        {
            Breadcrumbs.Add(new BreadcrumbItem(item.DisplayName, () => NavigateTo(item)));
        }

        if (e.CurrentItem != null)
        {
            Breadcrumbs.Add(new BreadcrumbItem(e.CurrentItem.DisplayName, null)); // Current item
        }
    }
}
```

### Tab Navigation

```csharp
public class TabNavigationManager
{
    private readonly Dictionary<string, INavigationStack<NavigationItem>> _tabStacks = new();
    private string _activeTab;

    public void CreateTab(string tabId)
    {
        _tabStacks[tabId] = new NavigationStack<NavigationItem>();
    }

    public void SwitchToTab(string tabId)
    {
        if (_tabStacks.ContainsKey(tabId))
        {
            _activeTab = tabId;
            OnActiveTabChanged?.Invoke(tabId, _tabStacks[tabId].Current);
        }
    }

    public void NavigateInActiveTab(NavigationItem item)
    {
        if (_activeTab != null && _tabStacks.ContainsKey(_activeTab))
        {
            _tabStacks[_activeTab].NavigateTo(item);
        }
    }
}
```
