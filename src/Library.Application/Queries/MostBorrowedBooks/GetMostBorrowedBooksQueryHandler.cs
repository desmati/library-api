// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Infrastructure.Queries;

using MediatR;

namespace Library.Application.Queries.MostBorrowedBooks;

public class GetMostBorrowedBooksQueryHandler(IQueryService _queryService)
	: IRequestHandler<GetMostBorrowedBooksQuery, GetMostBorrowedBooksResult>
{
	public async Task<GetMostBorrowedBooksResult> Handle(GetMostBorrowedBooksQuery request, CancellationToken cancellationToken)
	{
		var results = await _queryService.GetMostBorrowedBooksAsync(
			request.Top,
			request.Start,
			request.End,
			cancellationToken);

		var items = results.Select(r => new BookBorrowCount(
			r.BookId,
			r.Title,
			r.Author,
			r.PageCount,
			r.Count
		)).ToList();

		return new GetMostBorrowedBooksResult(items);
	}
}
