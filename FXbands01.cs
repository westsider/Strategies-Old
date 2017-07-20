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
	public class FXbands01 : Strategy
	{
		private VwapWeeklyCsi VwapWeeklyCsi1;
		private int TradeQuantity = 10000;
		private bool didSendMail = false;
		private MarketPosition  currentPosition;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "FXbands01";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= false;
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
				IsInstantiatedOnEachOptimizationIteration	= true;
				BandRange			= 0.03;
				Average				= 88;
				RangeLength			= 100;
				SmoothLength		= 100;
				BandOne				= 1;
				BandTwo				= 2;
				BandThree			= 3;
				CCIIposOne			= 100;
				CCIposTwo			= 180;
				CCInegOne			= -100;
				CCInegTwo			= -180;
				CCIperiod			= 14;
				
				TargetTicks			= 4200;
				StopTicks			= 2900;
				TrailTicks			= 1500;
				
				UseLastBandValue	= true;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Week, 1);
				SetProfitTarget("", CalculationMode.Ticks, TargetTicks);
				SetStopLoss("", CalculationMode.Ticks, StopTicks, false);
				// 	The SetTrailStop() method can NOT be used concurrently with the SetStopLoss()
        		// SetTrailStop(CalculationMode.Ticks, TrailTicks);
			}
			else if (State == State.DataLoaded)
			{				
				VwapWeeklyCsi1				= VwapWeeklyCsi(BandRange, Average, 100, 100, 1, 2, 3, true, true, 14, 100, 180, -100, -180, true);
			}
		}
		
		protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
		{
			
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 1)
			return;

			 // Long Entry
			if (VwapWeeklyCsi1[0] == 1 || VwapWeeklyCsi1[0] == 2 )
			{
				EnterLong(Convert.ToInt32(TradeQuantity), @"LE");
				
				currentPosition = Positions[0].MarketPosition;
			}
			
			 // Short Entry
			if (VwapWeeklyCsi1[0] == -1 || VwapWeeklyCsi1[0] == -2 )
			{
				EnterShort(Convert.ToInt32(TradeQuantity), @"SE");

				currentPosition = Positions[0].MarketPosition;
			}
			
			// trade notifications
			if (currentPosition != Positions[0].MarketPosition) {
				//Print("currentPosition" + currentPosition);
				
				currentPosition = Positions[0].MarketPosition;
				
				string messageBody = "On " + Time[0].ToShortDateString() + " at " + Time[0].ToShortTimeString() + " A " +  Positions[0].MarketPosition + "Entry on " + Instrument.MasterInstrument.Name + " was generated at " + Close[0];
			
				string messageTitle = "Trade Alert " + Positions[0].MarketPosition + " On " + Instrument.MasterInstrument.Name;
	
				messageBody = messageBody + "\n";
				//messageBody = messageBody + "\n" + "The strategy has taken " + SystemPerformance.LongTrades.TradesCount + " long trades.";
				messageBody = messageBody + "\n" + "Net profit is: " + SystemPerformance.AllTrades.TradesPerformance.NetProfit;
				messageBody = messageBody + "\n" + "Profit factor: " + SystemPerformance.AllTrades.TradesPerformance.ProfitFactor.ToString("0.000");
				if (SystemPerformance.AllTrades.WinningTrades.Count != 0) {
					double winPct = (Convert.ToDouble( SystemPerformance.AllTrades.WinningTrades.Count) / Convert.ToDouble(SystemPerformance.AllTrades.TradesPerformance.TradesCount));
					winPct = winPct * 100;
					messageBody = messageBody + "\n" + "Pct Win is " + winPct.ToString("0.0")+ "%";
				}
				
				messageBody = messageBody + "\n";
				messageBody = messageBody + "\n" + "Trades count is: " + SystemPerformance.AllTrades.TradesPerformance.TradesCount;
				messageBody = messageBody + "\n" + "Win Count is " + SystemPerformance.AllTrades.WinningTrades.Count;
				messageBody = messageBody + "\n" + "Gross profit is: " + SystemPerformance.AllTrades.TradesPerformance.GrossProfit;
				messageBody = messageBody + "\n" + "Gross loss is: " + SystemPerformance.AllTrades.TradesPerformance.GrossLoss;
				messageBody = messageBody + "\n" + "Average profit: " + SystemPerformance.AllTrades.TradesPerformance.Percent.AverageProfit.ToString("0.000");
				messageBody = messageBody + "\n" + "Profit per month: " + SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth.ToString("0.00");
				messageBody = messageBody + "\n" + "Drawdown: " + SystemPerformance.AllTrades.TradesPerformance.Currency.Drawdown.ToString();
				
//				Print(" ");
//				Print(messageTitle);
//				Print(messageBody);
//				Print(" ");
//				Print(" ");
				
				Share("Hotmail", messageBody, new object[]{ "whansen1@mac.com", messageTitle, @"C:\Users\MBPtrader\Pictures\EURUSD_Opt_6_27.PNG"});
			}
			
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="BandRange", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public double BandRange
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Average", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public int Average
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="RangeLength", Order=3, GroupName="NinjaScriptStrategyParameters")]
		public int RangeLength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="SmoothLength", Order=4, GroupName="NinjaScriptStrategyParameters")]
		public int SmoothLength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="BandOne", Order=5, GroupName="NinjaScriptStrategyParameters")]
		public double BandOne
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="BandTwo", Order=6, GroupName="NinjaScriptStrategyParameters")]
		public double BandTwo
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="BandThree", Order=7, GroupName="NinjaScriptStrategyParameters")]
		public double BandThree
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="CCIIposOne", Order=8, GroupName="NinjaScriptStrategyParameters")]
		public int CCIIposOne
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="CCIposTwo", Order=9, GroupName="NinjaScriptStrategyParameters")]
		public int CCIposTwo
		{ get; set; }

		[NinjaScriptProperty]
		[Range(-300, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="CCInegOne", Order=10, GroupName="NinjaScriptStrategyParameters")]
		public int CCInegOne
		{ get; set; }

		[NinjaScriptProperty]
		[Range(-300, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="CCInegTwo", Order=11, GroupName="NinjaScriptStrategyParameters")]
		public int CCInegTwo
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="CCIperiod", Order=12, GroupName="NinjaScriptStrategyParameters")]
		public int CCIperiod
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="UseLastBandValue", Order=13, GroupName="NinjaScriptStrategyParameters")]
		public bool UseLastBandValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="TargetTicks", Order=14, GroupName="NinjaScriptStrategyParameters")]
		public int TargetTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="StopTicks", Order=15, GroupName="NinjaScriptStrategyParameters")]
		public int StopTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="TrailTicks", Order=15, GroupName="NinjaScriptStrategyParameters")]
		public int TrailTicks
		{ get; set; }
		#endregion

	}
}
