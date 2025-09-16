/**
 * SIGNALR REAL-TIME COMMUNICATION MODULE
 *
 * This file manages the SignalR connection for real-time communication with the
 * Kalshi Trading Bot backend. It handles live updates for brain status, market
 * data, and system events through WebSocket connections.
 *
 * RATIONALE FOR SEPARATION:
 * - Isolates real-time communication logic from UI and data management
 * - Provides centralized connection management and error handling
 * - Enables easy switching between different real-time technologies
 * - Allows for connection state monitoring and automatic reconnection
 * - Makes real-time features easily testable and mockable
 *
 * CONTENTS:
 * - SignalR connection initialization and lifecycle management
 * - Real-time message handlers for brain status updates
 * - Check-in data processing and UI synchronization
 * - Connection state monitoring and error recovery
 * - Brain instance management and ticker operations
 */

/**
 * SignalR Configuration
 * Uses centralized configuration from CONFIG.SIGNALR
 */

/**
 * Connection Health Monitoring
 */
let connectionHealth = {
    isConnected: false,
    lastPingTime: null,
    lastPongTime: null,
    pingCount: 0,
    pongCount: 0,
    reconnectAttempts: 0,
    connectionQuality: 'unknown', // 'good', 'fair', 'poor', 'disconnected'
    messageQueue: [],
    batchTimer: null
};

/**
 * Initializes the SignalR connection to the backend hub
 * Sets up event handlers for real-time updates and manages connection lifecycle
 * Automatically reconnects on connection loss with enhanced monitoring
 */
async function initializeSignalR() {
    console.log('[BrainCards] building hub connection to', CONFIG.SIGNALR_HUB);

    const builder = new signalR.HubConnectionBuilder()
        .withUrl(CONFIG.SIGNALR_HUB)
        .configureLogging(CONFIG.SIGNALR.LOG_LEVEL);

    if (CONFIG.SIGNALR.AUTOMATIC_RECONNECT) {
        builder.withAutomaticReconnect(CONFIG.SIGNALR.RECONNECT_INTERVALS);
    }

    connection = builder.build();

    // Expose for other functions already in the page
    window.connection = connection;

    // EVENT HANDLERS FOR REAL-TIME UPDATES

    /**
     * Handles real-time brain status updates from the backend
     * Updates brain data, check-in information, and triggers UI refreshes
     */
    connection.on('BrainStatusUpdate', (m) => {
        logWithTimestamp('debug', '[BrainCards] BrainStatusUpdate received', { keys: Object.keys(m || {}), sample: m });
        handleBrainStatusUpdate(m);
    });

    /**
     * Handles brain check-in updates for monitoring brain instance health
     * Processes check-in data and updates the brain status display
     */
    connection.on('CheckInUpdate', (msg) => {
        console.log('[BrainCards] CheckInUpdate received:', msg);
        try {
            handleCheckInUpdate(msg);
        } catch (e) {
            console.error('[BrainCards] CheckInUpdate handler error:', e);
        }
    });

    /**
     * Optional handler for server-side trace messages
     * Useful for debugging broadcast reachability
     */
    connection.on('BroadcastTrace', (info) => {
        console.log('[BrainCards] BroadcastTrace:', info);
        // Handle both camelCase and PascalCase for compatibility
        const kind = info.kind ?? info.Kind ?? 'unknown';
        const brain = info.brain ?? info.Brain ?? 'unknown';
        const marketCount = info.marketCount ?? info.MarketCount ?? 0;
        const serverUtc = info.serverUtc ?? info.ServerUtc ?? null;
        console.log(`[BrainCards] Trace: ${kind} for ${brain} with ${marketCount} markets at ${serverUtc}`);
    });

    /**
     * Handle response from SendOverseerMessage
     */
    connection.on('OverseerMessageReceived', (response) => {
        console.log('[BrainCards] OverseerMessageReceived:', response);
        if (response.Success) {
            console.log('Refresh request processed successfully');
        } else {
            console.error('Refresh request failed:', response.Message);
            alert('Refresh request failed: ' + response.Message);
        }
    });

    /**
     * Handle data refresh broadcast from server
     */
    connection.on('DataRefreshRequested', (data) => {
        console.log('[BrainCards] Data refresh requested by server:', data);
        // Optionally trigger a UI refresh or data reload here
        // For example, you could call renderMarkets() or similar functions
    });

    // CONNECTION LIFECYCLE MANAGEMENT

    /**
     * Handles connection reconnection attempts
     * Logs reconnection events and updates health monitoring
     */
    connection.onreconnecting(err => {
        connectionHealth.reconnectAttempts++;
        connectionHealth.isConnected = false;
        connectionHealth.connectionQuality = 'disconnected';
        updateConnectionQualityIndicator();
        console.warn('[BrainCards] reconnecting…', err, 'Attempt:', connectionHealth.reconnectAttempts);
    });

    /**
     * Handles successful reconnection
     * Logs new connection ID and resets health metrics
     */
    connection.onreconnected(id => {
        connectionHealth.isConnected = true;
        connectionHealth.reconnectAttempts = 0;
        connectionHealth.connectionQuality = 'good';
        updateConnectionQualityIndicator();
        console.log('[BrainCards] reconnected, connId=', id);
        startPingMonitoring();
    });

    /**
     * Handles connection closure
     * Logs closure events and updates health status
     */
    connection.onclose(err => {
        connectionHealth.isConnected = false;
        connectionHealth.connectionQuality = 'disconnected';
        updateConnectionQualityIndicator();
        console.warn('[BrainCards] closed', err);
        stopPingMonitoring();
    });

    // Start
    try {
        await connection.start();
        console.log('[BrainCards] connection established, state=', connection.state);
    } catch (err) {
        console.error('[BrainCards] connection start failed:', err);
    }

    // Start health monitoring if connection successful
    if (connection.state === signalR.HubConnectionState.Connected) {
        connectionHealth.isConnected = true;
        connectionHealth.connectionQuality = 'good';
        updateConnectionQualityIndicator();
        startPingMonitoring();
    }
}

/**
 * Starts periodic ping monitoring for connection health
 */
function startPingMonitoring() {
    if (connectionHealth.pingIntervalId) {
        clearInterval(connectionHealth.pingIntervalId);
    }

    connectionHealth.pingIntervalId = setInterval(async () => {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            try {
                connectionHealth.lastPingTime = Date.now();
                connectionHealth.pingCount++;

                // Send ping and measure response time
                await connection.invoke('Ping');

                // Note: Actual pong handling would need server-side implementation
                // For now, we'll simulate pong detection
                setTimeout(() => {
                    if (connectionHealth.lastPingTime) {
                        const pingTime = Date.now() - connectionHealth.lastPingTime;
                        updateConnectionQuality(pingTime);
                        connectionHealth.lastPingTime = null;
                    }
                }, 100);

            } catch (err) {
                console.warn('[BrainCards] Ping failed:', err);
                connectionHealth.connectionQuality = 'poor';
                updateConnectionQualityIndicator();
            }
        }
    }, CONFIG.SIGNALR.PING_INTERVAL);
}

/**
 * Stops ping monitoring
 */
function stopPingMonitoring() {
    if (connectionHealth.pingIntervalId) {
        clearInterval(connectionHealth.pingIntervalId);
        connectionHealth.pingIntervalId = null;
    }
}

/**
 * Updates connection quality based on ping time
 * @param {number} pingTime - Time in milliseconds
 */
function updateConnectionQuality(pingTime) {
    if (pingTime < 100) {
        connectionHealth.connectionQuality = 'good';
    } else if (pingTime < CONFIG.SIGNALR.CONNECTION_QUALITY_THRESHOLD) {
        connectionHealth.connectionQuality = 'fair';
    } else {
        connectionHealth.connectionQuality = 'poor';
    }
    updateConnectionQualityIndicator();
}

/**
 * Updates the UI connection quality indicator
 */
function updateConnectionQualityIndicator() {
    const indicator = document.getElementById('connection-quality-indicator');
    if (indicator) {
        const quality = connectionHealth.connectionQuality;
        let text, color, icon;

        switch (quality) {
            case 'good':
                text = 'Connected';
                color = '#28a745';
                icon = 'fas fa-wifi';
                break;
            case 'fair':
                text = 'Fair';
                color = '#ffc107';
                icon = 'fas fa-wifi';
                break;
            case 'poor':
                text = 'Poor';
                color = '#fd7e14';
                icon = 'fas fa-exclamation-triangle';
                break;
            case 'disconnected':
                text = 'Disconnected';
                color = '#dc3545';
                icon = 'fas fa-times-circle';
                break;
            default:
                text = 'Unknown';
                color = '#6c757d';
                icon = 'fas fa-question-circle';
        }

        indicator.innerHTML = `<i class="${icon}"></i> ${text}`;
        indicator.style.color = color;
    }
}

// MESSAGE BATCHING SYSTEM

/**
 * Queues a message for batching
 * @param {string} method - SignalR method name
 * @param {...any} args - Method arguments
 */
function queueMessage(method, ...args) {
    connectionHealth.messageQueue.push({ method, args, timestamp: Date.now() });

    if (connectionHealth.messageQueue.length >= CONFIG.SIGNALR.MESSAGE_BATCH_SIZE) {
        flushMessageBatch();
    } else if (!connectionHealth.batchTimer) {
        connectionHealth.batchTimer = setTimeout(flushMessageBatch, CONFIG.SIGNALR.MESSAGE_BATCH_DELAY);
    }
}

/**
 * Flushes the message batch by sending all queued messages
 */
function flushMessageBatch() {
    if (connectionHealth.batchTimer) {
        clearTimeout(connectionHealth.batchTimer);
        connectionHealth.batchTimer = null;
    }

    if (connectionHealth.messageQueue.length === 0) return;

    const batch = [...connectionHealth.messageQueue];
    connectionHealth.messageQueue = [];

    // Send messages in batch
    batch.forEach(({ method, args }) => {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke(method, ...args).catch(err => {
                console.error(`[BrainCards] Batch message failed (${method}):`, err);
            });
        }
    });

    console.log(`[BrainCards] Flushed ${batch.length} messages in batch`);
}

/**
 * Gets current connection health metrics
 * @returns {Object} Connection health information
 */
function getConnectionHealth() {
    return {
        ...connectionHealth,
        state: connection ? connection.state : 'disconnected',
        connectionId: connection ? connection.connectionId : null
    };
}

/**
 * Manually trigger connection quality update
 * Useful for debugging or forced refresh
 */
function refreshConnectionQuality() {
    updateConnectionQualityIndicator();
}

// BRAIN STATUS MANAGEMENT

/**
 * Processes real-time brain status updates from SignalR
 * Merges real-time data with existing brain data and updates UI accordingly
 * Handles both PascalCase and camelCase property naming for flexibility
 *
 * @param {Object} msg - SignalR message containing brain status information
 */
function handleBrainStatusUpdate(msg) {
    // Accept either brainInstanceName or BrainInstanceName for compatibility
    const cam = msg && msg.brainInstanceName;
    const pas = msg && msg.BrainInstanceName;
    const name = cam || pas || null;

    logWithTimestamp('debug', '[BrainCards] handleBrainStatusUpdate: entry', { name, hasCamel: !!cam, hasPascal: !!pas });

    if (!name) {
        logWithTimestamp('warn', '[BrainCards] handleBrainStatusUpdate: missing brain name; aborting', { keys: Object.keys(msg || {}) });
        return;
    }

    // Normalize brain name to lowercase for consistent key lookup
    const normalizedName = name.toLowerCase();

    // Update existing brain data with real-time SignalR information
    if (brainData[normalizedName]) {
        // Preserve database data and enrich with SignalR real-time data
        const originalDBData = brainData[normalizedName];
        brainData[normalizedName] = {
            ...brainData[normalizedName], // Keep database data (WatchedMarkets, etc.)
            ...msg, // Override with SignalR real-time data
            brainInstanceName: name // Preserve original casing for display
        };
        logWithTimestamp('debug', '[BrainCards] handleBrainStatusUpdate: brainData merged - DB data preserved', {
            name,
            normalizedName,
            originalMarketCount: originalDBData.marketCount || 0,
            signalRMarketCount: msg.marketCount || 0,
            finalMarketCount: brainData[normalizedName].marketCount || 0,
            dbKeys: Object.keys(originalDBData),
            signalRKeys: Object.keys(msg)
        });
    } else {
        // If brain not in database data, add it (shouldn't happen with backend filtering)
        brainData[normalizedName] = {
            ...msg,
            brainInstanceName: name // Preserve original casing for display
        };
        logWithTimestamp('warning', '[BrainCards] handleBrainStatusUpdate: brain not found in DB, using SignalR data only', { name, normalizedName });
    }
    logWithTimestamp('debug', '[BrainCards] handleBrainStatusUpdate: brainData merged', { name, normalizedName, storedKeys: Object.keys(brainData[normalizedName] || {}) });

    // Derive check-in badge fields for the brain lock card
    const lastCheckIn = msg.lastCheckIn ?? msg.LastCheckIn ?? null;
    const lastSnapshot = msg.lastSnapshot ?? msg.LastSnapshot ?? null;
    const marketCount = (msg.markets?.length ?? msg.Markets?.length ?? msg.marketCount ?? msg.MarketCount) ?? 0;
    const errorCount = (msg.errorCount ?? msg.ErrorCount) ?? 0;
    const isStartingUp = !!(msg.isStartingUp ?? msg.IsStartingUp);
    const isShuttingDown = !!(msg.isShuttingDown ?? msg.IsShuttingDown);

    checkInData[normalizedName] = {
        brainInstanceName: name, // Preserve original casing for display
        marketCount: marketCount,
        errorCount: errorCount,
        lastSnapshot: lastSnapshot,
        lastCheckIn: lastCheckIn,
        isStartingUp: isStartingUp,
        isShuttingDown: isShuttingDown
    };
    logWithTimestamp('debug', '[BrainCards] handleBrainStatusUpdate: checkInData updated', checkInData[normalizedName]);

    // Update the UI that depends on this status
    try { updateBrainStatusUI(msg); } catch (e) { logWithTimestamp('error', '[BrainCards] updateBrainStatusUI failed', e); }
    try { renderPerformanceChartsForBrains(msg); } catch (e) { logWithTimestamp('error', '[BrainCards] renderPerformanceChartsForBrains failed', e); }

    // Re-render brain cards so badges/timestamps change immediately
    try {
        renderBrains();
        logWithTimestamp('debug', '[BrainCards] handleBrainStatusUpdate: renderBrains done', { name, hasCheckIn: !!checkInData[name] });
    } catch (e) {
        logWithTimestamp('error', '[BrainCards] renderBrains failed', e);
    }
}

// Handle CheckIn messages from bots and update brain cards
function handleCheckInUpdate(checkInMessage) {
    try {
        console.log('DEBUG: handleCheckInUpdate called with:', checkInMessage);

        if (!checkInMessage || !checkInMessage.BrainInstanceName) {
            console.warn('DEBUG: handleCheckInUpdate: missing BrainInstanceName', checkInMessage);
            return;
        }

        const brainInstanceName = checkInMessage.brainInstanceName ?? checkInMessage.BrainInstanceName;
        const marketCount = checkInMessage.marketCount ?? checkInMessage.MarketCount ?? 0;
        const errorCount = checkInMessage.errorCount ?? checkInMessage.ErrorCount ?? 0;
        const lastSnapshot = checkInMessage.lastSnapshot ?? checkInMessage.LastSnapshot ?? null;
        const lastCheckIn = checkInMessage.lastCheckIn ?? checkInMessage.LastCheckIn ?? null;
        const isStartingUp = !!(checkInMessage.isStartingUp ?? checkInMessage.IsStartingUp);
        const isShuttingDown = !!(checkInMessage.isShuttingDown ?? checkInMessage.IsShuttingDown);

        // Update global cache used by brain cards
        checkInData[brainInstanceName] = {
            brainInstanceName,
            marketCount,
            errorCount,
            lastSnapshot,
            lastCheckIn,
            isStartingUp,
            isShuttingDown
        };

        console.log('DEBUG: Updated checkInData for', brainInstanceName, ':', checkInData[brainInstanceName]);

        // Re-render cards so stats/status text reflect the new check-in
        renderBrains();

        console.log(
            `CheckIn received from ${brainInstanceName}: Markets=${marketCount}, ` +
            `ErrorCount=${errorCount}, LastSnapshot=${lastSnapshot}, ` +
            `StartingUp=${isStartingUp}, ShuttingDown=${isShuttingDown}`
        );
    } catch (err) {
        console.error('ERROR: handleCheckInUpdate failed:', err, checkInMessage);
    }
}

function updateBrainStatusUI(msg) {
    const cam = msg && msg.brainInstanceName;
    const pas = msg && msg.BrainInstanceName;
    const name = cam || pas || '';
    const casing = cam ? 'camelCase' : (pas ? 'PascalCase' : 'missing');
    const safe = name.replace(/[^a-zA-Z0-9]/g, '_');

    const lastCheckIn = msg.lastCheckIn ?? msg.LastCheckIn ?? null;
    const lastSnapshot = msg.lastSnapshot ?? msg.LastSnapshot ?? null;
    const errorCount = (msg.errorCount ?? msg.ErrorCount) ?? 0;
    const marketCount = (msg.marketCount ?? msg.MarketCount) ?? 0;
    const isStartingUp = !!(msg.isStartingUp ?? msg.IsStartingUp);
    const isShuttingDown = !!(msg.isShuttingDown ?? msg.IsShuttingDown);

    const ids = {
        lastCheckIn: `last-checkin-${safe}`,
        lastSnapshot: `last-snapshot-${safe}`,
        errorCount: `error-count-${safe}`,
        marketCount: `market-count-${safe}`,
        status: `status-indicator-${safe}`
    };
    const els = {
        lastCheckIn: document.getElementById(ids.lastCheckIn),
        lastSnapshot: document.getElementById(ids.lastSnapshot),
        errorCount: document.getElementById(ids.errorCount),
        marketCount: document.getElementById(ids.marketCount),
        status: document.getElementById(ids.status)
    };

    logWithTimestamp('debug', 'updateBrainStatusUI: mapping + presence', {
        casing, name, safe, ids,
        present: Object.fromEntries(Object.entries(els).map(([k, v]) => [k, !!v]))
    });

    if (els.lastCheckIn) {
        els.lastCheckIn.textContent = lastCheckIn ? formatDateTime(lastCheckIn) : 'Never';
        els.lastCheckIn.style.color = lastCheckIn ? '#28a745' : '#6c757d';
    }
    if (els.lastSnapshot) {
        els.lastSnapshot.textContent = lastSnapshot ? formatDateTime(lastSnapshot) : 'Never';
        els.lastSnapshot.style.color = lastSnapshot ? '#28a745' : '#6c757d';
    }
    if (els.errorCount) {
        els.errorCount.textContent = errorCount;
        els.errorCount.style.color = errorCount > 0 ? '#dc3545' : '#28a745';
    }
    if (els.marketCount) {
        els.marketCount.textContent = marketCount;
    }
    if (els.status) {
        let text = 'Active', color = '#28a745';
        if (isStartingUp) { text = 'Starting Up'; color = '#ffc107'; }
        else if (isShuttingDown) { text = 'Shutting Down'; color = '#dc3545'; }
        els.status.textContent = text; els.status.style.color = color;
    }
}

// Brain ticker management functions
async function confirmTargetTickersReceived(brainInstanceName) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        queueMessage('ConfirmTargetTickersReceived', brainInstanceName);
        console.log(`Queued confirmation of target tickers for ${brainInstanceName}`);
    } else {
        console.warn('SignalR connection not available for target tickers confirmation');
    }
}

async function requestTargetTickers(brainInstanceName) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        queueMessage('GetTargetTickers', brainInstanceName);
        console.log(`Queued request for target tickers for ${brainInstanceName}`);
    } else {
        console.warn('SignalR connection not available for target tickers request');
    }
}

async function updateCurrentTickers(brainInstanceName, tickers) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        queueMessage('UpdateCurrentTickers', brainInstanceName, tickers);
        console.log(`Queued update for current tickers for ${brainInstanceName}:`, tickers);
    } else {
        console.warn('SignalR connection not available for current tickers update');
    }
}

// Handle responses from server
function handleTargetTickersResponse(response) {
    if (response.Success) {
        console.log(`Target tickers received for ${response.BrainInstanceName}:`, response.Tickers);
        // Optionally update UI or store locally
    } else {
        console.error(`Failed to get target tickers for ${response.BrainInstanceName}:`, response.Message);
    }
}

function handleCurrentTickersResponse(response) {
    if (response.Success) {
        console.log(`Current tickers updated successfully for ${response.BrainInstanceName}`);
    } else {
        console.error(`Failed to update current tickers for ${response.BrainInstanceName}:`, response.Message);
    }
}

function handleTargetTickersConfirmationResponse(response) {
    if (response.Success) {
        console.log(`Target tickers confirmation acknowledged for ${response.BrainInstanceName}`);
        // Refresh brain locks display after confirmation
        setTimeout(refreshBrainLocksDisplay, 500);
    } else {
        console.error(`Target tickers confirmation failed for ${response.BrainInstanceName}:`, response.Message);
    }
}

// Handle CheckIn response from server
function handleCheckInResponse(response) {
    if (response.Success) {
        console.log('CheckIn acknowledged by server');
        // Handle target tickers if provided in response
        if (response.TargetTickers && response.TargetTickers.length > 0) {
            console.log('Target tickers received in CheckIn response:', response.TargetTickers);
            // Confirm receipt of target tickers
            confirmTargetTickersReceived(response.BrainInstanceName || 'unknown');
        }
        // Refresh brain locks display after CheckIn response
        setTimeout(refreshBrainLocksDisplay, 500);
    } else {
        console.error('CheckIn failed:', response.Message);
    }
}

function requestDataRefresh() {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        // Use message batching for high-frequency requests
        queueMessage('SendOverseerMessage', 'refresh_data', 'Request market data refresh');

        console.log('Refresh request queued for overseer');
        // Show loading state
        const refreshBtn = document.getElementById('refreshChartBtn');
        const originalText = refreshBtn.innerHTML;
        refreshBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Refreshing...';
        refreshBtn.disabled = true;

        // Re-enable after 5 seconds
        setTimeout(() => {
            refreshBtn.innerHTML = originalText;
            refreshBtn.disabled = false;
        }, 5000);
    } else {
        console.warn('SignalR connection not available');
        alert('Connection not available. Please refresh the page.');
    }
}
