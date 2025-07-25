// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.Navigation.Core.Contracts;
using ktsu.Navigation.Core.Models;

/// <summary>
/// A navigation stack implementation that supports undo/redo and persistence
/// </summary>
/// <typeparam name="T">The type of navigation items in the stack</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="Navigation{T}"/> class
/// </remarks>
/// <param name="undoRedoProvider">Optional undo/redo provider</param>
/// <param name="persistenceProvider">Optional persistence provider</param>
public class Navigation<T>(IUndoRedoProvider? undoRedoProvider = null, IPersistenceProvider<T>? persistenceProvider = null) : INavigation<T> where T : INavigationItem
{
	private readonly List<T> _items = [];
	private int _currentIndex = -1;

	/// <inheritdoc />
	public T? Current => _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : default;

	/// <inheritdoc />
	public bool CanGoBack => _currentIndex > 0;

	/// <inheritdoc />
	public bool CanGoForward => _currentIndex < _items.Count - 1;

	/// <inheritdoc />
	public int Count => _items.Count;

	/// <inheritdoc />
	public event EventHandler<NavigationEventArgs<T>>? NavigationChanged;

	/// <inheritdoc />
	public void NavigateTo(T item)
	{
		ArgumentNullException.ThrowIfNull(item);

		T? previousItem = Current;

		// Capture state before navigation for undo
		int beforeIndex = _currentIndex;
		List<T> beforeItems = [.. _items];

		// Remove any forward history when navigating to a new item
		if (_currentIndex < _items.Count - 1)
		{
			_items.RemoveRange(_currentIndex + 1, _items.Count - _currentIndex - 1);
		}

		_items.Add(item);
		_currentIndex = _items.Count - 1;

		// Create undoable action if undo/redo provider is available
		if (undoRedoProvider != null)
		{
			NavigateToAction<T> action = new(this, beforeIndex, beforeItems, _currentIndex, [.. _items]);
			undoRedoProvider.RegisterAction(action, $"Navigate to {item.DisplayName}");
		}

		OnNavigationChanged(NavigationType.NavigateTo, previousItem, Current);
	}

	/// <inheritdoc />
	public T? GoBack()
	{
		if (!CanGoBack)
		{
			return default;
		}

		T? previousItem = Current;
		_currentIndex--;

		OnNavigationChanged(NavigationType.GoBack, previousItem, Current);
		return Current;
	}

	/// <inheritdoc />
	public T? GoForward()
	{
		if (!CanGoForward)
		{
			return default;
		}

		T? previousItem = Current;
		_currentIndex++;

		OnNavigationChanged(NavigationType.GoForward, previousItem, Current);
		return Current;
	}

	/// <inheritdoc />
	public void Clear()
	{
		T? previousItem = Current;
		_items.Clear();
		_currentIndex = -1;

		OnNavigationChanged(NavigationType.Clear, previousItem, default);
	}

	/// <inheritdoc />
	public IReadOnlyList<T> GetHistory() => _items.AsReadOnly();

	/// <inheritdoc />
	public IReadOnlyList<T> GetBackStack() => _items.Take(_currentIndex).ToList().AsReadOnly();

	/// <inheritdoc />
	public IReadOnlyList<T> GetForwardStack() =>
		_currentIndex < _items.Count - 1
			? _items.Skip(_currentIndex + 1).ToList().AsReadOnly()
			: new List<T>().AsReadOnly();

	/// <summary>
	/// Saves the current navigation state using the persistence provider
	/// </summary>
	/// <param name="cancellationToken">A cancellation token</param>
	/// <returns>A task representing the asynchronous save operation</returns>
	public async Task SaveStateAsync(CancellationToken cancellationToken = default)
	{
		if (persistenceProvider == null)
		{
			return;
		}

		NavigationState<T> state = NavigationState<T>.FromNavigationStack(this);
		await persistenceProvider.SaveStateAsync(state, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Loads navigation state using the persistence provider
	/// </summary>
	/// <param name="cancellationToken">A cancellation token</param>
	/// <returns>A task representing the asynchronous load operation</returns>
	public async Task<bool> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (persistenceProvider == null)
		{
			return false;
		}

		INavigationState<T>? state = await persistenceProvider.LoadStateAsync(cancellationToken).ConfigureAwait(false);
		if (state == null)
		{
			return false;
		}

		Clear();
		_items.AddRange(state.Items);
		_currentIndex = state.CurrentIndex;

		OnNavigationChanged(NavigationType.NavigateTo, default, Current);
		return true;
	}

	/// <summary>
	/// Restores the navigation stack to a specific state (used by undo/redo operations)
	/// </summary>
	/// <param name="items">The items to restore</param>
	/// <param name="currentIndex">The current index to restore</param>
	internal void RestoreState(IEnumerable<T> items, int currentIndex)
	{
		T? previousItem = Current;

		_items.Clear();
		_items.AddRange(items);
		_currentIndex = currentIndex;

		OnNavigationChanged(NavigationType.NavigateTo, previousItem, Current);
	}

	/// <summary>
	/// Raises the navigation changed event
	/// </summary>
	/// <param name="navigationType">The type of navigation</param>
	/// <param name="previousItem">The previous item</param>
	/// <param name="currentItem">The current item</param>
	protected virtual void OnNavigationChanged(NavigationType navigationType, T? previousItem, T? currentItem) => NavigationChanged?.Invoke(this, new NavigationEventArgs<T>(navigationType, previousItem, currentItem));
}
