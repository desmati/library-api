// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Policies;
using Library.Domain.Repositories;

using MediatR;

namespace Library.Application.Queries.UserReadingPace;

public class GetUserReadingPaceQueryHandler(ILoanRepository _loanRepository) :
	IRequestHandler<GetUserReadingPaceQuery, GetUserReadingPaceResult>
{
	public async Task<GetUserReadingPaceResult> Handle(GetUserReadingPaceQuery request, CancellationToken cancellationToken)
	{
		var loans = await _loanRepository.GetLoansByUserAsync(
			request.UserId,
			request.Start,
			request.End,
			cancellationToken);

		var paceResult = ReadingPacePolicy.CalculateUserPace(loans);

		var loanPaceInfos = paceResult.LoanPaces.Select(lp => new LoanPaceInfo(
			lp.BookId,
			lp.BookTitle,
			lp.Pages,
			lp.Days,
			lp.PagesPerDay,
			lp.BorrowedAt,
			lp.ReturnedAt
		)).ToList();

		return new GetUserReadingPaceResult(
			paceResult.AggregatePagesPerDay,
			loanPaceInfos
		);
	}
}
