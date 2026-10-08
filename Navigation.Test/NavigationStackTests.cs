// Copyright (c) 2023-2026 ktsu-dev contributors

[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.Parallelize(Scope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope.MethodLevel)]

namespace ktsu.Navigation.Test;

using ktsu.Navigation.Contracts;
using ktsu.Navigation.Models;
using ktsu.Navigation.Services;

[TestClass]
public class NavigationStackTests
{
	private Navigation<NavigationItem>? _navigation;
	private SimpleUndoRedoProvider? _undoRedoProvider;
	private InMemoryPersistenceProvider<NavigationItem>? _persistenceProvider;

	[TestInitialize]
	public void Setup()
	{
		_undoRedoProvider = new SimpleUndoRedoProvider();
		_persistenceProvider = new InMemoryPersistenceProvider<NavigationItem>();
		_navigation = new Navigation<NavigationItem>(_undoRedoProvider, _persistenceProvider);
	}

	[TestMethod]
	public void NavigationStack_InitialState_IsEmpty()
	{
		// Arrange & Act
		// Setup already creates the navigation stack

		// Assert
		Assert.IsNull(_navigation!.Current);
		Assert.IsFalse(_navigation.CanGoBack);
		Assert.IsFalse(_navigation.CanGoForward);
		Assert.AreEqual(0, _navigation.Count);
	}

	[TestMethod]
	public void NavigateTo_SingleItem_SetsCurrent()
	{
		// Arrange
		NavigationItem item = new("1", "First Item");

		// Act
		_navigation!.NavigateTo(item);

		// Assert
		Assert.AreEqual(item, _navigation.Current);
		Assert.IsFalse(_navigation.CanGoBack);
		Assert.IsFalse(_navigation.CanGoForward);
		Assert.AreEqual(1, _navigation.Count);
	}

	[TestMethod]
	public void NavigateTo_MultipleItems_AllowsBackNavigation()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");

		// Act
		_navigation!.NavigateTo(item1);
		_navigation.NavigateTo(item2);

		// Assert
		Assert.AreEqual(item2, _navigation.Current);
		Assert.IsTrue(_navigation.CanGoBack);
		Assert.IsFalse(_navigation.CanGoForward);
		Assert.AreEqual(2, _navigation.Count);
	}

	[TestMethod]
	public void GoBack_WithHistory_NavigatesToPrevious()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");
		_navigation!.NavigateTo(item1);
		_navigation.NavigateTo(item2);

		// Act
		NavigationItem? result = _navigation.GoBack();

		// Assert
		Assert.AreEqual(item1, result);
		Assert.AreEqual(item1, _navigation.Current);
		Assert.IsFalse(_navigation.CanGoBack);
		Assert.IsTrue(_navigation.CanGoForward);
	}

	[TestMethod]
	public void GoForward_WithForwardHistory_NavigatesToNext()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");
		_navigation!.NavigateTo(item1);
		_navigation.NavigateTo(item2);
		_navigation.GoBack();

		// Act
		NavigationItem? result = _navigation.GoForward();

		// Assert
		Assert.AreEqual(item2, result);
		Assert.AreEqual(item2, _navigation.Current);
		Assert.IsTrue(_navigation.CanGoBack);
		Assert.IsFalse(_navigation.CanGoForward);
	}

	[TestMethod]
	public void NavigateTo_ClearsForwardHistory()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");
		NavigationItem item3 = new("3", "Third Item");
		_navigation!.NavigateTo(item1);
		_navigation.NavigateTo(item2);
		_navigation.GoBack();

		// Act
		_navigation.NavigateTo(item3);

		// Assert
		Assert.AreEqual(item3, _navigation.Current);
		Assert.IsTrue(_navigation.CanGoBack);
		Assert.IsFalse(_navigation.CanGoForward);
		Assert.AreEqual(2, _navigation.Count);
	}

	[TestMethod]
	public void Clear_RemovesAllItems()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");
		_navigation!.NavigateTo(item1);
		_navigation.NavigateTo(item2);

		// Act
		_navigation.Clear();

		// Assert
		Assert.IsNull(_navigation.Current);
		Assert.IsFalse(_navigation.CanGoBack);
		Assert.IsFalse(_navigation.CanGoForward);
		Assert.AreEqual(0, _navigation.Count);
	}

	[TestMethod]
	public void NavigationChanged_RaisedOnNavigation()
	{
		// Arrange
		bool eventRaised = false;
		NavigationEventArgs<NavigationItem>? eventArgs = null;
		_navigation!.NavigationChanged += (sender, e) =>
		{
			eventRaised = true;
			eventArgs = e;
		};
		NavigationItem item = new("1", "First Item");

		// Act
		_navigation.NavigateTo(item);

		// Assert
		Assert.IsTrue(eventRaised);
		Assert.IsNotNull(eventArgs);
		Assert.AreEqual(NavigationType.NavigateTo, eventArgs.NavigationType);
		Assert.IsNull(eventArgs.PreviousItem);
		Assert.AreEqual(item, eventArgs.CurrentItem);
	}

	[TestMethod]
	public void UndoRedo_WithUndoRedoProvider_WorksCorrectly()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");
		_navigation!.NavigateTo(item1);

		// Act
		_navigation.NavigateTo(item2);
		_undoRedoProvider!.Undo();

		// Assert
		Assert.AreEqual(item1, _navigation.Current);
		Assert.IsTrue(_undoRedoProvider.CanRedo);

		// Act - Redo
		_undoRedoProvider.Redo();

		// Assert
		Assert.AreEqual(item2, _navigation.Current);
	}

	[TestMethod]
	public void GetBackStack_ReturnsCorrectItems()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");
		NavigationItem item3 = new("3", "Third Item");
		_navigation!.NavigateTo(item1);
		_navigation.NavigateTo(item2);
		_navigation.NavigateTo(item3);

		// Act
		IReadOnlyList<NavigationItem> backStack = _navigation.GetBackStack();

		// Assert
		Assert.HasCount(2, backStack);
		Assert.AreEqual(item1, backStack[0]);
		Assert.AreEqual(item2, backStack[1]);
	}

	[TestMethod]
	public void GetForwardStack_ReturnsCorrectItems()
	{
		// Arrange
		NavigationItem item1 = new("1", "First Item");
		NavigationItem item2 = new("2", "Second Item");
		NavigationItem item3 = new("3", "Third Item");
		_navigation!.NavigateTo(item1);
		_navigation.NavigateTo(item2);
		_navigation.NavigateTo(item3);
		_navigation.GoBack();
		_navigation.GoBack();

		// Act
		IReadOnlyList<NavigationItem> forwardStack = _navigation.GetForwardStack();

		// Assert
		Assert.HasCount(2, forwardStack);
		Assert.AreEqual(item2, forwardStack[0]);
		Assert.AreEqual(item3, forwardStack[1]);
	}

	[TestMethod]
	public async Task SaveThenLoad_CurrentPageRepeatedInHistory_RestoresPosition()
	{
		// Arrange
		_navigation!.NavigateTo(new NavigationItem("home", "Home"));
		_navigation.NavigateTo(new NavigationItem("about", "About"));
		_navigation.NavigateTo(new NavigationItem("home", "Home"));
		await _navigation.SaveStateAsync().ConfigureAwait(false);

		// Act
		Navigation<NavigationItem> restored = new(null, _persistenceProvider);
		bool loaded = await restored.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsTrue(loaded);
		Assert.IsTrue(restored.CanGoBack);
		Assert.IsFalse(restored.CanGoForward);
		Assert.AreEqual("home,about", string.Join(",", restored.GetBackStack().Select(item => item.Id)));
	}

	[TestMethod]
	public async Task LoadStateAsync_RaisesOneNavigateToEventFromThePreviousPage()
	{
		// Arrange
		Navigation<NavigationItem> saved = new(null, _persistenceProvider);
		saved.NavigateTo(new NavigationItem("x", "X"));
		await saved.SaveStateAsync().ConfigureAwait(false);

		NavigationItem previous = new("a", "A");
		_navigation!.NavigateTo(previous);
		List<NavigationEventArgs<NavigationItem>> events = [];
		_navigation.NavigationChanged += (sender, e) => events.Add(e);

		// Act
		bool loaded = await _navigation.LoadStateAsync().ConfigureAwait(false);

		// Assert
		Assert.IsTrue(loaded);
		Assert.HasCount(1, events);
		Assert.AreEqual(NavigationType.NavigateTo, events[0].NavigationType);
		Assert.AreEqual(previous, events[0].PreviousItem);
		Assert.AreEqual("x", events[0].CurrentItem?.Id);
	}

	[TestMethod]
	public void Undo_AfterGoBack_UndoesTheGoBackAndKeepsHistory()
	{
		// Arrange
		NavigateToABC();
		_navigation!.GoBack();
		_navigation.GoBack();

		// Act
		_undoRedoProvider!.Undo();

		// Assert
		AssertStack("A,B,C", "B");

		// Act - Redo
		_undoRedoProvider.Redo();

		// Assert
		AssertStack("A,B,C", "A");
	}

	[TestMethod]
	public void Undo_AfterGoForward_UndoesTheGoForward()
	{
		// Arrange
		NavigateToABC();
		_navigation!.GoBack();
		_navigation.GoBack();
		_navigation.GoForward();

		// Act
		_undoRedoProvider!.Undo();

		// Assert
		AssertStack("A,B,C", "A");
	}

	[TestMethod]
	public void Undo_AfterClear_RestoresClearedHistory()
	{
		// Arrange
		NavigateToABC();
		_navigation!.Clear();
		Assert.IsTrue(_undoRedoProvider!.CanUndo);

		// Act
		_undoRedoProvider.Undo();

		// Assert
		AssertStack("A,B,C", "C");

		// Act - Redo
		_undoRedoProvider.Redo();

		// Assert
		Assert.AreEqual(0, _navigation.Count);
		Assert.IsNull(_navigation.Current);
	}

	[TestMethod]
	public void Clear_OnEmptyStack_RecordsNothing()
	{
		// Act
		_navigation!.Clear();

		// Assert
		Assert.IsFalse(_undoRedoProvider!.CanUndo);
	}

	[TestMethod]
	public async Task Undo_AfterLoadStateAsync_RestoresPreLoadState()
	{
		// Arrange
		Navigation<NavigationItem> saver = new(null, _persistenceProvider);
		saver.NavigateTo(new NavigationItem("X", "X"));
		saver.NavigateTo(new NavigationItem("Y", "Y"));
		await saver.SaveStateAsync().ConfigureAwait(false);
		_navigation!.NavigateTo(new NavigationItem("A", "A"));
		_navigation.NavigateTo(new NavigationItem("B", "B"));
		Assert.IsTrue(await _navigation.LoadStateAsync().ConfigureAwait(false));
		AssertStack("X,Y", "Y");

		// Act
		_undoRedoProvider!.Undo();

		// Assert
		AssertStack("A,B", "B");

		// Act - Redo
		_undoRedoProvider.Redo();

		// Assert
		AssertStack("X,Y", "Y");
	}

	[TestMethod]
	public void GoBack_WithoutUndoProvider_StillNavigates()
	{
		// Arrange
		Navigation<NavigationItem> navigation = new();
		navigation.NavigateTo(new NavigationItem("A", "A"));
		navigation.NavigateTo(new NavigationItem("B", "B"));

		// Act
		NavigationItem? current = navigation.GoBack();

		// Assert
		Assert.AreEqual("A", current?.Id);
	}

	[TestMethod]
	public void NavigateTo_FromManyThreads_KeepsEveryItemAndAValidCurrent()
	{
		// Act
		Parallel.For(0, 2000, i => _navigation!.NavigateTo(new NavigationItem("p" + i, "P" + i)));

		// Assert
		IReadOnlyList<NavigationItem> history = _navigation!.GetHistory();
		Assert.AreEqual(2000, history.Count);
		Assert.AreEqual(2000, history.Select(item => item.Id).Distinct().Count());
		Assert.AreSame(history[^1], _navigation.Current);
		Assert.AreEqual(1999, _navigation.GetBackStack().Count);
		Assert.IsFalse(_navigation.CanGoForward);
	}

	private void NavigateToABC()
	{
		_navigation!.NavigateTo(new NavigationItem("A", "A"));
		_navigation.NavigateTo(new NavigationItem("B", "B"));
		_navigation.NavigateTo(new NavigationItem("C", "C"));
	}

	private void AssertStack(string expectedIds, string expectedCurrentId)
	{
		Assert.AreEqual(expectedIds, string.Join(",", _navigation!.GetHistory().Select(item => item.Id)));
		Assert.AreEqual(expectedCurrentId, _navigation.Current?.Id);
	}
}
