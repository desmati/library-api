// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using Library.Warmups;

Console.WriteLine("=== Library Warm-up Exercises ===");
Console.WriteLine();

// Exercise 1: Check if a number is a power of two
Console.WriteLine("1. Power of Two Check:");
var testNumbers = new long[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 3, 5, 7, 10, 100, 1024 };
foreach (var num in testNumbers)
{
	Console.WriteLine($"   {num} is power of two: {WarmupExercises.IsPow2(num)}_{WarmupExercises.IsPow22(num)}");
}

// Exercise 2: Reverse a book title
Console.WriteLine();
Console.WriteLine("2. Reverse Book Title:");
var titles = new[] { "The Great Gatsby", "1984", "To Kill a Mockingbird", "Pride and Prejudice" };
foreach (var title in titles)
{
	Console.WriteLine($"   Original: {title}");
	Console.WriteLine($"   Reversed: {WarmupExercises.ReverseTitle(title)}");
}

// Exercise 3: Repeat a title N times
Console.WriteLine();
Console.WriteLine("3. Repeat Title:");
Console.WriteLine($"   'Book' x 3: {WarmupExercises.RepeatTitle("Book", 3)}");
Console.WriteLine($"   'Test' x 5: {WarmupExercises.RepeatTitle("Test", 5)}");

// Exercise 4: List all odd numbers between 0 and 100
Console.WriteLine();
Console.WriteLine("4. Odd Numbers (0-100):");
var oddNumbers = WarmupExercises.OddIds0To100().ToList();
Console.WriteLine($"   Count: {oddNumbers.Count}");
Console.WriteLine($"   First 10: {string.Join(", ", oddNumbers.Take(10))}");
Console.WriteLine($"   Last 10: {string.Join(", ", oddNumbers.TakeLast(10))}");

Console.WriteLine();
Console.WriteLine("=== Done ===");
