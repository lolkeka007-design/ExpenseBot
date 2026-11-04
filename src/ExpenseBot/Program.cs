using System.Globalization;
using ExpenseBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// Settings come from environment variables, so the token never ends up in the repository.
var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set the bot token first: export TELEGRAM_BOT_TOKEN=\"123456:ABC...\" (get it from @BotFather).");
    return 1;
}

var databasePath = Environment.GetEnvironmentVariable("DB_PATH") ?? "expenses.db";
var currency = Environment.GetEnvironmentVariable("CURRENCY") ?? "USD";

var languageCode = Environment.GetEnvironmentVariable("BOT_LANGUAGE");
var messages = Localization.Get(languageCode);
if (messages is null)
{
    Console.Error.WriteLine("BOT_LANGUAGE must be \"en\" or \"ru\".");
    return 1;
}

var offsetText = Environment.GetEnvironmentVariable("UTC_OFFSET_HOURS");
var offsetHours = 0.0;
if (!string.IsNullOrWhiteSpace(offsetText) &&
    !double.TryParse(offsetText, NumberStyles.Float, CultureInfo.InvariantCulture, out offsetHours))
{
    Console.Error.WriteLine("UTC_OFFSET_HOURS must be a number, for example 4 or -5.");
    return 1;
}

var store = new SqliteExpenseStore(databasePath);
var processor = new CommandProcessor(store, messages, currency, TimeSpan.FromHours(offsetHours), () => DateTime.UtcNow);
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
Console.WriteLine($"Bot @{me.Username} is running. Press Ctrl+C to stop.");

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Stopped.");
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
