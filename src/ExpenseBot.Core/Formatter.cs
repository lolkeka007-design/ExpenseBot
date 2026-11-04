using System.Globalization;
using System.Text;

namespace ExpenseBot;

/// <summary>Превращает данные в тексты ответов бота.</summary>
public static class Formatter
{
    private static readonly string[] Months =
    {
        "январь", "февраль", "март", "апрель", "май", "июнь",
        "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь"
    };

    public static string Money(decimal amount, string currency) =>
        $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currency}";

    public static string MonthTitle(DateTime localDate) =>
        $"{Months[localDate.Month - 1]} {localDate.Year}";

    public static string Totals(string title, IReadOnlyList<CategoryTotal> totals, string currency)
    {
        if (totals.Count == 0)
            return $"{title}\n\nРасходов пока нет.";

        var sb = new StringBuilder();
        sb.Append(title).Append("\n\n");

        foreach (var total in totals)
            sb.Append($"{total.Category} — {Money(total.Total, currency)} ({total.Count})\n");

        sb.Append($"\nИтого: {Money(totals.Sum(t => t.Total), currency)}");
        return sb.ToString();
    }

    public static string LastList(IReadOnlyList<Expense> expenses, string currency, TimeSpan utcOffset)
    {
        if (expenses.Count == 0)
            return "Записей пока нет.";

        var sb = new StringBuilder("Последние траты:\n\n");
        for (var i = 0; i < expenses.Count; i++)
        {
            var expense = expenses[i];
            var local = (expense.CreatedUtc + utcOffset).ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
            sb.Append($"{i + 1}. {local} — {expense.Category} {Money(expense.Amount, currency)}\n");
        }

        return sb.ToString().TrimEnd();
    }
}
