// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentValidation;

using Library.Application.Commands.ReturnBook;

namespace Library.Application.Validators;

public class ReturnBookCommandValidator : AbstractValidator<ReturnBookCommand>
{
	public ReturnBookCommandValidator()
	{
		RuleFor(x => x.LoanId)
			.NotEmpty()
			.WithMessage("Loan ID is required.");

		RuleFor(x => x.ReturnedAt)
			.NotEmpty()
			.WithMessage("Return date is required.")
			.LessThanOrEqualTo(DateTime.UtcNow)
			.WithMessage("Return date cannot be in the future.");
	}
}
