// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentValidation;

using MediatR;

namespace Library.Application.Behaviors;

/// <summary>
/// Validates requests using FluentValidation validators
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> _validators)
	: IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		if (!_validators.Any())
		{
			return await next(cancellationToken);
		}

		var context = new ValidationContext<TRequest>(request);
		var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

		var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

		if (failures.Count != 0)
		{
			throw new ValidationException(failures);
		}

		return await next(cancellationToken);
	}
}
