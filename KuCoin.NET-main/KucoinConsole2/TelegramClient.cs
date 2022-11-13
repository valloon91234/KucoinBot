using KuCoin.NET.Data.Market;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Valloon.Kucoin
{
    internal class TelegramClient
    {
        static TelegramBotClient? Client;
        static User? Me { get; set; }
        static string[]? adminArray;
        static Logger? logger;

        public static void Init()
        {
            if (Debugger.IsAttached)
            {
                var proxy = new WebProxy
                {
                    Address = new Uri("socks5://54.249.108.164:8443")
                };
                //proxy.Credentials = new NetworkCredential(); //Used to set Proxy logins. 
                var handler = new HttpClientHandler
                {
                    Proxy = proxy
                };
                var httpClient = new HttpClient(handler);
                Client = new TelegramBotClient(DotNetEnv.Env.GetString("TELEGRAM_TOKEN"), httpClient);
            }
            else
            {
                Client = new TelegramBotClient(DotNetEnv.Env.GetString("TELEGRAM_TOKEN"));
            }
            using var cts = new CancellationTokenSource();
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };
            Client.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );
            Me = Client.GetMeAsync().Result;
            adminArray = DotNetEnv.Env.GetString("TELEGRAM_ADMIN")?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            logger = new Logger($"{DateTime.UtcNow:yyyy-MM-dd}", "log");
            logger.WriteLine($"Telegram connected: username = {Me.Username}");
            logger.WriteLine($"adminArray = {(adminArray == null ? "Null" : string.Join(",", adminArray))}");
        }

        static readonly Dictionary<string, string> LastCommand = new();

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                long chatId;
                int messageId;
                string chatUsername;
                string senderUsername;
                string receivedMessageText;
                // Only process Message updates: https://core.telegram.org/bots/api#message
                if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text && update.Message!.Chat.Type == ChatType.Private)
                {
                    // Only process text messages
                    chatId = update.Message.Chat.Id;
                    messageId = update.Message.MessageId;
                    chatUsername = update.Message.Chat.Username!;
                    senderUsername = update.Message.From!.Username!;
                    receivedMessageText = update.Message.Text!;
                    Logger.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]  \"{receivedMessageText}\" from {senderUsername}. chatId = {chatId}, messageId = {messageId}", ConsoleColor.DarkGray);
                }
                else if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text && (update.Message!.Chat.Type == ChatType.Group || update.Message!.Chat.Type == ChatType.Supergroup))
                {
                    chatId = update.Message.Chat.Id;
                    messageId = update.Message.MessageId;
                    chatUsername = update.Message.Chat.Username!;
                    senderUsername = update.Message.From!.Username!;
                    receivedMessageText = update.Message.Text!;
                    Logger.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]  \"{receivedMessageText}\" from {senderUsername}. chatId = {chatId}, messageId = {messageId}", ConsoleColor.DarkGray);
                    //if (receivedMessageText[0] == '/' && receivedMessageText.EndsWith($"@{Me!.Username}"))
                    //{
                    //    var command = receivedMessageText[..^$"@{Me!.Username}".Length];
                    //    bool isAdmin = adminArray != null && adminArray.Contains(senderUsername!);
                    //    switch (command)
                    //    {
                    //        case $"/start":
                    //            if (isAdmin)
                    //            {
                    //                string replyMessageText = chatId.ToString();
                    //                await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                    //            }
                    //            break;
                    //        case $"/stop":
                    //            if (isAdmin)
                    //            {
                    //                string replyMessageText = chatId.ToString();
                    //                await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                    //            }
                    //            break;
                    //    }
                    //}
                    //return;
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    chatId = update.CallbackQuery!.Message!.Chat.Id;
                    senderUsername = update.CallbackQuery.From.Username!;
                    receivedMessageText = update.CallbackQuery.Data!;
                    await botClient.AnswerCallbackQueryAsync(callbackQueryId: update.CallbackQuery!.Id, cancellationToken: cancellationToken);
                }
                else
                    return;
                {
                    bool isAdmin = adminArray != null && adminArray.Contains(senderUsername!);
                    if (receivedMessageText[0] == '/')
                    {
                        var command = receivedMessageText;
                        switch (command)
                        {
                            case "/start":
                                if (isAdmin)
                                {
                                    string replyMessageText = chatId.ToString();
                                    await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                                }
                                break;
                            case "/stop":
                                if (isAdmin)
                                {
                                    string replyMessageText = chatId.ToString();
                                    await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                                }
                                break;
                            case string x when x.StartsWith("/a", StringComparison.OrdinalIgnoreCase):
                                if (isAdmin)
                                {
                                    try
                                    {
                                        ReplyId = chatId;
                                        var array = command.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                        var symbol = array[1].ToUpper();
                                        string timeString = array[2];
                                        DateTime time = DateTime.ParseExact(timeString, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
                                        KucoinClient.Symbol = symbol;
                                        KucoinClient.StartTime = time;
                                    }
                                    catch (Exception ex)
                                    {
                                        string replyMessageText = "Invalid input: " + ex.Message;
                                        await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                                    }
                                }
                                break;
                            case "/f":
                                if (isAdmin)
                                {
                                    KucoinClient.Symbol = null;
                                    KucoinClient.StartTime = null;
                                    string replyMessageText = "Stopped";
                                    await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                                }
                                break;
                            case "/s":
                                if (isAdmin)
                                {
                                    try
                                    {
                                        ReplyId = chatId;
                                        KucoinClient.CloseMarket();
                                    }
                                    catch (Exception ex)
                                    {
                                        string replyMessageText = "Invalid input: " + ex.Message;
                                        await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                                    }
                                }
                                break;
                            case "/b":
                                if (isAdmin)
                                {
                                    string replyMessageText = KucoinClient.BalanceNow();
                                    await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                                }
                                break;
                            case string x when x.StartsWith("/balance", StringComparison.OrdinalIgnoreCase):
                                if (isAdmin)
                                {
                                    ReplyId = chatId;
                                    string? currency = null, accountType = null;
                                    var array = command.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                    if (array.Length > 1) currency = array[1].ToUpper();
                                    if (array.Length > 2) accountType = array[2].ToLower();
                                    string replyMessageText = KucoinClient.Balance(currency, accountType);
                                    await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                                }
                                break;
                            case string x when x.StartsWith("/symbol", StringComparison.OrdinalIgnoreCase):
                                if (isAdmin)
                                {
                                    ReplyId = chatId;
                                    try
                                    {
                                        var array = command.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                                        var pattern = array[1];
                                        string replyMessageText = KucoinClient.FindSymbol(pattern);
                                        await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                                    }
                                    catch (Exception ex)
                                    {
                                        string replyMessageText = "Invalid input: " + ex.Message;
                                        await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                                    }
                                }
                                break;
                            default:
                                {
                                    string replyMessageText = $"Unknown command: {command}";
                                    await botClient.SendTextMessageAsync(chatId: chatId, text: replyMessageText, cancellationToken: cancellationToken);
                                    logger!.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  replied: \"{replyMessageText}\"", ConsoleColor.DarkGray);
                                }
                                LastCommand.Remove(senderUsername);
                                break;
                        }
                    }
                    else if (LastCommand.ContainsKey(senderUsername!))
                    {
                        if (receivedMessageText == "exit" || receivedMessageText == "/exit")
                            LastCommand.Remove(senderUsername!);
                        else
                            switch (LastCommand[senderUsername!])
                            {
                                default:
                                    {
                                        logger!.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  <ERROR>  Unknown error", ConsoleColor.Red);
                                    }
                                    break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger!.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  <ERROR>  {(ex.InnerException == null ? ex.Message : ex.InnerException.Message)}", ConsoleColor.Red, false);
                logger!.WriteFile($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  <ERROR>  {ex}");
            }
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            if (logger != null) logger.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static void SendMessageToBroadcastGroup(string text, ParseMode? parseMode = default)
        {
            if (Client == null || adminArray == null) return;
            try
            {
                int count = 0;
                foreach (var chat in adminArray)
                {
                    if (string.IsNullOrWhiteSpace(chat)) continue;
                    var result = Client.SendTextMessageAsync(chatId: chat, text: text, disableWebPagePreview: true, parseMode: parseMode).Result;
                    count++;
                }
                logger.WriteFile($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  Message sent to {count} chats: {text}");
            }
            catch (Exception ex)
            {
                logger.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  <ERROR>  {(ex.InnerException == null ? ex.Message : ex.InnerException.Message)}", ConsoleColor.Red, false);
                logger.WriteFile($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  <ERROR>  {ex}");
            }
        }

        static long? ReplyId;

        public static void ReplyMessage(string text, ParseMode? parseMode = default)
        {
            if (ReplyId == null) return;
            try
            {
                var result = Client!.SendTextMessageAsync(chatId: ReplyId, text: text, disableWebPagePreview: true, parseMode: parseMode).Result;
            }
            catch (Exception ex)
            {
                logger.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  <ERROR>  {(ex.InnerException == null ? ex.Message : ex.InnerException.Message)}", ConsoleColor.Red, false);
                logger.WriteFile($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]  <ERROR>  {ex}");
            }
        }

    }
}
