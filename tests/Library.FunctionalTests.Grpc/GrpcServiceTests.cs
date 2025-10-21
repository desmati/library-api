// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using Grpc.Core;
using Grpc.Net.Client;

using Library.Contracts.Circulation.V1;
using Library.Contracts.Inventory.V1;
using Library.Contracts.UserActivity.V1;
using Library.Domain.Entities;
using Library.Infrastructure.Data;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace Library.FunctionalTests.Grpc;

public class GrpcServiceTests : IAsyncLifetime
{
	private PostgreSqlContainer _postgresContainer = null!;
	private WebApplicationFactory<Program> _factory = null!;
	private GrpcChannel _channel = null!;
	private InventoryService.InventoryServiceClient _inventoryClient = null!;
	private UserActivityService.UserActivityServiceClient _userActivityClient = null!;
	private CirculationService.CirculationServiceClient _circulationClient = null!;

	public async Task InitializeAsync()
	{
		_postgresContainer = new PostgreSqlBuilder()
			.WithImage("postgres:16-alpine")
			.WithDatabase("library_test")
			.WithUsername("test_user")
			.WithPassword("test_password")
			.Build();

		await _postgresContainer.StartAsync();

		_factory = new WebApplicationFactory<Program>()
			.WithWebHostBuilder(builder =>
			{
				// Provide connection string via configuration instead of replacing services
				builder.UseSetting("ConnectionStrings:libdb", _postgresContainer.GetConnectionString());
				builder.UseSetting("ConnectionStrings:LibraryDb", _postgresContainer.GetConnectionString());
				builder.UseEnvironment("Testing");
			});

		var client = _factory.CreateDefaultClient();
		_channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions
		{
			HttpClient = client
		});

		_inventoryClient = new InventoryService.InventoryServiceClient(_channel);
		_userActivityClient = new UserActivityService.UserActivityServiceClient(_channel);
		_circulationClient = new CirculationService.CirculationServiceClient(_channel);

		// Initialize database
		using var scope = _factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
		await context.Database.EnsureCreatedAsync();
	}

	public async Task DisposeAsync()
	{
		_channel?.Dispose();
		await _factory.DisposeAsync();
		await _postgresContainer.DisposeAsync();
	}

	[Fact]
	public async Task GetMostBorrowedBooks_WithLoans_ShouldReturnOrderedResults()
	{
		// Arrange
		var (_, _, _) = await SeedTestDataAsync();

		// Act
		var request = new MostBorrowedRequest { Top = 5 };
		var response = await _inventoryClient.GetMostBorrowedBooksAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Items.Should().NotBeEmpty();
		response.Items.Should().HaveCountLessThanOrEqualTo(5);

		// Verify ordering (descending by borrow count)
		for (var i = 0; i < response.Items.Count - 1; i++)
		{
			response.Items[i].BorrowCount.Should().BeGreaterThanOrEqualTo(response.Items[i + 1].BorrowCount);
		}

		// Verify data structure
		var firstItem = response.Items[0];
		firstItem.BookId.Should().NotBeNullOrEmpty();
		firstItem.Title.Should().NotBeNullOrEmpty();
		firstItem.Author.Should().NotBeNullOrEmpty();
		firstItem.PageCount.Should().BeGreaterThan(0);
		firstItem.BorrowCount.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task GetMostBorrowedBooks_WithDateRange_ShouldFilterResults()
	{
		// Arrange
		await SeedTestDataAsync();

		var startDate = DateTime.UtcNow.AddDays(-30).ToString("O");
		var endDate = DateTime.UtcNow.ToString("O");

		// Act
		var request = new MostBorrowedRequest
		{
			Top = 10,
			Range = new Contracts.Inventory.V1.TimeRange { Start = startDate, End = endDate }
		};
		var response = await _inventoryClient.GetMostBorrowedBooksAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Items.Should().NotBeEmpty();
	}

	[Fact]
	public async Task GetMostBorrowedBooks_WithInvalidTop_ShouldThrowRpcException()
	{
		// Arrange
		var request = new MostBorrowedRequest { Top = 0 };

		// Act
		Func<Task> act = async () => await _inventoryClient.GetMostBorrowedBooksAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>()
			.Where(e => e.StatusCode == StatusCode.InvalidArgument);
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WithValidBookId_ShouldReturnRelatedBooks()
	{
		// Arrange
		var (books, _, _) = await SeedTestDataAsync();
		var targetBookId = books[0].BookId.ToString();

		// Act
		var request = new AlsoBorrowedRequest
		{
			BookId = targetBookId,
			Top = 5
		};
		var response = await _inventoryClient.GetAlsoBorrowedBooksAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Items.Should().NotBeNull();

		// If there are results, verify structure
		if (response.Items.Count > 0)
		{
			var firstItem = response.Items[0];
			firstItem.BookId.Should().NotBeNullOrEmpty();
			firstItem.BookId.Should().NotBe(targetBookId);
			firstItem.Title.Should().NotBeNullOrEmpty();
			firstItem.Author.Should().NotBeNullOrEmpty();
			firstItem.CoBorrowCount.Should().BeGreaterThan(0);
		}
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WithInvalidBookId_ShouldThrowRpcException()
	{
		// Arrange
		var request = new AlsoBorrowedRequest
		{
			BookId = "invalid-guid",
			Top = 5
		};

		// Act
		Func<Task> act = async () => await _inventoryClient.GetAlsoBorrowedBooksAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>();
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WithNonExistentBookId_ShouldReturnEmpty()
	{
		// Arrange
		await SeedTestDataAsync();
		var nonExistentBookId = Guid.NewGuid().ToString();

		// Act
		var request = new AlsoBorrowedRequest
		{
			BookId = nonExistentBookId,
			Top = 5
		};
		var response = await _inventoryClient.GetAlsoBorrowedBooksAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task GetTopBorrowers_WithLoans_ShouldReturnOrderedResults()
	{
		// Arrange
		await SeedTestDataAsync();

		// Act
		var request = new TopBorrowersRequest { Top = 5 };
		var response = await _userActivityClient.GetTopBorrowersAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Items.Should().NotBeEmpty();
		response.Items.Should().HaveCountLessThanOrEqualTo(5);

		// Verify ordering (descending by borrow count)
		for (var i = 0; i < response.Items.Count - 1; i++)
		{
			response.Items[i].BorrowCount.Should().BeGreaterThanOrEqualTo(response.Items[i + 1].BorrowCount);
		}

		// Verify data structure
		var firstItem = response.Items[0];
		firstItem.UserId.Should().NotBeNullOrEmpty();
		firstItem.FullName.Should().NotBeNullOrEmpty();
		firstItem.BorrowCount.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task GetTopBorrowers_WithDateRange_ShouldFilterResults()
	{
		// Arrange
		await SeedTestDataAsync();

		var startDate = DateTime.UtcNow.AddDays(-30).ToString("O");
		var endDate = DateTime.UtcNow.ToString("O");

		// Act
		var request = new TopBorrowersRequest
		{
			Top = 10,
			Range = new Contracts.UserActivity.V1.TimeRange { Start = startDate, End = endDate }
		};
		var response = await _userActivityClient.GetTopBorrowersAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Items.Should().NotBeEmpty();
	}

	[Fact]
	public async Task GetTopBorrowers_WithInvalidTop_ShouldThrowRpcException()
	{
		// Arrange
		var request = new TopBorrowersRequest { Top = -1 };

		// Act
		Func<Task> act = async () => await _userActivityClient.GetTopBorrowersAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>()
			.Where(e => e.StatusCode == StatusCode.InvalidArgument);
	}

	[Fact]
	public async Task GetReadingPace_WithReturnedLoans_ShouldReturnPaceResults()
	{
		// Arrange
		var (_, users, _) = await SeedTestDataAsync();

		// Return some loans
		using (var scope = _factory.Services.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
			var loansToReturn = await context.Loans.Take(3).ToListAsync();

			foreach (var loan in loansToReturn)
			{
				loan.Return(DateTime.UtcNow);
			}

			await context.SaveChangesAsync();
		}

		var userId = users[0].UserId.ToString();

		// Act
		var request = new ReadingPaceRequest { UserId = userId };
		var response = await _userActivityClient.GetReadingPaceAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.AggregatePagesPerDay.Should().BeGreaterThanOrEqualTo(0);

		// Verify item structure if there are returned loans
		if (response.Items.Count > 0)
		{
			var firstItem = response.Items[0];
			firstItem.BookId.Should().NotBeNullOrEmpty();
			firstItem.Title.Should().NotBeNullOrEmpty();
			firstItem.Pages.Should().BeGreaterThan(0);
			firstItem.Days.Should().BeGreaterThan(0);
			firstItem.PagesPerDay.Should().BeGreaterThan(0);
			firstItem.BorrowedAt.Should().NotBeNullOrEmpty();
			firstItem.ReturnedAt.Should().NotBeNullOrEmpty();
		}
	}

	[Fact]
	public async Task GetReadingPace_WithInvalidUserId_ShouldThrowRpcException()
	{
		// Arrange
		var request = new ReadingPaceRequest { UserId = "invalid-guid" };

		// Act
		Func<Task> act = async () => await _userActivityClient.GetReadingPaceAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>();
	}

	[Fact]
	public async Task GetReadingPace_WithNonExistentUser_ShouldReturnEmptyResult()
	{
		// Arrange
		await SeedTestDataAsync();
		var nonExistentUserId = Guid.NewGuid().ToString();

		// Act
		var request = new ReadingPaceRequest { UserId = nonExistentUserId };
		var response = await _userActivityClient.GetReadingPaceAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Items.Should().BeEmpty();
		response.AggregatePagesPerDay.Should().Be(0);
	}

	[Fact]
	public async Task BorrowBook_WithValidData_ShouldCreateLoan()
	{
		// Arrange
		var (books, users, _) = await SeedTestDataAsync();
		var userId = users[0].UserId.ToString();
		var bookId = books[4].BookId.ToString(); // Use book[4] which hasn't been borrowed by users[0]

		// Act
		var request = new Library.Contracts.Circulation.V1.BorrowBookRequest
		{
			UserId = userId,
			BookId = bookId,
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};
		var response = await _circulationClient.BorrowBookAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.LoanId.Should().NotBeNullOrEmpty();
		Guid.TryParse(response.LoanId, out _).Should().BeTrue();

		// Verify loan was created in database
		using var scope = _factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
		var loan = await context.Loans.FirstOrDefaultAsync(l => l.LoanId == Guid.Parse(response.LoanId));
		loan.Should().NotBeNull();
		loan!.UserId.Should().Be(Guid.Parse(userId));
		loan.BookId.Should().Be(Guid.Parse(bookId));
	}

	[Fact]
	public async Task BorrowBook_WithInvalidUserId_ShouldThrowRpcException()
	{
		// Arrange
		var (books, _, _) = await SeedTestDataAsync();

		var request = new Library.Contracts.Circulation.V1.BorrowBookRequest
		{
			UserId = "invalid-guid",
			BookId = books[0].BookId.ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		Func<Task> act = async () => await _circulationClient.BorrowBookAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>();
	}

	[Fact]
	public async Task BorrowBook_WithNonExistentUser_ShouldThrowRpcException()
	{
		// Arrange
		var (books, _, _) = await SeedTestDataAsync();

		var request = new Library.Contracts.Circulation.V1.BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = books[0].BookId.ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		Func<Task> act = async () => await _circulationClient.BorrowBookAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>()
			.Where(e => e.StatusCode == StatusCode.NotFound || e.StatusCode == StatusCode.InvalidArgument);
	}

	[Fact]
	public async Task BorrowBook_WithNonExistentBook_ShouldThrowRpcException()
	{
		// Arrange
		var (_, users, _) = await SeedTestDataAsync();

		var request = new Library.Contracts.Circulation.V1.BorrowBookRequest
		{
			UserId = users[0].UserId.ToString(),
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		Func<Task> act = async () => await _circulationClient.BorrowBookAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>()
			.Where(e => e.StatusCode == StatusCode.NotFound || e.StatusCode == StatusCode.InvalidArgument);
	}

	[Fact]
	public async Task BorrowBook_WhenUserAlreadyHasActiveLoan_ShouldThrowRpcException()
	{
		// Arrange
		var (books, users, _) = await SeedTestDataAsync();
		var userId = users[0].UserId.ToString();
		var bookId = books[4].BookId.ToString(); // Use book[4] which hasn't been borrowed yet

		// First borrow
		var firstRequest = new Library.Contracts.Circulation.V1.BorrowBookRequest
		{
			UserId = userId,
			BookId = bookId,
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};
		await _circulationClient.BorrowBookAsync(firstRequest);

		// Act - Try to borrow the same book again
		var secondRequest = new Library.Contracts.Circulation.V1.BorrowBookRequest
		{
			UserId = userId,
			BookId = bookId,
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};
		Func<Task> act = async () => await _circulationClient.BorrowBookAsync(secondRequest);

		// Assert
		await act.Should().ThrowAsync<RpcException>()
			.Where(e => e.StatusCode == StatusCode.InvalidArgument || e.StatusCode == StatusCode.FailedPrecondition);
	}

	[Fact]
	public async Task ReturnBook_WithValidLoanId_ShouldMarkAsReturned()
	{
		// Arrange
		var (books, users, loans) = await SeedTestDataAsync();
		var loanId = loans[0].LoanId.ToString();

		// Act
		var request = new Library.Contracts.Circulation.V1.ReturnBookRequest
		{
			LoanId = loanId,
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};
		var response = await _circulationClient.ReturnBookAsync(request);

		// Assert
		response.Should().NotBeNull();
		response.Success.Should().BeTrue();

		// Verify loan was returned in database
		using var scope = _factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
		var loan = await context.Loans.FirstOrDefaultAsync(l => l.LoanId == Guid.Parse(loanId));
		loan.Should().NotBeNull();
		loan!.IsReturned.Should().BeTrue();
		loan.ReturnedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task ReturnBook_WithInvalidLoanId_ShouldThrowRpcException()
	{
		// Arrange
		var request = new Library.Contracts.Circulation.V1.ReturnBookRequest
		{
			LoanId = "invalid-guid",
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		Func<Task> act = async () => await _circulationClient.ReturnBookAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>();
	}

	[Fact]
	public async Task ReturnBook_WithNonExistentLoanId_ShouldThrowRpcException()
	{
		// Arrange
		await SeedTestDataAsync();

		var request = new Library.Contracts.Circulation.V1.ReturnBookRequest
		{
			LoanId = Guid.NewGuid().ToString(),
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		Func<Task> act = async () => await _circulationClient.ReturnBookAsync(request);

		// Assert
		await act.Should().ThrowAsync<RpcException>()
			.Where(e => e.StatusCode == StatusCode.NotFound || e.StatusCode == StatusCode.InvalidArgument);
	}

	[Fact]
	public async Task ReturnBook_WithAlreadyReturnedLoan_ShouldThrowRpcException()
	{
		// Arrange
		var (books, users, loans) = await SeedTestDataAsync();
		var loanId = loans[0].LoanId.ToString();

		// First return
		var firstRequest = new Library.Contracts.Circulation.V1.ReturnBookRequest
		{
			LoanId = loanId,
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};
		await _circulationClient.ReturnBookAsync(firstRequest);

		// Act - Try to return again
		var secondRequest = new Library.Contracts.Circulation.V1.ReturnBookRequest
		{
			LoanId = loanId,
			ReturnedAt = DateTime.UtcNow.AddDays(1).ToString("O")
		};
		Func<Task> act = async () => await _circulationClient.ReturnBookAsync(secondRequest);

		// Assert
		await act.Should().ThrowAsync<RpcException>()
			.Where(e => e.StatusCode == StatusCode.InvalidArgument || e.StatusCode == StatusCode.FailedPrecondition);
	}

	private async Task<(List<Book> Books, List<User> Users, List<Loan> Loans)> SeedTestDataAsync()
	{
		using var scope = _factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

		var books = new List<Book>
		{
			Book.Create("ISBN-001", "Clean Architecture", "Robert C. Martin", 432, 2017),
			Book.Create("ISBN-002", "Domain-Driven Design", "Eric Evans", 560, 2003),
			Book.Create("ISBN-003", "The Pragmatic Programmer", "Andrew Hunt", 352, 1999),
			Book.Create("ISBN-004", "Refactoring", "Martin Fowler", 448, 2018),
			Book.Create("ISBN-005", "Design Patterns", "Gang of Four", 416, 1994)
		};

		var users = new List<User>
		{
			User.Create("Alice Johnson", DateTime.UtcNow.AddYears(-2)),
			User.Create("Bob Smith", DateTime.UtcNow.AddYears(-1)),
			User.Create("Charlie Brown", DateTime.UtcNow.AddMonths(-6))
		};

		await context.Books.AddRangeAsync(books);
		await context.Users.AddRangeAsync(users);
		await context.SaveChangesAsync();

		// Create some loans
		var loans = new List<Loan>
		{
			Loan.Create(users[0].UserId, books[0].BookId, DateTime.UtcNow.AddDays(-20)),
			Loan.Create(users[0].UserId, books[1].BookId, DateTime.UtcNow.AddDays(-15)),
			Loan.Create(users[1].UserId, books[0].BookId, DateTime.UtcNow.AddDays(-12)),
			Loan.Create(users[1].UserId, books[2].BookId, DateTime.UtcNow.AddDays(-10)),
			Loan.Create(users[2].UserId, books[1].BookId, DateTime.UtcNow.AddDays(-8)),
			Loan.Create(users[2].UserId, books[3].BookId, DateTime.UtcNow.AddDays(-5))
		};

		await context.Loans.AddRangeAsync(loans);
		await context.SaveChangesAsync();

		return (books, users, loans);
	}
}
