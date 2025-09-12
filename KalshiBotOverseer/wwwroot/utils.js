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
 * @param {string} message - Log message to send
 * @param {string} level - Log level (defaults to 'info')
 */
async function logToBackend(message, level = 'info') {
    try {
        await fetch(CONFIG.API_BASE + CONFIG.ENDPOINTS.LOG, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message, level })
        });
    } catch (error) {
        console.error('Failed to log to backend:', error);
    }
}

/**
 * Console logging utility with consistent formatting and timestamp
 * Provides structured logging with timestamps for debugging and monitoring
 * @param {string} level - Console method to use (log, warn, error, info, debug, etc.)
 * @param {string} msg - Log message to display
 * @param {*} obj - Optional object/data to log alongside the message
 */
function logWithTimestamp(level, msg, obj) {
    try {
        console[level](`[DASHBOARD][${new Date().toISOString()}] ${msg}`, obj ?? '');
    } catch (_) {
        // Fallback if console method doesn't exist
        console.log(`[DASHBOARD][${new Date().toISOString()}] ${msg}`, obj ?? '');
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