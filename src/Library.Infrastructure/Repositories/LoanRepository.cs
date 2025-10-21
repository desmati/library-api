// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;
using Library.Domain.Repositories;
using Library.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Repositories;

public class LoanRepository(LibraryDbContext _context)
	: ILoanRepository
{
	public async Task<Loan?> GetByIdAsync(Guid loanId, CancellationToken cancellationToken = default)
	{
		return await _context.Loans
			.Include(l => l.Book)
			.Include(l => l.User)
			.AsNoTracking()
			.FirstOrDefaultAsync(l => l.LoanId == loanId, cancellationToken);
	}

	public async Task<Loan?> GetActiveLoanAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default)
	{
		return await _context.Loans
			.Include(l => l.Book)
			.Include(l => l.User)
			.AsNoTracking()
			.FirstOrDefaultAsync(l => l.UserId == userId
				&& l.BookId == bookId
				&& l.ReturnedAt == null,
				cancellationToken);
	}

	public async Task<IEnumerable<Loan>> GetLoansByUserAsync(
		Guid userId,
		DateTime? start = null,
		DateTime? end = null,
		CancellationToken cancellationToken = default)
	{
		var query = _context.Loans
			.Include(l => l.Book)
			.Include(l => l.User)
			.AsNoTracking()
			.Where(l => l.UserId == userId);

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

		return await query
			.OrderByDescending(l => l.BorrowedAt)
			.ToListAsync(cancellationToken);
	}

	public async Task<IEnumerable<Loan>> GetLoansByBookAsync(
		Guid bookId,
		DateTime? start = null,
		DateTime? end = null,
		CancellationToken cancellationToken = default)
	{
		var query = _context.Loans
			.Include(l => l.Book)
			.Include(l => l.User)
			.AsNoTracking()
			.Where(l => l.BookId == bookId);

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

		return await query
			.OrderByDescending(l => l.BorrowedAt)
			.ToListAsync(cancellationToken);
	}

	public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
	{
		await _context.Loans.AddAsync(loan, cancellationToken);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task UpdateAsync(Loan loan, CancellationToken cancellationToken = default)
	{
		_context.Loans.Update(loan);
		await _context.SaveChangesAsync(cancellationToken);
	}
}
