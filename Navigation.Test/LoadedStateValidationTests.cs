// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Test;

using ktsu.Navigation.Contracts;
using ktsu.Navigation.Models;
using ktsu.Navigation.Services;

[TestClass]
public class LoadedStateValidationTests
{
	[TestMethod]
	public void NavigationState_NoCurrentIndexWithItems_Throws() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new NavigationState<NavigationItem>([new NavigationItem("a", "A")], -1));

	[TestMethod]
	public void NavigationState_NoCurrentIndexWithNoItems_IsAccepted()
	{
		NavigationState<NavigationItem> state = new([], -1);

		Assert.AreEqual(-1, state.CurrentIndex);
	}

	[TestMethod]
	[DataRow(-1, DisplayName = "No current item, although there are items")]
	[DataRow(2, DisplayName = "Current index past the last item")]
	[DataRow(-2, DisplayName = "Current index below -1")]
	public async Task LoadStateAsync_CustomProviderWithInvalidIndex_ReturnsFalseAndLeavesStackUnchanged(int currentIndex)
	{
		// Arrange: a custom provider is not held to NavigationState's constructor checks
		UncheckedState state = new([new NavigationItem("a", "A"), new NavigationItem("b", "B")], currentIndex);
		Navigation<NavigationItem> navigation = CreateNavigationWithOnePage(state);

		// Act
		bool loaded = await navigation.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsFalse(loaded);
		AssertUnchanged(navigation);
	}

	[TestMethod]
	public async Task LoadStateAsync_CustomProviderWithNullItem_ReturnsFalseAndLeavesStackUnchanged()
	{
		// Arrange
		UncheckedState state = new([new NavigationItem("a", "A"), null!], 0);
		Navigation<NavigationItem> navigation = CreateNavigationWithOnePage(state);

		// Act
		bool loaded = await navigation.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsFalse(loaded);
		AssertUnchanged(navigation);
	}

	[TestMethod]
	public async Task LoadStateAsync_CustomProviderWithValidState_Loads()
	{
		// Arrange
		UncheckedState state = new([new NavigationItem("a", "A"), new NavigationItem("b", "B")], 1);
		Navigation<NavigationItem> navigation = CreateNavigationWithOnePage(state);

		// Act
		bool loaded = await navigation.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsTrue(loaded);
		Assert.AreEqual("b", navigation.Current?.Id);
		Assert.IsTrue(navigation.CanGoBack);
	}

	[TestMethod]
	public async Task LoadStateAsync_JsonWithNoCurrentIndexButItems_ReturnsFalseAndLeavesStackUnchanged()
	{
		// Arrange
		string filePath = Path.Combine(Path.GetTempPath(), $"navigation-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(filePath, """{"items":[{"id":"a","displayName":"A"},{"id":"b","displayName":"B"}],"currentIndex":-1}""").ConfigureAwait(false);
			Navigation<NavigationItem> navigation = new(null, new JsonFilePersistenceProvider<NavigationItem>(filePath));
			navigation.NavigateTo(new NavigationItem("current", "Current"));

			// Act
			bool loaded = await navigation.LoadStateAsync().ConfigureAwait(false);

			// Assert
			Assert.IsFalse(loaded);
			AssertUnchanged(navigation);
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	private static Navigation<NavigationItem> CreateNavigationWithOnePage(INavigationState<NavigationItem> state)
	{
		Navigation<NavigationItem> navigation = new(null, new FixedStateProvider(state));
		navigation.NavigateTo(new NavigationItem("current", "Current"));
		return navigation;
	}

	private static void AssertUnchanged(Navigation<NavigationItem> navigation)
	{
		Assert.AreEqual(1, navigation.Count);
		Assert.AreEqual("current", navigation.Current?.Id);
	}

	/// <summary>
	/// A state that skips <see cref="NavigationState{T}"/>'s checks, as a custom provider's own state type can
	/// </summary>
	private sealed class UncheckedState(IReadOnlyList<NavigationItem> items, int currentIndex) : INavigationState<NavigationItem>
	{
		public IReadOnlyList<NavigationItem> Items { get; } = items;

		public int CurrentIndex { get; } = currentIndex;

		public DateTime CreatedAt { get; } = DateTime.UtcNow;
	}

	/// <summary>
	/// A provider that always loads the same state
	/// </summary>
	private sealed class FixedStateProvider(INavigationState<NavigationItem> state) : IPersistenceProvider<NavigationItem>
	{
		public Task SaveStateAsync(INavigationState<NavigationItem> state, CancellationToken cancellationToken = default) => Task.CompletedTask;

		public Task<INavigationState<NavigationItem>?> LoadStateAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<INavigationState<NavigationItem>?>(state);

		public Task<bool> HasSavedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

		public Task ClearSavedStateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	}
}
