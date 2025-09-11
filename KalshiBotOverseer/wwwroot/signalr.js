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
 * Initializes the SignalR connection to the backend hub
 * Sets up event handlers for real-time updates and manages connection lifecycle
 * Automatically reconnects on connection loss
 */
async function initializeSignalR() {
    const hubUrl = CONFIG.SIGNALR_HUB;
    console.log('[BrainCards] building hub connection to', hubUrl);

    connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Expose for other functions already in the page
    window.connection = connection;

    // EVENT HANDLERS FOR REAL-TIME UPDATES

    /**
     * Handles real-time brain status updates from the backend
     * Updates brain data, check-in information, and triggers UI refreshes
     */
    connection.on('BrainStatusUpdate', (m) => {
        mwLog('debug', '[BrainCards] BrainStatusUpdate received', { keys: Object.keys(m || {}), sample: m });
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
     * Logs reconnection events for monitoring connection stability
     */
    connection.onreconnecting(err => console.warn('[BrainCards] reconnecting…', err));

    /**
     * Handles successful reconnection
     * Logs new connection ID for debugging purposes
     */
    connection.onreconnected(id => console.log('[BrainCards] reconnected, connId=', id));

    /**
     * Handles connection closure
     * Logs closure events to monitor connection issues
     */
    connection.onclose(err => console.warn('[BrainCards] closed', err));

    // Start
    try {
        await connection.start();
        console.log('[BrainCards] connection established, state=', connection.state);
    } catch (err) {
        console.error('[BrainCards] connection start failed:', err);
    }
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
    // Accept either BrainInstanceName or brainInstanceName for compatibility
    const pas = msg && msg.BrainInstanceName;
    const cam = msg && msg.brainInstanceName;
    const name = pas || cam || null;

    mwLog('debug', '[BrainCards] handleBrainStatusUpdate: entry', { name, hasPascal: !!pas, hasCamel: !!cam });

    if (!name) {
        mwLog('warn', '[BrainCards] handleBrainStatusUpdate: missing brain name; aborting', { keys: Object.keys(msg || {}) });
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
        mwLog('debug', '[BrainCards] handleBrainStatusUpdate: brainData merged - DB data preserved', {
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
        mwLog('warning', '[BrainCards] handleBrainStatusUpdate: brain not found in DB, using SignalR data only', { name, normalizedName });
    }
    mwLog('debug', '[BrainCards] handleBrainStatusUpdate: brainData merged', { name, normalizedName, storedKeys: Object.keys(brainData[normalizedName] || {}) });

    // Derive check-in badge fields for the brain lock card
    const lastCheckIn = msg.LastCheckIn ?? msg.lastCheckIn ?? null;
    const lastSnapshot = msg.LastSnapshot ?? msg.lastSnapshot ?? null;
    const marketCount = (msg.MarketCount ?? msg.marketCount) ?? 0;
    const errorCount = (msg.ErrorCount ?? msg.errorCount) ?? 0;
    const isStartingUp = !!(msg.IsStartingUp ?? msg.isStartingUp);
    const isShuttingDown = !!(msg.IsShuttingDown ?? msg.isShuttingDown);

    checkInData[normalizedName] = {
        brainInstanceName: name, // Preserve original casing for display
        marketCount,
        errorCount,
        lastSnapshot,
        lastCheckIn,
        isStartingUp,
        isShuttingDown
    };
    mwLog('debug', '[BrainCards] handleBrainStatusUpdate: checkInData updated', checkInData[normalizedName]);

    // Update the UI that depends on this status
    try { updateBrainStatusUI(msg); } catch (e) { mwLog('error', '[BrainCards] updateBrainStatusUI failed', e); }
    try { renderBrainCharts(msg); } catch (e) { mwLog('error', '[BrainCards] renderBrainCharts failed', e); }

    // Re-render brain cards so badges/timestamps change immediately
    try {
        renderBrains();
        mwLog('debug', '[BrainCards] handleBrainStatusUpdate: renderBrains done', { name, hasCheckIn: !!checkInData[name] });
    } catch (e) {
        mwLog('error', '[BrainCards] renderBrains failed', e);
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

        const brainInstanceName = checkInMessage.BrainInstanceName;
        const marketCount = checkInMessage.MarketCount ?? 0;
        const errorCount = checkInMessage.ErrorCount ?? 0;
        const lastSnapshot = checkInMessage.LastSnapshot ?? null;
        const lastCheckIn = checkInMessage.LastCheckIn ?? null;
        const isStartingUp = !!checkInMessage.IsStartingUp;
        const isShuttingDown = !!checkInMessage.IsShuttingDown;

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
    const pas = msg && msg.BrainInstanceName;
    const cam = msg && msg.brainInstanceName;
    const name = pas || cam || '';
    const casing = pas ? 'PascalCase' : (cam ? 'camelCase' : 'missing');
    const safe = name.replace(/[^a-zA-Z0-9]/g, '_');

    const lastCheckIn = msg.LastCheckIn ?? msg.lastCheckIn ?? null;
    const lastSnapshot = msg.LastSnapshot ?? msg.lastSnapshot ?? null;
    const errorCount = (msg.ErrorCount ?? msg.errorCount) ?? 0;
    const marketCount = (msg.MarketCount ?? msg.marketCount) ?? 0;
    const isStartingUp = !!(msg.IsStartingUp ?? msg.isStartingUp);
    const isShuttingDown = !!(msg.IsShuttingDown ?? msg.isShuttingDown);

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

    mwLog('debug', 'updateBrainStatusUI: mapping + presence', {
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
        try {
            await connection.invoke('ConfirmTargetTickersReceived', brainInstanceName);
            console.log(`Confirmed receipt of target tickers for ${brainInstanceName}`);
        } catch (error) {
            console.error('Error confirming target tickers receipt:', error);
        }
    } else {
        console.warn('SignalR connection not available for target tickers confirmation');
    }
}

async function requestTargetTickers(brainInstanceName) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke('GetTargetTickers', brainInstanceName);
            console.log(`Requested target tickers for ${brainInstanceName}`);
        } catch (error) {
            console.error('Error requesting target tickers:', error);
        }
    } else {
        console.warn('SignalR connection not available for target tickers request');
    }
}

async function updateCurrentTickers(brainInstanceName, tickers) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke('UpdateCurrentTickers', brainInstanceName, tickers);
            console.log(`Updated current tickers for ${brainInstanceName}:`, tickers);
        } catch (error) {
            console.error('Error updating current tickers:', error);
        }
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
        connection.invoke('SendOverseerMessage', 'refresh_data', 'Request market data refresh')
            .then(() => {
                console.log('Refresh request sent to overseer');
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
            })
            .catch(err => {
                console.error('Error sending refresh request:', err);
                alert('Failed to send refresh request');
            });
    } else {
        console.warn('SignalR connection not available');
        alert('Connection not available. Please refresh the page.');
    }
}
