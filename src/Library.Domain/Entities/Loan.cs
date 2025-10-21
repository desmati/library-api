// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Domain.Entities;

public class Loan
{
	public Guid LoanId { get; private set; }
	public Guid BookId { get; private set; }
	public Guid UserId { get; private set; }
	public DateTime BorrowedAt { get; private set; }
	public DateTime? ReturnedAt { get; private set; }

	// Navigation properties
	public Book Book { get; private set; } = null!;
	public User User { get; private set; } = null!;

	public bool IsReturned => ReturnedAt.HasValue;

	public Loan(Guid loanId, Guid bookId, Guid userId, DateTime borrowedAt, DateTime? returnedAt = null)
	{
		if (loanId == Guid.Empty)
		{
			throw new ArgumentException("LoanId cannot be empty", nameof(loanId));
		}

		if (bookId == Guid.Empty)
		{
			throw new ArgumentException("BookId cannot be empty", nameof(bookId));
		}

		if (userId == Guid.Empty)
		{
			throw new ArgumentException("UserId cannot be empty", nameof(userId));
		}

		LoanId = loanId;
		BookId = bookId;
		UserId = userId;
		BorrowedAt = borrowedAt;
		ReturnedAt = returnedAt;
	}

	public static Loan Create(Guid userId, Guid bookId, DateTime borrowedAt)
	{
		return new Loan(Guid.NewGuid(), bookId, userId, borrowedAt);
	}

	public void Return(DateTime returnedAt)
	{
		if (ReturnedAt.HasValue)
		{
			throw new InvalidOperationException("Loan has already been returned");
		}

		if (returnedAt < BorrowedAt)
		{
			throw new ArgumentException("Return date cannot be before borrow date", nameof(returnedAt));
		}

		ReturnedAt = returnedAt;
	}

	[Obsolete("EF Core ctor", false)] private Loan() { }
}
