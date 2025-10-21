// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using Library.Domain.Entities;
using Library.Domain.Policies;

namespace Library.UnitTests.Domain;

public class ReadingPacePolicyTests
{
	public class CalculateLoanPaceTests
	{
		[Fact]
		public void CalculateLoanPace_WithReturnedLoan_ShouldCalculateCorrectly()
		{
			// Arrange
			var borrowedAt = new DateTime(2024, 1, 1);
			var returnedAt = new DateTime(2024, 1, 11); // 10 days
			var book = Book.Create("ISBN-123", "Test Book", "Test Author", 300);
			var loan = Loan.Create(Guid.NewGuid(), book.BookId, borrowedAt);

			// Use reflection to set the Book navigation property
			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan, book);

			loan.Return(returnedAt);

			// Act
			var pace = ReadingPacePolicy.CalculateLoanPace(loan);

			// Assert
			pace.Should().NotBeNull();
			pace.Should().Be(30.0); // 300 pages / 10 days
		}

		[Fact]
		public void CalculateLoanPace_WithSameDayReturn_ShouldUseMinimumOneDayForCalculation()
		{
			// Arrange
			var borrowedAt = new DateTime(2024, 1, 1, 10, 0, 0);
			var returnedAt = new DateTime(2024, 1, 1, 14, 0, 0); // Same day, 4 hours later
			var book = Book.Create("ISBN-123", "Test Book", "Test Author", 100);
			var loan = Loan.Create(Guid.NewGuid(), book.BookId, borrowedAt);

			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan, book);

			loan.Return(returnedAt);

			// Act
			var pace = ReadingPacePolicy.CalculateLoanPace(loan);

			// Assert
			pace.Should().NotBeNull();
			pace.Should().Be(100.0); // 100 pages / max(1, 0.166...) = 100 pages / 1 day
		}

		[Fact]
		public void CalculateLoanPace_WithUnreturnedLoan_ShouldReturnNull()
		{
			// Arrange
			var book = Book.Create("ISBN-123", "Test Book", "Test Author", 300);
			var loan = Loan.Create(Guid.NewGuid(), book.BookId, DateTime.UtcNow);

			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan, book);

			// Act
			var pace = ReadingPacePolicy.CalculateLoanPace(loan);

			// Assert
			pace.Should().BeNull();
		}
	}

	public class CalculatePaceTests
	{
		[Fact]
		public void CalculatePace_WithValidDates_ShouldCalculateCorrectly()
		{
			// Arrange
			var pageCount = 450;
			var borrowedAt = new DateTime(2024, 1, 1);
			var returnedAt = new DateTime(2024, 1, 16); // 15 days

			// Act
			var pace = ReadingPacePolicy.CalculatePace(pageCount, borrowedAt, returnedAt);

			// Assert
			pace.Should().Be(30.0); // 450 pages / 15 days
		}

		[Fact]
		public void CalculatePace_WithSameDayReturn_ShouldUseMinimumOneDayForCalculation()
		{
			// Arrange
			var pageCount = 200;
			var borrowedAt = new DateTime(2024, 1, 1, 9, 0, 0);
			var returnedAt = new DateTime(2024, 1, 1, 18, 0, 0); // Same day

			// Act
			var pace = ReadingPacePolicy.CalculatePace(pageCount, borrowedAt, returnedAt);

			// Assert
			pace.Should().Be(200.0); // 200 pages / 1 day
		}

		[Fact]
		public void CalculatePace_WithReturnBeforeBorrow_ShouldThrowArgumentException()
		{
			// Arrange
			var pageCount = 200;
			var borrowedAt = new DateTime(2024, 1, 10);
			var returnedAt = new DateTime(2024, 1, 5);

			// Act
			var act = () => ReadingPacePolicy.CalculatePace(pageCount, borrowedAt, returnedAt);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("Return date cannot be before borrow date");
		}
	}

	public class CalculateUserPaceTests
	{
		[Fact]
		public void CalculateUserPace_WithMultipleReturnedLoans_ShouldCalculateAveragePace()
		{
			// Arrange
			var book1 = Book.Create("ISBN-1", "Book 1", "Author 1", 300);
			var book2 = Book.Create("ISBN-2", "Book 2", "Author 2", 600);
			var book3 = Book.Create("ISBN-3", "Book 3", "Author 3", 150);

			var loan1 = Loan.Create(Guid.NewGuid(), book1.BookId, new DateTime(2024, 1, 1));
			var loan2 = Loan.Create(Guid.NewGuid(), book2.BookId, new DateTime(2024, 1, 5));
			var loan3 = Loan.Create(Guid.NewGuid(), book3.BookId, new DateTime(2024, 1, 10));

			// Set book navigation properties
			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan1, book1);
			bookProperty!.SetValue(loan2, book2);
			bookProperty!.SetValue(loan3, book3);

			// Return loans
			loan1.Return(new DateTime(2024, 1, 11)); // 10 days, 300 pages -> 30 pages/day
			loan2.Return(new DateTime(2024, 1, 25)); // 20 days, 600 pages -> 30 pages/day
			loan3.Return(new DateTime(2024, 1, 20)); // 10 days, 150 pages -> 15 pages/day

			var loans = new[] { loan1, loan2, loan3 };

			// Act
			var result = ReadingPacePolicy.CalculateUserPace(loans);

			// Assert
			result.Should().NotBeNull();
			result.AggregatePagesPerDay.Should().Be(25.0); // (30 + 30 + 15) / 3
			result.LoanPaces.Should().HaveCount(3);

			result.LoanPaces[0].PagesPerDay.Should().Be(30.0);
			result.LoanPaces[0].BookTitle.Should().Be("Book 1");
			result.LoanPaces[0].Pages.Should().Be(300);
			result.LoanPaces[0].Days.Should().Be(10);

			result.LoanPaces[1].PagesPerDay.Should().Be(30.0);
			result.LoanPaces[1].BookTitle.Should().Be("Book 2");
			result.LoanPaces[1].Pages.Should().Be(600);
			result.LoanPaces[1].Days.Should().Be(20);

			result.LoanPaces[2].PagesPerDay.Should().Be(15.0);
			result.LoanPaces[2].BookTitle.Should().Be("Book 3");
			result.LoanPaces[2].Pages.Should().Be(150);
			result.LoanPaces[2].Days.Should().Be(10);
		}

		[Fact]
		public void CalculateUserPace_WithNoReturnedLoans_ShouldReturnZeroPace()
		{
			// Arrange
			var book = Book.Create("ISBN-1", "Book 1", "Author 1", 300);
			var loan = Loan.Create(Guid.NewGuid(), book.BookId, DateTime.UtcNow);

			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan, book);

			var loans = new[] { loan };

			// Act
			var result = ReadingPacePolicy.CalculateUserPace(loans);

			// Assert
			result.Should().NotBeNull();
			result.AggregatePagesPerDay.Should().Be(0);
			result.LoanPaces.Should().BeEmpty();
		}

		[Fact]
		public void CalculateUserPace_WithEmptyLoanList_ShouldReturnZeroPace()
		{
			// Arrange
			var loans = Array.Empty<Loan>();

			// Act
			var result = ReadingPacePolicy.CalculateUserPace(loans);

			// Assert
			result.Should().NotBeNull();
			result.AggregatePagesPerDay.Should().Be(0);
			result.LoanPaces.Should().BeEmpty();
		}

		[Fact]
		public void CalculateUserPace_WithMixedReturnedAndUnreturnedLoans_ShouldOnlyConsiderReturned()
		{
			// Arrange
			var book1 = Book.Create("ISBN-1", "Book 1", "Author 1", 200);
			var book2 = Book.Create("ISBN-2", "Book 2", "Author 2", 400);
			var book3 = Book.Create("ISBN-3", "Book 3", "Author 3", 600);

			var loan1 = Loan.Create(Guid.NewGuid(), book1.BookId, new DateTime(2024, 1, 1));
			var loan2 = Loan.Create(Guid.NewGuid(), book2.BookId, new DateTime(2024, 1, 5));
			var loan3 = Loan.Create(Guid.NewGuid(), book3.BookId, new DateTime(2024, 1, 10));

			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan1, book1);
			bookProperty!.SetValue(loan2, book2);
			bookProperty!.SetValue(loan3, book3);

			// Only return loan1 and loan2
			loan1.Return(new DateTime(2024, 1, 11)); // 10 days, 200 pages -> 20 pages/day
			loan2.Return(new DateTime(2024, 1, 15)); // 10 days, 400 pages -> 40 pages/day
													 // loan3 is not returned

			var loans = new[] { loan1, loan2, loan3 };

			// Act
			var result = ReadingPacePolicy.CalculateUserPace(loans);

			// Assert
			result.Should().NotBeNull();
			result.AggregatePagesPerDay.Should().Be(30.0); // (20 + 40) / 2
			result.LoanPaces.Should().HaveCount(2);
			result.LoanPaces.Should().NotContain(lp => lp.BookTitle == "Book 3");
		}

		[Fact]
		public void CalculateUserPace_WithSingleReturnedLoan_ShouldCalculateCorrectly()
		{
			// Arrange
			var book = Book.Create("ISBN-1", "Book 1", "Author 1", 500);
			var loan = Loan.Create(Guid.NewGuid(), book.BookId, new DateTime(2024, 1, 1));

			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan, book);

			loan.Return(new DateTime(2024, 1, 26)); // 25 days

			var loans = new[] { loan };

			// Act
			var result = ReadingPacePolicy.CalculateUserPace(loans);

			// Assert
			result.Should().NotBeNull();
			result.AggregatePagesPerDay.Should().Be(20.0); // 500 / 25
			result.LoanPaces.Should().HaveCount(1);
			result.LoanPaces[0].PagesPerDay.Should().Be(20.0);
		}

		[Fact]
		public void CalculateUserPace_ShouldIncludeAllLoanPaceDetails()
		{
			// Arrange
			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 300);
			var userId = Guid.NewGuid();
			var borrowedAt = new DateTime(2024, 1, 1);
			var returnedAt = new DateTime(2024, 1, 11);

			var loan = Loan.Create(userId, book.BookId, borrowedAt);
			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan, book);
			loan.Return(returnedAt);

			var loans = new[] { loan };

			// Act
			var result = ReadingPacePolicy.CalculateUserPace(loans);

			// Assert
			result.LoanPaces.Should().HaveCount(1);
			var loanPace = result.LoanPaces[0];

			loanPace.LoanId.Should().Be(loan.LoanId);
			loanPace.BookId.Should().Be(book.BookId);
			loanPace.BookTitle.Should().Be("Test Book");
			loanPace.Pages.Should().Be(300);
			loanPace.Days.Should().Be(10);
			loanPace.PagesPerDay.Should().Be(30.0);
			loanPace.BorrowedAt.Should().Be(borrowedAt);
			loanPace.ReturnedAt.Should().Be(returnedAt);
		}
	}
}
