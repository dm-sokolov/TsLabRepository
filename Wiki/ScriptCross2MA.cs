
using System.Collections.Generic;
using TSLab.Script;
using TSLab.Script.Handlers;
using TSLab.Script.Optimization;

namespace Wiki
{
    public sealed class ScriptCross2MA : IExternalScript
    {

        private Close Close_h = new Close();

        private EMA SlowEMA_h = new EMA();

        private EMA FastEMA_h = new EMA();

        private CrossUnder CrossUnder_h = new CrossUnder();

        private TrailStop TrailStop_h = new TrailStop();

        private AbsolutCommission AbsComission_h = new AbsolutCommission();

        public IntOptimProperty SlowEMA_Period = new IntOptimProperty(20, false, 10, 100, 5);

        public IntOptimProperty FastEMA_Period = new IntOptimProperty(20, false, 10, 100, 5);

        public OptimProperty TrailStop_StopLoss = new OptimProperty(1.5D, false, 0.1D, 5D, 0.1D, 1);

        public OptimProperty TrailStop_TrailEnable = new OptimProperty(0.5D, false, 0.1D, 3D, 0.1D, 1);

        public OptimProperty TrailStop_TrailLoss = new OptimProperty(0.5D, false, 0.1D, 3D, 0.1D, 1);

        public BoolOptimProperty OpenOrderMarket_Long = new BoolOptimProperty(true, false);

        public ScriptCross2MA()
        {
        }

        public void Execute(IContext context, ISecurity Symbol)
        {
            // =================================================
            // Graph & Canvas Panes
            // =================================================
            // Make 'MainChart' pane
            IGraphPane MainChart_pane = context.CreateGraphPane("MainChart", null);
            MainChart_pane.Visible = true;
            MainChart_pane.HideLegend = false;
            // Initialize 'Close' item
            this.Close_h.Context = context;
            // Make 'Close' item data
            IList<double> Close = context.GetData("Close", new string[] {
                "Symbol"
            }, delegate {
                return this.Close_h.Execute(Symbol);

            });
            // Initialize 'SlowEMA' item
            this.SlowEMA_h.Context = context;
            this.SlowEMA_h.Period = ((int)(this.SlowEMA_Period.Value));
            // Make 'SlowEMA' item data
            IList<double> SlowEMA = context.GetData("SlowEMA", new string[] {
                this.SlowEMA_h.Period.ToString(),
                "Symbol"
            }, delegate {
                return this.SlowEMA_h.Execute(Close);

            });
            // Initialize 'FastEMA' item
            this.FastEMA_h.Context = context;
            this.FastEMA_h.Period = ((int)(this.FastEMA_Period.Value));
            // Make 'FastEMA' item data
            IList<double> FastEMA = context.GetData("FastEMA", new string[] {
                this.FastEMA_h.Period.ToString(),
                "Symbol"
            }, delegate {
                return this.FastEMA_h.Execute(Close);

            });
            // Initialize 'CrossUnder' item
            this.CrossUnder_h.Context = context;
            // Make 'CrossUnder' item data
            IList<bool> CrossUnder = context.GetData("CrossUnder", new string[] {
                this.SlowEMA_h.Period.ToString(),
                this.FastEMA_h.Period.ToString(),
                "Symbol"
            }, delegate {
                return this.CrossUnder_h.Execute(SlowEMA, FastEMA);

            });
            IPosition OpenOrderMarket;
            // Initialize 'TrailStop' item
            this.TrailStop_h.StopLoss = ((double)(this.TrailStop_StopLoss.Value));
            this.TrailStop_h.TrailEnable = ((double)(this.TrailStop_TrailEnable.Value));
            this.TrailStop_h.TrailLoss = ((double)(this.TrailStop_TrailLoss.Value));
            this.TrailStop_h.UseCalcPrice = false;
            double TrailStop = 0;
            // =================================================
            // Handlers
            // =================================================
            // Initialize 'AbsComission' item
            this.AbsComission_h.Commission = 0.0002D;
            // Make 'AbsComission' item data
            this.AbsComission_h.Execute(Symbol);
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
                TrailStop = this.TrailStop_h.Execute(OpenOrderMarket, i);
                if ((OpenOrderMarket == null))
                {
                    if (CrossUnder[i])
                    {
                        if ((context.TradeFromBar <= i))
                        {
                            Symbol.Positions.OpenAtMarket(((bool)(this.OpenOrderMarket_Long.Value)), i + 1, 1D, "OpenOrderMarket");
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
                            + (" (" + this.SlowEMA_h.Period))
                            + ")")
                            + (" ["
                            + (Symbol.Symbol + "]"))), SlowEMA, ListStyles.LINE, -6815694, LineStyles.SOLID, PaneSides.RIGHT);
            MainChart_pane_SlowEMA_chart.AlternativeColor = -16777216;
            MainChart_pane_SlowEMA_chart.Autoscaling = true;
            MainChart_pane.UpdatePrecision(PaneSides.RIGHT, Symbol.Decimals);
            // Make 'FastEMA' chart
            IGraphList MainChart_pane_FastEMA_chart = MainChart_pane.AddList("MainChart_pane_FastEMA_chart", ((("FastEMA"
                            + (" (" + this.FastEMA_h.Period))
                            + ")")
                            + (" ["
                            + (Symbol.Symbol + "]"))), FastEMA, ListStyles.LINE, -16737219, LineStyles.DASH, PaneSides.RIGHT);
            MainChart_pane_FastEMA_chart.AlternativeColor = -16777216;
            MainChart_pane_FastEMA_chart.Autoscaling = true;
            MainChart_pane.UpdatePrecision(PaneSides.RIGHT, Symbol.Decimals);
        }

        public void Dispose()
        {
        }
    }
}
