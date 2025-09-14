/**
 * MAIN APPLICATION MODULE
 *
 * This file contains the main application initialization logic and coordination
 * between different modules. It serves as the entry point for the Kalshi Trading Bot
 * Dashboard application and manages the overall application lifecycle.
 *
 * RATIONALE FOR SEPARATION:
 * - Provides a single entry point for application startup and initialization
 * - Coordinates initialization of different modules (SignalR, data loading, UI)
 * - Manages global error handling and logging for debugging and monitoring
 * - Handles application-level events and state management
 * - Keeps main application logic separate from specific feature implementations
 * - Ensures proper startup sequence and dependency management
 *
 * ARCHITECTURAL ROLE:
 * - Entry point for the single-page application
 * - Coordinates between data management, real-time communication, and UI rendering
 * - Manages application state and lifecycle events
 * - Provides compatibility functions for legacy code paths
 *
 * CONTENTS:
 * - Global error handling setup for debugging and error reporting
 * - Application initialization sequence with proper async coordination
 * - Active tab refresh scheduling for real-time data updates
 * - Tab switching and navigation coordination
 * - Compatibility functions for legacy brain data handling
 * - SignalR connection establishment for real-time updates
 */

// SOPHISTICATED ERROR REPORTING SYSTEM
// Enhanced error handling with categorization, user feedback, and backend reporting

/**
 * Enhanced error logger with multiple output channels
 * @param {string} level - Error level: 'debug', 'info', 'warn', 'error', 'critical'
 * @param {string} message - Error message
 * @param {Error|Object} error - Error object or additional context
 * @param {Object} context - Additional context information
 */
function logError(level, message, error = null, context = {}) {
    const timestamp = new Date().toISOString();
    const errorData = {
        timestamp,
        level: level.toUpperCase(),
        message,
        source: 'DASHBOARD',
        userAgent: navigator.userAgent,
        url: window.location.href,
        context,
        stack: error?.stack || null
    };

    // Console logging with enhanced formatting
    const consoleMessage = `[${timestamp}] [${level.toUpperCase()}] ${message}`;
    switch (level) {
        case 'debug':
            console.debug(consoleMessage, error, context);
            break;
        case 'info':
            console.info(consoleMessage, error, context);
            break;
        case 'warn':
            console.warn(consoleMessage, error, context);
            break;
        case 'error':
        case 'critical':
            console.error(consoleMessage, error, context);
            break;
    }

    // Send critical errors to backend for monitoring
    if (level === 'error' || level === 'critical') {
        sendErrorToBackend(errorData);
    }

    // Show user-friendly alerts for critical errors
    if (level === 'critical') {
        showUserErrorAlert(message, error);
    }
}

/**
 * Send error data to backend for monitoring and analysis
 * @param {Object} errorData - Structured error information
 */
function sendErrorToBackend(errorData) {
    // Check if error reporting is enabled in configuration
    if (!CONFIG?.DASHBOARD?.ERROR_REPORTING_ENABLED) {
        return;
    }

    // Use the existing log endpoint for error reporting
    if (CONFIG?.ENDPOINTS?.LOG) {
        fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.LOG, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(errorData)
        }).catch(networkError => {
            // Don't recursively log network errors from error reporting
            console.warn('[ERROR REPORTING] Failed to send error to backend:', networkError);
        });
    }
}

/**
 * Show user-friendly error alert for critical issues
 * @param {string} message - User-friendly error message
 * @param {Error} error - Original error object
 */
function showUserErrorAlert(message, error) {
    const alertMessage = `An error occurred: ${message}\n\nPlease refresh the page or contact support if this persists.`;
    alert(alertMessage);
}

// GLOBAL ERROR HANDLING
// Enhanced handlers with sophisticated error reporting
window.onerror = (message, source, line, column, error) => {
    logError('error', `Global error: ${message}`, error, {
        source,
        line,
        column,
        filename: source.split('/').pop()
    });
    return false; // Allow default handling as well
};

window.onunhandledrejection = (event) => {
    const reason = event?.reason || event;
    const message = reason instanceof Error ? reason.message : 'Unhandled promise rejection';
    logError('error', `Unhandled promise rejection: ${message}`, reason, {
        type: 'unhandledrejection'
    });
};

// PERFORMANCE METRICS COLLECTION
// Track initialization timing and performance
const performanceMetrics = {
    startTime: null,
    endTime: null,
    steps: {}
};

/**
 * Start timing a performance step
 * @param {string} stepName - Name of the step being timed
 */
function startPerformanceTimer(stepName) {
    performanceMetrics.steps[stepName] = {
        start: performance.now(),
        end: null,
        duration: null
    };
}

/**
 * End timing a performance step
 * @param {string} stepName - Name of the step being timed
 */
function endPerformanceTimer(stepName) {
    if (performanceMetrics.steps[stepName]) {
        performanceMetrics.steps[stepName].end = performance.now();
        performanceMetrics.steps[stepName].duration =
            performanceMetrics.steps[stepName].end - performanceMetrics.steps[stepName].start;
    }
}

/**
 * Log performance metrics to console and backend
 */
function logPerformanceMetrics() {
    // Check if performance metrics collection is enabled
    if (!CONFIG?.DASHBOARD?.PERFORMANCE_METRICS_ENABLED) {
        return;
    }

    const totalDuration = performance.now() - performanceMetrics.startTime;
    const metrics = {
        totalInitializationTime: totalDuration,
        steps: performanceMetrics.steps,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent
    };

    logError('info', `Performance metrics: Total init time ${totalDuration.toFixed(2)}ms`, null, metrics);

    // Send performance data to backend for monitoring
    if (CONFIG?.ENDPOINTS?.LOG) {
        fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.LOG, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                type: 'performance',
                data: metrics
            })
        }).catch(err => {
            console.warn('[PERFORMANCE] Failed to send metrics to backend:', err);
        });
    }
}

// APPLICATION INITIALIZATION
// Main entry point that coordinates the startup sequence with performance tracking
window.onload = async () => {
    performanceMetrics.startTime = performance.now();

    try {
        logError('info', 'Starting application initialization');

        // Step 1: Set default tab
        startPerformanceTimer('tabSwitch');
        switchTab('brainlocks');
        endPerformanceTimer('tabSwitch');

        // Step 2: Initialize SignalR
        startPerformanceTimer('signalR');
        await initializeSignalR();
        endPerformanceTimer('signalR');

        // Step 3: Load initial data
        startPerformanceTimer('dataLoad');
        await refreshAllData();
        endPerformanceTimer('dataLoad');

        // Step 4: Setup refresh timers
        startPerformanceTimer('refreshSetup');
        setupActiveTabRefresh('brainlocks');
        endPerformanceTimer('refreshSetup');

        performanceMetrics.endTime = performance.now();
        logPerformanceMetrics();

        logError('info', 'Application initialized successfully');
    } catch (error) {
        performanceMetrics.endTime = performance.now();
        logPerformanceMetrics();

        logError('critical', 'Failed to initialize application', error, {
            initializationStep: 'unknown',
            totalTimeAttempted: performance.now() - performanceMetrics.startTime
        });
    }
};

// DYNAMIC CONFIGURATION MANAGEMENT
// Runtime configuration options for user customization

/**
 * Load user configuration from localStorage
 */
function loadUserConfig() {
    try {
        const savedConfig = localStorage.getItem('kalshiDashboardConfig');
        if (savedConfig) {
            const userConfig = JSON.parse(savedConfig);
            // Merge user config with defaults
            Object.keys(userConfig).forEach(key => {
                if (CONFIG.REFRESH_INTERVALS[key] !== undefined) {
                    CONFIG.REFRESH_INTERVALS[key] = userConfig[key];
                }
            });
            logError('info', 'User configuration loaded from localStorage', null, userConfig);
        }
    } catch (error) {
        logError('warn', 'Failed to load user configuration', error);
    }
}

/**
 * Save user configuration to localStorage
 */
function saveUserConfig() {
    try {
        const userConfig = {
            GLOBAL: CONFIG.REFRESH_INTERVALS.GLOBAL,
            MARKETS: CONFIG.REFRESH_INTERVALS.MARKETS,
            BRAINS: CONFIG.REFRESH_INTERVALS.BRAINS,
            POSITIONS: CONFIG.REFRESH_INTERVALS.POSITIONS,
            ORDERS: CONFIG.REFRESH_INTERVALS.ORDERS,
            SNAPSHOTS: CONFIG.REFRESH_INTERVALS.SNAPSHOTS,
            CHART: CONFIG.REFRESH_INTERVALS.CHART,
            ACCOUNT: CONFIG.REFRESH_INTERVALS.ACCOUNT
        };
        localStorage.setItem('kalshiDashboardConfig', JSON.stringify(userConfig));
        logError('info', 'User configuration saved to localStorage', null, userConfig);
    } catch (error) {
        logError('error', 'Failed to save user configuration', error);
    }
}

/**
 * Update refresh interval for a specific tab
 * @param {string} tabName - Name of the tab ('GLOBAL', 'MARKETS', etc.)
 * @param {number} intervalMs - New interval in milliseconds
 */
function updateRefreshInterval(tabName, intervalMs) {
    if (CONFIG.REFRESH_INTERVALS[tabName] !== undefined) {
        const minInterval = CONFIG?.DASHBOARD?.MIN_REFRESH_INTERVAL || 5000; // 5 seconds default
        const maxInterval = CONFIG?.DASHBOARD?.MAX_REFRESH_INTERVAL || 300000; // 5 minutes default

        // Validate interval is within configured bounds
        const validatedInterval = Math.max(minInterval, Math.min(maxInterval, intervalMs));

        if (validatedInterval !== intervalMs) {
            logError('warn', `Refresh interval for ${tabName} adjusted to stay within bounds: ${intervalMs}ms -> ${validatedInterval}ms`);
        }

        const oldInterval = CONFIG.REFRESH_INTERVALS[tabName];
        CONFIG.REFRESH_INTERVALS[tabName] = validatedInterval;

        // Restart refresh timers if they're running
        if (activeTabRefreshTimer) {
            clearActiveTabRefreshTimer();
            setupActiveTabRefresh(currentTab);
        }

        saveUserConfig();
        logError('info', `Refresh interval updated for ${tabName}: ${oldInterval}ms -> ${validatedInterval}ms`);
    } else {
        logError('warn', `Invalid tab name for refresh interval update: ${tabName}`);
    }
}

/**
 * Get current refresh interval for a tab
 * @param {string} tabName - Name of the tab
 * @returns {number} Current interval in milliseconds
 */
function getRefreshInterval(tabName) {
    return CONFIG.REFRESH_INTERVALS[tabName] || CONFIG.REFRESH_INTERVALS.GLOBAL;
}

/**
 * Reset all refresh intervals to defaults
 */
function resetRefreshIntervals() {
    CONFIG.REFRESH_INTERVALS = {
        GLOBAL: 30000,
        MARKETS: 30000,
        BRAINS: 30000,
        POSITIONS: 30000,
        ORDERS: 30000,
        SNAPSHOTS: 30000,
        CHART: 30000,
        ACCOUNT: 30000
    };
    localStorage.removeItem('kalshiDashboardConfig');

    // Restart refresh timers
    if (activeTabRefreshTimer) {
        clearActiveTabRefreshTimer();
        setupActiveTabRefresh(currentTab);
    }

    logError('info', 'Refresh intervals reset to defaults');
}

/**
 * Configure refresh intervals programmatically
 * Usage examples:
 * - updateRefreshInterval('GLOBAL', 60000); // 1 minute global refresh
 * - updateRefreshInterval('MARKETS', 15000); // 15 seconds for markets
 * - updateRefreshInterval('BRAINS', 45000); // 45 seconds for brains
 *
 * Available tabs: GLOBAL, MARKETS, BRAINS, POSITIONS, ORDERS, SNAPSHOTS, CHART, ACCOUNT
 * Minimum interval: 5000ms (5 seconds)
 */
function configureRefreshIntervals(config) {
    Object.keys(config).forEach(tab => {
        if (CONFIG.REFRESH_INTERVALS[tab] !== undefined) {
            updateRefreshInterval(tab, config[tab]);
        }
    });
}

// Load user configuration on startup
loadUserConfig();

// COMPATIBILITY FUNCTIONS
// These functions maintain backward compatibility with legacy code
// that expects different function names for brain data rendering

/**
 * Refreshes the brains display if brain data is available
 * Legacy compatibility function for brain data rendering
 */
function refreshBrainsDisplay() {
    if (brainData && Object.keys(brainData).length > 0) {
        renderBrains();
    }
}

/**
 * Refreshes the brain locks display by rendering brains
 * Legacy compatibility function that maps to current brain rendering
 */
function refreshBrainLocksDisplay() {
    renderBrains();
}