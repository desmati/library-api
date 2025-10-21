// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Api.Models;

/// <summary>
/// Request to borrow a book.
/// </summary>
public record BorrowBookRequest
{
	/// <summary>
	/// Unique identifier for the user borrowing the book.
	/// </summary>
	public required string UserId { get; init; }

	/// <summary>
	/// Unique identifier for the book being borrowed.
	/// </summary>
	public required string BookId { get; init; }

	/// <summary>
	/// ISO 8601 timestamp when the book was borrowed. If not provided, current time is used.
	/// </summary>
	public string? BorrowedAt { get; init; }
}

/// <summary>
/// Response from borrowing a book.
/// </summary>
public record BorrowBookResponse
{
	/// <summary>
	/// Unique identifier for the loan record.
	/// </summary>
	public required string LoanId { get; init; }
}
