// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.Navigation.Core.Contracts;

/// <summary>
/// A simple implementation of an undo/redo provider
/// </summary>
public class SimpleUndoRedoProvider : IUndoRedoProvider
{
	private readonly List<IUndoableAction> _undoStack;
	private readonly List<IUndoableAction> _redoStack;
	private readonly int _maxHistorySize;

	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleUndoRedoProvider"/> class
	/// </summary>
	/// <param name="maxHistorySize">The maximum number of actions to keep in history</param>
	public SimpleUndoRedoProvider(int maxHistorySize = 100)
	{
		if (maxHistorySize <= 0)
		{
			throw new ArgumentException("Max history size must be greater than zero.", nameof(maxHistorySize));
		}

		_maxHistorySize = maxHistorySize;
		_undoStack = [];
		_redoStack = [];
	}

	/// <inheritdoc />
	public bool CanUndo => _undoStack.Count > 0;

	/// <inheritdoc />
	public bool CanRedo => _redoStack.Count > 0;

	/// <inheritdoc />
	public event EventHandler? StateChanged;

	/// <inheritdoc />
	public void RegisterAction(IUndoableAction action, string description)
	{
		Ensure.NotNull(action);

		// Add to undo stack
		_undoStack.Add(action);

		// Clear redo stack when new action is registered
		_redoStack.Clear();

		// Trim undo stack if it exceeds max size
		if (_undoStack.Count > _maxHistorySize)
		{
			_undoStack.RemoveAt(0);
		}

		OnStateChanged();
	}

	/// <inheritdoc />
	public bool Undo()
	{
		if (!CanUndo)
		{
			return false;
		}

		IUndoableAction action = _undoStack.Last();
		_undoStack.RemoveAt(_undoStack.Count - 1);

		try
		{
			action.Undo();
			_redoStack.Add(action);
			OnStateChanged();
			return true;
		}
		catch
		{
			// If undo fails, put the action back on the undo stack
			_undoStack.Add(action);
			throw;
		}
	}

	/// <inheritdoc />
	public bool Redo()
	{
		if (!CanRedo)
		{
			return false;
		}

		IUndoableAction action = _redoStack.Last();
		_redoStack.RemoveAt(_redoStack.Count - 1);

		try
		{
			action.Execute();
			_undoStack.Add(action);
			OnStateChanged();
			return true;
		}
		catch
		{
			// If redo fails, put the action back on the redo stack
			_redoStack.Add(action);
			throw;
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		_undoStack.Clear();
		_redoStack.Clear();
		OnStateChanged();
	}

	/// <summary>
	/// Gets the current undo stack for debugging or inspection
	/// </summary>
	/// <returns>A read-only list of undoable actions</returns>
	public IReadOnlyList<IUndoableAction> GetUndoStack() => _undoStack.AsReadOnly();

	/// <summary>
	/// Gets the current redo stack for debugging or inspection
	/// </summary>
	/// <returns>A read-only list of undoable actions</returns>
	public IReadOnlyList<IUndoableAction> GetRedoStack() => _redoStack.AsReadOnly();

	/// <summary>
	/// Raises the state changed event
	/// </summary>
	protected virtual void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
