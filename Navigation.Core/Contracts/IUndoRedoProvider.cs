// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using System;

namespace ktsu.Navigation.Core.Contracts;

/// <summary>
/// Defines the contract for external undo/redo providers
/// </summary>
public interface IUndoRedoProvider
{
	/// <summary>
	/// Gets a value indicating whether undo operations are available
	/// </summary>
	bool CanUndo { get; }

	/// <summary>
	/// Gets a value indicating whether redo operations are available
	/// </summary>
	bool CanRedo { get; }

	/// <summary>
	/// Registers an undoable action with the provider
	/// </summary>
	/// <param name="action">The action that can be undone</param>
	/// <param name="description">A description of the action</param>
	void RegisterAction(IUndoableAction action, string description);

	/// <summary>
	/// Performs an undo operation
	/// </summary>
	/// <returns>True if undo was successful; otherwise, false</returns>
	bool Undo();

	/// <summary>
	/// Performs a redo operation
	/// </summary>
	/// <returns>True if redo was successful; otherwise, false</returns>
	bool Redo();

	/// <summary>
	/// Clears all undo/redo history
	/// </summary>
	void Clear();

	/// <summary>
	/// Event raised when the undo/redo state changes
	/// </summary>
	event EventHandler? StateChanged;
}

/// <summary>
/// Defines the contract for undoable actions
/// </summary>
public interface IUndoableAction
{
	/// <summary>
	/// Gets the description of this action
	/// </summary>
	string Description { get; }

	/// <summary>
	/// Executes the action
	/// </summary>
	void Execute();

	/// <summary>
	/// Undoes the action
	/// </summary>
	void Undo();
}
