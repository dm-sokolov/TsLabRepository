using System.Collections.Generic;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Optimization;

namespace Wiki
{
    public sealed class ScriptCross2Ma : IExternalScript
    {

        #region New Objects

        private Close Close_h = new Close();

        private EMA SlowEMA_h = new EMA();

        private EMA FastEMA_h = new EMA();

        private CrossUnder CrossUnder_h = new CrossUnder();

        private TrailStop TrailStop_h = new TrailStop();

        private AbsolutCommission AbsComission_h = new AbsolutCommission();

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
            Close_h.Context = context;
            // Make 'Close' item data
            var close = context.GetData("Close", new string[] {
                "Symbol"
            }, () => Close_h.Execute(symbol));

            // Initialize 'SlowEMA' item
            SlowEMA_h.Context = context;
            SlowEMA_h.Period = ((int)(SlowEmaPeriod.Value));
            // Make 'SlowEMA' item data
            var slowEma = context.GetData("SlowEMA", new string[] {
                SlowEMA_h.Period.ToString(),
                "Symbol"
            }, () => SlowEMA_h.Execute(close));

            // Initialize 'FastEMA' item
            FastEMA_h.Context = context;
            FastEMA_h.Period = ((int)(FastEmaPeriod.Value));
            // Make 'FastEMA' item data
            var fastEma = context.GetData("FastEMA", new string[] {
                FastEMA_h.Period.ToString(),
                "Symbol"
            }, () => FastEMA_h.Execute(close));

            // Initialize 'CrossUnder' item
            CrossUnder_h.Context = context;
            // Make 'CrossUnder' item data
            var crossUnder = context.GetData("CrossUnder", new string[] {
                SlowEMA_h.Period.ToString(),
                FastEMA_h.Period.ToString(),
                "Symbol"
            }, () => CrossUnder_h.Execute(slowEma, fastEma));

            IPosition openOrderMarket;

            // Initialize 'TrailStop' item
            TrailStop_h.StopLoss = ((double)(TrailStopStopLoss.Value));
            TrailStop_h.TrailEnable = ((double)(TrailStopTrailEnable.Value));
            TrailStop_h.TrailLoss = ((double)(TrailStopTrailLoss.Value));
            TrailStop_h.UseCalcPrice = false;
            double trailStop = 0;

            #endregion

            #region Handlers
            // =================================================
            // Handlers
            // =================================================

            // Initialize 'AbsComission' item
            AbsComission_h.Commission = 0.0002D;
            // Make 'AbsComission' item data
            AbsComission_h.Execute(symbol);

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
                trailStop = TrailStop_h.Execute(openOrderMarket, i);
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
                            + (" (" + SlowEMA_h.Period))
                            + ")")
                            + (" ["
                            + (symbol.Symbol + "]"))), slowEma, ListStyles.LINE, -6815694, LineStyles.SOLID, PaneSides.RIGHT);
            mainChartPaneSlowEmaChart.AlternativeColor = -16777216;
            mainChartPaneSlowEmaChart.Autoscaling = true;
            mainChartPane.UpdatePrecision(PaneSides.RIGHT, symbol.Decimals);

            // Make 'FastEMA' chart
            var mainChartPaneFastEmaChart = mainChartPane.AddList("MainChart_pane_FastEMA_chart", ((("FastEMA"
                            + (" (" + FastEMA_h.Period))
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
