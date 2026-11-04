using Xunit;

namespace ExpenseBot.Tests;

public class ExpenseParserTests
{
    [Theory]
    [InlineData("coffee 5", "coffee", 5.0)]
    [InlineData("coffee 5.5", "coffee", 5.5)]
    [InlineData("coffee 5,5", "coffee", 5.5)]
    [InlineData("5 coffee", "coffee", 5.0)]
    [InlineData("  Taxi   12.5  ", "taxi", 12.5)]
    [InlineData("Coffee with milk 3", "coffee with milk", 3.0)]
    [InlineData("кофе 5", "кофе", 5.0)]
    [InlineData("5 кофе", "кофе", 5.0)]
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
    [InlineData("hello")]
    [InlineData("привет")]
    [InlineData("5")]
    [InlineData("coffee 0")]
    [InlineData("coffee -5")]
    [InlineData("coffee 5 6")]
    [InlineData("coffee 99999999")]
    [InlineData("!!! 5")]
    public void Rejects_invalid_input(string input)
    {
        Assert.Null(ExpenseParser.TryParse(input));
    }
}
