// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using ktsu.Navigation.Core.Contracts;

namespace ktsu.Navigation.Core.Services;

/// <summary>
/// An in-memory persistence provider for testing and simple scenarios
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
public class InMemoryPersistenceProvider<T> : IPersistenceProvider<T> where T : INavigationItem
{
	private INavigationState<T>? _savedState;

	/// <inheritdoc />
	public Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default)
	{
		_savedState = state;
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<INavigationState<T>?> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(_savedState);
	}

	/// <inheritdoc />
	public Task<bool> HasSavedStateAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(_savedState != null);
	}

	/// <inheritdoc />
	public Task ClearSavedStateAsync(CancellationToken cancellationToken = default)
	{
		_savedState = null;
		return Task.CompletedTask;
	}
}
