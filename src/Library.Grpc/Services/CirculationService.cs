// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Grpc.Core;
using Library.Application.Commands.BorrowBook;
using Library.Application.Commands.ReturnBook;
using Library.Contracts.Circulation.V1;
using MediatR;

namespace Library.Grpc.Services;

public class CirculationService(IMediator _mediator, ILogger<CirculationService> _logger)
	: Contracts.Circulation.V1.CirculationService.CirculationServiceBase
{
	public override async Task<BorrowBookResponse> BorrowBook(BorrowBookRequest request, ServerCallContext context)
	{
		_logger.LogInformation("Processing borrow book request: UserId={UserId}, BookId={BookId}, BorrowedAt={BorrowedAt}", request.UserId, request.BookId, request.BorrowedAt);

		var command = new BorrowBookCommand(Guid.Parse(request.UserId), Guid.Parse(request.BookId), ParseDateTime(request.BorrowedAt));

		var result = await _mediator.Send(command, context.CancellationToken);

		return new BorrowBookResponse { LoanId = result.LoanId.ToString() };
	}

	public override async Task<ReturnBookResponse> ReturnBook(ReturnBookRequest request, ServerCallContext context)
	{
		_logger.LogInformation("Processing return book request: LoanId={LoanId}, ReturnedAt={ReturnedAt}", request.LoanId, request.ReturnedAt);

		var command = new ReturnBookCommand(Guid.Parse(request.LoanId), ParseDateTime(request.ReturnedAt));

		var result = await _mediator.Send(command, context.CancellationToken);

		return new ReturnBookResponse { Success = result.Success };
	}

	private static DateTime ParseDateTime(string isoString)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(isoString, nameof(isoString));

		return DateTime.Parse(isoString, null, System.Globalization.DateTimeStyles.RoundtripKind);
	}
}
