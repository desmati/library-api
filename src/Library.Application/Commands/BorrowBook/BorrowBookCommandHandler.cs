// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.Domain.Repositories;

using MediatR;

namespace Library.Application.Commands.BorrowBook;

public class BorrowBookCommandHandler(ILoanRepository _loanRepository, IUserRepository _userRepository, IBookRepository _bookRepository)
	: IRequestHandler<BorrowBookCommand, BorrowBookResult>
{
	public async Task<BorrowBookResult> Handle(BorrowBookCommand request, CancellationToken cancellationToken)
	{
		if (!await _userRepository.ExistsAsync(request.UserId, cancellationToken))
		{
			throw new EntityNotFoundException(nameof(User), request.UserId);
		}

		if (!await _bookRepository.ExistsAsync(request.BookId, cancellationToken))
		{
			throw new EntityNotFoundException(nameof(Book), request.BookId);
		}

		var existingLoan = await _loanRepository.GetActiveLoanAsync(request.UserId, request.BookId, cancellationToken);
		if (existingLoan != null)
		{
			throw new InvalidOperationDomainException($"User {request.UserId} already has an active loan for book {request.BookId}");
		}

		var loan = Loan.Create(request.UserId, request.BookId, request.BorrowedAt);
		await _loanRepository.AddAsync(loan, cancellationToken);

		return new BorrowBookResult(loan.LoanId);
	}
}
