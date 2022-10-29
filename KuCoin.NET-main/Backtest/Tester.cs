using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using static System.Formats.Asn1.AsnWriter;
using System.Threading;

namespace Valloon.Kucoin
{
    static class Tester
    {
        public static (decimal firstCandleX, decimal entryPrice, decimal, int, DateTime?, decimal volume, decimal volumeX) Test(string symbol, decimal maxFirstCandleX1, decimal startX1, decimal closeX1, decimal stopX1, int timeout1, decimal closeXX1, int timeoutStop1, decimal maxFirstCandleX2, decimal startX2, decimal closeX2, decimal stopX2, int timeout2, decimal closeXX2, int timeoutStop2, decimal volume1, decimal volume2)
        {
            //var list = Dao.SelectAll(symbol);
            var lines = File.ReadAllLines($"data-{symbol}.csv");
            var lineCount = lines.Length;
            List<CandleQuote> list = new();
            for (int i = 1; i < lineCount; i++)
            {
                try
                {
                    var paramArray = lines[i].Split(",");
                    var m = new CandleQuote
                    {
                        Timestamp = DateTime.ParseExact(paramArray[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture),
                        Open = decimal.Parse(paramArray[1]),
                        High = decimal.Parse(paramArray[2]),
                        Low = decimal.Parse(paramArray[3]),
                        Close = decimal.Parse(paramArray[4]),
                        Volume = decimal.Parse(paramArray[5]),
                    };
                    list.Add(m);
                }
                catch { }
            }
            var firstCandleX = list[0].Close / list[0].Open;
            var firstVolume = list[0].Volume;
            var firstVolumeX = list[1].Volume / list[0].Volume;
            if (firstVolume < volume1)
            {
                //return (firstCandleX, 0, 0, 0, null, 0);
                throw new Exception($"Too low first volume: {firstVolume}");
            }
            if (firstVolume < volume2)
            {
                if (firstCandleX > maxFirstCandleX1)
                    throw new Exception($"Too high firstCandleX: {firstCandleX}");
                //return (firstCandleX, 0, 0, 0, null, 0);
                int count = list.Count;
                var entryPrice = list[0].Close * (1 + startX1);
                var closePrice = entryPrice * (1 + closeX1);
                var stopPrice = entryPrice * (1 - stopX1);
                var closePrice2 = entryPrice * (1 + closeXX1);
                for (int i = 1; i < count - 1; i++)
                {
                    if (list[i].Low <= stopPrice)
                        return (firstCandleX, entryPrice, stopPrice, i, list[i].Timestamp, firstVolume, firstVolumeX);
                    if (i <= timeout1 && list[i].High >= closePrice)
                        return (firstCandleX, entryPrice, closePrice, i, list[i].Timestamp, firstVolume, firstVolumeX);
                    if (i > timeout1 && i <= timeoutStop1 && list[i].High >= closePrice2)
                        return (firstCandleX, entryPrice, closePrice2, i, list[i].Timestamp, firstVolume, firstVolumeX);
                    if (i > timeoutStop1)
                        return (firstCandleX, entryPrice, list[i].Open, i, list[i].Timestamp, firstVolume, firstVolumeX);
                }
                return (firstCandleX, entryPrice, list.Last().Open, count - 1, list.Last().Timestamp, firstVolume, firstVolumeX);
            }
            else
            {
                if (firstCandleX > maxFirstCandleX2)
                    throw new Exception($"Too low firstCandleX: {firstCandleX}");
                int count = list.Count;
                var entryPrice = list[0].Close * (1 + startX2);
                var closePrice = entryPrice * (1 + closeX2);
                var stopPrice = entryPrice * (1 - stopX2);
                var closePrice2 = entryPrice * (1 + closeXX2);
                for (int i = 1; i < count - 1; i++)
                {
                    if (list[i].Low <= stopPrice)
                        return (firstCandleX, entryPrice, stopPrice, i, list[i].Timestamp, firstVolume, firstVolumeX);
                    if (i <= timeout2 && list[i].High >= closePrice)
                        return (firstCandleX, entryPrice, closePrice, i, list[i].Timestamp, firstVolume, firstVolumeX);
                    if (i > timeout2 && i <= timeoutStop2 && list[i].High >= closePrice2)
                        return (firstCandleX, entryPrice, closePrice2, i, list[i].Timestamp, firstVolume, firstVolumeX);
                    if (i > timeoutStop2)
                        return (firstCandleX, entryPrice, list[i].Open, i, list[i].Timestamp, firstVolume, firstVolumeX);
                }
                return (firstCandleX, entryPrice, list.Last().Open, count - 1, list.Last().Timestamp, firstVolume, firstVolumeX);
            }
        }

    }

}
