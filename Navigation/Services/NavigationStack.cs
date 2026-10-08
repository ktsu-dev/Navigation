// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.Navigation.Contracts;
using ktsu.Navigation.Models;

/// <summary>
/// A navigation stack implementation that supports undo/redo and persistence
/// </summary>
/// <typeparam name="T">The type of navigation items in the stack</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="Navigation{T}"/> class.
/// Every read and write of the history and current index happens under one private lock, so the stack can
/// be used from several threads. <see cref="NavigationChanged"/> is raised after the lock is released.
/// </remarks>
/// <param name="undoRedoProvider">Optional undo/redo provider</param>
/// <param name="persistenceProvider">Optional persistence provider</param>
public class Navigation<T>(IUndoRedoProvider? undoRedoProvider = null, IPersistenceProvider<T>? persistenceProvider = null) : INavigation<T> where T : class, INavigationItem
{
	private readonly List<T> _items = [];
	private int _currentIndex = -1;
	private readonly Lock _sync = new();

	/// <inheritdoc />
	public T? Current
	{
		get
		{
			lock (_sync)
			{
				return CurrentUnlocked;
			}
		}
	}

	/// <inheritdoc />
	public bool CanGoBack
	{
		get
		{
			lock (_sync)
			{
				return _currentIndex > 0;
			}
		}
	}

	/// <inheritdoc />
	public bool CanGoForward
	{
		get
		{
			lock (_sync)
			{
				return _currentIndex < _items.Count - 1;
			}
		}
	}

	/// <inheritdoc />
	public int Count
	{
		get
		{
			lock (_sync)
			{
				return _items.Count;
			}
		}
	}

	private T? CurrentUnlocked => _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : default;

	/// <inheritdoc />
	public event EventHandler<NavigationEventArgs<T>>? NavigationChanged;

	/// <inheritdoc />
	public void NavigateTo(T item)
	{
		Ensure.NotNull(item);

		T? previousItem;
		lock (_sync)
		{
			previousItem = CurrentUnlocked;
			int beforeIndex = _currentIndex;
			List<T>? beforeItems = SnapshotForUndo();

			// Remove any forward history when navigating to a new item
			if (_currentIndex < _items.Count - 1)
			{
				_items.RemoveRange(_currentIndex + 1, _items.Count - _currentIndex - 1);
			}

			_items.Add(item);
			_currentIndex = _items.Count - 1;

			RegisterUndo(beforeItems, beforeIndex, $"Navigate to {item.DisplayName}");
		}

		OnNavigationChanged(NavigationType.NavigateTo, previousItem, item);
	}

	/// <inheritdoc />
	public T? GoBack()
	{
		T? previousItem;
		T? currentItem;
		lock (_sync)
		{
			if (_currentIndex <= 0)
			{
				return default;
			}

			previousItem = CurrentUnlocked;
			int beforeIndex = _currentIndex;
			List<T>? beforeItems = SnapshotForUndo();
			_currentIndex--;
			currentItem = CurrentUnlocked;

			RegisterUndo(beforeItems, beforeIndex, $"Go back to {currentItem?.DisplayName}");
		}

		OnNavigationChanged(NavigationType.GoBack, previousItem, currentItem);
		return currentItem;
	}

	/// <inheritdoc />
	public T? GoForward()
	{
		T? previousItem;
		T? currentItem;
		lock (_sync)
		{
			if (_currentIndex >= _items.Count - 1)
			{
				return default;
			}

			previousItem = CurrentUnlocked;
			int beforeIndex = _currentIndex;
			List<T>? beforeItems = SnapshotForUndo();
			_currentIndex++;
			currentItem = CurrentUnlocked;

			RegisterUndo(beforeItems, beforeIndex, $"Go forward to {currentItem?.DisplayName}");
		}

		OnNavigationChanged(NavigationType.GoForward, previousItem, currentItem);
		return currentItem;
	}

	/// <inheritdoc />
	public void Clear()
	{
		T? previousItem;
		lock (_sync)
		{
			previousItem = CurrentUnlocked;
			int beforeIndex = _currentIndex;
			List<T>? beforeItems = _items.Count > 0 ? SnapshotForUndo() : null;
			_items.Clear();
			_currentIndex = -1;

			RegisterUndo(beforeItems, beforeIndex, "Clear navigation history");
		}

		OnNavigationChanged(NavigationType.Clear, previousItem, default);
	}

	/// <inheritdoc />
	/// <remarks>Returns a copy, so a navigation on another thread cannot change it while it is enumerated.</remarks>
	public IReadOnlyList<T> GetHistory()
	{
		lock (_sync)
		{
			return _items.ToList().AsReadOnly();
		}
	}

	/// <inheritdoc />
	public IReadOnlyList<T> GetBackStack()
	{
		lock (_sync)
		{
			return _items.Take(_currentIndex).ToList().AsReadOnly();
		}
	}

	/// <inheritdoc />
	public IReadOnlyList<T> GetForwardStack()
	{
		lock (_sync)
		{
			return _currentIndex < _items.Count - 1
				? _items.Skip(_currentIndex + 1).ToList().AsReadOnly()
				: new List<T>().AsReadOnly();
		}
	}

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

		// Read the history and current index in one step, so a navigation on another thread cannot land between them
		NavigationState<T> state;
		lock (_sync)
		{
			List<T> items = [.. _items];
			state = new NavigationState<T>(items, _currentIndex);
		}

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

		T? previousItem;
		T? currentItem;
		lock (_sync)
		{
			previousItem = CurrentUnlocked;
			int beforeIndex = _currentIndex;
			List<T>? beforeItems = SnapshotForUndo();

			// Replace the stack directly rather than through the public Clear(), so a load raises a single
			// NavigateTo event from the page that was current before it, not a Clear followed by a NavigateTo.
			ReplaceItems(state.Items, state.CurrentIndex);
			currentItem = CurrentUnlocked;

			RegisterUndo(beforeItems, beforeIndex, "Load navigation state");
		}

		OnNavigationChanged(NavigationType.NavigateTo, previousItem, currentItem);
		return true;
	}

	/// <summary>
	/// Restores the navigation stack to a specific state (used by undo/redo operations)
	/// </summary>
	/// <param name="items">The items to restore</param>
	/// <param name="currentIndex">The current index to restore</param>
	internal void RestoreState(IEnumerable<T> items, int currentIndex)
	{
		T? previousItem;
		T? currentItem;
		lock (_sync)
		{
			previousItem = CurrentUnlocked;
			ReplaceItems(items, currentIndex);
			currentItem = CurrentUnlocked;
		}

		OnNavigationChanged(NavigationType.NavigateTo, previousItem, currentItem);
	}

	/// <summary>
	/// Replaces the items and current index; the caller holds the lock
	/// </summary>
	/// <param name="items">The items to restore</param>
	/// <param name="currentIndex">The current index to restore</param>
	private void ReplaceItems(IEnumerable<T> items, int currentIndex)
	{
		_items.Clear();
		_items.AddRange(items);
		_currentIndex = currentIndex;
	}

	/// <summary>
	/// Copies the stack's items so the change about to be made can be undone, or returns null when
	/// there is no undo/redo provider to record it with
	/// </summary>
	/// <returns>A copy of the items, or null</returns>
	private List<T>? SnapshotForUndo() => undoRedoProvider != null ? [.. _items] : null;

	/// <summary>
	/// Records the change just made as one undoable action, so an undo restores exactly the stack the
	/// change started from instead of an older snapshot
	/// </summary>
	/// <param name="beforeItems">The items before the change, from <see cref="SnapshotForUndo"/>; null records nothing</param>
	/// <param name="beforeIndex">The current index before the change</param>
	/// <param name="description">A description of the change</param>
	/// <remarks>
	/// Called under the lock, so actions reach the provider in the order the changes were made
	/// </remarks>
	private void RegisterUndo(List<T>? beforeItems, int beforeIndex, string description)
	{
		if (undoRedoProvider == null || beforeItems == null)
		{
			return;
		}

		NavigateToAction<T> action = new(this, beforeIndex, beforeItems, _currentIndex, [.. _items], description);
		undoRedoProvider.RegisterAction(action, description);
	}

	/// <summary>
	/// Raises the navigation changed event
	/// </summary>
	/// <param name="navigationType">The type of navigation</param>
	/// <param name="previousItem">The previous item</param>
	/// <param name="currentItem">The current item</param>
	protected virtual void OnNavigationChanged(NavigationType navigationType, T? previousItem, T? currentItem) => NavigationChanged?.Invoke(this, new NavigationEventArgs<T>(navigationType, previousItem, currentItem));
}
