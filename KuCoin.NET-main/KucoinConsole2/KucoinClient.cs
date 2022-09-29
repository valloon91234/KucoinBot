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

        public static void Init()
        {
            KucoinKey = DotNetEnv.Env.GetString("KUCOIN_KEY");
            KucoinSecret = DotNetEnv.Env.GetString("KUCOIN_SECRET");
            KucoinPassphrase = DotNetEnv.Env.GetString("KUCOIN_PASSPHRASE");
        }

        public static void Run()
        {
            string symbol = DotNetEnv.Env.GetString("SYMBOL");
            string timeString = DotNetEnv.Env.GetString("TIME");
            DateTime time = DateTime.ParseExact(timeString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            decimal size = decimal.Parse(DotNetEnv.Env.GetString("SIZE"));
            decimal closeX = decimal.Parse(DotNetEnv.Env.GetString("CLOSE"));
            int timeout = DotNetEnv.Env.GetInt("TIMEOUT_1", 90);
            decimal closeX2 = decimal.Parse(DotNetEnv.Env.GetString("CLOSE_2"));
            int timeout2 = DotNetEnv.Env.GetInt("TIMEOUT_2", 15);
            int interval = DotNetEnv.Env.GetInt("INTERVAL", 5);

            Logger logger = new($"{DateTime.UtcNow:yyyy-MM-dd  HHmmss}  {symbol}", "log");
            logger.WriteLine($"symbol = {symbol}");
            logger.WriteLine($"time = {time:yyyy-MM-dd HH:mm}");
            logger.WriteLine($"size = {size}");
            logger.WriteLine($"CLOSE = {closeX}");
            logger.WriteLine($"TIMEOUT_1 = {timeout}");
            logger.WriteLine($"CLOSE_2 = {closeX2}");
            logger.WriteLine($"TIMEOUT_2 = {timeout2}");
            logger.WriteLine($"interval = {interval}");
            logger.WriteLine();

            var currency = symbol.Split('-')[0];

            Console.WriteLine($"Waiting for {time:yyyy-MM-dd HH:mm} ...");
            while (DateTime.UtcNow < time)
            {
                Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss:fff}");
                Thread.Sleep(1000);
            }

            User userApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
            Trade tradeApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);

            try
            {
                var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                var balance = balanceResult[0].Available;
                if (balance > 0)
                {
                    logger.WriteLine($"{currency} balance = {balance}", ConsoleColor.Red);
                    goto makeCloseOrder;
                }
            }
            catch (Exception ex)
            {
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                logger.WriteLine(message, ConsoleColor.Red, false);
                logger.WriteFile(ex.ToString());
                if (message.Contains("TooManyRequests") || message.Contains("Balance insufficient!")) return;
            }

            while (timeout == 0 || DateTime.UtcNow < time.AddMinutes(timeout))
            {
                try
                {
                    var orderResult = tradeApi.CreateMarketSpotOrder(new MarketOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Buy,
                        Symbol = symbol,
                        Type = OrderType.Market,
                        Remark = $"Open",
                        Funds = size
                    }).Result;
                    var orderResultText = JToken.FromObject(orderResult).ToString();
                    logger.WriteLine("Open order: " + orderResultText);
                    goto makeCloseOrder;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine(message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                    if (message.Contains("TooManyRequests") || message.Contains("Balance insufficient!")) return;
                }
                Thread.Sleep(interval * 1000);
            }
            logger.WriteLine($"Over!    target = {time:yyyy-MM-dd HH:mm:ss}    now = {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}", ConsoleColor.Red);

        makeCloseOrder:;
            logger.WriteLine();
            OrderReceipt? closeOrderResult;
            int scale;
            while (true)
            {
                try
                {
                    var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                    var balance = balanceResult[0].Available;
                    if (balance == 0)
                    {
                        logger.WriteLine($"{currency} balance = {balance}", ConsoleColor.Red);
                        return;
                    }
                    var entryPrice = size / balance;
                    logger.WriteLine($"{currency} balance = {balance}    entryPrice = {entryPrice}", ConsoleColor.Green);

                    //var candleList = Market.Instance.GetKline(symbol, KlineType.Min1).Result;
                    //if (candleList.Count > 1)
                    //    StartPrice = candleList[candleList.Count - 2].OpenPrice;
                    //else
                    //    StartPrice = candleList.Last().OpenPrice;

                    var ticker = Market.Instance.GetTicker(symbol).Result;
                    var currentPrice = ticker.Price;
                    scale = ((SqlDecimal)currentPrice).Scale;
                    decimal closePrice = decimal.Round(entryPrice * (1 + closeX), scale);
                    decimal closeSize = decimal.Round(balance, 4, MidpointRounding.ToZero);
                    logger.WriteLine($"{currency} price = {currentPrice}    scale = {scale}    close price = {closePrice}    close size = {closeSize}", ConsoleColor.Green);

                    closeOrderResult = tradeApi.CreateLimitSpotOrder(new LimitOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Sell,
                        Symbol = symbol,
                        Type = OrderType.Limit,
                        Remark = $"Close",
                        Price = closePrice,
                        Size = closeSize
                    }).Result;
                    var orderResultText = JToken.FromObject(closeOrderResult).ToString();
                    logger.WriteLine("close order: " + orderResultText);
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
                int t = (int)(time.AddMinutes(timeout) - DateTime.UtcNow).TotalSeconds;
                if (t < 0) break;
                Logger.WriteLine($"Next close timeout = {t}", ConsoleColor.DarkGray);
                Thread.Sleep(1000);
            }

            logger.WriteLine();
            while (closeOrderResult != null)
            {
                try
                {
                    var canceledResult = tradeApi.CancelOrderById(closeOrderResult.OrderId).Result;
                    var canceledResultText = JToken.FromObject(canceledResult).ToString();
                    logger.WriteLine("order canceled: " + canceledResultText, ConsoleColor.Green);
                    closeOrderResult = null;
                    break;
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger.WriteLine("cancel order: " + message, ConsoleColor.Red, false);
                    logger.WriteFile(ex.ToString());
                    if (message.Contains("order_not_exist_or_not_allow_to_cancel"))
                    {
                        closeOrderResult = null;
                        break;
                    }
                }
                Thread.Sleep(1000);
            }

            while (true)
            {
                try
                {
                    var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                    var balance = balanceResult[0].Available;
                    if (balance == 0)
                    {
                        logger.WriteLine($"{currency} balance = {balance}", ConsoleColor.Red);
                        return;
                    }
                    var entryPrice = size / balance;
                    logger.WriteLine($"{currency} balance = {balance}    entryPrice = {entryPrice}", ConsoleColor.Green);

                    decimal closePrice = decimal.Round(entryPrice * (1 + closeX2), scale);
                    decimal closeSize = decimal.Round(balance, 4, MidpointRounding.ToZero);
                    logger.WriteLine($"Second close price = {closePrice}    close size = {closeSize}", ConsoleColor.Green);

                    closeOrderResult = tradeApi.CreateLimitSpotOrder(new LimitOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Sell,
                        Symbol = symbol,
                        Type = OrderType.Limit,
                        Remark = $"Close",
                        Price = closePrice,
                        Size = closeSize
                    }).Result;
                    var orderResultText = JToken.FromObject(closeOrderResult).ToString();
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
                int t = (int)(time.AddMinutes(timeout2) - DateTime.UtcNow).TotalSeconds;
                if (t < 0) break;
                Logger.WriteLine($"Stop timeout = {t}", ConsoleColor.DarkGray);
                Thread.Sleep(1000);
            }
            while (true)
            {
                if (closeOrderResult != null)
                    try
                    {
                        var canceledResult = tradeApi.CancelOrderById(closeOrderResult.OrderId).Result;
                        var canceledResultText = JToken.FromObject(canceledResult).ToString();
                        logger.WriteLine("order canceled: " + canceledResultText, ConsoleColor.Green);
                        closeOrderResult = null;
                    }
                    catch (Exception ex)
                    {
                        string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                        logger.WriteLine("cancel order: " + message, ConsoleColor.Red, false);
                        logger.WriteFile(ex.ToString());
                        if (message.Contains("order_not_exist_or_not_allow_to_cancel")) closeOrderResult = null;
                    }
                try
                {
                    var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                    var balance = balanceResult[0].Available;
                    logger.WriteLine($"{currency} balance = {balance}");
                    if (balance == 0) break;

                    var orderResult = tradeApi.CreateMarketSpotOrder(new MarketOrder
                    {
                        ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                        Side = Side.Sell,
                        Symbol = symbol,
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
            logger.WriteLine();
        }

    }
}
