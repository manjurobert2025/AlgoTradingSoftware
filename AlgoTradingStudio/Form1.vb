Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Linq
Imports System.IO
Imports System.Globalization
Imports System.Net.Http
Imports System.Text.Json
Imports System.Diagnostics
Imports System.Threading.Tasks

Public Class Form1

    ' =========================================================
    ' TRADE VARIABLES
    ' =========================================================

    Private tradeActive As Boolean = False
    Private ctcActivated As Boolean = False

    Private entryPrice As Double
    Private stopLoss As Double
    Private currentStopLoss As Double
    Private risk As Double
    Private ctcPrice As Double
    Private targetPrice As Double
    Private currentPrice As Double

    ' =========================================================
    ' LAST TRADE RESULT / QUANTITY
    ' =========================================================

    Private tradeQuantity As Integer = 0
    Private lastExitReason As String = ""
    Private lastExitPrice As Double = 0D
    Private lastTradeProfit As Double = 0D
    Private lastTradeProfitR As Double = 0D

    ' =========================================================
    ' BACKTEST VARIABLES
    ' =========================================================

    Private backtestTotalTrades As Integer = 0
    Private backtestWins As Integer = 0
    Private backtestLosses As Integer = 0
    Private backtestCTC As Integer = 0
    Private backtestTotalR As Double = 0

    ' =========================================================
    ' CSV
    ' =========================================================

    Private loadedCandles As New List(Of Candle)

    Private csvLoaded As Boolean = False
    Private csvFileName As String = ""

    ' =========================================================
    ' CONSOLIDATION
    ' =========================================================

    Private Const CONSOLIDATION_CANDLES As Integer = 3

    Private Const CONSOLIDATION_RANGE_PERCENT As Double = 1D

    ' =========================================================
    ' NIFTY MARKET FILTER
    ' =========================================================

    Private Const FAST_EMA_PERIOD As Integer = 9

    Private Const SLOW_EMA_PERIOD As Integer = 21

    ' =========================================================
    ' BREAKOUT / POSITION SIZING
    ' =========================================================

    Private Const BREAKOUT_VOLUME_LOOKBACK As Integer = 3
    Private Const CAPITAL As Double = 50000D
    Private Const MAX_RISK_PERCENT As Double = 2D
    Private Const MIN_EXPECTED_PROFIT As Double = 500D

    ' =========================================================
    ' UI
    ' =========================================================

    Private lblNifty As Label
    Private lblStock As Label
    Private lblSignal As Label

    Private lblEntry As Label
    Private lblSL As Label
    Private lblCTC As Label
    Private lblTarget As Label
    Private lblQuantity As Label
    Private lblPositionValue As Label
    Private lblRiskAmount As Label

    Private lblTradeStatus As Label
    Private lblCSVStatus As Label

    Private btnStart As Button
    Private btnStop As Button
    Private btnSimulate As Button
    Private btnBacktest As Button
    Private btnLoadCSV As Button
    Private chkTestMode As CheckBox

    Private txtLog As TextBox

    ' =========================================================
    ' UPSTOX
    ' =========================================================

    ' SECURITY: replace these placeholders with newly rotated credentials.
    ' Do not commit real API credentials to source control or share them in chat.
    Private Const UPSTOX_API_KEY As String =
        "b47da02a-4c8b-443a-a74a-1e1edb75c9cf"

    Private Const UPSTOX_API_SECRET As String =
        "5sg7glw5e0"

    Private Const UPSTOX_REDIRECT_URI As String =
        "https://portfolio.manjuecomstore.in/upstox-callback/"

    Private upstoxAccessToken As String = ""
    Private upstoxState As String = ""

    Private txtUpstoxCode As TextBox
    Private btnConnectUpstox As Button
    Private btnExchangeUpstoxCode As Button
    Private btnTestUpstox As Button
    Private lblUpstoxStatus As Label

    ' =========================================================
    ' LIVE MARKET DATA
    ' =========================================================

    Private Const NIFTY_INSTRUMENT_KEY As String =
        "NSE_INDEX|Nifty 50"

    Private txtLiveInstrumentKey As TextBox
    Private btnGetLivePrices As Button
    Private lblNiftyLive As Label
    Private lblStockLive As Label

    ' =========================================================
    ' 5-MINUTE CHART
    ' =========================================================

    Private btnLoadNiftyChart As Button
    Private pnlNiftyChart As Panel
    Private lblChartStatus As Label
    Private chartCandles As New List(Of ChartCandle)
    Private chartEma9 As New List(Of Double)
    Private chartEma21 As New List(Of Double)
    Private chartVwap As New List(Of Double)

    ' =========================================================
    ' LIVE BULLISH STOCK SCANNER
    ' =========================================================

    Private btnScanBullish As Button
    Private btnStartLiveScanner As Button
    Private btnStopLiveScanner As Button
    Private txtScanSymbols As TextBox
    Private dgvBullishStocks As DataGridView
    Private lblScannerStatus As Label

    ' =========================================================
    ' NIFTY OPTIONS SIGNAL (SIGNAL ONLY; NO ORDER PLACEMENT)
    ' =========================================================

    Private btnNiftyOptionSignal As Button
    Private lastNiftyOptionSignalTime As String = ""

    ' Continuous live-scanner control. The strategy is based on 5-minute candles,
    ' so the application checks frequently but performs the full scan only when
    ' a new 5-minute candle bucket is reached. This avoids repeatedly processing
    ' the same candle and keeps the scanner responsive.
    Private liveScannerTimer As Timer
    Private liveScannerRunning As Boolean = False
    Private scannerScanInProgress As Boolean = False
    Private lastScannerBucket As DateTime = DateTime.MinValue
    Private ReadOnly scanInstrumentCache As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly scanLeverageCache As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)

    Private Const DEFAULT_SCAN_SYMBOLS As String =
        "RELIANCE,HDFCBANK,ICICIBANK,SBIN,AXISBANK,KOTAKBANK,INFY,TCS,HCLTECH,WIPRO,LT,BHARTIARTL,ITC,M&M,MARUTI,TITAN,SUNPHARMA,NTPC,POWERGRID,BEL,GRMOVER,NBCC,NCC,MTARTECH,HUDCO,NMDC,GMRAIRPORT,SUZLON,NLCINDIA,REDINGTON,JINDALSAW,ELECTCAST"

    ' =========================================================
    ' FORM LOAD
    ' =========================================================

    Private Sub Form1_Load(
        sender As Object,
        e As EventArgs) Handles MyBase.Load

        Me.Text = "ALGO TRADING STUDIO"

        Me.Size =
            New Size(1200, 830)

        Me.StartPosition =
            FormStartPosition.CenterScreen

        ' Enable a vertical scrollbar for the complete dashboard.
        ' This prevents lower controls from becoming inaccessible on smaller screens.
        Me.AutoScroll = True
        Me.AutoScrollMinSize = New Size(1160, 1580)

        BuildInterface()

    End Sub

    ' =========================================================
    ' BUILD INTERFACE
    ' =========================================================

    Private Sub BuildInterface()

        ' -----------------------------------------------------
        ' TITLE
        ' -----------------------------------------------------

        Dim title As New Label()

        title.Text =
            "ALGO TRADING STUDIO"

        title.Font =
            New Font(
                "Arial",
                20,
                FontStyle.Bold)

        title.Location =
            New Point(10, 10)

        title.AutoSize = True

        Me.Controls.Add(title)

        ' -----------------------------------------------------
        ' NIFTY
        ' -----------------------------------------------------

        lblNifty = New Label()

        lblNifty.Text =
            "Nifty: WAITING"

        lblNifty.Font =
            New Font(
                "Arial",
                12,
                FontStyle.Bold)

        lblNifty.Location =
            New Point(10, 65)

        lblNifty.AutoSize = True

        Me.Controls.Add(lblNifty)

        ' -----------------------------------------------------
        ' STOCK
        ' -----------------------------------------------------

        lblStock = New Label()

        lblStock.Text =
            "Stock: ---"

        lblStock.Font =
            New Font(
                "Arial",
                12,
                FontStyle.Bold)

        lblStock.Location =
            New Point(380, 65)

        lblStock.AutoSize = True

        Me.Controls.Add(lblStock)

        ' -----------------------------------------------------
        ' SIGNAL
        ' -----------------------------------------------------

        lblSignal = New Label()

        lblSignal.Text =
            "Signal: WAITING"

        lblSignal.Font =
            New Font(
                "Arial",
                14,
                FontStyle.Bold)

        lblSignal.Location =
            New Point(10, 115)

        lblSignal.AutoSize = True

        Me.Controls.Add(lblSignal)

        ' -----------------------------------------------------
        ' ENTRY
        ' -----------------------------------------------------

        lblEntry = New Label()

        lblEntry.Text =
            "Entry: ---"

        lblEntry.Font =
            New Font(
                "Arial",
                11)

        lblEntry.Location =
            New Point(10, 170)

        lblEntry.AutoSize = True

        Me.Controls.Add(lblEntry)

        ' -----------------------------------------------------
        ' STOP LOSS
        ' -----------------------------------------------------

        lblSL = New Label()

        lblSL.Text =
            "Stop Loss: ---"

        lblSL.Font =
            New Font(
                "Arial",
                11)

        lblSL.Location =
            New Point(380, 170)

        lblSL.AutoSize = True

        Me.Controls.Add(lblSL)

        ' -----------------------------------------------------
        ' CTC
        ' -----------------------------------------------------

        lblCTC = New Label()

        lblCTC.Text =
            "Cost-to-Cost: ---"

        lblCTC.Font =
            New Font(
                "Arial",
                11)

        lblCTC.Location =
            New Point(10, 215)

        lblCTC.AutoSize = True

        Me.Controls.Add(lblCTC)

        ' -----------------------------------------------------
        ' TARGET
        ' -----------------------------------------------------

        lblTarget = New Label()

        lblTarget.Text =
            "Target: 1.5R / 1:1 ---"

        lblTarget.Font =
            New Font(
                "Arial",
                11)

        lblTarget.Location =
            New Point(380, 215)

        lblTarget.AutoSize = True

        Me.Controls.Add(lblTarget)

        ' -----------------------------------------------------
        ' QUANTITY
        ' -----------------------------------------------------

        lblQuantity = New Label()

        lblQuantity.Text =
            "Quantity: ---"

        lblQuantity.Font =
            New Font(
                "Arial",
                11)

        lblQuantity.Location =
            New Point(10, 300)

        lblQuantity.AutoSize = True

        Me.Controls.Add(lblQuantity)

        ' -----------------------------------------------------
        ' POSITION VALUE
        ' -----------------------------------------------------

        lblPositionValue = New Label()

        lblPositionValue.Text =
            "Position Value: ---"

        lblPositionValue.Font =
            New Font(
                "Arial",
                11)

        lblPositionValue.Location =
            New Point(380, 300)

        lblPositionValue.AutoSize = True

        Me.Controls.Add(lblPositionValue)

        ' -----------------------------------------------------
        ' RISK AMOUNT
        ' -----------------------------------------------------

        lblRiskAmount = New Label()

        lblRiskAmount.Text =
            "Risk Amount: ---"

        lblRiskAmount.Font =
            New Font(
                "Arial",
                11)

        lblRiskAmount.Location =
            New Point(10, 330)

        lblRiskAmount.AutoSize = True

        Me.Controls.Add(lblRiskAmount)

        ' -----------------------------------------------------
        ' TRADE STATUS
        ' -----------------------------------------------------

        lblTradeStatus = New Label()

        lblTradeStatus.Text =
            "Trade Status: WAITING"

        lblTradeStatus.Font =
            New Font(
                "Arial",
                11,
                FontStyle.Bold)

        lblTradeStatus.Location =
            New Point(10, 260)

        lblTradeStatus.AutoSize = True

        Me.Controls.Add(lblTradeStatus)

        ' -----------------------------------------------------
        ' CSV STATUS
        ' -----------------------------------------------------

        lblCSVStatus = New Label()

        lblCSVStatus.Text =
            "CSV: Not Loaded"

        lblCSVStatus.Font =
            New Font(
                "Arial",
                10,
                FontStyle.Bold)

        lblCSVStatus.Location =
            New Point(380, 260)

        lblCSVStatus.AutoSize = True

        Me.Controls.Add(lblCSVStatus)

        ' -----------------------------------------------------
        ' START
        ' -----------------------------------------------------

        btnStart = New Button()

        btnStart.Text =
            "Start Scan"

        btnStart.Location =
            New Point(7, 365)

        btnStart.Size =
            New Size(125, 38)

        AddHandler btnStart.Click,
            AddressOf btnStart_Click

        Me.Controls.Add(btnStart)

        ' -----------------------------------------------------
        ' STOP
        ' -----------------------------------------------------

        btnStop = New Button()

        btnStop.Text =
            "Stop Scan"

        btnStop.Location =
            New Point(145, 365)

        btnStop.Size =
            New Size(125, 38)

        AddHandler btnStop.Click,
            AddressOf btnStop_Click

        Me.Controls.Add(btnStop)

        ' -----------------------------------------------------
        ' NEXT PRICE
        ' -----------------------------------------------------

        btnSimulate = New Button()

        btnSimulate.Text =
            "Next Price"

        btnSimulate.Location =
            New Point(283, 365)

        btnSimulate.Size =
            New Size(125, 38)

        AddHandler btnSimulate.Click,
            AddressOf btnSimulate_Click

        Me.Controls.Add(btnSimulate)

        ' -----------------------------------------------------
        ' BACKTEST
        ' -----------------------------------------------------

        btnBacktest = New Button()

        btnBacktest.Text =
            "Run Backtest"

        btnBacktest.Location =
            New Point(421, 365)

        btnBacktest.Size =
            New Size(135, 38)

        AddHandler btnBacktest.Click,
            AddressOf btnBacktest_Click

        Me.Controls.Add(btnBacktest)

        ' -----------------------------------------------------
        ' LOAD CSV
        ' -----------------------------------------------------

        btnLoadCSV = New Button()

        btnLoadCSV.Text =
            "Load CSV"

        btnLoadCSV.Location =
            New Point(569, 365)

        btnLoadCSV.Size =
            New Size(135, 38)

        AddHandler btnLoadCSV.Click,
            AddressOf btnLoadCSV_Click

        Me.Controls.Add(btnLoadCSV)

        ' -----------------------------------------------------
        ' TEST MODE
        ' -----------------------------------------------------

        chkTestMode = New CheckBox()

        chkTestMode.Text =
            "TEST MODE (Skip NIFTY filter)"

        chkTestMode.Location =
            New Point(10, 410)

        chkTestMode.Size =
            New Size(250, 25)

        chkTestMode.Checked = True

        Me.Controls.Add(chkTestMode)

        ' -----------------------------------------------------
        ' UPSTOX CONNECT
        ' -----------------------------------------------------

        btnConnectUpstox = New Button()

        btnConnectUpstox.Text =
            "Connect Upstox"

        btnConnectUpstox.Location =
            New Point(10, 455)

        btnConnectUpstox.Size =
            New Size(170, 38)

        AddHandler btnConnectUpstox.Click,
            AddressOf btnConnectUpstox_Click

        Me.Controls.Add(btnConnectUpstox)

        ' -----------------------------------------------------
        ' TEST UPSTOX API
        ' -----------------------------------------------------

        btnTestUpstox = New Button()

        btnTestUpstox.Text =
            "Test Upstox API"

        btnTestUpstox.Location =
            New Point(195, 455)

        btnTestUpstox.Size =
            New Size(150, 38)

        AddHandler btnTestUpstox.Click,
            AddressOf btnTestUpstox_Click

        Me.Controls.Add(btnTestUpstox)

        ' -----------------------------------------------------
        ' UPSTOX STATUS
        ' -----------------------------------------------------

        lblUpstoxStatus = New Label()

        lblUpstoxStatus.Text =
            "Upstox: NOT CONNECTED"

        lblUpstoxStatus.Font =
            New Font(
                "Arial",
                10,
                FontStyle.Bold)

        lblUpstoxStatus.Location =
            New Point(370, 465)

        lblUpstoxStatus.AutoSize = True

        Me.Controls.Add(lblUpstoxStatus)

        ' -----------------------------------------------------
        ' UPSTOX AUTHORIZATION CODE
        ' -----------------------------------------------------

        Dim lblUpstoxCode As New Label()

        lblUpstoxCode.Text =
            "Authorization Code:"

        lblUpstoxCode.Location =
            New Point(10, 505)

        lblUpstoxCode.AutoSize = True

        Me.Controls.Add(lblUpstoxCode)

        txtUpstoxCode = New TextBox()

        txtUpstoxCode.Location =
            New Point(140, 500)

        txtUpstoxCode.Size =
            New Size(400, 25)

        Me.Controls.Add(txtUpstoxCode)

        btnExchangeUpstoxCode = New Button()

        btnExchangeUpstoxCode.Text =
            "Exchange Code"

        btnExchangeUpstoxCode.Location =
            New Point(550, 497)

        btnExchangeUpstoxCode.Size =
            New Size(150, 32)

        AddHandler btnExchangeUpstoxCode.Click,
            AddressOf btnExchangeUpstoxCode_Click

        Me.Controls.Add(btnExchangeUpstoxCode)

        ' -----------------------------------------------------
        ' LIVE MARKET DATA
        ' -----------------------------------------------------

        Dim lblLiveMarket As New Label()

        lblLiveMarket.Text =
            "LIVE MARKET DATA (snapshot):"

        lblLiveMarket.Font =
            New Font("Arial", 10, FontStyle.Bold)

        lblLiveMarket.Location =
            New Point(10, 545)

        lblLiveMarket.AutoSize = True

        Me.Controls.Add(lblLiveMarket)

        Dim lblLiveNote As New Label()

        lblLiveNote.Text =
            "Click Get Live Prices after Upstox is connected."

        lblLiveNote.Location =
            New Point(190, 543)

        lblLiveNote.AutoSize = True

        Me.Controls.Add(lblLiveNote)

        Dim lblInstrumentKey As New Label()

        lblInstrumentKey.Text =
            "Stock Instrument Key:"

        lblInstrumentKey.Location =
            New Point(10, 575)

        lblInstrumentKey.AutoSize = True

        Me.Controls.Add(lblInstrumentKey)

        txtLiveInstrumentKey = New TextBox()

        txtLiveInstrumentKey.Location =
            New Point(145, 571)

        txtLiveInstrumentKey.Size =
            New Size(320, 25)

        txtLiveInstrumentKey.Text =
            "NSE_EQ|INE002A01018"

        Me.Controls.Add(txtLiveInstrumentKey)

        btnGetLivePrices = New Button()

        btnGetLivePrices.Text =
            "Get Live Prices"

        btnGetLivePrices.Location =
            New Point(480, 568)

        btnGetLivePrices.Size =
            New Size(180, 32)

        AddHandler btnGetLivePrices.Click,
            AddressOf btnGetLivePrices_Click

        Me.Controls.Add(btnGetLivePrices)

        lblNiftyLive = New Label()

        lblNiftyLive.Text =
            "NIFTY 50: ---"

        lblNiftyLive.Font =
            New Font("Arial", 10, FontStyle.Bold)

        lblNiftyLive.Location =
            New Point(10, 610)

        lblNiftyLive.AutoSize = True

        Me.Controls.Add(lblNiftyLive)

        lblStockLive = New Label()

        lblStockLive.Text =
            "Stock LTP: ---"

        lblStockLive.Font =
            New Font("Arial", 10, FontStyle.Bold)

        lblStockLive.Location =
            New Point(300, 610)

        lblStockLive.AutoSize = True

        Me.Controls.Add(lblStockLive)

        ' -----------------------------------------------------
        ' BULLISH STOCK SCANNER
        ' -----------------------------------------------------

        Dim lblScannerTitle As New Label()
        lblScannerTitle.Text = "BUY / SELL STOCK SCANNER - 5 MINUTE"
        lblScannerTitle.Font = New Font("Arial", 11, FontStyle.Bold)
        lblScannerTitle.Location = New Point(10, 645)
        lblScannerTitle.AutoSize = True
        Me.Controls.Add(lblScannerTitle)

        Dim lblScannerHelp As New Label()
        lblScannerHelp.Text = "Live Price = current Upstox LTP | 5M Close = strategy price | BUY = bullish setup | SELL = bearish setup | Risk capped at ₹1,000 | Expected Profit ≥ ₹500."
        lblScannerHelp.Location = New Point(10, 670)
        lblScannerHelp.AutoSize = True
        Me.Controls.Add(lblScannerHelp)

        Dim lblScanSymbols As New Label()
        lblScanSymbols.Text = "Stocks:"
        lblScanSymbols.Location = New Point(10, 700)
        lblScanSymbols.AutoSize = True
        Me.Controls.Add(lblScanSymbols)

        txtScanSymbols = New TextBox()
        txtScanSymbols.Location = New Point(70, 697)
        txtScanSymbols.Size = New Size(520, 25)
        txtScanSymbols.Text = DEFAULT_SCAN_SYMBOLS
        Me.Controls.Add(txtScanSymbols)

        btnScanBullish = New Button()
        btnScanBullish.Text = "Scan Now"
        btnScanBullish.Location = New Point(600, 695)
        btnScanBullish.Size = New Size(120, 32)
        AddHandler btnScanBullish.Click, AddressOf btnScanBullish_Click
        Me.Controls.Add(btnScanBullish)

        btnStartLiveScanner = New Button()
        btnStartLiveScanner.Text = "Start Live Scanner"
        btnStartLiveScanner.Location = New Point(430, 735)
        btnStartLiveScanner.Size = New Size(140, 32)
        AddHandler btnStartLiveScanner.Click, AddressOf btnStartLiveScanner_Click
        Me.Controls.Add(btnStartLiveScanner)

        btnStopLiveScanner = New Button()
        btnStopLiveScanner.Text = "Stop Live Scanner"
        btnStopLiveScanner.Location = New Point(580, 735)
        btnStopLiveScanner.Size = New Size(140, 32)
        btnStopLiveScanner.Enabled = False
        AddHandler btnStopLiveScanner.Click, AddressOf btnStopLiveScanner_Click
        Me.Controls.Add(btnStopLiveScanner)

        lblScannerStatus = New Label()
        lblScannerStatus.Text = "Scanner: Ready - click Start Live Scanner"
        lblScannerStatus.Location = New Point(10, 775)
        lblScannerStatus.AutoSize = True
        Me.Controls.Add(lblScannerStatus)

        dgvBullishStocks = New DataGridView()
        dgvBullishStocks.Location = New Point(7, 800)
        dgvBullishStocks.Size = New Size(1160, 360)
        dgvBullishStocks.AllowUserToAddRows = False
        dgvBullishStocks.AllowUserToDeleteRows = False
        dgvBullishStocks.ReadOnly = True
        dgvBullishStocks.RowHeadersVisible = False
        dgvBullishStocks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgvBullishStocks.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvBullishStocks.ScrollBars = ScrollBars.Both

        dgvBullishStocks.Columns.Add("Stock", "Stock")
        dgvBullishStocks.Columns.Add("Status", "Status")
        dgvBullishStocks.Columns.Add("LivePrice", "Live Price")
        dgvBullishStocks.Columns.Add("Price", "5M Close")
        dgvBullishStocks.Columns.Add("Entry", "Entry")
        dgvBullishStocks.Columns.Add("SL", "Stop Loss")
        dgvBullishStocks.Columns.Add("Target", "Target")
        dgvBullishStocks.Columns.Add("Qty", "Qty")
        dgvBullishStocks.Columns.Add("Leverage", "Leverage")
        dgvBullishStocks.Columns.Add("Risk", "Risk")
        dgvBullishStocks.Columns.Add("ExpectedProfit", "Expected Profit")

        dgvBullishStocks.Columns("Stock").Width = 90
        dgvBullishStocks.Columns("Status").Width = 90
        dgvBullishStocks.Columns("LivePrice").Width = 80
        dgvBullishStocks.Columns("Price").Width = 75
        dgvBullishStocks.Columns("Entry").Width = 75
        dgvBullishStocks.Columns("SL").Width = 75
        dgvBullishStocks.Columns("Target").Width = 85
        dgvBullishStocks.Columns("Qty").Width = 55
        dgvBullishStocks.Columns("Leverage").Width = 70
        dgvBullishStocks.Columns("Risk").Width = 75
        dgvBullishStocks.Columns("ExpectedProfit").Width = 105

        Me.Controls.Add(dgvBullishStocks)
        ' -----------------------------------------------------
        ' LOG LABEL
        ' -----------------------------------------------------

        Dim lblLog2 As New Label()

        lblLog2.Text =
            "Activity Log"

        lblLog2.Font =
            New Font(
                "Arial",
                11,
                FontStyle.Bold)

        lblLog2.Location =
            New Point(10, 1140)

        lblLog2.AutoSize = True

        Me.Controls.Add(lblLog2)

        ' -----------------------------------------------------
        ' LOG
        ' -----------------------------------------------------

        txtLog = New TextBox()

        txtLog.Multiline = True

        txtLog.ScrollBars =
            ScrollBars.Vertical

        txtLog.Location =
            New Point(7, 1205)

        txtLog.Size =
            New Size(720, 230)

        txtLog.ReadOnly = True

        Me.Controls.Add(txtLog)

        ' -----------------------------------------------------
        ' NIFTY OPTIONS SIGNAL CONTROLS
        ' -----------------------------------------------------

        ' No futures key is required. The strategy reads NIFTY 50 index candles
        ' and selects a CE or PE contract from the Upstox option chain.
        btnNiftyOptionSignal = New Button()
        btnNiftyOptionSignal.Text = "Get NIFTY Options Signal (CE / PE)"
        btnNiftyOptionSignal.Location = New Point(10, 1454)
        btnNiftyOptionSignal.Size = New Size(280, 36)
        AddHandler btnNiftyOptionSignal.Click, AddressOf btnNiftyOptionSignal_Click
        Me.Controls.Add(btnNiftyOptionSignal)

    End Sub


    ' =========================================================
    ' LIVE BULLISH STOCK SCANNER
    ' =========================================================

    Private Class LiveScanResult
        Public Property Symbol As String
        Public Property Status As String
        Public Property Price As Double
        Public Property LivePrice As Double
        Public Property EMA9 As Double
        Public Property EMA21 As Double
        Public Property VWAP As Double
        Public Property Entry As Double
        Public Property StopLoss As Double
        Public Property Target As Double
        Public Property Quantity As Integer
        Public Property Leverage As Double
        Public Property RiskAmount As Double
        Public Property ExpectedProfit As Double
        Public Property Conditions As String
        Public Property Direction As String
    End Class

    Private Class ScannerCandleSet
        Public Property AllCandles As List(Of Candle)
        Public Property TodayCandles As List(Of Candle)
    End Class

    Private Async Sub btnScanBullish_Click(
        sender As Object,
        e As EventArgs)

        Await RunBullishScanAsync(False)

    End Sub

    Private Async Sub btnStartLiveScanner_Click(
        sender As Object,
        e As EventArgs)

        If String.IsNullOrWhiteSpace(upstoxAccessToken) Then
            MessageBox.Show("Please connect to Upstox first.", "Live Scanner", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If liveScannerRunning Then Return

        liveScannerRunning = True
        lastScannerBucket = DateTime.MinValue
        btnStartLiveScanner.Enabled = False
        btnStopLiveScanner.Enabled = True
        btnScanBullish.Enabled = False

        If liveScannerTimer Is Nothing Then
            liveScannerTimer = New Timer()
            liveScannerTimer.Interval = 30000
            AddHandler liveScannerTimer.Tick, AddressOf liveScannerTimer_Tick
        End If

        liveScannerTimer.Start()
        lblScannerStatus.Text = "Scanner: LIVE - waiting for next 5-minute candle (BUY / SELL)..."
        LogMessage("LIVE SCANNER STARTED - automatic 5-minute strategy scans enabled.")

        ' Run immediately so the user gets an initial result without waiting.
        Await RunBullishScanAsync(True)

        If liveScannerRunning Then
            Dim nowIndia As DateTime = GetIndiaTime()
            Dim bucketMinute As Integer = (nowIndia.Minute \ 5) * 5
            lastScannerBucket = New DateTime(nowIndia.Year, nowIndia.Month, nowIndia.Day, nowIndia.Hour, bucketMinute, 0)
            lblScannerStatus.Text = "Scanner: LIVE - monitoring 5-minute candles | Last scan: " & nowIndia.ToString("HH:mm:ss")
        End If

    End Sub

    Private Sub btnStopLiveScanner_Click(
        sender As Object,
        e As EventArgs)

        StopLiveScanner()

    End Sub

    Private Async Sub liveScannerTimer_Tick(
        sender As Object,
        e As EventArgs)

        If Not liveScannerRunning OrElse scannerScanInProgress Then Return
        If String.IsNullOrWhiteSpace(upstoxAccessToken) Then
            StopLiveScanner()
            Return
        End If

        If Not IsNseMarketOpen() Then
            lblScannerStatus.Text = "Scanner: LIVE - market closed / waiting for NSE open"
            Return
        End If

        Dim nowIndia As DateTime = GetIndiaTime()
        Dim bucketMinute As Integer = (nowIndia.Minute \ 5) * 5
        Dim currentBucket As New DateTime(nowIndia.Year, nowIndia.Month, nowIndia.Day, nowIndia.Hour, bucketMinute, 0)

        If currentBucket <= lastScannerBucket Then Return

        Await RunBullishScanAsync(True)
        If liveScannerRunning Then lastScannerBucket = currentBucket

    End Sub

    Private Sub StopLiveScanner()

        liveScannerRunning = False

        If liveScannerTimer IsNot Nothing Then
            liveScannerTimer.Stop()
        End If

        If btnStartLiveScanner IsNot Nothing Then btnStartLiveScanner.Enabled = True
        If btnStopLiveScanner IsNot Nothing Then btnStopLiveScanner.Enabled = False
        If btnScanBullish IsNot Nothing Then btnScanBullish.Enabled = Not scannerScanInProgress

        If lblScannerStatus IsNot Nothing Then
            lblScannerStatus.Text = "Scanner: Stopped"
        End If

        LogMessage("LIVE SCANNER STOPPED.")

    End Sub

    Private Function GetIndiaTime() As DateTime
        Try
            Dim istZone As TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
            Return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, istZone).DateTime
        Catch
            Return DateTime.Now
        End Try
    End Function

    Private Async Function RunBullishScanAsync(
        automaticScan As Boolean) As Task

        If scannerScanInProgress Then Return

        scannerScanInProgress = True

        If Not automaticScan Then btnScanBullish.Enabled = False

        If String.IsNullOrWhiteSpace(upstoxAccessToken) Then
            If Not automaticScan Then
                MessageBox.Show("Please connect to Upstox first.", "Bullish Scanner", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
            scannerScanInProgress = False
            If Not automaticScan Then btnScanBullish.Enabled = True
            Return
        End If

        Dim symbols As New List(Of String)()
        For Each part As String In txtScanSymbols.Text.Split(","c)
            Dim symbol As String = part.Trim().ToUpperInvariant()
            If symbol <> "" AndAlso Not symbols.Contains(symbol) Then symbols.Add(symbol)
        Next

        If symbols.Count = 0 Then
            If Not automaticScan Then
                MessageBox.Show("Enter at least one NSE stock symbol.", "Bullish Scanner", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
            scannerScanInProgress = False
            If Not automaticScan Then btnScanBullish.Enabled = True
            Return
        End If

        Try
            ' Replace the previous scan instead of appending duplicate rows.
            dgvBullishStocks.Rows.Clear()

            LogMessage("")
            LogMessage("================================")
            LogMessage("LIVE BUY / SELL STOCK SCANNER")
            LogMessage("================================")
            LogMessage("Capital: ₹" & CAPITAL.ToString("0.00") & " | Max Risk: ₹" & (CAPITAL * MAX_RISK_PERCENT / 100D).ToString("0.00") & " | Minimum Expected Profit: ₹" & MIN_EXPECTED_PROFIT.ToString("0.00"))

            If Not IsNseMarketOpen() Then
                lblScannerStatus.Text = "Scanner: WAITING - NSE market opens at 9:15 AM"
                lblNifty.Text = "Nifty: MARKET WAITING"
                lblSignal.Text = "Signal: WAITING"

                dgvBullishStocks.Rows.Add(
                    "---",
                    "WAITING",
                    "---",
                    "---",
                    "---",
                    "---",
                    "---",
                    "---",
                    "---",
                    "---",
                    "---")

                LogMessage("MARKET STATUS: CLOSED / WAITING FOR 9:15 AM")
                LogMessage("No BUY or SELL candidates are evaluated before the market opens.")
                Return
            End If

            lblScannerStatus.Text = "Scanner: MARKET OPEN - reading 5-minute data..."
            LogMessage("MARKET STATUS: OPEN")

            Dim niftySet As ScannerCandleSet =
                Await GetScannerCandleSetAsync(NIFTY_INSTRUMENT_KEY, "NIFTY 50")

            If niftySet Is Nothing OrElse niftySet.AllCandles.Count < SLOW_EMA_PERIOD Then
                lblScannerStatus.Text = "Scanner: Waiting for sufficient NIFTY data"
                LogMessage("NIFTY: insufficient historical + current 5M candles for EMA 21.")
                Return
            End If

            Dim niftyAll As List(Of Candle) = SortCandles(niftySet.AllCandles)
            Dim niftyToday As List(Of Candle) = SortCandles(niftySet.TodayCandles)
            Dim niftyPrice As Double = niftyAll(niftyAll.Count - 1).Close
            Dim niftyEMA9 As Double = CalculateEMA(niftyAll, FAST_EMA_PERIOD)
            Dim niftyEMA21 As Double = CalculateEMA(niftyAll, SLOW_EMA_PERIOD)

            ' NIFTY 50 is an index. Its candle volume can be zero, so do NOT block
            ' the market filter because an index VWAP is unavailable. Stock VWAP is
            ' still required below because NSE equity candles have traded volume.
            Dim niftyVWAP As Double = CalculateVWAP(niftyToday)
            Dim niftyBullish As Boolean =
                niftyPrice > niftyEMA9 AndAlso
                niftyEMA9 > niftyEMA21

            Dim niftyBearish As Boolean =
                niftyPrice < niftyEMA9 AndAlso
                niftyEMA9 < niftyEMA21

            Dim marketDirection As String = "NEUTRAL"
            If niftyBullish Then
                marketDirection = "BUY"
            ElseIf niftyBearish Then
                marketDirection = "SELL"
            End If

            lblNifty.Text =
                "Nifty: " & marketDirection

            LogMessage("NIFTY PRICE: " & niftyPrice.ToString("0.00"))
            LogMessage("NIFTY EMA 9: " & niftyEMA9.ToString("0.00"))
            LogMessage("NIFTY EMA 21: " & niftyEMA21.ToString("0.00"))
            LogMessage("NIFTY VWAP: " & If(niftyVWAP > 0, niftyVWAP.ToString("0.00"), "N/A (index volume not available)"))
            LogMessage("NIFTY FILTER: " & marketDirection)

            If marketDirection = "NEUTRAL" Then
                lblScannerStatus.Text = "Scanner: NIFTY neutral - BUY and SELL candidates blocked"
                lblSignal.Text = "Signal: WAITING"
                dgvBullishStocks.Rows.Add(
                    "NIFTY",
                    "MARKET NEUTRAL",
                    "---",
                    niftyPrice.ToString("0.00"),
                    "---",
                    "---",
                    "---",
                    "---",
                    "---",
                    "---",
                    "---")
                Return
            End If

            Dim bullishCount As Integer = 0
            Dim buyReadyCount As Integer = 0
            Dim sellCount As Integer = 0
            Dim sellReadyCount As Integer = 0
            Dim scanned As Integer = 0

            For Each symbol As String In symbols
                scanned += 1
                lblScannerStatus.Text =
                    "Scanner: " & scanned.ToString() & "/" & symbols.Count.ToString() & " - " & symbol
                Application.DoEvents()

                Try
                    Dim instrumentKey As String = ""
                    If scanInstrumentCache.ContainsKey(symbol) Then
                        instrumentKey = scanInstrumentCache(symbol)
                    Else
                        instrumentKey = Await SearchNseEquityInstrumentAsync(symbol)
                        If instrumentKey <> "" Then scanInstrumentCache(symbol) = instrumentKey
                    End If

                    If instrumentKey = "" Then
                        LogMessage(symbol & ": instrument not found")
                        Continue For
                    End If

                    Dim candleSet As ScannerCandleSet =
                        Await GetScannerCandleSetAsync(instrumentKey, symbol)

                    If candleSet Is Nothing OrElse candleSet.AllCandles.Count < SLOW_EMA_PERIOD Then
                        LogMessage(symbol & ": insufficient 5M data for EMA 21")
                        Continue For
                    End If

                    If candleSet.TodayCandles.Count = 0 Then
                        LogMessage(symbol & ": today's 5M candles not available yet")
                        Continue For
                    End If

                    Dim leverage As Double = 1D
                    If scanLeverageCache.ContainsKey(symbol) Then
                        leverage = scanLeverageCache(symbol)
                    Else
                        leverage = Await GetIntradayLeverageAsync(instrumentKey, symbol)
                        scanLeverageCache(symbol) = leverage
                    End If

                    Dim result As LiveScanResult =
                        EvaluateLiveStock(
                            symbol,
                            candleSet.AllCandles,
                            candleSet.TodayCandles,
                            leverage,
                            marketDirection)

                    If result Is Nothing Then Continue For

                    ' The strategy uses completed 5-minute candles.
                    ' Also fetch the current Upstox LTP so the user can see
                    ' why the live price may differ slightly from the 5M close.
                    result.LivePrice = Await GetLiveLtpAsync(instrumentKey)
                    LogMessage(symbol & " | Live LTP: " & If(result.LivePrice > 0, result.LivePrice.ToString("0.00"), "N/A") & " | 5M Close: " & result.Price.ToString("0.00"))

                    If result.Status = "BULLISH" OrElse
                       result.Status = "BUY READY" OrElse
                       result.Status = "BEARISH" OrElse
                       result.Status = "SELL READY" Then

                        If result.Status = "BULLISH" OrElse result.Status = "BUY READY" Then
                            bullishCount += 1
                        Else
                            sellCount += 1
                        End If

                        Dim rowIndex As Integer = dgvBullishStocks.Rows.Add(
                            result.Symbol,
                            result.Status,
                            If(result.LivePrice > 0, result.LivePrice.ToString("0.00"), "---"),
                            result.Price.ToString("0.00"),
                            If(result.Entry > 0, result.Entry.ToString("0.00"), "---"),
                            If(result.StopLoss > 0, result.StopLoss.ToString("0.00"), "---"),
                            If(result.Target > 0, result.Target.ToString("0.00"), "---"),
                            If(result.Quantity > 0, result.Quantity.ToString(), "---"),
                            If(result.Leverage > 0, result.Leverage.ToString("0.0") & "x", "1.0x"),
                            If(result.RiskAmount > 0, "₹" & result.RiskAmount.ToString("0.00"), "---"),
                            If(result.ExpectedProfit > 0, "₹" & result.ExpectedProfit.ToString("0.00"), "---"))

                        If result.Status = "BUY READY" Then
                            dgvBullishStocks.Rows(rowIndex).DefaultCellStyle.Font =
                                New Font(dgvBullishStocks.Font, FontStyle.Bold)
                            buyReadyCount += 1
                        ElseIf result.Status = "SELL READY" Then
                            dgvBullishStocks.Rows(rowIndex).DefaultCellStyle.Font =
                                New Font(dgvBullishStocks.Font, FontStyle.Bold)
                            sellReadyCount += 1
                        End If
                    End If

                Catch ex As Exception
                    LogMessage(symbol & ": scan error - " & ex.Message)
                End Try
            Next

            Dim scanTime As DateTime = GetIndiaTime()
            lblScannerStatus.Text =
                If(automaticScan, "Scanner: LIVE - ", "Scanner: ") &
                "Complete - Market: " & marketDirection &
                " | Bullish: " & bullishCount.ToString() &
                " | BUY READY: " & buyReadyCount.ToString() &
                " | Bearish: " & sellCount.ToString() &
                " | SELL READY: " & sellReadyCount.ToString() &
                " | " & scanTime.ToString("HH:mm:ss")

            If bullishCount = 0 AndAlso sellCount = 0 Then
                dgvBullishStocks.Rows.Add(
                    "---", "NONE", "---", "---", "---", "---", "---", "---", "---", "---", "---")
            End If

            LogMessage("")
            LogMessage("SCAN COMPLETE")
            LogMessage("Market direction: " & marketDirection)
            LogMessage("Bullish stocks: " & bullishCount.ToString())
            LogMessage("BUY READY stocks: " & buyReadyCount.ToString())
            LogMessage("Bearish stocks: " & sellCount.ToString())
            LogMessage("SELL READY stocks: " & sellReadyCount.ToString())

        Catch ex As Exception
            lblScannerStatus.Text = "Scanner: Error - " & ex.Message
            LogMessage("Bullish scanner error: " & ex.Message)
            If Not automaticScan Then
                MessageBox.Show("Bullish scanner error:" & Environment.NewLine & Environment.NewLine & ex.Message, "Scanner Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Finally
            scannerScanInProgress = False
            If btnScanBullish IsNot Nothing Then
                btnScanBullish.Enabled = Not liveScannerRunning
            End If
        End Try

    End Function

    Private Function IsNseMarketOpen() As Boolean
        Dim nowIndia As DateTime = GetIndiaTime()

        If nowIndia.DayOfWeek = DayOfWeek.Saturday OrElse
           nowIndia.DayOfWeek = DayOfWeek.Sunday Then
            Return False
        End If

        Dim marketOpen As TimeSpan = New TimeSpan(9, 15, 0)
        Dim marketClose As TimeSpan = New TimeSpan(15, 30, 0)

        Return nowIndia.TimeOfDay >= marketOpen AndAlso
               nowIndia.TimeOfDay <= marketClose
    End Function

    Private Function IsTodayCandle(candle As Candle) As Boolean
        If candle Is Nothing OrElse String.IsNullOrWhiteSpace(candle.DateValue) Then Return False
        Return candle.DateValue.StartsWith(GetIndiaTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
    End Function

    Private Async Function GetScannerCandleSetAsync(
        instrumentKey As String,
        symbol As String) As Task(Of ScannerCandleSet)

        Dim combined As New List(Of Candle)()
        Dim today As New List(Of Candle)()

        If String.IsNullOrWhiteSpace(upstoxAccessToken) Then
            Return New ScannerCandleSet With {.AllCandles = combined, .TodayCandles = today}
        End If

        Dim encodedKey As String = Uri.EscapeDataString(instrumentKey)
        Dim intradayUrl As String =
            "https://api.upstox.com/v3/historical-candle/intraday/" &
            encodedKey & "/minutes/5"

        Dim responseText As String = Await UpstoxGetAsync(intradayUrl)
        Dim intraday As List(Of Candle) = ParseUpstoxCandleResponse(responseText, symbol)

        For Each c As Candle In intraday
            If IsTodayCandle(c) Then today.Add(c)
        Next

        Dim toDate As DateTime = GetIndiaTime().Date
        Dim fromDate As DateTime = toDate.AddDays(-7)
        Dim historicalUrl As String =
            "https://api.upstox.com/v3/historical-candle/" &
            encodedKey & "/minutes/5/" &
            toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) & "/" &
            fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

        responseText = Await UpstoxGetAsync(historicalUrl)
        Dim historical As List(Of Candle) = ParseUpstoxCandleResponse(responseText, symbol)

        ' Merge historical and intraday candles without duplicate timestamps.
        Dim byTime As New Dictionary(Of String, Candle)(StringComparer.Ordinal)
        For Each c As Candle In historical
            If Not byTime.ContainsKey(c.DateValue) Then byTime.Add(c.DateValue, c)
        Next
        For Each c As Candle In intraday
            byTime(c.DateValue) = c
        Next

        combined = SortCandles(byTime.Values.ToList())
        today = SortCandles(today)

        ' Use only CLOSED 5-minute candles for strategy decisions.
        ' This prevents a partially formed candle from generating a premature
        ' breakout / BUY READY signal.
        Dim nowIndiaForCandles As DateTime = GetIndiaTime()
        Dim currentBucketMinute As Integer = (nowIndiaForCandles.Minute \ 5) * 5
        Dim currentBucket As New DateTime(
            nowIndiaForCandles.Year,
            nowIndiaForCandles.Month,
            nowIndiaForCandles.Day,
            nowIndiaForCandles.Hour,
            currentBucketMinute,
            0)

        Dim closedCandlePredicate =
            Function(c As Candle) As Boolean
                Try
                    Return DateTime.Parse(c.DateValue, CultureInfo.InvariantCulture) < currentBucket
                Catch
                    Return False
                End Try
            End Function

        combined = combined.Where(closedCandlePredicate).ToList()
        today = today.Where(closedCandlePredicate).ToList()

        Return New ScannerCandleSet With {
            .AllCandles = combined,
            .TodayCandles = today
        }

    End Function

    Private Function EvaluateLiveStock(
        symbol As String,
        allCandles As List(Of Candle),
        todayCandles As List(Of Candle),
        leverage As Double,
        marketDirection As String) As LiveScanResult

        Dim sortedAll As List(Of Candle) = SortCandles(allCandles)
        Dim sortedToday As List(Of Candle) = SortCandles(todayCandles)

        If sortedAll.Count < SLOW_EMA_PERIOD OrElse sortedToday.Count = 0 Then Return Nothing

        Dim last As Candle = sortedToday(sortedToday.Count - 1)
        Dim price As Double = last.Close
        Dim ema9 As Double = CalculateEMA(sortedAll, FAST_EMA_PERIOD)
        Dim ema21 As Double = CalculateEMA(sortedAll, SLOW_EMA_PERIOD)

        ' VWAP is calculated from TODAY'S equity session only.
        Dim vwap As Double = CalculateVWAP(sortedToday)

        Dim result As New LiveScanResult()
        result.Symbol = symbol
        result.Price = price
        result.Leverage = If(leverage >= 1D, leverage, 1D)
        result.EMA9 = ema9
        result.EMA21 = ema21
        result.VWAP = vwap

        Dim bullishTrend As Boolean =
            price > ema9 AndAlso
            ema9 > ema21 AndAlso
            vwap > 0 AndAlso
            price > vwap

        Dim bearishTrend As Boolean =
            price < ema9 AndAlso
            ema9 < ema21 AndAlso
            vwap > 0 AndAlso
            price < vwap

        ' The NIFTY filter determines which direction is allowed.
        If marketDirection = "BUY" AndAlso Not bullishTrend Then Return Nothing
        If marketDirection = "SELL" AndAlso Not bearishTrend Then Return Nothing

        Dim conditions As New List(Of String)

        If marketDirection = "BUY" Then
            conditions.Add("5M Close > EMA9")
            conditions.Add("EMA9 > EMA21")
            conditions.Add("5M Close > today's VWAP")
        Else
            conditions.Add("5M Close < EMA9")
            conditions.Add("EMA9 < EMA21")
            conditions.Add("5M Close < today's VWAP")
        End If

        If sortedToday.Count < CONSOLIDATION_CANDLES + 1 Then
            result.Status = If(marketDirection = "BUY", "BULLISH", "BEARISH")
            result.Direction = marketDirection
            result.Conditions = String.Join(" | ", conditions) &
                                " | Waiting for 3-candle consolidation + breakout/breakdown"
            Return result
        End If

        Dim breakoutIndex As Integer = sortedToday.Count - 1
        Dim startIndex As Integer = breakoutIndex - CONSOLIDATION_CANDLES
        Dim highestHigh As Double = Double.MinValue
        Dim lowestLow As Double = Double.MaxValue
        Dim totalClose As Double = 0D

        For i As Integer = startIndex To breakoutIndex - 1
            highestHigh = Math.Max(highestHigh, sortedToday(i).High)
            lowestLow = Math.Min(lowestLow, sortedToday(i).Low)
            totalClose += sortedToday(i).Close
        Next

        Dim avgClose As Double = totalClose / CDbl(CONSOLIDATION_CANDLES)
        Dim rangePercent As Double =
            If(avgClose > 0,
               ((highestHigh - lowestLow) / avgClose) * 100D,
               999D)

        Dim consolidationOk As Boolean =
            rangePercent <= CONSOLIDATION_RANGE_PERCENT

        Dim breakoutOk As Boolean =
            last.Close > highestHigh

        Dim breakdownOk As Boolean =
            last.Close < lowestLow

        Dim volumeTotal As Double = 0D
        Dim volumeCount As Integer = 0

        For i As Integer =
            Math.Max(0, breakoutIndex - BREAKOUT_VOLUME_LOOKBACK) To breakoutIndex - 1

            volumeTotal += CDbl(sortedToday(i).Volume)
            volumeCount += 1
        Next

        Dim avgVolume As Double =
            If(volumeCount > 0,
               volumeTotal / CDbl(volumeCount),
               0D)

        Dim volumeOk As Boolean =
            avgVolume > 0 AndAlso
            CDbl(last.Volume) > avgVolume

        Dim bullishCandle As Boolean =
            last.Close > last.Open

        Dim bearishCandle As Boolean =
            last.Close < last.Open

        If marketDirection = "BUY" Then

            conditions.Add("3-candle consolidation <= 1%")
            conditions.Add("Breakout close > consolidation high")
            conditions.Add("Breakout volume > previous 3-candle average")
            conditions.Add("Bullish breakout candle")

            If consolidationOk AndAlso
               breakoutOk AndAlso
               volumeOk AndAlso
               bullishCandle Then

                Dim entry As Double = last.Close
                Dim stopPrice As Double = lowestLow
                Dim riskPerShare As Double = entry - stopPrice

                If riskPerShare > 0 Then

                    Dim qty As Integer =
                        CalculateSafeQuantity(
                            entry,
                            riskPerShare,
                            result.Leverage)

                    If qty > 0 Then

                        Dim riskAmount As Double =
                            qty * riskPerShare

                        Dim targetRiskMultiple As Double = 1.5D
                        Dim target As Double =
                            entry + (riskPerShare * targetRiskMultiple)

                        Dim expectedProfit As Double =
                            (target - entry) * qty

                        If expectedProfit < MIN_EXPECTED_PROFIT Then
                            targetRiskMultiple = 1D
                            target = entry + riskPerShare
                            expectedProfit = (target - entry) * qty
                        End If

                        result.Entry = entry
                        result.StopLoss = stopPrice
                        result.Target = target
                        result.Quantity = qty
                        result.RiskAmount = riskAmount
                        result.ExpectedProfit = expectedProfit
                        result.Direction = "BUY"

                        If expectedProfit >= MIN_EXPECTED_PROFIT Then
                            result.Status = "BUY READY"
                            result.Conditions =
                                String.Join(" | ", conditions) &
                                " | Target " & targetRiskMultiple.ToString("0.0") & "R" &
                                " | Leverage " & result.Leverage.ToString("0.0") & "x" &
                                " | Risk/share ₹" & riskPerShare.ToString("0.00") &
                                " | Risk ₹" & riskAmount.ToString("0.00") &
                                " | Expected Profit ₹" & expectedProfit.ToString("0.00")

                            Return result
                        Else
                            result.Status = "BULLISH"
                            result.Conditions =
                                String.Join(" | ", conditions) &
                                " | TARGET FILTER: SKIP" &
                                " | Expected Profit ₹" &
                                expectedProfit.ToString("0.00")
                            Return result
                        End If
                    End If
                End If
            End If

            result.Status = "BULLISH"
            result.Direction = "BUY"
            result.Conditions =
                String.Join(" | ", conditions) &
                " | Consolidation: " & If(consolidationOk, "YES", "NO") &
                " | Breakout: " & If(breakoutOk, "YES", "NO") &
                " | Volume: " & If(volumeOk, "YES", "NO")

            Return result

        Else

            ' SELL / SHORT conditions:
            ' 1. 5M close below EMA9
            ' 2. EMA9 below EMA21
            ' 3. 5M close below today's VWAP
            ' 4. Three-candle consolidation <= 1%
            ' 5. Breakdown below consolidation low
            ' 6. Breakdown volume above previous 3-candle average
            ' 7. Bearish breakdown candle

            conditions.Add("3-candle consolidation <= 1%")
            conditions.Add("Breakdown close < consolidation low")
            conditions.Add("Breakdown volume > previous 3-candle average")
            conditions.Add("Bearish breakdown candle")

            If consolidationOk AndAlso
               breakdownOk AndAlso
               volumeOk AndAlso
               bearishCandle Then

                Dim entry As Double = last.Close
                Dim stopPrice As Double = highestHigh
                Dim riskPerShare As Double = stopPrice - entry

                If riskPerShare > 0 Then

                    Dim qty As Integer =
                        CalculateSafeQuantity(
                            entry,
                            riskPerShare,
                            result.Leverage)

                    If qty > 0 Then

                        Dim riskAmount As Double =
                            qty * riskPerShare

                        Dim targetRiskMultiple As Double = 1.5D
                        Dim target As Double =
                            entry - (riskPerShare * targetRiskMultiple)

                        Dim expectedProfit As Double =
                            (entry - target) * qty

                        If expectedProfit < MIN_EXPECTED_PROFIT Then
                            targetRiskMultiple = 1D
                            target = entry - riskPerShare
                            expectedProfit = (entry - target) * qty
                        End If

                        result.Entry = entry
                        result.StopLoss = stopPrice
                        result.Target = target
                        result.Quantity = qty
                        result.RiskAmount = riskAmount
                        result.ExpectedProfit = expectedProfit
                        result.Direction = "SELL"

                        If expectedProfit >= MIN_EXPECTED_PROFIT Then
                            result.Status = "SELL READY"
                            result.Conditions =
                                String.Join(" | ", conditions) &
                                " | Target " & targetRiskMultiple.ToString("0.0") & "R" &
                                " | Leverage " & result.Leverage.ToString("0.0") & "x" &
                                " | Risk/share ₹" & riskPerShare.ToString("0.00") &
                                " | Risk ₹" & riskAmount.ToString("0.00") &
                                " | Expected Profit ₹" & expectedProfit.ToString("0.00")

                            Return result
                        Else
                            result.Status = "BEARISH"
                            result.Conditions =
                                String.Join(" | ", conditions) &
                                " | TARGET FILTER: SKIP" &
                                " | Expected Profit ₹" &
                                expectedProfit.ToString("0.00")
                            Return result
                        End If
                    End If
                End If
            End If

            result.Status = "BEARISH"
            result.Direction = "SELL"
            result.Conditions =
                String.Join(" | ", conditions) &
                " | Consolidation: " & If(consolidationOk, "YES", "NO") &
                " | Breakdown: " & If(breakdownOk, "YES", "NO") &
                " | Volume: " & If(volumeOk, "YES", "NO")

            Return result

        End If

    End Function

    Private Function CalculateSafeQuantity(
        entry As Double,
        riskPerShare As Double,
        leverage As Double) As Integer

        If entry <= 0 OrElse riskPerShare <= 0 Then Return 0

        If leverage < 1D Then leverage = 1D

        ' Leverage increases available buying power, but NEVER increases
        ' the maximum allowed loss. Risk sizing remains the final constraint.
        Dim leveragedBuyingPower As Double = CAPITAL * leverage
        Dim buyingPowerQuantity As Integer = CInt(Math.Floor(leveragedBuyingPower / entry))

        Dim maxRiskAmount As Double = CAPITAL * (MAX_RISK_PERCENT / 100D)
        Dim riskQuantity As Integer = CInt(Math.Floor(maxRiskAmount / riskPerShare))

        Return Math.Max(0, Math.Min(buyingPowerQuantity, riskQuantity))

    End Function

    Private Async Function GetLiveLtpAsync(
        instrumentKey As String) As Task(Of Double)

        If String.IsNullOrWhiteSpace(instrumentKey) OrElse
           String.IsNullOrWhiteSpace(upstoxAccessToken) Then
            Return 0D
        End If

        Try
            Dim url As String =
                "https://api.upstox.com/v3/market-quote/ltp?instrument_key=" &
                Uri.EscapeDataString(instrumentKey)

            Using client As New HttpClient()
                client.DefaultRequestHeaders.Clear()
                client.DefaultRequestHeaders.Accept.Add(
                    New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                        "application/json"))
                client.DefaultRequestHeaders.Authorization =
                    New System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", upstoxAccessToken)

                Dim response As HttpResponseMessage = Await client.GetAsync(url)
                If Not response.IsSuccessStatusCode Then Return 0D

                Dim responseText As String =
                    Await response.Content.ReadAsStringAsync()

                Using json As JsonDocument = JsonDocument.Parse(responseText)
                    Dim dataElement As JsonElement
                    If Not json.RootElement.TryGetProperty("data", dataElement) Then
                        Return 0D
                    End If

                    Return GetLtpFromData(dataElement, instrumentKey)
                End Using
            End Using

        Catch
            ' LTP is display-only. A temporary LTP failure must not stop
            ' the 5-minute strategy scan.
            Return 0D
        End Try

    End Function

    Private Async Function SearchNseEquityInstrumentAsync(
        symbol As String) As Task(Of String)

        Dim url As String =
            "https://api.upstox.com/v2/instruments/search?query=" &
            Uri.EscapeDataString(symbol) &
            "&exchanges=NSE&segments=EQ&page_number=1&records=10"

        Using client As New HttpClient()
            client.DefaultRequestHeaders.Authorization =
                New System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", upstoxAccessToken)
            client.DefaultRequestHeaders.Accept.Add(
                New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))

            Dim response As HttpResponseMessage = Await client.GetAsync(url)
            Dim text As String = Await response.Content.ReadAsStringAsync()
            If Not response.IsSuccessStatusCode Then Return ""

            Using json As JsonDocument = JsonDocument.Parse(text)
                Dim data As JsonElement
                If Not json.RootElement.TryGetProperty("data", data) OrElse data.ValueKind <> JsonValueKind.Array Then Return ""

                For Each item As JsonElement In data.EnumerateArray()
                    Dim segment As String = ""
                    Dim tradingSymbol As String = ""
                    Dim key As String = ""
                    If item.TryGetProperty("segment", data) Then segment = data.GetString()
                    If item.TryGetProperty("trading_symbol", data) Then tradingSymbol = data.GetString()
                    If item.TryGetProperty("instrument_key", data) Then key = data.GetString()

                    If segment = "NSE_EQ" AndAlso tradingSymbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) Then
                        Dim leverage As Double = 1D
                        Dim leverageElement As JsonElement
                        If item.TryGetProperty("intraday_leverage", leverageElement) Then
                            If leverageElement.ValueKind = JsonValueKind.Number Then
                                leverage = leverageElement.GetDouble()
                            End If
                        End If

                        If leverage < 1D Then leverage = 1D
                        scanLeverageCache(symbol) = leverage

                        LogMessage(symbol & " | Upstox intraday leverage: " & leverage.ToString("0.0") & "x")
                        Return key
                    End If
                Next
            End Using
        End Using

        Return ""

    End Function

    Private Async Function GetIntradayLeverageAsync(
        instrumentKey As String,
        symbol As String) As Task(Of Double)

        If String.IsNullOrWhiteSpace(instrumentKey) OrElse
           String.IsNullOrWhiteSpace(upstoxAccessToken) Then
            Return 1D
        End If

        Try
            Dim querySymbol As String = symbol
            Dim url As String =
                "https://api.upstox.com/v2/instruments/search?query=" &
                Uri.EscapeDataString(querySymbol) &
                "&exchanges=NSE&segments=EQ&page_number=1&records=10"

            Using client As New HttpClient()
                client.DefaultRequestHeaders.Authorization =
                    New System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", upstoxAccessToken)
                client.DefaultRequestHeaders.Accept.Add(
                    New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))

                Dim response As HttpResponseMessage = Await client.GetAsync(url)
                Dim text As String = Await response.Content.ReadAsStringAsync()
                If Not response.IsSuccessStatusCode Then Return 1D

                Using json As JsonDocument = JsonDocument.Parse(text)
                    Dim data As JsonElement
                    If Not json.RootElement.TryGetProperty("data", data) OrElse data.ValueKind <> JsonValueKind.Array Then Return 1D

                    For Each item As JsonElement In data.EnumerateArray()
                        Dim keyElement As JsonElement
                        Dim key As String = ""
                        If item.TryGetProperty("instrument_key", keyElement) Then key = keyElement.GetString()

                        If key.Equals(instrumentKey, StringComparison.OrdinalIgnoreCase) Then
                            Dim leverageElement As JsonElement
                            If item.TryGetProperty("intraday_leverage", leverageElement) AndAlso
                               leverageElement.ValueKind = JsonValueKind.Number Then
                                Dim leverage As Double = leverageElement.GetDouble()
                                If leverage >= 1D Then Return leverage
                            End If
                        End If
                    Next
                End Using
            End Using
        Catch ex As Exception
            LogMessage(symbol & " | leverage lookup failed; using 1.0x")
        End Try

        Return 1D

    End Function

    Private Async Function GetUpstox5MinuteCandlesAsync(
        instrumentKey As String,
        symbol As String) As Task(Of List(Of Candle))

        Dim result As New List(Of Candle)()

        If String.IsNullOrWhiteSpace(upstoxAccessToken) Then Return result

        ' First try current-day intraday candles.
        Dim encodedKey As String = Uri.EscapeDataString(instrumentKey)
        Dim intradayUrl As String =
            "https://api.upstox.com/v3/historical-candle/intraday/" &
            encodedKey & "/minutes/5"

        Dim responseText As String = Await UpstoxGetAsync(intradayUrl)
        result = ParseUpstoxCandleResponse(responseText, symbol)

        If result.Count >= SLOW_EMA_PERIOD Then Return result

        ' Fallback to recent historical 5-minute candles.
        Dim toDate As DateTime = DateTime.Now.Date
        Dim fromDate As DateTime = toDate.AddDays(-7)
        Dim historicalUrl As String =
            "https://api.upstox.com/v3/historical-candle/" &
            encodedKey & "/minutes/5/" &
            toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) & "/" &
            fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

        responseText = Await UpstoxGetAsync(historicalUrl)
        Dim historical As List(Of Candle) = ParseUpstoxCandleResponse(responseText, symbol)
        If historical.Count > 0 Then result = historical

        Return result

    End Function

    Private Async Function UpstoxGetAsync(url As String) As Task(Of String)
        Using client As New HttpClient()
            client.DefaultRequestHeaders.Authorization =
                New System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", upstoxAccessToken)
            client.DefaultRequestHeaders.Accept.Add(
                New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
            Dim response As HttpResponseMessage = Await client.GetAsync(url)
            Return Await response.Content.ReadAsStringAsync()
        End Using
    End Function

    Private Function ParseUpstoxCandleResponse(
        responseText As String,
        symbol As String) As List(Of Candle)

        Dim result As New List(Of Candle)()
        If String.IsNullOrWhiteSpace(responseText) Then Return result

        Try
            Using json As JsonDocument = JsonDocument.Parse(responseText)
                Dim data As JsonElement
                If Not json.RootElement.TryGetProperty("data", data) Then Return result

                Dim candles As JsonElement
                If Not data.TryGetProperty("candles", candles) OrElse candles.ValueKind <> JsonValueKind.Array Then Return result

                For Each item As JsonElement In candles.EnumerateArray()
                    If item.ValueKind <> JsonValueKind.Array OrElse item.GetArrayLength() < 6 Then Continue For

                    Dim c As New Candle()
                    c.DateValue = item(0).GetString()
                    c.Symbol = symbol
                    c.Exchange = "NSE"
                    c.Interval = "5minute"
                    c.Open = item(1).GetDouble()
                    c.High = item(2).GetDouble()
                    c.Low = item(3).GetDouble()
                    c.Close = item(4).GetDouble()
                    c.Volume = item(5).GetInt64()

                    If c.Open > 0 AndAlso c.High > 0 AndAlso c.Low > 0 AndAlso c.Close > 0 Then
                        result.Add(c)
                    End If
                Next
            End Using
        Catch
            Return New List(Of Candle)()
        End Try

        Return SortCandles(result)

    End Function

    ' =========================================================
    ' LOAD NIFTY 50 - 5 MINUTE CANDLES
    ' =========================================================

    Private Async Sub btnLoadNiftyChart_Click(
        sender As Object,
        e As EventArgs)

        Try

            If String.IsNullOrWhiteSpace(upstoxAccessToken) Then

                MessageBox.Show(
                    "Please connect to Upstox first.",
                    "NIFTY Chart",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            btnLoadNiftyChart.Enabled = False
            lblChartStatus.Text = "Chart: Loading..."
            LogMessage("Requesting NIFTY 50 5-minute candles...")

            Dim url As String =
                "https://api.upstox.com/v3/historical-candle/intraday/" &
                Uri.EscapeDataString(NIFTY_INSTRUMENT_KEY) &
                "/minutes/5"

            Using client As New HttpClient()

                client.DefaultRequestHeaders.Clear()

                client.DefaultRequestHeaders.Accept.Add(
                    New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                        "application/json"))

                client.DefaultRequestHeaders.Authorization =
                    New System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        upstoxAccessToken)

                Dim response As HttpResponseMessage =
                    Await client.GetAsync(url)

                Dim responseText As String =
                    Await response.Content.ReadAsStringAsync()

                LogMessage(
                    "5M candle HTTP status: " &
                    CInt(response.StatusCode).ToString())

                If Not response.IsSuccessStatusCode Then

                    lblChartStatus.Text =
                        "Chart: API error"

                    LogMessage(
                        "5M candle request failed: HTTP " &
                        CInt(response.StatusCode).ToString())

                    LogMessage(responseText)

                    MessageBox.Show(
                        "Unable to load NIFTY candles:" &
                        Environment.NewLine & Environment.NewLine &
                        responseText,
                        "NIFTY Chart",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)

                    Return

                End If

                Using json As JsonDocument =
                    JsonDocument.Parse(responseText)

                    Dim dataElement As JsonElement

                    If Not json.RootElement.TryGetProperty(
                        "data",
                        dataElement) Then

                        lblChartStatus.Text =
                            "Chart: No data"

                        LogMessage(
                            "Upstox response did not contain data.")

                        Return

                    End If

                    Dim candlesElement As JsonElement

                    If Not dataElement.TryGetProperty(
                        "candles",
                        candlesElement) Then

                        lblChartStatus.Text =
                            "Chart: No candles"

                        LogMessage(
                            "Upstox response did not contain candles.")

                        Return

                    End If

                    chartCandles.Clear()

                    For Each candleElement As JsonElement In
                        candlesElement.EnumerateArray()

                        If candleElement.ValueKind <>
                           JsonValueKind.Array Then

                            Continue For

                        End If

                        Dim values As List(Of JsonElement) =
                            candleElement.EnumerateArray().ToList()

                        If values.Count < 6 Then
                            Continue For
                        End If

                        Dim cc As New ChartCandle()

                        cc.Time =
                            DateTime.Parse(
                                values(0).GetString(),
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind)

                        cc.Open =
                            values(1).GetDouble()

                        cc.High =
                            values(2).GetDouble()

                        cc.Low =
                            values(3).GetDouble()

                        cc.Close =
                            values(4).GetDouble()

                        cc.Volume =
                            values(5).GetInt64()

                        chartCandles.Add(cc)

                    Next

                End Using

            End Using

            ' Upstox returns the newest candle first in its examples.
            ' Sort chronologically so the chart draws left-to-right.
            chartCandles =
                chartCandles.
                OrderBy(Function(c) c.Time).
                ToList()

            If chartCandles.Count = 0 Then

                ' Before the NSE session starts (or after the session has not
                ' produced any intraday candles yet), the intraday endpoint can
                ' legitimately return an empty candle list. In that case, load
                ' the most recent available 5-minute historical candles instead.
                LogMessage(
                    "No current-day intraday candles. Trying recent historical 5-minute data...")

                Dim historicalCandles As List(Of ChartCandle) =
                    Await LoadRecentHistoricalNiftyCandlesAsync()

                If historicalCandles IsNot Nothing AndAlso
                   historicalCandles.Count > 0 Then

                    chartCandles = historicalCandles

                    LogMessage(
                        "Historical fallback returned " &
                        chartCandles.Count.ToString() &
                        " NIFTY 5-minute candles.")

                Else

                    lblChartStatus.Text =
                        "Chart: No candles available"

                    LogMessage(
                        "No current-day or recent historical NIFTY candles were returned.")

                    pnlNiftyChart.Invalidate()

                    Return

                End If

            End If

            ' Keep the most recent 75 candles for a readable chart.
            If chartCandles.Count > 75 Then

                chartCandles =
                    chartCandles.
                    Skip(chartCandles.Count - 75).
                    ToList()

            End If

            CalculateChartIndicators()

            pnlNiftyChart.Invalidate()

            lblChartStatus.Text =
                "Chart: " &
                chartCandles.Count.ToString() &
                " candles | EMA 9 | EMA 21 | VWAP"

            LogMessage(
                "NIFTY 5-minute chart loaded: " &
                chartCandles.Count.ToString() &
                " candles.")

            LogMessage(
                "Indicators calculated: EMA 9, EMA 21, VWAP.")

        Catch ex As Exception

            lblChartStatus.Text =
                "Chart: Error"

            LogMessage(
                "NIFTY chart error: " &
                ex.Message)

            MessageBox.Show(
                "NIFTY chart error:" &
                Environment.NewLine & Environment.NewLine &
                ex.Message,
                "NIFTY Chart",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        Finally

            If btnLoadNiftyChart IsNot Nothing Then
                btnLoadNiftyChart.Enabled = True
            End If

        End Try

    End Sub

    ' =========================================================
    ' FALLBACK: RECENT HISTORICAL 5-MINUTE CANDLES
    ' =========================================================

    Private Async Function LoadRecentHistoricalNiftyCandlesAsync() As Task(Of List(Of ChartCandle))

        Dim result As New List(Of ChartCandle)()

        Try

            Dim toDate As Date = Date.Today
            Dim fromDate As Date = toDate.AddDays(-7)

            Dim url As String =
                "https://api.upstox.com/v3/historical-candle/" &
                Uri.EscapeDataString(NIFTY_INSTRUMENT_KEY) &
                "/minutes/5/" &
                toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) &
                "/" &
                fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

            LogMessage(
                "Requesting historical 5M candles from " &
                fromDate.ToString("yyyy-MM-dd") &
                " to " &
                toDate.ToString("yyyy-MM-dd") &
                "...")

            Using client As New HttpClient()

                client.DefaultRequestHeaders.Clear()

                client.DefaultRequestHeaders.Accept.Add(
                    New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                        "application/json"))

                client.DefaultRequestHeaders.Authorization =
                    New System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        upstoxAccessToken)

                Dim response As HttpResponseMessage =
                    Await client.GetAsync(url)

                Dim responseText As String =
                    Await response.Content.ReadAsStringAsync()

                LogMessage(
                    "Historical 5M HTTP status: " &
                    CInt(response.StatusCode).ToString())

                If Not response.IsSuccessStatusCode Then
                    LogMessage(
                        "Historical 5M request failed: " &
                        responseText)
                    Return result
                End If

                Using json As JsonDocument =
                    JsonDocument.Parse(responseText)

                    Dim dataElement As JsonElement

                    If Not json.RootElement.TryGetProperty("data", dataElement) Then
                        Return result
                    End If

                    Dim candlesElement As JsonElement

                    If Not dataElement.TryGetProperty("candles", candlesElement) Then
                        Return result
                    End If

                    For Each candleElement As JsonElement In
                        candlesElement.EnumerateArray()

                        If candleElement.ValueKind <> JsonValueKind.Array Then
                            Continue For
                        End If

                        Dim values As List(Of JsonElement) =
                            candleElement.EnumerateArray().ToList()

                        If values.Count < 6 Then
                            Continue For
                        End If

                        Dim cc As New ChartCandle()

                        cc.Time =
                            DateTime.Parse(
                                values(0).GetString(),
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind)

                        cc.Open = values(1).GetDouble()
                        cc.High = values(2).GetDouble()
                        cc.Low = values(3).GetDouble()
                        cc.Close = values(4).GetDouble()
                        cc.Volume = values(5).GetInt64()

                        result.Add(cc)

                    Next

                End Using

            End Using

            Return result.
                OrderBy(Function(c) c.Time).
                ToList()

        Catch ex As Exception

            LogMessage(
                "Historical 5M fallback error: " &
                ex.Message)

            Return result

        End Try

    End Function


    ' =========================================================
    ' CALCULATE EMA 9, EMA 21 AND VWAP
    ' =========================================================

    Private Sub CalculateChartIndicators()

        chartEma9.Clear()
        chartEma21.Clear()
        chartVwap.Clear()

        If chartCandles.Count = 0 Then
            Return
        End If

        Dim ema9 As Double =
            chartCandles(0).Close

        Dim ema21 As Double =
            chartCandles(0).Close

        Dim alpha9 As Double =
            2D / (FAST_EMA_PERIOD + 1D)

        Dim alpha21 As Double =
            2D / (SLOW_EMA_PERIOD + 1D)

        Dim cumulativePV As Double = 0D
        Dim cumulativeVolume As Double = 0D

        For Each candle As ChartCandle In chartCandles

            ema9 =
                ((candle.Close - ema9) * alpha9) +
                ema9

            ema21 =
                ((candle.Close - ema21) * alpha21) +
                ema21

            Dim typicalPrice As Double =
                (candle.High +
                 candle.Low +
                 candle.Close) / 3D

            cumulativePV +=
                typicalPrice *
                CDbl(candle.Volume)

            cumulativeVolume +=
                CDbl(candle.Volume)

            Dim vwap As Double

            If cumulativeVolume > 0 Then
                vwap =
                    cumulativePV /
                    cumulativeVolume
            Else
                vwap =
                    candle.Close
            End If

            chartEma9.Add(ema9)
            chartEma21.Add(ema21)
            chartVwap.Add(vwap)

        Next

    End Sub

    ' =========================================================
    ' DRAW CANDLESTICK CHART
    ' =========================================================

    Private Sub pnlNiftyChart_Paint(
        sender As Object,
        e As PaintEventArgs)

        Dim g As Graphics =
            e.Graphics

        g.Clear(Color.White)

        If chartCandles.Count = 0 Then

            Using f As New Font("Arial", 11)

                g.DrawString(
                    "Click 'Load / Refresh 5M Chart' after Upstox is connected.",
                    f,
                    Brushes.Black,
                    New PointF(20, 20))

            End Using

            Return

        End If

        g.SmoothingMode =
            Drawing2D.SmoothingMode.AntiAlias

        Dim left As Integer = 55
        Dim right As Integer = 10
        Dim top As Integer = 25
        Dim bottom As Integer = 40

        Dim chartWidth As Integer =
            pnlNiftyChart.ClientSize.Width -
            left -
            right

        Dim chartHeight As Integer =
            pnlNiftyChart.ClientSize.Height -
            top -
            bottom

        If chartWidth <= 0 OrElse
           chartHeight <= 0 Then

            Return

        End If

        Dim minPrice As Double =
            chartCandles.Min(Function(c) c.Low)

        Dim maxPrice As Double =
            chartCandles.Max(Function(c) c.High)

        If chartEma9.Count > 0 Then

            minPrice =
                Math.Min(
                    minPrice,
                    chartEma9.Min())

            maxPrice =
                Math.Max(
                    maxPrice,
                    chartEma9.Max())

        End If

        If chartEma21.Count > 0 Then

            minPrice =
                Math.Min(
                    minPrice,
                    chartEma21.Min())

            maxPrice =
                Math.Max(
                    maxPrice,
                    chartEma21.Max())

        End If

        If chartVwap.Count > 0 Then

            minPrice =
                Math.Min(
                    minPrice,
                    chartVwap.Min())

            maxPrice =
                Math.Max(
                    maxPrice,
                    chartVwap.Max())

        End If

        Dim padding As Double =
            Math.Max(
                (maxPrice - minPrice) * 0.08D,
                0.5D)

        minPrice -= padding
        maxPrice += padding

        Dim priceRange As Double =
            maxPrice - minPrice

        ' Grid lines
        Using gridPen As New Pen(Color.Gainsboro)

            For i As Integer = 0 To 5

                Dim gy As Single =
                    CSng(
                        top +
                        (chartHeight *
                         i /
                         5D))

                g.DrawLine(
                    gridPen,
                    left,
                    gy,
                    left + chartWidth,
                    gy)

            Next

            For i As Integer = 0 To 5

                Dim gx As Single =
                    CSng(
                        left +
                        (chartWidth *
                         i /
                         5D))

                g.DrawLine(
                    gridPen,
                    gx,
                    top,
                    gx,
                    top + chartHeight)

            Next

        End Using

        ' Price labels
        Using f As New Font("Arial", 8)

            For i As Integer = 0 To 5

                Dim price As Double =
                    maxPrice -
                    ((maxPrice - minPrice) *
                     i /
                     5D)

                Dim y As Single =
                    CSng(
                        top +
                        (chartHeight *
                         i /
                         5D))

                g.DrawString(
                    price.ToString("0.00"),
                    f,
                    Brushes.Black,
                    2,
                    y - 7)

            Next

        End Using

        Dim count As Integer =
            chartCandles.Count

        Dim stepX As Double =
            chartWidth /
            CDbl(Math.Max(count, 1))

        Dim bodyWidth As Single =
            CSng(
                Math.Max(
                    2D,
                    stepX * 0.62D))

        For i As Integer = 0 To count - 1

            Dim candle As ChartCandle =
                chartCandles(i)

            ' Protect GDI+ from invalid numeric values returned by an
            ' incomplete/invalid candle. RectangleF throws
            ' ArgumentException when any coordinate is NaN or Infinity.
            If Not IsFinite(candle.Open) OrElse
               Not IsFinite(candle.High) OrElse
               Not IsFinite(candle.Low) OrElse
               Not IsFinite(candle.Close) Then
                Continue For
            End If

            Dim x As Single =
                CSng(
                    left +
                    ((i + 0.5D) * stepX))

            Dim yHigh As Single =
                PriceToY(
                    candle.High,
                    minPrice,
                    priceRange,
                    top,
                    chartHeight)

            Dim yLow As Single =
                PriceToY(
                    candle.Low,
                    minPrice,
                    priceRange,
                    top,
                    chartHeight)

            Dim yOpen As Single =
                PriceToY(
                    candle.Open,
                    minPrice,
                    priceRange,
                    top,
                    chartHeight)

            Dim yClose As Single =
                PriceToY(
                    candle.Close,
                    minPrice,
                    priceRange,
                    top,
                    chartHeight)

            Dim isUp As Boolean =
                candle.Close >= candle.Open

            Dim bodyTop As Single =
                Math.Min(yOpen, yClose)

            Dim bodyBottom As Single =
                Math.Max(yOpen, yClose)

            Dim bodyHeight As Single =
                Math.Max(
                    1,
                    bodyBottom - bodyTop)

            Using wickPen As New Pen(
                If(isUp,
                   Color.SeaGreen,
                   Color.Firebrick),
                1)

                g.DrawLine(
                    wickPen,
                    x,
                    yHigh,
                    x,
                    yLow)

            End Using

            Dim rectX As Single =
                x - (bodyWidth / 2)

            ' Final finite-value guard before calling GDI+.
            If Not IsFinite(x) OrElse
               Not IsFinite(bodyTop) OrElse
               Not IsFinite(bodyHeight) OrElse
               bodyWidth <= 0 OrElse
               bodyHeight <= 0 Then
                Continue For
            End If

            Dim rect As New RectangleF(
                rectX,
                bodyTop,
                bodyWidth,
                bodyHeight)

            Using bodyBrush As Brush =
                If(isUp,
                   CType(Brushes.White, Brush),
                   CType(Brushes.Firebrick, Brush))

                g.FillRectangle(
                    bodyBrush,
                    rect)

            End Using

            Using bodyPen As New Pen(
                If(isUp,
                   Color.SeaGreen,
                   Color.Firebrick),
                1)

                g.DrawRectangle(
                    bodyPen,
                    rect.X,
                    rect.Y,
                    rect.Width,
                    rect.Height)

            End Using

        Next

        ' Draw EMA 9
        DrawIndicatorLine(
            g,
            chartEma9,
            minPrice,
            priceRange,
            top,
            chartHeight,
            left,
            chartWidth,
            stepX,
            Pens.DodgerBlue)

        ' Draw EMA 21
        DrawIndicatorLine(
            g,
            chartEma21,
            minPrice,
            priceRange,
            top,
            chartHeight,
            left,
            chartWidth,
            stepX,
            Pens.DarkOrange)

        ' Draw VWAP
        DrawIndicatorLine(
            g,
            chartVwap,
            minPrice,
            priceRange,
            top,
            chartHeight,
            left,
            chartWidth,
            stepX,
            Pens.MediumVioletRed)

        ' Legend
        Using f As New Font(
            "Arial",
            8,
            FontStyle.Bold)

            g.DrawString(
                "EMA 9",
                f,
                Brushes.DodgerBlue,
                left + 5,
                5)

            g.DrawString(
                "EMA 21",
                f,
                Brushes.DarkOrange,
                left + 65,
                5)

            g.DrawString(
                "VWAP",
                f,
                Brushes.MediumVioletRed,
                left + 135,
                5)

        End Using

        ' Time labels
        Using f As New Font("Arial", 8)

            Dim labelIndexes() As Integer =
                {
                    0,
                    count \ 4,
                    count \ 2,
                    (count * 3) \ 4,
                    count - 1
                }

            For Each index As Integer In labelIndexes

                If index < 0 OrElse
                   index >= count Then
                    Continue For
                End If

                Dim x As Single =
                    CSng(
                        left +
                        ((index + 0.5D) * stepX))

                Dim labelText As String =
                    chartCandles(index).Time.ToString("HH:mm")

                g.DrawString(
                    labelText,
                    f,
                    Brushes.Black,
                    x - 15,
                    top + chartHeight + 5)

            Next

        End Using

    End Sub

    Private Function PriceToY(
        price As Double,
        minPrice As Double,
        priceRange As Double,
        top As Integer,
        height As Integer) As Single

        If Not IsFinite(price) OrElse
           Not IsFinite(minPrice) OrElse
           Not IsFinite(priceRange) OrElse
           priceRange <= 0 Then
            Return CSng(top + (height / 2D))
        End If

        Dim y As Double =
            top +
            (height *
             (1D -
              ((price - minPrice) /
               priceRange)))

        If Not IsFinite(y) Then
            Return CSng(top + (height / 2D))
        End If

        Return CSng(y)

    End Function

    Private Function IsFinite(value As Double) As Boolean
        Return Not Double.IsNaN(value) AndAlso
               Not Double.IsInfinity(value)
    End Function

    Private Function IsFinite(value As Single) As Boolean
        Return Not Single.IsNaN(value) AndAlso
               Not Single.IsInfinity(value)
    End Function

    Private Sub DrawIndicatorLine(
        g As Graphics,
        values As List(Of Double),
        minPrice As Double,
        priceRange As Double,
        top As Integer,
        height As Integer,
        left As Integer,
        width As Integer,
        stepX As Double,
        pen As Pen)

        If values Is Nothing OrElse
           values.Count < 2 Then

            Return

        End If

        Dim points(values.Count - 1) As PointF

        For i As Integer = 0 To values.Count - 1

            points(i) =
                New PointF(
                    CSng(
                        left +
                        ((i + 0.5D) * stepX)),
                    PriceToY(
                        values(i),
                        minPrice,
                        priceRange,
                        top,
                        height))

        Next

        g.DrawLines(
            pen,
            points)

    End Sub

    ' =========================================================
    ' CHART CANDLE
    ' =========================================================

    Private Class ChartCandle

        Public Property Time As DateTime

        Public Property Open As Double

        Public Property High As Double

        Public Property Low As Double

        Public Property Close As Double

        Public Property Volume As Long

    End Class

    ' =========================================================
    ' LOG
    ' =========================================================

    Private Sub LogMessage(
        message As String)

        If txtLog Is Nothing Then
            Return
        End If

        txtLog.AppendText(
            DateTime.Now.ToString("HH:mm:ss") &
            "  " &
            message &
            Environment.NewLine)

    End Sub

    ' =========================================================
    ' UPSTOX CONNECT
    ' =========================================================

    Private Sub btnConnectUpstox_Click(
        sender As Object,
        e As EventArgs)

        Try

            If String.IsNullOrWhiteSpace(UPSTOX_API_KEY) OrElse
               UPSTOX_API_KEY = "YOUR_UPSTOX_API_KEY" Then

                MessageBox.Show(
                    "Please enter your complete Upstox API Key in Form1.vb.",
                    "Upstox Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            If String.IsNullOrWhiteSpace(UPSTOX_API_SECRET) OrElse
               UPSTOX_API_SECRET = "YOUR_UPSTOX_API_SECRET" Then

                MessageBox.Show(
                    "Please enter your Upstox API Secret in Form1.vb.",
                    "Upstox Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            upstoxState = Guid.NewGuid().ToString("N")

            Dim authUrl As String =
                "https://api.upstox.com/v2/login/authorization/dialog" &
                "?response_type=code" &
                "&client_id=" & Uri.EscapeDataString(UPSTOX_API_KEY) &
                "&redirect_uri=" & Uri.EscapeDataString(UPSTOX_REDIRECT_URI) &
                "&state=" & Uri.EscapeDataString(upstoxState)

            LogMessage("Opening Upstox authorization page...")

            Process.Start(New ProcessStartInfo With {
                .FileName = authUrl,
                .UseShellExecute = True
            })

            lblUpstoxStatus.Text =
                "Upstox: WAITING FOR AUTHORIZATION CODE"

        Catch ex As Exception

            LogMessage("Upstox error: " & ex.Message)

            MessageBox.Show(
                "Unable to open Upstox authorization page:" &
                Environment.NewLine & Environment.NewLine &
                ex.Message,
                "Upstox Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

    End Sub

    ' =========================================================
    ' EXCHANGE UPSTOX CODE
    ' =========================================================

    Private Sub btnExchangeUpstoxCode_Click(
        sender As Object,
        e As EventArgs)

        Dim authorizationCode As String =
            txtUpstoxCode.Text.Trim()

        If String.IsNullOrWhiteSpace(authorizationCode) Then

            MessageBox.Show(
                "Please paste the Upstox Authorization Code first.",
                "Upstox",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

            Return

        End If

        ExchangeCodeForToken(authorizationCode)

    End Sub

    ' =========================================================
    ' TOKEN EXCHANGE
    ' =========================================================

    Private Async Sub ExchangeCodeForToken(
        authorizationCode As String)

        Try

            lblUpstoxStatus.Text =
                "Upstox: EXCHANGING CODE..."

            LogMessage(
                "Exchanging authorization code for access token...")

            Using client As New HttpClient()

                Dim tokenUrl As String =
                    "https://api.upstox.com/v2/login/authorization/token"

                Dim formData As New Dictionary(Of String, String) From {
                    {"code", authorizationCode},
                    {"client_id", UPSTOX_API_KEY},
                    {"client_secret", UPSTOX_API_SECRET},
                    {"redirect_uri", UPSTOX_REDIRECT_URI},
                    {"grant_type", "authorization_code"}
                }

                Using content As New FormUrlEncodedContent(formData)

                    Dim response As HttpResponseMessage =
                        Await client.PostAsync(tokenUrl, content)

                    Dim responseText As String =
                        Await response.Content.ReadAsStringAsync()

                    If Not response.IsSuccessStatusCode Then

                        lblUpstoxStatus.Text =
                            "Upstox: TOKEN FAILED"

                        LogMessage(
                            "Token request failed: HTTP " &
                            CInt(response.StatusCode).ToString())

                        LogMessage(responseText)

                        MessageBox.Show(
                            "Upstox token request failed:" &
                            Environment.NewLine & Environment.NewLine &
                            responseText,
                            "Upstox Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)

                        Return

                    End If

                    Dim json As JsonDocument =
                        JsonDocument.Parse(responseText)

                    Dim tokenElement As JsonElement

                    If Not json.RootElement.TryGetProperty(
                        "access_token", tokenElement) Then

                        lblUpstoxStatus.Text =
                            "Upstox: TOKEN NOT RECEIVED"

                        LogMessage(
                            "Upstox did not return an access token.")

                        LogMessage(responseText)

                        MessageBox.Show(
                            "Upstox did not return an access token." &
                            Environment.NewLine & Environment.NewLine &
                            responseText,
                            "Upstox Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)

                        Return

                    End If

                    upstoxAccessToken =
                        tokenElement.GetString()

                    If String.IsNullOrWhiteSpace(upstoxAccessToken) Then

                        lblUpstoxStatus.Text =
                            "Upstox: TOKEN EMPTY"

                        MessageBox.Show(
                            "Upstox returned an empty access token.",
                            "Upstox Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)

                        Return

                    End If

                    lblUpstoxStatus.Text =
                        "Upstox: CONNECTED"

                    LogMessage(
                        "Upstox connection successful.")

                    LogMessage(
                        "Access token received successfully.")

                    MessageBox.Show(
                        "Upstox Connected Successfully!" &
                        Environment.NewLine & Environment.NewLine &
                        "The access token was received.",
                        "Upstox",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

                End Using

            End Using

        Catch ex As Exception

            lblUpstoxStatus.Text =
                "Upstox: ERROR"

            LogMessage(
                "Token exchange error: " & ex.Message)

            MessageBox.Show(
                "Error obtaining Upstox access token:" &
                Environment.NewLine & Environment.NewLine &
                ex.Message,
                "Upstox Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

    End Sub

    ' =========================================================
    ' TEST UPSTOX API
    ' =========================================================

    Private Async Sub btnTestUpstox_Click(
        sender As Object,
        e As EventArgs)

        Try

            If String.IsNullOrWhiteSpace(upstoxAccessToken) Then

                MessageBox.Show(
                    "Please connect to Upstox first.",
                    "Upstox",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            lblUpstoxStatus.Text =
                "Upstox: TESTING API..."

            LogMessage(
                "Testing Upstox Profile API...")

            Using client As New HttpClient()

                Dim profileUrl As String =
                    "https://api.upstox.com/v2/user/profile"

                client.DefaultRequestHeaders.Clear()

                client.DefaultRequestHeaders.Accept.Add(
                    New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                        "application/json"))

                client.DefaultRequestHeaders.Authorization =
                    New System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        upstoxAccessToken)

                Dim response As HttpResponseMessage =
                    Await client.GetAsync(profileUrl)

                Dim responseText As String =
                    Await response.Content.ReadAsStringAsync()

                If response.IsSuccessStatusCode Then

                    lblUpstoxStatus.Text =
                        "Upstox: API WORKING"

                    LogMessage(
                        "Upstox Profile API test successful.")

                    LogMessage(
                        "Authenticated profile data received.")

                    MessageBox.Show(
                        "Upstox API connection is working!" &
                        Environment.NewLine & Environment.NewLine &
                        "Profile data received successfully." &
                        Environment.NewLine & Environment.NewLine &
                        "No order was placed.",
                        "Upstox API Test",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

                Else

                    lblUpstoxStatus.Text =
                        "Upstox: API TEST FAILED"

                    LogMessage(
                        "Upstox Profile API test failed: HTTP " &
                        CInt(response.StatusCode).ToString())

                    LogMessage(responseText)

                    MessageBox.Show(
                        "Upstox API test failed:" &
                        Environment.NewLine & Environment.NewLine &
                        responseText,
                        "Upstox API Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)

                End If

            End Using

        Catch ex As Exception

            lblUpstoxStatus.Text =
                "Upstox: API ERROR"

            LogMessage(
                "Upstox API test error: " & ex.Message)

            MessageBox.Show(
                "Upstox API test error:" &
                Environment.NewLine & Environment.NewLine &
                ex.Message,
                "Upstox Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

    End Sub

    ' =========================================================
    ' LIVE MARKET DATA - LTP V3
    ' =========================================================

    Private Async Sub btnGetLivePrices_Click(
        sender As Object,
        e As EventArgs)

        Try

            If String.IsNullOrWhiteSpace(upstoxAccessToken) Then

                MessageBox.Show(
                    "Please connect to Upstox first.",
                    "Upstox",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            Dim stockInstrumentKey As String =
                txtLiveInstrumentKey.Text.Trim()

            If String.IsNullOrWhiteSpace(stockInstrumentKey) Then

                MessageBox.Show(
                    "Please enter a stock instrument key.",
                    "Market Data",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)

                Return

            End If

            btnGetLivePrices.Enabled = False

            LogMessage(
                "Requesting live LTP for NIFTY 50 and " &
                stockInstrumentKey & "...")

            Dim combinedKeys As String =
                NIFTY_INSTRUMENT_KEY & "," &
                stockInstrumentKey

            Dim url As String =
                "https://api.upstox.com/v3/market-quote/ltp?instrument_key=" &
                Uri.EscapeDataString(combinedKeys)

            Using client As New HttpClient()

                client.DefaultRequestHeaders.Clear()

                client.DefaultRequestHeaders.Accept.Add(
                    New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                        "application/json"))

                client.DefaultRequestHeaders.Authorization =
                    New System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        upstoxAccessToken)

                Dim response As HttpResponseMessage =
                    Await client.GetAsync(url)

                Dim responseText As String =
                    Await response.Content.ReadAsStringAsync()

                LogMessage(
                    "Upstox LTP HTTP status: " &
                    CInt(response.StatusCode).ToString())

                If Not response.IsSuccessStatusCode Then

                    LogMessage(
                        "Market LTP request failed: HTTP " &
                        CInt(response.StatusCode).ToString())

                    LogMessage(responseText)

                    MessageBox.Show(
                        "Unable to get live market data:" &
                        Environment.NewLine & Environment.NewLine &
                        responseText,
                        "Market Data Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)

                    Return

                End If

                Dim json As JsonDocument =
                    JsonDocument.Parse(responseText)

                Dim dataElement As JsonElement

                If Not json.RootElement.TryGetProperty(
                    "data", dataElement) Then

                    MessageBox.Show(
                        "Upstox did not return market data." &
                        Environment.NewLine & Environment.NewLine &
                        responseText,
                        "Market Data Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)

                    Return

                End If

                Dim niftyPrice As Double =
                    GetLtpFromData(
                        dataElement,
                        NIFTY_INSTRUMENT_KEY)

                Dim stockPrice As Double =
                    GetLtpFromData(
                        dataElement,
                        stockInstrumentKey)

                If niftyPrice > 0 Then

                    lblNiftyLive.Text =
                        "NIFTY 50: " &
                        niftyPrice.ToString("0.00")

                    ' Also update the main dashboard.
                    lblNifty.Text =
                        "Nifty: ₹" &
                        niftyPrice.ToString("0.00")

                Else

                    lblNiftyLive.Text =
                        "NIFTY 50: No live value"

                    lblNifty.Text =
                        "Nifty: NO LIVE DATA"

                End If

                If stockPrice > 0 Then

                    lblStockLive.Text =
                        "Stock LTP: " &
                        stockPrice.ToString("0.00")

                    ' Also update the main dashboard.
                    lblStock.Text =
                        "Stock: ₹" &
                        stockPrice.ToString("0.00")

                Else

                    lblStockLive.Text =
                        "Stock LTP: No live value"

                    lblStock.Text =
                        "Stock: NO LIVE DATA"

                End If

                LogMessage(
                    "NIFTY 50 LTP: " &
                    If(niftyPrice > 0,
                       niftyPrice.ToString("0.00"),
                       "Not returned"))

                LogMessage(
                    stockInstrumentKey &
                    " LTP: " &
                    If(stockPrice > 0,
                       stockPrice.ToString("0.00"),
                       "Not returned"))

                LogMessage(
                    "Live market snapshot received successfully.")

            End Using

        Catch ex As Exception

            LogMessage(
                "Live market data error: " &
                ex.Message)

            MessageBox.Show(
                "Live market data error:" &
                Environment.NewLine & Environment.NewLine &
                ex.Message,
                "Market Data Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        Finally

            If btnGetLivePrices IsNot Nothing Then
                btnGetLivePrices.Enabled = True
            End If

        End Try

    End Sub

    ' =========================================================
    ' EXTRACT LTP FROM V3 RESPONSE
    ' =========================================================

    Private Function GetLtpFromData(
        dataElement As JsonElement,
        instrumentKey As String) As Double

        ' Upstox V3 normally returns keys such as:
        ' NSE_INDEX:Nifty 50
        ' NSE_EQ:RELIANCE
        '
        ' First try the expected response key.
        Dim responseKey As String =
            instrumentKey.Replace("|"c, ":"c)

        Dim quoteElement As JsonElement

        If dataElement.TryGetProperty(
            responseKey,
            quoteElement) Then

            Dim priceElement As JsonElement

            If quoteElement.TryGetProperty(
                "last_price",
                priceElement) Then

                If priceElement.ValueKind =
                   JsonValueKind.Number Then

                    Return priceElement.GetDouble()

                End If

            End If

        End If

        ' Fallback: search all returned instruments by
        ' instrument_token. This makes the code more robust
        ' if the response key format changes.
        For Each propertyItem As JsonProperty
            In dataElement.EnumerateObject()

            Dim item As JsonElement =
                propertyItem.Value

            Dim tokenElement As JsonElement

            If item.TryGetProperty(
                "instrument_token",
                tokenElement) Then

                If tokenElement.ValueKind =
                   JsonValueKind.String Then

                    If String.Equals(
                        tokenElement.GetString(),
                        instrumentKey,
                        StringComparison.OrdinalIgnoreCase) Then

                        Dim priceElement As JsonElement

                        If item.TryGetProperty(
                            "last_price",
                            priceElement) Then

                            If priceElement.ValueKind =
                               JsonValueKind.Number Then

                                Return priceElement.GetDouble()

                            End If

                        End If

                    End If

                End If

            End If

        Next

        Return 0D

    End Function

    ' =========================================================
    ' LOAD CSV BUTTON
    ' =========================================================

    Private Sub btnLoadCSV_Click(
        sender As Object,
        e As EventArgs)

        Dim dialog As New OpenFileDialog()

        dialog.Filter =
            "CSV files (*.csv)|*.csv|All files (*.*)|*.*"

        dialog.Title =
            "Select Market Data CSV"

        If dialog.ShowDialog() <>
           DialogResult.OK Then

            Return

        End If

        Try

            LoadCSV(
                dialog.FileName)

        Catch ex As Exception

            MessageBox.Show(
                "Error loading CSV:" &
                Environment.NewLine &
                ex.Message,
                "CSV Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

    End Sub

    ' =========================================================
    ' LOAD CSV
    ' =========================================================

    Private Sub LoadCSV(
        filePath As String)

        loadedCandles.Clear()

        csvLoaded = False

        Dim lines() As String =
            File.ReadAllLines(filePath)

        If lines.Length <= 1 Then

            MessageBox.Show(
                "The CSV does not contain market data.",
                "CSV Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

            Return

        End If

        LogMessage(
            "CSV HEADER:")

        LogMessage(
            lines(0))

        ' -----------------------------------------------------
        ' READ CANDLES
        ' -----------------------------------------------------

        For i As Integer =
            1 To lines.Length - 1

            If String.IsNullOrWhiteSpace(
                lines(i)) Then

                Continue For

            End If

            Dim parts() As String =
                lines(i).Split(","c)

            If parts.Length < 9 Then

                Continue For

            End If

            Dim candle As New Candle()

            candle.DateValue =
                parts(0).Trim()

            candle.Symbol =
                parts(1).Trim()

            candle.Exchange =
                parts(2).Trim()

            candle.Interval =
                parts(3).Trim()

            candle.Open =
                ParseDouble(
                    parts(4))

            candle.High =
                ParseDouble(
                    parts(5))

            candle.Low =
                ParseDouble(
                    parts(6))

            candle.Close =
                ParseDouble(
                    parts(7))

            candle.Volume =
                ParseLong(
                    parts(8))

            loadedCandles.Add(
                candle)

        Next

        csvFileName =
            Path.GetFileName(
                filePath)

        csvLoaded =
            loadedCandles.Count > 0

        If Not csvLoaded Then

            LogMessage(
                "No valid candles found.")

            Return

        End If

        lblCSVStatus.Text =
            "CSV: " &
            loadedCandles.Count.ToString() &
            " candle(s)"

        LogMessage("--------------------------------")

        LogMessage(
            "CSV LOADED")

        LogMessage(
            "File: " &
            csvFileName)

        LogMessage(
            "Candles Loaded: " &
            loadedCandles.Count.ToString())

        ' -----------------------------------------------------
        ' FIND STOCKS
        ' -----------------------------------------------------

        Dim stockNames As New List(Of String)

        For Each candle As Candle In
            loadedCandles

            If Not stockNames.Contains(
                candle.Symbol) Then

                stockNames.Add(
                    candle.Symbol)

            End If

        Next

        LogMessage(
            "Symbols Found: " &
            stockNames.Count.ToString())

        For Each stockName As String
            In stockNames

            LogMessage(
                "  " &
                stockName)

        Next

        LogMessage("--------------------------------")

    End Sub

    ' =========================================================
    ' PARSE DOUBLE
    ' =========================================================

    Private Function ParseDouble(
        value As String) As Double

        Dim result As Double

        If Double.TryParse(
            value.Trim(),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            result) Then

            Return result

        End If

        Return 0D

    End Function

    ' =========================================================
    ' PARSE LONG
    ' =========================================================

    Private Function ParseLong(
        value As String) As Long

        Dim result As Long

        If Long.TryParse(
            value.Trim(),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            result) Then

            Return result

        End If

        Return 0

    End Function

    ' =========================================================
    ' GET STOCK CANDLES
    ' =========================================================

    Private Function GetStockCandles(
        stockName As String) As List(Of Candle)

        Dim result As New List(Of Candle)

        For Each candle As Candle In
            loadedCandles

            If candle.Symbol =
               stockName Then

                result.Add(
                    candle)

            End If

        Next

        Return result

    End Function

    ' =========================================================
    ' SORT CANDLES
    ' =========================================================

    Private Function SortCandles(
        candles As List(Of Candle)) As List(Of Candle)

        Dim result As New List(Of Candle)

        For Each candle As Candle
            In candles

            result.Add(
                candle)

        Next

        For i As Integer =
            0 To result.Count - 2

            For j As Integer =
                i + 1 To result.Count - 1

                If String.Compare(
                    result(i).DateValue,
                    result(j).DateValue,
                    StringComparison.Ordinal) > 0 Then

                    Dim temp As Candle =
                        result(i)

                    result(i) =
                        result(j)

                    result(j) =
                        temp

                End If

            Next

        Next

        Return result

    End Function

    ' =========================================================
    ' GET NIFTY CANDLES
    ' =========================================================

    Private Function GetNiftyCandles() As List(Of Candle)

        Dim result As New List(Of Candle)

        For Each candle As Candle
            In loadedCandles

            Dim symbol As String =
                candle.Symbol.ToUpper()

            If symbol = "NIFTY" OrElse
               symbol = "NIFTY50" OrElse
               symbol = "NIFTY 50" Then

                result.Add(
                    candle)

            End If

        Next

        Return result

    End Function

    ' =========================================================
    ' CALCULATE EMA
    ' =========================================================

    Private Function CalculateEMA(
        candles As List(Of Candle),
        period As Integer) As Double

        If candles Is Nothing OrElse
           candles.Count < period Then

            Return 0D

        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(candles)

        Dim multiplier As Double =
            2D / (period + 1D)

        Dim ema As Double = 0D

        ' Initial SMA

        For i As Integer =
            0 To period - 1

            ema +=
                sortedCandles(i).Close

        Next

        ema =
            ema / period

        ' EMA

        For i As Integer =
            period To sortedCandles.Count - 1

            ema =
                (
                    (sortedCandles(i).Close - ema) *
                    multiplier
                ) + ema

        Next

        Return ema

    End Function

    ' =========================================================
    ' CALCULATE VWAP
    ' =========================================================

    Private Function CalculateVWAP(
        candles As List(Of Candle)) As Double

        If candles Is Nothing OrElse
           candles.Count = 0 Then

            Return 0D

        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(candles)

        Dim totalPV As Double = 0D
        Dim totalVolume As Double = 0D

        For Each candle As Candle
            In sortedCandles

            If candle.Volume > 0 Then

                Dim typicalPrice As Double =
                    (
                        candle.High +
                        candle.Low +
                        candle.Close
                    ) / 3D

                totalPV +=
                    typicalPrice *
                    CDbl(candle.Volume)

                totalVolume +=
                    CDbl(candle.Volume)

            End If

        Next

        If totalVolume <= 0 Then

            Return 0D

        End If

        Return totalPV /
               totalVolume

    End Function

    ' =========================================================
    ' NIFTY MARKET TREND
    ' =========================================================

    Private Function GetNiftyMarketTrend(
        ByRef lastClose As Double,
        ByRef ema9 As Double,
        ByRef ema21 As Double,
        ByRef vwap As Double) As String

        lastClose = 0D
        ema9 = 0D
        ema21 = 0D
        vwap = 0D

        Dim niftyCandles As List(Of Candle) =
            GetNiftyCandles()

        If niftyCandles.Count <
           SLOW_EMA_PERIOD Then

            Return "NO DATA"

        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(niftyCandles)

        lastClose =
            sortedCandles(
                sortedCandles.Count - 1
            ).Close

        ema9 =
            CalculateEMA(
                sortedCandles,
                FAST_EMA_PERIOD)

        ema21 =
            CalculateEMA(
                sortedCandles,
                SLOW_EMA_PERIOD)

        vwap =
            CalculateVWAP(
                sortedCandles)

        ' -----------------------------------------------------
        ' BULLISH
        ' -----------------------------------------------------

        If ema9 > ema21 AndAlso
           lastClose > ema9 AndAlso
           vwap > 0 AndAlso
           lastClose > vwap Then

            Return "BULLISH"

        End If

        ' -----------------------------------------------------
        ' BEARISH
        ' -----------------------------------------------------

        If ema9 < ema21 AndAlso
           lastClose < ema9 AndAlso
           vwap > 0 AndAlso
           lastClose < vwap Then

            Return "BEARISH"

        End If

        ' -----------------------------------------------------
        ' NEUTRAL
        ' -----------------------------------------------------

        Return "NEUTRAL"

    End Function

    ' =========================================================
    ' BULLISH STOCK
    ' =========================================================

    Private Function IsBullishStock(
        candles As List(Of Candle)) As Boolean

        If candles Is Nothing OrElse
           candles.Count < 3 Then

            Return False

        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(candles)

        Dim c1 As Candle =
            sortedCandles(
                sortedCandles.Count - 3)

        Dim c2 As Candle =
            sortedCandles(
                sortedCandles.Count - 2)

        Dim c3 As Candle =
            sortedCandles(
                sortedCandles.Count - 1)

        Dim higherHigh As Boolean =
            c3.High > c2.High AndAlso
            c2.High > c1.High

        Dim higherLow As Boolean =
            c3.Low > c2.Low AndAlso
            c2.Low > c1.Low

        Dim higherClose As Boolean =
            c3.Close > c2.Close AndAlso
            c2.Close > c1.Close

        Return higherHigh AndAlso
               higherLow AndAlso
               higherClose

    End Function

    ' =========================================================
    ' BEARISH STOCK
    ' =========================================================

    Private Function IsBearishStock(
        candles As List(Of Candle)) As Boolean

        If candles Is Nothing OrElse
           candles.Count < 3 Then

            Return False

        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(candles)

        Dim c1 As Candle =
            sortedCandles(
                sortedCandles.Count - 3)

        Dim c2 As Candle =
            sortedCandles(
                sortedCandles.Count - 2)

        Dim c3 As Candle =
            sortedCandles(
                sortedCandles.Count - 1)

        Dim lowerHigh As Boolean =
            c3.High < c2.High AndAlso
            c2.High < c1.High

        Dim lowerLow As Boolean =
            c3.Low < c2.Low AndAlso
            c2.Low < c1.Low

        Dim lowerClose As Boolean =
            c3.Close < c2.Close AndAlso
            c2.Close < c1.Close

        Return lowerHigh AndAlso
               lowerLow AndAlso
               lowerClose

    End Function

    ' =========================================================
    ' CONSOLIDATION
    ' =========================================================

    Private Function IsConsolidating(
        candles As List(Of Candle)) As Boolean

        If candles Is Nothing OrElse
           candles.Count <
           CONSOLIDATION_CANDLES Then

            Return False

        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(candles)

        Dim startIndex As Integer =
            sortedCandles.Count -
            CONSOLIDATION_CANDLES

        Dim highestHigh As Double =
            Double.MinValue

        Dim lowestLow As Double =
            Double.MaxValue

        Dim totalClose As Double = 0D

        For i As Integer =
            startIndex To sortedCandles.Count - 1

            Dim candle As Candle =
                sortedCandles(i)

            If candle.High > highestHigh Then

                highestHigh =
                    candle.High

            End If

            If candle.Low < lowestLow Then

                lowestLow =
                    candle.Low

            End If

            totalClose +=
                candle.Close

        Next

        Dim averageClose As Double =
            totalClose /
            CDbl(CONSOLIDATION_CANDLES)

        If averageClose <= 0 Then

            Return False

        End If

        Dim priceRange As Double =
            highestHigh -
            lowestLow

        Dim rangePercent As Double =
            (
                priceRange /
                averageClose
            ) * 100D

        Return rangePercent <=
               CONSOLIDATION_RANGE_PERCENT

    End Function

    ' =========================================================
    ' CONSOLIDATION RANGE
    ' =========================================================

    Private Function GetConsolidationRangePercent(
        candles As List(Of Candle)) As Double

        If candles Is Nothing OrElse
           candles.Count <
           CONSOLIDATION_CANDLES Then

            Return 0D

        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(candles)

        Dim startIndex As Integer =
            sortedCandles.Count -
            CONSOLIDATION_CANDLES

        Dim highestHigh As Double =
            Double.MinValue

        Dim lowestLow As Double =
            Double.MaxValue

        Dim totalClose As Double = 0D

        For i As Integer =
            startIndex To sortedCandles.Count - 1

            Dim candle As Candle =
                sortedCandles(i)

            If candle.High > highestHigh Then

                highestHigh =
                    candle.High

            End If

            If candle.Low < lowestLow Then

                lowestLow =
                    candle.Low

            End If

            totalClose +=
                candle.Close

        Next

        Dim averageClose As Double =
            totalClose /
            CDbl(CONSOLIDATION_CANDLES)

        If averageClose <= 0 Then

            Return 0D

        End If

        Dim priceRange As Double =
            highestHigh -
            lowestLow

        Return (
            priceRange /
            averageClose
        ) * 100D

    End Function

    ' =========================================================
    ' BREAKOUT RESULT
    ' =========================================================

    Private Class BreakoutResult

        Public Property Found As Boolean
        Public Property Entry As Double
        Public Property StopLoss As Double
        Public Property ConsolidationHigh As Double
        Public Property ConsolidationLow As Double
        Public Property BreakoutVolume As Long
        Public Property AverageVolume As Double
        Public Property BreakoutDate As String

    End Class

    ' =========================================================
    ' FIND BREAKOUT
    ' =========================================================

    Private Function FindBreakout(
        candles As List(Of Candle)) As BreakoutResult

        Dim result As New BreakoutResult()

        If candles Is Nothing Then
            Return result
        End If

        Dim sortedCandles As List(Of Candle) =
            SortCandles(candles)

        ' We need:
        ' 3 candles for consolidation +
        ' 1 breakout candle +
        ' 3 previous candles for volume comparison.
        If sortedCandles.Count <
           CONSOLIDATION_CANDLES + 1 Then

            Return result

        End If

        ' Scan chronologically. We never use a future candle
        ' to decide whether an earlier candle was a breakout.
        For breakoutIndex As Integer = CONSOLIDATION_CANDLES To sortedCandles.Count - 1

            Dim rangeStart As Integer =
                breakoutIndex -
                CONSOLIDATION_CANDLES

            Dim highestHigh As Double =
                Double.MinValue

            Dim lowestLow As Double =
                Double.MaxValue

            Dim totalRangeClose As Double = 0D

            For i As Integer = rangeStart To breakoutIndex - 1

                Dim c As Candle =
                    sortedCandles(i)

                If c.High > highestHigh Then
                    highestHigh = c.High
                End If

                If c.Low < lowestLow Then
                    lowestLow = c.Low
                End If

                totalRangeClose += c.Close

            Next

            Dim averageRangeClose As Double =
                totalRangeClose /
                CDbl(CONSOLIDATION_CANDLES)

            If averageRangeClose <= 0D Then
                Continue For
            End If

            Dim rangePercent As Double =
                ((highestHigh - lowestLow) /
                 averageRangeClose) * 100D

            If rangePercent >
               CONSOLIDATION_RANGE_PERCENT Then

                Continue For

            End If

            Dim breakoutCandle As Candle =
                sortedCandles(breakoutIndex)

            ' Breakout must close above the previous
            ' consolidation high.
            If breakoutCandle.Close <= highestHigh Then
                Continue For
            End If

            ' Volume confirmation:
            ' breakout volume must be greater than the
            ' average volume of the candles immediately
            ' before the breakout.
            Dim volumeCount As Integer =
                Math.Min(
                    BREAKOUT_VOLUME_LOOKBACK,
                    breakoutIndex)

            If volumeCount <= 0 Then
                Continue For
            End If

            Dim volumeTotal As Double = 0D

            For i As Integer = breakoutIndex - volumeCount To breakoutIndex - 1

                volumeTotal +=
                    CDbl(sortedCandles(i).Volume)

            Next

            Dim averageVolume As Double =
                volumeTotal /
                CDbl(volumeCount)

            If averageVolume <= 0D Then
                Continue For
            End If

            If CDbl(breakoutCandle.Volume) <=
               averageVolume Then

                Continue For

            End If

            result.Found = True

            ' Entry is the confirmed breakout candle close.
            result.Entry =
                breakoutCandle.Close

            ' Stop is below the consolidation low.
            result.StopLoss =
                lowestLow

            result.ConsolidationHigh =
                highestHigh

            result.ConsolidationLow =
                lowestLow

            result.BreakoutVolume =
                breakoutCandle.Volume

            result.AverageVolume =
                averageVolume

            result.BreakoutDate =
                breakoutCandle.DateValue

            Return result

        Next

        Return result

    End Function

    ' =========================================================
    ' START MARKET SCAN
    ' =========================================================

    Private Sub btnStart_Click(
        sender As Object,
        e As EventArgs)

        If Not csvLoaded Then

            MessageBox.Show(
                "Please load a CSV file first.",
                "Market Scanner",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

            Return

        End If

        txtLog.Clear()

        LogMessage(
            "================================")

        LogMessage(
            "ALGO TRADING STUDIO")

        LogMessage(
            "MARKET FILTER + STOCK SCAN")

        LogMessage(
            "================================")

        ' =====================================================
        ' STAGE 1
        ' NIFTY MARKET FILTER / TEST MODE
        ' =====================================================

        LogMessage("")

        If chkTestMode.Checked Then

            ' TEST MODE intentionally skips the NIFTY filter.
            ' This is for development only when NIFTY candles
            ' are not available in the CSV.

            lblNifty.Text =
                "Nifty: TEST MODE"

            LogMessage(
                "TEST MODE ENABLED")

            LogMessage(
                "NIFTY MARKET FILTER SKIPPED")

            LogMessage(
                "This mode is for development/testing only.")

            lblTradeStatus.Text =
                "Trade Status: TEST MODE"

        Else

            LogMessage(
                "================================")

            LogMessage(
                "NIFTY MARKET FILTER")

            LogMessage(
                "================================")

            Dim niftyClose As Double = 0D
            Dim niftyEMA9 As Double = 0D
            Dim niftyEMA21 As Double = 0D
            Dim niftyVWAP As Double = 0D

            Dim niftyTrend As String =
                GetNiftyMarketTrend(
                    niftyClose,
                    niftyEMA9,
                    niftyEMA21,
                    niftyVWAP)

            If niftyTrend = "NO DATA" Then

                lblNifty.Text =
                    "Nifty: NO DATA"

                lblStock.Text =
                    "Stock: ---"

                lblSignal.Text =
                    "Signal: WAITING"

                lblTradeStatus.Text =
                    "Trade Status: MARKET DATA REQUIRED"

                LogMessage(
                    "NIFTY DATA NOT FOUND")

                LogMessage(
                    "At least 21 NIFTY candles are required.")

                LogMessage(
                    "LIVE MODE SCAN STOPPED")

                Return

            End If

            LogMessage(
                "NIFTY CLOSE: " &
                niftyClose.ToString("0.00"))

            LogMessage(
                "NIFTY EMA 9: " &
                niftyEMA9.ToString("0.00"))

            LogMessage(
                "NIFTY EMA 21: " &
                niftyEMA21.ToString("0.00"))

            If niftyVWAP > 0 Then

                LogMessage(
                    "NIFTY VWAP: " &
                    niftyVWAP.ToString("0.00"))

            Else

                LogMessage(
                    "NIFTY VWAP: NOT AVAILABLE")

            End If

            LogMessage("")

            LogMessage(
                "NIFTY TREND: " &
                niftyTrend)

            lblNifty.Text =
                "Nifty: " &
                niftyTrend

            If niftyTrend <> "BULLISH" Then

                lblStock.Text =
                    "Stock: ---"

                If niftyTrend = "BEARISH" Then

                    lblSignal.Text =
                        "Signal: NO LONG BUY"

                    lblTradeStatus.Text =
                        "Trade Status: MARKET BEARISH"

                    LogMessage("")

                    LogMessage(
                        "NIFTY IS BEARISH")

                    LogMessage(
                        "LONG BUY SIGNALS BLOCKED")

                Else

                    lblSignal.Text =
                        "Signal: WAITING"

                    lblTradeStatus.Text =
                        "Trade Status: MARKET NEUTRAL"

                    LogMessage("")

                    LogMessage(
                        "NIFTY IS NEUTRAL")

                    LogMessage(
                        "WAITING FOR MARKET CONFIRMATION")

                End If

                LogMessage("")

                LogMessage(
                    "================================")

                LogMessage(
                    "SCAN STOPPED BY MARKET FILTER")

                LogMessage(
                    "================================")

                Return

            End If

            LogMessage("")

            LogMessage(
                "NIFTY BULLISH")

            LogMessage(
                "STOCK SCAN ENABLED")

        End If

        If chkTestMode.Checked Then

            LogMessage("")

            LogMessage(
                "================================")

            LogMessage(
                "TEST MODE - STOCK SCAN ENABLED")

            LogMessage(
                "================================")

        End If

        ' =====================================================
        ' STAGE 2
        ' GET UNIQUE STOCKS
        ' =====================================================

        Dim stockNames As New List(Of String)

        For Each candle As Candle
            In loadedCandles

            Dim symbol As String =
                candle.Symbol.ToUpper()

            If symbol <> "NIFTY" AndAlso
               symbol <> "NIFTY50" AndAlso
               symbol <> "NIFTY 50" Then

                If Not stockNames.Contains(
                    candle.Symbol) Then

                    stockNames.Add(
                        candle.Symbol)

                End If

            End If

        Next

        ' =====================================================
        ' RESULT LISTS
        ' =====================================================

        Dim bullishStocks As New List(Of String)

        Dim bearishStocks As New List(Of String)

        Dim neutralStocks As New List(Of String)

        Dim consolidatingStocks As New List(Of String)

        ' =====================================================
        ' STAGE 3
        ' STOCK TREND
        ' =====================================================

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "STOCK MARKET SCAN")

        LogMessage(
            "================================")

        For Each stockName As String
            In stockNames

            Dim stockCandles As List(Of Candle) =
                GetStockCandles(
                    stockName)

            If stockCandles.Count < 3 Then

                neutralStocks.Add(
                    stockName)

            ElseIf IsBullishStock(
                stockCandles) Then

                bullishStocks.Add(
                    stockName)

            ElseIf IsBearishStock(
                stockCandles) Then

                bearishStocks.Add(
                    stockName)

            Else

                neutralStocks.Add(
                    stockName)

            End If

        Next

        ' =====================================================
        ' BULLISH STOCKS
        ' =====================================================

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "BULLISH STOCKS")

        LogMessage(
            "================================")

        If bullishStocks.Count = 0 Then

            LogMessage("None")

        Else

            For Each stockName As String
                In bullishStocks

                LogMessage(
                    stockName &
                    " → BULLISH")

            Next

        End If

        ' =====================================================
        ' BEARISH STOCKS
        ' =====================================================

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "BEARISH STOCKS")

        LogMessage(
            "================================")

        If bearishStocks.Count = 0 Then

            LogMessage("None")

        Else

            For Each stockName As String
                In bearishStocks

                LogMessage(
                    stockName &
                    " → BEARISH")

            Next

        End If

        ' =====================================================
        ' NEUTRAL
        ' =====================================================

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "NEUTRAL STOCKS")

        LogMessage(
            "================================")

        If neutralStocks.Count = 0 Then

            LogMessage("None")

        Else

            For Each stockName As String
                In neutralStocks

                LogMessage(
                    stockName &
                    " → NEUTRAL")

            Next

        End If

        ' =====================================================
        ' STAGE 4
        ' CONSOLIDATION
        ' =====================================================

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "CONSOLIDATION CHECK")

        LogMessage(
            "================================")

        For Each stockName As String
            In bullishStocks

            Dim stockCandles As List(Of Candle) =
                GetStockCandles(
                    stockName)

            Dim rangePercent As Double =
                GetConsolidationRangePercent(
                    stockCandles)

            Dim consolidationFound As Boolean =
                IsConsolidating(
                    stockCandles)

            If consolidationFound Then

                consolidatingStocks.Add(
                    stockName)

                LogMessage(
                    stockName &
                    " → CONSOLIDATION: YES")

                LogMessage(
                    "Range: " &
                    rangePercent.ToString("0.00") &
                    "%")

            Else

                LogMessage(
                    stockName &
                    " → CONSOLIDATION: NO")

                LogMessage(
                    "Range: " &
                    rangePercent.ToString("0.00") &
                    "%")

            End If

        Next

        ' =====================================================
        ' STAGE 5
        ' BREAKOUT DETECTION
        ' =====================================================

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "BREAKOUT DETECTION")

        LogMessage(
            "================================")

        Dim breakoutFound As Boolean = False

        If consolidatingStocks.Count = 0 Then

            LogMessage(
                "NO STOCKS READY FOR BREAKOUT")

            lblStock.Text =
                "Stock: ---"

            lblSignal.Text =
                "Signal: WAITING"

            lblTradeStatus.Text =
                "Trade Status: NO SETUP"

        Else

            For Each stockName As String
                In consolidatingStocks

                Dim stockCandles As List(Of Candle) =
                    GetStockCandles(stockName)

                Dim breakout As BreakoutResult =
                    FindBreakout(stockCandles)

                If breakout.Found Then

                    breakoutFound = True

                    LogMessage("")

                    LogMessage(
                        stockName &
                        " → BREAKOUT CONFIRMED")

                    LogMessage(
                        "Breakout Close: ₹" &
                        breakout.Entry.ToString("0.00"))

                    LogMessage(
                        "Consolidation High: ₹" &
                        breakout.ConsolidationHigh.ToString("0.00"))

                    LogMessage(
                        "Consolidation Low: ₹" &
                        breakout.ConsolidationLow.ToString("0.00"))

                    LogMessage(
                        "Breakout Volume: " &
                        breakout.BreakoutVolume.ToString())

                    LogMessage(
                        "Average Volume: " &
                        breakout.AverageVolume.ToString("0"))

                    LogMessage(
                        "Breakout Time: " &
                        breakout.BreakoutDate)

                    GenerateBuySignal(
                        stockName,
                        breakout.Entry,
                        breakout.StopLoss)

                    Exit For

                Else

                    LogMessage(
                        stockName &
                        " → NO CONFIRMED BREAKOUT")

                End If

            Next

            If Not breakoutFound Then

                Dim selectedStock As String =
                    consolidatingStocks(0)

                lblStock.Text =
                    "Stock: " &
                    selectedStock

                lblSignal.Text =
                    "Signal: BREAKOUT WATCH"

                lblTradeStatus.Text =
                    "Trade Status: WATCHING"

                LogMessage("")

                LogMessage(
                    "SELECTED STOCK: " &
                    selectedStock)

                LogMessage(
                    "WAITING FOR BREAKOUT")

                LogMessage(
                    "WAITING FOR VOLUME CONFIRMATION")

                LogMessage(
                    "NO BUY YET")

            End If

        End If

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "SCAN COMPLETE")

        LogMessage(
            "================================")

    End Sub

    ' =========================================================
    ' STOP
    ' =========================================================

    Private Sub btnStop_Click(
        sender As Object,
        e As EventArgs)

        LogMessage(
            "SCAN STOPPED")

        lblTradeStatus.Text =
            "Trade Status: STOPPED"

    End Sub

    ' =========================================================
    ' NEXT PRICE
    ' =========================================================

    Private Sub btnSimulate_Click(
    sender As Object,
    e As EventArgs)

        If Not tradeActive Then

            LogMessage(
            "No active trade.")

            Return

        End If

        ' ---------------------------------------------------------
        ' TEST PRICE SEQUENCE
        '
        ' Click 1 → ₹2135
        ' Click 2 → ₹2144  (+1R / CTC)
        ' Click 3 → ₹2135  (reversal)
        ' Click 4 → ₹2125  (cost-to-cost exit)
        ' ---------------------------------------------------------

        If currentPrice = 0 Then

            currentPrice = 2135D

        ElseIf currentPrice = 2135D AndAlso
           Not ctcActivated Then

            currentPrice = 2144D

        ElseIf currentPrice = 2144D AndAlso
           ctcActivated Then

            currentPrice = 2135D

        ElseIf currentPrice = 2135D AndAlso
           ctcActivated Then

            currentPrice = 2125D

        Else

            LogMessage(
            "TEST PRICE SEQUENCE COMPLETE")

            Return

        End If

        LogMessage(
        "Current Price: ₹" &
        currentPrice.ToString("0.00"))

        ManageTrade()

    End Sub

    ' =========================================================
    ' CURRENT TRADE QUANTITY
    ' =========================================================

    Private Function GetCurrentTradeQuantity() As Integer
        Return tradeQuantity
    End Function

    ' =========================================================
    ' MANAGE TRADE
    ' =========================================================

    Private Sub ManageTrade()

        If Not tradeActive Then
            Return
        End If

        ' =====================================================
        ' TARGET HIT
        ' =====================================================

        If currentPrice >= targetPrice Then

            lastExitReason = "TARGET HIT"
            lastExitPrice = currentPrice

            lastTradeProfit =
            (lastExitPrice - entryPrice) *
            GetCurrentTradeQuantity()

            If risk > 0 Then
                lastTradeProfitR =
                (lastExitPrice - entryPrice) /
                risk
            Else
                lastTradeProfitR = 0D
            End If

            LogMessage("================================")
            LogMessage("TARGET HIT")
            LogMessage("EXIT PRICE: ₹" & lastExitPrice.ToString("0.00"))
            LogMessage("RESULT: +" & lastTradeProfitR.ToString("0.00") & "R")
            LogMessage("PROFIT: ₹" & lastTradeProfit.ToString("0.00"))
            LogMessage("================================")

            ExitTrade()

            Return

        End If


        ' =====================================================
        ' CTC / COST TO COST
        ' =====================================================

        If Not ctcActivated AndAlso
       currentPrice >= ctcPrice Then

            ctcActivated = True

            currentStopLoss =
            entryPrice

            lblSL.Text =
            "Stop Loss: ₹" &
            currentStopLoss.ToString("0.00")

            lblTradeStatus.Text =
            "Trade Status: CTC ACTIVE"

            LogMessage("+1R REACHED")
            LogMessage("SL MOVED TO COST")

        End If


        ' =====================================================
        ' STOP LOSS
        ' =====================================================

        If currentPrice <= currentStopLoss Then

            lastExitReason =
            If(ctcActivated,
               "CTC EXIT",
               "STOP LOSS HIT")

            lastExitPrice =
            currentPrice

            lastTradeProfit =
            (lastExitPrice - entryPrice) *
            GetCurrentTradeQuantity()

            If risk > 0 Then

                lastTradeProfitR =
                (lastExitPrice - entryPrice) /
                risk

            Else

                lastTradeProfitR = 0D

            End If

            LogMessage("================================")
            LogMessage(lastExitReason)
            LogMessage("EXIT PRICE: ₹" &
                   lastExitPrice.ToString("0.00"))

            LogMessage("RESULT: " &
                   lastTradeProfitR.ToString("0.00") &
                   "R")

            LogMessage("P&L: ₹" &
                   lastTradeProfit.ToString("0.00"))

            LogMessage("================================")

            ExitTrade()

            Return

        End If

    End Sub

    ' =========================================================
    ' EXIT
    ' =========================================================

    Private Sub ExitTrade()

        tradeActive = False

        lblTradeStatus.Text =
            "Trade Status: CLOSED"

        If lblQuantity IsNot Nothing Then
            lblQuantity.Text =
                "Quantity: ---"
        End If

        If lblPositionValue IsNot Nothing Then
            lblPositionValue.Text =
                "Position Value: ---"
        End If

        If lblRiskAmount IsNot Nothing Then
            lblRiskAmount.Text =
                "Risk Amount: ---"
        End If

    End Sub

    ' =========================================================
    ' BUY SIGNAL
    ' =========================================================

    Private Sub GenerateBuySignal(
        stock As String,
        price As Double,
        setupStopLoss As Double)

        entryPrice =
            price

        stopLoss =
            setupStopLoss

        ' Safety: the stop must be below the entry for a long trade.
        If stopLoss >= entryPrice Then

            LogMessage(
                "BUY CANCELLED: INVALID STOP LOSS")

            lblSignal.Text =
                "Signal: WAITING"

            lblTradeStatus.Text =
                "Trade Status: INVALID SETUP"

            Return

        End If

        currentStopLoss =
            stopLoss

        risk =
            entryPrice -
            stopLoss

        ctcPrice =
            entryPrice +
            risk

        targetPrice =
            entryPrice +
            (risk * 1.5D)

        ' Capital-based quantity.
        Dim capitalQuantity As Integer =
            CInt(
                Math.Floor(
                    CAPITAL /
                    entryPrice))

        ' Risk-based quantity using 1% of ₹50,000.
        Dim maxRiskAmount As Double =
            CAPITAL *
            (MAX_RISK_PERCENT / 100D)

        Dim riskQuantity As Integer =
            CInt(
                Math.Floor(
                    maxRiskAmount /
                    risk))

        ' Use the smaller quantity so both capital and
        ' the configured maximum risk are respected.
        Dim quantity As Integer =
            Math.Min(
                capitalQuantity,
                riskQuantity)

        If quantity < 1 Then

            LogMessage(
                "BUY CANCELLED: POSITION SIZE < 1 SHARE")

            lblSignal.Text =
                "Signal: WAITING"

            lblTradeStatus.Text =
                "Trade Status: RISK TOO HIGH"

            Return

        End If

        Dim positionValue As Double =
            quantity *
            entryPrice

        Dim riskAmount As Double =
            quantity *
            risk

        ' Store the quantity so ManageTrade can calculate the actual
        ' rupee profit/loss when this trade is closed.
        tradeQuantity = quantity

        tradeActive = True

        ctcActivated = False

        lblStock.Text =
            "Stock: " &
            stock

        lblSignal.Text =
            "Signal: BUY"

        lblEntry.Text =
            "Entry: ₹" &
            entryPrice.ToString("0.00")

        lblSL.Text =
            "Stop Loss: ₹" &
            currentStopLoss.ToString("0.00")

        lblCTC.Text =
            "Cost-to-Cost: ₹" &
            ctcPrice.ToString("0.00")

        lblTarget.Text =
            "Target 1.5R: ₹" &
            targetPrice.ToString("0.00")

        lblQuantity.Text =
            "Quantity: " &
            quantity.ToString()

        lblPositionValue.Text =
            "Position Value: ₹" &
            positionValue.ToString("0.00")

        lblRiskAmount.Text =
            "Risk Amount: ₹" &
            riskAmount.ToString("0.00")

        lblTradeStatus.Text =
            "Trade Status: ACTIVE"

        LogMessage(
            "BUY SIGNAL")

        LogMessage(
            "ENTRY: ₹" &
            entryPrice.ToString("0.00"))

        LogMessage(
            "SL: ₹" &
            stopLoss.ToString("0.00"))

        LogMessage(
            "RISK/SHARE: ₹" &
            risk.ToString("0.00"))

        LogMessage(
            "CTC: ₹" &
            ctcPrice.ToString("0.00"))

        LogMessage(
            "TARGET 1.5R: ₹" &
            targetPrice.ToString("0.00"))

        LogMessage(
            "CAPITAL: ₹" &
            CAPITAL.ToString("0.00"))

        LogMessage(
            "QUANTITY: " &
            quantity.ToString())

        LogMessage(
            "POSITION VALUE: ₹" &
            positionValue.ToString("0.00"))

        LogMessage(
            "RISK AMOUNT: ₹" &
            riskAmount.ToString("0.00"))

    End Sub

    ' =========================================================
    ' BACKTEST BUTTON
    ' =========================================================

    Private Sub btnBacktest_Click(
        sender As Object,
        e As EventArgs)

        If Not csvLoaded Then

            MessageBox.Show(
                "Please load a CSV file first.",
                "Backtest",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

            Return

        End If

        RunBacktest()

    End Sub

    ' =========================================================
    ' SIMPLE BACKTEST
    ' =========================================================

    Private Sub RunBacktest()

        backtestTotalTrades = 0
        backtestWins = 0
        backtestLosses = 0
        backtestCTC = 0
        backtestTotalR = 0

        txtLog.Clear()

        LogMessage(
            "================================")

        LogMessage(
            "CSV BACKTEST STARTED")

        LogMessage(
            "================================")

        LogMessage(
            "File: " &
            csvFileName)

        LogMessage(
            "Candles: " &
            loadedCandles.Count.ToString())

        LogMessage("")

        LogMessage(
            "Note: Full NIFTY-filtered historical")
        LogMessage(
            "backtest will be added after the")
        LogMessage(
            "market filter is validated.")

        LogMessage("")

        LogMessage(
            "================================")

        LogMessage(
            "BACKTEST RESULTS")

        LogMessage(
            "================================")

        LogMessage(
            "Total Trades: " &
            backtestTotalTrades.ToString())

        LogMessage(
            "Wins: " &
            backtestWins.ToString())

        LogMessage(
            "Losses: " &
            backtestLosses.ToString())

        LogMessage(
            "CTC Events: " &
            backtestCTC.ToString())

        LogMessage(
            "Total R: " &
            backtestTotalR.ToString("0.00"))

        LogMessage(
            "================================")

    End Sub

    ' =========================================================
    ' NIFTY OPTIONS SIGNAL - SIGNAL ONLY, NO ORDER PLACEMENT
    ' Strategy: 9 EMA / 21 EMA / today's futures VWAP + breakout
    ' =========================================================

    Private Async Sub btnNiftyOptionSignal_Click(
        sender As Object,
        e As EventArgs)

        If btnNiftyOptionSignal IsNot Nothing Then btnNiftyOptionSignal.Enabled = False

        Try
            If String.IsNullOrWhiteSpace(upstoxAccessToken) Then
                MessageBox.Show("Connect to Upstox first.", "NIFTY Options Signal", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            LogMessage("NIFTY options signal: loading NIFTY 50 closed 5-minute candles...")

            Dim spotSet As ScannerCandleSet = Await GetScannerCandleSetAsync(NIFTY_INSTRUMENT_KEY, "NIFTY 50")
            If spotSet Is Nothing OrElse spotSet.AllCandles Is Nothing OrElse spotSet.AllCandles.Count < SLOW_EMA_PERIOD + 1 Then
                Throw New InvalidOperationException("Not enough NIFTY 50 5-minute candles were returned by Upstox.")
            End If

            Dim candles As List(Of Candle) = SortCandles(spotSet.AllCandles)
            Dim current As Candle = candles(candles.Count - 1)
            Dim previous As Candle = candles(candles.Count - 2)
            Dim spot As Double = current.Close
            Dim ema9 As Double = CalculateEMA(candles, FAST_EMA_PERIOD)
            Dim ema21 As Double = CalculateEMA(candles, SLOW_EMA_PERIOD)

            If ema9 <= 0 OrElse ema21 <= 0 OrElse spot <= 0 Then
                Throw New InvalidOperationException("EMA or NIFTY spot price could not be calculated.")
            End If

            Dim candleTime As String = current.DateValue
            If candleTime = lastNiftyOptionSignalTime Then
                MessageBox.Show("This 5-minute candle has already been evaluated. Wait for the next completed candle.", "NIFTY Options Signal", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Index candles do not provide reliable traded volume for a conventional VWAP.
            ' Therefore, this index-only version uses EMA alignment plus a candle breakout.
            Dim bullish As Boolean = current.Close > ema9 AndAlso ema9 > ema21 AndAlso current.Close > previous.High
            Dim bearish As Boolean = current.Close < ema9 AndAlso ema9 < ema21 AndAlso current.Close < previous.Low

            If Not bullish AndAlso Not bearish Then
                lastNiftyOptionSignalTime = candleTime
                Dim noSignal As String = "NO TRADE | No 9 EMA / 21 EMA breakout setup"
                LogMessage(noSignal & " | NIFTY=" & spot.ToString("0.00") & " | EMA9=" & ema9.ToString("0.00") & " | EMA21=" & ema21.ToString("0.00"))
                MessageBox.Show(noSignal & Environment.NewLine & Environment.NewLine &
                                "NIFTY spot: " & spot.ToString("0.00") & Environment.NewLine &
                                "EMA 9: " & ema9.ToString("0.00") & Environment.NewLine &
                                "EMA 21: " & ema21.ToString("0.00") & Environment.NewLine &
                                "VWAP filter: not used (index volume is not suitable for conventional VWAP).",
                                "NIFTY Options Signal", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim chainUrl As String = "https://api.upstox.com/v2/option/chain?instrument_key=" &
                                     Uri.EscapeDataString(NIFTY_INSTRUMENT_KEY) & "&expiry_date=current_week"
            Dim chainText As String = Await UpstoxGetAsync(chainUrl)
            If String.IsNullOrWhiteSpace(chainText) Then Throw New InvalidOperationException("Upstox returned an empty option-chain response.")

            Dim selectedStrike As Double = 0D
            Dim selectedOptionKey As String = ""
            Dim optionPremium As Double = 0D
            Dim selectedType As String = If(bullish, "CE", "PE")
            Dim bestDistance As Double = Double.MaxValue

            Using chainJson As JsonDocument = JsonDocument.Parse(chainText)
                Dim data As JsonElement
                If Not chainJson.RootElement.TryGetProperty("data", data) OrElse data.ValueKind <> JsonValueKind.Array Then
                    Dim errorText As String = chainText
                    If errorText.Length > 600 Then errorText = errorText.Substring(0, 600)
                    Throw New InvalidOperationException("Option-chain data was not returned. API response: " & errorText)
                End If

                For Each item As JsonElement In data.EnumerateArray()
                    Dim strikeElement As JsonElement
                    If Not item.TryGetProperty("strike_price", strikeElement) OrElse strikeElement.ValueKind <> JsonValueKind.Number Then Continue For
                    Dim strike As Double = strikeElement.GetDouble()
                    Dim optionNode As JsonElement
                    Dim instrumentNode As JsonElement
                    Dim marketNode As JsonElement
                    Dim ltpNode As JsonElement
                    Dim optionKey As String = ""
                    Dim ltp As Double = 0D

                    If Not item.TryGetProperty(If(bullish, "call_options", "put_options"), optionNode) Then Continue For
                    If Not optionNode.TryGetProperty("instrument_key", instrumentNode) OrElse instrumentNode.ValueKind <> JsonValueKind.String Then Continue For
                    optionKey = instrumentNode.GetString()
                    If String.IsNullOrWhiteSpace(optionKey) Then Continue For
                    If Not optionNode.TryGetProperty("market_data", marketNode) Then Continue For
                    If Not marketNode.TryGetProperty("ltp", ltpNode) OrElse ltpNode.ValueKind <> JsonValueKind.Number Then Continue For
                    ltp = ltpNode.GetDouble()
                    If ltp <= 0 Then Continue For

                    Dim distance As Double = Math.Abs(strike - spot)
                    If distance < bestDistance Then
                        bestDistance = distance
                        selectedStrike = strike
                        selectedOptionKey = optionKey
                        optionPremium = ltp
                    End If
                Next
            End Using

            If String.IsNullOrWhiteSpace(selectedOptionKey) OrElse optionPremium <= 0 Then
                Throw New InvalidOperationException("Could not find a valid ATM " & selectedType & " contract with a positive LTP in the current-week option chain.")
            End If

            ' Illustrative premium-risk model: 20% premium stop and 1.5R reward.
            Dim entry As Double = optionPremium
            Dim sl As Double = Math.Round(entry * 0.8D, 2)
            Dim riskPerUnit As Double = entry - sl
            Dim target As Double = Math.Round(entry + (riskPerUnit * 1.5D), 2)

            lastNiftyOptionSignalTime = candleTime
            Dim report As String = "NIFTY " & selectedType & " SIGNAL" & Environment.NewLine &
                "ATM strike: " & selectedStrike.ToString("0.##") & Environment.NewLine &
                "Option premium / entry reference: ₹" & entry.ToString("0.00") & Environment.NewLine &
                "Stop-loss reference: ₹" & sl.ToString("0.00") & " (20% premium risk)" & Environment.NewLine &
                "Target reference: ₹" & target.ToString("0.00") & " (1.5R)" & Environment.NewLine &
                "Option instrument_key: " & selectedOptionKey & Environment.NewLine & Environment.NewLine &
                "NIFTY spot: " & spot.ToString("0.00") & Environment.NewLine &
                "EMA 9: " & ema9.ToString("0.00") & " | EMA 21: " & ema21.ToString("0.00") & Environment.NewLine &
                "Candle: " & candleTime & Environment.NewLine &
                "VWAP filter: not used; NIFTY index does not have suitable traded volume for conventional VWAP." & Environment.NewLine & Environment.NewLine &
                "SIGNAL ONLY — no order has been placed. Verify expiry, liquidity, spread and risk before trading."

            LogMessage("================================")
            LogMessage(report.Replace(Environment.NewLine, " | "))
            LogMessage("================================")
            MessageBox.Show(report, "NIFTY Options Signal", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            LogMessage("NIFTY options signal error: " & ex.Message)
            MessageBox.Show("Could not generate NIFTY options signal:" & Environment.NewLine & Environment.NewLine & ex.Message,
                            "NIFTY Options Signal", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            If btnNiftyOptionSignal IsNot Nothing Then btnNiftyOptionSignal.Enabled = True
        End Try

    End Sub

    ' =========================================================
    ' CANDLE CLASS
    ' =========================================================

    Private Class Candle

        Public Property DateValue As String

        Public Property Symbol As String

        Public Property Exchange As String

        Public Property Interval As String

        Public Property Open As Double

        Public Property High As Double

        Public Property Low As Double

        Public Property Close As Double

        Public Property Volume As Long

    End Class

End Class