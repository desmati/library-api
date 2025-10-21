// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Queries;

public class QueryService(LibraryDbContext _context) : IQueryService
{
	public async Task<List<(Guid BookId, string Title, string Author, int PageCount, long Count)>>
		GetMostBorrowedBooksAsync(int top, DateTime? start, DateTime? end, CancellationToken cancellationToken = default)
	{
		var query = _context.Loans.AsNoTracking();

		if (start.HasValue)
		{
			var startUtc = start.Value.ToUniversalTime();
			query = query.Where(l => l.BorrowedAt >= startUtc);
		}

		if (end.HasValue)
		{
			var endUtc = end.Value.ToUniversalTime();
			query = query.Where(l => l.BorrowedAt <= endUtc);
		}

		var result = await query
			.GroupBy(l => new
			{
				l.BookId,
				l.Book.Title,
				l.Book.Author,
				l.Book.PageCount
			})
			.Select(g => new
			{
				g.Key.BookId,
				g.Key.Title,
				g.Key.Author,
				g.Key.PageCount,
				Count = (long)g.Count()
			})
			.OrderByDescending(x => x.Count)
			.Take(top)
			.ToListAsync(cancellationToken);

		return [.. result.Select(x => (x.BookId, x.Title, x.Author, x.PageCount, x.Count))];
	}

	public async Task<List<(Guid UserId, string FullName, long Count)>> GetTopBorrowersAsync(
		int top,
		DateTime? start,
		DateTime? end,
		CancellationToken cancellationToken = default)
	{
		var query = _context.Loans.AsNoTracking();

		if (start.HasValue)
		{
			var startUtc = start.Value.ToUniversalTime();
			query = query.Where(l => l.BorrowedAt >= startUtc);
		}

		if (end.HasValue)
		{
			var endUtc = end.Value.ToUniversalTime();
			query = query.Where(l => l.BorrowedAt <= endUtc);
		}

		var result = await query
			.GroupBy(l => new
			{
				l.UserId,
				l.User.FullName
			})
			.Select(g => new
			{
				g.Key.UserId,
				g.Key.FullName,
				Count = (long)g.Count()
			})
			.OrderByDescending(x => x.Count)
			.Take(top)
			.ToListAsync(cancellationToken);

		return [.. result.Select(x => (x.UserId, x.FullName, x.Count))];
	}

	public async Task<List<(Guid BookId, string Title, string Author, long Count)>> GetAlsoBorrowedBooksAsync(
		Guid bookId,
		int top,
		DateTime? start,
		DateTime? end,
		CancellationToken cancellationToken = default)
	{
		// All users who borrowed the specified book
		var usersQuery = _context.Loans
			.AsNoTracking()
			.Where(l => l.BookId == bookId);

		if (start.HasValue)
		{
			var startUtc = start.Value.ToUniversalTime();
			usersQuery = usersQuery.Where(l => l.BorrowedAt >= startUtc);
		}

		if (end.HasValue)
		{
			var endUtc = end.Value.ToUniversalTime();
			usersQuery = usersQuery.Where(l => l.BorrowedAt <= endUtc);
		}

		var userIds = await usersQuery
			.Select(l => l.UserId)
			.Distinct()
			.ToListAsync(cancellationToken);

		if (userIds.Count == 0)
		{
			return [];
		}

		// Other books borrowed by these users
		var otherBooksQuery = _context.Loans
			.AsNoTracking()
			.Where(l => userIds.Contains(l.UserId) && l.BookId != bookId);

		if (start.HasValue)
		{
			var startUtc = start.Value.ToUniversalTime();
			otherBooksQuery = otherBooksQuery.Where(l => l.BorrowedAt >= startUtc);
		}

		if (end.HasValue)
		{
			var endUtc = end.Value.ToUniversalTime();
			otherBooksQuery = otherBooksQuery.Where(l => l.BorrowedAt <= endUtc);
		}

		var result = await otherBooksQuery
			.GroupBy(l => new
			{
				l.BookId,
				l.Book.Title,
				l.Book.Author
			})
			.Select(g => new
			{
				g.Key.BookId,
				g.Key.Title,
				g.Key.Author,
				Count = (long)g.Count()
			})
			.OrderByDescending(x => x.Count)
			.Take(top)
			.ToListAsync(cancellationToken);

		return [.. result.Select(x => (x.BookId, x.Title, x.Author, x.Count))];
	}
}
