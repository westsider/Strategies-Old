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
	public class EMJcross : Strategy
	{
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "EMJ Cross";
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
				Fast					= 34;
				Med					= 68;
				Slow					= 116;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			double fastMa = EMA(34)[0]; 
			double medMa = EMA(68)[0]; 
			double slowMa = SMA(116)[0]; 
			int tradeQuantity = 500;
			
			/// Long
			if ( CrossAbove( EMA(34), EMA(68), 1 ) && Close[0] >= slowMa) {
				//Draw.ArrowUp(this, "xUP"+CurrentBar.ToString(), true, 1, fastMa - (TickSize *5 ),Brushes.LimeGreen); 
				EnterLong(Convert.ToInt32(tradeQuantity), "LE");
			}
			/// Short
			if ( CrossBelow( EMA(34), EMA(68), 1 ) && Close[0] <= slowMa) {
				//Draw.ArrowDown(this, "xDN"+CurrentBar.ToString(), true, 1, fastMa + (TickSize *5 ),Brushes.Red); 
				EnterShort(Convert.ToInt32(tradeQuantity), "SE");
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Fast", Order=1, GroupName="NinjaScriptStrategyParameters")]
		public int Fast
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Med", Order=2, GroupName="NinjaScriptStrategyParameters")]
		public int Med
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name="Slow", Order=3, GroupName="NinjaScriptStrategyParameters")]
		public int Slow
		{ get; set; }
		#endregion

	}
}
