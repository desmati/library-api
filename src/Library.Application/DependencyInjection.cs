// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Reflection;

using FluentValidation;

using Library.Application.Behaviors;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Library.Application;

/// <summary>
/// "Application" services config
/// </summary>
public static class DependencyInjection
{
	/// <summary>
	/// Adds Application layer services to the DI container.
	/// </summary>
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		var assembly = Assembly.GetExecutingAssembly();

		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
		services.AddValidatorsFromAssembly(assembly);
		services.AddMemoryCache();

		// Pipeline behaviors in order:
		// 1. Logging - logs all requests
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

		// 2. Validation - validates requests and throws on failure
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

		// 3. Caching - caches query results for ICacheableQuery implementations
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

		return services;
	}
}
