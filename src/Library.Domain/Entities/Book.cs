// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

namespace Library.Domain.Entities;

public class Book
{
	public Guid BookId { get; private set; }
	public string Isbn { get; private set; } = string.Empty;
	public string Title { get; private set; } = string.Empty;
	public string Author { get; private set; } = string.Empty;
	public int PageCount { get; private set; }
	public int? PublishedYear { get; private set; }

	public Book(Guid bookId, string isbn, string title, string author, int pageCount, int? publishedYear = null)
	{
		if (bookId == Guid.Empty)
		{
			throw new ArgumentException("BookId cannot be empty", nameof(bookId));
		}

		if (string.IsNullOrWhiteSpace(isbn))
		{
			throw new ArgumentException("ISBN cannot be empty", nameof(isbn));
		}

		if (string.IsNullOrWhiteSpace(title))
		{
			throw new ArgumentException("Title cannot be empty", nameof(title));
		}

		if (string.IsNullOrWhiteSpace(author))
		{
			throw new ArgumentException("Author cannot be empty", nameof(author));
		}

		if (pageCount <= 0)
		{
			throw new ArgumentException("Page count must be positive", nameof(pageCount));
		}

		BookId = bookId;
		Isbn = isbn;
		Title = title;
		Author = author;
		PageCount = pageCount;
		PublishedYear = publishedYear;
	}

	public static Book Create(string isbn, string title, string author, int pageCount, int? publishedYear = null)
	{
		return new Book(Guid.NewGuid(), isbn, title, author, pageCount, publishedYear);
	}

	[Obsolete("EF Core ctor", false)] private Book() { }
}
