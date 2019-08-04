using System.Collections.Generic;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Optimization;

namespace Wiki
{
    public sealed class ScriptCross2Ma : IExternalScript
    {

        #region New Objects

        private Close Close = new Close();

        private EMA SlowEMA = new EMA();

        private EMA FastEMA = new EMA();

        private CrossUnder CrossUnder = new CrossUnder();

        private TrailStop TrailStop = new TrailStop();

        private AbsolutCommission AbsComission = new AbsolutCommission();

        #endregion

        #region OptimProperty

        public IntOptimProperty SlowEmaPeriod = new IntOptimProperty(75, false, 10, 100, 5);

        public IntOptimProperty FastEmaPeriod = new IntOptimProperty(45, false, 10, 100, 5);

        public OptimProperty TrailStopStopLoss = new OptimProperty(1.5D, false, 0.1D, 5D, 0.1D, 1);

        public OptimProperty TrailStopTrailEnable = new OptimProperty(0.5D, false, 0.1D, 3D, 0.1D, 1);

        public OptimProperty TrailStopTrailLoss = new OptimProperty(0.5D, false, 0.1D, 3D, 0.1D, 1);

        public BoolOptimProperty OpenOrderMarketLong = new BoolOptimProperty(true, false);

        #endregion

        public void Execute(IContext context, ISecurity symbol)
        {
            #region Graph & Canvas Panes
            // =================================================
            // Graph & Canvas Panes
            // =================================================
            // Make 'MainChart' pane
            var mainChartPane = context.CreateGraphPane("MainChart", null);
            mainChartPane.Visible = true;
            mainChartPane.HideLegend = false;

            // Initialize 'Close' item
            Close.Context = context;
            // Make 'Close' item data
            var close = context.GetData("Close", new string[] {
                "Symbol"
            }, () => Close.Execute(symbol));

            // Initialize 'SlowEMA' item
            SlowEMA.Context = context;
            SlowEMA.Period = ((int)(SlowEmaPeriod.Value));
            // Make 'SlowEMA' item data
            var slowEma = context.GetData("SlowEMA", new string[] {
                SlowEMA.Period.ToString(),
                "Symbol"
            }, () => SlowEMA.Execute(close));

            // Initialize 'FastEMA' item
            FastEMA.Context = context;
            FastEMA.Period = ((int)(FastEmaPeriod.Value));
            // Make 'FastEMA' item data
            var fastEma = context.GetData("FastEMA", new string[] {
                FastEMA.Period.ToString(),
                "Symbol"
            }, () => FastEMA.Execute(close));

            // Initialize 'CrossUnder' item
            CrossUnder.Context = context;
            // Make 'CrossUnder' item data
            var crossUnder = context.GetData("CrossUnder", new string[] {
                SlowEMA.Period.ToString(),
                FastEMA.Period.ToString(),
                "Symbol"
            }, () => CrossUnder.Execute(slowEma, fastEma));

            IPosition openOrderMarket;

            // Initialize 'TrailStop' item
            TrailStop.StopLoss = ((double)(TrailStopStopLoss.Value));
            TrailStop.TrailEnable = ((double)(TrailStopTrailEnable.Value));
            TrailStop.TrailLoss = ((double)(TrailStopTrailLoss.Value));
            TrailStop.UseCalcPrice = false;
            double trailStop = 0;

            #endregion

            #region Handlers
            // =================================================
            // Handlers
            // =================================================

            // Initialize 'AbsComission' item
            AbsComission.Commission = 0.0002D;
            // Make 'AbsComission' item data
            AbsComission.Execute(symbol);

            #endregion

            #region Trading
            // =================================================
            // Trading
            // =================================================
            var barsCount = symbol.Bars.Count;

            if ((context.IsLastBarUsed == false))
            {
                barsCount--;
            }

            for (var i = 0; (i < barsCount); i++)
            {
                openOrderMarket = symbol.Positions.GetLastActiveForSignal("OpenOrderMarket", i);
                trailStop = TrailStop.Execute(openOrderMarket, i);
                if ((openOrderMarket == null))
                {
                    if (crossUnder[i])
                    {
                        if ((context.TradeFromBar <= i))
                        {
                            symbol.Positions.OpenAtMarket(((bool)(OpenOrderMarketLong.Value)), i + 1, 1D, "OpenOrderMarket");
                        }
                    }
                }
                else
                {
                    if ((openOrderMarket.EntryBarNum <= i))
                    {
                        openOrderMarket.CloseAtStop(i + 1, trailStop, "CloseOrderSL");
                    }
                }
            }

            if (context.IsOptimization)
            {
                return;
            }

            #endregion

            #region Charts
            // =================================================
            // Charts
            // =================================================

            // Make 'Symbol' chart
            var mainChartPaneSymbolChart = mainChartPane.AddList("MainChart_pane_Symbol_chart", ("Symbol"
                            + (" ["
                            + (symbol.Symbol + "]"))), symbol, CandleStyles.BAR_CANDLE, CandleFillStyle.All, true, -16722859, PaneSides.RIGHT);
            symbol.ConnectSecurityList(mainChartPaneSymbolChart);
            mainChartPaneSymbolChart.AlternativeColor = -54485;
            mainChartPaneSymbolChart.Autoscaling = true;
            mainChartPane.UpdatePrecision(PaneSides.RIGHT, symbol.Decimals);

            // Make 'SlowEMA' chart
            var mainChartPaneSlowEmaChart = mainChartPane.AddList("MainChart_pane_SlowEMA_chart", ((("SlowEMA"
                            + (" (" + SlowEMA.Period))
                            + ")")
                            + (" ["
                            + (symbol.Symbol + "]"))), slowEma, ListStyles.LINE, -6815694, LineStyles.SOLID, PaneSides.RIGHT);
            mainChartPaneSlowEmaChart.AlternativeColor = -16777216;
            mainChartPaneSlowEmaChart.Autoscaling = true;
            mainChartPane.UpdatePrecision(PaneSides.RIGHT, symbol.Decimals);

            // Make 'FastEMA' chart
            var mainChartPaneFastEmaChart = mainChartPane.AddList("MainChart_pane_FastEMA_chart", ((("FastEMA"
                            + (" (" + FastEMA.Period))
                            + ")")
                            + (" ["
                            + (symbol.Symbol + "]"))), fastEma, ListStyles.LINE, -16737219, LineStyles.DASH, PaneSides.RIGHT);
            mainChartPaneFastEmaChart.AlternativeColor = -16777216;
            mainChartPaneFastEmaChart.Autoscaling = true;
            mainChartPane.UpdatePrecision(PaneSides.RIGHT, symbol.Decimals);

            #endregion
        }
    }
}
