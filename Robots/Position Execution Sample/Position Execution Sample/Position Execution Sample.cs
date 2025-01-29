// -------------------------------------------------------------------------------------------------
//    Enhanced cTrader Algo with Stop-Loss, Take-Profit, and Trailing Stop-Loss
// -------------------------------------------------------------------------------------------------

using cAlgo.API;
using cAlgo.API.Internals;
using System;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EnhancedPositionExecutionWithNews : Robot
    {
        [Parameter("Volume (Lots)", DefaultValue = 0.01)]
        public double VolumeInLots { get; set; }

        [Parameter("Stop Loss (Pips)", DefaultValue = 20, MinValue = 0)]
        public double StopLossInPips { get; set; }

        [Parameter("Take Profit (Pips)", DefaultValue = 60, MinValue = 0)]
        public double TakeProfitInPips { get; set; }

        [Parameter("Trailing Stop (Pips)", DefaultValue = 30, MinValue = 0)]
        public double TrailingStopInPips { get; set; }

        protected override void OnStart()
        {
            Print("Trading Algorithm Started.");
            Timer.Start(60); // Check every minute
        }

        protected override void OnTimer()
        {
            var currentTime = Server.Time;

            if ((currentTime.Hour >= 10 && currentTime.Hour < 13) ||
                (currentTime.Hour >= 13 && currentTime.Hour < 16))
            {
                ExecuteTrades(currentTime);
            }
        }

        private void ExecuteTrades(DateTime currentTime)
        {
            var eurUsdSymbol = MarketData.GetSymbol("EURUSD");
            if (eurUsdSymbol == null)
            {
                Print("Error: EURUSD symbol data not available.");
                return;
            }

            double eurUsdRate = eurUsdSymbol.Bid;

            if (currentTime.Hour >= 10 && currentTime.Hour < 13) // London Session
            {
                if (IsUSDWeak() && IsGoodNewsForEUR())
                {
                    ExecuteMarketOrder(TradeType.Buy, eurUsdSymbol, "USD Weak & Good News", eurUsdRate);
                }
            }
            else if (currentTime.Hour >= 13 && currentTime.Hour < 16) // New York Session
            {
                if (IsUSDStrong() && IsBadNewsForEUR())
                {
                    ExecuteMarketOrder(TradeType.Sell, eurUsdSymbol, "USD Strong & Bad News", eurUsdRate);
                }
            }
        }

        private void ExecuteMarketOrder(TradeType tradeType, Symbol symbol, string comment, double currentPrice)
        {
            double stopLossPrice, takeProfitPrice;
            if (tradeType == TradeType.Buy)
            {
                stopLossPrice = currentPrice - (StopLossInPips * symbol.PipSize);
                takeProfitPrice = currentPrice + (TakeProfitInPips * symbol.PipSize);
            }
            else
            {
                stopLossPrice = currentPrice + (StopLossInPips * symbol.PipSize);
                takeProfitPrice = currentPrice - (TakeProfitInPips * symbol.PipSize);
            }

            var result = ExecuteMarketOrder(tradeType, symbol.Name, VolumeInLots, comment, stopLossPrice, takeProfitPrice);

            if (!result.IsSuccessful)
            {
                Print("Error executing trade: ", result.Error);
                return;
            }

            Print($"Trade executed: {tradeType} {symbol.Name} at {currentPrice}, SL: {stopLossPrice}, TP: {takeProfitPrice}");

            // Apply trailing stop-loss
            foreach (var position in Positions)
            {
                if (position.SymbolName == symbol.Name && position.TradeType == tradeType)
                {
                    ModifyTrailingStop(position);
                }
            }
        }

        private void ModifyTrailingStop(Position position)
        {
            double newStopLoss;
            if (position.TradeType == TradeType.Buy)
            {
                newStopLoss = position.EntryPrice + (TrailingStopInPips * Symbol.PipSize);
                if (newStopLoss > position.StopLoss)
                {
                    ModifyPosition(position, newStopLoss, position.TakeProfit);
                    Print($"Updated Trailing Stop for Buy Order: {newStopLoss}");
                }
            }
            else
            {
                newStopLoss = position.EntryPrice - (TrailingStopInPips * Symbol.PipSize);
                if (newStopLoss < position.StopLoss)
                {
                    ModifyPosition(position, newStopLoss, position.TakeProfit);
                    Print($"Updated Trailing Stop for Sell Order: {newStopLoss}");
                }
            }
        }

        private bool IsUSDWeak()
        {
            var symbol = MarketData.GetSymbol("EURUSD");
            return symbol != null && symbol.Bid > 1.20;
        }

        private bool IsUSDStrong()
        {
            var symbol = MarketData.GetSymbol("EURUSD");
            return symbol != null && symbol.Bid < 1.15;
        }

        private bool IsGoodNewsForEUR()
        {
            return true; // Replace with actual news data
        }

        private bool IsBadNewsForEUR()
        {
            return false; // Replace with actual news data
        }
    }
}
