using KuCoin.NET.Rest;

namespace Valloon.Kucoin
{
    internal class KucoinClient
    {
        static string? Symbol;
        static int Interval = 1;

        public static void Init()
        {
            Symbol = DotNetEnv.Env.GetString("SYMBOL");
            Interval = DotNetEnv.Env.GetInt("Interval", 1000);
        }

        public static void Run()
        {
            Logger logger = new($"{DateTime.UtcNow:yyyy-MM-dd  HHmmss}  {Symbol}", "log");
            while (true)
            {
                try
                {
                    var ticker = Market.Instance.GetTicker(Symbol).Result;
                    var currentPrice = ticker.Price;
                    string message = $"{DateTime.UtcNow:yyyy-MM-dd  HH:mm:ss:fff}    {ticker.Timestamp.ToUniversalTime():HH:mm:ss:fff} \t {currentPrice}";
                    logger.WriteLine(message, ConsoleColor.Green);
                    Console.Title = $"{currentPrice}  |  {Symbol}";
                }
                catch (Exception ex)
                {
                    logger.WriteLine(ex.ToString(), ConsoleColor.Red);
                }
                Thread.Sleep(Interval);
            }
        }

    }
}
