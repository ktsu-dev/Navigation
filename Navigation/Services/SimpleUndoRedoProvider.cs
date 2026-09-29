// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.Navigation.Contracts;
using ktsu.UndoRedo;
using ktsu.UndoRedo.Core.Services;
using ktsu.UndoRedo.Models;

/// <summary>
/// An undo/redo provider backed by <c>ktsu.UndoRedo</c>'s <see cref="UndoRedoService"/>.
/// </summary>
/// <remarks>
/// This is an adapter: <see cref="IUndoRedoProvider"/> stays the contract navigation stacks are
/// written against, and the history itself is kept by <see cref="UndoRedoService"/>.
/// <para>
/// An action that throws from <see cref="Undo"/> or <see cref="Redo"/> stays where it was, and the
/// exception propagates. <see cref="UndoRedoService"/> moves past a command before running it, so on
/// its own a failed undo would leave the action on the redo stack as though it had been undone. The
/// adapter moves it back without running it again.
/// </para>
/// </remarks>
public class SimpleUndoRedoProvider : IUndoRedoProvider
{
	private readonly UndoRedoService _service;

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

		// Navigation actions are never merged, and navigating to a change is this library's job rather
		// than the history's, so both are switched off.
		UndoRedoOptions options = new(MaxStackSize: maxHistorySize, AutoMergeCommands: false, EnableNavigation: false);
		_service = new UndoRedoService(new StackManager(), new SaveBoundaryManager(), new CommandMerger(), options);
	}

	/// <inheritdoc />
	public bool CanUndo => _service.CanUndo;

	/// <inheritdoc />
	public bool CanRedo => _service.CanRedo;

	/// <inheritdoc />
	public event EventHandler? StateChanged;

	/// <inheritdoc />
	public void RegisterAction(IUndoableAction action, string description)
	{
		Ensure.NotNull(action);

		_service.Execute(new RegisteredAction(action, description));
		OnStateChanged();
	}

	/// <inheritdoc />
	public bool Undo()
	{
		if (!_service.CanUndo)
		{
			return false;
		}

		RegisteredAction current = (RegisteredAction)_service.Commands[_service.CurrentPosition];

		try
		{
			_service.Undo();
		}
		catch
		{
			// The service has already moved past the action. Put it back without running it again.
			current.SkipNextExecute();
			_service.Redo();
			throw;
		}

		OnStateChanged();
		return true;
	}

	/// <inheritdoc />
	public bool Redo()
	{
		if (!_service.CanRedo)
		{
			return false;
		}

		RegisteredAction next = (RegisteredAction)_service.Commands[_service.CurrentPosition + 1];

		try
		{
			_service.Redo();
		}
		catch
		{
			// The service has already moved past the action. Put it back without undoing it.
			next.SkipNextUndo();
			_service.Undo();
			throw;
		}

		OnStateChanged();
		return true;
	}

	/// <inheritdoc />
	public void Clear()
	{
		_service.Clear();
		OnStateChanged();
	}

	/// <summary>
	/// Gets the current undo stack for debugging or inspection
	/// </summary>
	/// <returns>A read-only list of undoable actions, ending with the one the next undo would run</returns>
	public IReadOnlyList<IUndoableAction> GetUndoStack() =>
		_service.Commands
			.Take(_service.CurrentPosition + 1)
			.Select(Unwrap)
			.ToList()
			.AsReadOnly();

	/// <summary>
	/// Gets the current redo stack for debugging or inspection
	/// </summary>
	/// <returns>A read-only list of undoable actions, ending with the one the next redo would run</returns>
	public IReadOnlyList<IUndoableAction> GetRedoStack() =>
		_service.Commands
			.Skip(_service.CurrentPosition + 1)
			.Reverse()
			.Select(Unwrap)
			.ToList()
			.AsReadOnly();

	/// <summary>
	/// Raises the state changed event
	/// </summary>
	protected virtual void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

	private static IUndoableAction Unwrap(ICommand command) => ((RegisteredAction)command).Action;

	/// <summary>
	/// Presents an <see cref="IUndoableAction"/> to the history as a command.
	/// </summary>
	/// <remarks>
	/// <see cref="UndoRedoService.Execute(ICommand)"/> runs a command as it records it, but an action
	/// reaches <see cref="RegisterAction"/> after its caller has already done the work, so the first
	/// <see cref="Execute"/> is skipped. The same switch lets a failed undo or redo move the history's
	/// position back without running the action a second time.
	/// </remarks>
	/// <param name="action">The action being recorded.</param>
	/// <param name="description">The description it was registered with.</param>
	private sealed class RegisteredAction(IUndoableAction action, string description) : ICommand
	{
		private bool _skipNextExecute = true;
		private bool _skipNextUndo;

		public IUndoableAction Action { get; } = action;

		public string Description { get; } = description;

		public string? NavigationContext => null;

		public ChangeMetadata Metadata { get; } = new(ChangeType.Custom, [], DateTimeOffset.UtcNow);

		public void SkipNextExecute() => _skipNextExecute = true;

		public void SkipNextUndo() => _skipNextUndo = true;

		public void Execute()
		{
			if (_skipNextExecute)
			{
				_skipNextExecute = false;
				return;
			}

			Action.Execute();
		}

		public void Undo()
		{
			if (_skipNextUndo)
			{
				_skipNextUndo = false;
				return;
			}

			Action.Undo();
		}

		public bool CanMergeWith(ICommand other) => false;

		public ICommand MergeWith(ICommand other) =>
			throw new NotSupportedException("Navigation actions are never merged.");
	}
}
