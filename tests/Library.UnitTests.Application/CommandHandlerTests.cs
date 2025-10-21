// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using Library.Application.Commands.BorrowBook;
using Library.Application.Commands.ReturnBook;
using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.Domain.Repositories;

using Moq;

namespace Library.UnitTests.Application;

public class CommandHandlerTests
{
	public class BorrowBookCommandHandlerTests
	{
		private readonly Mock<ILoanRepository> _loanRepositoryMock;
		private readonly Mock<IUserRepository> _userRepositoryMock;
		private readonly Mock<IBookRepository> _bookRepositoryMock;
		private readonly BorrowBookCommandHandler _handler;

		public BorrowBookCommandHandlerTests()
		{
			_loanRepositoryMock = new Mock<ILoanRepository>();
			_userRepositoryMock = new Mock<IUserRepository>();
			_bookRepositoryMock = new Mock<IBookRepository>();
			_handler = new BorrowBookCommandHandler(
				_loanRepositoryMock.Object,
				_userRepositoryMock.Object,
				_bookRepositoryMock.Object);
		}

		[Fact]
		public async Task Handle_WithValidData_ShouldCreateLoanAndReturnLoanId()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var bookId = Guid.NewGuid();
			var borrowedAt = DateTime.UtcNow.AddDays(-1);
			var command = new BorrowBookCommand(userId, bookId, borrowedAt);

			_userRepositoryMock
				.Setup(x => x.ExistsAsync(userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			_bookRepositoryMock
				.Setup(x => x.ExistsAsync(bookId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			_loanRepositoryMock
				.Setup(x => x.GetActiveLoanAsync(userId, bookId, It.IsAny<CancellationToken>()))
				.ReturnsAsync((Loan?)null);

			Loan? capturedLoan = null;
			_loanRepositoryMock
				.Setup(x => x.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
				.Callback<Loan, CancellationToken>((loan, ct) => capturedLoan = loan)
				.Returns(Task.CompletedTask);

			// Act
			var result = await _handler.Handle(command, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.LoanId.Should().NotBeEmpty();

			capturedLoan.Should().NotBeNull();
			capturedLoan!.UserId.Should().Be(userId);
			capturedLoan.BookId.Should().Be(bookId);
			capturedLoan.BorrowedAt.Should().Be(borrowedAt);
			capturedLoan.IsReturned.Should().BeFalse();

			_userRepositoryMock.Verify(x => x.ExistsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
			_bookRepositoryMock.Verify(x => x.ExistsAsync(bookId, It.IsAny<CancellationToken>()), Times.Once);
			_loanRepositoryMock.Verify(x => x.GetActiveLoanAsync(userId, bookId, It.IsAny<CancellationToken>()), Times.Once);
			_loanRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithNonExistentUser_ShouldThrowEntityNotFoundException()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var bookId = Guid.NewGuid();
			var command = new BorrowBookCommand(userId, bookId, DateTime.UtcNow);

			_userRepositoryMock
				.Setup(x => x.ExistsAsync(userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);

			// Act
			var act = async () => await _handler.Handle(command, CancellationToken.None);

			// Assert
			await act.Should().ThrowAsync<EntityNotFoundException>()
				.WithMessage($"User with id {userId} was not found");

			_userRepositoryMock.Verify(x => x.ExistsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
			_bookRepositoryMock.Verify(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
			_loanRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Handle_WithNonExistentBook_ShouldThrowEntityNotFoundException()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var bookId = Guid.NewGuid();
			var command = new BorrowBookCommand(userId, bookId, DateTime.UtcNow);

			_userRepositoryMock
				.Setup(x => x.ExistsAsync(userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			_bookRepositoryMock
				.Setup(x => x.ExistsAsync(bookId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);

			// Act
			var act = async () => await _handler.Handle(command, CancellationToken.None);

			// Assert
			await act.Should().ThrowAsync<EntityNotFoundException>()
				.WithMessage($"Book with id {bookId} was not found");

			_userRepositoryMock.Verify(x => x.ExistsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
			_bookRepositoryMock.Verify(x => x.ExistsAsync(bookId, It.IsAny<CancellationToken>()), Times.Once);
			_loanRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Handle_WithExistingActiveLoan_ShouldThrowInvalidOperationDomainException()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var bookId = Guid.NewGuid();
			var command = new BorrowBookCommand(userId, bookId, DateTime.UtcNow);

			var existingLoan = Loan.Create(userId, bookId, DateTime.UtcNow.AddDays(-5));

			_userRepositoryMock
				.Setup(x => x.ExistsAsync(userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			_bookRepositoryMock
				.Setup(x => x.ExistsAsync(bookId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			_loanRepositoryMock
				.Setup(x => x.GetActiveLoanAsync(userId, bookId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(existingLoan);

			// Act
			var act = async () => await _handler.Handle(command, CancellationToken.None);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationDomainException>()
				.WithMessage($"User {userId} already has an active loan for book {bookId}");

			_loanRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Handle_WithCancellationToken_ShouldPassItThrough()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var bookId = Guid.NewGuid();
			var command = new BorrowBookCommand(userId, bookId, DateTime.UtcNow);
			var cancellationToken = new CancellationToken();

			_userRepositoryMock
				.Setup(x => x.ExistsAsync(userId, cancellationToken))
				.ReturnsAsync(true);

			_bookRepositoryMock
				.Setup(x => x.ExistsAsync(bookId, cancellationToken))
				.ReturnsAsync(true);

			_loanRepositoryMock
				.Setup(x => x.GetActiveLoanAsync(userId, bookId, cancellationToken))
				.ReturnsAsync((Loan?)null);

			_loanRepositoryMock
				.Setup(x => x.AddAsync(It.IsAny<Loan>(), cancellationToken))
				.Returns(Task.CompletedTask);

			// Act
			await _handler.Handle(command, cancellationToken);

			// Assert
			_userRepositoryMock.Verify(x => x.ExistsAsync(userId, cancellationToken), Times.Once);
			_bookRepositoryMock.Verify(x => x.ExistsAsync(bookId, cancellationToken), Times.Once);
			_loanRepositoryMock.Verify(x => x.GetActiveLoanAsync(userId, bookId, cancellationToken), Times.Once);
			_loanRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Loan>(), cancellationToken), Times.Once);
		}
	}

	public class ReturnBookCommandHandlerTests
	{
		private readonly Mock<ILoanRepository> _loanRepositoryMock;
		private readonly ReturnBookCommandHandler _handler;

		public ReturnBookCommandHandlerTests()
		{
			_loanRepositoryMock = new Mock<ILoanRepository>();
			_handler = new ReturnBookCommandHandler(_loanRepositoryMock.Object);
		}

		[Fact]
		public async Task Handle_WithValidLoan_ShouldReturnLoanAndReturnSuccess()
		{
			// Arrange
			var loanId = Guid.NewGuid();
			var returnedAt = DateTime.UtcNow;
			var command = new ReturnBookCommand(loanId, returnedAt);

			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-7));

			_loanRepositoryMock
				.Setup(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(loan);

			Loan? capturedLoan = null;
			_loanRepositoryMock
				.Setup(x => x.UpdateAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()))
				.Callback<Loan, CancellationToken>((l, ct) => capturedLoan = l)
				.Returns(Task.CompletedTask);

			// Act
			var result = await _handler.Handle(command, CancellationToken.None);

			// Assert
			result.Should().NotBeNull();
			result.Success.Should().BeTrue();

			capturedLoan.Should().NotBeNull();
			capturedLoan!.IsReturned.Should().BeTrue();
			capturedLoan.ReturnedAt.Should().Be(returnedAt);

			_loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()), Times.Once);
			_loanRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task Handle_WithNonExistentLoan_ShouldThrowEntityNotFoundException()
		{
			// Arrange
			var loanId = Guid.NewGuid();
			var command = new ReturnBookCommand(loanId, DateTime.UtcNow);

			_loanRepositoryMock
				.Setup(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()))
				.ReturnsAsync((Loan?)null);

			// Act
			var act = async () => await _handler.Handle(command, CancellationToken.None);

			// Assert
			await act.Should().ThrowAsync<EntityNotFoundException>()
				.WithMessage($"Loan with id {loanId} was not found");

			_loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()), Times.Once);
			_loanRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Handle_WithAlreadyReturnedLoan_ShouldThrowInvalidOperationException()
		{
			// Arrange
			var loanId = Guid.NewGuid();
			var command = new ReturnBookCommand(loanId, DateTime.UtcNow);

			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-7));
			loan.Return(DateTime.UtcNow.AddDays(-1));

			_loanRepositoryMock
				.Setup(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(loan);

			// Act
			var act = async () => await _handler.Handle(command, CancellationToken.None);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("Loan has already been returned");

			_loanRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task Handle_WithCancellationToken_ShouldPassItThrough()
		{
			// Arrange
			var loanId = Guid.NewGuid();
			var command = new ReturnBookCommand(loanId, DateTime.UtcNow);
			var cancellationToken = new CancellationToken();

			var loan = Loan.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-7));

			_loanRepositoryMock
				.Setup(x => x.GetByIdAsync(loanId, cancellationToken))
				.ReturnsAsync(loan);

			_loanRepositoryMock
				.Setup(x => x.UpdateAsync(It.IsAny<Loan>(), cancellationToken))
				.Returns(Task.CompletedTask);

			// Act
			await _handler.Handle(command, cancellationToken);

			// Assert
			_loanRepositoryMock.Verify(x => x.GetByIdAsync(loanId, cancellationToken), Times.Once);
			_loanRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Loan>(), cancellationToken), Times.Once);
		}
	}
}
