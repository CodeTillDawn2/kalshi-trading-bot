/**
 * CONFIGURATION AND CONSTANTS
 *
 * This file contains all application-wide configuration constants, API endpoints,
 * UI settings, and global state variables. It serves as the central configuration
 * hub for the Kalshi Trading Bot Dashboard application.
 *
 * RATIONALE FOR SEPARATION:
 * - Centralizes all configuration in one place for easy maintenance
 * - Eliminates magic numbers and hardcoded strings throughout the codebase
 * - Provides a single source of truth for application settings
 * - Makes it easy to modify behavior without touching multiple files
 * - Enables environment-specific configurations
 *
 * CONTENTS:
 * - API endpoints and base URLs
 * - SignalR connection settings
 * - Default UI behavior settings
 * - Status colors and visual constants
 * - Category icon mappings for market types
 * - Chart configuration defaults
 * - Tab navigation constants
 * - Global state variables (initialized here to avoid duplicates)
 */

const CONFIG = {
    // API endpoints - centralized for easy backend URL changes
    API_BASE: '/MarketWatch',
    ENDPOINTS: {
        DATA: '/data',           // Market data endpoint
        BRAINS: '/brains',       // Brain instances endpoint
        POSITIONS: '/positions', // Trading positions endpoint
        ORDERS: '/orders',       // Trading orders endpoint
        SNAPSHOTS: '/snapshots', // Market snapshots endpoint
        ACCOUNT: '/account',     // Account balance/portfolio endpoint
        LOG: '/log'             // Logging endpoint
    },

    // SignalR real-time communication settings
    SIGNALR_HUB: '/chartHub',

    // Enhanced SignalR configuration for connection management
    SIGNALR: {
        // Connection parameters
        LOG_LEVEL: signalR.LogLevel.Information,
        AUTOMATIC_RECONNECT: true,
        RECONNECT_INTERVALS: [0, 2000, 10000, 30000], // Custom reconnect intervals in ms
        MAX_RECONNECT_ATTEMPTS: 10,

        // Health monitoring
        PING_INTERVAL: 30000, // Ping every 30 seconds for health monitoring
        CONNECTION_QUALITY_THRESHOLD: 1000, // Consider connection poor if ping > 1s

        // Message batching for high-frequency updates
        MESSAGE_BATCH_SIZE: 10, // Batch up to 10 messages before sending
        MESSAGE_BATCH_DELAY: 100 // Delay in ms before sending batch
    },

    // Default application behavior settings
    DEFAULT_PAGE_SIZE: 50,              // Default items per page in grids

    // UI Refresh Intervals (in milliseconds) - Now configurable via backend and user preferences
    REFRESH_INTERVALS: {
        GLOBAL: 30000,                  // 30 seconds - Global data refresh interval
        MARKETS: 30000,                 // 30 seconds - Markets tab refresh interval
        BRAINS: 30000,                  // 30 seconds - Brains tab refresh interval
        POSITIONS: 30000,               // 30 seconds - Positions tab refresh interval
        ORDERS: 30000,                  // 30 seconds - Orders tab refresh interval
        SNAPSHOTS: 30000,               // 30 seconds - Snapshots tab refresh interval
        CHART: 30000,                   // 30 seconds - Chart modal refresh interval
        ACCOUNT: 30000                  // 30 seconds - Account data refresh interval
    },

    // Backend integration settings for new features
    DASHBOARD: {
        ERROR_REPORTING_ENABLED: true,
        PERFORMANCE_METRICS_ENABLED: true,
        MIN_REFRESH_INTERVAL: 5000,     // 5 seconds minimum
        MAX_REFRESH_INTERVAL: 300000    // 5 minutes maximum
    },

    // Legacy compatibility - maintain existing intervals
    AUTO_REFRESH_INTERVAL: 30000,       // 30 seconds - how often to refresh data
    CHART_AUTO_REFRESH_INTERVAL: 30000, // Chart modal auto-refresh interval

    // UI constants for consistent behavior
    SORT_ORDERS: {
        DESC: 'desc',  // Descending sort order
        ASC: 'asc'     // Ascending sort order
    },

    // Status colors for consistent visual feedback across the app
    STATUS_COLORS: {
        ACTIVE: '#28a745',           // Green for active/healthy states
        INACTIVE: '#dc3545',         // Red for inactive/error states
        WARNING: '#ffc107',          // Yellow/Orange for warnings
        OFFLINE: '#6c757d',          // Gray for offline/disconnected states
        PENDING: 'rgba(255, 193, 7, 0.8)',   // Yellow for pending states
        FILLED: 'rgba(40, 167, 69, 0.8)',    // Green for completed orders
        CANCELLED: 'rgba(220, 53, 69, 0.8)', // Red for cancelled states
        EXPIRED: 'rgba(108, 117, 125, 0.8)'  // Gray for expired states
    },

    // Category icon mappings for market visualization
    // Maps market categories to FontAwesome icons and colors
    CATEGORY_ICONS: {
        'Climate and Weather': { icon: 'fas fa-cloud-sun', color: '#17a2b8' },
        'Mentions': { icon: 'fas fa-at', color: '#6f42c1' },
        'Crypto': { icon: 'fab fa-bitcoin', color: '#f39c12' },
        'Entertainment': { icon: 'fas fa-film', color: '#e83e8c' },
        'Science and Technology': { icon: 'fas fa-microscope', color: '#28a745' },
        'Social': { icon: 'fas fa-users', color: '#007bff' },
        'Financials': { icon: 'fas fa-chart-line', color: '#28a745' },
        'Elections': { icon: 'fas fa-vote-yea', color: '#dc3545' },
        'Sports': { icon: 'fas fa-futbol', color: '#fd7e14' },
        'Health': { icon: 'fas fa-heartbeat', color: '#dc3545' },
        'World': { icon: 'fas fa-globe', color: '#6c757d' },
        'Transportation': { icon: 'fas fa-plane', color: '#17a2b8' },
        'Politics': { icon: 'fas fa-landmark', color: '#6c757d' },
        'Education': { icon: 'fas fa-graduation-cap', color: '#ffc107' },
        'Companies': { icon: 'fas fa-building', color: '#007bff' },
        'Economics': { icon: 'fas fa-chart-bar', color: '#28a745' }
    },

    // Default chart configuration settings
    CHART_DEFAULTS: {
        TIMEFRAME: '1h',     // Default timeframe for charts
        INDICATOR: 'none',   // Default technical indicator
        TIME_UNITS: {        // Timeframe configurations
            '15m': { points: 15, unit: 'minutes' },
            '1h': { points: 60, unit: 'minutes' },
            '3h': { points: 180, unit: 'minutes' },
            '1d': { points: 1440, unit: 'hours' },
            '3d': { points: 4320, unit: 'hours' },
            '1w': { points: 10080, unit: 'hours' }
        }
    },

    // Tab navigation constants for consistent tab switching
    TABS: {
        BRAINS: 'brainlocks',    // Brain instances tab
        MARKETS: 'markets',      // Markets overview tab
        POSITIONS: 'positions',  // Trading positions tab
        ORDERS: 'orders',        // Trading orders tab
        SNAPSHOTS: 'snapshots'   // Market snapshots tab
    },

    // Logging configuration for consistent logging behavior
    // PERFORMANCE CONSIDERATIONS:
    // - Set VERBOSITY to 'error' in production to reduce console noise
    // - Use 'json' format for log aggregation systems
    // - MAX_MESSAGE_LENGTH prevents memory issues with large log messages
    // - BACKEND logging can be disabled to reduce server load during debugging
    LOGGING: {
        // Log levels: 'debug', 'info', 'warn', 'error', 'none'
        // Lower levels include higher levels (debug includes info, warn, error)
        VERBOSITY: 'info',       // Default: show info and above (warn, error)

        // Log formats: 'simple', 'detailed', 'json'
        // 'simple': [DASHBOARD] message
        // 'detailed': [timestamp] [DASHBOARD] [LEVEL] message
        // 'json': {"timestamp": "...", "level": "...", "message": "...", "source": "..."}
        FORMAT: 'simple',        // Default: clean, readable format

        // Enable/disable specific log types for fine-grained control
        ENABLED_TYPES: {
            DEBUG: true,         // Debug messages (development only)
            INFO: true,          // Info messages (general application flow)
            WARN: true,          // Warning messages (potential issues)
            ERROR: true,         // Error messages (failures that need attention)
            BACKEND: true        // Backend logging (server-side persistence)
        },

        // Maximum log message length in characters (0 = unlimited)
        // Prevents memory issues and log file bloat from large objects
        MAX_MESSAGE_LENGTH: 500,

        // Include ISO timestamps in log output
        // Essential for debugging timing issues and log correlation
        INCLUDE_TIMESTAMP: true,

        // Include source identifier in logs
        // Helps identify which part of the application generated the log
        INCLUDE_SOURCE: true
    }
};

// GLOBAL STATE VARIABLES
// These are declared here to avoid duplicate declarations across files
// They will be initialized and managed by the respective modules

// Data arrays for different data types
let marketData = [];        // Array of market objects
let brainLockData = [];     // Legacy brain lock data (if still used)
let positionsData = [];     // Array of position objects
let ordersData = [];        // Array of order objects
let snapshotsData = [];     // Array of snapshot objects

// Navigation and UI state
let currentTab = CONFIG.TABS.BRAINS;     // Currently active tab
let previousTab = CONFIG.TABS.BRAINS;    // Previously active tab
let currentChartMarket = '';             // Market currently displayed in chart modal

// Sorting and pagination state
let sortOrder = CONFIG.SORT_ORDERS.DESC;         // Current sort order for markets
let positionsSortOrder = CONFIG.SORT_ORDERS.DESC; // Current sort order for positions
let ordersSortOrder = CONFIG.SORT_ORDERS.DESC;   // Current sort order for orders
let snapshotsSortOrder = CONFIG.SORT_ORDERS.DESC; // Current sort order for snapshots
let currentPage = 1;     // Current page number for pagination
let pageSize = CONFIG.DEFAULT_PAGE_SIZE; // Items per page
let filteredData = [];   // Currently filtered data array

// Brain and real-time data state
let brainData = {};      // Object containing brain instance data
let checkInData = {};    // Real-time check-in data from SignalR

// Connection and timing state
let connection = null;                   // SignalR connection object
let chartAutoRefreshInterval = null;     // Chart auto-refresh timer