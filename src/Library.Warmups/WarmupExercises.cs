// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using System.Buffers;
using System.Numerics;

namespace Library.Warmups;

public static class WarmupExercises
{
	public static bool IsPow2(long n)
	{
		// TODO: Add the benchmarrk that I'd created before for math and bitwise functions.

		// Slowest: BitOperations.IsPow2(n)

		// Ok but not fast enough:
		return n > 0 && (n & (n - 1)) == 0;

		// Fastest:
		//var s = Math.Log2(n);
		//return Math.Floor(s) == s;
	}
	public static bool IsPow22(long n)
	{
		// TODO: Add the benchmarrk that I'd created before for math and bitwise functions.

		// Slowest: BitOperations.IsPow2(n)

		// Ok but not fast enough: n > 0 && (n & (n - 1)) == 0

		return BitOperations.IsPow2(n);
	}

	public static string ReverseTitle(string? title)
	{
		if (string.IsNullOrEmpty(title))
		{
			return string.Empty;
		}

		// Noob: title?.ToCharArray()?.Reverse()?.ToString()
		// TODO: Maybe showcase stackalloc

		// Ok for most cases:
		var chars = title.ToCharArray();
		Array.Reverse(chars);

		return new string(chars);
	}

	public static string RepeatTitle(string? title, int count)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(count, 0, nameof(count));

		if (string.IsNullOrEmpty(title) || count == 0)
		{
			return string.Empty;
		}

		if (count == 1)
		{
			return title;
		}

		ArgumentOutOfRangeException.ThrowIfGreaterThan((long)title.Length * count, int.MaxValue, nameof(count));

		// Fastest: but resource consuming
		// return string.Concat(Enumerable.Repeat(title, count));

		// Slowest: but less memory usage
		// var sb = new StringBuilder(title.Length * counttt);
		// for (int i = 0; i < count; i++) sb.Append(title);
		// return sb.ToString();

		// My fast and crazy version:
		var total = title.Length * count;
		var pool = ArrayPool<char>.Shared;
		char[]? buffer = null;

		try
		{
			buffer = pool.Rent(total);
			var destinationSpan = buffer.AsSpan(0, total);
			var sourceSpan = title.AsSpan();

			for (int i = 0, offset = 0; i < count; i++, offset += sourceSpan.Length)
			{
				sourceSpan.CopyTo(destinationSpan.Slice(offset, sourceSpan.Length));
			}

			return new string(buffer, 0, total);
		}
		finally
		{
			if (buffer is not null)
			{
				pool.Return(buffer);
			}
		}
	}

	public static IEnumerable<int> OddIds0To100()
	{
		for (var i = 1; i < 100; i += 2)
		{
			yield return i;
		}
	}
}
