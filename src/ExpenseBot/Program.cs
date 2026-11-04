using System.Globalization;
using ExpenseBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// Настройки берём из переменных окружения, чтобы токен не попал в репозиторий.
var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Задай токен бота: export TELEGRAM_BOT_TOKEN=\"123456:ABC...\" (получить его можно у @BotFather).");
    return 1;
}

var databasePath = Environment.GetEnvironmentVariable("DB_PATH") ?? "expenses.db";
var currency = Environment.GetEnvironmentVariable("CURRENCY") ?? "₾";

var offsetText = Environment.GetEnvironmentVariable("UTC_OFFSET_HOURS");
var offsetHours = 4.0; // Тбилиси
if (!string.IsNullOrWhiteSpace(offsetText) &&
    !double.TryParse(offsetText, NumberStyles.Float, CultureInfo.InvariantCulture, out offsetHours))
{
    Console.Error.WriteLine("UTC_OFFSET_HOURS должен быть числом, например 4 или -5.");
    return 1;
}

var store = new SqliteExpenseStore(databasePath);
var processor = new CommandProcessor(store, currency, TimeSpan.FromHours(offsetHours), () => DateTime.UtcNow);
var bot = new TelegramBotClient(token);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

bot.StartReceiving(
    updateHandler: HandleUpdate,
    errorHandler: HandleError,
    receiverOptions: new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } },
    cancellationToken: cts.Token);

var me = await bot.GetMe(cts.Token);
Console.WriteLine($"Бот @{me.Username} запущен. Остановить: Ctrl+C.");

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Остановлен.");
}

return 0;

async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
{
    if (update.Message is not { Text: { } text } message)
        return;

    var userId = message.From?.Id ?? message.Chat.Id;
    var reply = processor.Process(userId, text);
    await client.SendMessage(message.Chat.Id, reply, cancellationToken: ct);
}

Task HandleError(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken ct)
{
    Console.Error.WriteLine($"[{source}] {exception.Message}");
    return Task.CompletedTask;
}
