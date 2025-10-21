// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Grpc.Core;

using Library.Api.Models;
using Library.Contracts.Inventory.V1;

using Moq;

namespace Library.SystemTests.Api;

public class InventoryEndpointsTests : IClassFixture<ApiTestFactory>, IDisposable
{
	private readonly ApiTestFactory _factory;
	private readonly HttpClient _client;

	public InventoryEndpointsTests(ApiTestFactory factory)
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
	public async Task GetMostBorrowedBooks_WithValidParameters_ShouldReturnOk()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";
		var limit = 10;

		var grpcResponse = new MostBorrowedResponse();
		grpcResponse.Items.Add(new BookCount
		{
			BookId = Guid.NewGuid().ToString(),
			Title = "Clean Architecture",
			Author = "Robert C. Martin",
			PageCount = 432,
			BorrowCount = 25
		});
		grpcResponse.Items.Add(new BookCount
		{
			BookId = Guid.NewGuid().ToString(),
			Title = "Domain-Driven Design",
			Author = "Eric Evans",
			PageCount = 560,
			BorrowCount = 20
		});

		_factory.InventoryServiceMock
			.Setup(x => x.GetMostBorrowedBooksAsync(
				It.IsAny<MostBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<MostBorrowedResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.GetAsync($"/inventory/most-borrowed?start={start}&end={end}&limit={limit}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<MostBorrowedBooksResponse>();
		result.Should().NotBeNull();
		result!.Items.Should().HaveCount(2);
		result.Items[0].Title.Should().Be("Clean Architecture");
		result.Items[0].BorrowCount.Should().Be(25);
		result.Items[1].Title.Should().Be("Domain-Driven Design");
		result.Items[1].BorrowCount.Should().Be(20);

		_factory.InventoryServiceMock.Verify(x => x.GetMostBorrowedBooksAsync(
			It.Is<MostBorrowedRequest>(r => r.Top == limit && r.Range.Start == start && r.Range.End == end),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetMostBorrowedBooks_WithInvalidDateFormat_ShouldReturnBadRequest()
	{
		// Arrange
		var start = "invalid-date";
		var end = "2024-12-31T23:59:59Z";

		// Act
		var response = await _client.GetAsync($"/inventory/most-borrowed?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("date");
	}

	[Fact]
	public async Task GetMostBorrowedBooks_WithNegativeLimit_ShouldUseDefaultLimit()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";
		var limit = -5;

		var grpcResponse = new MostBorrowedResponse();

		_factory.InventoryServiceMock
			.Setup(x => x.GetMostBorrowedBooksAsync(
				It.IsAny<MostBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<MostBorrowedResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.GetAsync($"/inventory/most-borrowed?start={start}&end={end}&limit={limit}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify that the limit was clamped to 10 (default)
		_factory.InventoryServiceMock.Verify(x => x.GetMostBorrowedBooksAsync(
			It.Is<MostBorrowedRequest>(r => r.Top == 10),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetMostBorrowedBooks_WithLimitOver100_ShouldClampTo100()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";
		var limit = 500;

		var grpcResponse = new MostBorrowedResponse();

		_factory.InventoryServiceMock
			.Setup(x => x.GetMostBorrowedBooksAsync(
				It.IsAny<MostBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<MostBorrowedResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.GetAsync($"/inventory/most-borrowed?start={start}&end={end}&limit={limit}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify that the limit was clamped to 100
		_factory.InventoryServiceMock.Verify(x => x.GetMostBorrowedBooksAsync(
			It.Is<MostBorrowedRequest>(r => r.Top == 100),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetMostBorrowedBooks_WhenGrpcThrowsInvalidArgument_ShouldReturnBadRequest()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.InventoryServiceMock
			.Setup(x => x.GetMostBorrowedBooksAsync(
				It.IsAny<MostBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.InvalidArgument, "Invalid date range")));

		// Act
		var response = await _client.GetAsync($"/inventory/most-borrowed?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Invalid date range");
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WithValidParameters_ShouldReturnOk()
	{
		// Arrange
		var bookId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";
		var limit = 5;

		var grpcResponse = new AlsoBorrowedResponse();
		grpcResponse.Items.Add(new AlsoBorrowedItem
		{
			BookId = Guid.NewGuid().ToString(),
			Title = "The Pragmatic Programmer",
			Author = "Andrew Hunt",
			CoBorrowCount = 15
		});

		_factory.InventoryServiceMock
			.Setup(x => x.GetAlsoBorrowedBooksAsync(
				It.IsAny<AlsoBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<AlsoBorrowedResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.GetAsync($"/books/{bookId}/also-borrowed?start={start}&end={end}&limit={limit}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<AlsoBorrowedBooksResponse>();
		result.Should().NotBeNull();
		result!.Items.Should().HaveCount(1);
		result.Items[0].Title.Should().Be("The Pragmatic Programmer");
		result.Items[0].CoBorrowCount.Should().Be(15);

		_factory.InventoryServiceMock.Verify(x => x.GetAlsoBorrowedBooksAsync(
			It.Is<AlsoBorrowedRequest>(r => r.BookId == bookId && r.Top == limit),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WithInvalidDateFormat_ShouldReturnBadRequest()
	{
		// Arrange
		var bookId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "invalid-date";

		// Act
		var response = await _client.GetAsync($"/books/{bookId}/also-borrowed?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("date");
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WithNonExistentBook_ShouldReturnEmptyResult()
	{
		// Arrange
		var bookId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		var grpcResponse = new AlsoBorrowedResponse();

		_factory.InventoryServiceMock
			.Setup(x => x.GetAlsoBorrowedBooksAsync(
				It.IsAny<AlsoBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<AlsoBorrowedResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => [],
				() => { }));

		// Act
		var response = await _client.GetAsync($"/books/{bookId}/also-borrowed?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<AlsoBorrowedBooksResponse>();
		result.Should().NotBeNull();
		result!.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WhenGrpcThrowsNotFound_ShouldReturnNotFound()
	{
		// Arrange
		var bookId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.InventoryServiceMock
			.Setup(x => x.GetAlsoBorrowedBooksAsync(
				It.IsAny<AlsoBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.NotFound, "Book not found")));

		// Act
		var response = await _client.GetAsync($"/books/{bookId}/also-borrowed?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Book not found");
	}

	[Fact]
	public async Task GetAlsoBorrowedBooks_WhenGrpcThrowsInternalError_ShouldReturnInternalServerError()
	{
		// Arrange
		var bookId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.InventoryServiceMock
			.Setup(x => x.GetAlsoBorrowedBooksAsync(
				It.IsAny<AlsoBorrowedRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.Internal, "Internal server error")));

		// Act
		var response = await _client.GetAsync($"/books/{bookId}/also-borrowed?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}
}
