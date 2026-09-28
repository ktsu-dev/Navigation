// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Test;

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
}
