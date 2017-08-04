// 
// Copyright (C) 2015, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SamplePnL : Strategy
	{
		private int priorTradesCount = 0;
		private double priorTradesCumProfit = 0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description						= @"Using trade performance statistics for money management";
				Name							= "Sample PnL";
				Calculate						= Calculate.OnBarClose;
				EntriesPerDirection				= 1;
				EntryHandling					= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy 	= true;
          		ExitOnSessionCloseSeconds    	= 30;
				IsFillLimitOnTouch				= false;
				MaximumBarsLookBack				= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution				= OrderFillResolution.Standard;
				Slippage						= 0;
				StartBehavior					= StartBehavior.WaitUntilFlat;
				TimeInForce						= TimeInForce.Gtc;
				TraceOrders						= false;
				RealtimeErrorHandling			= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling				= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade				= 20;
			}
			if (State == State.Configure)
			{
				// Profit target is 10 ticks above entry price
				SetProfitTarget(CalculationMode.Ticks, 10);

				// Stop loss is 4 ticks below entry price
				SetStopLoss(CalculationMode.Ticks, 4);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade) return;
			// At the start of a new session
			if (Bars.IsFirstBarOfSession)
			{
				// Store the strategy's prior cumulated realized profit and number of trades
				priorTradesCount = SystemPerformance.AllTrades.Count;
				priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;

				/* NOTE: Using .AllTrades will include both historical virtual trades as well as real-time trades.
				If you want to only count profits from real-time trades please use .RealtimeTrades. */
			}

			/* Prevents further trading if the current session's realized profit exceeds $1000 or if realized losses exceed $400.
			Also prevent trading if 10 trades have already been made in this session. */
			if (SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit >= 1000
				|| SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit <= -400
				|| SystemPerformance.AllTrades.Count - priorTradesCount > 10)
			{
				/* TIP FOR EXPERIENCED CODERS: This only prevents trade logic in the context of the OnBarUpdate() method. If you are utilizing
				other methods like OnOrderUpdate() or OnMarketData() you will need to insert this code segment there as well. */

				// Returns out of the OnBarUpdate() method. This prevents any further evaluation of trade logic in the OnBarUpdate() method.
				return;
			}

			// ENTRY CONDITION: If current close is greater than previous close, enter long
			if (Close[0] > Close[1])
			{
				EnterLong();
			}
		}
	}
}