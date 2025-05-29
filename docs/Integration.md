# Integration Guide

This guide covers how to integrate the Navigation Stack Library with external systems, frameworks, and dependency injection containers.

## Dependency Injection Integration

### ASP.NET Core

Register navigation services in your ASP.NET Core application:

```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register core services
    services.AddSingleton<IUndoRedoProvider, SimpleUndoRedoProvider>();
    services.AddScoped(typeof(IPersistenceProvider<>), typeof(JsonFilePersistenceProvider<>));
    services.AddScoped<NavigationStackFactory>();

    // Register navigation stacks
    services.AddScoped<INavigationStack<PageNavigationItem>>(provider =>
    {
        var factory = provider.GetRequiredService<NavigationStackFactory>();
        var persistenceProvider = provider.GetRequiredService<IPersistenceProvider<PageNavigationItem>>();
        return factory.CreateNavigationStack(persistenceProvider);
    });

    // Register custom navigation items
    services.AddTransient<PageNavigationItem>();
}

// Usage in controllers
[ApiController]
[Route("api/[controller]")]
public class NavigationController : ControllerBase
{
    private readonly INavigationStack<PageNavigationItem> _navigationStack;

    public NavigationController(INavigationStack<PageNavigationItem> navigationStack)
    {
        _navigationStack = navigationStack;
    }

    [HttpPost("navigate")]
    public async Task<IActionResult> NavigateTo([FromBody] NavigationRequest request)
    {
        var item = new PageNavigationItem(request.Id, request.DisplayName, request.Url);
        _navigationStack.NavigateTo(item);

        if (_navigationStack is NavigationStack<PageNavigationItem> stack)
        {
            await stack.SaveStateAsync();
        }

        return Ok();
    }
}
```

### WPF with Microsoft.Extensions.DependencyInjection

```csharp
// App.xaml.cs
public partial class App : Application
{
    private ServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Navigation services
        services.AddSingleton<IUndoRedoProvider>(provider =>
            new SimpleUndoRedoProvider(maxHistorySize: 100));

        services.AddSingleton<IPersistenceProvider<ViewNavigationItem>>(provider =>
            new JsonFilePersistenceProvider<ViewNavigationItem>(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                           "MyApp", "navigation.json")));

        services.AddSingleton<NavigationStackFactory>();

        services.AddSingleton<INavigationStack<ViewNavigationItem>>(provider =>
        {
            var factory = provider.GetRequiredService<NavigationStackFactory>();
            var undoProvider = provider.GetRequiredService<IUndoRedoProvider>();
            var persistenceProvider = provider.GetRequiredService<IPersistenceProvider<ViewNavigationItem>>();
            return factory.CreateNavigationStack(undoProvider, persistenceProvider);
        });

        // ViewModels and Views
        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();
    }
}

// MainViewModel.cs
public class MainViewModel : INotifyPropertyChanged
{
    private readonly INavigationStack<ViewNavigationItem> _navigationStack;
    private readonly IUndoRedoProvider _undoRedoProvider;

    public MainViewModel(INavigationStack<ViewNavigationItem> navigationStack, IUndoRedoProvider undoRedoProvider)
    {
        _navigationStack = navigationStack;
        _undoRedoProvider = undoRedoProvider;

        _navigationStack.NavigationChanged += OnNavigationChanged;
        _undoRedoProvider.StateChanged += OnUndoRedoStateChanged;

        SetupCommands();
    }

    public ICommand NavigateCommand { get; private set; }
    public ICommand BackCommand { get; private set; }
    public ICommand ForwardCommand { get; private set; }
    public ICommand UndoCommand { get; private set; }
    public ICommand RedoCommand { get; private set; }

    private void SetupCommands()
    {
        NavigateCommand = new RelayCommand<string>(NavigateTo);
        BackCommand = new RelayCommand(() => _navigationStack.GoBack(), () => _navigationStack.CanGoBack);
        ForwardCommand = new RelayCommand(() => _navigationStack.GoForward(), () => _navigationStack.CanGoForward);
        UndoCommand = new RelayCommand(() => _undoRedoProvider.Undo(), () => _undoRedoProvider.CanUndo);
        RedoCommand = new RelayCommand(() => _undoRedoProvider.Redo(), () => _undoRedoProvider.CanRedo);
    }
}
```

### Autofac Integration

```csharp
public class NavigationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register providers
        builder.RegisterType<SimpleUndoRedoProvider>()
               .As<IUndoRedoProvider>()
               .SingleInstance()
               .WithParameter("maxHistorySize", 100);

        builder.RegisterGeneric(typeof(JsonFilePersistenceProvider<>))
               .As(typeof(IPersistenceProvider<>))
               .InstancePerLifetimeScope();

        // Register factory
        builder.RegisterType<NavigationStackFactory>()
               .AsSelf()
               .SingleInstance();

        // Register navigation stacks
        builder.Register<INavigationStack<NavigationItem>>(context =>
        {
            var factory = context.Resolve<NavigationStackFactory>();
            var undoProvider = context.Resolve<IUndoRedoProvider>();
            var persistenceProvider = context.Resolve<IPersistenceProvider<NavigationItem>>();
            return factory.CreateNavigationStack(undoProvider, persistenceProvider);
        }).InstancePerLifetimeScope();
    }
}

// Usage
var builder = new ContainerBuilder();
builder.RegisterModule<NavigationModule>();
var container = builder.Build();

var navigationStack = container.Resolve<INavigationStack<NavigationItem>>();
```

## External Undo/Redo Integration

### Integration with Command Pattern Systems

```csharp
public class CommandBasedUndoRedoProvider : IUndoRedoProvider
{
    private readonly ICommandManager _commandManager; // External command system

    public CommandBasedUndoRedoProvider(ICommandManager commandManager)
    {
        _commandManager = commandManager;
        _commandManager.StateChanged += (s, e) => StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool CanUndo => _commandManager.CanUndo;
    public bool CanRedo => _commandManager.CanRedo;

    public event EventHandler? StateChanged;

    public void RegisterAction(IUndoableAction action, string description)
    {
        var command = new NavigationCommand(action, description);
        _commandManager.ExecuteCommand(command);
    }

    public bool Undo() => _commandManager.Undo();
    public bool Redo() => _commandManager.Redo();
    public void Clear() => _commandManager.Clear();

    private class NavigationCommand : ICommand
    {
        private readonly IUndoableAction _action;
        public string Description { get; }

        public NavigationCommand(IUndoableAction action, string description)
        {
            _action = action;
            Description = description;
        }

        public void Execute() => _action.Execute();
        public void Undo() => _action.Undo();
    }
}
```

### Integration with Microsoft Office-style Undo/Redo

```csharp
public class OfficeStyleUndoRedoProvider : IUndoRedoProvider
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private readonly int _maxActions;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    // Additional properties for Office-style menus
    public IEnumerable<string> UndoDescriptions => _undoStack.Select(a => a.Description).Reverse();
    public IEnumerable<string> RedoDescriptions => _redoStack.Select(a => a.Description).Reverse();

    public void RegisterAction(IUndoableAction action, string description)
    {
        _undoStack.Push(action);
        _redoStack.Clear();

        // Limit stack size
        while (_undoStack.Count > _maxActions)
        {
            var actionsToRemove = _undoStack.ToArray().Skip(_maxActions).ToArray();
            _undoStack.Clear();
            foreach (var a in actionsToRemove.Reverse().Take(_maxActions))
            {
                _undoStack.Push(a);
            }
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    // Undo multiple actions at once
    public bool UndoMultiple(int count)
    {
        for (int i = 0; i < count && CanUndo; i++)
        {
            if (!Undo()) return false;
        }
        return true;
    }
}
```

## Database Persistence Integration

### Entity Framework Core Integration

```csharp
public class NavigationState
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string NavigationData { get; set; } // JSON serialized
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DatabasePersistenceProvider<T> : IPersistenceProvider<T>
    where T : INavigationItem
{
    private readonly NavigationDbContext _context;
    private readonly string _userId;
    private readonly string _sessionId;
    private readonly JsonSerializerOptions _jsonOptions;

    public DatabasePersistenceProvider(NavigationDbContext context, string userId, string sessionId)
    {
        _context = context;
        _userId = userId;
        _sessionId = sessionId;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);

        var existingState = await _context.NavigationStates
            .FirstOrDefaultAsync(s => s.UserId == _userId && s.SessionId == _sessionId, cancellationToken);

        if (existingState != null)
        {
            existingState.NavigationData = json;
            existingState.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.NavigationStates.Add(new NavigationState
            {
                UserId = _userId,
                SessionId = _sessionId,
                NavigationData = json,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<INavigationState<T>?> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        var state = await _context.NavigationStates
            .FirstOrDefaultAsync(s => s.UserId == _userId && s.SessionId == _sessionId, cancellationToken);

        if (state?.NavigationData == null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<NavigationState<T>>(state.NavigationData, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<bool> HasSavedStateAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NavigationStates
            .AnyAsync(s => s.UserId == _userId && s.SessionId == _sessionId, cancellationToken);
    }

    public async Task ClearSavedStateAsync(CancellationToken cancellationToken = default)
    {
        var states = await _context.NavigationStates
            .Where(s => s.UserId == _userId && s.SessionId == _sessionId)
            .ToListAsync(cancellationToken);

        _context.NavigationStates.RemoveRange(states);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### Redis Integration

```csharp
public class RedisPersistenceProvider<T> : IPersistenceProvider<T>
    where T : INavigationItem
{
    private readonly IDatabase _database;
    private readonly string _keyPrefix;
    private readonly string _userId;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisPersistenceProvider(IConnectionMultiplexer redis, string userId, string keyPrefix = "nav:")
    {
        _database = redis.GetDatabase();
        _userId = userId;
        _keyPrefix = keyPrefix;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private string GetKey() => $"{_keyPrefix}{_userId}";

    public async Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await _database.StringSetAsync(GetKey(), json, TimeSpan.FromDays(30));
    }

    public async Task<INavigationState<T>?> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        var json = await _database.StringGetAsync(GetKey());
        if (!json.HasValue)
            return null;

        try
        {
            return JsonSerializer.Deserialize<NavigationState<T>>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<bool> HasSavedStateAsync(CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(GetKey());
    }

    public async Task ClearSavedStateAsync(CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(GetKey());
    }
}
```

## Framework Integration Examples

### Blazor Integration

```csharp
// NavigationService.cs
public class BlazorNavigationService
{
    private readonly INavigationStack<PageNavigationItem> _navigationStack;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    public BlazorNavigationService(
        INavigationStack<PageNavigationItem> navigationStack,
        NavigationManager navigationManager,
        IJSRuntime jsRuntime)
    {
        _navigationStack = navigationStack;
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;

        _navigationStack.NavigationChanged += OnNavigationChanged;
    }

    public async Task NavigateToAsync(string uri, string displayName = null)
    {
        var item = new PageNavigationItem(
            Guid.NewGuid().ToString(),
            displayName ?? uri,
            uri);

        _navigationStack.NavigateTo(item);

        // Update browser URL
        _navigationManager.NavigateTo(uri);

        // Update browser history
        await _jsRuntime.InvokeVoidAsync("history.pushState", null, displayName, uri);
    }

    private async void OnNavigationChanged(object sender, NavigationEventArgs<PageNavigationItem> e)
    {
        if (e.CurrentItem != null)
        {
            // Update browser title
            await _jsRuntime.InvokeVoidAsync("document.title = arguments[0]", e.CurrentItem.DisplayName);
        }
    }
}

// In Program.cs
builder.Services.AddScoped<BlazorNavigationService>();
builder.Services.AddScoped<INavigationStack<PageNavigationItem>>(provider =>
{
    var persistenceProvider = new JsonFilePersistenceProvider<PageNavigationItem>("blazor-nav.json");
    return new NavigationStack<PageNavigationItem>(null, persistenceProvider);
});
```

### MAUI Integration

```csharp
// MauiNavigationService.cs
public class MauiNavigationService : INavigationService
{
    private readonly INavigationStack<PageNavigationItem> _navigationStack;
    private readonly IServiceProvider _serviceProvider;

    public MauiNavigationService(INavigationStack<PageNavigationItem> navigationStack, IServiceProvider serviceProvider)
    {
        _navigationStack = navigationStack;
        _serviceProvider = serviceProvider;
        _navigationStack.NavigationChanged += OnNavigationChanged;
    }

    public async Task NavigateToAsync<TViewModel>() where TViewModel : class
    {
        var viewModelType = typeof(TViewModel);
        var viewType = GetViewTypeForViewModel(viewModelType);

        var item = new PageNavigationItem(
            viewModelType.Name,
            GetDisplayNameForViewModel(viewModelType),
            viewType.Name);

        _navigationStack.NavigateTo(item);

        // Perform actual MAUI navigation
        var page = (Page)_serviceProvider.GetRequiredService(viewType);
        await Shell.Current.Navigation.PushAsync(page);
    }

    public async Task GoBackAsync()
    {
        var previous = _navigationStack.GoBack();
        if (previous != null)
        {
            await Shell.Current.Navigation.PopAsync();
        }
    }

    private void OnNavigationChanged(object sender, NavigationEventArgs<PageNavigationItem> e)
    {
        // Update shell title, tab badges, etc.
        if (e.CurrentItem != null)
        {
            Shell.Current.Title = e.CurrentItem.DisplayName;
        }
    }
}

// In MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        // Register navigation services
        builder.Services.AddSingleton<INavigationStack<PageNavigationItem>>(provider =>
        {
            var persistenceProvider = new JsonFilePersistenceProvider<PageNavigationItem>(
                Path.Combine(FileSystem.AppDataDirectory, "navigation.json"));
            return new NavigationStack<PageNavigationItem>(null, persistenceProvider);
        });

        builder.Services.AddSingleton<MauiNavigationService>();

        return builder.Build();
    }
}
```

## Testing Integration

### Unit Testing with Mocks

```csharp
[TestClass]
public class NavigationIntegrationTests
{
    private Mock<IUndoRedoProvider> _mockUndoRedoProvider;
    private Mock<IPersistenceProvider<NavigationItem>> _mockPersistenceProvider;
    private INavigationStack<NavigationItem> _navigationStack;

    [TestInitialize]
    public void Setup()
    {
        _mockUndoRedoProvider = new Mock<IUndoRedoProvider>();
        _mockPersistenceProvider = new Mock<IPersistenceProvider<NavigationItem>>();

        _navigationStack = new NavigationStack<NavigationItem>(
            _mockUndoRedoProvider.Object,
            _mockPersistenceProvider.Object);
    }

    [TestMethod]
    public void NavigateTo_CallsUndoRedoProvider()
    {
        // Arrange
        var item = new NavigationItem("test", "Test");

        // Act
        _navigationStack.NavigateTo(item);

        // Assert
        _mockUndoRedoProvider.Verify(p => p.RegisterAction(
            It.IsAny<IUndoableAction>(),
            It.Is<string>(s => s.Contains("Test"))),
            Times.Once);
    }

    [TestMethod]
    public async Task SaveStateAsync_CallsPersistenceProvider()
    {
        // Arrange
        var item = new NavigationItem("test", "Test");
        _navigationStack.NavigateTo(item);

        // Act
        await ((NavigationStack<NavigationItem>)_navigationStack).SaveStateAsync();

        // Assert
        _mockPersistenceProvider.Verify(p => p.SaveStateAsync(
            It.IsAny<INavigationState<NavigationItem>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Integration Testing with TestContainers

```csharp
[TestClass]
public class DatabasePersistenceIntegrationTests
{
    private SqlServerContainer _sqlServerContainer;
    private NavigationDbContext _context;
    private DatabasePersistenceProvider<NavigationItem> _persistenceProvider;

    [TestInitialize]
    public async Task Setup()
    {
        _sqlServerContainer = new SqlServerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _sqlServerContainer.StartAsync();

        var connectionString = _sqlServerContainer.GetConnectionString();
        var options = new DbContextOptionsBuilder<NavigationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _context = new NavigationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _persistenceProvider = new DatabasePersistenceProvider<NavigationItem>(
            _context, "testuser", "testsession");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _context?.Dispose();
        await _sqlServerContainer.DisposeAsync();
    }

    [TestMethod]
    public async Task SaveAndLoadState_WorksWithDatabase()
    {
        // Arrange
        var items = new[]
        {
            new NavigationItem("1", "Page 1"),
            new NavigationItem("2", "Page 2")
        };
        var state = new NavigationState<NavigationItem>(items, 1);

        // Act
        await _persistenceProvider.SaveStateAsync(state);
        var loadedState = await _persistenceProvider.LoadStateAsync();

        // Assert
        Assert.IsNotNull(loadedState);
        Assert.AreEqual(2, loadedState.Items.Count);
        Assert.AreEqual(1, loadedState.CurrentIndex);
        Assert.AreEqual("Page 1", loadedState.Items[0].DisplayName);
        Assert.AreEqual("Page 2", loadedState.Items[1].DisplayName);
    }
}
```

## Configuration Patterns

### Configuration-based Provider Selection

```csharp
public class NavigationConfiguration
{
    public class PersistenceSettings
    {
        public string Provider { get; set; } = "Memory"; // Memory, File, Database, Redis
        public string ConnectionString { get; set; }
        public string FilePath { get; set; }
        public int CacheTimeoutMinutes { get; set; } = 30;
    }

    public class UndoRedoSettings
    {
        public string Provider { get; set; } = "Simple"; // Simple, External, None
        public int MaxHistorySize { get; set; } = 100;
        public bool EnableMultiLevelUndo { get; set; } = false;
    }
}

public class NavigationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NavigationConfiguration _config;

    public NavigationServiceFactory(IServiceProvider serviceProvider, IOptions<NavigationConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
    }

    public INavigationStack<T> CreateNavigationStack<T>() where T : INavigationItem
    {
        var undoProvider = CreateUndoRedoProvider();
        var persistenceProvider = CreatePersistenceProvider<T>();

        return new NavigationStack<T>(undoProvider, persistenceProvider);
    }

    private IUndoRedoProvider CreateUndoRedoProvider()
    {
        return _config.UndoRedo.Provider switch
        {
            "Simple" => new SimpleUndoRedoProvider(_config.UndoRedo.MaxHistorySize),
            "External" => _serviceProvider.GetService<IUndoRedoProvider>(),
            "None" => null,
            _ => throw new ArgumentException($"Unknown undo/redo provider: {_config.UndoRedo.Provider}")
        };
    }

    private IPersistenceProvider<T> CreatePersistenceProvider<T>() where T : INavigationItem
    {
        return _config.Persistence.Provider switch
        {
            "Memory" => new InMemoryPersistenceProvider<T>(),
            "File" => new JsonFilePersistenceProvider<T>(_config.Persistence.FilePath),
            "Database" => _serviceProvider.GetRequiredService<IPersistenceProvider<T>>(),
            "Redis" => _serviceProvider.GetRequiredService<IPersistenceProvider<T>>(),
            _ => throw new ArgumentException($"Unknown persistence provider: {_config.Persistence.Provider}")
        };
    }
}
```

This comprehensive integration guide provides patterns for integrating the Navigation Stack Library with various frameworks, persistence systems, and external providers while maintaining clean architecture and testability.
