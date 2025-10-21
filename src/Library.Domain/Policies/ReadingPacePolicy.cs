// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;
using Library.Domain.ValueObjects;

namespace Library.Domain.Policies;

/// <summary>
/// Reading pace claculation policy
/// </summary>
/// <remarks>Only returned loans are considered.</remarks>
public static class ReadingPacePolicy
{
	/// <summary>
	/// Calculates reading pace (pages per day) for a single loan.
	/// </summary>
	public static double? CalculateLoanPace(Loan loan)
	{
		if (!loan.IsReturned || loan.ReturnedAt is null)
		{
			return null;
		}

		// At least 1 day per loan
		var days = Math.Max(1, (loan.ReturnedAt.Value - loan.BorrowedAt).TotalDays);

		return loan.Book.PageCount / days;
	}

	/// <summary>
	/// Calculates reading pace from a loan with page count provided separately
	/// </summary>
	public static double CalculatePace(int pageCount, DateTime borrowedAt, DateTime returnedAt)
	{
		if (returnedAt < borrowedAt)
		{
			throw new ArgumentException("Return date cannot be before borrow date");
		}

		var days = Math.Max(1, (returnedAt - borrowedAt).TotalDays);

		return pageCount / days;
	}

	/// <summary>
	/// Calculates aggregate reading pace from multiple loans
	/// </summary>
	/// <param name="loans"></param>
	/// <returns>Average of per-loan paces</returns>
	public static ReadingPaceResult CalculateUserPace(IEnumerable<Loan> loans)
	{
		var returnedLoans = loans.Where(l => l.IsReturned && l.ReturnedAt.HasValue).ToList();

		if (returnedLoans.Count == 0)
		{
			return new ReadingPaceResult(0, []);
		}

		var loanPaces = new List<LoanReadingPace>();

		foreach (var loan in returnedLoans)
		{
			var days = Math.Max(1, (loan.ReturnedAt!.Value - loan.BorrowedAt).TotalDays);
			var pagesPerDay = loan.Book.PageCount / days;

			loanPaces.Add(new LoanReadingPace(
				loan.LoanId,
				loan.BookId,
				loan.Book.Title,
				loan.Book.PageCount,
				days,
				pagesPerDay,
				loan.BorrowedAt,
				loan.ReturnedAt.Value
			));
		}

		var aggregatePace = loanPaces.Average(lp => lp.PagesPerDay);

		return new ReadingPaceResult(aggregatePace, loanPaces);
	}
}
