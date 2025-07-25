// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Services;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ktsu.Navigation.Core.Contracts;
using ktsu.Navigation.Core.Models;

/// <summary>
/// A JSON file-based persistence provider for navigation state
/// </summary>
/// <typeparam name="T">The type of navigation items</typeparam>
public class JsonFilePersistenceProvider<T> : IPersistenceProvider<T> where T : INavigationItem
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
		ArgumentNullException.ThrowIfNull(state);

		string? directory = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string json = JsonSerializer.Serialize(state, _jsonOptions);
		await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
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
