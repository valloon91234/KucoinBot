using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valloon.Kucoin
{
    /*
     * https://github.com/DaveSkender/Stock.Indicators/blob/main/src/_common/Candles/Candles.cs
     */
    [Serializable]
    public class CandleQuote
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }

}
