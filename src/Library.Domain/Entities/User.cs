// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Domain.Entities;

public class User
{
	public Guid UserId { get; private set; }
	public string FullName { get; private set; } = string.Empty;
	public DateTime RegisteredAt { get; private set; }

	public User(Guid userId, string fullName, DateTime registeredAt)
	{
		if (userId == Guid.Empty)
		{
			throw new ArgumentException("UserId cannot be empty", nameof(userId));
		}

		if (string.IsNullOrWhiteSpace(fullName))
		{
			throw new ArgumentException("FullName cannot be empty", nameof(fullName));
		}

		UserId = userId;
		FullName = fullName;
		RegisteredAt = registeredAt;
	}

	public static User Create(string fullName, DateTime? registeredAt = null)
	{
		return new User(Guid.NewGuid(), fullName, registeredAt ?? DateTime.UtcNow);
	}

	[Obsolete("EF Core ctor", false)] private User() { }
}
