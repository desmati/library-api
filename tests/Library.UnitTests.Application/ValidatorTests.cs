// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentValidation.TestHelper;

using Library.Application.Commands.BorrowBook;
using Library.Application.Commands.ReturnBook;
using Library.Application.Queries.AlsoBorrowedBooks;
using Library.Application.Queries.MostBorrowedBooks;
using Library.Application.Queries.TopBorrowers;
using Library.Application.Queries.UserReadingPace;
using Library.Application.Validators;

namespace Library.UnitTests.Application;

public class ValidatorTests
{
	public class BorrowBookCommandValidatorTests
	{
		private readonly BorrowBookCommandValidator _validator;

		public BorrowBookCommandValidatorTests()
		{
			_validator = new BorrowBookCommandValidator();
		}

		[Fact]
		public void Validate_WithValidCommand_ShouldNotHaveErrors()
		{
			// Arrange
			var command = new BorrowBookCommand(
				Guid.NewGuid(),
				Guid.NewGuid(),
				DateTime.UtcNow.AddHours(-1));

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Fact]
		public void Validate_WithEmptyUserId_ShouldHaveError()
		{
			// Arrange
			var command = new BorrowBookCommand(
				Guid.Empty,
				Guid.NewGuid(),
				DateTime.UtcNow);

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.UserId)
				.WithErrorMessage("User ID is required.");
		}

		[Fact]
		public void Validate_WithEmptyBookId_ShouldHaveError()
		{
			// Arrange
			var command = new BorrowBookCommand(
				Guid.NewGuid(),
				Guid.Empty,
				DateTime.UtcNow);

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.BookId)
				.WithErrorMessage("Book ID is required.");
		}

		[Fact]
		public void Validate_WithFutureBorrowedDate_ShouldHaveError()
		{
			// Arrange
			var command = new BorrowBookCommand(
				Guid.NewGuid(),
				Guid.NewGuid(),
				DateTime.UtcNow.AddDays(1));

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.BorrowedAt)
				.WithErrorMessage("Borrowed date cannot be in the future.");
		}

		[Fact]
		public void Validate_WithDefaultBorrowedDate_ShouldHaveError()
		{
			// Arrange
			var command = new BorrowBookCommand(
				Guid.NewGuid(),
				Guid.NewGuid(),
				default);

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.BorrowedAt)
				.WithErrorMessage("Borrowed date is required.");
		}
	}

	public class ReturnBookCommandValidatorTests
	{
		private readonly ReturnBookCommandValidator _validator;

		public ReturnBookCommandValidatorTests()
		{
			_validator = new ReturnBookCommandValidator();
		}

		[Fact]
		public void Validate_WithValidCommand_ShouldNotHaveErrors()
		{
			// Arrange
			var command = new ReturnBookCommand(
				Guid.NewGuid(),
				DateTime.UtcNow.AddMinutes(-1));

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Fact]
		public void Validate_WithEmptyLoanId_ShouldHaveError()
		{
			// Arrange
			var command = new ReturnBookCommand(
				Guid.Empty,
				DateTime.UtcNow);

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.LoanId)
				.WithErrorMessage("Loan ID is required.");
		}

		[Fact]
		public void Validate_WithFutureReturnDate_ShouldHaveError()
		{
			// Arrange
			var command = new ReturnBookCommand(
				Guid.NewGuid(),
				DateTime.UtcNow.AddDays(1));

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.ReturnedAt)
				.WithErrorMessage("Return date cannot be in the future.");
		}

		[Fact]
		public void Validate_WithDefaultReturnDate_ShouldHaveError()
		{
			// Arrange
			var command = new ReturnBookCommand(
				Guid.NewGuid(),
				default);

			// Act
			var result = _validator.TestValidate(command);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.ReturnedAt)
				.WithErrorMessage("Return date is required.");
		}
	}

	public class GetMostBorrowedBooksQueryValidatorTests
	{
		private readonly GetMostBorrowedBooksQueryValidator _validator;

		public GetMostBorrowedBooksQueryValidatorTests()
		{
			_validator = new GetMostBorrowedBooksQueryValidator();
		}

		[Fact]
		public void Validate_WithValidQuery_ShouldNotHaveErrors()
		{
			// Arrange
			var query = new GetMostBorrowedBooksQuery(10, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(101)]
		public void Validate_WithInvalidTop_ShouldHaveError(int invalidTop)
		{
			// Arrange
			var query = new GetMostBorrowedBooksQuery(invalidTop, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Top)
				.WithErrorMessage("Top must be between 1 and 100.");
		}

		[Fact]
		public void Validate_WithStartAfterEnd_ShouldHaveError()
		{
			// Arrange
			var query = new GetMostBorrowedBooksQuery(
				10,
				new DateTime(2024, 12, 31),
				new DateTime(2024, 1, 1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Start)
				.WithErrorMessage("Start date must be before or equal to end date.");
		}

		[Fact]
		public void Validate_WithFutureEndDate_ShouldHaveError()
		{
			// Arrange
			var query = new GetMostBorrowedBooksQuery(
				10,
				null,
				DateTime.UtcNow.AddDays(1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.End)
				.WithErrorMessage("End date cannot be in the future.");
		}

		[Fact]
		public void Validate_WithValidDateRange_ShouldNotHaveErrors()
		{
			// Arrange
			var query = new GetMostBorrowedBooksQuery(
				10,
				new DateTime(2024, 1, 1),
				new DateTime(2024, 12, 31));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldNotHaveAnyValidationErrors();
		}
	}

	public class GetTopBorrowersQueryValidatorTests
	{
		private readonly GetTopBorrowersQueryValidator _validator;

		public GetTopBorrowersQueryValidatorTests()
		{
			_validator = new GetTopBorrowersQueryValidator();
		}

		[Fact]
		public void Validate_WithValidQuery_ShouldNotHaveErrors()
		{
			// Arrange
			var query = new GetTopBorrowersQuery(10, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(101)]
		public void Validate_WithInvalidTop_ShouldHaveError(int invalidTop)
		{
			// Arrange
			var query = new GetTopBorrowersQuery(invalidTop, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Top)
				.WithErrorMessage("Top must be between 1 and 100.");
		}

		[Fact]
		public void Validate_WithStartAfterEnd_ShouldHaveError()
		{
			// Arrange
			var query = new GetTopBorrowersQuery(
				10,
				new DateTime(2024, 12, 31),
				new DateTime(2024, 1, 1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Start)
				.WithErrorMessage("Start date must be before or equal to end date.");
		}

		[Fact]
		public void Validate_WithFutureEndDate_ShouldHaveError()
		{
			// Arrange
			var query = new GetTopBorrowersQuery(
				10,
				null,
				DateTime.UtcNow.AddDays(1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.End)
				.WithErrorMessage("End date cannot be in the future.");
		}
	}

	public class GetUserReadingPaceQueryValidatorTests
	{
		private readonly GetUserReadingPaceQueryValidator _validator;

		public GetUserReadingPaceQueryValidatorTests()
		{
			_validator = new GetUserReadingPaceQueryValidator();
		}

		[Fact]
		public void Validate_WithValidQuery_ShouldNotHaveErrors()
		{
			// Arrange
			var query = new GetUserReadingPaceQuery(Guid.NewGuid(), null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Fact]
		public void Validate_WithEmptyUserId_ShouldHaveError()
		{
			// Arrange
			var query = new GetUserReadingPaceQuery(Guid.Empty, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.UserId)
				.WithErrorMessage("User ID is required.");
		}

		[Fact]
		public void Validate_WithStartAfterEnd_ShouldHaveError()
		{
			// Arrange
			var query = new GetUserReadingPaceQuery(
				Guid.NewGuid(),
				new DateTime(2024, 12, 31),
				new DateTime(2024, 1, 1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Start)
				.WithErrorMessage("Start date must be before or equal to end date.");
		}

		[Fact]
		public void Validate_WithFutureEndDate_ShouldHaveError()
		{
			// Arrange
			var query = new GetUserReadingPaceQuery(
				Guid.NewGuid(),
				null,
				DateTime.UtcNow.AddDays(1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.End)
				.WithErrorMessage("End date cannot be in the future.");
		}
	}

	public class GetAlsoBorrowedBooksQueryValidatorTests
	{
		private readonly GetAlsoBorrowedBooksQueryValidator _validator;

		public GetAlsoBorrowedBooksQueryValidatorTests()
		{
			_validator = new GetAlsoBorrowedBooksQueryValidator();
		}

		[Fact]
		public void Validate_WithValidQuery_ShouldNotHaveErrors()
		{
			// Arrange
			var query = new GetAlsoBorrowedBooksQuery(Guid.NewGuid(), 10, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldNotHaveAnyValidationErrors();
		}

		[Fact]
		public void Validate_WithEmptyBookId_ShouldHaveError()
		{
			// Arrange
			var query = new GetAlsoBorrowedBooksQuery(Guid.Empty, 10, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.BookId)
				.WithErrorMessage("Book ID is required.");
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(101)]
		public void Validate_WithInvalidTop_ShouldHaveError(int invalidTop)
		{
			// Arrange
			var query = new GetAlsoBorrowedBooksQuery(Guid.NewGuid(), invalidTop, null, null);

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Top)
				.WithErrorMessage("Top must be between 1 and 100.");
		}

		[Fact]
		public void Validate_WithStartAfterEnd_ShouldHaveError()
		{
			// Arrange
			var query = new GetAlsoBorrowedBooksQuery(
				Guid.NewGuid(),
				10,
				new DateTime(2024, 12, 31),
				new DateTime(2024, 1, 1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.Start)
				.WithErrorMessage("Start date must be before or equal to end date.");
		}

		[Fact]
		public void Validate_WithFutureEndDate_ShouldHaveError()
		{
			// Arrange
			var query = new GetAlsoBorrowedBooksQuery(
				Guid.NewGuid(),
				10,
				null,
				DateTime.UtcNow.AddDays(1));

			// Act
			var result = _validator.TestValidate(query);

			// Assert
			result.ShouldHaveValidationErrorFor(x => x.End)
				.WithErrorMessage("End date cannot be in the future.");
		}
	}
}
