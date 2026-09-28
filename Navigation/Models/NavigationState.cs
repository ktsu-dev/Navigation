// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ktsu.Navigation.Contracts;

/// <summary>
/// Represents the state of a navigation stack that can be persisted
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
public class NavigationState<T> : INavigationState<T> where T : class, INavigationItem
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NavigationState{T}"/> class
	/// </summary>
	/// <param name="items">The items in the navigation stack</param>
	/// <param name="currentIndex">The index of the current item</param>
	/// <exception cref="ArgumentException">Thrown when any of the items is null</exception>
	public NavigationState(IEnumerable<T> items, int currentIndex)
	{
		Ensure.NotNull(items);

		List<T> itemList = [.. items];

		if (itemList.Exists(item => item is null))
		{
			throw new ArgumentException("Navigation state items cannot be null.", nameof(items));
		}

		if (currentIndex < -1 || currentIndex >= itemList.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(currentIndex),
				"Current index must be between -1 and the number of items minus 1.");
		}

		Items = itemList.AsReadOnly();
		CurrentIndex = currentIndex;
		CreatedAt = DateTime.UtcNow;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NavigationState{T}"/> class when deserializing.
	/// System.Text.Json requires each constructor parameter to have the same type as the property it binds to.
	/// </summary>
	/// <param name="items">The items in the navigation stack</param>
	/// <param name="currentIndex">The index of the current item</param>
	[JsonConstructor]
	public NavigationState(IReadOnlyList<T> items, int currentIndex)
		: this((IEnumerable<T>)items, currentIndex)
	{
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
		Ensure.NotNull(navigation);

		IReadOnlyList<T> history = navigation.GetHistory();
		// The back stack holds every item before the current one, so its size is the current index.
		// Searching the history for the current item instead would find an earlier visit to the same page.
		int currentIndex = navigation.Current != null ? navigation.GetBackStack().Count : -1;

		return new NavigationState<T>(history, currentIndex);
	}
}
