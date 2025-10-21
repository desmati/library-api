// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using FluentValidation;
using FluentValidation.Results;

using Library.Application.Behaviors;
using Library.Application.Queries;

using MediatR;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Moq;

namespace Library.UnitTests.Application;

public class BehaviorTests
{
	public class ValidationBehaviorTests
	{
		[Fact]
		public async Task Handle_WithNoValidators_ShouldCallNext()
		{
			// Arrange
			var validators = Array.Empty<IValidator<TestRequest>>();
			var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
			var request = new TestRequest("test");
			var expectedResponse = new TestResponse("result");
			var nextCalled = false;

			Task<TestResponse> next(CancellationToken ct = default)
			{
				nextCalled = true;
				return Task.FromResult(expectedResponse);
			}

			// Act
			var result = await behavior.Handle(request, next, CancellationToken.None);

			// Assert
			nextCalled.Should().BeTrue();
			result.Should().Be(expectedResponse);
		}

		[Fact]
		public async Task Handle_WithValidRequest_ShouldCallNext()
		{
			// Arrange
			var validatorMock = new Mock<IValidator<TestRequest>>();
			validatorMock
				.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new ValidationResult());

			var validators = new[] { validatorMock.Object };
			var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
			var request = new TestRequest("test");
			var expectedResponse = new TestResponse("result");
			var nextCalled = false;

			Task<TestResponse> next(CancellationToken ct = default)
			{
				nextCalled = true;
				return Task.FromResult(expectedResponse);
			}

			// Act
			var result = await behavior.Handle(request, next, CancellationToken.None);

			// Assert
			nextCalled.Should().BeTrue();
			result.Should().Be(expectedResponse);
		}

		[Fact]
		public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
		{
			// Arrange
			var validatorMock = new Mock<IValidator<TestRequest>>();
			var failures = new List<ValidationFailure>
			{
				new("Property", "Error message")
			};
			validatorMock
				.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new ValidationResult(failures));

			var validators = new[] { validatorMock.Object };
			var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
			var request = new TestRequest("test");

			static Task<TestResponse> next(CancellationToken ct = default)
			{
				return Task.FromResult(new TestResponse("result"));
			}

			// Act
			var act = async () => await behavior.Handle(request, next, CancellationToken.None);

			// Assert
			await act.Should().ThrowAsync<ValidationException>();
		}

		[Fact]
		public async Task Handle_WithMultipleValidators_ShouldAggregateErrors()
		{
			// Arrange
			var validator1Mock = new Mock<IValidator<TestRequest>>();
			var failures1 = new List<ValidationFailure>
			{
				new("Property1", "Error 1")
			};
			validator1Mock
				.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new ValidationResult(failures1));

			var validator2Mock = new Mock<IValidator<TestRequest>>();
			var failures2 = new List<ValidationFailure>
			{
				new("Property2", "Error 2")
			};
			validator2Mock
				.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new ValidationResult(failures2));

			var validators = new[] { validator1Mock.Object, validator2Mock.Object };
			var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
			var request = new TestRequest("test");

			// Act
			var act = async () => await behavior.Handle(request, (ct) => Task.FromResult(new TestResponse("result")), CancellationToken.None);

			// Assert
			var exception = await act.Should().ThrowAsync<ValidationException>();
			exception.Which.Errors.Should().HaveCount(2);
		}

		[Fact]
		public async Task Handle_WithCancellationToken_ShouldPassToValidators()
		{
			// Arrange
			var validatorMock = new Mock<IValidator<TestRequest>>();
			validatorMock
				.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new ValidationResult());

			var validators = new[] { validatorMock.Object };
			var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
			var request = new TestRequest("test");
			var cancellationToken = new CancellationToken();

			// Act
			await behavior.Handle(request, (ct) => Task.FromResult(new TestResponse("result")), cancellationToken);

			// Assert
			validatorMock.Verify(
				x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken),
				Times.Once);
		}

		public sealed record TestRequest(string Value) : IRequest<TestResponse>;
		public sealed record TestResponse(string Value);
	}

	public class LoggingBehaviorTests
	{
		[Fact]
		public async Task Handle_ShouldLogRequestInformation()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
			var behavior = new LoggingBehavior<TestRequest, TestResponse>(loggerMock.Object);
			var request = new TestRequest("test");
			var expectedResponse = new TestResponse("result");

			// Act
			await behavior.Handle(request, (ct) => Task.FromResult(expectedResponse), CancellationToken.None);

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling")),
					null,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task Handle_OnSuccess_ShouldLogCompletionWithDuration()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
			var behavior = new LoggingBehavior<TestRequest, TestResponse>(loggerMock.Object);
			var request = new TestRequest("test");
			var expectedResponse = new TestResponse("result");

			// Act
			var result = await behavior.Handle(request, (ct) => Task.FromResult(expectedResponse), CancellationToken.None);

			// Assert
			result.Should().Be(expectedResponse);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled")),
					null,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task Handle_OnException_ShouldLogWarningWithDuration()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
			var behavior = new LoggingBehavior<TestRequest, TestResponse>(loggerMock.Object);
			var request = new TestRequest("test");
			var expectedException = new InvalidOperationException("Test exception");

			// Act
			var act = async () => await behavior.Handle(request, (ct) => throw expectedException, CancellationToken.None);

			// Assert
			await act.Should().ThrowAsync<InvalidOperationException>();
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed")),
					expectedException,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task Handle_ShouldReturnResponseFromNext()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
			var behavior = new LoggingBehavior<TestRequest, TestResponse>(loggerMock.Object);
			var request = new TestRequest("test");
			var expectedResponse = new TestResponse("result");

			// Act
			var result = await behavior.Handle(request, (ct) => Task.FromResult(expectedResponse), CancellationToken.None);

			// Assert
			result.Should().Be(expectedResponse);
		}

		public sealed record TestRequest(string Value) : IRequest<TestResponse>;
		public sealed record TestResponse(string Value);
	}

	public class CachingBehaviorTests
	{
		[Fact]
		public async Task Handle_WithNonCacheableRequest_ShouldCallNext()
		{
			// Arrange
			var cache = new MemoryCache(new MemoryCacheOptions());
			var loggerMock = new Mock<ILogger<CachingBehavior<TestRequest, TestResponse>>>();
			var behavior = new CachingBehavior<TestRequest, TestResponse>(cache, loggerMock.Object);
			var request = new TestRequest("test");
			var expectedResponse = new TestResponse("result");
			var nextCalled = false;

			// Act
			var result = await behavior.Handle(request, (ct) =>
			{
				nextCalled = true;
				return Task.FromResult(expectedResponse);
			}, CancellationToken.None);

			// Assert
			nextCalled.Should().BeTrue();
			result.Should().Be(expectedResponse);
		}

		[Fact]
		public async Task Handle_WithCacheableRequest_OnCacheMiss_ShouldCallNextAndCache()
		{
			// Arrange
			var cache = new MemoryCache(new MemoryCacheOptions());
			var loggerMock = new Mock<ILogger<CachingBehavior<CacheableTestRequest, TestResponse>>>();
			var behavior = new CachingBehavior<CacheableTestRequest, TestResponse>(cache, loggerMock.Object);
			var request = new CacheableTestRequest("test");
			var expectedResponse = new TestResponse("result");
			var nextCallCount = 0;

			Task<TestResponse> next(CancellationToken ct = default)
			{
				nextCallCount++;
				return Task.FromResult(expectedResponse);
			}

			// Act
			var result1 = await behavior.Handle(request, next, CancellationToken.None);
			var result2 = await behavior.Handle(request, next, CancellationToken.None);

			// Assert
			result1.Should().Be(expectedResponse);
			result2.Should().Be(expectedResponse);
			nextCallCount.Should().Be(1); // Should only call next once, second time from cache
		}

		[Fact]
		public async Task Handle_WithCacheableRequest_OnCacheHit_ShouldReturnCachedValue()
		{
			// Arrange
			var cache = new MemoryCache(new MemoryCacheOptions());
			var loggerMock = new Mock<ILogger<CachingBehavior<CacheableTestRequest, TestResponse>>>();
			var behavior = new CachingBehavior<CacheableTestRequest, TestResponse>(cache, loggerMock.Object);
			var request = new CacheableTestRequest("test");
			var firstResponse = new TestResponse("result1");
			var secondResponse = new TestResponse("result2");

			var responseIndex = 0;
			Task<TestResponse> next(CancellationToken ct = default)
			{
				return Task.FromResult(responseIndex++ == 0 ? firstResponse : secondResponse);
			}

			// Act
			var result1 = await behavior.Handle(request, next, CancellationToken.None);
			var result2 = await behavior.Handle(request, next, CancellationToken.None);

			// Assert
			result1.Should().Be(firstResponse);
			result2.Should().Be(firstResponse); // Should return cached first response
		}

		[Fact]
		public async Task Handle_WithDifferentCacheableRequests_ShouldCacheSeparately()
		{
			// Arrange
			var cache = new MemoryCache(new MemoryCacheOptions());
			var loggerMock = new Mock<ILogger<CachingBehavior<CacheableTestRequest, TestResponse>>>();
			var behavior = new CachingBehavior<CacheableTestRequest, TestResponse>(cache, loggerMock.Object);
			var request1 = new CacheableTestRequest("test1");
			var request2 = new CacheableTestRequest("test2");
			var response1 = new TestResponse("result1");
			var response2 = new TestResponse("result2");

			var currentRequest = 0;
			Task<TestResponse> next(CancellationToken ct = default)
			{
				return Task.FromResult(currentRequest++ == 0 ? response1 : response2);
			}

			// Act
			var result1 = await behavior.Handle(request1, next, CancellationToken.None);
			var result2 = await behavior.Handle(request2, next, CancellationToken.None);
			var result3 = await behavior.Handle(request1, next, CancellationToken.None);

			// Assert
			result1.Should().Be(response1);
			result2.Should().Be(response2);
			result3.Should().Be(response1); // Should return cached value for request1
		}

		[Fact]
		public async Task Handle_ShouldLogCacheHitsAndMisses()
		{
			// Arrange
			var cache = new MemoryCache(new MemoryCacheOptions());
			var loggerMock = new Mock<ILogger<CachingBehavior<CacheableTestRequest, TestResponse>>>();
			var behavior = new CachingBehavior<CacheableTestRequest, TestResponse>(cache, loggerMock.Object);
			var request = new CacheableTestRequest("test");
			var expectedResponse = new TestResponse("result");

			Task<TestResponse> next(CancellationToken ct = default) => Task.FromResult(expectedResponse);

			// Act
			await behavior.Handle(request, next, CancellationToken.None); // Cache miss
			await behavior.Handle(request, next, CancellationToken.None); // Cache hit

			// Assert - Should log both cache miss and cache hit
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("miss")),
					null,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);

			loggerMock.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("hit")),
					null,
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		public sealed record TestRequest(string Value) : IRequest<TestResponse>;
		public sealed record CacheableTestRequest(string Value) : IRequest<TestResponse>, ICacheableQuery;
		public sealed record TestResponse(string Value);
	}
}
