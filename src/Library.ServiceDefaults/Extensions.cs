// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

// TODO: Ask chatgpt for possible enhancements.

using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Events;

namespace Library.ServiceDefaults;

/// <summary>
/// Extension methods for configuring service defaults including logging, telemetry, and health checks.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Adds service defaults including Serilog, OpenTelemetry, health checks, and service discovery.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <returns>The modified host application builder.</returns>
	public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
	{
		ConfigureSerilog(builder);
		ConfigureOpenTelemetry(builder);
		ConfigureHealthChecks(builder);
		ConfigureServiceDiscovery(builder);

		return builder;
	}

	/// <summary>
	/// Configures Serilog with console and Seq sinks, enrichment, and configuration from appsettings.
	/// </summary>
	private static void ConfigureSerilog(IHostApplicationBuilder builder)
	{
		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(builder.Configuration)
			.Enrich.FromLogContext()
			.Enrich.WithMachineName()
			.Enrich.WithThreadId()
			.Enrich.WithEnvironmentName()
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
			.WriteTo.Console(
				outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
				formatProvider: CultureInfo.InvariantCulture)
			.WriteTo.Seq(
				serverUrl: builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
				apiKey: builder.Configuration["Seq:ApiKey"],
				formatProvider: CultureInfo.InvariantCulture)
			.CreateLogger();

		builder.Services.AddSerilog(Log.Logger);

		builder.Logging.ClearProviders();
		builder.Logging.AddSerilog(dispose: true);
	}

	/// <summary>
	/// Configures OpenTelemetry with tracing, metrics, and OTLP export.
	/// </summary>
	private static void ConfigureOpenTelemetry(IHostApplicationBuilder builder)
	{
		var serviceName = builder.Configuration["ServiceName"] ?? builder.Environment.ApplicationName;
		var serviceVersion = builder.Configuration["ServiceVersion"] ?? "1.0.0";

		builder.Services.AddOpenTelemetry()
			.ConfigureResource(resource => resource
				.AddService(
					serviceName: serviceName,
					serviceVersion: serviceVersion,
					serviceInstanceId: Environment.MachineName)
			)
			.WithTracing(tracing =>
			{
				tracing
					.AddAspNetCoreInstrumentation(options =>
					{
						options.RecordException = true;
						options.Filter = httpContext => httpContext.Request.Path.Value is not "/health" and not "/health/ready" and not "/alive";
					})
					.AddHttpClientInstrumentation(options =>
					{
						options.RecordException = true;
						options.FilterHttpRequestMessage = request => request.RequestUri?.AbsolutePath is not "/health" and not "/health/ready" and not "/alive";
					});

				// OTLP exporter if configured
				if (builder.Configuration["OpenTelemetry:OtlpEndpoint"] is { Length: > 0 } otlpEndpoint)
				{
					tracing.AddOtlpExporter(options => { options.Endpoint = new Uri(otlpEndpoint); });
				}
				else
				{
					// Use default OTLP endpoint
					tracing.AddOtlpExporter();
				}
			})
			.WithMetrics(metrics =>
			{
				metrics
					.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation();

				// OTLP exporter if configured
				if (builder.Configuration["OpenTelemetry:OtlpEndpoint"] is { Length: > 0 } otlpEndpoint)
				{
					metrics.AddOtlpExporter(options => { options.Endpoint = new Uri(otlpEndpoint); });
				}
				else
				{
					// Use default OTLP endpoint
					metrics.AddOtlpExporter();
				}
			});
	}

	/// <summary>
	/// Configures health checks for liveness and readiness.
	/// </summary>
	private static void ConfigureHealthChecks(IHostApplicationBuilder builder)
	{
		builder.Services
				.AddHealthChecks()
				.AddDefaultHealthChecks();

		builder.Services.AddRequestTimeouts(options =>
		{
			options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
			{
				Timeout = TimeSpan.FromSeconds(30)
			};
		});
	}

	/// <summary>
	/// Configures service discovery if available.
	/// </summary>
	private static void ConfigureServiceDiscovery(IHostApplicationBuilder builder)
	{
		// Add service discovery
		builder.Services.AddServiceDiscovery();

		// Configure HTTP client to use service discovery
		builder.Services.ConfigureHttpClientDefaults(http =>
		{
			// Turn on resilience by default
			http.AddStandardResilienceHandler();

			// Turn on service discovery by default
			http.AddServiceDiscovery();
		});
	}

	/// <summary>
	/// Adds default health checks for common infrastructure components.
	/// </summary>
	/// <param name="healthChecksBuilder">The health checks builder.</param>
	/// <returns>The modified health checks builder.</returns>
	public static IHealthChecksBuilder AddDefaultHealthChecks(this IHealthChecksBuilder healthChecksBuilder)
	{
		// Add a self health check that always returns healthy
		healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy("Service is running"), tags: ["ready"]);

		return healthChecksBuilder;
	}

	/// <summary>
	/// Maps health check endpoints for liveness and readiness probes.
	/// </summary>
	/// <param name="app">The web application.</param>
	/// <returns>The modified web application.</returns>
	public static WebApplication MapDefaultEndpoints(this WebApplication app)
	{
		// Liveness probe - checks if the service is alive
		app.MapHealthChecks("/health", new HealthCheckOptions
		{
			Predicate = _ => false, // Don't run any checks, just return healthy if service is running
			AllowCachingResponses = false
		});

		app.MapHealthChecks("/alive", new HealthCheckOptions
		{
			Predicate = _ => false, // Don't run any checks, just return healthy if service is running
			AllowCachingResponses = false
		});

		// Readiness probe - checks if the service is ready to accept requests
		app.MapHealthChecks("/health/ready", new HealthCheckOptions
		{
			Predicate = check => check.Tags.Contains("ready"),
			AllowCachingResponses = false,
			ResponseWriter = async (context, report) =>
			{
				context.Response.ContentType = "application/json";
				var result = System.Text.Json.JsonSerializer.Serialize(new
				{
					status = report.Status.ToString(),
					checks = report.Entries.Select(entry => new
					{
						name = entry.Key,
						status = entry.Value.Status.ToString(),
						description = entry.Value.Description,
						duration = entry.Value.Duration.TotalMilliseconds
					}),
					totalDuration = report.TotalDuration.TotalMilliseconds
				});
				await context.Response.WriteAsync(result);
			}
		});

		return app;
	}
}
