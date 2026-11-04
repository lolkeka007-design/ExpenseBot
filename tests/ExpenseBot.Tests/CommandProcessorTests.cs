using Xunit;

namespace ExpenseBot.Tests;

public class CommandProcessorTests
{
    // 4 ноября 2026, 13:00 UTC = 17:00 по Тбилиси (UTC+4).
    private static readonly DateTime Now = new(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc);

    private static (CommandProcessor Processor, InMemoryExpenseStore Store) Create()
    {
        var store = new InMemoryExpenseStore();
        var processor = new CommandProcessor(store, "GEL", TimeSpan.FromHours(4), () => Now);
        return (processor, store);
    }

    [Fact]
    public void Adding_an_expense_confirms_and_stores_it()
    {
        var (processor, store) = Create();

        var reply = processor.Process(1, "кофе 5");

        Assert.Contains("кофе", reply);
        Assert.Contains("5.00 GEL", reply);
        Assert.Equal(1, store.GetLast(1, 10).Count);
    }

    [Fact]
    public void Stats_groups_by_category_and_sums()
    {
        var (processor, _) = Create();
        processor.Process(1, "кофе 5");
        processor.Process(1, "кофе 3.5");
        processor.Process(1, "такси 10");

        var reply = processor.Process(1, "/stats");

        Assert.Contains("ноябрь 2026", reply);
        Assert.Contains("кофе — 8.50 GEL (2)", reply);
        Assert.Contains("такси — 10.00 GEL (1)", reply);
        Assert.Contains("Итого: 18.50 GEL", reply);
    }

    [Fact]
    public void Undo_removes_the_last_expense_only()
    {
        var (processor, _) = Create();
        processor.Process(1, "кофе 5");
        processor.Process(1, "такси 10");

        var undoReply = processor.Process(1, "/undo");
        var statsReply = processor.Process(1, "/stats");

        Assert.Contains("такси", undoReply);
        Assert.Contains("Итого: 5.00 GEL", statsReply);
    }

    [Fact]
    public void Undo_with_no_expenses_says_so()
    {
        var (processor, _) = Create();

        Assert.Equal("Нечего отменять.", processor.Process(1, "/undo"));
    }

    [Fact]
    public void Users_do_not_see_each_others_expenses()
    {
        var (processor, _) = Create();
        processor.Process(1, "кофе 5");

        var reply = processor.Process(2, "/stats");

        Assert.Contains("Расходов пока нет", reply);
    }

    [Fact]
    public void Month_boundary_respects_utc_offset()
    {
        var (processor, store) = Create();
        // 21:30 UTC 31 октября = 01:30 1 ноября по местному времени -> ноябрь.
        store.Add(1, 5m, "кофе", new DateTime(2026, 10, 31, 21, 30, 0, DateTimeKind.Utc));
        // 19:30 UTC 31 октября = 23:30 31 октября по местному времени -> октябрь.
        store.Add(1, 7m, "такси", new DateTime(2026, 10, 31, 19, 30, 0, DateTimeKind.Utc));

        var reply = processor.Process(1, "/stats");

        Assert.Contains("Итого: 5.00 GEL", reply);
        Assert.False(reply.Contains("такси"));
    }

    [Fact]
    public void Today_counts_only_the_local_day()
    {
        var (processor, store) = Create();
        // 08:00 UTC 4 ноября = 12:00 местного -> сегодня.
        store.Add(1, 5m, "кофе", new DateTime(2026, 11, 4, 8, 0, 0, DateTimeKind.Utc));
        // 18:00 UTC 3 ноября = 22:00 3 ноября местного -> вчера.
        store.Add(1, 9m, "ужин", new DateTime(2026, 11, 3, 18, 0, 0, DateTimeKind.Utc));

        var reply = processor.Process(1, "/today");

        Assert.Contains("Итого: 5.00 GEL", reply);
        Assert.False(reply.Contains("ужин"));
    }

    [Fact]
    public void Unknown_text_returns_a_hint()
    {
        var (processor, store) = Create();

        var reply = processor.Process(1, "привет");

        Assert.Contains("Не понял", reply);
        Assert.Equal(0, store.GetLast(1, 10).Count);
    }

    [Fact]
    public void Command_with_bot_suffix_still_works()
    {
        var (processor, _) = Create();

        var reply = processor.Process(1, "/stats@MyExpenseBot");

        Assert.Contains("Расходов пока нет", reply);
    }
}
