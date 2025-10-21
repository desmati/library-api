// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Api.Models;

/// <summary>
/// Response containing the most borrowed books within a time range.
/// </summary>
public record MostBorrowedBooksResponse
{
	/// <summary>
	/// List of books ordered by borrow count.
	/// </summary>
	public required List<BookCountDto> Items { get; init; }
}

/// <summary>
/// Represents a book with its borrow count.
/// </summary>
public record BookCountDto
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
	/// Number of pages in the book.
	/// </summary>
	public required int PageCount { get; init; }

	/// <summary>
	/// Number of times the book was borrowed.
	/// </summary>
	public required long BorrowCount { get; init; }
}
