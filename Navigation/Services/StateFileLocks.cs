// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Services;

using System.Collections.Concurrent;
using System.IO;

/// <summary>
/// One lock per state file, shared by every <see cref="JsonFilePersistenceProvider{T}"/> in the process
/// whatever its item type, so two saves to the same file never swap their temporary files in at once.
/// </summary>
internal static class StateFileLocks
{
	private static readonly ConcurrentDictionary<string, object> Locks = new();

	/// <summary>
	/// Gets the lock for a state file
	/// </summary>
	/// <param name="path">The state file's path</param>
	/// <returns>The same object for every call with a path that names the same file</returns>
	internal static object For(string path) => Locks.GetOrAdd(Path.GetFullPath(path), _ => new object());
}
