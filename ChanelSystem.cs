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
		public int 		entryBar2;
		
		public bool 	entryOne	 	= false;
		public bool 	entryTwo 		= false;
		public string 	ComputerName	= "MBP";
		///  money management
		public double 	shares;
		public int  	portfolioSize	= 826000;
		public int	numSystems		= 10;
		private int 	initialBalance;
		private	int 	cashAvailiable;
		private	double 	priorTradesCumProfit;
		private	int 	priorTradesCount;
		private	double 	sharesFraction;
		
		/// stops
		private double 	stopLine;
		private double 	trailStop;
		private double  lossInPonts;
		private bool	autoStop = true;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Channel System";
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
		/// Universe: SPY, QQQ, DIA, MDY, IWM, EFA, ILF, EEM, EPP,  IEV (USO Good too)
		/// Performance:  cash 75% of the time,Win Pct 65%, average wins >= average losses
		/// </summary>
		protected override void OnBarUpdate()
		{
			/// Long Channel Conditions + Entry
			setLongSentry(stopPct: 3);
			/// %R Target
			setLongTarget();
			/// Stop - check math change worls indexes to 5%
			setInitialStop(pct: 3, auto: true);
			/// N day stop
			exitAfterNBars(bars: 7, active: ExitAfterNbars);  // minor differece
			/// trail stop
			setTrailOnClose(isOn: true);

			/// MARK: - TODO - market Condition Green color bars in indicator, add public series
			/// MARK: - TODO - Position sizing: Maximum 2 positions per index
			/// MARK: - TODO ------>   Entry #2
			setSecondEntry(stopPct: 3);
			
		}
		
		/// Advanced: use the word market model to but the strongest index
		/* Optional: Windfall profit exit: if you a 5% gain in a position, 
		either cash it or tighten your trailing stop to 1% trailing stop. 
		reduce volatility, you could elect to take only the US index signals. 
		To reduce volatility, you could elect to not take signals when price is within 2% of the index’s 200 day MA. 
		To try for more profits, you could elect to trail successful trades with a 1x ATR% trailing stop or 3% trailing stop 
		and try to convert this trade into a longer term trend following position.
		*/

		protected int calcPositionSize() {
			/// d. Maximum 20% of capital in any single index , Maximum 10 positions 
			/// e. Buy in equal dollar amounts
			/// c. Maximum 10% of capital in any single position 
			/// Store the strategy's prior cumulated realized profit and number of trades
			priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
			/// cal initialBalance s portion  of portfoli / num systems
			initialBalance = portfolioSize / numSystems ;
			/// Adjust position size for profit and loss
			cashAvailiable = initialBalance + (int)priorTradesCumProfit;
			/// calc positionsize
			sharesFraction = cashAvailiable / Close[0];
			return (int)sharesFraction;
		}
		
		protected void setLongSentry(int stopPct) {
			/// Entry #1
			if ( Position.MarketPosition == MarketPosition.Flat && entryConditions() ) {
				shares = calcPositionSize();
				EnterLong(Convert.ToInt32(shares), "");
				entryPrice = Close[0];
				entryBar = 1;
				entryOne = true;
				string entryData = "------->  LE on " +Instrument.MasterInstrument.Name + " shares " + shares + "  $" + cashAvailiable;
				Print(entryData);
				
				string entryTxt = "LE 1\n" + shares+" shares" + "\n$"+cashAvailiable.ToString("0");
				Draw.Text(this, "Cash"+CurrentBar, entryTxt, 0, MIN(Low, 10)[0] - (TickSize* 40), Brushes.LimeGreen);
				
				calcInitialStop(pct: stopPct);
				// calc hard stop loss in $$$
				/// entryPrice - stopline * shares
				// show loss at stop line
				lossInPonts =  ( stopLine - entryPrice );
				double lossInTrade = shares * lossInPonts;
				double lossInPct  = lossInTrade / cashAvailiable;
				string traderLossStats = lossInPonts.ToString("0.00") + "\t$" + lossInTrade.ToString("0") + "\t%" +lossInPct.ToString("0.00") ;
				Draw.Text(this, "loss"+CurrentBar, traderLossStats, 0, stopLine - (TickSize* 40), Brushes.Crimson);
			}
		}
		
		/// MARK: - TODO Entry #2
		protected void setSecondEntry(int stopPct) {
			/// this extremely fukin complicated
			/// assign entry names for both entries
			/// assign discrete exits for both LAMO wait TIll last
//			if (Position.MarketPosition == MarketPosition.Flat && entryConditions() && !entryTwo  ) {
//				EnterLong(Convert.ToInt32(shares), "");
//				entryBar2 = 1;
//				entryTwo = true;
//			}
		}
		
		protected void setLongTarget() {
			/// %R Target
			if ( Position.MarketPosition == MarketPosition.Long && WilliamsR(10)[0] > -30 ) {
				ExitLong(Convert.ToInt32(shares));
				resetEntry();
				tradeUpdate(tradeType: " LX_Target on "+Instrument.MasterInstrument.Name);
			}
		}
		
		protected void calcInitialStop(int pct) {
			double convertedPct = 0.03;
			if ( !autoStop ) {
				convertedPct = pct * 0.01;
			}
			/// Auto Set Position stop to 3% US SPY QQQ DIA MDY IWM, 5% EFA ILF EEM EPP IEV
			if( autoStop ) {
				if ( Instrument.MasterInstrument.Name == "SPY" ||
						Instrument.MasterInstrument.Name == "QQQ" ||
						Instrument.MasterInstrument.Name == "DIA" ||
						Instrument.MasterInstrument.Name == "MDY" ||
						Instrument.MasterInstrument.Name == "IWM" 
					) {
						convertedPct = 3 * 0.01;	// 3% for US Indexes
				} else {
					convertedPct = 5 * 0.01;		// 5% for worls
				}
			}
			stopLine =  entryPrice - ( entryPrice * convertedPct);
			//if( BarsSinceEntryExecution() == 1 ) {
				trailStop = stopLine;
			//}
		}
		
		///  initial stop
		protected void setInitialStop(int pct, bool auto) {
//			double converteddPct = pct * 0.01;
//			stopLine =  entryPrice - ( entryPrice * converteddPct);
			/// show entry + stop line
			if ( Position.MarketPosition == MarketPosition.Long ){
				Draw.Text(this, "EL"+CurrentBar, "-", 0, entryPrice, Brushes.LimeGreen);
				Draw.Text(this, "sL"+CurrentBar, "-", 0, stopLine, Brushes.Crimson);
			}
			/// exit trade
			if ( Position.MarketPosition == MarketPosition.Long && Close[0] < stopLine ) {
				ExitLong(Convert.ToInt32(shares));
				resetEntry();
				tradeUpdate(tradeType: " LX_Stop on "+Instrument.MasterInstrument.Name);
			}
		}
		
		/// trail stopadjusted for close > close[1]
		protected void setTrailOnClose(bool isOn) {
			if ( !isOn ) { return; }
			if ( Position.MarketPosition == MarketPosition.Long && BarsSinceEntryExecution() >= 1 ){
				if (Close[0] > Close[1] ) {
					double newTrailStop = Close[0]  + lossInPonts;
					/// stop only moves up
					if (newTrailStop > trailStop ) {
						trailStop = newTrailStop;
					}
					
				}
				Draw.Text(this, "trail"+CurrentBar, "*", 0, trailStop, Brushes.Crimson);
			}
			exitOnTrailStop(isOn: isOn);
		}
		/// trail stop
		protected void exitOnTrailStop(bool isOn) {
			if ( !isOn ) { return; }
			if ( Position.MarketPosition == MarketPosition.Long && Close[0] < trailStop ) {
				ExitLong(Convert.ToInt32(shares));
				resetEntry();
				tradeUpdate(tradeType: " LX_Trail on "+Instrument.MasterInstrument.Name);
				Draw.Text(this, "LX_Trail"+CurrentBar, "LX_Trail", 0, Low[0]- (TickSize * 40), Brushes.Crimson);
			}
		}
		
		///  exit  N Bars
		protected void exitAfterNBars(int bars, bool active)
		{
			/// Stop after n Bars
			entryBar ++;
			if ( Position.MarketPosition == MarketPosition.Long && entryBar > bars ) {
				ExitLong(Convert.ToInt32(shares));
				resetEntry();
				tradeUpdate(tradeType: " LX_Time on "+Instrument.MasterInstrument.Name);
			}
		}
		
		protected void resetEntry()
		{
			entryBar = 0;
			entryOne = false;
		}
		
		protected bool entryConditions()
		{
			bool signal = false;
			if ( Position.MarketPosition == MarketPosition.Flat && Close[0] > Math.Abs(sma0[0])  && High[0] < SMA(10)[0] && WilliamsR(10)[0] < -80 )
				signal = true;
			return signal;
		}
		
		public void tradeUpdate(string tradeType) {
			//var titleMessage = tradeType;
			var bodyMessage = ComputerName + ", " + tradeType + ", occurred on, " + Time[0].ToString() +", "+Close[0] +",";
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
			
			/// TODO: save file as stream
			string messageToDisplay = ComputerName+" Trade at " +Time[0].ToString()  + " " + Instrument.MasterInstrument.Name;
			string messageToFile = messageToDisplay + ", " + bodyMessage;
			//appendConnectionFile(message: messageToFile);
			Print(messageToFile);
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
