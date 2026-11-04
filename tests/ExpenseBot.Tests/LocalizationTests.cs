using Xunit;

namespace ExpenseBot.Tests;

public class LocalizationTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Missing_language_means_english()
    {
        Assert.True(Localization.Get(null) is EnglishMessages);
        Assert.True(Localization.Get("") is EnglishMessages);
        Assert.True(Localization.Get("en") is EnglishMessages);
    }

    [Fact]
    public void Russian_is_selected_by_code_ignoring_case_and_spaces()
    {
        Assert.True(Localization.Get("ru") is RussianMessages);
        Assert.True(Localization.Get(" RU ") is RussianMessages);
    }

    [Fact]
    public void Unsupported_language_returns_null()
    {
        Assert.Null(Localization.Get("xx"));
    }

    [Fact]
    public void Russian_processor_replies_in_russian()
    {
        var store = new InMemoryExpenseStore();
        var processor = new CommandProcessor(store, new RussianMessages(), "GEL", TimeSpan.FromHours(4), () => Now);

        var saved = processor.Process(1, "кофе 5");
        var stats = processor.Process(1, "/stats");
        var undo = processor.Process(1, "/undo");
        var undoAgain = processor.Process(1, "/undo");

        Assert.Contains("Записал", saved);
        Assert.Contains("ноябрь 2026", stats);
        Assert.Contains("Итого: 5.00 GEL", stats);
        Assert.Contains("Удалил", undo);
        Assert.Equal("Нечего отменять.", undoAgain);
    }
}
