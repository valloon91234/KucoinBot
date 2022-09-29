using KuCoin.NET.Data.Market;
using KuCoin.NET.Rest;
using System.Globalization;
using System.Text;

namespace Valloon.Kucoin
{
    internal static class Loader
    {
        public static bool LoadCSV(string symbol, string startTimeString, string? endTimeString = null)
        {
            DateTime startTime = DateTime.ParseExact(startTimeString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            startTime = startTime.ToUniversalTime();
            DateTime? endTime = null;
            if (endTimeString != null)
                endTime = DateTime.ParseExact(endTimeString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return LoadCSV(symbol, startTime, endTime);
        }

        public static bool LoadCSV(string symbol, DateTime startTime, DateTime? endTime = null)
        {
            Logger.WriteLine($"Starting  {symbol}  {startTime:yyyy-MM-dd HH:mm}");
            if (endTime == null) endTime = DateTime.UtcNow;
            string filename = $"data-{symbol}-1m  {startTime:yyyy-MM-dd HH.mm}.csv";
            File.Delete(filename);
            using var writer = new StreamWriter(filename, false, Encoding.UTF8);
            writer.WriteLine($"timestamp,open,high,low,close,volume");
            while (true)
            {
                try
                {
                    if (startTime > endTime.Value)
                        break;
                    var nextTime = startTime.AddDays(1);
                    var list = Market.Instance.GetKline(symbol, KlineType.Min1, startTime, nextTime).Result;
                    int count = list.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var t = list[i];
                        writer.WriteLine($"{t.Timestamp:yyyy-MM-dd HH:mm:ss},{t.OpenPrice},{t.HighPrice},{t.LowPrice},{t.ClosePrice},{t.Volume}");
                        writer.Flush();
                    }
                    Console.WriteLine($"Inserted: {startTime:yyyy-MM-dd HH:mm}");
                    startTime = nextTime;
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    string message = ex.InnerException == null ? ex.Message : ex.InnerException.Message;
                    Logger.WriteLine(message, ConsoleColor.Red);
                    if (message.Contains("TooManyRequests") || message.Contains("A task was canceled."))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    return false;
                }
            }
            Logger.WriteLine($"Completed: {filename}", ConsoleColor.Green);
            Logger.WriteLine();
            return true;
        }
    }
}
