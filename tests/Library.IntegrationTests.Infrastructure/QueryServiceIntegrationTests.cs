// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Bogus;

using FluentAssertions;

using Library.Domain.Entities;
using Library.Infrastructure.Data;
using Library.Infrastructure.Queries;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace Library.IntegrationTests.Infrastructure;

public class QueryServiceIntegrationTests : IAsyncLifetime
{
	private PostgreSqlContainer _postgresContainer = null!;
	private LibraryDbContext _context = null!;
	private QueryService _queryService = null!;

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

		_queryService = new QueryService(_context);
	}

	public async Task DisposeAsync()
	{
		await _context.DisposeAsync();
		await _postgresContainer.DisposeAsync();
	}

	[Fact]
	public async Task GetMostBorrowedBooksAsync_WithMultipleLoans_ShouldReturnOrderedByBorrowCount()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();

		// Create loans: Book1 has 5 loans, Book2 has 3 loans, Book3 has 2 loans
		for (var i = 0; i < 5; i++)
		{
			var loan = Loan.Create(users[i % users.Count].UserId, books[0].BookId, DateTime.UtcNow.AddDays(-10 + i));
			await _context.Loans.AddAsync(loan);
		}
		for (var i = 0; i < 3; i++)
		{
			var loan = Loan.Create(users[i % users.Count].UserId, books[1].BookId, DateTime.UtcNow.AddDays(-8 + i));
			await _context.Loans.AddAsync(loan);
		}
		for (var i = 0; i < 2; i++)
		{
			var loan = Loan.Create(users[i % users.Count].UserId, books[2].BookId, DateTime.UtcNow.AddDays(-6 + i));
			await _context.Loans.AddAsync(loan);
		}
		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetMostBorrowedBooksAsync(3, null, null);

		// Assert
		results.Should().HaveCount(3);
		results[0].BookId.Should().Be(books[0].BookId);
		results[0].Count.Should().Be(5);
		results[0].Title.Should().Be(books[0].Title);
		results[0].Author.Should().Be(books[0].Author);
		results[0].PageCount.Should().Be(books[0].PageCount);

		results[1].BookId.Should().Be(books[1].BookId);
		results[1].Count.Should().Be(3);

		results[2].BookId.Should().Be(books[2].BookId);
		results[2].Count.Should().Be(2);
	}

	[Fact]
	public async Task GetMostBorrowedBooksAsync_WithDateRange_ShouldFilterCorrectly()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();

		var startDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2024, 8, 31, 0, 0, 0, DateTimeKind.Utc);

		// Loans before range
		var loan1 = Loan.Create(users[0].UserId, books[0].BookId, new DateTime(2024, 5, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan1);

		// Loans within range
		var loan2 = Loan.Create(users[0].UserId, books[1].BookId, new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
		var loan3 = Loan.Create(users[1].UserId, books[1].BookId, new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan2);
		await _context.Loans.AddAsync(loan3);

		// Loans after range
		var loan4 = Loan.Create(users[0].UserId, books[2].BookId, new DateTime(2024, 9, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan4);

		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetMostBorrowedBooksAsync(10, startDate, endDate);

		// Assert
		results.Should().HaveCount(1);
		results[0].BookId.Should().Be(books[1].BookId);
		results[0].Count.Should().Be(2);
	}

	[Fact]
	public async Task GetMostBorrowedBooksAsync_WithNoLoans_ShouldReturnEmptyList()
	{
		// Arrange
		await SeedTestDataAsync();

		// Act
		var results = await _queryService.GetMostBorrowedBooksAsync(10, null, null);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public async Task GetTopBorrowersAsync_WithMultipleLoans_ShouldReturnOrderedByLoanCount()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();

		// User1 has 4 loans, User2 has 2 loans, User3 has 1 loan
		for (var i = 0; i < 4; i++)
		{
			var loan = Loan.Create(users[0].UserId, books[i % books.Count].BookId, DateTime.UtcNow.AddDays(-10 + i));
			await _context.Loans.AddAsync(loan);
		}
		for (var i = 0; i < 2; i++)
		{
			var loan = Loan.Create(users[1].UserId, books[i].BookId, DateTime.UtcNow.AddDays(-8 + i));
			await _context.Loans.AddAsync(loan);
		}
		var singleLoan = Loan.Create(users[2].UserId, books[0].BookId, DateTime.UtcNow.AddDays(-5));
		await _context.Loans.AddAsync(singleLoan);

		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetTopBorrowersAsync(3, null, null);

		// Assert
		results.Should().HaveCount(3);
		results[0].UserId.Should().Be(users[0].UserId);
		results[0].FullName.Should().Be(users[0].FullName);
		results[0].Count.Should().Be(4);

		results[1].UserId.Should().Be(users[1].UserId);
		results[1].Count.Should().Be(2);

		results[2].UserId.Should().Be(users[2].UserId);
		results[2].Count.Should().Be(1);
	}

	[Fact]
	public async Task GetTopBorrowersAsync_WithDateRange_ShouldFilterCorrectly()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();

		var startDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2024, 8, 31, 0, 0, 0, DateTimeKind.Utc);

		// Loans before range
		var loan1 = Loan.Create(users[0].UserId, books[0].BookId, new DateTime(2024, 5, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan1);

		// Loans within range
		var loan2 = Loan.Create(users[1].UserId, books[0].BookId, new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
		var loan3 = Loan.Create(users[1].UserId, books[1].BookId, new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc));
		var loan4 = Loan.Create(users[2].UserId, books[0].BookId, new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan2);
		await _context.Loans.AddAsync(loan3);
		await _context.Loans.AddAsync(loan4);

		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetTopBorrowersAsync(10, startDate, endDate);

		// Assert
		results.Should().HaveCount(2);
		results[0].UserId.Should().Be(users[1].UserId);
		results[0].Count.Should().Be(2);
		results[1].UserId.Should().Be(users[2].UserId);
		results[1].Count.Should().Be(1);
	}

	[Fact]
	public async Task GetAlsoBorrowedBooksAsync_WithCoBorrowedBooks_ShouldReturnRelatedBooks()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();
		var targetBook = books[0];

		// User1 and User2 borrowed targetBook
		var loan1 = Loan.Create(users[0].UserId, targetBook.BookId, DateTime.UtcNow.AddDays(-10));
		var loan2 = Loan.Create(users[1].UserId, targetBook.BookId, DateTime.UtcNow.AddDays(-9));
		await _context.Loans.AddAsync(loan1);
		await _context.Loans.AddAsync(loan2);

		// User1 also borrowed books[1] and books[2]
		var loan3 = Loan.Create(users[0].UserId, books[1].BookId, DateTime.UtcNow.AddDays(-8));
		var loan4 = Loan.Create(users[0].UserId, books[2].BookId, DateTime.UtcNow.AddDays(-7));
		await _context.Loans.AddAsync(loan3);
		await _context.Loans.AddAsync(loan4);

		// User2 also borrowed books[1] (so books[1] has 2 co-borrows)
		var loan5 = Loan.Create(users[1].UserId, books[1].BookId, DateTime.UtcNow.AddDays(-6));
		await _context.Loans.AddAsync(loan5);

		// User3 borrowed books[3] (not related to targetBook)
		var loan6 = Loan.Create(users[2].UserId, books[3].BookId, DateTime.UtcNow.AddDays(-5));
		await _context.Loans.AddAsync(loan6);

		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetAlsoBorrowedBooksAsync(targetBook.BookId, 10, null, null);

		// Assert
		results.Should().HaveCount(2);
		results[0].BookId.Should().Be(books[1].BookId);
		results[0].Title.Should().Be(books[1].Title);
		results[0].Author.Should().Be(books[1].Author);
		results[0].Count.Should().Be(2);

		results[1].BookId.Should().Be(books[2].BookId);
		results[1].Count.Should().Be(1);

		results.Should().NotContain(r => r.BookId == books[3].BookId);
	}

	[Fact]
	public async Task GetAlsoBorrowedBooksAsync_WithDateRange_ShouldFilterCorrectly()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();
		var targetBook = books[0];

		var startDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		var endDate = new DateTime(2024, 8, 31, 0, 0, 0, DateTimeKind.Utc);

		// User1 borrowed targetBook within range
		var loan1 = Loan.Create(users[0].UserId, targetBook.BookId, new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan1);

		// User1 borrowed books[1] within range
		var loan2 = Loan.Create(users[0].UserId, books[1].BookId, new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan2);

		// User1 borrowed books[2] outside range
		var loan3 = Loan.Create(users[0].UserId, books[2].BookId, new DateTime(2024, 5, 15, 0, 0, 0, DateTimeKind.Utc));
		await _context.Loans.AddAsync(loan3);

		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetAlsoBorrowedBooksAsync(targetBook.BookId, 10, startDate, endDate);

		// Assert
		results.Should().HaveCount(1);
		results[0].BookId.Should().Be(books[1].BookId);
		results.Should().NotContain(r => r.BookId == books[2].BookId);
	}

	[Fact]
	public async Task GetAlsoBorrowedBooksAsync_WhenNoUsersBorrowedTargetBook_ShouldReturnEmpty()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();
		var targetBook = books[0];

		// User1 borrowed books[1] and books[2], but not targetBook
		var loan1 = Loan.Create(users[0].UserId, books[1].BookId, DateTime.UtcNow.AddDays(-8));
		var loan2 = Loan.Create(users[0].UserId, books[2].BookId, DateTime.UtcNow.AddDays(-7));
		await _context.Loans.AddAsync(loan1);
		await _context.Loans.AddAsync(loan2);

		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetAlsoBorrowedBooksAsync(targetBook.BookId, 10, null, null);

		// Assert
		results.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAlsoBorrowedBooksAsync_ShouldExcludeTargetBook()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();
		var targetBook = books[0];

		// User1 borrowed targetBook multiple times
		var loan1 = Loan.Create(users[0].UserId, targetBook.BookId, DateTime.UtcNow.AddDays(-10));
		var loan2 = Loan.Create(users[0].UserId, targetBook.BookId, DateTime.UtcNow.AddDays(-5));
		await _context.Loans.AddAsync(loan1);
		await _context.Loans.AddAsync(loan2);

		// User1 also borrowed books[1]
		var loan3 = Loan.Create(users[0].UserId, books[1].BookId, DateTime.UtcNow.AddDays(-8));
		await _context.Loans.AddAsync(loan3);

		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetAlsoBorrowedBooksAsync(targetBook.BookId, 10, null, null);

		// Assert
		results.Should().HaveCount(1);
		results[0].BookId.Should().Be(books[1].BookId);
		results.Should().NotContain(r => r.BookId == targetBook.BookId);
	}

	[Fact]
	public async Task GetMostBorrowedBooksAsync_WithTopLimit_ShouldRespectLimit()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();

		// Create loans for all books
		foreach (var book in books)
		{
			for (var i = 0; i < books.Count; i++)
			{
				var loan = Loan.Create(users[i % users.Count].UserId, book.BookId, DateTime.UtcNow.AddDays(-10 + i));
				await _context.Loans.AddAsync(loan);
			}
		}
		await _context.SaveChangesAsync();

		// Act
		var results = await _queryService.GetMostBorrowedBooksAsync(2, null, null);

		// Assert
		results.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetTopBorrowersAsync_WithCancellationToken_ShouldRespectCancellation()
	{
		// Arrange
		var (books, users) = await SeedTestDataAsync();
		using var cts = new CancellationTokenSource();

		// Act
		cts.Cancel();
		Func<Task> act = async () => await _queryService.GetTopBorrowersAsync(10, null, null, cts.Token);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private async Task<(List<Book> Books, List<User> Users)> SeedTestDataAsync()
	{
		var faker = new Faker();

		var books = new List<Book>
		{
			Book.Create($"ISBN-{faker.Random.AlphaNumeric(10)}", "Clean Architecture", "Robert C. Martin", 432, 2017),
			Book.Create($"ISBN-{faker.Random.AlphaNumeric(10)}", "Domain-Driven Design", "Eric Evans", 560, 2003),
			Book.Create($"ISBN-{faker.Random.AlphaNumeric(10)}", "The Pragmatic Programmer", "Andrew Hunt", 352, 1999),
			Book.Create($"ISBN-{faker.Random.AlphaNumeric(10)}", "Refactoring", "Martin Fowler", 448, 2018),
			Book.Create($"ISBN-{faker.Random.AlphaNumeric(10)}", "Design Patterns", "Gang of Four", 416, 1994)
		};

		var users = new List<User>
		{
			User.Create("Alice Johnson", DateTime.UtcNow.AddYears(-2)),
			User.Create("Bob Smith", DateTime.UtcNow.AddYears(-1)),
			User.Create("Charlie Brown", DateTime.UtcNow.AddMonths(-6))
		};

		await _context.Books.AddRangeAsync(books);
		await _context.Users.AddRangeAsync(users);
		await _context.SaveChangesAsync();

		return (books, users);
	}
}
