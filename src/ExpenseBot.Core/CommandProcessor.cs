namespace ExpenseBot;

/// <summary>
/// The whole bot logic: takes the text of a message, returns the text of the reply.
/// Knows nothing about Telegram or a particular database.
/// </summary>
public sealed class CommandProcessor
{
    private readonly IExpenseStore _store;
    private readonly IMessages _messages;
    private readonly string _currency;
    private readonly TimeSpan _utcOffset;
    private readonly Func<DateTime> _utcNow;

    public CommandProcessor(
        IExpenseStore store,
        IMessages messages,
        string currency,
        TimeSpan utcOffset,
        Func<DateTime> utcNow)
    {
        _store = store;
        _messages = messages;
        _currency = currency;
        _utcOffset = utcOffset;
        _utcNow = utcNow;
    }

    public string Process(long userId, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return _messages.Help;

        text = text.Trim();
        if (text.StartsWith('/'))
            return HandleCommand(userId, text);

        var parsed = ExpenseParser.TryParse(text);
        if (parsed is null)
            return _messages.NotUnderstood;

        var expense = _store.Add(userId, parsed.Amount, parsed.Category, _utcNow());
        return _messages.Saved(expense.Category, Formatter.Money(expense.Amount, _currency));
    }

    private string HandleCommand(long userId, string text)
    {
        // "/stats@MyBot extra text" -> "/stats"
        var command = text.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0];
        var at = command.IndexOf('@');
        if (at >= 0)
            command = command[..at];

        switch (command.ToLowerInvariant())
        {
            case "/start":
            case "/help":
                return _messages.Help;

            case "/today":
            {
                var localStart = LocalNow.Date;
                var (from, to) = ToUtcRange(localStart, localStart.AddDays(1));
                var totals = _store.GetTotalsByCategory(userId, from, to);
                return Formatter.Totals(_messages.TodayTitle, totals, _currency, _messages);
            }

            case "/stats":
            {
                var now = LocalNow;
                var localStart = new DateTime(now.Year, now.Month, 1);
                var (from, to) = ToUtcRange(localStart, localStart.AddMonths(1));
                var totals = _store.GetTotalsByCategory(userId, from, to);
                return Formatter.Totals(_messages.StatsTitle(now), totals, _currency, _messages);
            }

            case "/last":
                return Formatter.LastList(_store.GetLast(userId, 10), _currency, _utcOffset, _messages);

            case "/undo":
            {
                var removed = _store.RemoveLast(userId);
                return removed is null
                    ? _messages.NothingToUndo
                    : _messages.Removed(removed.Category, Formatter.Money(removed.Amount, _currency));
            }

            default:
                return _messages.UnknownCommand;
        }
    }

    private DateTime LocalNow => _utcNow() + _utcOffset;

    // The period is defined in local time, while the database stores everything in UTC.
    private (DateTime FromUtc, DateTime ToUtc) ToUtcRange(DateTime localStart, DateTime localEnd) =>
        (DateTime.SpecifyKind(localStart - _utcOffset, DateTimeKind.Utc),
         DateTime.SpecifyKind(localEnd - _utcOffset, DateTimeKind.Utc));
}
