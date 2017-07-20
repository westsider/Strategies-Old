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
	public class MooreStratSystem01 : Strategy
	{
		private MooreTechSwing01 MooreTechSwing011;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "MooreStratSystem01";
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
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				MooreTechSwing011 = MooreTechSwing01(100, 0.005, 70, true, 3, true, 5, 0.2, false, false, false, false, true);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 1)
			return;
			/// check data
			if( MooreTechSwing011.Signals.IsValidDataPoint(0) ) {
				//Print("/nValid Signal ----------->  "+MooreTechSwing011.Signals[0]);
				/// long Entry
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)
					if (MooreTechSwing011.Signals[0] == 1 )
						{
							EnterLong(Convert.ToInt32(DefaultQuantity), "");
						}
				/// short entry
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)
					if (MooreTechSwing011.Signals[0] == -1 )
					{
						EnterShort(Convert.ToInt32(DefaultQuantity), "");
					}
				/// long exit
				if (MooreTechSwing011.Signals[0] == 2 )
				{
					ExitLong();
				}
				/// short exit
				if (MooreTechSwing011.Signals[0] == -2 )
				{
					ExitShort();
				}
			} 
		}
	}
}
