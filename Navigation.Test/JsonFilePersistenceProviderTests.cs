// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Test;

using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using ktsu.Navigation.Contracts;
using ktsu.Navigation.Models;
using ktsu.Navigation.Services;

[TestClass]
public class JsonFilePersistenceProviderTests
{
	private string? _filePath;

	[TestInitialize]
	public void Setup() => _filePath = Path.Combine(Path.GetTempPath(), $"navigation-{Guid.NewGuid():N}.json");

	[TestCleanup]
	public void Cleanup()
	{
		if (File.Exists(_filePath))
		{
			RestoreReadAccess(_filePath!);
		}

		if (File.Exists(_filePath))
		{
			File.Delete(_filePath);
		}
	}

	[TestMethod]
	public async Task SaveThenLoad_IntoNewNavigation_RestoresCurrentAndStacks()
	{
		// Arrange
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);
		Navigation<NavigationItem> navigation = new(null, provider);
		navigation.NavigateTo(new NavigationItem("a", "A"));
		navigation.NavigateTo(new NavigationItem("b", "B"));
		navigation.NavigateTo(new NavigationItem("c", "C"));
		navigation.GoBack();
		await navigation.SaveStateAsync().ConfigureAwait(false);

		// Act
		Navigation<NavigationItem> restored = new(null, provider);
		bool loaded = await restored.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsTrue(loaded);
		Assert.AreEqual(3, restored.Count);
		Assert.AreEqual("b", restored.Current?.Id);
		Assert.AreEqual("B", restored.Current?.DisplayName);
		Assert.AreEqual("a", string.Join(",", restored.GetBackStack().Select(item => item.Id)));
		Assert.AreEqual("c", string.Join(",", restored.GetForwardStack().Select(item => item.Id)));
	}

	[TestMethod]
	public async Task LoadStateAsync_UnreadableData_ReturnsNull()
	{
		// Arrange
		await File.WriteAllTextAsync(_filePath!, """{"items":[{"id":"a","displayName":"A"}],"currentIndex":5}""").ConfigureAwait(false);
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);

		// Act & Assert
		Assert.IsNull(await provider.LoadStateAsync().ConfigureAwait(false));
	}

	[TestMethod]
	public void NavigationItem_JsonRoundTrip_KeepsCreatedAtAndMetadata()
	{
		// Arrange
		string json = """{"Id":"x","DisplayName":"X","CreatedAt":"2020-01-01T00:00:00Z","Metadata":{"k":"v"}}""";

		// Act
		NavigationItem item = JsonSerializer.Deserialize<NavigationItem>(json)!;

		// Assert
		Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), item.CreatedAt);
		Assert.HasCount(1, item.Metadata);
		Assert.AreEqual("v", ((JsonElement)item.Metadata["k"]).GetString());
	}

	[TestMethod]
	public async Task LoadStateAsync_KeepsSavedStateCreatedAt()
	{
		// Arrange
		await File.WriteAllTextAsync(_filePath!, """{"items":[{"id":"a","displayName":"A"}],"currentIndex":0,"createdAt":"2020-01-01T00:00:00Z"}""").ConfigureAwait(false);
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);

		// Act
		INavigationState<NavigationItem>? state = await provider.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(state);
		Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), state.CreatedAt);
	}

	[TestMethod]
	public async Task SaveThenLoad_KeepsStateCreatedAt()
	{
		// Arrange
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);
		List<NavigationItem> items = [new NavigationItem("a", "A")];
		NavigationState<NavigationItem> saved = new(items, 0);
		await provider.SaveStateAsync(saved).ConfigureAwait(false);
		await Task.Delay(50).ConfigureAwait(false);

		// Act
		INavigationState<NavigationItem>? loaded = await provider.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(loaded);
		Assert.AreEqual(saved.CreatedAt, loaded.CreatedAt);
	}

	[TestMethod]
	public async Task SaveThenLoad_KeepsItemCreatedAtAndMetadata()
	{
		// Arrange
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);
		Navigation<NavigationItem> navigation = new(null, provider);
		NavigationItem item = new("a", "A");
		item.SetMetadata("scroll", 42);
		navigation.NavigateTo(item);
		await navigation.SaveStateAsync().ConfigureAwait(false);

		// Act
		Navigation<NavigationItem> restored = new(null, provider);
		bool loaded = await restored.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsTrue(loaded);
		NavigationItem current = restored.Current!;
		Assert.AreEqual(item.CreatedAt, current.CreatedAt);
		Assert.HasCount(1, current.Metadata);
		Assert.AreEqual(42, ((JsonElement)current.Metadata["scroll"]).GetInt32());
	}

	[TestMethod]
	public async Task LoadStateAsync_NullItem_ReturnsFalseAndLeavesStackUnchanged()
	{
		// Arrange
		await File.WriteAllTextAsync(_filePath!, """{"items":[null],"currentIndex":0}""").ConfigureAwait(false);
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);
		Navigation<NavigationItem> navigation = new(null, provider);
		navigation.NavigateTo(new NavigationItem("a", "A"));

		// Act
		bool loaded = await navigation.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsFalse(loaded);
		Assert.AreEqual(1, navigation.Count);
		Assert.AreEqual("a", navigation.Current?.Id);
	}

	[TestMethod]
	public async Task LoadStateAsync_UnreadableFile_ReturnsNull()
	{
		// Arrange
		await File.WriteAllTextAsync(_filePath!, """{"items":[{"id":"a","displayName":"A"}],"currentIndex":0}""").ConfigureAwait(false);
		RevokeReadAccess(_filePath!);
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);

		// Act
		INavigationState<NavigationItem>? state = await provider.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsNull(state);
	}

	[TestMethod]
	public async Task NavigationLoadStateAsync_UnreadableFile_ReportsNoStateLoaded()
	{
		// Arrange
		await File.WriteAllTextAsync(_filePath!, """{"items":[{"id":"x","displayName":"X"}],"currentIndex":0}""").ConfigureAwait(false);
		RevokeReadAccess(_filePath!);
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);
		Navigation<NavigationItem> navigation = new(null, provider);
		navigation.NavigateTo(new NavigationItem("a", "A"));

		// Act
		bool loaded = await navigation.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsFalse(loaded);
		Assert.AreEqual(1, navigation.Count);
		Assert.AreEqual("a", navigation.Current?.Id);
	}

	/// <summary>
	/// Makes a file unreadable by the current process: a deny-read ACL on Windows, mode 000 elsewhere.
	/// Marks the test inconclusive when the process can read the file anyway, as root can.
	/// </summary>
	private static void RevokeReadAccess(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			FileInfo file = new(path);
			FileSecurity security = file.GetAccessControl();
			security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.ReadData, AccessControlType.Deny));
			file.SetAccessControl(security);
		}
		else
		{
			File.SetUnixFileMode(path, UnixFileMode.None);
		}

		try
		{
			using FileStream stream = File.OpenRead(path);
		}
		catch (UnauthorizedAccessException)
		{
			return;
		}

		Assert.Inconclusive("The test process can read a file it has no read permission on, for example because it runs as root.");
	}

	private static void RestoreReadAccess(string path)
	{
		if (OperatingSystem.IsWindows())
		{
			FileInfo file = new(path);
			FileSecurity security = file.GetAccessControl();
			security.RemoveAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.ReadData, AccessControlType.Deny));
			file.SetAccessControl(security);
		}
		else
		{
			File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
		}
	}
}
