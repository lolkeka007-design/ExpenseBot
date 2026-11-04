using System.Globalization;
using System.Text;

namespace ExpenseBot;

/// <summary>Turns data into the text of the bot's replies.</summary>
public static class Formatter
{
    public static string Money(decimal amount, string currency) =>
        $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currency}";

    public static string Totals(string title, IReadOnlyList<CategoryTotal> totals, string currency, IMessages messages)
    {
        if (totals.Count == 0)
            return $"{title}\n\n{messages.NoExpensesYet}";

        var sb = new StringBuilder();
        sb.Append(title).Append("\n\n");

        foreach (var total in totals)
            sb.Append($"{total.Category} — {Money(total.Total, currency)} ({total.Count})\n");

        sb.Append($"\n{messages.Total}: {Money(totals.Sum(t => t.Total), currency)}");
        return sb.ToString();
    }

    public static string LastList(IReadOnlyList<Expense> expenses, string currency, TimeSpan utcOffset, IMessages messages)
    {
        if (expenses.Count == 0)
            return messages.NoEntriesYet;

        var sb = new StringBuilder(messages.LastTitle).Append("\n\n");
        for (var i = 0; i < expenses.Count; i++)
        {
            var expense = expenses[i];
            var local = (expense.CreatedUtc + utcOffset).ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
            sb.Append($"{i + 1}. {local} — {expense.Category} {Money(expense.Amount, currency)}\n");
        }

        return sb.ToString().TrimEnd();
    }
}
