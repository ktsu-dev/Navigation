// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Contracts;

using System;

/// <summary>
/// Provides data for navigation events
/// </summary>
/// <typeparam name="T">The type of navigation item</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="NavigationEventArgs{T}"/> class
/// </remarks>
/// <param name="navigationType">The type of navigation that occurred</param>
/// <param name="previousItem">The previous navigation item</param>
/// <param name="currentItem">The current navigation item</param>
public class NavigationEventArgs<T>(NavigationType navigationType, T? previousItem, T? currentItem) : EventArgs where T : class, INavigationItem
{

	/// <summary>
	/// Gets the type of navigation that occurred
	/// </summary>
	public NavigationType NavigationType { get; } = navigationType;

	/// <summary>
	/// Gets the previous navigation item
	/// </summary>
	public T? PreviousItem { get; } = previousItem;

	/// <summary>
	/// Gets the current navigation item
	/// </summary>
	public T? CurrentItem { get; } = currentItem;
}

/// <summary>
/// Defines the types of navigation operations
/// </summary>
public enum NavigationType
{
	/// <summary>
	/// Navigation to a new item
	/// </summary>
	NavigateTo,

	/// <summary>
	/// Navigation backward in the stack
	/// </summary>
	GoBack,

	/// <summary>
	/// Navigation forward in the stack
	/// </summary>
	GoForward,

	/// <summary>
	/// The navigation stack was cleared
	/// </summary>
	Clear
}
