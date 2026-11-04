namespace ExpenseBot;

/// <summary>Одна запись о трате. Время хранится в UTC.</summary>
public sealed record Expense(long Id, decimal Amount, string Category, DateTime CreatedUtc);

/// <summary>Сумма и количество трат по одной категории.</summary>
public sealed record CategoryTotal(string Category, decimal Total, int Count);

/// <summary>
/// Хранилище трат. Логика бота зависит только от этого интерфейса,
/// поэтому её можно тестировать без базы данных и без Telegram.
/// </summary>
public interface IExpenseStore
{
    Expense Add(long userId, decimal amount, string category, DateTime createdUtc);

    /// <summary>Последние траты пользователя, от новых к старым.</summary>
    IReadOnlyList<Expense> GetLast(long userId, int limit);

    /// <summary>Итоги по категориям за период [fromUtc, toUtc), от большей суммы к меньшей.</summary>
    IReadOnlyList<CategoryTotal> GetTotalsByCategory(long userId, DateTime fromUtc, DateTime toUtc);

    /// <summary>Удаляет и возвращает последнюю трату пользователя (или null, если трат нет).</summary>
    Expense? RemoveLast(long userId);
}
