// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Services;

using System.Collections.Generic;
using ktsu.Navigation.Contracts;

/// <summary>
/// An undoable action for a change to a navigation stack, recorded as the stack before and after it
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
/// <param name="description">A description of the change</param>
internal sealed class NavigateToAction<T>(Navigation<T> navigation, int beforeIndex, List<T> beforeItems, int afterIndex, List<T> afterItems, string description) : IUndoableAction where T : class, INavigationItem
{
	private readonly Navigation<T> _navigation = Ensure.NotNull(navigation);
	private readonly List<T> _beforeItems = [.. beforeItems];
	private readonly List<T> _afterItems = [.. afterItems];

	/// <inheritdoc />
	public string Description { get; } = description;

	/// <inheritdoc />
	public void Execute() => _navigation.RestoreState(_afterItems, afterIndex);

	/// <inheritdoc />
	public void Undo() => _navigation.RestoreState(_beforeItems, beforeIndex);
}
