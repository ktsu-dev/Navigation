# Code Examples

This document provides comprehensive code examples for common scenarios and advanced use cases with the Navigation Stack Library.

## Basic Examples

### Simple Navigation Stack

```csharp
using ktsu.Navigation.Core.Contracts;
using ktsu.Navigation.Core.Models;
using ktsu.Navigation.Core.Services;

// Create a basic navigation stack
var navigationStack = new NavigationStack<NavigationItem>();

// Create navigation items
var home = new NavigationItem("home", "Home");
var products = new NavigationItem("products", "Products");
var about = new NavigationItem("about", "About Us");

// Navigate through pages
navigationStack.NavigateTo(home);
Console.WriteLine($"Current: {navigationStack.Current?.DisplayName}"); // "Home"

navigationStack.NavigateTo(products);
Console.WriteLine($"Current: {navigationStack.Current?.DisplayName}"); // "Products"

navigationStack.NavigateTo(about);
Console.WriteLine($"Current: {navigationStack.Current?.DisplayName}"); // "About Us"

// Navigate back
var previous = navigationStack.GoBack();
Console.WriteLine($"Went back to: {previous?.DisplayName}"); // "Products"

// Navigate forward
var forward = navigationStack.GoForward();
Console.WriteLine($"Went forward to: {forward?.DisplayName}"); // "About Us"

// Check navigation state
Console.WriteLine($"Can go back: {navigationStack.CanGoBack}"); // True
Console.WriteLine($"Can go forward: {navigationStack.CanGoForward}"); // False
Console.WriteLine($"Total items: {navigationStack.Count}"); // 3
```

### With Event Handling

```csharp
var navigationStack = new NavigationStack<NavigationItem>();

// Subscribe to navigation events
navigationStack.NavigationChanged += (sender, e) =>
{
    Console.WriteLine($"Navigation: {e.NavigationType}");
    Console.WriteLine($"From: {e.PreviousItem?.DisplayName ?? "None"}");
    Console.WriteLine($"To: {e.CurrentItem?.DisplayName ?? "None"}");
    Console.WriteLine();
};

// This will trigger the event
navigationStack.NavigateTo(new NavigationItem("page1", "Page 1"));
// Output:
// Navigation: NavigateTo
// From: None
// To: Page 1

navigationStack.NavigateTo(new NavigationItem("page2", "Page 2"));
// Output:
// Navigation: NavigateTo
// From: Page 1
// To: Page 2

navigationStack.GoBack();
// Output:
// Navigation: GoBack
// From: Page 2
// To: Page 1
```

## Advanced Examples

### Custom Navigation Items

```csharp
// Define a specialized navigation item for web pages
public class WebPageNavigationItem : INavigationItem
{
    private readonly Dictionary<string, object> _metadata = new();

    public string Id { get; }
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();

    // Web-specific properties
    public string Url { get; }
    public string Title { get; set; }
    public Dictionary<string, string> QueryParameters { get; } = new();
    public string? ReferrerUrl { get; set; }
    public TimeSpan LoadTime { get; set; }

    public WebPageNavigationItem(string id, string displayName, string url)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Title = displayName;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetMetadata(string key, object value) => _metadata[key] = value;
    public bool RemoveMetadata(string key) => _metadata.Remove(key);

    public void AddQueryParameter(string key, string value) => QueryParameters[key] = value;

    public string GetFullUrl()
    {
        if (!QueryParameters.Any()) return Url;

        var query = string.Join("&", QueryParameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{Url}?{query}";
    }
}

// Usage
var navigationStack = new NavigationStack<WebPageNavigationItem>();

var homePage = new WebPageNavigationItem("home", "Home Page", "/");
homePage.SetMetadata("pageType", "landing");
homePage.LoadTime = TimeSpan.FromMilliseconds(250);

var productPage = new WebPageNavigationItem("product-123", "Product Details", "/products/123");
productPage.AddQueryParameter("tab", "reviews");
productPage.AddQueryParameter("sort", "newest");
productPage.ReferrerUrl = "/";
productPage.SetMetadata("productId", 123);
productPage.SetMetadata("category", "electronics");

navigationStack.NavigateTo(homePage);
navigationStack.NavigateTo(productPage);

Console.WriteLine($"Full URL: {productPage.GetFullUrl()}");
// Output: /products/123?tab=reviews&sort=newest
```

### Undo/Redo Integration

```csharp
// Create undo/redo provider
var undoRedoProvider = new SimpleUndoRedoProvider(maxHistorySize: 10);

// Create navigation stack with undo support
var navigationStack = new NavigationStack<NavigationItem>(undoRedoProvider);

// Listen to undo/redo state changes
undoRedoProvider.StateChanged += (sender, e) =>
{
    Console.WriteLine($"Undo available: {undoRedoProvider.CanUndo}");
    Console.WriteLine($"Redo available: {undoRedoProvider.CanRedo}");
};

// Perform navigation operations
var page1 = new NavigationItem("1", "Page 1");
var page2 = new NavigationItem("2", "Page 2");
var page3 = new NavigationItem("3", "Page 3");

navigationStack.NavigateTo(page1);
navigationStack.NavigateTo(page2);
navigationStack.NavigateTo(page3);

Console.WriteLine($"Current: {navigationStack.Current?.DisplayName}"); // "Page 3"

// Undo last navigation
if (undoRedoProvider.CanUndo)
{
    undoRedoProvider.Undo();
    Console.WriteLine($"After undo: {navigationStack.Current?.DisplayName}"); // "Page 2"
}

// Undo again
if (undoRedoProvider.CanUndo)
{
    undoRedoProvider.Undo();
    Console.WriteLine($"After second undo: {navigationStack.Current?.DisplayName}"); // "Page 1"
}

// Redo
if (undoRedoProvider.CanRedo)
{
    undoRedoProvider.Redo();
    Console.WriteLine($"After redo: {navigationStack.Current?.DisplayName}"); // "Page 2"
}
```

### Persistence Examples

#### File-Based Persistence

```csharp
// Create persistence provider
var persistenceProvider = new JsonFilePersistenceProvider<NavigationItem>("navigation.json");

// Create navigation stack with persistence
var navigationStack = new NavigationStack<NavigationItem>(null, persistenceProvider);

// Try to load previous state
if (await navigationStack.LoadStateAsync())
{
    Console.WriteLine("Previous navigation state restored");
    Console.WriteLine($"Current page: {navigationStack.Current?.DisplayName}");
    Console.WriteLine($"History count: {navigationStack.Count}");
}
else
{
    Console.WriteLine("No previous state found, starting fresh");
}

// Perform navigation
navigationStack.NavigateTo(new NavigationItem("home", "Home"));
navigationStack.NavigateTo(new NavigationItem("products", "Products"));

// Save state (happens automatically, but can be done manually)
await navigationStack.SaveStateAsync();
Console.WriteLine("Navigation state saved");
```

#### In-Memory Persistence (for testing)

```csharp
var memoryProvider = new InMemoryPersistenceProvider<NavigationItem>();
var navigationStack = new NavigationStack<NavigationItem>(null, memoryProvider);

// Add some navigation items
navigationStack.NavigateTo(new NavigationItem("1", "Page 1"));
navigationStack.NavigateTo(new NavigationItem("2", "Page 2"));

// Save state
await navigationStack.SaveStateAsync();

// Create a new stack and load the state
var newStack = new NavigationStack<NavigationItem>(null, memoryProvider);
var loaded = await newStack.LoadStateAsync();

Console.WriteLine($"State loaded: {loaded}");
Console.WriteLine($"Current page: {newStack.Current?.DisplayName}"); // "Page 2"
Console.WriteLine($"Can go back: {newStack.CanGoBack}"); // True
```

### Factory Pattern Examples

```csharp
// Set up factory with default providers
var defaultUndoProvider = new SimpleUndoRedoProvider();
var factory = new NavigationStackFactory(defaultUndoProvider);

// Create different types of navigation stacks
var basicStack = factory.CreateBasicNavigationStack<NavigationItem>();
var undoStack = factory.CreateNavigationStack<NavigationItem>();
var persistentStack = factory.CreateNavigationStack<NavigationItem>(
    new JsonFilePersistenceProvider<NavigationItem>("nav.json"));

// Full-featured stack
var fullStack = factory.CreateNavigationStack<NavigationItem>(
    new SimpleUndoRedoProvider(50),
    new JsonFilePersistenceProvider<NavigationItem>("full-nav.json"));
```

## Real-World Scenarios

### Web Application Navigation

```csharp
public class WebNavigationManager
{
    private readonly INavigationStack<WebPageNavigationItem> _navigationStack;
    private readonly ILogger<WebNavigationManager> _logger;

    public WebNavigationManager(
        INavigationStack<WebPageNavigationItem> navigationStack,
        ILogger<WebNavigationManager> logger)
    {
        _navigationStack = navigationStack;
        _logger = logger;

        _navigationStack.NavigationChanged += OnNavigationChanged;
    }

    public async Task NavigateToPageAsync(string url, string title, string? referrer = null)
    {
        var pageId = GeneratePageId(url);
        var item = new WebPageNavigationItem(pageId, title, url)
        {
            ReferrerUrl = referrer
        };

        // Add analytics metadata
        item.SetMetadata("timestamp", DateTime.UtcNow);
        item.SetMetadata("userAgent", GetUserAgent());
        item.SetMetadata("sessionId", GetSessionId());

        _navigationStack.NavigateTo(item);

        // Save state for session persistence
        if (_navigationStack is NavigationStack<WebPageNavigationItem> stack)
        {
            await stack.SaveStateAsync();
        }

        _logger.LogInformation("Navigated to {Url} with title {Title}", url, title);
    }

    public async Task<bool> GoBackAsync()
    {
        var previous = _navigationStack.GoBack();
        if (previous != null)
        {
            await UpdateBrowserHistoryAsync(previous);
            _logger.LogInformation("Navigated back to {Url}", previous.Url);
            return true;
        }
        return false;
    }

    public async Task<bool> GoForwardAsync()
    {
        var next = _navigationStack.GoForward();
        if (next != null)
        {
            await UpdateBrowserHistoryAsync(next);
            _logger.LogInformation("Navigated forward to {Url}", next.Url);
            return true;
        }
        return false;
    }

    public IEnumerable<WebPageNavigationItem> GetRecentPages(int count = 10)
    {
        return _navigationStack.GetHistory()
            .OrderByDescending(p => p.CreatedAt)
            .Take(count);
    }

    public async Task RestoreSessionAsync()
    {
        if (_navigationStack is NavigationStack<WebPageNavigationItem> stack)
        {
            var restored = await stack.LoadStateAsync();
            if (restored)
            {
                _logger.LogInformation("Session restored with {Count} pages", _navigationStack.Count);

                // Navigate to the current page
                var current = _navigationStack.Current;
                if (current != null)
                {
                    await UpdateBrowserHistoryAsync(current);
                }
            }
        }
    }

    private void OnNavigationChanged(object sender, NavigationEventArgs<WebPageNavigationItem> e)
    {
        // Update page title
        if (e.CurrentItem != null)
        {
            UpdatePageTitle(e.CurrentItem.Title);
        }

        // Track analytics
        TrackPageView(e);

        // Update navigation UI
        UpdateNavigationButtons();
    }

    private string GeneratePageId(string url) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(url)).Replace("+", "-").Replace("/", "_");

    private async Task UpdateBrowserHistoryAsync(WebPageNavigationItem item)
    {
        // Implementation depends on framework (Blazor, SignalR, etc.)
        await Task.CompletedTask;
    }

    private void UpdatePageTitle(string title)
    {
        // Update browser title or app title
    }

    private void TrackPageView(NavigationEventArgs<WebPageNavigationItem> e)
    {
        // Send to analytics service
    }

    private void UpdateNavigationButtons()
    {
        // Update UI button states
    }

    private string GetUserAgent() => "WebApp/1.0";
    private string GetSessionId() => Guid.NewGuid().ToString();
}
```

### Desktop Application with Views

```csharp
public class ViewNavigationItem : INavigationItem
{
    private readonly Dictionary<string, object> _metadata = new();

    public string Id { get; }
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();

    // View-specific properties
    public Type ViewType { get; }
    public Type? ViewModelType { get; }
    public object? ViewModelInstance { get; set; }
    public bool IsModal { get; set; }
    public Dictionary<string, object> Parameters { get; } = new();

    public ViewNavigationItem(string id, string displayName, Type viewType, Type? viewModelType = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ViewType = viewType ?? throw new ArgumentNullException(nameof(viewType));
        ViewModelType = viewModelType;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetMetadata(string key, object value) => _metadata[key] = value;
    public bool RemoveMetadata(string key) => _metadata.Remove(key);
}

public class DesktopNavigationService
{
    private readonly INavigationStack<ViewNavigationItem> _navigationStack;
    private readonly IServiceProvider _serviceProvider;
    private readonly Window _mainWindow;

    public DesktopNavigationService(
        INavigationStack<ViewNavigationItem> navigationStack,
        IServiceProvider serviceProvider,
        Window mainWindow)
    {
        _navigationStack = navigationStack;
        _serviceProvider = serviceProvider;
        _mainWindow = mainWindow;

        _navigationStack.NavigationChanged += OnNavigationChanged;
    }

    public async Task NavigateToViewAsync<TView, TViewModel>(object? parameter = null)
        where TView : UserControl
        where TViewModel : class
    {
        var viewType = typeof(TView);
        var viewModelType = typeof(TViewModel);

        var item = new ViewNavigationItem(
            viewType.Name,
            GetDisplayNameForView(viewType),
            viewType,
            viewModelType);

        if (parameter != null)
        {
            item.Parameters["parameter"] = parameter;
        }

        // Create ViewModel instance
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        item.ViewModelInstance = viewModel;

        _navigationStack.NavigateTo(item);

        // Save navigation state
        if (_navigationStack is NavigationStack<ViewNavigationItem> stack)
        {
            await stack.SaveStateAsync();
        }
    }

    public async Task NavigateToViewAsync<TView>(object? parameter = null) where TView : UserControl
    {
        var viewType = typeof(TView);

        var item = new ViewNavigationItem(
            viewType.Name,
            GetDisplayNameForView(viewType),
            viewType);

        if (parameter != null)
        {
            item.Parameters["parameter"] = parameter;
        }

        _navigationStack.NavigateTo(item);

        if (_navigationStack is NavigationStack<ViewNavigationItem> stack)
        {
            await stack.SaveStateAsync();
        }
    }

    public void GoBack()
    {
        _navigationStack.GoBack();
    }

    public void GoForward()
    {
        _navigationStack.GoForward();
    }

    private void OnNavigationChanged(object sender, NavigationEventArgs<ViewNavigationItem> e)
    {
        if (e.CurrentItem != null)
        {
            ShowView(e.CurrentItem);
        }
    }

    private void ShowView(ViewNavigationItem item)
    {
        // Create view instance
        var view = (UserControl)_serviceProvider.GetRequiredService(item.ViewType);

        // Set DataContext if ViewModel exists
        if (item.ViewModelInstance != null)
        {
            view.DataContext = item.ViewModelInstance;
        }

        // Pass parameters to view/viewmodel
        if (item.Parameters.Any())
        {
            PassParametersToView(view, item.Parameters);
        }

        // Show the view
        _mainWindow.Content = view;
        _mainWindow.Title = $"My App - {item.DisplayName}";
    }

    private void PassParametersToView(UserControl view, Dictionary<string, object> parameters)
    {
        // Implementation depends on your parameter passing strategy
        if (view.DataContext is IParameterReceiver parameterReceiver)
        {
            parameterReceiver.ReceiveParameters(parameters);
        }
    }

    private string GetDisplayNameForView(Type viewType)
    {
        // Convert ViewType name to friendly display name
        var name = viewType.Name;
        if (name.EndsWith("View"))
        {
            name = name[..^4]; // Remove "View" suffix
        }

        // Convert PascalCase to Title Case
        return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
    }
}

public interface IParameterReceiver
{
    void ReceiveParameters(Dictionary<string, object> parameters);
}
```

### Multi-Tab Navigation

```csharp
public class TabNavigationManager
{
    private readonly Dictionary<string, INavigationStack<NavigationItem>> _tabStacks = new();
    private readonly IUndoRedoProvider _globalUndoRedoProvider;
    private string? _activeTabId;

    public event EventHandler<TabChangedEventArgs>? ActiveTabChanged;
    public event EventHandler<TabCreatedEventArgs>? TabCreated;
    public event EventHandler<TabClosedEventArgs>? TabClosed;

    public TabNavigationManager(IUndoRedoProvider globalUndoRedoProvider)
    {
        _globalUndoRedoProvider = globalUndoRedoProvider;
    }

    public string CreateTab(string? tabId = null, string displayName = "New Tab")
    {
        tabId ??= Guid.NewGuid().ToString();

        var navigationStack = new NavigationStack<NavigationItem>(_globalUndoRedoProvider);
        navigationStack.NavigationChanged += (s, e) => OnTabNavigationChanged(tabId, e);

        _tabStacks[tabId] = navigationStack;

        TabCreated?.Invoke(this, new TabCreatedEventArgs(tabId, displayName));

        if (_activeTabId == null)
        {
            SwitchToTab(tabId);
        }

        return tabId;
    }

    public void CloseTab(string tabId)
    {
        if (_tabStacks.Remove(tabId))
        {
            TabClosed?.Invoke(this, new TabClosedEventArgs(tabId));

            if (_activeTabId == tabId)
            {
                var remainingTab = _tabStacks.Keys.FirstOrDefault();
                if (remainingTab != null)
                {
                    SwitchToTab(remainingTab);
                }
                else
                {
                    _activeTabId = null;
                    ActiveTabChanged?.Invoke(this, new TabChangedEventArgs(null, null));
                }
            }
        }
    }

    public void SwitchToTab(string tabId)
    {
        if (_tabStacks.ContainsKey(tabId))
        {
            var previousTabId = _activeTabId;
            _activeTabId = tabId;

            var currentItem = _tabStacks[tabId].Current;
            ActiveTabChanged?.Invoke(this, new TabChangedEventArgs(previousTabId, tabId));
        }
    }

    public void NavigateInTab(string tabId, NavigationItem item)
    {
        if (_tabStacks.TryGetValue(tabId, out var stack))
        {
            stack.NavigateTo(item);
        }
    }

    public void NavigateInActiveTab(NavigationItem item)
    {
        if (_activeTabId != null && _tabStacks.TryGetValue(_activeTabId, out var stack))
        {
            stack.NavigateTo(item);
        }
    }

    public void GoBackInActiveTab()
    {
        if (_activeTabId != null && _tabStacks.TryGetValue(_activeTabId, out var stack))
        {
            stack.GoBack();
        }
    }

    public void GoForwardInActiveTab()
    {
        if (_activeTabId != null && _tabStacks.TryGetValue(_activeTabId, out var stack))
        {
            stack.GoForward();
        }
    }

    public INavigationStack<NavigationItem>? GetActiveTabStack()
    {
        return _activeTabId != null ? _tabStacks.GetValueOrDefault(_activeTabId) : null;
    }

    public IReadOnlyDictionary<string, INavigationStack<NavigationItem>> GetAllTabs()
    {
        return _tabStacks.AsReadOnly();
    }

    private void OnTabNavigationChanged(string tabId, NavigationEventArgs<NavigationItem> e)
    {
        // Only update UI if this is the active tab
        if (tabId == _activeTabId)
        {
            // Update navigation buttons, URL bar, etc.
            UpdateUIForNavigation(e);
        }

        // Always track for history/analytics
        TrackTabNavigation(tabId, e);
    }

    private void UpdateUIForNavigation(NavigationEventArgs<NavigationItem> e)
    {
        // Update UI elements
    }

    private void TrackTabNavigation(string tabId, NavigationEventArgs<NavigationItem> e)
    {
        // Analytics tracking
    }
}

public class TabChangedEventArgs : EventArgs
{
    public string? PreviousTabId { get; }
    public string? NewTabId { get; }

    public TabChangedEventArgs(string? previousTabId, string? newTabId)
    {
        PreviousTabId = previousTabId;
        NewTabId = newTabId;
    }
}

public class TabCreatedEventArgs : EventArgs
{
    public string TabId { get; }
    public string DisplayName { get; }

    public TabCreatedEventArgs(string tabId, string displayName)
    {
        TabId = tabId;
        DisplayName = displayName;
    }
}

public class TabClosedEventArgs : EventArgs
{
    public string TabId { get; }

    public TabClosedEventArgs(string tabId)
    {
        TabId = tabId;
    }
}
```

### Testing Examples

```csharp
[TestClass]
public class NavigationExampleTests
{
    [TestMethod]
    public void BasicNavigation_WorksCorrectly()
    {
        // Arrange
        var stack = new NavigationStack<NavigationItem>();
        var item1 = new NavigationItem("1", "Page 1");
        var item2 = new NavigationItem("2", "Page 2");

        // Act
        stack.NavigateTo(item1);
        stack.NavigateTo(item2);

        // Assert
        Assert.AreEqual(item2, stack.Current);
        Assert.IsTrue(stack.CanGoBack);
        Assert.IsFalse(stack.CanGoForward);
        Assert.AreEqual(2, stack.Count);
    }

    [TestMethod]
    public async Task PersistenceProvider_SavesAndLoadsState()
    {
        // Arrange
        var provider = new InMemoryPersistenceProvider<NavigationItem>();
        var stack1 = new NavigationStack<NavigationItem>(null, provider);

        var item1 = new NavigationItem("1", "Page 1");
        var item2 = new NavigationItem("2", "Page 2");

        // Act
        stack1.NavigateTo(item1);
        stack1.NavigateTo(item2);
        await stack1.SaveStateAsync();

        var stack2 = new NavigationStack<NavigationItem>(null, provider);
        var loaded = await stack2.LoadStateAsync();

        // Assert
        Assert.IsTrue(loaded);
        Assert.AreEqual("Page 2", stack2.Current?.DisplayName);
        Assert.AreEqual(2, stack2.Count);
        Assert.IsTrue(stack2.CanGoBack);
    }

    [TestMethod]
    public void UndoRedoProvider_UndoesNavigation()
    {
        // Arrange
        var undoProvider = new SimpleUndoRedoProvider();
        var stack = new NavigationStack<NavigationItem>(undoProvider);

        var item1 = new NavigationItem("1", "Page 1");
        var item2 = new NavigationItem("2", "Page 2");

        // Act
        stack.NavigateTo(item1);
        stack.NavigateTo(item2);

        Assert.AreEqual("Page 2", stack.Current?.DisplayName);

        undoProvider.Undo(); // Should undo the navigation to Page 2

        // Assert
        Assert.AreEqual("Page 1", stack.Current?.DisplayName);
        Assert.IsTrue(undoProvider.CanRedo);
    }

    [TestMethod]
    public void NavigationEvents_FireCorrectly()
    {
        // Arrange
        var stack = new NavigationStack<NavigationItem>();
        var eventsFired = new List<NavigationEventArgs<NavigationItem>>();

        stack.NavigationChanged += (s, e) => eventsFired.Add(e);

        var item1 = new NavigationItem("1", "Page 1");
        var item2 = new NavigationItem("2", "Page 2");

        // Act
        stack.NavigateTo(item1);
        stack.NavigateTo(item2);
        stack.GoBack();

        // Assert
        Assert.AreEqual(3, eventsFired.Count);

        Assert.AreEqual(NavigationType.NavigateTo, eventsFired[0].NavigationType);
        Assert.IsNull(eventsFired[0].PreviousItem);
        Assert.AreEqual("Page 1", eventsFired[0].CurrentItem?.DisplayName);

        Assert.AreEqual(NavigationType.NavigateTo, eventsFired[1].NavigationType);
        Assert.AreEqual("Page 1", eventsFired[1].PreviousItem?.DisplayName);
        Assert.AreEqual("Page 2", eventsFired[1].CurrentItem?.DisplayName);

        Assert.AreEqual(NavigationType.GoBack, eventsFired[2].NavigationType);
        Assert.AreEqual("Page 2", eventsFired[2].PreviousItem?.DisplayName);
        Assert.AreEqual("Page 1", eventsFired[2].CurrentItem?.DisplayName);
    }
}
```

These examples demonstrate the flexibility and power of the Navigation Stack Library across various scenarios, from simple navigation to complex multi-tab applications with persistence and undo/redo functionality.
