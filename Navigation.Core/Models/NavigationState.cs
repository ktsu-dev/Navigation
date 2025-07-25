// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.Navigation.Core.Contracts;

/// <summary>
/// Represents the state of a navigation stack that can be persisted
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
public class NavigationState<T> : INavigationState<T> where T : INavigationItem
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NavigationState{T}"/> class
	/// </summary>
	/// <param name="items">The items in the navigation stack</param>
	/// <param name="currentIndex">The index of the current item</param>
	public NavigationState(IEnumerable<T> items, int currentIndex)
	{
		ArgumentNullException.ThrowIfNull(items);

		List<T> itemList = [.. items];

		if (currentIndex < -1 || currentIndex >= itemList.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(currentIndex),
				"Current index must be between -1 and the number of items minus 1.");
		}

		Items = itemList.AsReadOnly();
		CurrentIndex = currentIndex;
		CreatedAt = DateTime.UtcNow;
	}

	/// <inheritdoc />
	public IReadOnlyList<T> Items { get; }

	/// <inheritdoc />
	public int CurrentIndex { get; }

	/// <inheritdoc />
	public DateTime CreatedAt { get; }

	/// <summary>
	/// Gets the current item in the navigation state
	/// </summary>
	public T? Current => CurrentIndex >= 0 && CurrentIndex < Items.Count ? Items[CurrentIndex] : default;

	/// <summary>
	/// Creates a new navigation state from a navigation stack
	/// </summary>
	/// <param name="navigation">The navigation to create state from</param>
	/// <returns>A new navigation state instance</returns>
	public static NavigationState<T> FromNavigationStack(INavigation<T> navigation)
	{
		ArgumentNullException.ThrowIfNull(navigation);

		IReadOnlyList<T> history = navigation.GetHistory();
		T? current = navigation.Current;
		int currentIndex = current != null ? history.ToList().FindIndex(item => item.Id == current.Id) : -1;

		return new NavigationState<T>(history, currentIndex);
	}
}
