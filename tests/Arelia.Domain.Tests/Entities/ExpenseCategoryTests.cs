using Arelia.Domain.Entities;
using FluentAssertions;

namespace Arelia.Domain.Tests.Entities;

public class ExpenseCategoryTests
{
    [Theory]
    [InlineData("SHEET MUSIC", "SHEET MUSIC")]
    [InlineData("  sheet   music  ", "SHEET MUSIC")]
    [InlineData("sheet music", "SHEET MUSIC")]
    [InlineData("  TRAVEL  ", "TRAVEL")]
    [InlineData("venue   rental", "VENUE RENTAL")]
    public void NormalizeShouldUppercaseTrimAndCollapseSpaces(string input, string expected)
    {
        var result = ExpenseCategory.Normalize(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NormalizeShouldReturnEmptyForBlankInput(string? input)
    {
        var result = ExpenseCategory.Normalize(input!);

        result.Should().BeEmpty();
    }
}
