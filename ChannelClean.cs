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
using System.Windows.Forms;
using System.Net;
using System.Net.Cache;
using System.Web.Script.Serialization;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class ChannelClean : Strategy
	{
		public double 	entryPrice;
		public int 		entryBar;
		public int 		entryBar2;
		public bool 	entryOne	 	= false;
		public bool 	entryTwo 		= false;
		
		///  money management
		public double 	shares;
		public double 	cost;
		private int 	initialBalance;
		private	int 	cashAvailiable;
		private	double 	priorTradesCumProfit;
		private	int 	priorTradesCount;
		private	double 	sharesFraction;
		
		/// stops
		private double 	theStop;
		private double 	trailStop = 0.0;
		private double  lossInPonts;
		private bool	autoStop = true;
		private double  entryBarnum;
		private double  stopDistance;
		private int 	tradeCount;
		private bool SavedCSVtoday = false;
		
		/// struct for firebase
		private struct PriceData
		{
			public  string 	date 	{ get; set; }
			public  string 	ticker 	{ get; set; }
			public	double 	profit	{ get; set; }
			public	double 	winPct 	{ get; set; }
			public	double 	cost		{ get; set; }
			public  double 	roi	{ get; set; }
		}
		private PriceData priceData = new PriceData{};
		private List<PriceData> myList = new List<PriceData>();
		
		private string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		NinjaTrader.NinjaScript.PerformanceMetrics.SampleCumProfit myProfit;
		
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Channel Clean";
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
				IncludeTradeHistoryInBacktest = true;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				MaxRisk					= 100;
				ExitAfter				= 7;
				ShowText 				= false;
			}
			else if (State == State.DataLoaded)
			{				
				ClearOutputWindow(); 
				checkForDirectory();
			}
			else if (State == State.Configure)
			  {
			    // Instantiate myProfit to a new instance of SampleCumProfit
			    myProfit = new NinjaTrader.NinjaScript.PerformanceMetrics.SampleCumProfit();
			    // Use AddPerformanceMetric to add myProfit to the strategy
			    AddPerformanceMetric(myProfit);
			  }
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 20 ) { return; }
			SavedCSVtoday = false;
			enterLong(showStop: ShowText);
			exitAfter(days: ExitAfter);
			exitHardStop();
			percentRexit();
			setTrailStop();
			extendedTarget();
			createCSV(isOn: false, debug: false);
			pushToFirebase(isOn: ShowText, debug: false);  // showtext now firebase switch
		}
		
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									push to firebase
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		
		private void pushToFirebase(bool isOn, bool debug) {
			if ( !isOn ) {
				return;
			}
			
			if ( tradeCount != SystemPerformance.AllTrades.Count )
			  {
				  tradeCount = SystemPerformance.AllTrades.Count;
			      Trade lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
				  var exitName = lastTrade.Exit.Name.ToString();
				  var entryDate = lastTrade.Entry.Time.ToShortDateString();
				  //var exitDate = lastTrade.Exit.Time.ToShortDateString();
				  var cumProfit = SystemPerformance.AllTrades.TradesPerformance.NetProfit.ToString("0.0");
				  var profitFactor = SystemPerformance.AllTrades.TradesPerformance.ProfitFactor.ToString("0.00");
				  var roi = lastTrade.ProfitCurrency / cost;
				  //var MaxConsecutiveLoser = SystemPerformance.AllTrades.TradesPerformance.MaxConsecutiveLoser;
				  //var LargestLoser =  SystemPerformance.AllTrades.TradesPerformance.Currency.LargestLoser.ToString("0.0");
				  //var LargestWinner =  SystemPerformance.AllTrades.TradesPerformance.Currency.LargestWinner.ToString("0.0");
				  //var ProfitPerMonth =  SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth.ToString("0.0");
				  double winPct = (Convert.ToDouble( SystemPerformance.AllTrades.WinningTrades.Count) / Convert.ToDouble(SystemPerformance.AllTrades.TradesPerformance.TradesCount));
				  winPct = winPct * 100;
				  
				  if ( debug ) {
				      Print( tradeCount + 
					  "\t\tEntry: " + entryDate +  
					  "\t\tProfit: " + lastTrade.ProfitCurrency.ToString("0.0") + "\t\tTotal " + cumProfit + "\t\t exit Type" + exitName +
					 "\t\tWin Percent " + winPct.ToString("0.0") + "%" + "\t\tPF " + profitFactor); // +
					 // "\t\tMax LR " + MaxConsecutiveLoser + "\t\tLL " + LargestLoser + "\t\tLW " + LargestWinner + "\t\tMonthly " + ProfitPerMonth);
				  }

				  /// update data
					priceData.date 		=  	entryDate;
					priceData.ticker	=	Instrument.MasterInstrument.Name;
					priceData.profit	= 	Math.Round(lastTrade.ProfitCurrency, 2); 
				  //Print();
					priceData.winPct	= 	Math.Round(winPct, 2);
					priceData.cost 		= 	Math.Round(cost);
					priceData.roi		=	Math.Round(roi, 4);
					
				   if ( debug ) { Print(priceData.date + "\t" + priceData.ticker + "\tprofit: " + priceData.profit  + "\t\t%win: " + priceData.winPct  + "\t\tcost: " + priceData.cost + "\t\tROI: " + priceData.roi); }
				   deployFirebase(payload: priceData); // List<PriceData>
			  }
		}
		
		

		
		private void deployFirebase(PriceData payload) {
			string jsonDataset = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(payload);
			var myFirebase = "https://mtdash01.firebaseio.com/.json";
            var request = WebRequest.CreateHttp(myFirebase);
			request.Method = "POST";		// PUT writes over + POST - Pushing Data
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonDataset);
            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;
			Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
			dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
			reader.Close();
            dataStream.Close();
            response.Close();
            request.Abort();
		}
		
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									CSV Export
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		private void checkForDirectory() {

			Print("\nCheckingDirectory...");
			var dateTime = DateTime.Today.ToString("MM_dd_yyyy") ;
			/// check to see if Firebase Dir exists
			bool folderExists = Directory.Exists(systemPath+ @"\Channel"+"_"+ dateTime );
			Print("path to documents: " + systemPath + " Does Firebase folder exists? " + folderExists);
		
			/// if not create the directory
			if (!folderExists) {
				Print("creating directory... " + systemPath+ @"\Channel"+"_"+ dateTime +"\n"  );
				Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Channel"+"_"+ dateTime));
			} else {
				Print("found diretory... " + systemPath+ @"\Channel"+"_"+ dateTime +"\n" );
			}
		}
		
		private void createCSV(bool isOn, bool debug) {
			if ( !isOn ) {
				return;
			}
			
			if ( tradeCount != SystemPerformance.AllTrades.Count )
			  {
				  tradeCount = SystemPerformance.AllTrades.Count;
			      Trade lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
				  var exit = lastTrade.Exit.Name.ToString();
				  var entryDate = lastTrade.Entry.Time.ToShortDateString();
				  var exitDate = lastTrade.Exit.Time.ToShortDateString();
				  var cumProfit = SystemPerformance.AllTrades.TradesPerformance.NetProfit.ToString("0.0");
				  var profitFactor = SystemPerformance.AllTrades.TradesPerformance.ProfitFactor.ToString("0.00");
				  var MaxConsecutiveLoser = SystemPerformance.AllTrades.TradesPerformance.MaxConsecutiveLoser;
				  var LargestLoser =  SystemPerformance.AllTrades.TradesPerformance.Currency.LargestLoser.ToString("0.0");
				  var LargestWinner =  SystemPerformance.AllTrades.TradesPerformance.Currency.LargestWinner.ToString("0.0");
				  var ProfitPerMonth =  SystemPerformance.AllTrades.TradesPerformance.Currency.ProfitPerMonth.ToString("0.0");
				  double winPct = (Convert.ToDouble( SystemPerformance.AllTrades.WinningTrades.Count) / Convert.ToDouble(SystemPerformance.AllTrades.TradesPerformance.TradesCount));
				  winPct = winPct * 100;
				  
				  if ( debug ) {
				      Print( tradeCount + 
					  "\t\tEntry: " + entryDate + "\t\tExit " + exitDate + 
					  "\t\tProfit: " + lastTrade.ProfitCurrency.ToString("0.0") + "\t\tTotal " + cumProfit + "\t\t" + exit +
					 "\t\tWin Percent " + winPct.ToString("0.0")+ "%" + "\t\tPF " + profitFactor +
					  "\t\tMax LR " + MaxConsecutiveLoser + "\t\tLL " + LargestLoser + "\t\tLW " + LargestWinner + "\t\tMonthly " + ProfitPerMonth);
				  }
			  
			  
			checkForDirectory();
			var dateTime = DateTime.Today.ToString("MM_dd_yyyy");
			var filePath = systemPath+ @"\Channel"+"_"+ dateTime + @"\"+  Instrument.MasterInstrument.Name  + ".csv";
			//Print("writing file... " + filePath);


			using (StreamWriter writer = new StreamWriter(filePath, true))
			{
				/// Trade Count, Date In, Date Out, Ticker, lastProfitCurrency, cumProfit, exit name, profit factor, winPct Consecutive losers, largest loser, largest winner, profit per month
				var newLine =  tradeCount + ", " + 
					entryDate + ", " + exitDate + ", " +
					Instrument.MasterInstrument.Name + ", " + lastTrade.ProfitCurrency.ToString("0.0") + ", " + cumProfit + ", " + 
					exit + ", " + profitFactor + ", "  + winPct.ToString("0.0") + ", " +
					+ MaxConsecutiveLoser + ", " + LargestLoser + ", " + LargestWinner + ", " + ProfitPerMonth;
		
				writer.WriteLine(newLine);
	
				writer.Dispose();
			}
			}
		}
		
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									Channel Long Conditions
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected bool entryConditionsChannel()
		{
			if ( Position.MarketPosition == MarketPosition.Long ) { return false; }
			bool signal = false;		// && High[0] < SMA(10)[0] 
			if ( Close[0] > Math.Abs(SMA(200)[0])  && Close[0] < Math.Abs(SMA(10)[0]) && WilliamsR(10)[0] < -80  ){
				signal = true;
				entryBar = CurrentBar;
				theStop = calcInitialStop(pct: 3, isLong: true);
				trailStop = theStop;
				shares = calcPositionSize(stopPrice: theStop, isLong: true); 
				//Draw.ArrowUp(this, "entryArruw"+CurrentBar, true, 0, Low[0] - (TickSize * 20), Brushes.DodgerBlue);
			}
			return signal;
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									Enter Long 
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected void enterLong(bool showStop )
		{
			entryOne = entryConditionsChannel();
			if ( entryOne ) {
				
				EnterLong(Convert.ToInt32(shares), "LE 1");
				if ( showStop ) {
					Draw.Text(this, "stop"+CurrentBar, "-", 0, theStop);
					Draw.ArrowUp(this, "entryArruw"+CurrentBar, true, 0, Low[0] - (TickSize * 20), Brushes.DodgerBlue);
				}
			}
			if ( showStop ) {
				/// show stop
				if ( Position.MarketPosition == MarketPosition.Long ) {
					Draw.Text(this, "stop"+CurrentBar, "-", 0, theStop);
				}
			}
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									Time Exit 
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected void exitAfter(int days )
		{
			
			if (Position.MarketPosition == MarketPosition.Flat ) { return; }
			if ( (  CurrentBar - entryBar ) > days) {
				ExitLong("Time1", "LE 1");
			}
			
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									Hard stop 
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected void exitHardStop()
		{
			if (Position.MarketPosition == MarketPosition.Flat ) { return; }
			if (Low[0] <= theStop) {
				ExitLong("Stop", "LE 1");
			}
			
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									%R Exit 
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected void percentRexit()
		{
			if ( Position.MarketPosition == MarketPosition.Long && WilliamsR(10)[0] > -30 ) {
				ExitLong("Pct(R)", "LE 1");
			}
			
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									Target 2 
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected void extendedTarget()
		{
			if ( Position.MarketPosition == MarketPosition.Long && Math.Abs(High[0]) >  Math.Abs(MAX(SMA(10), 20)[0]) ) {
				ExitLong("LX T2", "LE 1");
			}
			
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									Trail stop 
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected void setTrailStop()
		{
			if (Position.MarketPosition == MarketPosition.Flat ) { return; }
			if ( Position.MarketPosition == MarketPosition.Long ) {
				if (Low[0] > Low[1]) {
					double newStop =  Low[0] - stopDistance;
					if (newStop > trailStop) {
						trailStop = newStop;
					}
				}
				if ( ShowText ) {
					Draw.TriangleUp(this,"trailStop"+CurrentBar.ToString(), false, 0, trailStop, Brushes.DimGray);
				}
				if (Low[0] <= trailStop) {
					ExitLong("Trail", "LE 1");
				}
			}
			
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									POSTION SIZE
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
	
		protected int calcPositionSize(double stopPrice, bool isLong) {
			if (isLong) {
				sharesFraction = MaxRisk / ( Close[0] -stopPrice );
			} else {
				sharesFraction = MaxRisk / ( stopPrice - Close[0] );
			}
			//Print(sharesFraction);
			cost = Close[0] *  (int)sharesFraction;
			return (int)sharesFraction;
		}
		/// ////////////////////////////////////////////////////////////////////////////////////////////////
		/// 	
		/// 									STOP PRICE
		/// 
		/// ////////////////////////////////////////////////////////////////////////////////////////////////		
		protected double calcInitialStop(int pct, bool isLong) {
			double result;
			/// set stop 3% if Nasdaq 5%
			foreach(Exchange exchange in Bars.Instrument.MasterInstrument.Exchanges)
				{
					if (exchange.ToString() != "Default") {
						//Print(exchange); // Default, Nasdaq, NYSE
						if (exchange.ToString() == "Nasdaq") {
							//Print("\tListed on Nasdaq, Change stiop to 5%");
							pct = 5;
						}						
					}
				} 
			double convertedPct = pct * 0.01;

			if (isLong) {
				stopDistance =  Close[0] * convertedPct;
				result = Close[0] - stopDistance;
				trailStop = result;
				entryBarnum = CurrentBar;
			} else {
				result = Close[0] + ( Close[0] * convertedPct);
			}
			
			return result; 
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MaxRisk", Order=1, GroupName="Parameters")]
		public int MaxRisk
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ExitAfter", Order=2, GroupName="Parameters")]
		public int ExitAfter
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Push To Firebase", Description="is this thing on?", Order=1, GroupName="Parameters")]
		public bool ShowText
		{ get; set; }
		#endregion

	}
}
