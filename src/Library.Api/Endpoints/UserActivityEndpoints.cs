// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Grpc.Core;

using Library.Contracts.UserActivity.V1;

using Mapster;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Endpoints;

/// <summary>
/// Extension methods for mapping user activity-related endpoints.
/// </summary>
public static class UserActivityEndpoints
{
	/// <summary>
	/// Maps user activity endpoints to the application.
	/// </summary>
	/// <param name="app">The web application.</param>
	/// <returns>The web application for chaining.</returns>
	public static WebApplication MapUserActivityEndpoints(this WebApplication app)
	{
		var group = app.MapGroup("/users")
				.WithTags("User Activity")
				.WithOpenApi();

		group.MapGet("/top-borrowers", GetTopBorrowers)
				.WithName("GetTopBorrowers")
				.WithSummary("Get the top borrowers within a time range")
				.WithDescription("Returns a list of users who borrowed the most books within the specified time range, ordered by borrow count.")
				.Produces<Models.TopBorrowersResponse>(StatusCodes.Status200OK)
				.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
				.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

		group.MapGet("/{userId}/reading-pace", GetReadingPace)
				.WithName("GetReadingPace")
				.WithSummary("Get a user's reading pace analytics")
				.WithDescription("Returns reading pace analytics for a specific user, including pages per day for each completed book.")
				.Produces<Models.ReadingPaceResponse>(StatusCodes.Status200OK)
				.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
				.Produces<ProblemDetails>(StatusCodes.Status404NotFound)
				.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

		return app;
	}

	/// <summary>
	/// Gets the top borrowers within a time range.
	/// </summary>
	/// <param name="client">The user activity service gRPC client.</param>
	/// <param name="start">Start date in ISO 8601 format (e.g., 2024-01-01T00:00:00Z).</param>
	/// <param name="end">End date in ISO 8601 format (e.g., 2024-12-31T23:59:59Z).</param>
	/// <param name="limit">Maximum number of results to return (default: 10, max: 100).</param>
	/// <param name="logger">The logger instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A list of the top borrowers.</returns>
	private static async Task<IResult> GetTopBorrowers(
			[FromServices] UserActivityService.UserActivityServiceClient client,
			[FromQuery] string start,
			[FromQuery] string end,
			[FromQuery] int limit = 10,
			[FromServices] ILogger<UserActivityService.UserActivityServiceClient> logger = null!,
			CancellationToken cancellationToken = default)
	{
		try
		{
			// Validate parameters
			if (string.IsNullOrWhiteSpace(start))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'start' parameter is required and must be a valid ISO 8601 date."
					},
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

			var request = new TopBorrowersRequest
			{
				Top = limit,
				Range = new Contracts.UserActivity.V1.TimeRange
				{
					Start = start,
					End = end
				}
			};

			logger?.LogInformation("Calling gRPC GetTopBorrowers with start={Start}, end={End}, limit={Limit}", start, end, limit);

			var response = await client.GetTopBorrowersAsync(request, cancellationToken: cancellationToken);

			var result = response.Adapt<Models.TopBorrowersResponse>();

			return Results.Ok(result);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
		{
			logger?.LogWarning(ex, "Invalid argument for GetTopBorrowers");
			return Results.BadRequest(new ProblemDetails
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Invalid request",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
		{
			logger?.LogWarning(ex, "Resource not found for GetTopBorrowers");
			return Results.NotFound(new ProblemDetails
			{
				Status = StatusCodes.Status404NotFound,
				Title = "Not found",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex)
		{
			logger?.LogError(ex, "gRPC error calling GetTopBorrowers: {StatusCode}", ex.StatusCode);
			return TypedResults.Problem(
					detail: ex.Status.Detail,
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Service error");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Unexpected error calling GetTopBorrowers");
			return TypedResults.Problem(
					detail: "An unexpected error occurred while processing your request.",
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Internal server error");
		}
	}

	/// <summary>
	/// Gets reading pace analytics for a specific user.
	/// </summary>
	/// <param name="client">The user activity service gRPC client.</param>
	/// <param name="userId">The ID of the user.</param>
	/// <param name="start">Start date in ISO 8601 format (e.g., 2024-01-01T00:00:00Z).</param>
	/// <param name="end">End date in ISO 8601 format (e.g., 2024-12-31T23:59:59Z).</param>
	/// <param name="logger">The logger instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Reading pace analytics for the user.</returns>
	private static async Task<IResult> GetReadingPace(
			[FromServices] UserActivityService.UserActivityServiceClient client,
			[FromRoute] string userId,
			[FromQuery] string start,
			[FromQuery] string end,
			[FromServices] ILogger<UserActivityService.UserActivityServiceClient> logger = null!,
			CancellationToken cancellationToken = default)
	{
		try
		{
			// Validate parameters
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'userId' parameter is required."
					},
					statusCode: StatusCodes.Status400BadRequest);
			}

			if (string.IsNullOrWhiteSpace(start))
			{
				return Results.Json(
					new ProblemDetails
					{
						Status = StatusCodes.Status400BadRequest,
						Title = "Invalid request",
						Detail = "The 'start' parameter is required and must be a valid ISO 8601 date."
					},
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

			var request = new ReadingPaceRequest
			{
				UserId = userId,
				Range = new Contracts.UserActivity.V1.TimeRange
				{
					Start = start,
					End = end
				}
			};

			logger?.LogInformation("Calling gRPC GetReadingPace for userId={UserId}, start={Start}, end={End}",
					userId, start, end);

			var response = await client.GetReadingPaceAsync(request, cancellationToken: cancellationToken);

			var result = response.Adapt<Models.ReadingPaceResponse>();

			return Results.Ok(result);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
		{
			logger?.LogWarning(ex, "Invalid argument for GetReadingPace");
			return Results.BadRequest(new ProblemDetails
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Invalid request",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
		{
			logger?.LogWarning(ex, "User not found for GetReadingPace: {UserId}", userId);
			return Results.NotFound(new ProblemDetails
			{
				Status = StatusCodes.Status404NotFound,
				Title = "Not found",
				Detail = ex.Status.Detail
			});
		}
		catch (RpcException ex)
		{
			logger?.LogError(ex, "gRPC error calling GetReadingPace: {StatusCode}", ex.StatusCode);
			return TypedResults.Problem(
					detail: ex.Status.Detail,
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Service error");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Unexpected error calling GetReadingPace");
			return TypedResults.Problem(
					detail: "An unexpected error occurred while processing your request.",
					statusCode: StatusCodes.Status500InternalServerError,
					title: "Internal server error");
		}
	}
}
