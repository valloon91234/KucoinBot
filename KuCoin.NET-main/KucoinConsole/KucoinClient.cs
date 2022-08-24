using KuCoin.NET.Data.Market;
using KuCoin.NET.Data.Order;
using KuCoin.NET.Data.User;
using KuCoin.NET.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                    if (key.Contains(pattern, StringComparison.OrdinalIgnoreCase)) resultList.Add(key);
                }
                if (resultList.Count > 0)
                {
                    var resultText = string.Join('\n', resultList).Trim();
                    return resultText;
                }
                else
                {
                    return "Not found";
                }
            }
            catch (Exception ex)
            {
                logger!.WriteLine(ex.ToString(), ConsoleColor.Red);
                return ex.InnerException == null ? ex.Message : ex.InnerException.Message;
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
                logger!.WriteLine(ex.ToString(), ConsoleColor.Red);
                return ex.InnerException == null ? ex.Message : ex.InnerException.Message;
            }
        }

        public static string Buy(string symbol, decimal size, int timeout)
        {
            try
            {
                Trade tradeApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
                var orderResult = tradeApi.CreateMarketSpotOrder(new MarketOrder
                {
                    ClientOid = DateTime.UtcNow.ToString("HHmmssfff"),
                    Side = Side.Buy,
                    Symbol = symbol,
                    Type = OrderType.Market,
                    Remark = $"Open",
                    Funds = size
                }).Result;
                var orderResultText = JContainer.FromObject(orderResult).ToString();
                logger!.WriteLine(orderResultText, ConsoleColor.Green);

                Symbol = symbol;
                Timeout = timeout;

                User userApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
                var currency = symbol.Split('-')[0];
                var balanceResult = userApi.GetAccountList(currency, AccountType.Trading).Result;
                var balance = balanceResult[0].Available;

                var candleList = Market.Instance.GetKline(Symbol, KlineType.Min1).Result;
                StartPrice = candleList[candleList.Count - 2].OpenPrice;

                var ticker = Market.Instance.GetTicker(Symbol).Result;
                var currentPrice = ticker.Price;
                BuyPrice = size / balance;

                return $"<pre>{currency} Balance = {balance}\nstart = {StartPrice} / {currentPrice} = {currentPrice / StartPrice:F2}\nbuy = {BuyPrice:F3} / {currentPrice} = {currentPrice / BuyPrice:F2}</pre>";
            }
            catch (Exception ex)
            {
                logger!.WriteLine(ex.ToString(), ConsoleColor.Red);
                Symbol = null;
                Timeout = 0;
                return ex.InnerException == null ? ex.Message : ex.InnerException.Message;
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
            try
            {
                Trade tradeApi = new(KucoinKey, KucoinSecret, KucoinPassphrase);
                var orderResult = tradeApi.CreateMarketSpotOrder(new MarketOrder
                {
                    ClientOid = DateTime.UtcNow.ToString("HHmmssfff"),
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
                logger!.WriteLine(ex.ToString(), ConsoleColor.Red);
                return ex.InnerException == null ? ex.Message : ex.InnerException.Message;
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
                logger!.WriteLine(ex.ToString(), ConsoleColor.Red);
                return ex.InnerException == null ? ex.Message : ex.InnerException.Message;
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
