// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentValidation;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Library.Domain.Exceptions;

namespace Library.Grpc.Interceptors;

/// <summary>
/// Centralized exception handling for all gRPC calls
/// </summary>
public class ExceptionInterceptor(ILogger<ExceptionInterceptor> _logger)
	: Interceptor
{
	public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
		TRequest request,
		ServerCallContext context,
		UnaryServerMethod<TRequest, TResponse> continuation)
	{
		try
		{
			return await continuation(request, context);
		}
		catch (Exception ex)
		{
			throw TransverseException(ex);
		}
	}

	private RpcException TransverseException(Exception exception)
	{
		_logger.LogError(exception, "An error occurred processing gRPC request");

		return exception switch
		{
			ValidationException validationEx => new RpcException(
				new(StatusCode.InvalidArgument, string.Join("; ", validationEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))),
				CreateExceptionMetadata(exception)),

			EntityNotFoundException notFoundEx => new RpcException(
				new(StatusCode.NotFound, notFoundEx.Message),
				CreateExceptionMetadata(exception)),

			InvalidOperationDomainException invalidOpEx => new RpcException(
				new(StatusCode.FailedPrecondition, invalidOpEx.Message),
				CreateExceptionMetadata(exception)),

			DomainException domainEx => new RpcException(
				new(StatusCode.FailedPrecondition, domainEx.Message),
				CreateExceptionMetadata(exception)),

			_ => new RpcException(
				new(StatusCode.Internal, "An internal error occurred"),
				CreateExceptionMetadata(exception))
		};

		static Metadata CreateExceptionMetadata(Exception exception)
		{
			var metadata = new Metadata { { "exception-type", exception.GetType().Name } };

			if (!string.IsNullOrEmpty(exception.Message))
			{
				metadata.Add("exception-message", exception.Message);
			}

			return metadata;
		}
	}
}
