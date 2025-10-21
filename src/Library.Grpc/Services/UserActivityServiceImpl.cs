// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Grpc.Core;
using Library.Application.Queries.TopBorrowers;
using Library.Application.Queries.UserReadingPace;
using Library.Contracts.UserActivity.V1;
using MediatR;

namespace Library.Grpc.Services;

public class UserActivityService(IMediator _mediator, ILogger<UserActivityService> _logger)
	: Contracts.UserActivity.V1.UserActivityService.UserActivityServiceBase
{
	public override async Task<TopBorrowersResponse> GetTopBorrowers(TopBorrowersRequest request, ServerCallContext context)
	{
		_logger.LogInformation("Getting top borrowers: Top={Top}, Start={Start}, End={End}", request.Top, request.Range?.Start, request.Range?.End);

		var query = new GetTopBorrowersQuery(request.Top, ParseDateTime(request.Range?.Start), ParseDateTime(request.Range?.End));

		var result = await _mediator.Send(query, context.CancellationToken);

		var response = new TopBorrowersResponse();
		foreach (var item in result.Items)
		{
			response.Items.Add(new Borrower
			{
				UserId = item.UserId.ToString(),
				FullName = item.FullName,
				BorrowCount = item.BorrowCount
			});
		}

		return response;
	}

	public override async Task<ReadingPaceResponse> GetReadingPace(ReadingPaceRequest request, ServerCallContext context)
	{
		_logger.LogInformation("Getting reading pace: UserId={UserId}, Start={Start}, End={End}", request.UserId, request.Range?.Start, request.Range?.End);

		var query = new GetUserReadingPaceQuery(Guid.Parse(request.UserId), ParseDateTime(request.Range?.Start), ParseDateTime(request.Range?.End));

		var result = await _mediator.Send(query, context.CancellationToken);

		var response = new ReadingPaceResponse
		{
			AggregatePagesPerDay = result.AggregatePagesPerDay
		};

		foreach (var item in result.Items)
		{
			response.Items.Add(new ReadingPaceItem
			{
				BookId = item.BookId.ToString(),
				Title = item.Title,
				Pages = item.Pages,
				Days = item.Days,
				PagesPerDay = item.PagesPerDay,
				BorrowedAt = FormatDateTime(item.BorrowedAt),
				ReturnedAt = FormatDateTime(item.ReturnedAt)
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

	private static string FormatDateTime(DateTime dateTime)
	{
		return dateTime.ToUniversalTime().ToString("O");
	}
}
