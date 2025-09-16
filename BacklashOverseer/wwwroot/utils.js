/**
 * UTILITY FUNCTIONS
 *
 * This file contains reusable utility functions that are used throughout the
 * application. These functions handle common operations like date formatting,
 * status mapping, logging, and data transformation.
 *
 * RATIONALE FOR SEPARATION:
 * - Provides a centralized location for commonly used helper functions
 * - Eliminates code duplication across different modules
 * - Makes functions easily testable in isolation
 * - Allows for consistent behavior across the application
 * - Easier to maintain and update utility logic
 *
 * CONTENTS:
 * - Date and time formatting utilities
 * - Status and color mapping functions
 * - Category icon mapping for market visualization
 * - Logging utilities for debugging and monitoring
 * - Data transformation helpers
 */

// DATE AND TIME UTILITIES

/**
 * Formats a date string into a localized date-time string
 * @param {string} dateStr - ISO date string to format
 * @returns {string} - Formatted date-time string or '--' if invalid
 */
function formatDateTime(dateStr) {
    if (!dateStr) return '--';
    const date = new Date(dateStr);
    return date.toLocaleString();
}

// STATUS AND VISUAL MAPPING UTILITIES

/**
 * Maps market categories to FontAwesome icons and colors for visualization
 * @param {string} category - Market category name
 * @returns {Object} - Object with icon and color properties
 */
function getCategoryIcon(category) {
    if (!category) return { icon: 'fas fa-question-circle', color: '#6c757d' };

    // Use centralized category mapping from CONFIG
    return CONFIG.CATEGORY_ICONS[category] || { icon: 'fas fa-question-circle', color: '#6c757d' };
}

/**
 * Maps order/position status to CSS class names for styling
 * @param {string} status - Status string (filled, cancelled, expired, etc.)
 * @returns {string} - CSS class name ('positive', 'negative', or empty string)
 */
function getStatusClass(status) {
    if (!status) return '';
    const statusLower = status.toLowerCase();
    if (statusLower === 'filled') return 'positive';
    if (statusLower === 'cancelled' || statusLower === 'expired') return 'negative';
    return '';
}

/**
 * Maps status strings to RGBA color values for consistent visual feedback
 * @param {string} status - Status string to map
 * @returns {string} - RGBA color string
 */
function getStatusColor(status) {
    if (!status) return 'rgba(16, 16, 13, 0.8)';
    const statusLower = status.toLowerCase();

    // Use centralized status colors from CONFIG
    switch (statusLower) {
        case 'pending': return CONFIG.STATUS_COLORS.PENDING;
        case 'filled': return CONFIG.STATUS_COLORS.FILLED;
        case 'cancelled': return CONFIG.STATUS_COLORS.CANCELLED;
        case 'expired': return CONFIG.STATUS_COLORS.EXPIRED;
        default: return 'rgba(16, 16, 13, 0.8)';
    }
}

// LOGGING UTILITIES

/**
 * Sends log messages to the backend for persistent storage
 *
 * PERFORMANCE & RELIABILITY CONSIDERATIONS:
 * - Early returns prevent unnecessary network calls
 * - Message truncation prevents payload size issues
 * - Async operation doesn't block UI thread
 * - Graceful error handling prevents logging failures from breaking app
 *
 * @param {string} message - Log message to send
 * @param {string} level - Log level (defaults to 'info')
 */
async function logToBackend(message, level = 'info') {
    // PERFORMANCE: Early return if backend logging disabled
    // Saves network round-trip and server processing
    if (!CONFIG.LOGGING.ENABLED_TYPES.BACKEND) {
        return;
    }

    // PERFORMANCE: Early return if this log level disabled
    // Prevents processing of unwanted log levels
    if (!CONFIG.LOGGING.ENABLED_TYPES[level.toUpperCase()]) {
        return;
    }

    // RELIABILITY: Truncate message to prevent oversized payloads
    // Large messages can cause network timeouts or server errors
    let processedMessage = message;
    if (CONFIG.LOGGING.MAX_MESSAGE_LENGTH > 0 && message.length > CONFIG.LOGGING.MAX_MESSAGE_LENGTH) {
        processedMessage = message.substring(0, CONFIG.LOGGING.MAX_MESSAGE_LENGTH) + '...';
    }

    try {
        // RELIABILITY: Fire-and-forget async operation
        // Don't await to prevent blocking UI on slow networks
        await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.LOG, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: processedMessage, level })
        });
    } catch (error) {
        // RELIABILITY: Silent failure handling
        // Logging failures shouldn't break the application
        // Consider console.error only in development
        console.error('Failed to log to backend:', error);
    }
}

/**
 * Console logging utility with consistent formatting and timestamp
 *
 * FLEXIBILITY & PERFORMANCE FEATURES:
 * - Multiple output formats (simple, detailed, JSON) for different use cases
 * - Hierarchical verbosity control (debug < info < warn < error)
 * - Configurable message truncation to prevent console bloat
 * - Optional timestamps and source identification
 * - Graceful fallback for unsupported console methods
 *
 * USE CASES:
 * - Development: Use 'debug' verbosity with 'detailed' format
 * - Production: Use 'error' verbosity with 'simple' format
 * - Log aggregation: Use 'json' format with timestamps
 *
 * @param {string} level - Console method to use (log, warn, error, info, debug, etc.)
 * @param {string} msg - Log message to display
 * @param {*} obj - Optional object/data to log alongside the message
 */
function logWithTimestamp(level, msg, obj) {
    // PERFORMANCE: Early return if log level disabled
    // Prevents unnecessary string processing and console operations
    if (!CONFIG.LOGGING.ENABLED_TYPES[level.toUpperCase()]) {
        return;
    }

    // PERFORMANCE: Hierarchical verbosity filtering
    // Lower verbosity levels include higher priority messages
    // Example: 'info' level includes 'warn' and 'error' but not 'debug'
    const levelPriority = { 'debug': 0, 'info': 1, 'warn': 2, 'error': 3 };
    const currentPriority = levelPriority[CONFIG.LOGGING.VERBOSITY] ?? 1;
    const messagePriority = levelPriority[level] ?? 1;

    if (messagePriority < currentPriority) {
        return;
    }

    // RELIABILITY: Message truncation prevents console performance issues
    // Large objects or messages can slow down or crash browser console
    let processedMessage = msg;
    if (CONFIG.LOGGING.MAX_MESSAGE_LENGTH > 0 && msg.length > CONFIG.LOGGING.MAX_MESSAGE_LENGTH) {
        processedMessage = msg.substring(0, CONFIG.LOGGING.MAX_MESSAGE_LENGTH) + '...';
    }

    // FLEXIBILITY: Multiple output formats for different scenarios
    let formattedMessage;
    if (CONFIG.LOGGING.FORMAT === 'json') {
        // JSON format: Perfect for log aggregation systems and structured analysis
        const logEntry = {
            timestamp: CONFIG.LOGGING.INCLUDE_TIMESTAMP ? new Date().toISOString() : undefined,
            level: level,
            message: processedMessage,
            source: CONFIG.LOGGING.INCLUDE_SOURCE ? 'DASHBOARD' : undefined,
            data: obj
        };
        // Clean up undefined properties for cleaner JSON
        Object.keys(logEntry).forEach(key => logEntry[key] === undefined && delete logEntry[key]);
        formattedMessage = JSON.stringify(logEntry);
    } else if (CONFIG.LOGGING.FORMAT === 'detailed') {
        // Detailed format: Human-readable with full context
        let parts = [];
        if (CONFIG.LOGGING.INCLUDE_TIMESTAMP) parts.push(`[${new Date().toISOString()}]`);
        if (CONFIG.LOGGING.INCLUDE_SOURCE) parts.push('[DASHBOARD]');
        parts.push(`[${level.toUpperCase()}]`);
        parts.push(processedMessage);
        formattedMessage = parts.join(' ');
    } else { // simple format (default)
        // Simple format: Clean, minimal output for production
        let prefix = '[DASHBOARD]';
        if (CONFIG.LOGGING.INCLUDE_TIMESTAMP) {
            prefix += `[${new Date().toISOString()}]`;
        }
        formattedMessage = `${prefix} ${processedMessage}`;
    }

    try {
        // RELIABILITY: Use appropriate console method with fallback
        // Some environments may not support all console methods
        console[level](formattedMessage, obj ?? '');
    } catch (_) {
        // Fallback to console.log if the specific method doesn't exist
        // Ensures logging always works regardless of environment
        console.log(formattedMessage, obj ?? '');
    }
}

// DATA TRANSFORMATION UTILITIES

/**
 * Resolves brain instance name from SignalR message, handling different casing
 * @param {Object} m - SignalR message object
 * @returns {Object} - Object with name and casing properties
 */
function resolveNameFromMsg(m) {
    const pas = m && m.BrainInstanceName;  // PascalCase
    const cam = m && m.brainInstanceName;  // camelCase
    const name = pas || cam || null;
    const casing = pas ? 'PascalCase' : (cam ? 'camelCase' : 'missing');
    return { name, casing };
}