// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Navigation.Core.Contracts;

using System;
using System.Collections.Generic;

/// <summary>
/// Defines the contract for items that can be stored in a navigation stack
/// </summary>
public interface INavigationItem
{
	/// <summary>
	/// Gets the unique identifier for this navigation item
	/// </summary>
	public string Id { get; }

	/// <summary>
	/// Gets or sets the display name for this navigation item
	/// </summary>
	public string DisplayName { get; set; }

	/// <summary>
	/// Gets the timestamp when this navigation item was created
	/// </summary>
	public DateTime CreatedAt { get; }

	/// <summary>
	/// Gets additional metadata associated with this navigation item
	/// </summary>
	public IReadOnlyDictionary<string, object> Metadata { get; }

	/// <summary>
	/// Adds or updates metadata for this navigation item
	/// </summary>
	/// <param name="key">The metadata key</param>
	/// <param name="value">The metadata value</param>
	public void SetMetadata(string key, object value);

	/// <summary>
	/// Removes metadata from this navigation item
	/// </summary>
	/// <param name="key">The metadata key to remove</param>
	/// <returns>True if the metadata was removed; otherwise, false</returns>
	public bool RemoveMetadata(string key);
}
