// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Contracts;

using System;

/// <summary>
/// Defines the contract for external undo/redo providers
/// </summary>
public interface IUndoRedoProvider
{
	/// <summary>
	/// Gets a value indicating whether undo operations are available
	/// </summary>
	public bool CanUndo { get; }

	/// <summary>
	/// Gets a value indicating whether redo operations are available
	/// </summary>
	public bool CanRedo { get; }

	/// <summary>
	/// Registers an undoable action with the provider
	/// </summary>
	/// <param name="action">The action that can be undone</param>
	/// <param name="description">A description of the action</param>
	public void RegisterAction(IUndoableAction action, string description);

	/// <summary>
	/// Performs an undo operation
	/// </summary>
	/// <returns>True if undo was successful; otherwise, false</returns>
	public bool Undo();

	/// <summary>
	/// Performs a redo operation
	/// </summary>
	/// <returns>True if redo was successful; otherwise, false</returns>
	public bool Redo();

	/// <summary>
	/// Clears all undo/redo history
	/// </summary>
	public void Clear();

	/// <summary>
	/// Event raised when the undo/redo state changes
	/// </summary>
	public event EventHandler? StateChanged;
}

/// <summary>
/// Defines the contract for undoable actions
/// </summary>
public interface IUndoableAction
{
	/// <summary>
	/// Gets the description of this action
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// Executes the action
	/// </summary>
	public void Execute();

	/// <summary>
	/// Undoes the action
	/// </summary>
	public void Undo();
}
