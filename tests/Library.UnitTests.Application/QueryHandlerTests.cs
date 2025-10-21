// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using Library.Application.Queries.AlsoBorrowedBooks;
using Library.Application.Queries.MostBorrowedBooks;
using Library.Application.Queries.TopBorrowers;
using Library.Application.Queries.UserReadingPace;
using Library.Domain.Entities;
using Library.Domain.Repositories;
using Library.Infrastructure.Queries;

using Moq;

namespace Library.UnitTests.Application;

public class QueryHandlerTests
{
	public class GetUserReadingPaceQueryHandlerTests
	{
		private readonly Mock<ILoanRepository> _loanRepositoryMock;
		private readonly GetUserReadingPaceQueryHandler _handler;

		public GetUserReadingPaceQueryHandlerTests()
		{
			_loanRepositoryMock = new Mock<ILoanRepository>();
			_handler = new GetUserReadingPaceQueryHandler(_loanRepositoryMock.Object);
		}

		[Fact]
		public async Task Handle_WithReturnedLoans_ShouldCalculateReadingPace()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var query = new GetUserReadingPaceQuery(userId, null, null);

			var book1 = Book.Create("ISBN-1", "Book 1", "Author 1", 300);
			var book2 = Book.Create("ISBN-2", "Book 2", "Author 2", 600);

			var loan1 = Loan.Create(userId, book1.BookId, new DateTime(2024, 1, 1));
			var loan2 = Loan.Create(userId, book2.BookId, new DateTime(2024, 1, 5));

			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan1, book1);
			bookProperty!.SetValue(loan2, book2);

			loan1.Return(new DateTime(2024, 1, 11)); // 10 days, 30 pages/day
			loan2.Return(new DateTime(2024, 1, 25)); // 20 days, 30 pages/day

			var loans = new[] { loan1, loan2 };

			_loanRepositoryMock
				.Setup(x => x.GetLoansByUserAsync(userId, null, null, It.IsAny<CancellationToken>()))
				.ReturnsAsync(loans);

			// Act
			var result = await _handler.Handle(query, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.AggregatePagesPerDay.Should().Be(30.0);
			result.Items.Should().HaveCount(2);

			result.Items[0].Title.Should().Be("Book 1");
			result.Items[0].PagesPerDay.Should().Be(30.0);

			result.Items[1].Title.Should().Be("Book 2");
			result.Items[1].PagesPerDay.Should().Be(30.0);

			_loanRepositoryMock.Verify(x => x.GetLoansByUserAsync(userId, null, null, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithNoReturnedLoans_ShouldReturnZeroPace()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var query = new GetUserReadingPaceQuery(userId, null, null);

			var book = Book.Create("ISBN-1", "Book 1", "Author 1", 300);
			var loan = Loan.Create(userId, book.BookId, DateTime.UtcNow);

			var bookProperty = typeof(Loan).GetProperty(nameof(Loan.Book));
			bookProperty!.SetValue(loan, book);

			var loans = new[] { loan };

			_loanRepositoryMock
				.Setup(x => x.GetLoansByUserAsync(userId, null, null, It.IsAny<CancellationToken>()))
				.ReturnsAsync(loans);

			// Act
			var result = await _handler.Handle(query, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.AggregatePagesPerDay.Should().Be(0);
			result.Items.Should().BeEmpty();
		}

		[Fact]
		public async Task Handle_WithDateRange_ShouldPassParametersToRepository()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var start = new DateTime(2024, 1, 1);
			var end = new DateTime(2024, 12, 31);
			var query = new GetUserReadingPaceQuery(userId, start, end);

			_loanRepositoryMock
				.Setup(x => x.GetLoansByUserAsync(userId, start, end, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			// Act
			await _handler.Handle(query, CancellationToken.None);

			// Assert
			_loanRepositoryMock.Verify(x => x.GetLoansByUserAsync(userId, start, end, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithCancellationToken_ShouldPassItThrough()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var query = new GetUserReadingPaceQuery(userId, null, null);
			var cancellationToken = new CancellationToken();

			_loanRepositoryMock
				.Setup(x => x.GetLoansByUserAsync(userId, null, null, cancellationToken))
				.ReturnsAsync([]);

			// Act
			await _handler.Handle(query, cancellationToken);

			// Assert
			_loanRepositoryMock.Verify(x => x.GetLoansByUserAsync(userId, null, null, cancellationToken), Times.Once);
		}
	}

	public class GetMostBorrowedBooksQueryHandlerTests
	{
		private readonly Mock<IQueryService> _queryServiceMock;
		private readonly GetMostBorrowedBooksQueryHandler _handler;

		public GetMostBorrowedBooksQueryHandlerTests()
		{
			_queryServiceMock = new Mock<IQueryService>();
			_handler = new GetMostBorrowedBooksQueryHandler(_queryServiceMock.Object);
		}

		[Fact]
		public async Task Handle_ShouldReturnMostBorrowedBooks()
		{
			// Arrange
			var query = new GetMostBorrowedBooksQuery(5, null, null);

			var serviceResults = new List<(Guid BookId, string Title, string Author, int PageCount, long Count)>
			{
				(Guid.NewGuid(), "Book 1", "Author 1", 300, 100),
				(Guid.NewGuid(), "Book 2", "Author 2", 400, 75),
				(Guid.NewGuid(), "Book 3", "Author 3", 250, 50)
			};

			_queryServiceMock
				.Setup(x => x.GetMostBorrowedBooksAsync(5, null, null, It.IsAny<CancellationToken>()))
				.ReturnsAsync(serviceResults);

			// Act
			var result = await _handler.Handle(query, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Items.Should().HaveCount(3);

			result.Items[0].Title.Should().Be("Book 1");
			result.Items[0].BorrowCount.Should().Be(100);

			result.Items[1].Title.Should().Be("Book 2");
			result.Items[1].BorrowCount.Should().Be(75);

			result.Items[2].Title.Should().Be("Book 3");
			result.Items[2].BorrowCount.Should().Be(50);

			_queryServiceMock.Verify(x => x.GetMostBorrowedBooksAsync(5, null, null, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithDateRange_ShouldPassParametersToQueryService()
		{
			// Arrange
			var start = new DateTime(2024, 1, 1);
			var end = new DateTime(2024, 12, 31);
			var query = new GetMostBorrowedBooksQuery(10, start, end);

			_queryServiceMock
				.Setup(x => x.GetMostBorrowedBooksAsync(10, start, end, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new List<(Guid, string, string, int, long)>());

			// Act
			await _handler.Handle(query, CancellationToken.None);

			// Assert
			_queryServiceMock.Verify(x => x.GetMostBorrowedBooksAsync(10, start, end, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithCancellationToken_ShouldPassItThrough()
		{
			// Arrange
			var query = new GetMostBorrowedBooksQuery(5, null, null);
			var cancellationToken = new CancellationToken();

			_queryServiceMock
				.Setup(x => x.GetMostBorrowedBooksAsync(5, null, null, cancellationToken))
				.ReturnsAsync(new List<(Guid, string, string, int, long)>());

			// Act
			await _handler.Handle(query, cancellationToken);

			// Assert
			_queryServiceMock.Verify(x => x.GetMostBorrowedBooksAsync(5, null, null, cancellationToken), Times.Once);
		}
	}

	public class GetTopBorrowersQueryHandlerTests
	{
		private readonly Mock<IQueryService> _queryServiceMock;
		private readonly GetTopBorrowersQueryHandler _handler;

		public GetTopBorrowersQueryHandlerTests()
		{
			_queryServiceMock = new Mock<IQueryService>();
			_handler = new GetTopBorrowersQueryHandler(_queryServiceMock.Object);
		}

		[Fact]
		public async Task Handle_ShouldReturnTopBorrowers()
		{
			// Arrange
			var query = new GetTopBorrowersQuery(5, null, null);

			var serviceResults = new List<(Guid UserId, string FullName, long Count)>
			{
				(Guid.NewGuid(), "Alice Johnson", 50),
				(Guid.NewGuid(), "Bob Smith", 45),
				(Guid.NewGuid(), "Charlie Brown", 40)
			};

			_queryServiceMock
				.Setup(x => x.GetTopBorrowersAsync(5, null, null, It.IsAny<CancellationToken>()))
				.ReturnsAsync(serviceResults);

			// Act
			var result = await _handler.Handle(query, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Items.Should().HaveCount(3);

			result.Items[0].FullName.Should().Be("Alice Johnson");
			result.Items[0].BorrowCount.Should().Be(50);

			result.Items[1].FullName.Should().Be("Bob Smith");
			result.Items[1].BorrowCount.Should().Be(45);

			result.Items[2].FullName.Should().Be("Charlie Brown");
			result.Items[2].BorrowCount.Should().Be(40);

			_queryServiceMock.Verify(x => x.GetTopBorrowersAsync(5, null, null, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithDateRange_ShouldPassParametersToQueryService()
		{
			// Arrange
			var start = new DateTime(2024, 1, 1);
			var end = new DateTime(2024, 12, 31);
			var query = new GetTopBorrowersQuery(10, start, end);

			_queryServiceMock
				.Setup(x => x.GetTopBorrowersAsync(10, start, end, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new List<(Guid, string, long)>());

			// Act
			await _handler.Handle(query, CancellationToken.None);

			// Assert
			_queryServiceMock.Verify(x => x.GetTopBorrowersAsync(10, start, end, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithCancellationToken_ShouldPassItThrough()
		{
			// Arrange
			var query = new GetTopBorrowersQuery(5, null, null);
			var cancellationToken = new CancellationToken();

			_queryServiceMock
				.Setup(x => x.GetTopBorrowersAsync(5, null, null, cancellationToken))
				.ReturnsAsync(new List<(Guid, string, long)>());

			// Act
			await _handler.Handle(query, cancellationToken);

			// Assert
			_queryServiceMock.Verify(x => x.GetTopBorrowersAsync(5, null, null, cancellationToken), Times.Once);
		}
	}

	public class GetAlsoBorrowedBooksQueryHandlerTests
	{
		private readonly Mock<IQueryService> _queryServiceMock;
		private readonly GetAlsoBorrowedBooksQueryHandler _handler;

		public GetAlsoBorrowedBooksQueryHandlerTests()
		{
			_queryServiceMock = new Mock<IQueryService>();
			_handler = new GetAlsoBorrowedBooksQueryHandler(_queryServiceMock.Object);
		}

		[Fact]
		public async Task Handle_ShouldReturnAlsoBorrowedBooks()
		{
			// Arrange
			var bookId = Guid.NewGuid();
			var query = new GetAlsoBorrowedBooksQuery(bookId, 5, null, null);

			var serviceResults = new List<(Guid BookId, string Title, string Author, long Count)>
			{
				(Guid.NewGuid(), "Related Book 1", "Author 1", 30),
				(Guid.NewGuid(), "Related Book 2", "Author 2", 25),
				(Guid.NewGuid(), "Related Book 3", "Author 3", 20)
			};

			_queryServiceMock
				.Setup(x => x.GetAlsoBorrowedBooksAsync(bookId, 5, null, null, It.IsAny<CancellationToken>()))
				.ReturnsAsync(serviceResults);

			// Act
			var result = await _handler.Handle(query, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Items.Should().HaveCount(3);

			result.Items[0].Title.Should().Be("Related Book 1");
			result.Items[0].CoBorrowCount.Should().Be(30);

			result.Items[1].Title.Should().Be("Related Book 2");
			result.Items[1].CoBorrowCount.Should().Be(25);

			result.Items[2].Title.Should().Be("Related Book 3");
			result.Items[2].CoBorrowCount.Should().Be(20);

			_queryServiceMock.Verify(x => x.GetAlsoBorrowedBooksAsync(bookId, 5, null, null, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithDateRange_ShouldPassParametersToQueryService()
		{
			// Arrange
			var bookId = Guid.NewGuid();
			var start = new DateTime(2024, 1, 1);
			var end = new DateTime(2024, 12, 31);
			var query = new GetAlsoBorrowedBooksQuery(bookId, 10, start, end);

			_queryServiceMock
				.Setup(x => x.GetAlsoBorrowedBooksAsync(bookId, 10, start, end, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new List<(Guid, string, string, long)>());

			// Act
			await _handler.Handle(query, CancellationToken.None);

			// Assert
			_queryServiceMock.Verify(x => x.GetAlsoBorrowedBooksAsync(bookId, 10, start, end, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithCancellationToken_ShouldPassItThrough()
		{
			// Arrange
			var bookId = Guid.NewGuid();
			var query = new GetAlsoBorrowedBooksQuery(bookId, 5, null, null);
			var cancellationToken = new CancellationToken();

			_queryServiceMock
				.Setup(x => x.GetAlsoBorrowedBooksAsync(bookId, 5, null, null, cancellationToken))
				.ReturnsAsync(new List<(Guid, string, string, long)>());

			// Act
			await _handler.Handle(query, cancellationToken);

			// Assert
			_queryServiceMock.Verify(x => x.GetAlsoBorrowedBooksAsync(bookId, 5, null, null, cancellationToken), Times.Once);
		}
	}
}
