// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.Domain.Repositories;

using MediatR;

namespace Library.Application.Commands.ReturnBook;

public class ReturnBookCommandHandler(ILoanRepository _loanRepository)
	: IRequestHandler<ReturnBookCommand, ReturnBookResult>
{
	public async Task<ReturnBookResult> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
	{
		var loan = await _loanRepository.GetByIdAsync(request.LoanId, cancellationToken) ?? throw new EntityNotFoundException(nameof(Loan), request.LoanId);

		loan.Return(request.ReturnedAt);

		await _loanRepository.UpdateAsync(loan, cancellationToken);

		return new ReturnBookResult(true);
	}
}
