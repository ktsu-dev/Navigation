// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Services;

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ktsu.Navigation.Contracts;
using ktsu.Navigation.Models;

/// <summary>
/// A JSON file-based persistence provider for navigation state
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
public class JsonFilePersistenceProvider<T> : IPersistenceProvider<T> where T : class, INavigationItem
{
	private readonly string _filePath;
	private readonly JsonSerializerOptions _jsonOptions;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonFilePersistenceProvider{T}"/> class
	/// </summary>
	/// <param name="filePath">The file path to store the navigation state</param>
	/// <param name="jsonOptions">Optional JSON serializer options</param>
	public JsonFilePersistenceProvider(string filePath, JsonSerializerOptions? jsonOptions = null)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
		}

		_filePath = filePath;
		_jsonOptions = jsonOptions ?? new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
	}

	/// <inheritdoc />
	public async Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default)
	{
		Ensure.NotNull(state);

		string? directory = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string json = JsonSerializer.Serialize(state, _jsonOptions);
		byte[] bytes = Utf8NoBom.GetBytes(json);

		// Writing over the live file truncates it first, so a cancelled token, a crash or a full disk
		// part way through the write destroyed the last good save. Write the new state next to it and
		// swap it in only once it is complete and on disk.
		string tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
		try
		{
			using (FileStream stream = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
			{
				await stream.WriteAsync(bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
				await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

				// There is no asynchronous way to flush through the OS cache, and without it a power loss
				// just after the swap below can still leave an empty file behind the new name
#pragma warning disable CA1849 // Call async methods when in an async method
				stream.Flush(flushToDisk: true);
#pragma warning restore CA1849
			}

			cancellationToken.ThrowIfCancellationRequested();

			SwapIntoPlace(tempPath, _filePath);
		}
		catch
		{
			TryDelete(tempPath);
			throw;
		}
	}

	private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

	/// <summary>
	/// Moves a complete temporary file over the state file, whether or not the state file exists yet.
	/// Two saves can both find no state file. Whichever moves second must replace the first one's file
	/// rather than fail with "already exists", so the last writer wins as it does once the file exists.
	/// </summary>
	private static void SwapIntoPlace(string tempPath, string filePath)
	{
		if (File.Exists(filePath))
		{
			File.Replace(tempPath, filePath, destinationBackupFileName: null);
			return;
		}

#if NETCOREAPP3_0_OR_GREATER
		File.Move(tempPath, filePath, overwrite: true);
#else
		try
		{
			File.Move(tempPath, filePath);
		}
		catch (IOException) when (File.Exists(filePath))
		{
			// Another save created the file after the check above
			File.Replace(tempPath, filePath, destinationBackupFileName: null);
		}
#endif
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (IOException)
		{
			// Best effort: a leftover temporary file never replaces the saved state
		}
		catch (UnauthorizedAccessException)
		{
			// Best effort, as above
		}
	}

	/// <inheritdoc />
	public async Task<INavigationState<T>?> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (!File.Exists(_filePath))
		{
			return null;
		}

		try
		{
			string json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
			return JsonSerializer.Deserialize<NavigationState<T>>(json, _jsonOptions);
		}
		catch (JsonException)
		{
			// If deserialization fails, return null
			return null;
		}
		catch (IOException)
		{
			// If file read fails, return null
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			// If the file exists but cannot be read (permissions, ACLs), degrade the same way as other read failures
			return null;
		}
		catch (ArgumentException)
		{
			// If the saved state is invalid (for example, a current index outside the items), return null
			return null;
		}
		catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
		{
			// System.Text.Json saves some item types it cannot construct again: an interface or abstract T,
			// or a constructor whose parameters don't match properties. Degrade the same way, rather than
			// throwing on every start-up until the state file is deleted
			return null;
		}
	}

	/// <inheritdoc />
	public Task<bool> HasSavedStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(File.Exists(_filePath));

	/// <inheritdoc />
	public Task ClearSavedStateAsync(CancellationToken cancellationToken = default)
	{
		if (File.Exists(_filePath))
		{
			File.Delete(_filePath);
		}

		return Task.CompletedTask;
	}
}
