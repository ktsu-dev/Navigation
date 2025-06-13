// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.Navigation.Core.Contracts;
using ktsu.Navigation.Core.Models;
using ktsu.Navigation.Core.Services;

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
		Assert.AreEqual(2, backStack.Count);
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
		Assert.AreEqual(2, forwardStack.Count);
		Assert.AreEqual(item2, forwardStack[0]);
		Assert.AreEqual(item3, forwardStack[1]);
	}
}
