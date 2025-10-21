// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Library.Application.Behaviors;

/// <summary>
/// Logs request information, response, and execution duration
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> _logger)
	: IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		var requestName = typeof(TRequest).Name;
		var stopwatch = Stopwatch.StartNew();
		_logger.LogInformation("Handling {RequestName}: {Request}", requestName, request);

		try
		{
			var response = await next(cancellationToken);
			stopwatch.Stop();

			// Information level for successful requests
			_logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);

			return response;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();

			// Warning level for exceptions. // TODO: Or meybe make this Error level?
			_logger.LogWarning(ex, "Failed to handle {RequestName} after {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);

			throw;
		}
	}
}
