using System.Collections.Generic;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Optimization;

namespace Wiki
{
    public sealed class ScriptCross2MA : IExternalScript
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

        public IntOptimProperty SlowEMA_Period = new IntOptimProperty(75, false, 10, 100, 5);

        public IntOptimProperty FastEMA_Period = new IntOptimProperty(45, false, 10, 100, 5);

        public OptimProperty TrailStop_StopLoss = new OptimProperty(1.5D, false, 0.1D, 5D, 0.1D, 1);

        public OptimProperty TrailStop_TrailEnable = new OptimProperty(0.5D, false, 0.1D, 3D, 0.1D, 1);

        public OptimProperty TrailStop_TrailLoss = new OptimProperty(0.5D, false, 0.1D, 3D, 0.1D, 1);

        public BoolOptimProperty OpenOrderMarket_Long = new BoolOptimProperty(true, false);

        #endregion

        public void Execute(IContext context, ISecurity Symbol)
        {
            #region Graph & Canvas Panes
            // =================================================
            // Graph & Canvas Panes
            // =================================================
            // Make 'MainChart' pane
            IGraphPane MainChart_pane = context.CreateGraphPane("MainChart", null);
            MainChart_pane.Visible = true;
            MainChart_pane.HideLegend = false;

            // Initialize 'Close' item
            Close_h.Context = context;
            // Make 'Close' item data
            IList<double> Close = context.GetData("Close", new string[] {
                "Symbol"
            }, () => Close_h.Execute(Symbol));

            // Initialize 'SlowEMA' item
            SlowEMA_h.Context = context;
            SlowEMA_h.Period = ((int)(SlowEMA_Period.Value));
            // Make 'SlowEMA' item data
            IList<double> SlowEMA = context.GetData("SlowEMA", new string[] {
                SlowEMA_h.Period.ToString(),
                "Symbol"
            }, () => SlowEMA_h.Execute(Close));

            // Initialize 'FastEMA' item
            FastEMA_h.Context = context;
            FastEMA_h.Period = ((int)(FastEMA_Period.Value));
            // Make 'FastEMA' item data
            IList<double> FastEMA = context.GetData("FastEMA", new string[] {
                FastEMA_h.Period.ToString(),
                "Symbol"
            }, () => FastEMA_h.Execute(Close));

            // Initialize 'CrossUnder' item
            CrossUnder_h.Context = context;
            // Make 'CrossUnder' item data
            IList<bool> CrossUnder = context.GetData("CrossUnder", new string[] {
                SlowEMA_h.Period.ToString(),
                FastEMA_h.Period.ToString(),
                "Symbol"
            }, () => CrossUnder_h.Execute(SlowEMA, FastEMA));

            IPosition OpenOrderMarket;

            // Initialize 'TrailStop' item
            TrailStop_h.StopLoss = ((double)(TrailStop_StopLoss.Value));
            TrailStop_h.TrailEnable = ((double)(TrailStop_TrailEnable.Value));
            TrailStop_h.TrailLoss = ((double)(TrailStop_TrailLoss.Value));
            TrailStop_h.UseCalcPrice = false;
            double TrailStop = 0;

            #endregion

            #region Handlers
            // =================================================
            // Handlers
            // =================================================

            // Initialize 'AbsComission' item
            AbsComission_h.Commission = 0.0002D;
            // Make 'AbsComission' item data
            AbsComission_h.Execute(Symbol);

            #endregion

            #region Trading
            // =================================================
            // Trading
            // =================================================
            int barsCount = Symbol.Bars.Count;

            if ((context.IsLastBarUsed == false))
            {
                barsCount--;
            }

            for (int i = 0; (i < barsCount); i++)
            {
                OpenOrderMarket = Symbol.Positions.GetLastActiveForSignal("OpenOrderMarket", i);
                TrailStop = TrailStop_h.Execute(OpenOrderMarket, i);
                if ((OpenOrderMarket == null))
                {
                    if (CrossUnder[i])
                    {
                        if ((context.TradeFromBar <= i))
                        {
                            Symbol.Positions.OpenAtMarket(((bool)(OpenOrderMarket_Long.Value)), i + 1, 1D, "OpenOrderMarket");
                        }
                    }
                }
                else
                {
                    if ((OpenOrderMarket.EntryBarNum <= i))
                    {
                        OpenOrderMarket.CloseAtStop(i + 1, TrailStop, "CloseOrderSL");
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
            IGraphList MainChart_pane_Symbol_chart = MainChart_pane.AddList("MainChart_pane_Symbol_chart", ("Symbol"
                            + (" ["
                            + (Symbol.Symbol + "]"))), Symbol, CandleStyles.BAR_CANDLE, CandleFillStyle.All, true, -16722859, PaneSides.RIGHT);
            Symbol.ConnectSecurityList(MainChart_pane_Symbol_chart);
            MainChart_pane_Symbol_chart.AlternativeColor = -54485;
            MainChart_pane_Symbol_chart.Autoscaling = true;
            MainChart_pane.UpdatePrecision(PaneSides.RIGHT, Symbol.Decimals);

            // Make 'SlowEMA' chart
            IGraphList MainChart_pane_SlowEMA_chart = MainChart_pane.AddList("MainChart_pane_SlowEMA_chart", ((("SlowEMA"
                            + (" (" + SlowEMA_h.Period))
                            + ")")
                            + (" ["
                            + (Symbol.Symbol + "]"))), SlowEMA, ListStyles.LINE, -6815694, LineStyles.SOLID, PaneSides.RIGHT);
            MainChart_pane_SlowEMA_chart.AlternativeColor = -16777216;
            MainChart_pane_SlowEMA_chart.Autoscaling = true;
            MainChart_pane.UpdatePrecision(PaneSides.RIGHT, Symbol.Decimals);

            // Make 'FastEMA' chart
            IGraphList MainChart_pane_FastEMA_chart = MainChart_pane.AddList("MainChart_pane_FastEMA_chart", ((("FastEMA"
                            + (" (" + FastEMA_h.Period))
                            + ")")
                            + (" ["
                            + (Symbol.Symbol + "]"))), FastEMA, ListStyles.LINE, -16737219, LineStyles.DASH, PaneSides.RIGHT);
            MainChart_pane_FastEMA_chart.AlternativeColor = -16777216;
            MainChart_pane_FastEMA_chart.Autoscaling = true;
            MainChart_pane.UpdatePrecision(PaneSides.RIGHT, Symbol.Decimals);

            #endregion
        }
    }
}
