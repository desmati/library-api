// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Repositories;
using Library.Infrastructure.Data;
using Library.Infrastructure.Queries;
using Library.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		// Read connection string from configuration (supports both Aspire and traditional config)
		var connectionString = configuration.GetConnectionString("libdb")
			?? configuration.GetConnectionString("LibraryDb")
			?? throw new InvalidOperationException("Connection string 'libdb' or 'LibraryDb' not found.");

		services.AddDbContext<LibraryDbContext>(options =>
		{
			options.UseNpgsql(connectionString, npgsqlOptions =>
			{
				npgsqlOptions.EnableRetryOnFailure(
					maxRetryCount: 3,
					maxRetryDelay: TimeSpan.FromSeconds(5),
					errorCodesToAdd: null);
			});

			if (bool.Parse(configuration["DetailedErrors"] ?? bool.FalseString))
			{
				options.EnableSensitiveDataLogging();
				options.EnableDetailedErrors();
			}
		});

		services.AddScoped<IBookRepository, BookRepository>();
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<ILoanRepository, LoanRepository>();

		services.AddScoped<IQueryService, QueryService>();

		return services;
	}
}
