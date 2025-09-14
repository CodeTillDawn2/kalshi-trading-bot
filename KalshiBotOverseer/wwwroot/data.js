/**
 * DATA MANAGEMENT MODULE
 *
 * This file handles all data loading, fetching, validation, and management operations for the
 * Kalshi Trading Bot Dashboard. It provides functions to retrieve data from various
 * API endpoints, validate responses, track performance metrics, and update the application's data stores.
 *
 * RATIONALE FOR SEPARATION:
 * - Isolates data fetching logic from UI rendering logic
 * - Provides a single point for API communication
 * - Enables easy mocking/testing of data operations
 * - Allows for centralized error handling and retry logic
 * - Makes it easy to switch data sources or add caching layers
 * - Includes data validation to prevent processing of malformed responses
 * - Tracks API performance metrics for monitoring and optimization
 *
 * CONTENTS:
 * - Performance metrics tracking (API timing, success rates)
 * - Data validation functions for all data types
 * - Data loading functions for all major data types (with validation and metrics)
 * - Account data management (balance, portfolio)
 * - Data refresh coordination
 * - Error handling for failed API requests
 * - Data transformation and preprocessing
 */

/**
 * PERFORMANCE METRICS TRACKING
 *
 * Tracks API call performance and reliability metrics across all data loading operations.
 * Provides insights into API response times, success rates, and failure patterns.
 */
let apiMetrics = {
    calls: 0,        // Total number of API calls made
    successes: 0,    // Number of successful API calls
    failures: 0,     // Number of failed API calls
    timings: [],     // Array of response times in milliseconds
    averageTiming: 0 // Calculated average response time
};

/**
 * Logs current API performance metrics to the console
 * Called after each API call to provide real-time monitoring
 * Displays total calls, success rate, failure count, and average response time
 */
function logMetrics() {
    const successRate = apiMetrics.calls > 0 ? (apiMetrics.successes / apiMetrics.calls * 100).toFixed(2) : 0;
    const avgTime = apiMetrics.timings.length > 0 ? (apiMetrics.timings.reduce((a, b) => a + b, 0) / apiMetrics.timings.length).toFixed(2) : 0;
    console.log(`API Metrics: ${apiMetrics.calls} calls, ${apiMetrics.successes} successes (${successRate}%), ${apiMetrics.failures} failures, Avg time: ${avgTime}ms`);
}

/**
 * DATA VALIDATION FUNCTIONS
 *
 * Validates API response data structures before processing to ensure data integrity.
 * Each function checks for expected data types and required fields to prevent
 * runtime errors from malformed API responses.
 */

/**
 * Validates account data structure received from the account API endpoint
 * Ensures balance and portfolioValue are present and numeric
 * @param {Object} data - Account data from API response
 * @returns {boolean} - True if data structure is valid for processing
 */
function validateAccountData(data) {
    return data && typeof data.balance === 'number' && typeof data.portfolioValue === 'number';
}

/**
 * Validates market watch data structure received from the data API endpoint
 * Ensures data is an array of objects representing market information
 * @param {Array} data - Market data array from API response
 * @returns {boolean} - True if data structure is valid for processing
 */
function validateMarketWatchData(data) {
    return Array.isArray(data) && data.every(item => item && typeof item === 'object');
}

/**
 * Validates brains data structure received from the brains API endpoint
 * Ensures data is an array of brain instances with required name fields
 * @param {Array} data - Brains data array from API response
 * @returns {boolean} - True if data structure is valid for processing
 */
function validateBrainsData(data) {
    return Array.isArray(data) && data.every(brain => brain && (brain.brainInstanceName || brain.BrainInstanceName));
}

/**
 * Validates positions data structure received from the positions API endpoint
 * Ensures data is an array of position objects
 * @param {Array} data - Positions data array from API response
 * @returns {boolean} - True if data structure is valid for processing
 */
function validatePositionsData(data) {
    return Array.isArray(data);
}

/**
 * Validates orders data structure received from the orders API endpoint
 * Ensures data is an array of order objects
 * @param {Array} data - Orders data array from API response
 * @returns {boolean} - True if data structure is valid for processing
 */
function validateOrdersData(data) {
    return Array.isArray(data);
}

/**
 * Validates snapshots data structure received from the snapshots API endpoint
 * Ensures data is either an array or object containing snapshot information
 * @param {Object|Array} data - Snapshots data from API response
 * @returns {boolean} - True if data structure is valid for processing
 */
function validateSnapshotsData(data) {
    return data && (Array.isArray(data) || typeof data === 'object');
}

/**
 * Refreshes all application data by loading from all API endpoints in parallel
 * This is the main data refresh function called by auto-refresh timers
 * Each load function includes data validation and performance metrics tracking
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
 * Includes data validation and performance metrics tracking
 * Updates the global account display elements with the fetched data
 */
async function loadAccountData() {
    const startTime = performance.now();
    try {
        // Fetch balance and portfolio value from API
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.ACCOUNT);
        const data = await response.json();

        // Validate data before processing
        if (!validateAccountData(data)) {
            throw new Error('Invalid account data structure');
        }

        // Update the UI with the fetched data
        updateBalance(data.balance);
        updatePortfolioValue(data.portfolioValue);

        // Update metrics
        apiMetrics.successes++;
    } catch (error) {
        console.error('Error loading account data:', error);
        apiMetrics.failures++;
    } finally {
        const duration = performance.now() - startTime;
        apiMetrics.timings.push(duration);
        apiMetrics.calls++;
        logMetrics();
    }
}

/**
 * Loads market watch data from the backend API
 * This includes all market information, brain lock assignments, and market metrics
 * Includes data validation and performance metrics tracking
 * Automatically triggers UI updates after successful loading
 */
async function loadMarketWatchData() {
    const startTime = performance.now();
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.DATA);
        marketData = await response.json();

        // Validate data before processing
        if (!validateMarketWatchData(marketData)) {
            throw new Error('Invalid market watch data structure');
        }

        updateBrainLockFilter();
        currentPage = 1; // Reset to first page when loading new data
        renderMarkets();

        // Update metrics
        apiMetrics.successes++;
    } catch (error) {
        console.error('Error loading market data:', error);
        document.getElementById('marketsContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load market data</div></div>';
        apiMetrics.failures++;
    } finally {
        const duration = performance.now() - startTime;
        apiMetrics.timings.push(duration);
        apiMetrics.calls++;
        logMetrics();
    }
}

/**
 * Loads brain instance data from the backend API
 * Transforms the data into the expected format for the UI components
 * Includes data validation and performance metrics tracking
 * Falls back gracefully if the endpoint is not available (404)
 */
async function loadBrainsData() {
    const startTime = performance.now();
    let madeCall = false;
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.BRAINS);
        madeCall = true;
        if (!response.ok) {
            console.warn('Brains endpoint not available, falling back to SignalR data only');
            return; // Don't count as failure
        }
        const brainInstances = await response.json();

        // Validate data before processing
        if (!validateBrainsData(brainInstances)) {
            throw new Error('Invalid brains data structure');
        }

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

        // Update metrics
        apiMetrics.successes++;
    } catch (error) {
        console.error('Error loading brain data:', error);
        // Don't show error for 404, just skip
        if (!error.message.includes('404')) {
            document.getElementById('brainsContainer').innerHTML =
                '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load brain data</div></div>';
            apiMetrics.failures++;
        }
    } finally {
        if (madeCall) {
            const duration = performance.now() - startTime;
            apiMetrics.timings.push(duration);
            apiMetrics.calls++;
            logMetrics();
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
 * Includes data validation and performance metrics tracking
 * Automatically triggers UI updates after successful loading
 */
async function loadPositionsData() {
    const startTime = performance.now();
    try {
        const currentOnly = document.getElementById('currentPositionsOnly').checked;
        const response = await fetch(`${CONFIG.API_BASE}${CONFIG.ENDPOINTS.POSITIONS}?currentOnly=${currentOnly}`);
        positionsData = await response.json();

        // Validate data before processing
        if (!validatePositionsData(positionsData)) {
            throw new Error('Invalid positions data structure');
        }

        renderPositions();

        // Update metrics
        apiMetrics.successes++;
    } catch (error) {
        console.error('Error loading positions data:', error);
        document.getElementById('positionsContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load positions data</div></div>';
        apiMetrics.failures++;
    } finally {
        const duration = performance.now() - startTime;
        apiMetrics.timings.push(duration);
        apiMetrics.calls++;
        logMetrics();
    }
}

/**
 * Loads trading orders data from the backend API
 * Includes all order types (pending, filled, cancelled, expired)
 * Includes data validation and performance metrics tracking
 * Automatically triggers UI updates after successful loading
 */
async function loadOrdersData() {
    const startTime = performance.now();
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.ORDERS);
        ordersData = await response.json();

        // Validate data before processing
        if (!validateOrdersData(ordersData)) {
            throw new Error('Invalid orders data structure');
        }

        renderOrders();

        // Update metrics
        apiMetrics.successes++;
    } catch (error) {
        console.error('Error loading orders data:', error);
        document.getElementById('ordersContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load orders data</div></div>';
        apiMetrics.failures++;
    } finally {
        const duration = performance.now() - startTime;
        apiMetrics.timings.push(duration);
        apiMetrics.calls++;
        logMetrics();
    }
}

/**
 * Loads market snapshots data from the backend API
 * Handles both JSON and text responses for flexibility
 * Includes data validation and performance metrics tracking
 * Automatically triggers UI updates after successful loading
 */
async function loadSnapshotsData() {
    const startTime = performance.now();
    try {
        const response = await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.SNAPSHOTS);
        const rawText = await response.text();

        snapshotsData = JSON.parse(rawText);

        // Validate data before processing
        if (!validateSnapshotsData(snapshotsData)) {
            throw new Error('Invalid snapshots data structure');
        }

        renderSnapshots();

        // Update metrics
        apiMetrics.successes++;
    } catch (error) {
        console.error('Error loading snapshots data:', error);
        document.getElementById('snapshotsContainer').innerHTML =
            '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Failed to load snapshots data: ' + error.message + '</div></div>';
        apiMetrics.failures++;
    } finally {
        const duration = performance.now() - startTime;
        apiMetrics.timings.push(duration);
        apiMetrics.calls++;
        logMetrics();
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