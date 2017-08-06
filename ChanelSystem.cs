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
	public class ChanelSystem : Strategy
	{
		private SMA		sma0;
		public double 	entryPrice;
		public int 		entryBar;
		public int 		shares = 100;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "ChanelSystem";
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
				IsInstantiatedOnEachOptimizationIteration	= true;
				USIndex							= true;
				SecondSignal					= true;
				NumberOfSystems					= 10;
				ExitAfterNbars					= false;
				StartingCapital					= 826000;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				sma0		= SMA(200);
			}
		}

		/// <summary>
		/// Universe: SPY, QQQ, DIA, MDY, IWM, EFA, ILF, EEM, EPP,  IEV
		/// Performance:  cash 75% of the time,Win Pct 65%, average wins >= average losses
		/// </summary>
		protected override void OnBarUpdate()
		{
			/// Position sizing: Maximum 2 positions per index, Maximum 10 positions c. Maximum 10% of capital in any single position 
			/// d. Maximum 20% of capital in any single index e. Buy in equal dollar amounts
			/// 2nd entry
			/// trail stop
			/// 7 day stop
			exitAfterNBars(bars: 7, active: ExitAfterNbars);  // minor differece
			/// market Condition Green color bars in indicator, add public series
			/// worlds stop of 5%
			
			
			/// Entry #1
			if ( Position.MarketPosition == MarketPosition.Flat && Close[0] > Math.Abs(sma0[0])  && High[0] < SMA(10)[0] && WilliamsR(10)[0] < -80 ) {
				EnterLong(Convert.ToInt32(shares), "");
				entryPrice = Close[0];
				entryBar = 1;
			}
			/// Entry #2
			/// 
			/// Target
			if (WilliamsR(10)[0] > -30 ) {
				ExitLong(Convert.ToInt32(shares));
				entryBar = 0;
			}
			/// Stop - check math
			if ( Close[0] < entryPrice - ( entryPrice * 0.03)) {
				ExitLong(Convert.ToInt32(shares));
				entryBar = 0;
			}
		}
		
		/// Advanced: use the word market model to but the strongest index
		/* Optional: Windfall profit exit: if you a 5% gain in a position, 
		either cash it or tighten your trailing stop to 1% trailing stop. 
		reduce volatility, you could elect to take only the US index signals. 
		To reduce volatility, you could elect to not take signals when price is within 2% of the index’s 200 day MA. 
		To try for more profits, you could elect to trail successful trades with a 1x ATR% trailing stop or 3% trailing stop 
		and try to convert this trade into a longer term trend following position.
		*/

		/// <summary>
		/// After 2 Pints exit o N Bars
		/// </summary>
		/// <param name="bars"></param>
		/// <param name="active"></param>
		protected void exitAfterNBars(int bars, bool active)
		{
			/// Stop after n Bars
			entryBar ++;
			if ( entryBar > bars ) {
				ExitLong(Convert.ToInt32(shares));
				entryBar = 0;
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="US Index", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public bool USIndex
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Second Signal", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public bool SecondSignal
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name="Exit After N bars", Order=3, GroupName="NinjaScriptStrategyParameters")]
		public bool ExitAfterNbars
		{ get; set; }
		

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Number Of Systems", Order=4, GroupName="NinjaScriptStrategyParameters")]
		public int NumberOfSystems
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Starting Capital", Order=5, GroupName="NinjaScriptStrategyParameters")]
		public int StartingCapital
		{ get; set; }
		#endregion

	}
}
