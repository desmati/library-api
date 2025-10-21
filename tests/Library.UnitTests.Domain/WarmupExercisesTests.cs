// © 2025 Mohammad Hossein Dokht Esmati <desmati@gmail.com> - Licensed under GPL-3.0-or-later
// This file is part of Library Management System. This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY

using FluentAssertions;

using Library.Warmups;

namespace Library.UnitTests.Domain;

public class WarmupExercisesTests
{
	[Theory]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(4, true)]
	[InlineData(8, true)]
	[InlineData(16, true)]
	[InlineData(32, true)]
	[InlineData(64, true)]
	[InlineData(128, true)]
	[InlineData(256, true)]
	[InlineData(512, true)]
	[InlineData(1024, true)]
	[InlineData(3, false)]
	[InlineData(5, false)]
	[InlineData(7, false)]
	[InlineData(10, false)]
	[InlineData(100, false)]
	[InlineData(0, false)]
	[InlineData(-1, false)]
	[InlineData(-8, false)]
	public void IsPowerOfTwo_ShouldReturnCorrectResult(long input, bool expected)
	{
		// Act
		var result = WarmupExercises.IsPow2(input);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("hello", "olleh")]
	[InlineData("Book", "kooB")]
	[InlineData("12345", "54321")]
	[InlineData("A", "A")]
	[InlineData("", "")]
	[InlineData("The Great Gatsby", "ybstaG taerG ehT")]
	public void ReverseTitle_ShouldReverseString(string input, string expected)
	{
		// Act
		var result = WarmupExercises.ReverseTitle(input);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void ReverseTitle_WithNull_ShouldReturnEmpty()
	{
		// Act
		var result = WarmupExercises.ReverseTitle(null);

		// Assert
		result.Should().BeEmpty();
	}

	[Theory]
	[InlineData("Book", 3, "BookBookBook")]
	[InlineData("Test", 5, "TestTestTestTestTest")]
	[InlineData("A", 10, "AAAAAAAAAA")]
	[InlineData("Hello", 1, "Hello")]
	[InlineData("World", 0, "")]
	[InlineData("", 5, "")]
	public void RepeatTitle_ShouldRepeatCorrectly(string input, int count, string expected)
	{
		// Act
		var result = WarmupExercises.RepeatTitle(input, count);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void RepeatTitle_WithNegativeCount_ShouldThrow()
	{
		// Act & Assert
		var act = () => WarmupExercises.RepeatTitle("Test", -1);
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void OddIds0To100_ShouldReturnAllOddNumbers()
	{
		// Act
		var result = WarmupExercises.OddIds0To100().ToList();

		// Assert
		result.Should().HaveCount(50);
		result.Should().OnlyContain(n => n % 2 == 1);
		result.First().Should().Be(1);
		result.Last().Should().Be(99);
		result.Should().BeInAscendingOrder();
	}
}
