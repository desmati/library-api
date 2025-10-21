// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Api.Models;

/// <summary>
/// Response containing a user's reading pace analytics.
/// </summary>
public record ReadingPaceResponse
{
	/// <summary>
	/// Average pages per day across all completed books.
	/// </summary>
	public required double AggregatePagesPerDay { get; init; }

	/// <summary>
	/// List of individual reading pace items for each completed book.
	/// </summary>
	public required List<ReadingPaceItemDto> Items { get; init; }
}

/// <summary>
/// Represents reading pace data for a single book.
/// </summary>
public record ReadingPaceItemDto
{
	/// <summary>
	/// Unique identifier for the book.
	/// </summary>
	public required string BookId { get; init; }

	/// <summary>
	/// Title of the book.
	/// </summary>
	public required string Title { get; init; }

	/// <summary>
	/// Number of pages in the book.
	/// </summary>
	public required int Pages { get; init; }

	/// <summary>
	/// Number of days the book was borrowed.
	/// </summary>
	public required double Days { get; init; }

	/// <summary>
	/// Reading pace in pages per day for this book.
	/// </summary>
	public required double PagesPerDay { get; init; }

	/// <summary>
	/// ISO 8601 timestamp when the book was borrowed.
	/// </summary>
	public required string BorrowedAt { get; init; }

	/// <summary>
	/// ISO 8601 timestamp when the book was returned.
	/// </summary>
	public required string ReturnedAt { get; init; }
}
