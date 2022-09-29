namespace Valloon.Kucoin
{
    static class Tester
    {
        public static (decimal, decimal, int, DateTime) Test(string symbol, decimal startX, decimal closeX, int timeout, decimal closeX2, int timeoutStop)
        {
            var list = Dao.SelectAll(symbol);
            int count = list.Count;
            var entryPrice = list[0].Close * (1 + startX);
            for (int i = 1; i < count - 1; i++)
            {
                var closePrice = entryPrice * (1 + closeX);
                if (i <= timeout && list[i].High > closePrice)
                    return (entryPrice, closePrice, i, list[i].Timestamp);
                var closePrice2 = entryPrice * (1 + closeX2);
                if (i > timeout && i <= timeoutStop && list[i].High > closePrice2)
                    return (entryPrice, closePrice2, i, list[i].Timestamp);
                if (i > timeoutStop)
                    return (entryPrice, list[i].Open, i, list[i].Timestamp);
            }
            return (entryPrice, list.Last().Open, count - 1, list.Last().Timestamp);
        }

    }

}
