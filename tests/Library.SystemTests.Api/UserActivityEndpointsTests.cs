// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Grpc.Core;

using Library.Contracts.UserActivity.V1;

using Moq;

namespace Library.SystemTests.Api;

public class UserActivityEndpointsTests : IClassFixture<ApiTestFactory>, IDisposable
{
	private readonly ApiTestFactory _factory;
	private readonly HttpClient _client;

	public UserActivityEndpointsTests(ApiTestFactory factory)
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
	public async Task GetTopBorrowers_WithValidParameters_ShouldReturnOk()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";
		var limit = 10;

		var grpcResponse = new Library.Contracts.UserActivity.V1.TopBorrowersResponse();
		grpcResponse.Items.Add(new Borrower
		{
			UserId = Guid.NewGuid().ToString(),
			FullName = "Alice Johnson",
			BorrowCount = 45
		});
		grpcResponse.Items.Add(new Borrower
		{
			UserId = Guid.NewGuid().ToString(),
			FullName = "Bob Smith",
			BorrowCount = 30
		});
		grpcResponse.Items.Add(new Borrower
		{
			UserId = Guid.NewGuid().ToString(),
			FullName = "Charlie Brown",
			BorrowCount = 25
		});

		_factory.UserActivityServiceMock
			.Setup(x => x.GetTopBorrowersAsync(
				It.IsAny<TopBorrowersRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.UserActivity.V1.TopBorrowersResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => new Grpc.Core.Metadata(),
				() => { }));

		// Act
		var response = await _client.GetAsync($"/users/top-borrowers?start={start}&end={end}&limit={limit}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<Library.Api.Models.TopBorrowersResponse>();
		result.Should().NotBeNull();
		result!.Items.Should().HaveCount(3);
		result.Items[0].FullName.Should().Be("Alice Johnson");
		result.Items[0].BorrowCount.Should().Be(45);
		result.Items[1].FullName.Should().Be("Bob Smith");
		result.Items[1].BorrowCount.Should().Be(30);

		_factory.UserActivityServiceMock.Verify(x => x.GetTopBorrowersAsync(
			It.Is<TopBorrowersRequest>(r => r.Top == limit && r.Range.Start == start && r.Range.End == end),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetTopBorrowers_WithInvalidDateFormat_ShouldReturnBadRequest()
	{
		// Arrange
		var start = "invalid-date";
		var end = "2024-12-31T23:59:59Z";

		// Act
		var response = await _client.GetAsync($"/users/top-borrowers?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("date");
	}

	[Fact]
	public async Task GetTopBorrowers_WithNegativeLimit_ShouldUseDefaultLimit()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";
		var limit = -5;

		var grpcResponse = new Library.Contracts.UserActivity.V1.TopBorrowersResponse();

		_factory.UserActivityServiceMock
			.Setup(x => x.GetTopBorrowersAsync(
				It.IsAny<TopBorrowersRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.UserActivity.V1.TopBorrowersResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => new Grpc.Core.Metadata(),
				() => { }));

		// Act
		var response = await _client.GetAsync($"/users/top-borrowers?start={start}&end={end}&limit={limit}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify that the limit was clamped to 10 (default)
		_factory.UserActivityServiceMock.Verify(x => x.GetTopBorrowersAsync(
			It.Is<TopBorrowersRequest>(r => r.Top == 10),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetTopBorrowers_WithLimitOver100_ShouldClampTo100()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";
		var limit = 500;

		var grpcResponse = new Library.Contracts.UserActivity.V1.TopBorrowersResponse();

		_factory.UserActivityServiceMock
			.Setup(x => x.GetTopBorrowersAsync(
				It.IsAny<TopBorrowersRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.UserActivity.V1.TopBorrowersResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => new Grpc.Core.Metadata(),
				() => { }));

		// Act
		var response = await _client.GetAsync($"/users/top-borrowers?start={start}&end={end}&limit={limit}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify that the limit was clamped to 100
		_factory.UserActivityServiceMock.Verify(x => x.GetTopBorrowersAsync(
			It.Is<TopBorrowersRequest>(r => r.Top == 100),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetTopBorrowers_WhenGrpcThrowsInvalidArgument_ShouldReturnBadRequest()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.UserActivityServiceMock
			.Setup(x => x.GetTopBorrowersAsync(
				It.IsAny<TopBorrowersRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.InvalidArgument, "Invalid date range")));

		// Act
		var response = await _client.GetAsync($"/users/top-borrowers?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Invalid date range");
	}

	[Fact]
	public async Task GetTopBorrowers_WhenGrpcThrowsInternalError_ShouldReturnInternalServerError()
	{
		// Arrange
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.UserActivityServiceMock
			.Setup(x => x.GetTopBorrowersAsync(
				It.IsAny<TopBorrowersRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.Internal, "Internal server error")));

		// Act
		var response = await _client.GetAsync($"/users/top-borrowers?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}

	[Fact]
	public async Task GetReadingPace_WithValidParameters_ShouldReturnOk()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		var grpcResponse = new Library.Contracts.UserActivity.V1.ReadingPaceResponse
		{
			AggregatePagesPerDay = 42.5
		};
		grpcResponse.Items.Add(new ReadingPaceItem
		{
			BookId = Guid.NewGuid().ToString(),
			Title = "Clean Architecture",
			Pages = 432,
			Days = 10,
			PagesPerDay = 43.2,
			BorrowedAt = "2024-01-05T00:00:00Z",
			ReturnedAt = "2024-01-15T00:00:00Z"
		});
		grpcResponse.Items.Add(new ReadingPaceItem
		{
			BookId = Guid.NewGuid().ToString(),
			Title = "Domain-Driven Design",
			Pages = 560,
			Days = 14,
			PagesPerDay = 40.0,
			BorrowedAt = "2024-02-01T00:00:00Z",
			ReturnedAt = "2024-02-15T00:00:00Z"
		});

		_factory.UserActivityServiceMock
			.Setup(x => x.GetReadingPaceAsync(
				It.IsAny<ReadingPaceRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.UserActivity.V1.ReadingPaceResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => new Grpc.Core.Metadata(),
				() => { }));

		// Act
		var response = await _client.GetAsync($"/users/{userId}/reading-pace?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<Library.Api.Models.ReadingPaceResponse>();
		result.Should().NotBeNull();
		result!.AggregatePagesPerDay.Should().Be(42.5);
		result.Items.Should().HaveCount(2);
		result.Items[0].Title.Should().Be("Clean Architecture");
		result.Items[0].PagesPerDay.Should().Be(43.2);
		result.Items[1].Title.Should().Be("Domain-Driven Design");
		result.Items[1].PagesPerDay.Should().Be(40.0);

		_factory.UserActivityServiceMock.Verify(x => x.GetReadingPaceAsync(
			It.Is<ReadingPaceRequest>(r => r.UserId == userId && r.Range.Start == start && r.Range.End == end),
			It.IsAny<Grpc.Core.Metadata>(),
			It.IsAny<DateTime?>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetReadingPace_WithInvalidDateFormat_ShouldReturnBadRequest()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "invalid-date";

		// Act
		var response = await _client.GetAsync($"/users/{userId}/reading-pace?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("date");
	}

	[Fact]
	public async Task GetReadingPace_WithNonExistentUser_ShouldReturnEmptyResult()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		var grpcResponse = new Library.Contracts.UserActivity.V1.ReadingPaceResponse
		{
			AggregatePagesPerDay = 0
		};

		_factory.UserActivityServiceMock
			.Setup(x => x.GetReadingPaceAsync(
				It.IsAny<ReadingPaceRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Returns(new AsyncUnaryCall<Library.Contracts.UserActivity.V1.ReadingPaceResponse>(
				Task.FromResult(grpcResponse),
				Task.FromResult(new Grpc.Core.Metadata()),
				() => Status.DefaultSuccess,
				() => new Grpc.Core.Metadata(),
				() => { }));

		// Act
		var response = await _client.GetAsync($"/users/{userId}/reading-pace?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var result = await response.Content.ReadFromJsonAsync<Library.Api.Models.ReadingPaceResponse>();
		result.Should().NotBeNull();
		result!.Items.Should().BeEmpty();
		result.AggregatePagesPerDay.Should().Be(0);
	}

	[Fact]
	public async Task GetReadingPace_WhenGrpcThrowsInvalidArgument_ShouldReturnBadRequest()
	{
		// Arrange
		var userId = "invalid-guid";
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.UserActivityServiceMock
			.Setup(x => x.GetReadingPaceAsync(
				It.IsAny<ReadingPaceRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID")));

		// Act
		var response = await _client.GetAsync($"/users/{userId}/reading-pace?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("Invalid user ID");
	}

	[Fact]
	public async Task GetReadingPace_WhenGrpcThrowsNotFound_ShouldReturnNotFound()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.UserActivityServiceMock
			.Setup(x => x.GetReadingPaceAsync(
				It.IsAny<ReadingPaceRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.NotFound, "User not found")));

		// Act
		var response = await _client.GetAsync($"/users/{userId}/reading-pace?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
		problem.Should().NotBeNull();
		problem!.Detail.Should().Contain("User not found");
	}

	[Fact]
	public async Task GetReadingPace_WhenGrpcThrowsInternalError_ShouldReturnInternalServerError()
	{
		// Arrange
		var userId = Guid.NewGuid().ToString();
		var start = "2024-01-01T00:00:00Z";
		var end = "2024-12-31T23:59:59Z";

		_factory.UserActivityServiceMock
			.Setup(x => x.GetReadingPaceAsync(
				It.IsAny<ReadingPaceRequest>(),
				It.IsAny<Grpc.Core.Metadata>(),
				It.IsAny<DateTime?>(),
				It.IsAny<CancellationToken>()))
			.Throws(new RpcException(new Status(StatusCode.Internal, "Internal server error")));

		// Act
		var response = await _client.GetAsync($"/users/{userId}/reading-pace?start={start}&end={end}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
	}
}
