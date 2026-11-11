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
	/// <exception cref="NotSupportedException">
	/// An item is a subclass of <typeparamref name="T"/> and the JSON options configure no polymorphism for
	/// <typeparamref name="T"/>, so its type and the properties it adds would not be saved.
	/// </exception>
	public async Task SaveStateAsync(INavigationState<T> state, CancellationToken cancellationToken = default)
	{
		Ensure.NotNull(state);
		EnsureItemsKeepTheirType(state);

		// A state path that is a symbolic link, for example into a dotfiles repository, is saved
		// through: the new state goes to the link's final target and the link stays. Swapping the
		// temporary file in at the link's own path would replace the link with a regular file.
		string targetPath = ResolveFinalTarget(_filePath);

		string? directory = Path.GetDirectoryName(targetPath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string json = JsonSerializer.Serialize(state, _jsonOptions);
		byte[] bytes = Utf8NoBom.GetBytes(json);

		// Writing over the live file truncates it first, so a cancelled token, a crash or a full disk
		// part way through the write destroyed the last good save. Write the new state next to it and
		// swap it in only once it is complete and on disk.
		string tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
		try
		{
			using (FileStream stream = CreateTemporaryFile(tempPath, targetPath))
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

			if (File.Exists(targetPath))
			{
				File.Replace(tempPath, targetPath, destinationBackupFileName: null);
			}
			else
			{
				File.Move(tempPath, targetPath);
			}
		}
		catch
		{
			TryDelete(tempPath);
			throw;
		}
	}

	private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

	/// <summary>
	/// Refuses a state holding an item that JSON would save as the base <typeparamref name="T"/>
	/// </summary>
	/// <param name="state">The state about to be saved</param>
	/// <remarks>
	/// System.Text.Json writes each item as the declared <typeparamref name="T"/>, so without polymorphism a
	/// subclass's own properties were dropped and the item came back as a plain <typeparamref name="T"/>, with
	/// nothing reporting the loss. An interface or abstract <typeparamref name="T"/> is left alone: its saved
	/// state cannot be loaded back in any case, and <see cref="LoadStateAsync"/> reports that by returning null.
	/// </remarks>
	private void EnsureItemsKeepTheirType(INavigationState<T> state)
	{
		Type declaredType = typeof(T);
		if (declaredType.IsInterface || declaredType.IsAbstract)
		{
			return;
		}

		foreach (T item in state.Items)
		{
			Type? itemType = item?.GetType();
			if (itemType is null || itemType == declaredType)
			{
				continue;
			}

			if (_jsonOptions.GetTypeInfo(declaredType).PolymorphismOptions is null)
			{
				throw new NotSupportedException(
					$"Cannot save a navigation item of type '{itemType.FullName}' as '{declaredType.FullName}': JSON would "
					+ $"save it as a plain '{declaredType.Name}', losing its type and the properties it adds. Configure "
					+ $"polymorphism for '{declaredType.Name}' in the JSON options (a JsonDerivedType for '{itemType.Name}'), "
					+ $"or use a navigation stack of '{itemType.Name}'.");
			}

			return;
		}
	}

	private static string ResolveFinalTarget(string path)
	{
#if NET6_0_OR_GREATER
		FileInfo file = new(path);

		// LinkTarget is null both for a regular file and for a path that does not exist yet, where
		// ResolveLinkTarget would throw instead
		if (file.LinkTarget is null)
		{
			return path;
		}

		return file.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? path;
#else
		return path;
#endif
	}

	/// <summary>
	/// Creates the file the new state is written to before it replaces <paramref name="targetPath"/>.
	/// The swap renames it over the target, so the saved file has the temporary file's mode. On Unix
	/// that would be the umask default, which turned a state file restricted to 0600 into 0644 on
	/// the next save. The temporary file therefore takes the target's mode, from creation onward so
	/// that the new state is never readable by more users than the old one was.
	/// </summary>
	private static FileStream CreateTemporaryFile(string tempPath, string targetPath)
	{
#if NET7_0_OR_GREATER
		if (!OperatingSystem.IsWindows() && File.Exists(targetPath))
		{
			UnixFileMode mode = File.GetUnixFileMode(targetPath);
			FileStream stream = new(tempPath, new FileStreamOptions
			{
				Mode = FileMode.CreateNew,
				Access = FileAccess.Write,
				Share = FileShare.None,
				BufferSize = 4096,
				Options = FileOptions.Asynchronous,
				UnixCreateMode = mode,
			});

			// UnixCreateMode is masked by the umask, so set the mode exactly once the file exists
			try
			{
				File.SetUnixFileMode(tempPath, mode);
			}
			catch
			{
				stream.Dispose();
				throw;
			}

			return stream;
		}
#endif
		return new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
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
