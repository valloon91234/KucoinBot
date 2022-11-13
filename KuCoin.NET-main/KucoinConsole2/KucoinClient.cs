using KuCoin.NET.Data.Market;
using KuCoin.NET.Data.Order;
using KuCoin.NET.Data.User;
using KuCoin.NET.Rest;
using Newtonsoft.Json.Linq;
using System.Data.SqlTypes;
using System.Globalization;

namespace Valloon.Kucoin
{
    internal class KucoinClient
    {
        static string? KucoinKey;
        static string? KucoinSecret;
        static string? KucoinPassphrase;
        static User UserApi;
        static Trade TradeApi;

        public static void Init()
        {
            KucoinKey = DotNetEnv.Env.GetString("KUCOIN_KEY");
            KucoinSecret = DotNetEnv.Env.GetString("KUCOIN_SECRET");
            KucoinPassphrase = DotNetEnv.Env.GetString("KUCOIN_PASSPHRASE");
            UserApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
            TradeApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
        }

        public static string FindSymbol(string pattern)
        {
            try
            {
                Market.Instance.RefreshSymbolsAsync().Wait();
                var symbols = Market.Instance.FilterSymbolsByCurrency("USDT");
                List<string> resultList = new();
                foreach (var key in symbols.Keys)
                {
                    if (key.Contains(pattern, StringComparison.OrdinalIgnoreCase)) resultList.Add(key + "    " + symbols[key].QuoteMaxSize);
                }
                if (resultList.Count > 0)
                {
                    //var resultText = JContainer.FromObject(resultList).ToString();
                    var resultText = string.Join('\n', resultList).Trim();
                    return "<pre>" + resultText + "</pre>";
                }
                else
                {
                    return "Not found";
                }
            }
            catch (Exception ex)
            {
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return message;
            }
        }

        public static string Balance(string? currency = null, string? accountType = null)
        {
            if (currency == "*") currency = null;
            try
            {
                User userApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
                var balanceResult = userApi.GetAccountList(currency, accountType == null ? null : AccountType.Parse(accountType)).Result;
                var balanceResultText = JContainer.FromObject(balanceResult).ToString();
                return balanceResultText;
            }
            catch (Exception ex)
            {
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return message;
            }
        }

        public static string? Symbol;
        public static DateTime? StartTime;
        static string Currency;
        static decimal StartPrice;
        static OrderReceipt? CloseOrderResult;
        static Logger logger = new($"{DateTime.UtcNow:yyyy-MM-dd  HHmmss}", "log");

        public static void Run()
        {
            decimal size = decimal.Parse(DotNetEnv.Env.GetString("SIZE"));
            decimal maxFirstCandleX1 = decimal.Parse(DotNetEnv.Env.GetString("MAX_FIRST_CANDLE_X_1"));
            decimal closeX1 = decimal.Parse(DotNetEnv.Env.GetString("CLOSE_1"));
            decimal stopX1 = decimal.Parse(DotNetEnv.Env.GetString("STOP_1"));
            int timeout1 = DotNetEnv.Env.GetInt("TIMEOUT_1");
            decimal closeXX1 = decimal.Parse(DotNetEnv.Env.GetString("CLOSE2_1"));
            int timeoutStop1 = DotNetEnv.Env.GetInt("TIMEOUTSTOP_1");
            decimal maxFirstCandleX2 = decimal.Parse(DotNetEnv.Env.GetString("MAX_FIRST_CANDLE_X_2"));
            decimal closeX2 = decimal.Parse(DotNetEnv.Env.GetString("CLOSE_2"));
            decimal stopX2 = decimal.Parse(DotNetEnv.Env.GetString("STOP_2"));
            int timeout2 = DotNetEnv.Env.GetInt("TIMEOUT_2");
            decimal closeXX2 = decimal.Parse(DotNetEnv.Env.GetString("CLOSE2_2"));
            int timeoutStop2 = DotNetEnv.Env.GetInt("TIMEOUTSTOP_2");
            decimal volume1 = decimal.Parse(DotNetEnv.Env.GetString("VOLUME_1"));
            decimal volume2 = decimal.Parse(DotNetEnv.Env.GetString("VOLUME_2"));
            int availableTimeout = DotNetEnv.Env.GetInt("AVAILABLE_TIMEOUT");
            int interval = DotNetEnv.Env.GetInt("INTERVAL", 5);

            logger = new($"{DateTime.UtcNow:yyyy-MM-dd  HHmmss}", "log");
            logger.WriteLine($"size = {size}");
            logger.WriteLine($"maxFirstCandleX1 = {maxFirstCandleX1}");
            logger.WriteLine($"closeX1 = {closeX1}");
            logger.WriteLine($"stopX1 = {stopX1}");
            logger.WriteLine($"timeout1 = {timeout1}");
            logger.WriteLine($"closeXX1 = {closeXX1}");
            logger.WriteLine($"timeoutStop1 = {timeoutStop1}");
            logger.WriteLine($"maxFirstCandleX2 = {maxFirstCandleX2}");
            logger.WriteLine($"closeX2 = {closeX2}");
            logger.WriteLine($"stopX2 = {stopX2}");
            logger.WriteLine($"timeout2 = {timeout2}");
            logger.WriteLine($"closeXX2 = {closeXX2}");
            logger.WriteLine($"timeoutStop2 = {timeoutStop2}");
            logger.WriteLine($"volume1 = {volume1}");
            logger.WriteLine($"volume2 = {volume2}");
            logger.WriteLine($"availableTimeout = {availableTimeout}");
            logger.WriteLine($"interval = {interval}");

        line_standby:
            logger.WriteLine();
            Console.WriteLine($"Running...");
            while (Symbol == null || StartTime == null)
            {
                Thread.Sleep(1000);
            }

            var StartTimeValue = StartTime.Value;
            Console.WriteLine($"Symbol = {Symbol}");
            Console.WriteLine($"Waiting for {StartTimeValue:yyyy-MM-dd HH:mm} ...");
            TelegramClient.ReplyMessage($"<pre>Symbol = {Symbol}\nStartTime = {StartTimeValue:yyyy-MM-dd HH:mm:ss}\nNow = {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</pre>", Telegram.Bot.Types.Enums.ParseMode.Html);
            Currency = Symbol.Split('-')[0];
            while (DateTime.UtcNow < StartTimeValue)
            {
                Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss:fff}");
                if (Symbol == null || StartTime == null)
                    goto line_standby;
                Thread.Sleep(1000);
            }

            //try
            //{
            //    var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
            //    var balance = balanceResult[0].Available;
            //    if (balance > 0)
            //    {
            //        logger.WriteLine($"{currency} balance = {balance}", ConsoleColor.Red);
            //        goto makeCloseOrder;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
            //    logger.WriteLine(message, ConsoleColor.Red, false);
            //    logger.WriteFile(ex.ToString());
            //    if (message.Contains("TooManyRequests") || message.Contains("Balance insufficient!")) return;
            //}

            Candle firstCandle;
            while (true)
            {
                try
                {
                    var candleList = Market.Instance.GetKline(Symbol, KlineType.Min1).Result;
                    if (candleList.Count < 2)
                    {
                        logger.WriteLine($"Got only one candle! Try again in {interval} seconds...");
                        Thread.Sleep(interval * 1000);
                        continue;
                    }
                    firstCandle = candleList[0];
                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine(message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                }
                Thread.Sleep(interval * 1000);
            }

            var firstVolume = firstCandle.Volume;
            var firstCandleX = firstCandle.ClosePrice / firstCandle.OpenPrice;
            TelegramClient.ReplyMessage($"<pre>firstVolume = {firstVolume}\nfirstCandleX = {firstCandleX:F4}</pre>", Telegram.Bot.Types.Enums.ParseMode.Html);

            if (firstVolume < volume1)
            {
                logger.WriteLine($"Too low first volume: {firstVolume}");
                TelegramClient.ReplyMessage($"Too low first volume: {firstVolume}");
                return;
            }

            decimal closeX;
            decimal stopX;
            int timeout;
            decimal closeXX;
            int timeoutStop;

            if (firstVolume < volume2)
            {
                if (firstCandleX > maxFirstCandleX1)
                {
                    logger.WriteLine($"Too high firstCandleX: {firstCandleX}");
                    TelegramClient.ReplyMessage($"Too high firstCandleX: {firstCandleX}");
                    return;
                }
                closeX = closeX1;
                stopX = stopX1;
                timeout = timeout1;
                closeXX = closeXX1;
                timeoutStop = timeoutStop1;
            }
            else
            {
                if (firstCandleX > maxFirstCandleX2)
                {
                    logger.WriteLine($"Too high firstCandleX: {firstCandleX}");
                    TelegramClient.ReplyMessage($"Too high firstCandleX: {firstCandleX}");
                    return;
                }
                closeX = closeX2;
                stopX = stopX2;
                timeout = timeout2;
                closeXX = closeXX2;
                timeoutStop = timeoutStop2;
            }

            bool positionOpened = false;
            while (DateTime.UtcNow < StartTimeValue.AddSeconds(availableTimeout))
            {
                try
                {
                    var orderResult = TradeApi.CreateMarketSpotOrder(new MarketOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Buy,
                        Symbol = Symbol,
                        Type = OrderType.Market,
                        Remark = $"Open",
                        Funds = size
                    }).Result;
                    var orderResultText = JToken.FromObject(orderResult).ToString();
                    logger.WriteLine("Open order: " + orderResultText);
                    positionOpened = true;
                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine(message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                    if (message.Contains("TooManyRequests") || message.Contains("Balance insufficient!"))
                    {
                        TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                        return;
                    }
                }
                Thread.Sleep(interval * 1000);
            }
            if (!positionOpened)
            {
                string message = $"Over!    target = {StartTimeValue:yyyy-MM-dd HH:mm:ss}    now = {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                logger.WriteLine(message, ConsoleColor.Red);
                TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                return;
            }

            logger.WriteLine();
            int scale;
            while (true)
            {
                try
                {
                    var balanceResult = UserApi.GetAccountList(Currency, AccountType.Trading).Result;
                    var balance = balanceResult[0].Available;
                    if (balance == 0)
                    {
                        string message = $"{Currency} balance = {balance}";
                        logger.WriteLine(message, ConsoleColor.Red);
                        TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                        return;
                    }
                    var entryPrice = size / balance;
                    var ticker = Market.Instance.GetTicker(Symbol).Result;
                    var currentPrice = ticker.Price;
                    scale = ((SqlDecimal)currentPrice).Scale;
                    decimal closePrice = decimal.Round(entryPrice * (1 + closeX), scale);
                    decimal closeSize = decimal.Round(balance, 4, MidpointRounding.ToZero);
                    CloseOrderResult = TradeApi.CreateLimitSpotOrder(new LimitOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Sell,
                        Symbol = Symbol,
                        Type = OrderType.Limit,
                        Remark = $"Close",
                        Price = closePrice,
                        Size = closeSize
                    }).Result;
                    var orderResultText = JToken.FromObject(CloseOrderResult).ToString();
                    logger.WriteLine("close order: " + orderResultText);
                    {
                        decimal entryPricePrint = decimal.Round(entryPrice, scale);
                        string message = $"<pre>{Currency} price = {currentPrice}\n{Currency} balance = {balance}\nEntryPrice = {entryPricePrint}\nClose price = {closePrice}\nClose size = {closeSize}</pre>";
                        logger.WriteLine(message, ConsoleColor.Green);
                        TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine("close order: " + message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                }
                Thread.Sleep(1000);
            }

            logger.WriteLine();
            while (true)
            {
                int t = (int)(StartTimeValue.AddMinutes(timeout) - DateTime.UtcNow).TotalSeconds;
                if (t < 0) break;
                Logger.WriteLine($"Next close timeout = {t}", ConsoleColor.DarkGray);
                Thread.Sleep(1000);
            }

            logger.WriteLine();
            while (CloseOrderResult != null)
            {
                try
                {
                    var canceledResult = TradeApi.CancelOrderById(CloseOrderResult.OrderId).Result;
                    var canceledResultText = JToken.FromObject(canceledResult).ToString();
                    logger.WriteLine("order canceled: " + canceledResultText, ConsoleColor.Green);
                    CloseOrderResult = null;
                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine("cancel order: " + message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                    if (message.Contains("order_not_exist_or_not_allow_to_cancel"))
                    {
                        CloseOrderResult = null;
                        break;
                    }
                }
                Thread.Sleep(1000);
            }

            while (true)
            {
                try
                {
                    var balanceResult = UserApi.GetAccountList(Currency, AccountType.Trading).Result;
                    var balance = balanceResult[0].Available;
                    if (balance == 0)
                    {
                        string message = $"{Currency} balance = {balance}";
                        logger.WriteLine(message, ConsoleColor.Red);
                        TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                        return;
                    }
                    var entryPrice = size / balance;
                    decimal closePrice = decimal.Round(entryPrice * (1 + closeXX), scale);
                    decimal closeSize = decimal.Round(balance, 4, MidpointRounding.ToZero);
                    {
                        decimal entryPricePrint = decimal.Round(entryPrice, scale);
                        string message = $"<pre>{Currency} balance = {balance}\nEntryPrice = {entryPricePrint}\nSecond close price = {closePrice}\nClose size = {closeSize}</pre>";
                        logger.WriteLine(message, ConsoleColor.Green);
                        TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                    }
                    CloseOrderResult = TradeApi.CreateLimitSpotOrder(new LimitOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Sell,
                        Symbol = Symbol,
                        Type = OrderType.Limit,
                        Remark = $"Close",
                        Price = closePrice,
                        Size = closeSize
                    }).Result;
                    var orderResultText = JToken.FromObject(CloseOrderResult).ToString();
                    logger.WriteLine("Second close order: " + orderResultText);
                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine("Second close order: " + message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                }
                Thread.Sleep(1000);
            }

            logger.WriteLine();
            while (true)
            {
                int t = (int)(StartTimeValue.AddMinutes(timeoutStop) - DateTime.UtcNow).TotalSeconds;
                if (t < 0) break;
                Logger.WriteLine($"Stop timeout = {t}", ConsoleColor.DarkGray);
                Thread.Sleep(1000);
            }
            while (true)
            {
                if (CloseOrderResult != null)
                    try
                    {
                        var canceledResult = TradeApi.CancelOrderById(CloseOrderResult.OrderId).Result;
                        var canceledResultText = JToken.FromObject(canceledResult).ToString();
                        logger.WriteLine("order canceled: " + canceledResultText, ConsoleColor.Green);
                        CloseOrderResult = null;
                    }
                    catch (Exception ex)
                    {
                        string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                        logger.WriteLine("cancel order: " + message, ConsoleColor.Red, false);
                        logger.WriteFile(ex.ToString());
                        if (message.Contains("order_not_exist_or_not_allow_to_cancel")) CloseOrderResult = null;
                    }
                try
                {
                    var balanceResult = UserApi.GetAccountList(Currency, AccountType.Trading).Result;
                    var balance = balanceResult[0].Available;
                    {
                        string message = $"{Currency} balance = {balance}";
                        logger.WriteLine(message);
                        TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                    }
                    if (balance == 0) break;

                    var orderResult = TradeApi.CreateMarketSpotOrder(new MarketOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Sell,
                        Symbol = Symbol,
                        Type = OrderType.Market,
                        Remark = $"Close",
                        Funds = 1000000
                    }).Result;
                    var orderResultText = JToken.FromObject(orderResult).ToString();
                    logger.WriteLine("stop order: " + orderResultText, ConsoleColor.Green);
                    continue;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine("stop order: " + message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                }
                Thread.Sleep(1000);
            }
        }

        public static void CloseMarket()
        {
            while (true)
            {
                if (CloseOrderResult != null)
                    try
                    {
                        var canceledResult = TradeApi.CancelOrderById(CloseOrderResult.OrderId).Result;
                        var canceledResultText = JToken.FromObject(canceledResult).ToString();
                        logger.WriteLine("order canceled: " + canceledResultText, ConsoleColor.Green);
                        CloseOrderResult = null;
                    }
                    catch (Exception ex)
                    {
                        string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                        logger.WriteLine("cancel order: " + message, ConsoleColor.Red, false);
                        logger.WriteFile(ex.ToString());
                        if (message.Contains("order_not_exist_or_not_allow_to_cancel")) CloseOrderResult = null;
                    }
                try
                {
                    var balanceResult = UserApi.GetAccountList(Currency, AccountType.Trading).Result;
                    var balance = balanceResult[0].Available;
                    {
                        string message = $"{Currency} balance = {balance}";
                        logger.WriteLine(message);
                        TelegramClient.ReplyMessage(message, Telegram.Bot.Types.Enums.ParseMode.Html);
                    }
                    if (balance == 0) break;

                    var orderResult = TradeApi.CreateMarketSpotOrder(new MarketOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Sell,
                        Symbol = Symbol,
                        Type = OrderType.Market,
                        Remark = $"Close",
                        Funds = 1000000
                    }).Result;
                    var orderResultText = JToken.FromObject(orderResult).ToString();
                    logger.WriteLine("stop order: " + orderResultText, ConsoleColor.Green);
                    continue;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine("stop order: " + message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                }
                Thread.Sleep(1000);
            }
        }

        public static string BalanceNow()
        {
            if (Currency == null) return "Currency not selected.";
            try
            {
                var balanceResult = UserApi.GetAccountList(Currency, AccountType.Trading).Result;
                var balance = balanceResult[0].Available;
                return $"<pre>{Currency} Balance = {balance}</pre>";
            }
            catch (Exception ex)
            {
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                logger.WriteLine("stop order: " + message, ConsoleColor.Red, false);
                logger.WriteFile(ex.ToString());
                return message;
            }
        }
    }
}
