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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class ichimoku01 : Strategy
	{
		private IchimokuSignal IchimokuSignal1;
		private int 	shares				= 500;
		private int 	initialBalance 		= 50000;
		private	bool 	longDisabled		= false;
		private	bool 	shortDisabled		= false;
		private	int 	cashAvailiable 		= 0;
		private	double 	priorTradesCumProfit;
		private	int 	priorTradesCount;
		private	double 	sharesFraction;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Ichimoku 01";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= false;
				
				SyncWithCloud		= true;
				TenkanSen 			= false;
				KijunSen 			= false;
				CloudStop			= false;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				IchimokuSignal1				= IchimokuSignal(Close, 9, 26, 52, 26, false, false, false, false);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 26) {
				/// calc cash available to trade for 1st setup
				cashAvailiable = initialBalance;
				/// calc positionsize
				sharesFraction = cashAvailiable / Close[0];
				shares = (int)sharesFraction;
				return;
			}
			
			/*
			public Series<double> BaseLine
			public Series<double> SpanALine
			public Series<double> SpanBLine
			public Series<double> LagLine
			public Series<double> SpanALine_Kumo
			public Series<double> SpanBLine_Kumo
			*/
			
			/// add input for syncWithCloud
			/// add cloud as stop
			/// 
			/// must add stop re entry after 1st stpo in this directiom
			/// add exit @ ConversionLine == Tenkan-sen
			/// add exit @ BaseLine == Kijun-sen
			
			/// Normalise shares for 50K start balance
			if (Bars.IsFirstBarOfSession)
			{
				/// Store the strategy's prior cumulated realized profit and number of trades
				priorTradesCount = SystemPerformance.AllTrades.Count;
				priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				/// Adjust position size for profit and loss
				cashAvailiable = initialBalance + (int)priorTradesCumProfit;
				/// calc positionsize
				sharesFraction = cashAvailiable / Close[0];
				shares = (int)sharesFraction;
			}
			
			/// Red Cloud - Entries
			if ( IchimokuSignal1.SpanBLine_Kumo[0] > IchimokuSignal1.SpanALine_Kumo[0]) {
				if ( Close[0] > IchimokuSignal1.SpanBLine_Kumo[0] )
				{
					BarBrush = Brushes.LimeGreen;
					CandleOutlineBrush = Brushes.LimeGreen;
					if ( !SyncWithCloud && !longDisabled ) {
						EnterLong(Convert.ToInt32(shares), "");
						longDisabled = true; }
				}
				if ( Close[0] < IchimokuSignal1.SpanALine_Kumo[0] )
				{
					BarBrush = Brushes.Red;
					CandleOutlineBrush = Brushes.Red;
					EnterShort(Convert.ToInt32(shares), "");
					longDisabled = false;
				}
			} else {
				/// green cloud
				if ( Close[0] < IchimokuSignal1.SpanBLine_Kumo[0] )
				{
					BarBrush = Brushes.Red;
					CandleOutlineBrush = Brushes.Red;
					if ( !SyncWithCloud ) {
						EnterShort(Convert.ToInt32(shares), "");
						longDisabled = false; }
				}
				if ( Close[0] > IchimokuSignal1.SpanALine_Kumo[0] && !longDisabled )
				{
					BarBrush = Brushes.LimeGreen;
					CandleOutlineBrush = Brushes.LimeGreen;
					EnterLong(Convert.ToInt32(shares), "");
					longDisabled = true;
				}
			}
			
			///cloud as stop
			if ( CloudStop ) {
				/// long 
				if ( Position.MarketPosition == MarketPosition.Long) {
					/// close < green cloud
					if ( Close[0] < IchimokuSignal1.SpanALine_Kumo[0]) {
						//IchimokuSignal1.SpanBLine_Kumo[0] > IchimokuSignal1.SpanALine_Kumo[0] && 
					ExitLong(Convert.ToInt32(shares));
					}
				}
				
				/// short
				if ( Position.MarketPosition == MarketPosition.Short) {
					/// close > green cloud
					if ( Close[0] > IchimokuSignal1.SpanALine_Kumo[0]) {
						//IchimokuSignal1.SpanBLine_Kumo[0] > IchimokuSignal1.SpanALine_Kumo[0] && 
					ExitShort(Convert.ToInt32(shares));
					}
				}
			}
			/// Long Stops
//			if ( TenkanSen && Position.MarketPosition == MarketPosition.Long) {
//				if (Close[0] < IchimokuSignal1.ConversionLine[0] ) {
//					ExitLong(Convert.ToInt32(shares));
//					longDisabled = true;
//				}
//			}
			
		}
		
		#region Properies
		
		[NinjaScriptProperty]
		[Display(Name="Enter With Cloud", Order=1, GroupName="Parameters")]
		public bool SyncWithCloud
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Exit with Cloud", Order=2, GroupName="Parameters")]
		public bool CloudStop

		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Tenkan-sen Exit", Order=3, GroupName="Parameters")]
		public bool TenkanSen
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Kijun-sen Exit", Order=4, GroupName="Parameters")]
		public bool KijunSen
		{ get; set; }
		
		
		
		
		#endregion
	}
}
