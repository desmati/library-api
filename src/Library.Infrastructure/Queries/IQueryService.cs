// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Infrastructure.Queries;

public interface IQueryService
{
	// TODO: Create contracts for the inline records

	/// <summary>
	/// Gets the most borrowed books within an optional date range.
	/// </summary>
	/// <param name="top">Number of top books to return</param>
	/// <param name="start">Optional start date for filtering loans</param>
	/// <param name="end">Optional end date for filtering loans</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of books with their borrow counts</returns>
	Task<List<(Guid BookId, string Title, string Author, int PageCount, long Count)>> GetMostBorrowedBooksAsync(
		int top,
		DateTime? start,
		DateTime? end,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the top borrowers (users with most loans) within an optional date range.
	/// </summary>
	/// <param name="top">Number of top borrowers to return</param>
	/// <param name="start">Optional start date for filtering loans</param>
	/// <param name="end">Optional end date for filtering loans</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of users with their loan counts</returns>
	Task<List<(Guid UserId, string FullName, long Count)>> GetTopBorrowersAsync(
		int top,
		DateTime? start,
		DateTime? end,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets books that were also borrowed by users who borrowed the specified book.
	/// </summary>
	/// <param name="bookId">The book ID to find related books for</param>
	/// <param name="top">Number of related books to return</param>
	/// <param name="start">Optional start date for filtering loans</param>
	/// <param name="end">Optional end date for filtering loans</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>List of related books with their co-borrow counts</returns>
	Task<List<(Guid BookId, string Title, string Author, long Count)>> GetAlsoBorrowedBooksAsync(
		Guid bookId,
		int top,
		DateTime? start,
		DateTime? end,
		CancellationToken cancellationToken = default);
}
