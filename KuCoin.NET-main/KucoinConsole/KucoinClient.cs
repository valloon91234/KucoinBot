using KuCoin.NET.Data.Market;
using KuCoin.NET.Data.Order;
using KuCoin.NET.Data.User;
using KuCoin.NET.Helpers;
using KuCoin.NET.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlTypes;
using System.Drawing;
using Telegram.Bot.Types.Enums;

namespace Valloon.Kucoin
{
    internal class KucoinClient
    {
        static string? KucoinKey;
        static string? KucoinSecret;
        static string? KucoinPassphrase;

        static string? Symbol;
        static int Timeout;
        //static decimal CloseSize;
        static decimal StartPrice, BuyPrice;

        static Logger? logger;

        public static void Init()
        {
            KucoinKey = DotNetEnv.Env.GetString("KUCOIN_KEY");
            KucoinSecret = DotNetEnv.Env.GetString("KUCOIN_SECRET");
            KucoinPassphrase = DotNetEnv.Env.GetString("KUCOIN_PASSPHRASE");
            logger = new Logger($"{DateTime.UtcNow:yyyy-MM-dd}", "log");
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
                logger!.WriteFile(ex.ToString());
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
                logger!.WriteFile(ex.ToString());
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return message;
            }
        }

        static OrderReceipt? closeOrderResult;
        static byte Scale;
        static decimal CoinBalance;

        public static string Buy(string symbol, decimal size, int timeout, decimal closeX, out Exception? e)
        {
            string returnMessage = "";
            Trade tradeApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
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
                var orderResultText = JContainer.FromObject(orderResult).ToString();
                logger!.WriteLine(orderResultText, ConsoleColor.Green);
                returnMessage += $"Bought: size = {size}\n";
                Symbol = symbol;
                Timeout = timeout;
            }
            catch (Exception ex)
            {
                logger!.WriteFile(ex.ToString());
                Symbol = null;
                Timeout = 0;
                e = ex;
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return $"Failed to buy: {message}";
            }
            try
            {
                User userApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
                var currency = Symbol.Split('-')[0];
                var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                CoinBalance = balanceResult[0].Available;

                var candleList = Market.Instance.GetKline(Symbol, KlineType.Min1).Result;
                if (candleList.Count > 1)
                    StartPrice = candleList[candleList.Count - 2].OpenPrice;
                else
                    StartPrice = candleList.Last().OpenPrice;

                var ticker = Market.Instance.GetTicker(Symbol).Result;
                var currentPrice = ticker.Price;
                Scale = ((SqlDecimal)currentPrice).Scale;
                BuyPrice = size / CoinBalance;

                decimal closePrice = decimal.Round(BuyPrice * closeX, Scale);
                decimal closeSize = decimal.Round(CoinBalance, 4, MidpointRounding.ToZero);

                closeOrderResult = tradeApi.CreateLimitSpotOrder(new LimitOrder
                {
                    ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                    Side = Side.Sell,
                    Symbol = Symbol,
                    Type = OrderType.Limit,
                    Remark = $"Close",
                    Price = closePrice,
                    Size = closeSize
                }).Result;
                var orderResultText2 = JContainer.FromObject(closeOrderResult).ToString();

                e = null;
                returnMessage += $"<pre>{currency} Balance = {CoinBalance}\nstart = {StartPrice}\nentry = {BuyPrice:F3}\nclose = {closePrice}</pre>";
            }
            catch (Exception ex)
            {
                logger!.WriteFile(ex.ToString());
                e = ex;
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                returnMessage += message;
            }
            return returnMessage;
        }

        public static string BalanceNow()
        {
            if (Symbol == null) return "Symbol not selected.";
            try
            {
                User userApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
                var currency = Symbol.Split('-')[0];
                var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                var balance = balanceResult[0].Available;
                return $"<pre>{currency} Balance = {balance}</pre>";
            }
            catch (Exception ex)
            {
                logger!.WriteFile(ex.ToString());
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return message;
            }
        }

        public static string Close2(decimal newCloseX)
        {
            Trade tradeApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
            while (closeOrderResult != null)
            {
                try
                {
                    var canceledResult = tradeApi.CancelOrderById(closeOrderResult.OrderId).Result;
                    var canceledResultText = JToken.FromObject(canceledResult).ToString();
                    logger!.WriteLine("order canceled: " + canceledResultText, ConsoleColor.Green);
                    closeOrderResult = null;
                    break;
                }
                catch (Exception ex)
                {
                    logger!.WriteFile(ex.ToString());
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger!.WriteLine("cancel order: " + message, ConsoleColor.Red, false);
                    if (message.Contains("order_not_exist_or_not_allow_to_cancel"))
                    {
                        closeOrderResult = null;
                        break;
                    }
                }
                Thread.Sleep(1000);
            }

            decimal closePrice = decimal.Round(BuyPrice * newCloseX, Scale);
            decimal closeSize = decimal.Round(CoinBalance, 4, MidpointRounding.ToZero);

            try
            {
                closeOrderResult = tradeApi.CreateLimitSpotOrder(new LimitOrder
                {
                    ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                    Side = Side.Sell,
                    Symbol = Symbol,
                    Type = OrderType.Limit,
                    Remark = $"Close",
                    Price = closePrice,
                    Size = closeSize
                }).Result;
                var orderResultText = JContainer.FromObject(closeOrderResult).ToString();
                logger!.WriteLine(orderResultText, ConsoleColor.Green);
                return $"<pre>New Close = {closePrice}</pre>";
            }
            catch (Exception ex)
            {
                logger!.WriteFile(ex.ToString());
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return message;
            }
        }

        public static string Sell(string? symbol = null, decimal? size = null, int timeout = 0)
        {
            if (symbol != null) Symbol = symbol;
            if (Symbol == null) return "Nothing to sell.";
            if (timeout > 0)
            {
                Timeout = timeout;
                return $"Timeout = {Timeout}";
            }

            Trade tradeApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
            while (closeOrderResult != null)
            {
                try
                {
                    var canceledResult = tradeApi.CancelOrderById(closeOrderResult.OrderId).Result;
                    var canceledResultText = JToken.FromObject(canceledResult).ToString();
                    logger!.WriteLine("order canceled: " + canceledResultText, ConsoleColor.Green);
                    closeOrderResult = null;
                    break;
                }
                catch (Exception ex)
                {
                    logger.WriteFile(ex.ToString());
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    logger!.WriteLine("cancel order: " + message, ConsoleColor.Red, false);
                    if (message.Contains("order_not_exist_or_not_allow_to_cancel"))
                    {
                        closeOrderResult = null;
                        break;
                    }
                }
                Thread.Sleep(1000);
            }

            try
            {
                var orderResult = tradeApi.CreateMarketSpotOrder(new MarketOrder
                {
                    ClientOid = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff"),
                    Side = Side.Sell,
                    Symbol = Symbol,
                    Type = OrderType.Market,
                    Remark = $"Close",
                    Funds = size == null ? 1000000 : size
                }).Result;
                var orderResultText = JContainer.FromObject(orderResult).ToString();
                logger!.WriteLine(orderResultText, ConsoleColor.Green);

                var currency = Symbol.Split('-')[0];
                User userApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
                var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                var balance = balanceResult[0].Available;

                if (balance == 0)
                {
                    Symbol = null;
                    StartPrice = 0;
                    BuyPrice = 0;
                }
                string replyMessge = $"<pre>Result {currency} Balance = {balance}</pre>";
                return replyMessge;
            }
            catch (Exception ex)
            {
                logger!.WriteFile(ex.ToString());
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return message;
            }
        }

        public static string Info()
        {
            if (Symbol == null) return "Symbol is null.";
            if (StartPrice == 0) return "StartPrice is null.";
            if (BuyPrice == 0) return "BuyPrice is null.";
            try
            {
                var ticker = Market.Instance.GetTicker(Symbol).Result;
                var currentPrice = ticker.Price;
                return $"<pre>start = {StartPrice} / {currentPrice} = {currentPrice / StartPrice:F2}\nbuy = {BuyPrice:F3} / {currentPrice} = {currentPrice / BuyPrice:F2}</pre>";
            }
            catch (Exception ex)
            {
                logger!.WriteFile(ex.ToString());
                string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                Logger.WriteLine(message, ConsoleColor.Red);
                return message;
            }
        }

        public static void Run()
        {
            while (true)
            {
                if (Symbol == null || Timeout <= 0) goto endLoop;
                Timeout--;
                Console.WriteLine($"Timeout = {Timeout}");
                if (Timeout == 0)
                {
                    var replyMessage = Sell();
                    TelegramClient.ReplyMessage(replyMessage, ParseMode.Html);
                }
            endLoop:
                Thread.Sleep(1000);
            }
        }

    }
}
