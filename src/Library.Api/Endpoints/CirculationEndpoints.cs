// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Grpc.Core;

using Library.Contracts.Circulation.V1;

using Mapster;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Endpoints;

/// <summary>
/// Extension methods for mapping circulation-related endpoints.
/// </summary>
public static class CirculationEndpoints
{
	/// <summary>
	/// Maps circulation endpoints to the application.
	/// </summary>
	/// <param name="app">The web application.</param>
	/// <returns>The web application for chaining.</returns>
	public static WebApplication MapCirculationEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/circulation")
				.WithTags("Circulation")
				.WithOpenApi();

		group.MapPost("/borrow", BorrowBook)
				.WithName("BorrowBook")
				.WithSummary("Borrow a book")
				.WithDescription("Records a book borrowing transaction and returns a loan ID.")
				.Produces<Models.BorrowBookResponse>(StatusCodes.Status200OK)
				.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
				.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

		group.MapPost("/return", ReturnBook)
				.WithName("ReturnBook")
				.WithSummary("Return a borrowed book")
				.WithDescription("Records a book return transaction for an existing loan.")
				.Produces<Models.ReturnBookResponse>(StatusCodes.Status200OK)
				.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
				.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
				.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

		return app;
	}

	/// <summary>
	/// Borrows a book for a user.
	/// </summary>
	/// <param name="client">The circulation service gRPC client.</param>
	/// <param name="request">The borrow book request.</param>
	/// <param name="logger">The logger instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A loan ID for the borrowing transaction.</returns>
	private static async Task<IResult> BorrowBook(
			[FromServices] CirculationService.CirculationServiceClient client,
			[FromBody] Models.BorrowBookRequest request,
			[FromServices] ILogger<CirculationService.CirculationServiceClient> logger = null!,
			CancellationToken cancellationToken = default)
	{
		try
		{
			// Validate request
			if (string.IsNullOrWhiteSpace(request.UserId))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'userId' field is required."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			if (string.IsNullOrWhiteSpace(request.BookId))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'bookId' field is required."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			// Use current time if borrowedAt is not provided
			var borrowedAt = request.BorrowedAt ?? DateTime.UtcNow.ToString("O");

			// Validate date format if provided
			if (!DateTime.TryParse(borrowedAt, out _))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'borrowedAt' field must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			var grpcRequest = new Contracts.Circulation.V1.BorrowBookRequest
			{
				UserId = request.UserId,
				BookId = request.BookId,
				BorrowedAt = borrowedAt
			};

			logger?.LogInformation("Calling gRPC BorrowBook for userId={UserId}, bookId={BookId}, borrowedAt={BorrowedAt}",
					request.UserId, request.BookId, borrowedAt);

			var response = await client.BorrowBookAsync(grpcRequest, cancellationToken: cancellationToken);

			var result = response.Adapt<Models.BorrowBookResponse>();

			return Results.Ok(result);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
		{
			logger?.LogWarning(ex, "Invalid argument for BorrowBook");
			return Results.BadRequest(new ProblemDetails
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Invalid request",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
		{
			logger?.LogWarning(ex, "Resource not found for BorrowBook");
			return Results.NotFound(new ProblemDetails
			{
				Status = StatusCodes.Status404NotFound,
				Title = "Not found",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
		{
			logger?.LogWarning(ex, "Resource already exists for BorrowBook");
			return Results.Conflict(new ProblemDetails
			{
				Status = StatusCodes.Status409Conflict,
				Title = "Conflict",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex)
		{
			logger?.LogError(ex, "gRPC error calling BorrowBook: {StatusCode}", ex.StatusCode);
			return TypedResults.Problem(
					detail: ex.Status.Detail,
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Service error");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Unexpected error calling BorrowBook");
			return TypedResults.Problem(
					detail: "An unexpected error occurred while processing your request.",
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Internal server error");
		}
	}

	/// <summary>
	/// Returns a borrowed book.
	/// </summary>
	/// <param name="client">The circulation service gRPC client.</param>
	/// <param name="request">The return book request.</param>
	/// <param name="logger">The logger instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A success indicator for the return transaction.</returns>
	private static async Task<IResult> ReturnBook(
			[FromServices] CirculationService.CirculationServiceClient client,
			[FromBody] Models.ReturnBookRequest request,
			[FromServices] ILogger<CirculationService.CirculationServiceClient> logger = null!,
			CancellationToken cancellationToken = default)
	{
		try
		{
			// Validate request
			if (string.IsNullOrWhiteSpace(request.LoanId))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'loanId' field is required."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			// Use current time if returnedAt is not provided
			var returnedAt = request.ReturnedAt ?? DateTime.UtcNow.ToString("O");

			// Validate date format if provided
			if (!DateTime.TryParse(returnedAt, out _))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'returnedAt' field must be a valid ISO 8601 date."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			var grpcRequest = new Contracts.Circulation.V1.ReturnBookRequest
			{
				LoanId = request.LoanId,
				ReturnedAt = returnedAt
			};

			logger?.LogInformation("Calling gRPC ReturnBook for loanId={LoanId}, returnedAt={ReturnedAt}",
					request.LoanId, returnedAt);

			var response = await client.ReturnBookAsync(grpcRequest, cancellationToken: cancellationToken);

			var result = response.Adapt<Models.ReturnBookResponse>();

			return Results.Ok(result);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
		{
			logger?.LogWarning(ex, "Invalid argument for ReturnBook");
			return Results.BadRequest(new ProblemDetails
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Invalid request",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
		{
			logger?.LogWarning(ex, "Loan not found for ReturnBook: {LoanId}", request.LoanId);
			return Results.NotFound(new ProblemDetails
			{
				Status = StatusCodes.Status404NotFound,
				Title = "Not found",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex)
		{
			logger?.LogError(ex, "gRPC error calling ReturnBook: {StatusCode}", ex.StatusCode);
			return TypedResults.Problem(
					detail: ex.Status.Detail,
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Service error");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Unexpected error calling ReturnBook");
			return TypedResults.Problem(
					detail: "An unexpected error occurred while processing your request.",
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Internal server error");
		}
	}
}
