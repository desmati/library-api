// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Grpc.Core;
using Library.Application.Queries.AlsoBorrowedBooks;
using Library.Application.Queries.MostBorrowedBooks;
using Library.Contracts.Inventory.V1;
using MediatR;

namespace Library.Grpc.Services;

public class InventoryService(IMediator _mediator, ILogger<InventoryService> _logger)
	: Contracts.Inventory.V1.InventoryService.InventoryServiceBase
{
	public override async Task<MostBorrowedResponse> GetMostBorrowedBooks(MostBorrowedRequest request, ServerCallContext context)
	{
		_logger.LogInformation("Getting most borrowed books: Top={Top}, Start={Start}, End={End}", request.Top, request.Range?.Start, request.Range?.End);

		var query = new GetMostBorrowedBooksQuery(request.Top, ParseDateTime(request.Range?.Start), ParseDateTime(request.Range?.End));

		var result = await _mediator.Send(query, context.CancellationToken);

		var response = new MostBorrowedResponse();
		foreach (var item in result.Items)
		{
			response.Items.Add(new BookCount
			{
				BookId = item.BookId.ToString(),
				Title = item.Title,
				Author = item.Author,
				PageCount = item.PageCount,
				BorrowCount = item.BorrowCount
			});
		}

		return response;
	}

	public override async Task<AlsoBorrowedResponse> GetAlsoBorrowedBooks(AlsoBorrowedRequest request, ServerCallContext context)
	{
		_logger.LogInformation("Getting also borrowed books: BookId={BookId}, Top={Top}, Start={Start}, End={End}", request.BookId, request.Top, request.Range?.Start, request.Range?.End);

		var query = new GetAlsoBorrowedBooksQuery(Guid.Parse(request.BookId), request.Top, ParseDateTime(request.Range?.Start), ParseDateTime(request.Range?.End));

		var result = await _mediator.Send(query, context.CancellationToken);

		var response = new AlsoBorrowedResponse();
		foreach (var item in result.Items)
		{
			response.Items.Add(new AlsoBorrowedItem
			{
				BookId = item.BookId.ToString(),
				Title = item.Title,
				Author = item.Author,
				CoBorrowCount = item.CoBorrowCount
			});
		}

		return response;
	}

	private static DateTime? ParseDateTime(string? isoString)
	{
		if (string.IsNullOrWhiteSpace(isoString))
		{
			return null;
		}

		return DateTime.Parse(isoString, null, System.Globalization.DateTimeStyles.RoundtripKind);
	}
}
