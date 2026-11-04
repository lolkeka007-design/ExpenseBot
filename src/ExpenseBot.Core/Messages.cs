namespace ExpenseBot;

/// <summary>All user-facing texts of the bot for one language.</summary>
public interface IMessages
{
    string Help { get; }
    string NotUnderstood { get; }
    string UnknownCommand { get; }
    string NothingToUndo { get; }
    string NoExpensesYet { get; }
    string NoEntriesYet { get; }
    string TodayTitle { get; }
    string LastTitle { get; }
    string Total { get; }

    string StatsTitle(DateTime localDate);
    string Saved(string category, string money);
    string Removed(string category, string money);
}

public sealed class EnglishMessages : IMessages
{
    private static readonly string[] Months =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    public string Help =>
        "I track your expenses.\n\n" +
        "Just send a message like:\n" +
        "coffee 5\n" +
        "5.5 taxi\n" +
        "groceries 23,40\n\n" +
        "Commands:\n" +
        "/today — today's expenses\n" +
        "/stats — this month's expenses by category\n" +
        "/last — last 10 entries\n" +
        "/undo — remove the last entry\n" +
        "/help — show this help";

    public string NotUnderstood =>
        "Sorry, I didn't get that. Send an expense like \"coffee 5\" or \"5 coffee\". All commands: /help";

    public string UnknownCommand => "Unknown command. See /help for the list of commands.";
    public string NothingToUndo => "Nothing to undo.";
    public string NoExpensesYet => "No expenses yet.";
    public string NoEntriesYet => "No entries yet.";
    public string TodayTitle => "📊 Expenses today";
    public string LastTitle => "Last expenses:";
    public string Total => "Total";

    public string StatsTitle(DateTime localDate) =>
        $"📊 Expenses for {Months[localDate.Month - 1]} {localDate.Year}";

    public string Saved(string category, string money) => $"✅ Saved: {category} — {money}";
    public string Removed(string category, string money) => $"↩️ Removed: {category} — {money}";
}

public sealed class RussianMessages : IMessages
{
    private static readonly string[] Months =
    {
        "январь", "февраль", "март", "апрель", "май", "июнь",
        "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь"
    };

    public string Help =>
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

    public string NotUnderstood =>
        "Не понял. Напиши трату в формате «кофе 5» или «5 кофе». Все команды: /help";

    public string UnknownCommand => "Неизвестная команда. Список команд: /help";
    public string NothingToUndo => "Нечего отменять.";
    public string NoExpensesYet => "Расходов пока нет.";
    public string NoEntriesYet => "Записей пока нет.";
    public string TodayTitle => "📊 Расходы за сегодня";
    public string LastTitle => "Последние траты:";
    public string Total => "Итого";

    public string StatsTitle(DateTime localDate) =>
        $"📊 Расходы за {Months[localDate.Month - 1]} {localDate.Year}";

    public string Saved(string category, string money) => $"✅ Записал: {category} — {money}";
    public string Removed(string category, string money) => $"↩️ Удалил: {category} — {money}";
}

/// <summary>Picks the message set by language code ("en" or "ru").</summary>
public static class Localization
{
    /// <returns>The messages, or null if the language code is not supported. An empty code means English.</returns>
    public static IMessages? Get(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        null or "" or "en" => new EnglishMessages(),
        "ru" => new RussianMessages(),
        _ => null
    };
}
