/**
 * DATA MANAGEMENT MODULE
 *
 * This file handles all data loading, fetching, and management operations for the
 * Kalshi Trading Bot Dashboard. It provides functions to retrieve data from various
 * API endpoints and update the application's data stores.
 *
 * RATIONALE FOR SEPARATION:
 * - Isolates data fetching logic from UI rendering logic
 * - Provides a single point for API communication
 * - Enables easy mocking/testing of data operations
 * - Allows for centralized error handling and retry logic
 * - Makes it easy to switch data sources or add caching layers
 *
 * CONTENTS:
 * - Data loading functions for all major data types
 * - Account data management (balance, portfolio)
 * - Data refresh coordination
 * - Error handling for failed API requests
 * - Data transformation and preprocessing
 */

/**
 * Refreshes all application data by loading from all API endpoints in parallel
 * This is the main data refresh function called by auto-refresh timers
 */
async function refreshAllData() {
    await Promise.all([
        loadMarketWatchData(),
        loadBrainsData(),
        loadPositionsData(),
        loadOrdersData(),
        loadSnapshotsData(),
        loadAccountData()
    ]);
    updateLastUpdated();
}

/**
 * Loads account data (balance and portfolio value) from the backend API
 * Updates the global account display elements with the fetched data
 */
async function loadAccountData() {
    try {
        // Fetch balance and portfolio value from API
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.ACCOUNT);
        const data = await response.json();

        // Update the UI with the fetched data
        updateBalance(data.balance);
        updatePortfolioValue(data.portfolioValue);
    } catch (error) {
        console.error('Error loading account data:', error);
    }
}

/**
 * Loads market watch data from the backend API
 * This includes all market information, brain lock assignments, and market metrics
 * Automatically triggers UI updates after successful loading
 */
async function loadMarketWatchData() {
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.DATA);
        marketData = await response.json();
        updateBrainLockFilter();
        currentPage = 1; // Reset to first page when loading new data
        renderMarkets();
    } catch (error) {
        console.error('Error loading market data:', error);
        document.getElementById('marketsContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load market data</div></div>';
    }
}

/**
 * Loads brain instance data from the backend API
 * Transforms the data into the expected format for the UI components
 * Falls back gracefully if the endpoint is not available (404)
 */
async function loadBrainsData() {
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.BRAINS);
        if (!response.ok) {
            console.warn('Brains endpoint not available, falling back to SignalR data only');
            return;
        }
        const brainInstances = await response.json();

        // Convert to brainData format expected by the rest of the code
        brainData = {};
        brainInstances.forEach(brain => {
            const brainName = brain.brainInstanceName || brain.BrainInstanceName;
            if (brainName) {
                const normalizedName = brainName.toLowerCase();
                brainData[normalizedName] = {
                    ...brain,
                    brainInstanceName: brainName, // Preserve original casing for display
                    marketCount: brain.WatchedMarkets ? brain.WatchedMarkets.length : 0
                };
            }
        });

        console.log('Loaded brain data from backend:', Object.keys(brainData).length, 'brains');
        renderBrains();
    } catch (error) {
        console.error('Error loading brain data:', error);
        // Don't show error for 404, just skip
        if (!error.message.includes('404')) {
            document.getElementById('brainsContainer').innerHTML =
                '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load brain data</div></div>';
        }
    }
}

/**
 * Legacy function for refreshing brains display (maintained for compatibility)
 * Checks if brain data exists before attempting to render
 */
function refreshBrainsDisplay() {
    if (brainData && Object.keys(brainData).length > 0) {
        renderBrains();
    }
}

/**
 * Updates the brain lock filter dropdown with unique brain instance names
 * Extracts brain names from current market data and populates the filter options
 */
function updateBrainLockFilter() {
    const brainLockFilter = document.getElementById('brainLockFilter');
    const uniqueNames = Array.from(
        new Set((marketData || []).map(item => (item.brainInstanceName || item.BrainInstanceName)).filter(Boolean))
    ).sort((a, b) => a.localeCompare(b));
    brainLockFilter.innerHTML = `
<option value="">All</option>
${uniqueNames.map(name => `<option value="${name}">${name}</option>`).join('')}
`;
}

/**
 * Loads trading positions data from the backend API
 * Supports filtering for current positions only
 * Automatically triggers UI updates after successful loading
 */
async function loadPositionsData() {
    try {
        const currentOnly = document.getElementById('currentPositionsOnly').checked;
        const response = await fetch(`${CONFIG.API_BASE}${CONFIG.ENDPOINTS.POSITIONS}?currentOnly=${currentOnly}`);
        positionsData = await response.json();
        renderPositions();
    } catch (error) {
        console.error('Error loading positions data:', error);
        document.getElementById('positionsContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load positions data</div></div>';
    }
}

/**
 * Loads trading orders data from the backend API
 * Includes all order types (pending, filled, cancelled, expired)
 * Automatically triggers UI updates after successful loading
 */
async function loadOrdersData() {
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.ORDERS);
        ordersData = await response.json();
        renderOrders();
    } catch (error) {
        console.error('Error loading orders data:', error);
        document.getElementById('ordersContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load orders data</div></div>';
    }
}

/**
 * Loads market snapshots data from the backend API
 * Handles both JSON and text responses for flexibility
 * Automatically triggers UI updates after successful loading
 */
async function loadSnapshotsData() {
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.SNAPSHOTS);
        const rawText = await response.text();

        snapshotsData = JSON.parse(rawText);
        renderSnapshots();
    } catch (error) {
        console.error('Error loading snapshots data:', error);
        document.getElementById('snapshotsContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load snapshots data: ' + error.message + '</div></div>';
    }
}

/**
 * ACCOUNT DATA MANAGEMENT
 * Functions for updating account-related UI elements
 */

/**
 * Updates the account balance display element
 * @param {number} balance - Account balance in dollars
 */
function updateBalance(balance) {
    const balanceElement = document.getElementById('accountBalance');
    if (balanceElement) {
        balanceElement.textContent = `$${balance.toFixed(2)}`;
    }
}

/**
 * Updates the portfolio value display element
 * Shows placeholder for zero values until portfolio calculation is implemented
 * @param {number} value - Portfolio value in dollars
 */
function updatePortfolioValue(value) {
    const valueElement = document.getElementById('portfolioValue');
    if (valueElement) {
        // Show placeholder until portfolio calculation is implemented
        if (value === 0.0) {
            valueElement.textContent = '--';
        } else {
            valueElement.textContent = `$${value.toFixed(2)}`;
        }
    }
}

/**
 * Updates the "last updated" timestamp display
 * Called after data refresh operations to show when data was last synchronized
 */
function updateLastUpdated() {
    const now = new Date();
    document.getElementById('lastUpdated').textContent = `Last updated: ${now.toLocaleTimeString()}`;
}