using System;
namespace IEXTrading.Models
{
    public class StockStrategy
    {
        public string Symbol;
        public float AvgPrice;

        public StockStrategy(string symbol, float avgPrice)
        {
            Symbol = symbol;
            AvgPrice = avgPrice;
        }
    }
}
