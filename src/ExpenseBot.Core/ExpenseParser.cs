using System.Globalization;
using System.Text.RegularExpressions;

namespace ExpenseBot;

public sealed record ParsedExpense(decimal Amount, string Category);

/// <summary>
/// Разбирает сообщения вида «кофе 5», «кофе 5.5», «кофе 5,5» и «5 кофе».
/// </summary>
public static class ExpenseParser
{
    public const decimal MaxAmount = 1_000_000m;
    public const int MaxCategoryLength = 50;

    private const string Number = @"\d+(?:[.,]\d{1,2})?";

    private static readonly Regex CategoryFirst =
        new($@"^(?<cat>.+?)\s+(?<amt>{Number})$", RegexOptions.Compiled);

    private static readonly Regex AmountFirst =
        new($@"^(?<amt>{Number})\s+(?<cat>.+)$", RegexOptions.Compiled);

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static ParsedExpense? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();
        return TryMatch(CategoryFirst, text) ?? TryMatch(AmountFirst, text);
    }

    private static ParsedExpense? TryMatch(Regex regex, string text)
    {
        var match = regex.Match(text);
        if (!match.Success)
            return null;

        var category = Whitespace.Replace(match.Groups["cat"].Value.Trim(), " ").ToLowerInvariant();

        // Категория — это слова: без цифр, хотя бы с одной буквой и не слишком длинная.
        if (category.Length == 0 || category.Length > MaxCategoryLength)
            return null;
        if (category.Any(char.IsDigit) || !category.Any(char.IsLetter))
            return null;

        var amountText = match.Groups["amt"].Value.Replace(',', '.');
        if (!decimal.TryParse(amountText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
            return null;
        if (amount <= 0 || amount > MaxAmount)
            return null;

        return new ParsedExpense(amount, category);
    }
}
