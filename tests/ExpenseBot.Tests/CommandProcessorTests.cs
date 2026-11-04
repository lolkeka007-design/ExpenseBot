using Xunit;

namespace ExpenseBot.Tests;

public class CommandProcessorTests
{
    // 4 November 2026, 13:00 UTC = 17:00 in Tbilisi (UTC+4).
    private static readonly DateTime Now = new(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc);

    private static (CommandProcessor Processor, InMemoryExpenseStore Store) Create()
    {
        var store = new InMemoryExpenseStore();
        var processor = new CommandProcessor(store, new EnglishMessages(), "GEL", TimeSpan.FromHours(4), () => Now);
        return (processor, store);
    }

    [Fact]
    public void Adding_an_expense_confirms_and_stores_it()
    {
        var (processor, store) = Create();

        var reply = processor.Process(1, "coffee 5");

        Assert.Contains("Saved", reply);
        Assert.Contains("coffee", reply);
        Assert.Contains("5.00 GEL", reply);
        Assert.Single(store.GetLast(1, 10));
    }

    [Fact]
    public void Stats_groups_by_category_and_sums()
    {
        var (processor, _) = Create();
        processor.Process(1, "coffee 5");
        processor.Process(1, "coffee 3.5");
        processor.Process(1, "taxi 10");

        var reply = processor.Process(1, "/stats");

        Assert.Contains("November 2026", reply);
        Assert.Contains("coffee — 8.50 GEL (2)", reply);
        Assert.Contains("taxi — 10.00 GEL (1)", reply);
        Assert.Contains("Total: 18.50 GEL", reply);
    }

    [Fact]
    public void Undo_removes_the_last_expense_only()
    {
        var (processor, _) = Create();
        processor.Process(1, "coffee 5");
        processor.Process(1, "taxi 10");

        var undoReply = processor.Process(1, "/undo");
        var statsReply = processor.Process(1, "/stats");

        Assert.Contains("taxi", undoReply);
        Assert.Contains("Total: 5.00 GEL", statsReply);
    }

    [Fact]
    public void Undo_with_no_expenses_says_so()
    {
        var (processor, _) = Create();

        Assert.Equal("Nothing to undo.", processor.Process(1, "/undo"));
    }

    [Fact]
    public void Users_do_not_see_each_others_expenses()
    {
        var (processor, _) = Create();
        processor.Process(1, "coffee 5");

        var reply = processor.Process(2, "/stats");

        Assert.Contains("No expenses yet", reply);
    }

    [Fact]
    public void Month_boundary_respects_utc_offset()
    {
        var (processor, store) = Create();
        // 21:30 UTC on 31 October = 01:30 on 1 November local time -> November.
        store.Add(1, 5m, "coffee", new DateTime(2026, 10, 31, 21, 30, 0, DateTimeKind.Utc));
        // 19:30 UTC on 31 October = 23:30 on 31 October local time -> October.
        store.Add(1, 7m, "taxi", new DateTime(2026, 10, 31, 19, 30, 0, DateTimeKind.Utc));

        var reply = processor.Process(1, "/stats");

        Assert.Contains("Total: 5.00 GEL", reply);
        Assert.DoesNotContain("taxi", reply);
    }

    [Fact]
    public void Today_counts_only_the_local_day()
    {
        var (processor, store) = Create();
        // 08:00 UTC on 4 November = 12:00 local time -> today.
        store.Add(1, 5m, "coffee", new DateTime(2026, 11, 4, 8, 0, 0, DateTimeKind.Utc));
        // 18:00 UTC on 3 November = 22:00 on 3 November local time -> yesterday.
        store.Add(1, 9m, "dinner", new DateTime(2026, 11, 3, 18, 0, 0, DateTimeKind.Utc));

        var reply = processor.Process(1, "/today");

        Assert.Contains("Total: 5.00 GEL", reply);
        Assert.DoesNotContain("dinner", reply);
    }

    [Fact]
    public void Last_lists_newest_first_in_local_time()
    {
        var (processor, _) = Create();
        processor.Process(1, "coffee 5");
        processor.Process(1, "taxi 10");

        var reply = processor.Process(1, "/last");

        Assert.Contains("1. 04.11 17:00 — taxi 10.00 GEL", reply);
        Assert.Contains("2. 04.11 17:00 — coffee 5.00 GEL", reply);
    }

    [Fact]
    public void Unknown_text_returns_a_hint()
    {
        var (processor, store) = Create();

        var reply = processor.Process(1, "hello");

        Assert.Contains("didn't get that", reply);
        Assert.Empty(store.GetLast(1, 10));
    }

    [Fact]
    public void Unknown_command_returns_a_hint()
    {
        var (processor, _) = Create();

        Assert.Contains("Unknown command", processor.Process(1, "/dance"));
    }

    [Fact]
    public void Help_lists_the_commands()
    {
        var (processor, _) = Create();

        var reply = processor.Process(1, "/help");

        Assert.Contains("/stats", reply);
        Assert.Contains("/undo", reply);
    }

    [Fact]
    public void Command_with_bot_suffix_still_works()
    {
        var (processor, _) = Create();

        var reply = processor.Process(1, "/stats@MyExpenseBot");

        Assert.Contains("No expenses yet", reply);
    }
}
