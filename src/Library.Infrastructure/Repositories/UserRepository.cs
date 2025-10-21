// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;
using Library.Domain.Repositories;
using Library.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Repositories;

public class UserRepository(LibraryDbContext _context)
	: IUserRepository
{
	public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return await _context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
	}

	public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Users
			.AsNoTracking()
			.OrderBy(u => u.FullName)
			.ToListAsync(cancellationToken);
	}

	public async Task AddAsync(User user, CancellationToken cancellationToken = default)
	{
		await _context.Users.AddAsync(user, cancellationToken);
		await _context.SaveChangesAsync(cancellationToken);
	}

	public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return await _context.Users
			.AnyAsync(u => u.UserId == userId, cancellationToken);
	}
}
