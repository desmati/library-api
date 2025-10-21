// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using Library.Domain.Entities;

namespace Library.UnitTests.Domain;

public class DomainEntityTests
{
	public class BookTests
	{
		[Fact]
		public void Book_Create_WithValidData_ShouldSucceed()
		{
			// Arrange
			var isbn = "978-0-13-468599-1";
			var title = "Clean Architecture";
			var author = "Robert C. Martin";
			var pageCount = 432;
			var publishedYear = 2017;

			// Act
			var book = Book.Create(isbn, title, author, pageCount, publishedYear);

			// Assert
			book.Should().NotBeNull();
			book.BookId.Should().NotBeEmpty();
			book.Isbn.Should().Be(isbn);
			book.Title.Should().Be(title);
			book.Author.Should().Be(author);
			book.PageCount.Should().Be(pageCount);
			book.PublishedYear.Should().Be(publishedYear);
		}

		[Fact]
		public void Book_Create_WithoutPublishedYear_ShouldSucceed()
		{
			// Arrange
			var isbn = "978-0-13-468599-1";
			var title = "Clean Architecture";
			var author = "Robert C. Martin";
			var pageCount = 432;

			// Act
			var book = Book.Create(isbn, title, author, pageCount);

			// Assert
			book.Should().NotBeNull();
			book.PublishedYear.Should().BeNull();
		}

		[Fact]
		public void Book_Constructor_WithEmptyGuid_ShouldThrowArgumentException()
		{
			// Arrange
			var bookId = Guid.Empty;
			var isbn = "978-0-13-468599-1";
			var title = "Clean Architecture";
			var author = "Robert C. Martin";
			var pageCount = 432;

			// Act
			var act = () => new Book(bookId, isbn, title, author, pageCount);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("BookId cannot be empty*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Book_Constructor_WithInvalidIsbn_ShouldThrowArgumentException(string? invalidIsbn)
		{
			// Arrange
			var bookId = Guid.NewGuid();
			var title = "Clean Architecture";
			var author = "Robert C. Martin";
			var pageCount = 432;

			// Act
			var act = () => new Book(bookId, invalidIsbn!, title, author, pageCount);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("ISBN cannot be empty*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Book_Constructor_WithInvalidTitle_ShouldThrowArgumentException(string? invalidTitle)
		{
			// Arrange
			var bookId = Guid.NewGuid();
			var isbn = "978-0-13-468599-1";
			var author = "Robert C. Martin";
			var pageCount = 432;

			// Act
			var act = () => new Book(bookId, isbn, invalidTitle!, author, pageCount);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("Title cannot be empty*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Book_Constructor_WithInvalidAuthor_ShouldThrowArgumentException(string? invalidAuthor)
		{
			// Arrange
			var bookId = Guid.NewGuid();
			var isbn = "978-0-13-468599-1";
			var title = "Clean Architecture";
			var pageCount = 432;

			// Act
			var act = () => new Book(bookId, isbn, title, invalidAuthor!, pageCount);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("Author cannot be empty*");
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(-100)]
		public void Book_Constructor_WithInvalidPageCount_ShouldThrowArgumentException(int invalidPageCount)
		{
			// Arrange
			var bookId = Guid.NewGuid();
			var isbn = "978-0-13-468599-1";
			var title = "Clean Architecture";
			var author = "Robert C. Martin";

			// Act
			var act = () => new Book(bookId, isbn, title, author, invalidPageCount);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("Page count must be positive*");
		}
	}

	public class UserTests
	{
		[Fact]
		public void User_Create_WithValidData_ShouldSucceed()
		{
			// Arrange
			var fullName = "John Doe";
			var registeredAt = DateTime.UtcNow;

			// Act
			var user = User.Create(fullName, registeredAt);

			// Assert
			user.Should().NotBeNull();
			user.UserId.Should().NotBeEmpty();
			user.FullName.Should().Be(fullName);
			user.RegisteredAt.Should().Be(registeredAt);
		}

		[Fact]
		public void User_Create_WithoutRegisteredAt_ShouldUseCurrentDateTime()
		{
			// Arrange
			var fullName = "John Doe";
			var before = DateTime.UtcNow;

			// Act
			var user = User.Create(fullName);
			var after = DateTime.UtcNow;

			// Assert
			user.Should().NotBeNull();
			user.RegisteredAt.Should().BeOnOrAfter(before);
			user.RegisteredAt.Should().BeOnOrBefore(after);
		}

		[Fact]
		public void User_Constructor_WithEmptyGuid_ShouldThrowArgumentException()
		{
			// Arrange
			var userId = Guid.Empty;
			var fullName = "John Doe";
			var registeredAt = DateTime.UtcNow;

			// Act
			var act = () => new User(userId, fullName, registeredAt);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("UserId cannot be empty*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void User_Constructor_WithInvalidFullName_ShouldThrowArgumentException(string? invalidFullName)
		{
			// Arrange
			var userId = Guid.NewGuid();
			var registeredAt = DateTime.UtcNow;

			// Act
			var act = () => new User(userId, invalidFullName!, registeredAt);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("FullName cannot be empty*");
		}
	}

	public class LoanTests
	{
		[Fact]
		public void Loan_Create_WithValidData_ShouldSucceed()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var bookId = Guid.NewGuid();
			var borrowedAt = DateTime.UtcNow;

			// Act
			var loan = Loan.Create(userId, bookId, borrowedAt);

			// Assert
			loan.Should().NotBeNull();
			loan.LoanId.Should().NotBeEmpty();
			loan.UserId.Should().Be(userId);
			loan.BookId.Should().Be(bookId);
			loan.BorrowedAt.Should().Be(borrowedAt);
			loan.ReturnedAt.Should().BeNull();
			loan.IsReturned.Should().BeFalse();
		}

		[Fact]
		public void Loan_Constructor_WithEmptyLoanId_ShouldThrowArgumentException()
		{
			// Arrange
			var loanId = Guid.Empty;
			var userId = Guid.NewGuid();
			var bookId = Guid.NewGuid();
			var borrowedAt = DateTime.UtcNow;

			// Act
			var act = () => new Loan(loanId, bookId, userId, borrowedAt);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("LoanId cannot be empty*");
		}

		[Fact]
		public void Loan_Constructor_WithEmptyBookId_ShouldThrowArgumentException()
		{
			// Arrange
			var loanId = Guid.NewGuid();
			var userId = Guid.NewGuid();
			var bookId = Guid.Empty;
			var borrowedAt = DateTime.UtcNow;

			// Act
			var act = () => new Loan(loanId, bookId, userId, borrowedAt);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("BookId cannot be empty*");
		}

		[Fact]
		public void Loan_Constructor_WithEmptyUserId_ShouldThrowArgumentException()
		{
			// Arrange
			var loanId = Guid.NewGuid();
			var userId = Guid.Empty;
			var bookId = Guid.NewGuid();
			var borrowedAt = DateTime.UtcNow;

			// Act
			var act = () => new Loan(loanId, bookId, userId, borrowedAt);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("UserId cannot be empty*");
		}

		[Fact]
		public void Loan_Return_WithValidDate_ShouldSucceed()
		{
			// Arrange
			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-5));
			var returnedAt = DateTime.UtcNow;

			// Act
			loan.Return(returnedAt);

			// Assert
			loan.ReturnedAt.Should().Be(returnedAt);
			loan.IsReturned.Should().BeTrue();
		}

		[Fact]
		public void Loan_Return_WhenAlreadyReturned_ShouldThrowInvalidOperationException()
		{
			// Arrange
			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-5));
			loan.Return(DateTime.UtcNow);

			// Act
			var act = () => loan.Return(DateTime.UtcNow);

			// Assert
			act.Should().Throw<InvalidOperationException>()
				.WithMessage("Loan has already been returned");
		}

		[Fact]
		public void Loan_Return_WithDateBeforeBorrow_ShouldThrowArgumentException()
		{
			// Arrange
			var borrowedAt = DateTime.UtcNow;
			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), borrowedAt);
			var returnedAt = borrowedAt.AddDays(-1);

			// Act
			var act = () => loan.Return(returnedAt);

			// Assert
			act.Should().Throw<ArgumentException>()
				.WithMessage("Return date cannot be before borrow date*");
		}

		[Fact]
		public void Loan_IsReturned_WhenNotReturned_ShouldBeFalse()
		{
			// Arrange
			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

			// Act & Assert
			loan.IsReturned.Should().BeFalse();
		}

		[Fact]
		public void Loan_IsReturned_WhenReturned_ShouldBeTrue()
		{
			// Arrange
			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-5));
			loan.Return(DateTime.UtcNow);

			// Act & Assert
			loan.IsReturned.Should().BeTrue();
		}
	}
}
