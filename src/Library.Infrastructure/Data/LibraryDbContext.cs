// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Library.Infrastructure.Data;

public class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
	: DbContext(options)
{
	public DbSet<Book> Books => Set<Book>();
	public DbSet<User> Users => Set<User>();
	public DbSet<Loan> Loans => Set<Loan>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			foreach (var property in entityType.GetProperties())
			{
				// Configuring UTC DateTime conversion for all DateTime properties
				if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
				{
					property.SetValueConverter(new ValueConverter<DateTime, DateTime>(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
				}
			}
		}

		modelBuilder.Entity<Book>(entity =>
		{
			entity.HasKey(e => e.BookId);

			entity.Property(e => e.Isbn)
				.IsRequired()
				.HasMaxLength(20);

			entity.Property(e => e.Title)
				.IsRequired()
				.HasMaxLength(500);

			entity.Property(e => e.Author)
				.IsRequired()
				.HasMaxLength(200);

			entity.Property(e => e.PageCount)
				.IsRequired();

			entity.Property(e => e.PublishedYear);

			// TODO: Remove if could not implement ISBN lookups
			entity.HasIndex(e => e.Isbn);
		});

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(e => e.UserId);

			entity.Property(e => e.FullName)
				.IsRequired()
				.HasMaxLength(200);

			entity.Property(e => e.RegisteredAt)
				.IsRequired();

			entity.HasIndex(e => e.RegisteredAt);
		});

		modelBuilder.Entity<Loan>(entity =>
		{
			entity.HasKey(e => e.LoanId);

			entity.Property(e => e.BookId)
				.IsRequired();

			entity.Property(e => e.UserId)
				.IsRequired();

			entity.Property(e => e.BorrowedAt)
				.IsRequired();

			entity.Property(e => e.ReturnedAt);

			entity.HasOne(e => e.Book)
				.WithMany()
				.HasForeignKey(e => e.BookId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(e => e.User)
				.WithMany()
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			// Indexes for query performance
			entity.HasIndex(e => e.BookId)
				.HasDatabaseName("IX_Loans_BookId");

			entity.HasIndex(e => new { e.UserId, e.BorrowedAt })
				.HasDatabaseName("IX_Loans_UserId_BorrowedAt");

			entity.HasIndex(e => new { e.BookId, e.BorrowedAt })
				.HasDatabaseName("IX_Loans_BookId_BorrowedAt");

			// TODO: Put more of Idempotency if got extra time 
			// Index for idempotency: no duplicates; same book/user/borrow date.
			entity.HasIndex(e => new { e.UserId, e.BookId, e.BorrowedAt })
				.IsUnique()
				.HasDatabaseName("IX_Loans_UserId_BookId_BorrowedAt_Unique");
		});
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		foreach (var entry in ChangeTracker.Entries())
		{
			foreach (var property in entry.Properties)
			{
				// Ensuring all DateTime values are in UTC
				if (property.CurrentValue is DateTime dateTime && dateTime.Kind != DateTimeKind.Utc)
				{
					property.CurrentValue = dateTime.ToUniversalTime();
				}
			}
		}

		return base.SaveChangesAsync(cancellationToken);
	}
}
