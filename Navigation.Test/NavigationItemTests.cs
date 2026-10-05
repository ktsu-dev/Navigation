// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Test;

using ktsu.Navigation.Models;
using ktsu.Navigation.Services;

[TestClass]
public class NavigationItemTests
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
	[DataRow("")]
	[DataRow("   ")]
	[DataRow(null)]
	public void DisplayName_SetToNullOrWhitespace_ThrowsAndKeepsPreviousName(string? displayName)
	{
		// Arrange
		NavigationItem item = new("a", "A");

		// Act & Assert
		Assert.ThrowsExactly<ArgumentException>(() => item.DisplayName = displayName!);
		Assert.AreEqual("A", item.DisplayName);
	}

	[TestMethod]
	public void DisplayName_SetToValidName_UpdatesName()
	{
		// Arrange
		NavigationItem item = new("a", "A");
		Assert.AreEqual("A", item.DisplayName);

		// Act
		item.DisplayName = "Renamed";

		// Assert
		Assert.AreEqual("Renamed", item.DisplayName);
	}

	[TestMethod]
	public async Task SaveThenLoad_AfterRenamingItems_RestoresEveryItem()
	{
		// Arrange
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);
		Navigation<NavigationItem> navigation = new(null, provider);
		navigation.NavigateTo(new NavigationItem("home", "Home"));
		navigation.NavigateTo(new NavigationItem("a", "A"));
		NavigationItem renamed = new("c", "Loading...");
		navigation.NavigateTo(renamed);
		renamed.DisplayName = "Page C";
		Assert.ThrowsExactly<ArgumentException>(() => renamed.DisplayName = "");
		await navigation.SaveStateAsync().ConfigureAwait(false);

		// Act
		Navigation<NavigationItem> restored = new(null, provider);
		bool loaded = await restored.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsTrue(loaded);
		Assert.AreEqual("home,a,c", string.Join(",", restored.GetHistory().Select(item => item.Id)));
		Assert.AreEqual("Page C", restored.Current?.DisplayName);
	}

	[TestMethod]
	public async Task LoadStateAsync_FileWithEmptyDisplayName_IsRejected()
	{
		// Arrange: a file written before the setter was validated
		await File.WriteAllTextAsync(_filePath!, """{"items":[{"id":"a","displayName":""}],"currentIndex":0}""").ConfigureAwait(false);
		JsonFilePersistenceProvider<NavigationItem> provider = new(_filePath!);

		// Act & Assert
		Assert.IsNull(await provider.LoadStateAsync().ConfigureAwait(false));
	}
}
