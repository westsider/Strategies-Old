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

/*
	when strat is 60% profitable - forex boat improvements

	Optimize strategy - how can i optimize time frame?
	1. Filters - time of day
			bollinger extended limits on entry
	2. Volume
	3. Trailstop
	4. session time
	5. Stop + Target
		Long Tgt + Stp, Short Tgt + Stop
		statistical hard stop at entry
		statistical volatility stop at entry
		ststistical atr target that is continually optimized my mean of MPE
	*/
//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class AblesysStrat02 : Strategy
	{
		private AblesysMTFD AblesysMTFD1;
		private int			tradeQuantity = 10000;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Ablesys Strat 02";
				Calculate									= Calculate.OnPriceChange;
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
				ATR						= 3;
				Period					= 21;
				Risk					= 14;
				HTFminutes				= 1440;
				EnterNearMA				= true;
				EnterNearT2				= false;
				UseBarDirection			= false;
				EnterWithT1				= true;
				MinTargetDistance		= 0.008;
				UseATRTrailStop			= false;
				UseATRTarget			= true;
				ShowTradesOnChart		= false;
				ShowTradesOnLog			= false;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, HTFminutes);
//				SetProfitTarget("", CalculationMode.Currency, 180);
//				SetStopLoss("", CalculationMode.Currency, 180, false);
			} 
			else if (State == State.DataLoaded)
			{				
				AblesysMTFD1	= AblesysMTFD(ATR, Period, Risk, HTFminutes, EnterNearMA, EnterNearT2, UseBarDirection, EnterWithT1, MinTargetDistance, UseATRTrailStop, UseATRTarget, ShowTradesOnChart, ShowTradesOnLog);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 21)
			return;

			// set up higher time frame
			foreach(int CurrentBarI in CurrentBars)
			{
				if (CurrentBarI < BarsRequiredToPlot)
				{
					return;
				}
			}
			 
			int hostedSignal = AblesysMTFD1.Signals[0];
			
			if (AblesysMTFD1.Signals[0]  == 1 )
			{
				EnterLong(Convert.ToInt32(tradeQuantity), "LE");
				Print("LE " +hostedSignal);
			}
			
			if (AblesysMTFD1.Signals[0]  == -1 )
			{
				EnterShort(Convert.ToInt32(tradeQuantity), "SE");
				Print("SE " +hostedSignal);
			}
			
			//  Exits
			if (AblesysMTFD1.Signals[0]  == 2 )
			{
				ExitLong(Convert.ToInt32(tradeQuantity), "LX", "LE");
				Print("LX " +hostedSignal);
			}
			
			if (AblesysMTFD1.Signals[0]  == -2 )
			{
				ExitShort(Convert.ToInt32(tradeQuantity), "SX", "SE");
				Print("SX " +hostedSignal);
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="ATR", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public int ATR
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Period", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Risk", Order=3, GroupName="NinjaScriptStrategyParameters")]
		public int Risk
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="HTF Minutes", Order=4, GroupName="NinjaScriptStrategyParameters")]
		public int HTFminutes
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Enter Near MA", Order=5, GroupName="NinjaScriptStrategyParameters")]
		public bool EnterNearMA
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Enter Near T2", Order=6, GroupName="NinjaScriptStrategyParameters")]
		public bool EnterNearT2
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Use Bar Direction", Order=7, GroupName="NinjaScriptStrategyParameters")]
		public bool UseBarDirection
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Enter With T1", Order=8, GroupName="NinjaScriptStrategyParameters")]
		public bool EnterWithT1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Min Target Distance", Order=9, GroupName="NinjaScriptStrategyParameters")]
		public double MinTargetDistance
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Use ATR TrailStop", Order=10, GroupName="NinjaScriptStrategyParameters")]
		public bool UseATRTrailStop
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Use ATR Target", Order=11, GroupName="NinjaScriptStrategyParameters")]
		public bool UseATRTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Show Trades On Chart", Order=12, GroupName="NinjaScriptStrategyParameters")]
		public bool ShowTradesOnChart
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Show Trades On Log", Order=13, GroupName="NinjaScriptStrategyParameters")]
		public bool ShowTradesOnLog
		{ get; set; }
		#endregion

	}
}
