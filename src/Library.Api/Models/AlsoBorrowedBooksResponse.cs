// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Api.Models;

/// <summary>
/// Response containing books that were also borrowed by users who borrowed a specific book.
/// </summary>
public record AlsoBorrowedBooksResponse
{
	/// <summary>
	/// List of books ordered by co-borrow count.
	/// </summary>
	public required List<AlsoBorrowedItemDto> Items { get; init; }
}

/// <summary>
/// Represents a book that was also borrowed with its co-borrow count.
/// </summary>
public record AlsoBorrowedItemDto
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
	/// Author of the book.
	/// </summary>
	public required string Author { get; init; }

	/// <summary>
	/// Number of times this book was borrowed by users who also borrowed the reference book.
	/// </summary>
	public required long CoBorrowCount { get; init; }
}
