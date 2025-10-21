// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Grpc.Net.Client;

using Library.Api.Endpoints;
using Library.Contracts.Circulation.V1;
using Library.Contracts.Inventory.V1;
using Library.Contracts.UserActivity.V1;
using Library.ServiceDefaults;

using Mapster;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Problem Details support
builder.Services.AddProblemDetails();

// Swagger - OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
	{
		Title = "Library API",
		Version = "v1",
		Description = "HTTP gateway for Library gRPC services providing inventory, user activity, and circulation endpoints."
	});

	// Including XML comments:
	var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
	if (File.Exists(xmlPath))
	{
		options.IncludeXmlComments(xmlPath);
	}
});

// CORS for development
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

builder.Services.AddMapster();

// gRPC
var libraryGrpcUrl = builder.Configuration["GrpcServices:LibraryGrpc"]
								?? throw new InvalidOperationException("GrpcServices:LibraryGrpc configuration is required");
var channel = GrpcChannel.ForAddress(libraryGrpcUrl, new GrpcChannelOptions
{
	HttpHandler = new SocketsHttpHandler
	{
		PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
		KeepAlivePingDelay = TimeSpan.FromSeconds(60),
		KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
		EnableMultipleHttp2Connections = true
	}
});

builder.Services.AddSingleton(new InventoryService.InventoryServiceClient(channel));
builder.Services.AddSingleton(new UserActivityService.UserActivityServiceClient(channel));
builder.Services.AddSingleton(new CirculationService.CirculationServiceClient(channel));

var app = builder.Build();

// Enable problem details middleware
app.UseExceptionHandler();
app.UseStatusCodePages();

// NO ADDITIONAL MIDDLEWARE NEEDED - Results.Problem() handles ProblemDetails directly

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "Library API v1");
		options.RoutePrefix = string.Empty;
	});

	app.UseCors("AllowAll");
}

app.MapDefaultEndpoints();
app.MapInventoryEndpoints();
app.MapUserActivityEndpoints();
app.MapCirculationEndpoints();

app.Run();

/// <summary>
/// Just making Program accessible to test projects
/// </summary>
public partial class Program { }
