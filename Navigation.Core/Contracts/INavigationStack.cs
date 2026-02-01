// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Contracts;

using System;
using System.Collections.Generic;

/// <summary>
/// Defines the contract for a navigation stack that supports undo/redo operations and persistence
/// </summary>
/// <typeparam name="T">The type of navigation items in the stack</typeparam>
public interface INavigation<T> where T : class, INavigationItem
{
	/// <summary>
	/// Gets the current item in the navigation stack
	/// </summary>
	public T? Current { get; }

	/// <summary>
	/// Gets a value indicating whether there are items to navigate back to
	/// </summary>
	public bool CanGoBack { get; }

	/// <summary>
	/// Gets a value indicating whether there are items to navigate forward to
	/// </summary>
	public bool CanGoForward { get; }

	/// <summary>
	/// Gets the number of items in the stack
	/// </summary>
	public int Count { get; }

	/// <summary>
	/// Navigates to a new item, adding it to the stack
	/// </summary>
	/// <param name="item">The item to navigate to</param>
	public void NavigateTo(T item);

	/// <summary>
	/// Navigates back to the previous item in the stack
	/// </summary>
	/// <returns>The item navigated to, or null if navigation was not possible</returns>
	public T? GoBack();

	/// <summary>
	/// Navigates forward to the next item in the stack
	/// </summary>
	/// <returns>The item navigated to, or null if navigation was not possible</returns>
	public T? GoForward();

	/// <summary>
	/// Clears all items from the navigation stack
	/// </summary>
	public void Clear();

	/// <summary>
	/// Gets a read-only view of the navigation history
	/// </summary>
	/// <returns>A read-only collection of navigation items</returns>
	public IReadOnlyList<T> GetHistory();

	/// <summary>
	/// Gets the back stack (items before current)
	/// </summary>
	/// <returns>A read-only collection of items that can be navigated back to</returns>
	public IReadOnlyList<T> GetBackStack();

	/// <summary>
	/// Gets the forward stack (items after current)
	/// </summary>
	/// <returns>A read-only collection of items that can be navigated forward to</returns>
	public IReadOnlyList<T> GetForwardStack();

	/// <summary>
	/// Event raised when navigation occurs
	/// </summary>
	public event EventHandler<NavigationEventArgs<T>>? NavigationChanged;
}
