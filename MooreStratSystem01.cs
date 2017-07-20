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
				Slippage									= 0.01;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= false;
				/// inputs 
				shares				= 100;			/// 	int		shares			= 100;
				swingPct			= 0.005;		///		double  swingPct		= 0.005;
				minBarsToLastSwing 	= 70;			/// 	int MinBarsToLastSwing 	= 70;
				enableHardStop 		= true;			/// 	bool setHardStop = true, int pctHardStop 3, 
				pctHardStop  		= 3;
				enablePivotStop 	= true;			/// 	bool setPivotStop = true, int pivotStopSwingSize = 5, 
				pivotStopSwingSize 	= 5;
				pivotStopPivotRange = 0.2;			///		double pivotStopPivotSlop = 0.2
				/// show plots
				showUpCount 			= false;		/// 	bool ShowUpCount 			= false;
				showHardStops 			= false;		/// 	bool show hard stops 		= false;
				printTradesOnChart		= false;		/// 	bool printtradesOn Chart	= false
				printTradesSimple 		= false;		/// 	bool printTradesSimple 		= false
				printTradesTolog 		= true;			/// 	bool printTradesTolog 		= true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				ClearOutputWindow();   
				MooreTechSwing011 = MooreTechSwing01(shares, 0.005, 70, true, 3, true, 5, 0.2, false, false, false, false, true);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 1)
			return;
			/// check data
			if( MooreTechSwing011.Signals.IsValidDataPoint(0) ) {

				/// long Entry
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)
					if (MooreTechSwing011.Signals[0] == 1 )
						{
							EnterLong(Convert.ToInt32(shares), "");
						}
				/// short entry
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)
					if (MooreTechSwing011.Signals[0] == -1 )
					{
						EnterShort(Convert.ToInt32(shares), "");
					}
				/// long exit
				if (MooreTechSwing011.Signals[0] == 2 )
				{
					ExitLong(Convert.ToInt32(shares));
				}
				/// short exit
				if (MooreTechSwing011.Signals[0] == -2 )
				{
					ExitShort(Convert.ToInt32(shares));
				}
			} 
		}
		
		#region Properies
		
		///  inputs
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Shares", Order=1, GroupName="Parameters")]
		public int shares
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Swing Pct", Order=2, GroupName="Parameters")]
		public double swingPct
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Min Bars To Last Swing", Order=3, GroupName="Parameters")]
		public int minBarsToLastSwing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Hard Stop", Order=4, GroupName="Parameters")]
		public bool enableHardStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Pct Hard Stop", Order=5, GroupName="Parameters")]
		public int pctHardStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Enable Pivot Stop", Order=6, GroupName="Parameters")]
		public bool enablePivotStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Pivot Stop Swing Size", Order=7, GroupName="Parameters")]
		public int pivotStopSwingSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Pivot Stop Range", Order=8, GroupName="Parameters")]
		public double pivotStopPivotRange
		{ get; set; }
		
		/// Statistics
		[NinjaScriptProperty]
		[Display(Name="Show Up Count", Order=1, GroupName="Statistics")]
		public bool showUpCount
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Hard Stops", Order=2, GroupName="Statistics")]
		public bool showHardStops
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Trades On Chart", Order=3, GroupName="Statistics")]
		public bool printTradesOnChart
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Show Trades Simple", Order=4, GroupName="Statistics")]
		public bool printTradesSimple
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Send Trades To log", Order=5, GroupName="Statistics")]
		public bool printTradesTolog
		{ get; set; }
		
		#endregion
	}
}
