using Xunit;

namespace ExpenseBot.Tests;

public class ExpenseParserTests
{
    [Theory]
    [InlineData("кофе 5", "кофе", 5.0)]
    [InlineData("кофе 5.5", "кофе", 5.5)]
    [InlineData("кофе 5,5", "кофе", 5.5)]
    [InlineData("5 кофе", "кофе", 5.0)]
    [InlineData("  Такси   12.5  ", "такси", 12.5)]
    [InlineData("Кофе с молоком 3", "кофе с молоком", 3.0)]
    public void Parses_valid_input(string input, string category, double amount)
    {
        var result = ExpenseParser.TryParse(input);

        Assert.NotNull(result);
        Assert.Equal(category, result!.Category);
        Assert.Equal((decimal)amount, result.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("привет")]
    [InlineData("5")]
    [InlineData("кофе 0")]
    [InlineData("кофе -5")]
    [InlineData("кофе 5 6")]
    [InlineData("кофе 99999999")]
    [InlineData("!!! 5")]
    public void Rejects_invalid_input(string input)
    {
        Assert.Null(ExpenseParser.TryParse(input));
    }
}
