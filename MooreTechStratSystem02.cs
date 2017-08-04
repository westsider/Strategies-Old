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
using System.IO;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class MooreTechStratSystem02 : Strategy
	{
		/// <summary>
		/// Working Strat Created on 7/20/2017
		/// [ ]  get Account value, buying power
		/// [ ]  create auto position sise by Close[0] / ( buying power / numStrategies)
		/// </summary>
		private MooreTechSwing02 MooreTechSwing021;
		private int 	startTime 	= 700;  // depending onlocal time
        private int	 	endTime 	= 1300;
		private int		ninja_Start_Time;
		private int		ninja_End_Time;
		private 		StreamWriter sw; 
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "MooreStrat System 02";
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
				DisconnectDelaySeconds 						= 120;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= false;
				/// inputs 
				shares					= 100;			/// 	int		shares			= 100;
				swingPct				= 0.005;		///		double  swingPct		= 0.005;
				minBarsToLastSwing 		= 70;			/// 	int MinBarsToLastSwing 	= 70;
				enableHardStop 			= true;			/// 	bool setHardStop = true, int pctHardStop 3, 
				pctHardStop  			= 3;
				enablePivotStop 		= true;			/// 	bool setPivotStop = true, int pivotStopSwingSize = 5, 
				pivotStopSwingSize 		= 5;
				pivotStopPivotRange 	= 0.2;			///		double pivotStopPivotSlop = 0.2
				/// show plots
				showUpCount 			= false;		/// 	bool ShowUpCount 			= false;
				showHardStops 			= false;		/// 	bool show hard stops 		= false;
				printTradesOnChart		= false;		/// 	bool printtradesOn Chart	= false
				printTradesSimple 		= false;		/// 	bool printTradesSimple 		= false
				printTradesTolog 		= true;			/// 	bool printTradesTolog 		= true;
				ComputerName			= "MBP";
				Path					= @"C:\Users\MBPtrader\Documents\NT_CSV\connected.csv";
				//Path					=  @"C:\Users\Administrator\Documents\connected.csv";
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				ClearOutputWindow();   
				MooreTechSwing021 = MooreTechSwing02(shares, swingPct, minBarsToLastSwing, enableHardStop, pctHardStop, 
					enablePivotStop, pivotStopSwingSize, pivotStopPivotRange, showUpCount, showHardStops, printTradesOnChart, 
					printTradesSimple, printTradesTolog);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 20)
			return;
			//Draw.TextFixed(this, "tag1", "Strategy is applied to " + Account.Name, TextPosition.BottomLeft);
			//Print(Account.UpdateCashValue);
//			Print("Account value = " + AccountItem.TotalCashBalance.ToString());
			/// entry on second bar is cool but if gap past we see no entry
			
			/// check data
			if( MooreTechSwing021.Signals.IsValidDataPoint(0) ) {

				/// long Entry
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)
					if (MooreTechSwing021.Signals[0] == 1 )
						{
							EnterLong(Convert.ToInt32(shares), "");
    						//string uniCodeArrow = "\u21E7"; 
							//tradeUpdate(tradeType: uniCodeArrow+" Long Entry on "+Instrument.MasterInstrument.Name);
						}
				/// short entry
				if (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)
					if ( MooreTechSwing021.Signals[0] == -1 )
					{
						EnterShort(Convert.ToInt32(shares), "");
						//string uniCodeArrow = "\u21E9"; 
						//tradeUpdate(tradeType: uniCodeArrow+" Short Entry on "+Instrument.MasterInstrument.Name);
					}
				/// long exit
				if (MooreTechSwing021.Signals[0] == 2 )
				{
					ExitLong(Convert.ToInt32(shares));
					//string uniCodeArrow = "\u23F9"; 
					//tradeUpdate(tradeType: uniCodeArrow+" Long Exit on "+Instrument.MasterInstrument.Name);
				}
				/// short exit
				if (MooreTechSwing021.Signals[0] == -2 )
				{
					ExitShort(Convert.ToInt32(shares));
					//string uniCodeArrow = "\u23F9"; 
					//tradeUpdate(tradeType: uniCodeArrow+" Short Exit on "+Instrument.MasterInstrument.Name);
				}
				
				///Draw.Text(this, "testSignal"+CurrentBar, MooreTechSwing011.Signals[0].ToString(), 0, High[0], Brushes.Yellow);
				
				Print(MooreTechSwing021.Signals[0]);
				
			} 
			
			/// missing signal if gap
			/// work on close > close 1 = missed decent entry, when bars over lap li || Close[0] >= EntryPrice
			/// however this is getting weird to get the strat this far out of sync, put this in the indicator!!!!
			/// figure out why the signal is 1 bar late and enter on bar close unless the price is rising.... possible?
//			if ( MooreTechSwing011.Signals.IsValidDataPoint(1)) {
//					///Draw.Text(this, "testSignal2"+CurrentBar, MooreTechSwing011.Signals[1].ToString(), 0, High[0], Brushes.Cyan);
//				// if long and missed signal was short, go short
//				if (Position.MarketPosition == MarketPosition.Long && MooreTechSwing011.Signals[1] == -1) {
//					/// if short entry benificial go short else exit
//					if (Close[0] >= Close[1] || Close[0] > Low[1] ) {
//						EnterShort(Convert.ToInt32(shares), "");
//					} else {
//						ExitLong(Convert.ToInt32(shares));
//					}
					
//				}
//				// if short and missed signal was long, go long
//				if (Position.MarketPosition == MarketPosition.Short && MooreTechSwing011.Signals[1] == 1) {
//					/// if long entry benificial go long else exit
//					if (Close[0] <= Close[1] || Close[0] <= Low[1]  ) {
//						EnterLong(Convert.ToInt32(shares), "");
//					} else {
//						ExitShort(Convert.ToInt32(shares));
//					}
//				}
//				/// TODO only enter if favorable gap else go flat
//			}
			
//	sendDailyReport();	
		}
		
		public void tradeUpdate(string tradeType) {
//			var titleMessage = tradeType;
//			var bodyMessage = ComputerName + " " + tradeType + " occurred on " + Time[0].ToString() +" @ "+Close[0] +"\n";
//			//bodyMessage = bodyMessage +"\n"+titleMessage +"\n"+ bodyMessage;
//			bodyMessage = bodyMessage +"Trade count:  " + SystemPerformance.AllTrades.TradesPerformance.TradesCount.ToString("0");
//			bodyMessage = bodyMessage +"\nNet profit:      " + SystemPerformance.AllTrades.TradesPerformance.NetProfit.ToString("0.0");
//			bodyMessage = bodyMessage +"\nProfit factor:   " + SystemPerformance.AllTrades.TradesPerformance.ProfitFactor.ToString("0.00");
//			bodyMessage = bodyMessage +"\n\n----->   Detailed Report   <-----";
//			bodyMessage = bodyMessage +"\nMax # of consecutive losers is: " + SystemPerformance.AllTrades.TradesPerformance.MaxConsecutiveLoser;
//			bodyMessage = bodyMessage +"\nLargest loss of all trades is:   $" + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestLoser.ToString("0.0");
//			bodyMessage = bodyMessage +"\nLargest win of all trades is:    $" + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestWinner.ToString("0.0");
//			bodyMessage = bodyMessage +"\nAverage monthly profit is:      $" + SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth.ToString("0.0");
			
//			if (SystemPerformance.AllTrades.WinningTrades.Count != 0) {
//					double winPct = (Convert.ToDouble( SystemPerformance.AllTrades.WinningTrades.Count) / Convert.ToDouble(SystemPerformance.AllTrades.TradesPerformance.TradesCount));
//					winPct = winPct * 100;
//					bodyMessage = bodyMessage + "\n" + "Win Percent is:       " + winPct.ToString("0.0")+ "%";
//				}
			var titleMessage = tradeType;
			var bodyMessage = ComputerName + ", " + tradeType + ", occurred on, " + Time[0].ToString() +", "+Close[0] +",";
			//bodyMessage = bodyMessage +"\n"+titleMessage +"\n"+ bodyMessage;
			bodyMessage = bodyMessage +"Trade count,  " + SystemPerformance.AllTrades.TradesPerformance.TradesCount.ToString("0")+", ";
			bodyMessage = bodyMessage +"Net profit, " + SystemPerformance.AllTrades.TradesPerformance.NetProfit.ToString("0.0")+", ";
			bodyMessage = bodyMessage +"Profit factor," + SystemPerformance.AllTrades.TradesPerformance.ProfitFactor.ToString("0.00")+", ";
			bodyMessage = bodyMessage +"----->   Detailed Report   <-----,";
			bodyMessage = bodyMessage +"Max # of consecutive losers is," + SystemPerformance.AllTrades.TradesPerformance.MaxConsecutiveLoser+", ";
			bodyMessage = bodyMessage +"Largest loss of all trades is, $," + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestLoser.ToString("0.0")+", ";
			bodyMessage = bodyMessage +"Largest win of all trades is $," + SystemPerformance.AllTrades.TradesPerformance.Currency.LargestWinner.ToString("0.0")+", ";
			bodyMessage = bodyMessage +"Average monthly profit is $," + SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth.ToString("0.0");
			
			if (SystemPerformance.AllTrades.WinningTrades.Count != 0) {
					double winPct = (Convert.ToDouble( SystemPerformance.AllTrades.WinningTrades.Count) / Convert.ToDouble(SystemPerformance.AllTrades.TradesPerformance.TradesCount));
					winPct = winPct * 100;
					bodyMessage = bodyMessage + ", " + "Win Percent is, " + winPct.ToString("0.0")+ "%";
				}
			
			///
			/// TODO: ROI
			/// 
			
			/// TODO: save file
			string messageToDisplay = ComputerName+" Trade at " +Time[0].ToString()  + " " + Instrument.MasterInstrument.Name;
			//Print(" ");
			//Print(messageToDisplay);
			//Print(titleMessage);
			//Print(bodyMessage);
			//Print(" ");
			string messageToFile = messageToDisplay + ", " + titleMessage + ", " + bodyMessage;
			appendConnectionFile(message: messageToFile);
			Print(messageToFile);
		}
		
		///  stremwriter
		public void appendConnectionFile(string message) {
			try {
				// write connection to a file
				sw = File.AppendText(Path);  // Open the path for writing
				sw.WriteLine(message);
				sw.Close(); // Close the file to allow future calls to access the file again
			}
			catch(IOException e) {
						Print(
				        "{0}: The write operation could not " +
				        "be performed because the specified " +
				        "part of the file is locked." + 
				        e.GetType().Name);
					}
		}
		
		/// Daily Report
		public void sendDailyReport() {
			/// C0nvert Military Time to Ninja Time
			ninja_Start_Time = startTime * 100;
			ninja_End_Time = endTime * 100;
			
			if (ToTime(Time[0]) == ninja_Start_Time ) {
				tradeUpdate(tradeType:"Market Open Report for "+Instrument.MasterInstrument.Name);
			}
			if (ToTime(Time[0]) == ninja_End_Time ) {
				tradeUpdate(tradeType:"Market Close Report for "+Instrument.MasterInstrument.Name);
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
				[NinjaScriptProperty]
		[Display(Name="Computer Name", Order=6, GroupName="Statistics")]
		public string ComputerName
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="File Path", Order=7, GroupName="Statistics")]
		public string Path
		{ get; set; }
		
		#endregion
	}
}
