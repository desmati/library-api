// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Api.Models;

/// <summary>
/// Request to return a borrowed book.
/// </summary>
public record ReturnBookRequest
{
	/// <summary>
	/// Unique identifier for the loan record.
	/// </summary>
	public required string LoanId { get; init; }

	/// <summary>
	/// ISO 8601 timestamp when the book was returned. If not provided, current time is used.
	/// </summary>
	public string? ReturnedAt { get; init; }
}

/// <summary>
/// Response from returning a book.
/// </summary>
public record ReturnBookResponse
{
	/// <summary>
	/// Indicates whether the return operation was successful.
	/// </summary>
	public required bool Success { get; init; }
}
