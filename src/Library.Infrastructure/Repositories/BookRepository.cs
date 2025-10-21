// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;
using Library.Domain.Repositories;
using Library.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Repositories;

public class BookRepository(LibraryDbContext _context)
	: IBookRepository
{
	public async Task<Book?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default)
	{
		return await _context.Books
			.AsNoTracking()
			.FirstOrDefaultAsync(b => b.BookId == bookId, cancellationToken);
	}

	public async Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Books
			.AsNoTracking()
			.OrderBy(b => b.Title)
			.ToListAsync(cancellationToken);
	}

	public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
	{
		await _context.Books.AddAsync(book, cancellationToken);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<bool> ExistsAsync(Guid bookId, CancellationToken cancellationToken = default)
	{
		return await _context.Books
			.AnyAsync(b => b.BookId == bookId, cancellationToken);
	}
}
