using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ExpenseBot;

/// <summary>
/// Хранит траты в файле SQLite. Сумма лежит в копейках (целое число),
/// чтобы не терять точность на дробных числах.
/// </summary>
public sealed class SqliteExpenseStore : IExpenseStore
{
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private readonly string _connectionString;

    public SqliteExpenseStore(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        CreateSchema();
    }

    public Expense Add(long userId, decimal amount, string category, DateTime createdUtc)
    {
        using var connection = Open();

        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO expenses (user_id, amount_cents, category, created_utc)
                VALUES ($user, $cents, $category, $created);
                """;
            insert.Parameters.AddWithValue("$user", userId);
            insert.Parameters.AddWithValue("$cents", ToCents(amount));
            insert.Parameters.AddWithValue("$category", category);
            insert.Parameters.AddWithValue("$created", FormatDate(createdUtc));
            insert.ExecuteNonQuery();
        }

        using var idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT last_insert_rowid();";
        var id = Convert.ToInt64(idCommand.ExecuteScalar(), CultureInfo.InvariantCulture);

        return new Expense(id, amount, category, createdUtc);
    }

    public IReadOnlyList<Expense> GetLast(long userId, int limit)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, amount_cents, category, created_utc
            FROM expenses
            WHERE user_id = $user
            ORDER BY id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$user", userId);
        command.Parameters.AddWithValue("$limit", limit);

        var result = new List<Expense>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(ReadExpense(reader));
        return result;
    }

    public IReadOnlyList<CategoryTotal> GetTotalsByCategory(long userId, DateTime fromUtc, DateTime toUtc)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT category, SUM(amount_cents), COUNT(*)
            FROM expenses
            WHERE user_id = $user AND created_utc >= $from AND created_utc < $to
            GROUP BY category
            ORDER BY SUM(amount_cents) DESC, category;
            """;
        command.Parameters.AddWithValue("$user", userId);
        command.Parameters.AddWithValue("$from", FormatDate(fromUtc));
        command.Parameters.AddWithValue("$to", FormatDate(toUtc));

        var result = new List<CategoryTotal>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new CategoryTotal(
                reader.GetString(0),
                reader.GetInt64(1) / 100m,
                (int)reader.GetInt64(2)));
        }
        return result;
    }

    public Expense? RemoveLast(long userId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();

        Expense? last;
        using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = """
                SELECT id, amount_cents, category, created_utc
                FROM expenses
                WHERE user_id = $user
                ORDER BY id DESC
                LIMIT 1;
                """;
            select.Parameters.AddWithValue("$user", userId);

            using var reader = select.ExecuteReader();
            last = reader.Read() ? ReadExpense(reader) : null;
        }

        if (last is null)
            return null;

        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM expenses WHERE id = $id;";
            delete.Parameters.AddWithValue("$id", last.Id);
            delete.ExecuteNonQuery();
        }

        transaction.Commit();
        return last;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void CreateSchema()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS expenses (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id      INTEGER NOT NULL,
                amount_cents INTEGER NOT NULL,
                category     TEXT    NOT NULL,
                created_utc  TEXT    NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_expenses_user_date ON expenses (user_id, created_utc);
            """;
        command.ExecuteNonQuery();
    }

    private static Expense ReadExpense(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetInt64(1) / 100m,
        reader.GetString(2),
        DateTime.ParseExact(
            reader.GetString(3),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal));

    private static long ToCents(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    // Фиксированный формат: даты в таком виде сравниваются как обычные строки.
    private static string FormatDate(DateTime utc) =>
        utc.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
}
