// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Services;

using System;
using System.Collections.Generic;
using ktsu.Navigation.Core.Contracts;

/// <summary>
/// An undoable action for navigation operations
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="NavigateToAction{T}"/> class
/// </remarks>
/// <param name="navigation">The navigation instance</param>
/// <param name="beforeIndex">The current index before navigation</param>
/// <param name="beforeItems">The items before navigation</param>
/// <param name="afterIndex">The current index after navigation</param>
/// <param name="afterItems">The items after navigation</param>
internal class NavigateToAction<T>(Navigation<T> navigation, int beforeIndex, List<T> beforeItems, int afterIndex, List<T> afterItems) : IUndoableAction where T : INavigationItem
{
	private readonly Navigation<T> _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
	private readonly List<T> _beforeItems = [.. beforeItems];
	private readonly List<T> _afterItems = [.. afterItems];

	/// <inheritdoc />
	public string Description => afterItems.Count > 0 ? $"Navigate to {afterItems[afterIndex].DisplayName}" : "Navigate";

	/// <inheritdoc />
	public void Execute() => _navigation.RestoreState(_afterItems, afterIndex);

	/// <inheritdoc />
	public void Undo() => _navigation.RestoreState(_beforeItems, beforeIndex);
}
