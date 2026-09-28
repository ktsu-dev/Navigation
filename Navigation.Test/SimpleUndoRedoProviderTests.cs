// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Test;

using ktsu.Navigation.Contracts;
using ktsu.Navigation.Services;

[TestClass]
public class SimpleUndoRedoProviderTests
{
	/// <summary>
	/// An action that records what was done to it, and can be told to fail.
	/// </summary>
	private sealed class RecordingAction(string name, List<string> log) : IUndoableAction
	{
		public string Description => name;

		public bool FailUndo { get; set; }

		public bool FailExecute { get; set; }

		public void Execute()
		{
			if (FailExecute)
			{
				throw new InvalidOperationException($"execute {name} failed");
			}

			log.Add($"execute {name}");
		}

		public void Undo()
		{
			if (FailUndo)
			{
				throw new InvalidOperationException($"undo {name} failed");
			}

			log.Add($"undo {name}");
		}
	}

	private static string Describe(IEnumerable<IUndoableAction> actions) =>
		string.Join(",", actions.Select(action => action.Description));

	[TestMethod]
	public void Constructor_WithANonPositiveHistorySize_Throws()
	{
		Assert.ThrowsExactly<ArgumentException>(() => new SimpleUndoRedoProvider(0));
		Assert.ThrowsExactly<ArgumentException>(() => new SimpleUndoRedoProvider(-1));
	}

	[TestMethod]
	public void RegisterAction_DoesNotExecuteTheAction()
	{
		// The caller has already done the work by the time it registers it. Navigation<T>.NavigateTo
		// navigates first and registers afterwards, so running the action again here would redo a
		// navigation that has already happened.
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();

		provider.RegisterAction(new RecordingAction("a", log), "a");

		Assert.IsEmpty(log);
		Assert.IsTrue(provider.CanUndo);
		Assert.IsFalse(provider.CanRedo);
	}

	[TestMethod]
	public void UndoAndRedo_OnEmptyHistory_ReturnFalse()
	{
		SimpleUndoRedoProvider provider = new();

		Assert.IsFalse(provider.Undo());
		Assert.IsFalse(provider.Redo());
	}

	[TestMethod]
	public void UndoAndRedo_RunInStackOrder()
	{
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();
		provider.RegisterAction(new RecordingAction("a", log), "a");
		provider.RegisterAction(new RecordingAction("b", log), "b");

		Assert.IsTrue(provider.Undo());
		Assert.IsTrue(provider.Undo());
		Assert.IsFalse(provider.CanUndo);
		Assert.IsTrue(provider.Redo());
		Assert.IsTrue(provider.Redo());
		Assert.IsFalse(provider.CanRedo);

		Assert.AreEqual("undo b,undo a,execute a,execute b", string.Join(",", log));
	}

	[TestMethod]
	public void RegisterAction_AfterAnUndo_ClearsTheRedoHistory()
	{
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();
		provider.RegisterAction(new RecordingAction("a", log), "a");
		provider.RegisterAction(new RecordingAction("b", log), "b");
		provider.Undo();

		provider.RegisterAction(new RecordingAction("c", log), "c");

		Assert.IsFalse(provider.CanRedo);
		Assert.AreEqual("c,a", Describe(provider.GetUndoStack().Reverse()));
	}

	[TestMethod]
	public void RegisterAction_BeyondTheHistorySize_DropsTheOldest()
	{
		List<string> log = [];
		SimpleUndoRedoProvider provider = new(maxHistorySize: 2);

		provider.RegisterAction(new RecordingAction("a", log), "a");
		provider.RegisterAction(new RecordingAction("b", log), "b");
		provider.RegisterAction(new RecordingAction("c", log), "c");

		Assert.AreEqual("b,c", Describe(provider.GetUndoStack()));
		Assert.IsTrue(provider.Undo());
		Assert.IsTrue(provider.Undo());
		Assert.IsFalse(provider.Undo());
	}

	[TestMethod]
	public void GetStacks_ReportTheActionsInPushOrder()
	{
		// Each list ends with the action the next Undo or Redo would run.
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();
		provider.RegisterAction(new RecordingAction("a", log), "a");
		provider.RegisterAction(new RecordingAction("b", log), "b");
		provider.RegisterAction(new RecordingAction("c", log), "c");
		provider.Undo();
		provider.Undo();

		Assert.AreEqual("a", Describe(provider.GetUndoStack()));
		Assert.AreEqual("c,b", Describe(provider.GetRedoStack()));
	}

	[TestMethod]
	public void Clear_EmptiesBothStacks()
	{
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();
		provider.RegisterAction(new RecordingAction("a", log), "a");
		provider.RegisterAction(new RecordingAction("b", log), "b");
		provider.Undo();

		provider.Clear();

		Assert.IsFalse(provider.CanUndo);
		Assert.IsFalse(provider.CanRedo);
		Assert.IsEmpty(provider.GetUndoStack());
		Assert.IsEmpty(provider.GetRedoStack());
	}

	[TestMethod]
	public void StateChanged_IsRaisedByEveryChange()
	{
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();
		int raised = 0;
		provider.StateChanged += (_, _) => raised++;

		provider.RegisterAction(new RecordingAction("a", log), "a");
		provider.Undo();
		provider.Redo();
		provider.Clear();

		Assert.AreEqual(4, raised);
	}

	[TestMethod]
	public void Undo_WhenTheActionThrows_PropagatesAndKeepsTheAction()
	{
		// A failed undo leaves the history where it was, so the action can be undone again once
		// whatever made it fail is fixed. It is not moved to the redo stack as though it had worked.
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();
		provider.RegisterAction(new RecordingAction("a", log), "a");
		RecordingAction failing = new("b", log) { FailUndo = true };
		provider.RegisterAction(failing, "b");

		Assert.ThrowsExactly<InvalidOperationException>(() => provider.Undo());

		Assert.AreEqual("a,b", Describe(provider.GetUndoStack()));
		Assert.IsFalse(provider.CanRedo);
		Assert.IsEmpty(log);

		failing.FailUndo = false;
		Assert.IsTrue(provider.Undo());
		Assert.AreEqual("undo b", string.Join(",", log));
	}

	[TestMethod]
	public void Redo_WhenTheActionThrows_PropagatesAndKeepsTheAction()
	{
		List<string> log = [];
		SimpleUndoRedoProvider provider = new();
		RecordingAction action = new("a", log);
		provider.RegisterAction(action, "a");
		provider.Undo();
		action.FailExecute = true;

		Assert.ThrowsExactly<InvalidOperationException>(() => provider.Redo());

		Assert.IsTrue(provider.CanRedo);
		Assert.IsFalse(provider.CanUndo);
		Assert.AreEqual("undo a", string.Join(",", log));

		action.FailExecute = false;
		Assert.IsTrue(provider.Redo());
		Assert.AreEqual("undo a,execute a", string.Join(",", log));
	}
}
