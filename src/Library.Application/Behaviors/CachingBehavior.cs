// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Text.Json;

using Library.Application.Queries;

using MediatR;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Library.Application.Behaviors;

/// <summary>
/// Caches query results for requests implementing ICacheableQuery.
/// </summary>
public class CachingBehavior<TRequest, TResponse>(IMemoryCache _cache, ILogger<CachingBehavior<TRequest, TResponse>> _logger)
	: IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
	/// <summary>
	/// Uses IMemoryCache with a 5-minute TTL
	/// </summary>
	private static readonly TimeSpan s_cacheDuration = TimeSpan.FromMinutes(5);

	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		// Only cache queries that implement ICacheableQuery
		if (request is not ICacheableQuery)
		{
			return await next(cancellationToken);
		}

		var cacheKey = GenerateCacheKey(request);
		if (_cache.TryGetValue<TResponse>(cacheKey, out var cachedResponse) && cachedResponse != null)
		{
			_logger.LogDebug("Cache hit for {RequestName} with key {CacheKey}", typeof(TRequest).Name, cacheKey);

			return cachedResponse;
		}

		_logger.LogDebug("Cache miss for {RequestName} with key {CacheKey}", typeof(TRequest).Name, cacheKey);

		var response = await next(cancellationToken);
		var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(s_cacheDuration);
		_cache.Set(cacheKey, response, cacheEntryOptions);

		_logger.LogDebug("Cached response for {RequestName} with key {CacheKey} for {Duration}", typeof(TRequest).Name, cacheKey, s_cacheDuration);

		return response;
	}

	/// <summary>
	/// generates cache keys based on query parameters, type and its properties
	/// </summary>
	private static string GenerateCacheKey(TRequest request)
	{
		var requestType = typeof(TRequest).Name;
		var requestJson = JsonSerializer.Serialize(request);

		return $"{requestType}:{requestJson}";
	}
}
