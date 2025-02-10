using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class NewsEventPendingOrders : Robot
    {
        // Parameters for the news event
        [Parameter("News Event Time (UTC)", DefaultValue = "2023-10-30 12:00:00")]
        public string NewsEventTime { get; set; }

        [Parameter("Buy Stop Distance (Pips)", DefaultValue = 20)]
        public int BuyStopDistance { get; set; }

        [Parameter("Sell Stop Distance (Pips)", DefaultValue = 20)]
        public int SellStopDistance { get; set; }

        [Parameter("Lot Size", DefaultValue = 0.1)]
        public double LotSize { get; set; }

        [Parameter("Take Profit (Pips)", DefaultValue = 40)]
        public int TakeProfitPips { get; set; }

        [Parameter("Stop Loss (Pips)", DefaultValue = 20)]
        public int StopLossPips { get; set; }

        private DateTime _newsEventDateTime;

        protected override void OnStart()
        {
            // Convert the news event time string to a DateTime object
            _newsEventDateTime = DateTime.Parse(NewsEventTime);

            // Check if the news event is in the future
            if (_newsEventDateTime <= Server.Time)
            {
                Print("News event time must be in the future. Please adjust the parameter.");
                Stop();
                return;
            }

            Print("cBot started. Waiting for news event time...");
        }

        protected override void OnBar()
        {
            // Check if it's time to place pending orders
            if (Server.Time >= _newsEventDateTime.AddMinutes(-15) && Server.Time < _newsEventDateTime)
            {
                PlacePendingOrders();
                Stop(); // Stop the cBot after placing orders
            }
        }

        private void PlacePendingOrders()
        {
            // Calculate Buy Stop and Sell Stop levels
            double buyStopPrice = Symbol.Bid + BuyStopDistance * Symbol.PipSize;
            double sellStopPrice = Symbol.Ask - SellStopDistance * Symbol.PipSize;

            // Place Buy Stop order
            PlaceStopOrder(TradeType.Buy, Symbol.Name, LotSize, buyStopPrice, "BuyStopNews", StopLossPips, TakeProfitPips);

            // Place Sell Stop order
            PlaceStopOrder(TradeType.Sell, Symbol.Name, LotSize, sellStopPrice, "SellStopNews", StopLossPips, TakeProfitPips);

            Print("Pending orders placed at Buy Stop: {0}, Sell Stop: {1}", buyStopPrice, sellStopPrice);
        }
    }
}
