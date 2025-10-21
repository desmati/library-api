// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Grpc.Core;

using Library.Api.Models;

using Moq;

namespace Library.SystemTests.Api;

public class CirculationEndpointsTests : IClassFixture<ApiTestFactory>, IDisposable
{
	private readonly ApiTestFactory _factory;
	private readonly HttpClient _client;

	public CirculationEndpointsTests(ApiTestFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();
		_factory.ResetMocks();
	}

	public void Dispose()
	{
		_client.Dispose();
		GC.SuppressFinalize(this);
	}

	[Fact]
	public async Task BorrowBook_WithValidRequest_ShouldReturnOk()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var bookId = Guid.NewGuid().ToString();
		var borrowedAt = DateTime.UtcNow.ToString("O");
		var loanId = Guid.NewGuid().ToString();

		var request = new BorrowBookRequest
		{
			UserId = userId,
			BookId = bookId,
			BorrowedAt = borrowedAt
		};

		var grpcResponse = new Library.Contracts.Circulation.V1.BorrowBookResponse
		{
			LoanId = loanId
		};

		_factory.CirculationServiceMock
			.Setup(x => x.BorrowBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.BorrowBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.Circulation.V1.BorrowBookResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<BorrowBookResponse>();
		result.Should().NotBeNull();
		result!.LoanId.Should().Be(loanId);

		_factory.CirculationServiceMock.Verify(x => x.BorrowBookAsync(
			It.Is<Library.Contracts.Circulation.V1.BorrowBookRequest>(r =>
				r.UserId == userId && r.BookId == bookId && r.BorrowedAt == borrowedAt),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task BorrowBook_WithoutBorrowedAt_ShouldUseCurrentTime()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var bookId = Guid.NewGuid().ToString();
		var loanId = Guid.NewGuid().ToString();

		var request = new BorrowBookRequest
		{
			UserId = userId,
			BookId = bookId,
			BorrowedAt = null
		};

		var grpcResponse = new Library.Contracts.Circulation.V1.BorrowBookResponse
		{
			LoanId = loanId
		};

		_factory.CirculationServiceMock
			.Setup(x => x.BorrowBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.BorrowBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.Circulation.V1.BorrowBookResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<BorrowBookResponse>();
		result.Should().NotBeNull();
		result!.LoanId.Should().Be(loanId);

		_factory.CirculationServiceMock.Verify(x => x.BorrowBookAsync(
			It.Is<Library.Contracts.Circulation.V1.BorrowBookRequest>(r =>
				r.UserId == userId && r.BookId == bookId && !string.IsNullOrEmpty(r.BorrowedAt)),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task BorrowBook_WithMissingUserId_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = "",
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("userId");
	}

	[Fact]
	public async Task BorrowBook_WithMissingBookId_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = "",
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("bookId");
	}

	[Fact]
	public async Task BorrowBook_WithInvalidDateFormat_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = "invalid-date"
		};

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("date");
	}

	[Fact]
	public async Task BorrowBook_WhenUserNotFound_ShouldReturnNotFound()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.BorrowBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.BorrowBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.NotFound, "User not found")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("User not found");
	}

	[Fact]
	public async Task BorrowBook_WhenBookNotFound_ShouldReturnNotFound()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.BorrowBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.BorrowBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.NotFound, "Book not found")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Book not found");
	}

	[Fact]
	public async Task BorrowBook_WhenActiveLoanExists_ShouldReturnConflict()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.BorrowBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.BorrowBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.AlreadyExists, "Active loan already exists")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Active loan already exists");
	}

	[Fact]
	public async Task BorrowBook_WhenGrpcThrowsInvalidArgument_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.BorrowBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.BorrowBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request parameters")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Invalid request parameters");
	}

	[Fact]
	public async Task BorrowBook_WhenGrpcThrowsInternalError_ShouldReturnInternalServerError()
	{
		// Arrange
		var request = new BorrowBookRequest
		{
			UserId = Guid.NewGuid().ToString(),
			BookId = Guid.NewGuid().ToString(),
			BorrowedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.BorrowBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.BorrowBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.Internal, "Internal server error")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/borrow", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}

	[Fact]
	public async Task ReturnBook_WithValidRequest_ShouldReturnOk()
	{
		// Arrange
		var loanId = Guid.NewGuid().ToString();
		var returnedAt = DateTime.UtcNow.ToString("O");

		var request = new ReturnBookRequest
		{
			LoanId = loanId,
			ReturnedAt = returnedAt
		};

		var grpcResponse = new Library.Contracts.Circulation.V1.ReturnBookResponse
		{
			Success = true
		};

		_factory.CirculationServiceMock
			.Setup(x => x.ReturnBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.ReturnBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.Circulation.V1.ReturnBookResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<ReturnBookResponse>();
		result.Should().NotBeNull();
		result!.Success.Should().BeTrue();

		_factory.CirculationServiceMock.Verify(x => x.ReturnBookAsync(
			It.Is<Library.Contracts.Circulation.V1.ReturnBookRequest>(r =>
				r.LoanId == loanId && r.ReturnedAt == returnedAt),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReturnBook_WithoutReturnedAt_ShouldUseCurrentTime()
	{
		// Arrange
		var loanId = Guid.NewGuid().ToString();

		var request = new ReturnBookRequest
		{
			LoanId = loanId,
			ReturnedAt = null
		};

		var grpcResponse = new Library.Contracts.Circulation.V1.ReturnBookResponse
		{
			Success = true
		};

		_factory.CirculationServiceMock
			.Setup(x => x.ReturnBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.ReturnBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.Circulation.V1.ReturnBookResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<ReturnBookResponse>();
		result.Should().NotBeNull();
		result!.Success.Should().BeTrue();

		_factory.CirculationServiceMock.Verify(x => x.ReturnBookAsync(
			It.Is<Library.Contracts.Circulation.V1.ReturnBookRequest>(r =>
				r.LoanId == loanId && !string.IsNullOrEmpty(r.ReturnedAt)),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReturnBook_WithMissingLoanId_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new ReturnBookRequest
		{
			LoanId = "",
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("loanId");
	}

	[Fact]
	public async Task ReturnBook_WithInvalidDateFormat_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new ReturnBookRequest
		{
			LoanId = Guid.NewGuid().ToString(),
			ReturnedAt = "invalid-date"
		};

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("date");
	}

	[Fact]
	public async Task ReturnBook_WhenLoanNotFound_ShouldReturnNotFound()
	{
		// Arrange
		var request = new ReturnBookRequest
		{
			LoanId = Guid.NewGuid().ToString(),
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.ReturnBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.ReturnBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.NotFound, "Loan not found")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Loan not found");
	}

	[Fact]
	public async Task ReturnBook_WhenAlreadyReturned_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new ReturnBookRequest
		{
			LoanId = Guid.NewGuid().ToString(),
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.ReturnBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.ReturnBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.InvalidArgument, "Loan has already been returned")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("already been returned");
	}

	[Fact]
	public async Task ReturnBook_WhenGrpcThrowsInvalidArgument_ShouldReturnBadRequest()
	{
		// Arrange
		var request = new ReturnBookRequest
		{
			LoanId = Guid.NewGuid().ToString(),
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.ReturnBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.ReturnBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.InvalidArgument, "Invalid request parameters")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Invalid request parameters");
	}

	[Fact]
	public async Task ReturnBook_WhenGrpcThrowsInternalError_ShouldReturnInternalServerError()
	{
		// Arrange
		var request = new ReturnBookRequest
		{
			LoanId = Guid.NewGuid().ToString(),
			ReturnedAt = DateTime.UtcNow.ToString("O")
		};

		_factory.CirculationServiceMock
			.Setup(x => x.ReturnBookAsync(
				It.IsAny<Library.Contracts.Circulation.V1.ReturnBookRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.Internal, "Internal server error")));

		// Act
		var response = await _client.PostAsJsonAsync("/circulation/return", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}
}
