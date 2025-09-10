// MAIN APPLICATION MODULE
//
// This file contains the main application initialization logic and coordination
// between different modules. It serves as the entry point for the application
// and manages the overall application lifecycle.
//
// RATIONALE FOR SEPARATION:
// - Provides a single entry point for application startup
// - Coordinates initialization of different modules
// - Manages global error handling and logging
// - Handles application-level events and state
// - Keeps main application logic separate from specific features
//
// CONTENTS:
// - Application initialization and startup sequence
// - Global error handling and logging setup
// - Module coordination and data flow management
// - Auto-refresh scheduling and lifecycle management
// - Tab switching and navigation coordination

window.onerror = (m, s, l, c, e) => console.error('[GLOBAL ERROR]', m, 'at', s + ':' + l + ':' + c, e);
window.onunhandledrejection = (ev) => console.error('[UNHANDLED REJECTION]', ev?.reason || ev);

// Initialize
window.onload = async () => {
    // Ensure Brain Locks tab is active on load
    switchTab('brainlocks');
    await initializeSignalR();
    await refreshAllData();
    // Auto-refresh all data every 30 seconds
    setInterval(refreshAllData, 30000);
};

// Refresh brains display function (for compatibility)
function refreshBrainsDisplay() {
    if (brainData && brainData.length > 0) {
        renderBrains();
    }
}

// Refresh brain locks display function (for compatibility)
function refreshBrainLocksDisplay() {
    renderBrains();
}