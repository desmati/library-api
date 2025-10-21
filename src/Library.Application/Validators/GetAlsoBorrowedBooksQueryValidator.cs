// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentValidation;

using Library.Application.Queries.AlsoBorrowedBooks;

namespace Library.Application.Validators;

public class GetAlsoBorrowedBooksQueryValidator : AbstractValidator<GetAlsoBorrowedBooksQuery>
{
	public GetAlsoBorrowedBooksQueryValidator()
	{
		RuleFor(x => x.BookId)
			.NotEmpty()
			.WithMessage("Book ID is required.");

		RuleFor(x => x.Top)
			.InclusiveBetween(1, 100)
			.WithMessage("Top must be between 1 and 100.");

		RuleFor(x => x.Start)
			.LessThanOrEqualTo(x => x.End)
			.When(x => x.Start.HasValue && x.End.HasValue)
			.WithMessage("Start date must be before or equal to end date.");

		RuleFor(x => x.End)
			.LessThanOrEqualTo(DateTime.UtcNow)
			.When(x => x.End.HasValue)
			.WithMessage("End date cannot be in the future.");
	}
}
