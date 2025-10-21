// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using Library.Domain.Entities;
using Library.Infrastructure.Data;
using Library.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace Library.IntegrationTests.Infrastructure;

public class RepositoryTests : IAsyncLifetime
{
	private PostgreSqlContainer _postgresContainer = null!;
	private LibraryDbContext _context = null!;

	public async Task InitializeAsync()
	{
		_postgresContainer = new PostgreSqlBuilder()
			.WithImage("postgres:16-alpine")
			.WithDatabase("library_test")
			.WithUsername("test_user")
			.WithPassword("test_password")
			.Build();

		await _postgresContainer.StartAsync();

		var options = new DbContextOptionsBuilder<LibraryDbContext>()
			.UseNpgsql(_postgresContainer.GetConnectionString())
			.Options;

		_context = new LibraryDbContext(options);
		await _context.Database.EnsureCreatedAsync();
	}

	public async Task DisposeAsync()
	{
		await _context.DisposeAsync();
		await _postgresContainer.DisposeAsync();
	}

	public class BookRepositoryTests : RepositoryTests
	{
		[Fact]
		public async Task AddAsync_WithValidBook_ShouldPersistToDatabase()
		{
			// Arrange
			var repository = new BookRepository(_context);
			var book = Book.Create("978-0-13-468599-1", "Clean Architecture", "Robert C. Martin", 432, 2017);

			// Act
			await repository.AddAsync(book);

			// Assert
			var retrieved = await repository.GetByIdAsync(book.BookId);
			retrieved.Should().NotBeNull();
			retrieved!.Isbn.Should().Be("978-0-13-468599-1");
			retrieved.Title.Should().Be("Clean Architecture");
			retrieved.Author.Should().Be("Robert C. Martin");
			retrieved.PageCount.Should().Be(432);
			retrieved.PublishedYear.Should().Be(2017);
		}

		[Fact]
		public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
		{
			// Arrange
			var repository = new BookRepository(_context);
			var nonExistentId = Guid.NewGuid();

			// Act
			var result = await repository.GetByIdAsync(nonExistentId);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetAllAsync_ShouldReturnAllBooksOrderedByTitle()
		{
			// Arrange
			var repository = new BookRepository(_context);
			var book1 = Book.Create("ISBN-1", "Zebra Book", "Author A", 100);
			var book2 = Book.Create("ISBN-2", "Apple Book", "Author B", 200);
			var book3 = Book.Create("ISBN-3", "Mango Book", "Author C", 300);

			await repository.AddAsync(book1);
			await repository.AddAsync(book2);
			await repository.AddAsync(book3);

			// Act
			var results = await repository.GetAllAsync();
			var bookList = results.ToList();

			// Assert
			bookList.Should().HaveCount(3);
			bookList[0].Title.Should().Be("Apple Book");
			bookList[1].Title.Should().Be("Mango Book");
			bookList[2].Title.Should().Be("Zebra Book");
		}

		[Fact]
		public async Task ExistsAsync_WithExistingBook_ShouldReturnTrue()
		{
			// Arrange
			var repository = new BookRepository(_context);
			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 100);
			await repository.AddAsync(book);

			// Act
			var exists = await repository.ExistsAsync(book.BookId);

			// Assert
			exists.Should().BeTrue();
		}

		[Fact]
		public async Task ExistsAsync_WithNonExistentBook_ShouldReturnFalse()
		{
			// Arrange
			var repository = new BookRepository(_context);
			var nonExistentId = Guid.NewGuid();

			// Act
			var exists = await repository.ExistsAsync(nonExistentId);

			// Assert
			exists.Should().BeFalse();
		}

		[Fact]
		public async Task AddAsync_WithCancellationToken_ShouldRespectToken()
		{
			// Arrange
			var repository = new BookRepository(_context);
			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 100);
			using var cts = new CancellationTokenSource();

			// Act
			await repository.AddAsync(book, cts.Token);

			// Assert
			var retrieved = await repository.GetByIdAsync(book.BookId, cts.Token);
			retrieved.Should().NotBeNull();
		}
	}

	public class UserRepositoryTests : RepositoryTests
	{
		[Fact]
		public async Task AddAsync_WithValidUser_ShouldPersistToDatabase()
		{
			// Arrange
			var repository = new UserRepository(_context);
			var registeredAt = DateTime.UtcNow;
			var user = User.Create("John Doe", registeredAt);

			// Act
			await repository.AddAsync(user);

			// Assert
			var retrieved = await repository.GetByIdAsync(user.UserId);
			retrieved.Should().NotBeNull();
			retrieved!.FullName.Should().Be("John Doe");
			retrieved.RegisteredAt.Should().BeCloseTo(registeredAt, TimeSpan.FromSeconds(1));
		}

		[Fact]
		public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
		{
			// Arrange
			var repository = new UserRepository(_context);
			var nonExistentId = Guid.NewGuid();

			// Act
			var result = await repository.GetByIdAsync(nonExistentId);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetAllAsync_ShouldReturnAllUsersOrderedByFullName()
		{
			// Arrange
			var repository = new UserRepository(_context);
			var user1 = User.Create("Zoe Smith", DateTime.UtcNow);
			var user2 = User.Create("Alice Johnson", DateTime.UtcNow);
			var user3 = User.Create("Mike Brown", DateTime.UtcNow);

			await repository.AddAsync(user1);
			await repository.AddAsync(user2);
			await repository.AddAsync(user3);

			// Act
			var results = await repository.GetAllAsync();
			var userList = results.ToList();

			// Assert
			userList.Should().HaveCount(3);
			userList[0].FullName.Should().Be("Alice Johnson");
			userList[1].FullName.Should().Be("Mike Brown");
			userList[2].FullName.Should().Be("Zoe Smith");
		}

		[Fact]
		public async Task ExistsAsync_WithExistingUser_ShouldReturnTrue()
		{
			// Arrange
			var repository = new UserRepository(_context);
			var user = User.Create("Test User", DateTime.UtcNow);
			await repository.AddAsync(user);

			// Act
			var exists = await repository.ExistsAsync(user.UserId);

			// Assert
			exists.Should().BeTrue();
		}

		[Fact]
		public async Task ExistsAsync_WithNonExistentUser_ShouldReturnFalse()
		{
			// Arrange
			var repository = new UserRepository(_context);
			var nonExistentId = Guid.NewGuid();

			// Act
			var exists = await repository.ExistsAsync(nonExistentId);

			// Assert
			exists.Should().BeFalse();
		}
	}

	public class LoanRepositoryTests : RepositoryTests
	{
		[Fact]
		public async Task AddAsync_WithValidLoan_ShouldPersistToDatabase()
		{
			// Arrange
			var bookRepository = new BookRepository(_context);
			var userRepository = new UserRepository(_context);
			var loanRepository = new LoanRepository(_context);

			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 300);
			var user = User.Create("Test User", DateTime.UtcNow);
			await bookRepository.AddAsync(book);
			await userRepository.AddAsync(user);

			var borrowedAt = DateTime.UtcNow.AddDays(-5);
			var loan = Loan.Create(user.UserId, book.BookId, borrowedAt);

			// Act
			await loanRepository.AddAsync(loan);

			// Assert
			var retrieved = await loanRepository.GetByIdAsync(loan.LoanId);
			retrieved.Should().NotBeNull();
			retrieved!.UserId.Should().Be(user.UserId);
			retrieved.BookId.Should().Be(book.BookId);
			retrieved.BorrowedAt.Should().BeCloseTo(borrowedAt, TimeSpan.FromSeconds(1));
			retrieved.ReturnedAt.Should().BeNull();
			retrieved.Book.Should().NotBeNull();
			retrieved.User.Should().NotBeNull();
		}

		[Fact]
		public async Task UpdateAsync_WithReturnedLoan_ShouldPersistChanges()
		{
			// Arrange
			var bookRepository = new BookRepository(_context);
			var userRepository = new UserRepository(_context);
			var loanRepository = new LoanRepository(_context);

			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 300);
			var user = User.Create("Test User", DateTime.UtcNow);
			await bookRepository.AddAsync(book);
			await userRepository.AddAsync(user);

			var loan = Loan.Create(user.UserId, book.BookId, DateTime.UtcNow.AddDays(-5));
			await loanRepository.AddAsync(loan);

			var returnedAt = DateTime.UtcNow;
			loan.Return(returnedAt);

			// Act
			await loanRepository.UpdateAsync(loan);

			// Assert
			var retrieved = await loanRepository.GetByIdAsync(loan.LoanId);
			retrieved.Should().NotBeNull();
			retrieved!.IsReturned.Should().BeTrue();
			retrieved.ReturnedAt.Should().NotBeNull();
			retrieved.ReturnedAt!.Value.Should().BeCloseTo(returnedAt, TimeSpan.FromSeconds(1));
		}

		[Fact]
		public async Task GetActiveLoanAsync_WithActiveLoan_ShouldReturnLoan()
		{
			// Arrange
			var bookRepository = new BookRepository(_context);
			var userRepository = new UserRepository(_context);
			var loanRepository = new LoanRepository(_context);

			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 300);
			var user = User.Create("Test User", DateTime.UtcNow);
			await bookRepository.AddAsync(book);
			await userRepository.AddAsync(user);

			var loan = Loan.Create(user.UserId, book.BookId, DateTime.UtcNow.AddDays(-5));
			await loanRepository.AddAsync(loan);

			// Act
			var retrieved = await loanRepository.GetActiveLoanAsync(user.UserId, book.BookId);

			// Assert
			retrieved.Should().NotBeNull();
			retrieved!.LoanId.Should().Be(loan.LoanId);
			retrieved.IsReturned.Should().BeFalse();
		}

		[Fact]
		public async Task GetActiveLoanAsync_WithReturnedLoan_ShouldReturnNull()
		{
			// Arrange
			var bookRepository = new BookRepository(_context);
			var userRepository = new UserRepository(_context);
			var loanRepository = new LoanRepository(_context);

			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 300);
			var user = User.Create("Test User", DateTime.UtcNow);
			await bookRepository.AddAsync(book);
			await userRepository.AddAsync(user);

			var loan = Loan.Create(user.UserId, book.BookId, DateTime.UtcNow.AddDays(-5));
			loan.Return(DateTime.UtcNow);
			await loanRepository.AddAsync(loan);

			// Act
			var retrieved = await loanRepository.GetActiveLoanAsync(user.UserId, book.BookId);

			// Assert
			retrieved.Should().BeNull();
		}

		[Fact]
		public async Task GetLoansByUserAsync_ShouldReturnAllLoansForUser()
		{
			// Arrange
			var bookRepository = new BookRepository(_context);
			var userRepository = new UserRepository(_context);
			var loanRepository = new LoanRepository(_context);

			var book1 = Book.Create("ISBN-1", "Book 1", "Author 1", 300);
			var book2 = Book.Create("ISBN-2", "Book 2", "Author 2", 400);
			var user = User.Create("Test User", DateTime.UtcNow);
			var otherUser = User.Create("Other User", DateTime.UtcNow);

			await bookRepository.AddAsync(book1);
			await bookRepository.AddAsync(book2);
			await userRepository.AddAsync(user);
			await userRepository.AddAsync(otherUser);

			var loan1 = Loan.Create(user.UserId, book1.BookId, DateTime.UtcNow.AddDays(-10));
			var loan2 = Loan.Create(user.UserId, book2.BookId, DateTime.UtcNow.AddDays(-5));
			var otherLoan = Loan.Create(otherUser.UserId, book1.BookId, DateTime.UtcNow.AddDays(-3));

			await loanRepository.AddAsync(loan1);
			await loanRepository.AddAsync(loan2);
			await loanRepository.AddAsync(otherLoan);

			// Act
			var results = await loanRepository.GetLoansByUserAsync(user.UserId);
			var loanList = results.ToList();

			// Assert
			loanList.Should().HaveCount(2);
			loanList.Should().Contain(l => l.LoanId == loan1.LoanId);
			loanList.Should().Contain(l => l.LoanId == loan2.LoanId);
			loanList.Should().NotContain(l => l.LoanId == otherLoan.LoanId);
			loanList[0].BorrowedAt.Should().BeAfter(loanList[1].BorrowedAt); // Descending order
		}

		[Fact]
		public async Task GetLoansByUserAsync_WithDateRange_ShouldFilterLoans()
		{
			// Arrange
			var bookRepository = new BookRepository(_context);
			var userRepository = new UserRepository(_context);
			var loanRepository = new LoanRepository(_context);

			var book1 = Book.Create("ISBN-1", "Book 1", "Author 1", 300);
			var book2 = Book.Create("ISBN-2", "Book 2", "Author 2", 400);
			var book3 = Book.Create("ISBN-3", "Book 3", "Author 3", 500);
			var user = User.Create("Test User", DateTime.UtcNow);

			await bookRepository.AddAsync(book1);
			await bookRepository.AddAsync(book2);
			await bookRepository.AddAsync(book3);
			await userRepository.AddAsync(user);

			var loan1 = Loan.Create(user.UserId, book1.BookId, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			var loan2 = Loan.Create(user.UserId, book2.BookId, new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
			var loan3 = Loan.Create(user.UserId, book3.BookId, new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));

			await loanRepository.AddAsync(loan1);
			await loanRepository.AddAsync(loan2);
			await loanRepository.AddAsync(loan3);

			// Act
			var start = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
			var end = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc);
			var results = await loanRepository.GetLoansByUserAsync(user.UserId, start, end);
			var loanList = results.ToList();

			// Assert
			loanList.Should().HaveCount(1);
			loanList[0].LoanId.Should().Be(loan2.LoanId);
		}

		[Fact]
		public async Task GetLoansByBookAsync_ShouldReturnAllLoansForBook()
		{
			// Arrange
			var bookRepository = new BookRepository(_context);
			var userRepository = new UserRepository(_context);
			var loanRepository = new LoanRepository(_context);

			var book = Book.Create("ISBN-1", "Test Book", "Test Author", 300);
			var user1 = User.Create("User 1", DateTime.UtcNow);
			var user2 = User.Create("User 2", DateTime.UtcNow);

			await bookRepository.AddAsync(book);
			await userRepository.AddAsync(user1);
			await userRepository.AddAsync(user2);

			var loan1 = Loan.Create(user1.UserId, book.BookId, DateTime.UtcNow.AddDays(-10));
			var loan2 = Loan.Create(user2.UserId, book.BookId, DateTime.UtcNow.AddDays(-5));

			await loanRepository.AddAsync(loan1);
			await loanRepository.AddAsync(loan2);

			// Act
			var results = await loanRepository.GetLoansByBookAsync(book.BookId);
			var loanList = results.ToList();

			// Assert
			loanList.Should().HaveCount(2);
			loanList.Should().Contain(l => l.LoanId == loan1.LoanId);
			loanList.Should().Contain(l => l.LoanId == loan2.LoanId);
		}
	}
}
