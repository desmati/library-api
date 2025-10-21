// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Infrastructure.Queries;

using MediatR;

namespace Library.Application.Queries.AlsoBorrowedBooks;

public class GetAlsoBorrowedBooksQueryHandler(IQueryService _queryService)
	: IRequestHandler<GetAlsoBorrowedBooksQuery, GetAlsoBorrowedBooksResult>
{
	public async Task<GetAlsoBorrowedBooksResult> Handle(GetAlsoBorrowedBooksQuery request, CancellationToken cancellationToken)
	{
		var results = await _queryService.GetAlsoBorrowedBooksAsync(
			request.BookId,
			request.Top,
			request.Start,
			request.End,
			cancellationToken);

		var items = results.Select(r => new CoBorrowedBook(
			r.BookId,
			r.Title,
			r.Author,
			r.Count
		)).ToList();

		return new GetAlsoBorrowedBooksResult(items);
	}
}
