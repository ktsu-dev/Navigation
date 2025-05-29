// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.Navigation.Core.Contracts;

namespace ktsu.Navigation.Core.Services;

/// <summary>
/// An undoable action for navigation operations
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
internal class NavigateToAction<T> : IUndoableAction where T : INavigationItem
{
	private readonly NavigationStack<T> _navigationStack;
	private readonly T _newItem;
	private readonly int _previousIndex;
	private readonly List<T> _previousItems;

	/// <summary>
	/// Initializes a new instance of the <see cref="NavigateToAction{T}"/> class
	/// </summary>
	/// <param name="navigationStack">The navigation stack</param>
	/// <param name="newItem">The new item to navigate to</param>
	/// <param name="previousIndex">The previous current index</param>
	/// <param name="previousItems">The previous items in the stack</param>
	public NavigateToAction(NavigationStack<T> navigationStack, T newItem, int previousIndex, List<T> previousItems)
	{
		_navigationStack = navigationStack ?? throw new ArgumentNullException(nameof(navigationStack));
		_newItem = newItem ?? throw new ArgumentNullException(nameof(newItem));
		_previousIndex = previousIndex;
		_previousItems = previousItems?.ToList() ?? throw new ArgumentNullException(nameof(previousItems));
	}

	/// <inheritdoc />
	public string Description => $"Navigate to {_newItem.DisplayName}";

	/// <inheritdoc />
	public void Execute()
	{
		// The action is already executed when it's created, so this is a no-op
		// or could be used to re-execute the navigation if needed
	}

	/// <inheritdoc />
	public void Undo()
	{
		_navigationStack.RestoreState(_previousItems, _previousIndex);
	}
}
