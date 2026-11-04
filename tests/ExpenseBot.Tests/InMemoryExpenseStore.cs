namespace ExpenseBot.Tests;

/// <summary>Хранилище в памяти для тестов: никакой базы данных не нужно.</summary>
public sealed class InMemoryExpenseStore : IExpenseStore
{
    private readonly List<(long UserId, Expense Expense)> _rows = new();
    private long _nextId = 1;

    public Expense Add(long userId, decimal amount, string category, DateTime createdUtc)
    {
        var expense = new Expense(_nextId++, amount, category, createdUtc);
        _rows.Add((userId, expense));
        return expense;
    }

    public IReadOnlyList<Expense> GetLast(long userId, int limit) =>
        _rows.Where(r => r.UserId == userId)
             .Select(r => r.Expense)
             .OrderByDescending(e => e.Id)
             .Take(limit)
             .ToList();

    public IReadOnlyList<CategoryTotal> GetTotalsByCategory(long userId, DateTime fromUtc, DateTime toUtc) =>
        _rows.Where(r => r.UserId == userId && r.Expense.CreatedUtc >= fromUtc && r.Expense.CreatedUtc < toUtc)
             .GroupBy(r => r.Expense.Category)
             .Select(g => new CategoryTotal(g.Key, g.Sum(r => r.Expense.Amount), g.Count()))
             .OrderByDescending(t => t.Total)
             .ThenBy(t => t.Category)
             .ToList();

    public Expense? RemoveLast(long userId)
    {
        var index = _rows.FindLastIndex(r => r.UserId == userId);
        if (index < 0)
            return null;

        var expense = _rows[index].Expense;
        _rows.RemoveAt(index);
        return expense;
    }
}
