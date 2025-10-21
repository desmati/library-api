// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Api.Models;

/// <summary>
/// Response containing the top borrowers within a time range.
/// </summary>
public record TopBorrowersResponse
{
	/// <summary>
	/// List of borrowers ordered by borrow count.
	/// </summary>
	public required List<BorrowerDto> Items { get; init; }
}

/// <summary>
/// Represents a user with their borrow count.
/// </summary>
public record BorrowerDto
{
	/// <summary>
	/// Unique identifier for the user.
	/// </summary>
	public required string UserId { get; init; }

	/// <summary>
	/// Full name of the user.
	/// </summary>
	public required string FullName { get; init; }

	/// <summary>
	/// Number of books borrowed by the user.
	/// </summary>
	public required long BorrowCount { get; init; }
}
