// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Services;

using System;
using ktsu.Navigation.Core.Contracts;

/// <summary>
/// A factory for creating navigation stacks with configured providers
/// </summary>
public class NavigationStackFactory
{
	private readonly IUndoRedoProvider? _defaultUndoRedoProvider;
	private readonly Func<Type, object?>? _serviceProvider;

	/// <summary>
	/// Initializes a new instance of the <see cref="NavigationStackFactory"/> class
	/// </summary>
	/// <param name="defaultUndoRedoProvider">Default undo/redo provider to use</param>
	/// <param name="serviceProvider">Optional service provider for dependency injection</param>
	public NavigationStackFactory(IUndoRedoProvider? defaultUndoRedoProvider = null, Func<Type, object?>? serviceProvider = null)
	{
		_defaultUndoRedoProvider = defaultUndoRedoProvider;
		_serviceProvider = serviceProvider;
	}

	/// <summary>
	/// Creates a new navigation stack with the default providers
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <returns>A new navigation stack instance</returns>
	public INavigation<T> CreateNavigationStack<T>() where T : INavigationItem
	{
		var persistenceProvider = _serviceProvider?.Invoke(typeof(IPersistenceProvider<T>)) as IPersistenceProvider<T>;
		var undoRedoProvider = _serviceProvider?.Invoke(typeof(IUndoRedoProvider)) as IUndoRedoProvider ?? _defaultUndoRedoProvider;

		return new NavigationStack<T>(undoRedoProvider, persistenceProvider);
	}

	/// <summary>
	/// Creates a new navigation stack with specific providers
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <param name="undoRedoProvider">The undo/redo provider to use</param>
	/// <param name="persistenceProvider">The persistence provider to use</param>
	/// <returns>A new navigation stack instance</returns>
	public INavigation<T> CreateNavigationStack<T>(IUndoRedoProvider? undoRedoProvider, IPersistenceProvider<T>? persistenceProvider) where T : INavigationItem
	{
		return new NavigationStack<T>(undoRedoProvider, persistenceProvider);
	}

	/// <summary>
	/// Creates a new navigation stack with only an undo/redo provider
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <param name="undoRedoProvider">The undo/redo provider to use</param>
	/// <returns>A new navigation stack instance</returns>
	public INavigation<T> CreateNavigationStack<T>(IUndoRedoProvider undoRedoProvider) where T : INavigationItem
	{
		return new NavigationStack<T>(undoRedoProvider);
	}

	/// <summary>
	/// Creates a new navigation stack with only a persistence provider
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <param name="persistenceProvider">The persistence provider to use</param>
	/// <returns>A new navigation stack instance</returns>
	public INavigation<T> CreateNavigationStack<T>(IPersistenceProvider<T> persistenceProvider) where T : INavigationItem
	{
		return new NavigationStack<T>(_defaultUndoRedoProvider, persistenceProvider);
	}

	/// <summary>
	/// Creates a basic navigation stack without any providers
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <returns>A new navigation stack instance</returns>
	public INavigation<T> CreateBasicNavigationStack<T>() where T : INavigationItem
	{
		return new NavigationStack<T>();
	}
}
