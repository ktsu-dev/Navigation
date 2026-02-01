// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Services;

using System;
using ktsu.Navigation.Core.Contracts;

/// <summary>
/// A factory for creating navigation stacks with configured providers
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NavigationStackFactory"/> class
/// </remarks>
/// <param name="defaultUndoRedoProvider">Default undo/redo provider to use</param>
/// <param name="serviceProvider">Optional service provider for dependency injection</param>
public class NavigationStackFactory(IUndoRedoProvider? defaultUndoRedoProvider = null, Func<Type, object?>? serviceProvider = null)
{

	/// <summary>
	/// Creates a new navigation stack with the default providers
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <returns>A new navigation stack instance</returns>
	public INavigation<T> CreateNavigationStack<T>() where T : class, INavigationItem
	{
		IPersistenceProvider<T>? persistenceProvider = serviceProvider?.Invoke(typeof(IPersistenceProvider<T>)) as IPersistenceProvider<T>;
		IUndoRedoProvider? undoRedoProvider = serviceProvider?.Invoke(typeof(IUndoRedoProvider)) as IUndoRedoProvider ?? defaultUndoRedoProvider;

		return new Navigation<T>(undoRedoProvider, persistenceProvider);
	}

	/// <summary>
	/// Creates a new navigation stack with specific providers
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <param name="undoRedoProvider">The undo/redo provider to use</param>
	/// <param name="persistenceProvider">The persistence provider to use</param>
	/// <returns>A new navigation stack instance</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
	public INavigation<T> CreateNavigationStack<T>(IUndoRedoProvider? undoRedoProvider, IPersistenceProvider<T>? persistenceProvider) where T : class, INavigationItem => new Navigation<T>(undoRedoProvider, persistenceProvider);

	/// <summary>
	/// Creates a new navigation stack with only an undo/redo provider
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <param name="undoRedoProvider">The undo/redo provider to use</param>
	/// <returns>A new navigation stack instance</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
	public INavigation<T> CreateNavigationStack<T>(IUndoRedoProvider undoRedoProvider) where T : class, INavigationItem => new Navigation<T>(undoRedoProvider);

	/// <summary>
	/// Creates a new navigation stack with only a persistence provider
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <param name="persistenceProvider">The persistence provider to use</param>
	/// <returns>A new navigation stack instance</returns>
	public INavigation<T> CreateNavigationStack<T>(IPersistenceProvider<T> persistenceProvider) where T : class, INavigationItem => new Navigation<T>(defaultUndoRedoProvider, persistenceProvider);

	/// <summary>
	/// Creates a basic navigation stack without any providers
	/// </summary>
	/// <typeparam name="T">The type of navigation items</typeparam>
	/// <returns>A new navigation stack instance</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
	public INavigation<T> CreateBasicNavigationStack<T>() where T : class, INavigationItem => new Navigation<T>();
}
