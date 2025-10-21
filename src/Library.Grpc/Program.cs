// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Application;
using Library.Grpc.Interceptors;
using Library.Grpc.Services;
using Library.Infrastructure;
using Library.ServiceDefaults;

using Mapster;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddMapster();

builder.Services.AddGrpc(options =>
{
	options.Interceptors.Add<ExceptionInterceptor>();
});

builder.Services.AddGrpcReflection(); // grpcurl
builder.Services.AddSingleton<ExceptionInterceptor>();

builder.WebHost.ConfigureKestrel(options =>
{
	options.ConfigureEndpointDefaults(listenOptions =>
	{
		listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
	});
});

var app = builder.Build();

app.MapGrpcService<InventoryService>();
app.MapGrpcService<UserActivityService>();
app.MapGrpcService<CirculationService>();
app.MapGrpcReflectionService();

app.MapDefaultEndpoints();
app.MapGet("/", () => ""); // For none-gRPC calls

app.Run();

/// <summary>
/// Just making Program accessible to test projects
/// </summary>
public partial class Program { }
