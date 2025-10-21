// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Grpc.Core;

using Library.Api.Models;
using Library.Contracts.Inventory.V1;

using Mapster;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Endpoints;

/// <summary>
/// Extension methods for mapping inventory-related endpoints.
/// </summary>
public static class InventoryEndpoints
{
	/// <summary>
	/// Maps inventory endpoints to the application.
	/// </summary>
	/// <param name="app">The web application.</param>
	/// <returns>The web application for chaining.</returns>
	public static WebApplication MapInventoryEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/inventory")
				.WithTags("Inventory")
				.WithOpenApi();

		group.MapGet("/most-borrowed", GetMostBorrowedBooks)
				.WithName("GetMostBorrowedBooks")
				.WithSummary("Get the most borrowed books within a time range")
				.WithDescription("Returns a list of the most borrowed books within the specified time range, ordered by borrow count.")
				.Produces<MostBorrowedBooksResponse>(StatusCodes.Status200OK)
				.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
				.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

		var booksGroup = app.MapGroup("/books")
				.WithTags("Inventory")
				.WithOpenApi();

		booksGroup.MapGet("/{bookId}/also-borrowed", GetAlsoBorrowedBooks)
				.WithName("GetAlsoBorrowedBooks")
				.WithSummary("Get books that were also borrowed by users who borrowed a specific book")
				.WithDescription("Returns a list of books that were borrowed by users who also borrowed the specified book, ordered by co-borrow count.")
				.Produces<AlsoBorrowedBooksResponse>(StatusCodes.Status200OK)
				.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
				.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
				.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

		return app;
	}

	/// <summary>
	/// Gets the most borrowed books within a time range.
	/// </summary>
	/// <param name="client">The inventory service gRPC client.</param>
	/// <param name="start">Start date in ISO 8601 format (e.g., 2024-01-01T00:00:00Z).</param>
	/// <param name="end">End date in ISO 8601 format (e.g., 2024-12-31T23:59:59Z).</param>
	/// <param name="limit">Maximum number of results to return (default: 10, max: 100).</param>
	/// <param name="logger">The logger instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A list of the most borrowed books.</returns>
	private static async Task<IResult> GetMostBorrowedBooks(
			[FromServices] InventoryService.InventoryServiceClient client,
			[FromQuery] string start,
			[FromQuery] string end,
			[FromQuery] int limit = 10,
			[FromServices] ILogger<InventoryService.InventoryServiceClient> logger = null!,
			CancellationToken cancellationToken = default)
	{
		try
		{
			// Validate parameters
			if (string.IsNullOrWhiteSpace(start))
			{
				return Results.Json(
					new
					{
						status = StatusCodes.Status400BadRequest,
						title = "Invalid request",
						detail = "The 'start' parameter is required and must be a valid ISO 8601 date."
					},
					contentType: "application/problem+json",
					statusCode: StatusCodes.Status400BadRequest);
			}

			if (string.IsNullOrWhiteSpace(end))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'end' parameter is required and must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			// Validate and clamp limit
			if (limit < 1)
			{
				limit = 10;
			}
			else if (limit > 100)
			{
				limit = 100;
			}

			// Validate date format
			if (!DateTime.TryParse(start, out _))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'start' parameter must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			if (!DateTime.TryParse(end, out _))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'end' parameter must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			var request = new MostBorrowedRequest
			{
				Top = limit,
				Range = new Contracts.Inventory.V1.TimeRange
				{
					Start = start,
					End = end
				}
			};

			logger?.LogInformation("Calling gRPC GetMostBorrowedBooks with start={Start}, end={End}, limit={Limit}", start, end, limit);

			var response = await client.GetMostBorrowedBooksAsync(request, cancellationToken: cancellationToken);

			var result = response.Adapt<MostBorrowedBooksResponse>();

			return Results.Ok(result);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
		{
			logger?.LogWarning(ex, "Invalid argument for GetMostBorrowedBooks");
			return Results.BadRequest(new ProblemDetails
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Invalid request",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
		{
			logger?.LogWarning(ex, "Resource not found for GetMostBorrowedBooks");
			return Results.NotFound(new ProblemDetails
			{
				Status = StatusCodes.Status404NotFound,
				Title = "Not found",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex)
		{
			logger?.LogError(ex, "gRPC error calling GetMostBorrowedBooks: {StatusCode}", ex.StatusCode);
			return Results.Problem(
					detail: ex.Status.Detail,
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Service error");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Unexpected error calling GetMostBorrowedBooks");
			return Results.Problem(
					detail: "An unexpected error occurred while processing your request.",
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Internal server error");
		}
	}

	/// <summary>
	/// Gets books that were also borrowed by users who borrowed a specific book.
	/// </summary>
	/// <param name="client">The inventory service gRPC client.</param>
	/// <param name="bookId">The ID of the book.</param>
	/// <param name="start">Start date in ISO 8601 format (e.g., 2024-01-01T00:00:00Z).</param>
	/// <param name="end">End date in ISO 8601 format (e.g., 2024-12-31T23:59:59Z).</param>
	/// <param name="limit">Maximum number of results to return (default: 10, max: 100).</param>
	/// <param name="logger">The logger instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A list of books that were also borrowed.</returns>
	private static async Task<IResult> GetAlsoBorrowedBooks(
			[FromServices] InventoryService.InventoryServiceClient client,
			[FromRoute] string bookId,
			[FromQuery] string start,
			[FromQuery] string end,
			[FromQuery] int limit = 10,
			[FromServices] ILogger<InventoryService.InventoryServiceClient> logger = null!,
			CancellationToken cancellationToken = default)
	{
		try
		{
			// Validate parameters
			if (string.IsNullOrWhiteSpace(bookId))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'bookId' parameter is required."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			if (string.IsNullOrWhiteSpace(start))
			{
				return Results.Json(
					new
					{
						status = StatusCodes.Status400BadRequest,
						title = "Invalid request",
						detail = "The 'start' parameter is required and must be a valid ISO 8601 date."
					},
					contentType: "application/problem+json",
					statusCode: StatusCodes.Status400BadRequest);
			}

			if (string.IsNullOrWhiteSpace(end))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'end' parameter is required and must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			// Validate and clamp limit
			if (limit < 1)
			{
				limit = 10;
			}
			else if (limit > 100)
			{
				limit = 100;
			}

			// Validate date format
			if (!DateTime.TryParse(start, out _))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'start' parameter must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			if (!DateTime.TryParse(end, out _))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'end' parameter must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			var request = new AlsoBorrowedRequest
			{
				BookId = bookId,
				Top = limit,
				Range = new Contracts.Inventory.V1.TimeRange
				{
					Start = start,
					End = end
				}
			};

			logger?.LogInformation("Calling gRPC GetAlsoBorrowedBooks for bookId={BookId}, start={Start}, end={End}, limit={Limit}",
					bookId, start, end, limit);

			var response = await client.GetAlsoBorrowedBooksAsync(request, cancellationToken: cancellationToken);

			var result = response.Adapt<AlsoBorrowedBooksResponse>();

			return Results.Ok(result);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
		{
			logger?.LogWarning(ex, "Invalid argument for GetAlsoBorrowedBooks");
			return Results.BadRequest(new ProblemDetails
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Invalid request",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
		{
			logger?.LogWarning(ex, "Book not found for GetAlsoBorrowedBooks: {BookId}", bookId);
			return Results.NotFound(new ProblemDetails
			{
				Status = StatusCodes.Status404NotFound,
				Title = "Not found",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex)
		{
			logger?.LogError(ex, "gRPC error calling GetAlsoBorrowedBooks: {StatusCode}", ex.StatusCode);
			return Results.Problem(
					detail: ex.Status.Detail,
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Service error");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Unexpected error calling GetAlsoBorrowedBooks");
			return Results.Problem(
					detail: "An unexpected error occurred while processing your request.",
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Internal server error");
		}
	}
}
