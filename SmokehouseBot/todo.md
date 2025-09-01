# Backlog
- [ ] Look into warnings during deploy
- [ ] SignalR for sending messages across network?

# Front End
- [ ] Restructure front end for maintainability and performance
- [ ] Ensure clients don't lose connection over time
- [ ] Ensure that the clients are not being marked absent when they are still connected
- [ ] Debounce refreshes from front end
- [ ] Ensure spamming front end add/remove buttons doesn't affect server performance
- [ ] Last Web Socket Event and orderbook last update shows 2024 years ago if empty
- [ ] Test on phone
- [ ] Add information about your resting orders to the front end
- [ ] Change position label to be more clear

# Soon
- [ ] Alerts?
- [ ] Variable refresh interval time? Why does it need to be the same length each time? Instead just refresh occasionally and work based on average web socket events capacity
- [ ] Refine average web socket events so that it resets when appropriate and doesn't divide by more time if market resets occur
- [ ] Add primary brain field to brain instances, so one of my instances can move watched markets between instances
- [ ] What are "Milestones" in the Kalshi API? Seems like it could be things that need to happen for events to trigger? Could be used for analysis
- [ ] Evaluate whether we can trust ticker feed to indicate when we should get candlesticks
- [ ] Batch subscription updates
- [ ] Dump snapshots to file system to reduce sql usage in the moment since snapshots aren't needed except in retrospect
- [ ] Expand Web socket testing to include: adding and removing markets quickly, conflicting commands, etc
- [ ] Detect upcoming downtimes and react to them, schedule maintenance
- [ ] ticker_v2
- [ ] Need some kind of "traffic cop" intermediary to handle graceful handoffs, potentially handle some of the maintenance duties
- [ ] Is no slope flipped the wrong direction from what would be expected?
- [ ] userdatatimestamp endpoint (https://docs.kalshi.com/api-reference/get-user-data-timestamp). Make system that monitors this and, beyond a defined threshold, cancels all resting orders and shuts down until it improves. 


# v0.2.6
- [ ] Automate data cleanup
- [ ] Make database logging of debug work
- [ ] Was forced to upgrade to market lifecycle v2, need to update code to capture new fields if necessary
- [ ] Clean up unused db objects
- [ ] Ensure category properly retrieved and saved (missing on some markets still)
- [ ] Revisit market interest score
- [ ] Make various refresh threshholds configurable (timing between refreshes, number of extras forced when not many to refresh, etc)
- [ ] Investigate: Exchange is inactive or reconnection disabled, skipping reconnection attempt (id 458076146,followed by no reconnection attempt for 2 hours, then skipping "late" snapshots, only restarting because of no snapshots in 10 minutes)
- [ ] Start periodically sampling exchange status and find out if they are warning about sudden outages
- [ ] Start with no markets watched then build list so there isn't so much of a downtime, start with most active markets to minimize impact of restart

# v0.2.5
Notes: Major issue which was causing snapshots after the first to not translate to change over time... all snapshots invalidated.
- [x] Configurable saving of feeds, for performance
- [x] Add web socket activity level to MarketWatches
- [x] OrderbookChangeTracker survived market KXJOINKARPATHY-26JAN-TES (SmokehouseBot.Services.OrderbookChangeTracker)
- [x] Make executable tasks setup smarter about path
- [x] Better change over time metrics
- [x] Clean new repo on github
- [x] Tests now runnable on other machines
- [x] So many strat and trading GUI changes

# v0.2.4
Note: Discovered bug which was causing the orderbook to be static in the snapshot due to dual storage in memory and only one copy being updated. All snapshots have been invalidated with no
upgrade plan.
- [x] Separate different types of web socket events so I can tell when one of them hasnt received a message in a while
- [x] Make API calls testable
- [x] Cancel overnight activities if the exchange starts up
- [x] Added API methods for schedule and placing/canceling orders
- [x] Fix misordered bollinger bands
- [x] Fixed static orderbook bug
- [x] Update date column names to reflect UTC or not
- [x] Fix schema deployment test
- [x] Make initial snapshots not spike delta numbers (even if metrics aren't mature, its messy)
- [x] Correct all async and non async methods
- [x] Remove check for snapshot version before saving snapshot, instead just make sure current schema matches the expected based on version
- [x] Highest volumes and recent volumes are null in snapshots, needs investigated
- [x] Laid out framework for backtesting, created a few strategies, gui for visualizing

# v0.2.3 (DEPLOY)
- [x] Bring DI back to ServiceFactory to align with best practices and improve testability
- [x] Stop using brain lock static string for brain instance
- [x] Snapshot groups added to overnight tasks
- [x] Remove uneeded markets overnight
- [x] Delete uneeded candlesticks overnight
- [x] Make web socket testable
- [x] Fix overlapping removal attempts when not managed
- [x] Moved some config settings to the database for better mid-run management
- [x] Lots of trading logic/reporting work, still experimenting

# v0.2.2 (DEPLOYED)
Goals: Better performance monitoring for other types of bottlenecks
Note: Found major bug with orderbook delta application... hard cutoff morning of 6/28
- [x] Figure out difference between AddMarketWatchToDb and SubscribeToMarketAsync
- [x] Fully abstract KalshiDBContext
- [x] Instead of resetting market maturity when orderbook snapshots occur, simulate the changes
- [x] Fully adaptive market limit based on performance
- [x] Add continuity to interest score
- [x] Fix removing markets due to high usage
- [x] Still calculate market score for already watched markets with positions
- [x] Throw catastrophic error if snapshots haven't been taken lately

# v0.2.1 (DEPLOYED)
Goals: Reorganization and fix major bug
Notes: Found pretty major bug where the orderbook levels were not being updated in the case where a delta did not cancel them out completely. 
There were also rate discrepancies which needed fixed.
Hard cut off at 3:00 PM 6/22/25 (19:00) for snapshot validity.
- [x] Add rate discrepancies to validation
- [x] Add session identifier token to logs to easily tell the session
- [x] Remove legacy "performance reports"
- [x] Remove legacy "counts of methods"
- [x] Interfaces for all services
- [x] Ensure consistent use of interfaces
- [x] Remove vestigial Orderbooks object

# v0.2.0 (DEPLOYED, CONFIG CHANGES, SCHEMA CHANGES)
Goals: Full performance monitoring medium term testing goals accomplished, full stability
Observations: After moving candlestick data to being cached on my new SSD, and upgrading the sql server,
performance has skyrocketed. Now getting around 275 markets on one instance as opposed to ~35 on 2 instances.
Efficiencies will have to be found there before the dashboard can be used in a meaningful way again. due to 
sheer volume. So goals related to the dashboard have been moved to a future version. Multiple instance support 
still has problems, and likewise has been pushed off until I can assess maximum single instance performance.
There is a new bottleneck which is the event queue in OrderbookService. Need to increase efficiency to prevent
huge queue counts which go up, not down.
Note: Current prices were removed from the snapshot as they were just a delayed reflection of BestYesBid etc, 
which was causing price discrepenacies due to high volatility. 
Note: Trade fix in place as of 11 am, 6/9/25
- [x] Endurance test (successfully run for 2 day/night cycles)
- [x] Double check snapshots logical consistency and throw errors
- [x] expecting 'ok' or 'subscribed', for markets despite expecting unsubscribed or ok
- [x] Don't refill markets, rather reuse
- [x] Output performance information in snapshot logging
- [x] Brain flags previously validated records
- [x] Preload forward filled markets as parquets
- [x] Brain validates prices against orderbook
- [x] Log an error if access is denied to the key file
- [x] LogCancellationToken to prevent initial "warning" if error is completely handled
- [x] Fix catastrophic error trigger
- [x] Figure out why snapshots are getting delays on account of market refresh activity
- [x] Fix deployment script...
- [x] Fix deserialization of market ticker and software version
- [x] Fix "minor" price discrepances (<1 diff in snapshot vs current price and orderbook) in snapshots
- [x] Warn about "minor" price discrepances, stop snapshot?
- [x] Add market type, Brain Instance and isvalidated to snapshots to prep for brain cleanup management
- [x] Add new config setting for overnight activities
- [x] Take interest score out of sql


# v0.1.9 (DEPLOYED 5/24/25)
Goals: Finally achieve stability and managed watchlists, with performance monitoring
- [x] Add brain instance to performance reports
- [x] Fix adding markets (yet again)
- [x] Fix brains not checking in overnight
- [x] Stop brain locks from being cleared out overnight (not really stale)
- [x] Initialization not canceling effectively
- [x] Change to performance graph (work to be done)
- [x] Better performance monitor layout
- [x] Unhandled errors thrown as critical
- [x] Don't handle unhandled errors repeatedly
- [x] Don't let snapshot irregularity warning ripple
- [x] Fix lifecycle issues

# v0.1.8 (DEPLOYED 5/20/25)
- [x] Speed up monitoring cycles, rolling period rather than just based on the last crunch
- [x] Fix usage monitoring not rebooting with the rest of the app (works when started fresh)
- [x] Response status code does not indicate success for candlesticks
- [x] Fixed removing finalized markets

# v0.1.7 (DEPLOYED 5/20/25, CONFIG CHANGES)
Goals: Performance Monitoring, deconflict web sockets
- [x] Non market specific web sockets conflict, such as a lifecyle event
- [x] Add performance monitoring to KalshiWebSocketClient
- [x] Add performance monitoring to BroadcastService
- [x] Create performance monitor endpoint
- [x] Add timestamp log for completion of all initialization

# v0.1.6 (DEPLOYED 5/19/25)
Goals: Improve stability, fix adding markets
- [x] Resetable individual markets
- [x] Add performance monitoring to KalshiAPIService
- [x] Handle: Trade cleared from queue without matching orderbook change
- [x] Market Refresh Service not in a namespace
- [x] Market KXDEBTSHRINK-28NOV11-1 confirmed currently watched (MarketRefreshService)

# v0.1.5 (CLEAN DEPLOYED 5/18/25, CONFIG CHANGES, SCHEMA CHANGE 14)
Goals: Less sensitive catastrophic error handling, multi-instance support
Observations: Had to create a second API key or else there were web socket conflicts. There was a bug in this version
that prevented it from successfully adding more markets
- [x] Configurable whether or not to launch Data Dashboard
- [x] PopulateMarketDataAsync still has a reference to market processor
- [x] Wrong dashboard version in logs
- [x] Expanded MarketWatch including cached interest score and Brain ID
- [x] Better error tracking for brain (send exceptions, not just a count)
- [x] Catastrophic error whitelist
- [x] Make usage targets configurable
- [x] Add information about your resting orders to the snapshot
- [x] Log brain instance
 
# v0.1.4 (DEPLOYED 5/16/25)
Goals: Improve stability and lifecycle control
Observations: Much of previous issues with start/stop were due to misconfiguration on the prd server
- [x] Reliable stop with IIS 
- [x] Stop on timer
- [x] Arithmetic operation resulted in an overflow.

# v0.1.3 (DEPLOYED)
Goals: Improve market selection and stability
Observations: App still not fully stopping with iis, didn't stop on timer
- [x] Develop comprehensive "market interest score" which can manage whether to drop markets
- [x] Cancellation tokens throughout
- [x] Better managed lifecycle with factory pattern
- [x] Snapshot upgrade process

# v0.1.2 (DEPLOYED)
Goals: Catastrophic error handling and other error handling
- [x] Rewrite simulator deserialization methods for better performance
- [x] Snapshot timing irregularity detected: 20250513T120456Z is 18316.9999308 seconds after 20250513T065939Z, expected approximately 60 seconds (TradingStrategies.Trading.TradingSnapshotService)
- [x] String or binary data would be truncated in table 'kalshibot-dev.dbo.t_Markets', column 'no_sub_title'. Truncated value: 'Conan O''Brien: The Kennedy Center Mark Twain Prize'
- [x] Rotate passwords
- [x] No market data available for SENATELA-26-D, skipping orderbook broadcast (SmokehouseBot.Services.BroadcastService)
- [x] Fix graceful resubscribe

# v0.1.1 (DEPLOYED)
Goals: Enhance overnight stability
- [x] Test maturity reset overnight
- [x] Fix shutdown overnight

# v0.1.0 (DEPLOYED)
- [x] Make the brain know when the market is closed and have it not generate snapshots
- [x] Make the snapshot aware of whether it has sufficient data to understand change over time metrics
- [x] Implement orderbook price cross reference check
- [x] Brain readiness check
- [x] Change how time since fields are snapshotted
- [x] Add to front end: GetYesCancellationRatePerMinute and GetNoCancellationRatePerMinute
- [x] Add to front end: RSI
- [x] Add to front end: MACD
- [x] Add to front end: EMA
- [x] Add to front end: Bollinger Bands
- [x] Add to front end: ATR
- [x] Add to front end: VWAP
- [x] Add to front end: Stochastic Oscilator
- [x] Add to front end: OBV
- [x] Add to front end: CancellationRatePerMinute
- [x] Fix timeframe zoom
- [x] Title refresh if missing
- [x] Externalize configurable fields to config file
- [x] Consider exchange hours and how that affects time based operations (rate of change, etc)
- [x] Evaluate WebSocketEventHandler and whether its even in use - it doesn't seem to be, nothing triggers there
- [x] Listen to LifeCycle events
- [x] Listen to Fill events (just trigger positions update?)
- [x] Mouse over fields for Time Left and Market Age
- [x] Left/right keyboard key for switching markets
- [x] Third color if exchange is down?
- [x] Add trading status to front end
- [x] Add exchange status to UI
- [x] Move Time Left and Market Age to top section
- [x] Move Bid/Ask Imbalance to Context & Deeper Book
- [x] Ask/Bid colors are flipped in the market info panel
- [x] Rename hold time to "Last Trade"
- [x] Color Bid/Ask Imbalance
- [x] Suppress "Last websocket event was too long ago, web socket may be inactive" warning if exchange is closed
New data
- [x] Supports and Resistances
- [x] Need can close early on UI
UI
- [x] Timestamps along x axis aren't consistent
- [x] Double check if chart is in UTC, it is labelled like that but shouldnt be UTC. Change label.
- [x] Implement front end changes to watch list
- [x] Handle adding market to watch list on front end so you can tell its happening
- [x] Some inconsistency with buyin price lines on the chart
- [x] Change dropdown so I know what is what easier
- [x] Front end 404 errors
- [x] Rate of Change and Average Trade Size need Ask/Bid
- [x] Spread not working
- [x] Remove Top Velocity from Rate of Change and make clear they are two sides of the same coin
- [x] Rename rate of change fields to be more intuitive, label informatively (number of levels included etc)
- [x] Colors backwards on top right
- [x] Position metrics aren't populating
- [x] Show "Loading..." as title after adding a market through front end
- [x] Position calculation not coming through immediately
- [x] Yes/no toggle not defaulting correctly when refreshed
- [x] Add resting orders to front end
- [x] Fix chart data display for other time periods
- [x] Add "All" to the time period dropdown
- [x] Support and resistance might not be being drawn more than a pixel?
- [x] Position not updating
- [x] Bid/Ask Imbalance wrong color, and shouldn't it be ask/bid imbalance?
- [x] Background color not shifting
- [x] Stochastic oscillator not coming through anywhere
- [x] All time high bid not coming through despite seemingly being populated
- [x] Move total trades to parenthesis
- [x] Visual inconsistency, Average Trade Size shows /min even with "--" whereas others don't
- [x] Add volume to chart
- [x] Legend doesn't show for "red line"
- [x] Cant add legend items after removing them
- [x] Need subtitle on UI
- [x] Remove Depth @ Best
- [x] Also add total orders in parenthesis to net order rate
- [x] Average trade size not populating
- [x] Add number of non-trade related events to Net Order Rate
- [x] Highs and lows don't flip properly with yes/no toggle
- [x] Noticeable delay before things are populated
- [x] Make average trade size specific to the ask/bid side
- [x] Make both trade rate and net trade rate display a percentage of the total velocity
- [x] Change chart display time to local timezone
- [x] Chart data offset 4 hours and cut off
- [x] Set y scale for volume based on biggest volume
- [x] Status on front end if no time left
- [x] Maintain individual statuses instead of only the current market and create indicator for staleness in all markets
- [x] Red flicker on price change
- [x] Display logged error count on front end
- [x] Ensure that last web socket event and orderbook last updated are working properly and displaying the right info
Logic fixes
- [x] Debounce orderbook events/grouping in a queue to prevent constant updates
- [x] Review historical data broadcast frequency
- [x] Move follow markets with a position out of FetchPositionsAsync, it doesn't make sense there. Isn't this done in UpdateWatchedMarketsAsync? Make it configurable 
- [x] Rate of change metrics are resetting too quickly, or staying the same. Over 5 minutes we'd expect a gradual change
- [x] We don't seem to using trade related data flag
- [x] Trade related logic needs to take "taker" side into account and figure out how orderbook represents intantly completed limit changes
- [x] Add to configuration: // Save to database only for Warning and above (customize this condition)
- [x] Add this to configuration: builder.Logging.SetMinimumLevel(LogLevel.Information);
- [x] Weight things by price level
- [x] Ensure no broadcasts if no clients connected
- [x] Find and fix duplicate ticker issue
- [x] Remove market tickers in the config
- [x] Received unknown message type: event_lifecycle
- [x] RSI: Fixed
- [x] Add configurable watch markets with resting orders
- [x] Add pending orders to data and snapshot
- [x] Remove channels from config
- [x] MACD: Fixed
- [x] Bollinger Bands: Fixed
- [x] What are "Log Odds"?
- [x] Remove 3 month filter on candlestick data on back end? (RetrieveHistoricalCandlesticksAsync)
- [x] Position upside/downside should reflect current position and price
- [x] Trading status not reacting to market open/close
- [x] Fixed center of mass
- [x] Logging config doesn't pick up all of its values
- [x] Make configurable: bool lockAcquired = await semaphore.WaitAsync(40000);
- [x] Cut off first couple of candles for "all time high" and "all time low"
- [x] Add delay on lifecycle events to avoid 404
- [x] Levels need to be transmitted regardless of whether there is a velocity
- [x] Highs and lows are incorrect
- [x] Rates are still wonky
- [x] Make total volume weighted by cost
- [x] Rename trade rate trade volume and create correct trade rate
- [x] Recent traded volume (1 hour, 3 hours, 1 day)
- [x] Cannot insert duplicate key in object 'dbo.t_feed_lifecycle_market'. The duplicate key value is (KXBTCD-25MAY0311-T92249.99, May  3 2025 11:02AM)
- [x] // Non-blocking full sync (two logic paths to update market)
- [x] Try including volume requirement in green lines? Need to rework them to be more useful
- [x] Why two methods? (SyncCandlesticksFromApiAsync, GetCandlesticksAsync)
- [x] Prices are getting out of synch somehow
- [x] Gotta double check removing old events/trades.. just saw six trades drop to zero
- [x] Add Change Metrics Mature to sql db
- [x] Fill event handling throws error
- [x] Remove quickly removed orders from rates
- [x] Remove fees from position ROI
- [x] Market refresh interval exceeded 100% of expected interval
- [x] Stop periodic refresh while disconnected
- [x] [Warning]: Replacing existing ticker for
- [x] Establish software version
Snapshot
- [x] Record all variables used in snapshot to ensure apples to apples... Configuration master singleton to help track the settings used for things
- [x] Include entire orderbook in snapshot
- [x] Hybridize with sql
- [x] Snapshot includes useful resistance/support data
- [x] Include rate of change metadata in snapshot, such as the number of levels used in the calculation
- [x] Double check for missing data in the snapshot
- [x] Include all xml comments
- [x] Clean up json output
- [x] Implement snapshot structure version number
- [x] Standardize names: YesBidOrderRatePerMinute, NoAskOrderRatePerMinute, NoBidOrderRatePerMinute, YesAskOrderRatePerMinute, TradeVolume_Yes, TradeVolume_No
- [x] Don't snapshot overnight
Logging
- [x] Environment in logs
- [x] Ensure warnings if things aren't being updated regularly
- [x] Declutter
- [x] Improve "Order book event queue has 7 items" warning
Tests
- [x] Make sure Market Lifecycle message received and processed successfully
- [x] Test watching uninvested market
- [x] Write test for MACD
- [x] Test Resting Orders
- [x] Test watch markets with resting orders
- [x] Test watch markets with positions
- [x] Write test for RSI
- [x] Write test for EMA
- [x] Write test for Bollinger Bands
- [x] Write test for ATR
- [x] Write test for VWAP
- [x] Write test for Stochastic Oscillator
- [x] Write test for OBV
- [x] Make sure Event Lifecycle message received and processed successfully
- [x] Test timeframe zoom
- [x] Make sure pseudo candlesticks are using the correct window
- [x] Ensure change over time metrics do not try to update too soon
- [x] Is Last Web Socket Event being updated by orderbook deltas?
- [x] Test watching closed market
- [x] Test that snapshot schema matches, change something make sure it doesn't match anymore
- [x] Validate all time and recent highs and lows
- [x] Test fill events
- [x] Stress test
- [x] Test that messages are not received for all channels after unsubscribe
- [x] Make sure all channels still work for subscribed markets after an unsubscribe
- [x] Use sql to validate metrics are consistent
- [x] Ensure all tests succeed
- [x] Test save + load + save + compare snapshots


