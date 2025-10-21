// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;

namespace Library.Domain.Repositories;

public interface IBookRepository
{
	Task<Book?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default);
	Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default);
	Task AddAsync(Book book, CancellationToken cancellationToken = default);
	Task<bool> ExistsAsync(Guid bookId, CancellationToken cancellationToken = default);
}
