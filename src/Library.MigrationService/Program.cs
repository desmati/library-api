// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Infrastructure.Data;
using Library.MigrationService;
using Library.ServiceDefaults;

using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
	.WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

// Use Aspire's AddNpgsqlDbContext which handles the connection string from Aspire
builder.AddNpgsqlDbContext<LibraryDbContext>("libdb", configureDbContextOptions: options =>
{
	if (bool.Parse(builder.Configuration["DetailedErrors"] ?? bool.FalseString))
	{
		options.EnableSensitiveDataLogging();
		options.EnableDetailedErrors();
	}
});

builder.Services.AddScoped<DataSeeder>();

var host = builder.Build();
host.Run();
