// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines the contract for persistence providers that can save and restore navigation state
/// </summary>
/// <typeparam name="T">The type of navigation items to persist</typeparam>
public interface IPersistenceProvider<T> where T : class, INavigationItem
{
	/// <summary>
	/// Saves the navigation state to persistent storage
	/// </summary>
	/// <param name="state">The navigation state to save</param>
	/// <param name="cancellationToken">A cancellation token</param>
	/// <returns>A task representing the asynchronous save operation</returns>
	public Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default);

	/// <summary>
	/// Loads the navigation state from persistent storage
	/// </summary>
	/// <param name="cancellationToken">A cancellation token</param>
	/// <returns>A task representing the asynchronous load operation, containing the navigation state or null if none exists</returns>
	public Task<INavigationState<T>?> LoadStateAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if saved state exists
	/// </summary>
	/// <param name="cancellationToken">A cancellation token</param>
	/// <returns>A task representing the asynchronous check operation, returning true if state exists</returns>
	public Task<bool> HasSavedStateAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Clears any saved state
	/// </summary>
	/// <param name="cancellationToken">A cancellation token</param>
	/// <returns>A task representing the asynchronous clear operation</returns>
	public Task ClearSavedStateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the state of a navigation stack that can be persisted
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
public interface INavigationState<T> where T : class, INavigationItem
{
	/// <summary>
	/// Gets the items in the navigation stack
	/// </summary>
	public IReadOnlyList<T> Items { get; }

	/// <summary>
	/// Gets the index of the current item
	/// </summary>
	public int CurrentIndex { get; }

	/// <summary>
	/// Gets the timestamp when this state was created
	/// </summary>
	public DateTime CreatedAt { get; }
}
