// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Navigation.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using ktsu.Navigation.Contracts;

/// <summary>
/// A base implementation of a navigation item
/// </summary>
public class NavigationItem : INavigationItem
{
	private readonly Dictionary<string, object> _metadata;

	/// <summary>
	/// Initializes a new instance of the <see cref="NavigationItem"/> class
	/// </summary>
	/// <param name="id">The unique identifier for this navigation item</param>
	/// <param name="displayName">The display name for this navigation item</param>
	public NavigationItem(string id, string displayName)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new ArgumentException("Id cannot be null or whitespace.", nameof(id));
		}

		if (string.IsNullOrWhiteSpace(displayName))
		{
			throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));
		}

		Id = id;
		DisplayName = displayName;
		CreatedAt = DateTime.UtcNow;
		_metadata = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NavigationItem"/> class when deserializing, keeping the
	/// creation time and metadata that were saved. System.Text.Json requires each constructor parameter to have
	/// the same name and type as the property it binds to.
	/// </summary>
	/// <remarks>
	/// Metadata values read from JSON are <see cref="System.Text.Json.JsonElement"/> instances rather than
	/// their original CLR types, because the saved JSON does not record those types.
	/// </remarks>
	/// <param name="id">The unique identifier for this navigation item</param>
	/// <param name="displayName">The display name for this navigation item</param>
	/// <param name="createdAt">The timestamp when this navigation item was first created</param>
	/// <param name="metadata">The metadata to restore; null entries are skipped</param>
	[JsonConstructor]
	public NavigationItem(string id, string displayName, DateTime createdAt, IReadOnlyDictionary<string, object>? metadata)
		: this(id, displayName)
	{
		if (createdAt != default)
		{
			CreatedAt = createdAt;
		}

		if (metadata != null)
		{
			foreach (KeyValuePair<string, object> entry in metadata.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null))
			{
				_metadata[entry.Key] = entry.Value;
			}
		}
	}

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public string DisplayName { get; set; }

	/// <inheritdoc />
	public DateTime CreatedAt { get; }

	/// <inheritdoc />
	public IReadOnlyDictionary<string, object> Metadata => new ReadOnlyDictionary<string, object>(_metadata);

	/// <inheritdoc />
	public void SetMetadata(string key, object value)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
		}

		Ensure.NotNull(value);

		_metadata[key] = value;
	}

	/// <inheritdoc />
	public bool RemoveMetadata(string key) => !string.IsNullOrWhiteSpace(key) && _metadata.Remove(key);

	/// <inheritdoc />
	public override string ToString() => DisplayName;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is NavigationItem other && Id.Equals(other.Id, StringComparison.Ordinal);

	/// <inheritdoc />
	public override int GetHashCode() => Id.GetHashCode(StringComparison.Ordinal);
}
