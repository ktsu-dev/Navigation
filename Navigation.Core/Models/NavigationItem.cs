// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ktsu.Navigation.Core.Contracts;

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

		ArgumentNullException.ThrowIfNull(value);

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
