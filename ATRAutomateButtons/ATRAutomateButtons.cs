using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System;
namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class AtrRiskBot : Robot
    {
        // Parameters for scale factor and percentage of account to risk
        [Parameter("Scale Factor", DefaultValue = 2.0)]
        public double ScaleFactor { get; set; }

        [Parameter("Risk Percent", DefaultValue = 1.0)]
        public double RiskPercent { get; set; }

        private StackPanel controlPanel;
        private Button buyButton;
        private Button sellButton;

        private AverageTrueRange atrM15;
        private AverageTrueRange atrH1;

        protected override void OnStart()
        {
            // Initialize ATR indicators for M15 and H1
            atrM15 = Indicators.AverageTrueRange(MarketData.GetSeries(TimeFrame.Minute15), 14, MovingAverageType.Simple);
            atrH1 = Indicators.AverageTrueRange(MarketData.GetSeries(TimeFrame.Hour), 14, MovingAverageType.Simple);

            // Create control panel
            controlPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = 10
            };

            // Create Buy button with ATR values
            buyButton = new Button
            {
                Text = $"Buy (ATR M15: {atrM15.Result.LastValue / Symbol.PipSize:F1} pips, H1: {atrH1.Result.LastValue / Symbol.PipSize:F1} pips)",
                BackgroundColor = Color.Green,
                Margin = 5
            };
            buyButton.Click += BuyButton_Click;

            // Create Sell button with ATR values
            sellButton = new Button
            {
                Text = $"Sell (ATR M15: {atrM15.Result.LastValue / Symbol.PipSize:F1} pips, H1: {atrH1.Result.LastValue / Symbol.PipSize:F1} pips)",
                BackgroundColor = Color.Red,
                Margin = 5
            };
            sellButton.Click += SellButton_Click;

            // Add buttons to the control panel
            controlPanel.AddChild(buyButton);
            controlPanel.AddChild(sellButton);

            // Add the control panel to the chart
            Chart.AddControl(controlPanel);
        }

        // Buy button click handler
        private void BuyButton_Click(ButtonClickEventArgs obj)
        {
            ExecuteMarketOrder(TradeType.Buy);
        }

        // Sell button click handler
        private void SellButton_Click(ButtonClickEventArgs obj)
        {
            ExecuteMarketOrder(TradeType.Sell);
        }
        
private void ExecuteMarketOrder(TradeType tradeType)
{
    // Get the latest ATR values
    double atrValueM15 = atrM15.Result.LastValue;
    double atrValueH1 = atrH1.Result.LastValue;

    // Calculate the stop loss in pips (ATR * ScaleFactor) using M15 ATR
    double stopLossPips = (atrValueM15 / Symbol.PipSize) * ScaleFactor;

    // Round stop loss pips to one decimal place
    stopLossPips = Math.Round(stopLossPips, 1);

    // Calculate account balance risk
    double accountBalance = Account.Balance;
    double riskAmount = accountBalance * (RiskPercent / 100);

    // Calculate pip value (depends on symbol)
    double pipValue = Symbol.PipValue;

    // Correct volume calculation
    double volume = riskAmount / (stopLossPips * pipValue);

    // Ensure volume is within minimum/maximum bounds
    volume = Symbol.NormalizeVolumeInUnits(volume);

    // Calculate stop loss price
    double stopLossPrice = tradeType == TradeType.Buy
        ? Symbol.Bid - stopLossPips * Symbol.PipSize
        : Symbol.Ask + stopLossPips * Symbol.PipSize;

    // Calculate take profit in pips and round it to one decimal place
    double takeProfitPips = Math.Round(stopLossPips * 2, 1); // Example take profit 2:1 risk-reward

    // Calculate take profit price
    double takeProfitPrice = tradeType == TradeType.Buy
        ? Symbol.Bid + takeProfitPips * Symbol.PipSize
        : Symbol.Ask - takeProfitPips * Symbol.PipSize;

    // Debug print statements
    Print("ATR M15 (in pips): {0}", Math.Round(atrValueM15 / Symbol.PipSize, 1));
    Print("ATR H1 (in pips): {0}", Math.Round(atrValueH1 / Symbol.PipSize, 1));
    Print("Stop Loss Pips: {0}", stopLossPips);
    Print("Pip Value: {0}", pipValue);
    Print("Lot Size: {0}", Symbol.LotSize);
    Print("Risk Amount: {0}", riskAmount);
    Print("Volume: {0}", volume);
    Print("Stop Loss Price: {0}", stopLossPrice);
    Print("Take Profit Price: {0}", takeProfitPrice);

    // Execute the market order
    ExecuteMarketOrder(tradeType, SymbolName, volume, "ATR Risk Management", stopLossPips, takeProfitPips);
}







        protected override void OnTick()
        {
            // Update the ATR values on the buttons
            buyButton.Text = $"Buy (ATR M15: {atrM15.Result.LastValue / Symbol.PipSize:F1} pips, H1: {atrH1.Result.LastValue / Symbol.PipSize:F1} pips)";
            sellButton.Text = $"Sell (ATR M15: {atrM15.Result.LastValue / Symbol.PipSize:F1} pips, H1: {atrH1.Result.LastValue / Symbol.PipSize:F1} pips)";
        }

        protected override void OnStop()
        {
            // Cleanup logic when the bot is stopped
            if (controlPanel != null)
            {
                Chart.RemoveControl(controlPanel);
            }
        }
    }
}
