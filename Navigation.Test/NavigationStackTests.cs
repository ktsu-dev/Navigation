// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.Navigation.Core.Contracts;
using ktsu.Navigation.Core.Models;
using ktsu.Navigation.Core.Services;

namespace ktsu.Navigation.Test;

[TestClass]
public class NavigationStackTests
{
	private INavigationStack<NavigationItem>? _navigationStack;
	private SimpleUndoRedoProvider? _undoRedoProvider;
	private InMemoryPersistenceProvider<NavigationItem>? _persistenceProvider;

	[TestInitialize]
	public void Setup()
	{
		_undoRedoProvider = new SimpleUndoRedoProvider();
		_persistenceProvider = new InMemoryPersistenceProvider<NavigationItem>();
		_navigationStack = new NavigationStack<NavigationItem>(_undoRedoProvider, _persistenceProvider);
	}

	[TestMethod]
	public void NavigationStack_InitialState_IsEmpty()
	{
		// Arrange & Act
		// Setup already creates the navigation stack

		// Assert
		Assert.IsNull(_navigationStack!.Current);
		Assert.IsFalse(_navigationStack.CanGoBack);
		Assert.IsFalse(_navigationStack.CanGoForward);
		Assert.AreEqual(0, _navigationStack.Count);
	}

	[TestMethod]
	public void NavigateTo_SingleItem_SetsCurrent()
	{
		// Arrange
		var item = new NavigationItem("1", "First Item");

		// Act
		_navigationStack!.NavigateTo(item);

		// Assert
		Assert.AreEqual(item, _navigationStack.Current);
		Assert.IsFalse(_navigationStack.CanGoBack);
		Assert.IsFalse(_navigationStack.CanGoForward);
		Assert.AreEqual(1, _navigationStack.Count);
	}

	[TestMethod]
	public void NavigateTo_MultipleItems_AllowsBackNavigation()
	{
		// Arrange
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");

		// Act
		_navigationStack!.NavigateTo(item1);
		_navigationStack.NavigateTo(item2);

		// Assert
		Assert.AreEqual(item2, _navigationStack.Current);
		Assert.IsTrue(_navigationStack.CanGoBack);
		Assert.IsFalse(_navigationStack.CanGoForward);
		Assert.AreEqual(2, _navigationStack.Count);
	}

	[TestMethod]
	public void GoBack_WithHistory_NavigatesToPrevious()
	{
		// Arrange
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");
		_navigationStack!.NavigateTo(item1);
		_navigationStack.NavigateTo(item2);

		// Act
		var result = _navigationStack.GoBack();

		// Assert
		Assert.AreEqual(item1, result);
		Assert.AreEqual(item1, _navigationStack.Current);
		Assert.IsFalse(_navigationStack.CanGoBack);
		Assert.IsTrue(_navigationStack.CanGoForward);
	}

	[TestMethod]
	public void GoForward_WithForwardHistory_NavigatesToNext()
	{
		// Arrange
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");
		_navigationStack!.NavigateTo(item1);
		_navigationStack.NavigateTo(item2);
		_navigationStack.GoBack();

		// Act
		var result = _navigationStack.GoForward();

		// Assert
		Assert.AreEqual(item2, result);
		Assert.AreEqual(item2, _navigationStack.Current);
		Assert.IsTrue(_navigationStack.CanGoBack);
		Assert.IsFalse(_navigationStack.CanGoForward);
	}

	[TestMethod]
	public void NavigateTo_ClearsForwardHistory()
	{
		// Arrange
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");
		var item3 = new NavigationItem("3", "Third Item");
		_navigationStack!.NavigateTo(item1);
		_navigationStack.NavigateTo(item2);
		_navigationStack.GoBack();

		// Act
		_navigationStack.NavigateTo(item3);

		// Assert
		Assert.AreEqual(item3, _navigationStack.Current);
		Assert.IsTrue(_navigationStack.CanGoBack);
		Assert.IsFalse(_navigationStack.CanGoForward);
		Assert.AreEqual(2, _navigationStack.Count);
	}

	[TestMethod]
	public void Clear_RemovesAllItems()
	{
		// Arrange
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");
		_navigationStack!.NavigateTo(item1);
		_navigationStack.NavigateTo(item2);

		// Act
		_navigationStack.Clear();

		// Assert
		Assert.IsNull(_navigationStack.Current);
		Assert.IsFalse(_navigationStack.CanGoBack);
		Assert.IsFalse(_navigationStack.CanGoForward);
		Assert.AreEqual(0, _navigationStack.Count);
	}

	[TestMethod]
	public void NavigationChanged_RaisedOnNavigation()
	{
		// Arrange
		var eventRaised = false;
		NavigationEventArgs<NavigationItem>? eventArgs = null;
		_navigationStack!.NavigationChanged += (sender, e) =>
		{
			eventRaised = true;
			eventArgs = e;
		};
		var item = new NavigationItem("1", "First Item");

		// Act
		_navigationStack.NavigateTo(item);

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
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");
		_navigationStack!.NavigateTo(item1);

		// Act
		_navigationStack.NavigateTo(item2);
		_undoRedoProvider!.Undo();

		// Assert
		Assert.AreEqual(item1, _navigationStack.Current);
		Assert.IsTrue(_undoRedoProvider.CanRedo);

		// Act - Redo
		_undoRedoProvider.Redo();

		// Assert
		Assert.AreEqual(item2, _navigationStack.Current);
	}

	[TestMethod]
	public void GetBackStack_ReturnsCorrectItems()
	{
		// Arrange
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");
		var item3 = new NavigationItem("3", "Third Item");
		_navigationStack!.NavigateTo(item1);
		_navigationStack.NavigateTo(item2);
		_navigationStack.NavigateTo(item3);

		// Act
		var backStack = _navigationStack.GetBackStack();

		// Assert
		Assert.AreEqual(2, backStack.Count);
		Assert.AreEqual(item1, backStack[0]);
		Assert.AreEqual(item2, backStack[1]);
	}

	[TestMethod]
	public void GetForwardStack_ReturnsCorrectItems()
	{
		// Arrange
		var item1 = new NavigationItem("1", "First Item");
		var item2 = new NavigationItem("2", "Second Item");
		var item3 = new NavigationItem("3", "Third Item");
		_navigationStack!.NavigateTo(item1);
		_navigationStack.NavigateTo(item2);
		_navigationStack.NavigateTo(item3);
		_navigationStack.GoBack();
		_navigationStack.GoBack();

		// Act
		var forwardStack = _navigationStack.GetForwardStack();

		// Assert
		Assert.AreEqual(2, forwardStack.Count);
		Assert.AreEqual(item2, forwardStack[0]);
		Assert.AreEqual(item3, forwardStack[1]);
	}
}
