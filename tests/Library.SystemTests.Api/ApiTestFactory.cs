// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Contracts.Circulation.V1;
using Library.Contracts.Inventory.V1;
using Library.Contracts.UserActivity.V1;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Library.SystemTests.Api;

/// <summary>
/// Factory for creating test instances of the Library API with mocked gRPC services.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
	public Mock<InventoryService.InventoryServiceClient> InventoryServiceMock { get; }
	public Mock<UserActivityService.UserActivityServiceClient> UserActivityServiceMock { get; }
	public Mock<CirculationService.CirculationServiceClient> CirculationServiceMock { get; }

	public ApiTestFactory()
	{
		InventoryServiceMock = new Mock<InventoryService.InventoryServiceClient>();
		UserActivityServiceMock = new Mock<UserActivityService.UserActivityServiceClient>();
		CirculationServiceMock = new Mock<CirculationService.CirculationServiceClient>();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		// Add test configuration FIRST - this must happen before services are configured
		builder.UseSetting("GrpcServices:LibraryGrpc", "http://localhost:5000");

		builder.ConfigureServices(services =>
		{
			// Remove the existing gRPC client registrations
			var inventoryDescriptor = services.SingleOrDefault(
				d => d.ServiceType == typeof(InventoryService.InventoryServiceClient));
			if (inventoryDescriptor != null)
			{
				services.Remove(inventoryDescriptor);
			}

			var userActivityDescriptor = services.SingleOrDefault(
				d => d.ServiceType == typeof(UserActivityService.UserActivityServiceClient));
			if (userActivityDescriptor != null)
			{
				services.Remove(userActivityDescriptor);
			}

			var circulationDescriptor = services.SingleOrDefault(
				d => d.ServiceType == typeof(CirculationService.CirculationServiceClient));
			if (circulationDescriptor != null)
			{
				services.Remove(circulationDescriptor);
			}

			// Add mocked gRPC clients
			services.AddSingleton(InventoryServiceMock.Object);
			services.AddSingleton(UserActivityServiceMock.Object);
			services.AddSingleton(CirculationServiceMock.Object);
		});

		builder.UseEnvironment("Testing");
	}

	/// <summary>
	/// Resets all mock setups for a clean test state.
	/// </summary>
	public void ResetMocks()
	{
		InventoryServiceMock.Reset();
		UserActivityServiceMock.Reset();
		CirculationServiceMock.Reset();
	}
}
