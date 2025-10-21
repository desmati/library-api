// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentValidation;

using Library.Application.Commands.BorrowBook;

namespace Library.Application.Validators;

public class BorrowBookCommandValidator : AbstractValidator<BorrowBookCommand>
{
	public BorrowBookCommandValidator()
	{
		RuleFor(x => x.UserId)
			.NotEmpty()
			.WithMessage("User ID is required.");

		RuleFor(x => x.BookId)
			.NotEmpty()
			.WithMessage("Book ID is required.");

		RuleFor(x => x.BorrowedAt)
			.NotEmpty()
			.WithMessage("Borrowed date is required.")
			.LessThanOrEqualTo(DateTime.UtcNow)
			.WithMessage("Borrowed date cannot be in the future.");
	}
}
