namespace ExpenseBot;

/// <summary>
/// Вся логика бота: принимает текст сообщения, возвращает текст ответа.
/// Ничего не знает ни про Telegram, ни про конкретную базу данных.
/// </summary>
public sealed class CommandProcessor
{
    public const string HelpText =
        "Я записываю твои траты.\n\n" +
        "Просто напиши сообщение:\n" +
        "кофе 5\n" +
        "5.5 такси\n" +
        "продукты 23,40\n\n" +
        "Команды:\n" +
        "/today — траты за сегодня\n" +
        "/stats — траты за этот месяц по категориям\n" +
        "/last — последние 10 записей\n" +
        "/undo — удалить последнюю запись\n" +
        "/help — эта подсказка";

    private readonly IExpenseStore _store;
    private readonly string _currency;
    private readonly TimeSpan _utcOffset;
    private readonly Func<DateTime> _utcNow;

    public CommandProcessor(IExpenseStore store, string currency, TimeSpan utcOffset, Func<DateTime> utcNow)
    {
        _store = store;
        _currency = currency;
        _utcOffset = utcOffset;
        _utcNow = utcNow;
    }

    public string Process(long userId, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return HelpText;

        text = text.Trim();
        if (text.StartsWith('/'))
            return HandleCommand(userId, text);

        var parsed = ExpenseParser.TryParse(text);
        if (parsed is null)
            return "Не понял. Напиши трату в формате «кофе 5» или «5 кофе». Все команды: /help";

        var expense = _store.Add(userId, parsed.Amount, parsed.Category, _utcNow());
        return $"✅ Записал: {expense.Category} — {Formatter.Money(expense.Amount, _currency)}";
    }

    private string HandleCommand(long userId, string text)
    {
        // "/stats@MyBot лишний текст" -> "/stats"
        var command = text.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0];
        var at = command.IndexOf('@');
        if (at >= 0)
            command = command[..at];

        switch (command.ToLowerInvariant())
        {
            case "/start":
            case "/help":
                return HelpText;

            case "/today":
            {
                var localStart = LocalNow.Date;
                var (from, to) = ToUtcRange(localStart, localStart.AddDays(1));
                var totals = _store.GetTotalsByCategory(userId, from, to);
                return Formatter.Totals("📊 Расходы за сегодня", totals, _currency);
            }

            case "/stats":
            {
                var now = LocalNow;
                var localStart = new DateTime(now.Year, now.Month, 1);
                var (from, to) = ToUtcRange(localStart, localStart.AddMonths(1));
                var totals = _store.GetTotalsByCategory(userId, from, to);
                return Formatter.Totals($"📊 Расходы за {Formatter.MonthTitle(now)}", totals, _currency);
            }

            case "/last":
                return Formatter.LastList(_store.GetLast(userId, 10), _currency, _utcOffset);

            case "/undo":
            {
                var removed = _store.RemoveLast(userId);
                return removed is null
                    ? "Нечего отменять."
                    : $"↩️ Удалил: {removed.Category} — {Formatter.Money(removed.Amount, _currency)}";
            }

            default:
                return "Неизвестная команда. Список команд: /help";
        }
    }

    private DateTime LocalNow => _utcNow() + _utcOffset;

    // Границы периода задаются по местному времени, а в базе всё лежит в UTC.
    private (DateTime FromUtc, DateTime ToUtc) ToUtcRange(DateTime localStart, DateTime localEnd) =>
        (DateTime.SpecifyKind(localStart - _utcOffset, DateTimeKind.Utc),
         DateTime.SpecifyKind(localEnd - _utcOffset, DateTimeKind.Utc));
}
