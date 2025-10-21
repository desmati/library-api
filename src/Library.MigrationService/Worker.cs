// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Diagnostics;

using Library.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace Library.MigrationService;

public class Worker(
	IServiceProvider serviceProvider,
	IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
	public const string ActivitySourceName = "Migrations";
	private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

		using var scope = serviceProvider.CreateScope();

		var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<Worker>>();
		var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

		var strategy = dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async () =>
		{
			logger.LogInformation("Running database migrations...");
			await dbContext.Database.MigrateAsync(stoppingToken);
			logger.LogInformation("Database migrations completed successfully");

			logger.LogInformation("Seeding database...");
			await seeder.SeedAsync(stoppingToken);
			logger.LogInformation("Database seeding completed successfully");
		});

		hostApplicationLifetime.StopApplication();
	}
}
