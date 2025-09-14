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

// GLOBAL ERROR HANDLING
// These handlers catch and log unhandled errors and promise rejections
// to prevent silent failures and provide debugging information
window.onerror = (message, source, line, column, error) =>
    console.error('[GLOBAL ERROR]', message, 'at', source + ':' + line + ':' + column, error);

window.onunhandledrejection = (event) =>
    console.error('[UNHANDLED PROMISE REJECTION]', event?.reason || event);

// APPLICATION INITIALIZATION
// Main entry point that coordinates the startup sequence
window.onload = async () => {
    try {
        // Set default tab to Brain Locks for immediate brain status visibility
        switchTab('brainlocks');

        // Initialize real-time communication with backend
        await initializeSignalR();

        // Load initial data from all API endpoints
        await refreshAllData();

        // Initialize active tab refresh for the default tab
        setupActiveTabRefresh('brainlocks');

        console.log('[APP] Application initialized successfully');
    } catch (error) {
        console.error('[APP] Failed to initialize application:', error);
        // Show user-friendly error message
        alert('Failed to initialize application. Please refresh the page.');
    }
};

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