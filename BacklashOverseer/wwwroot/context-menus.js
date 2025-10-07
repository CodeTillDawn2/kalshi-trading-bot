/**
 * CONTEXT MENU MANAGEMENT MODULE
 *
 * This file handles all right-click context menus throughout the Kalshi Trading Bot Dashboard.
 * Context menus provide quick access to actions specific to different data types
 * (markets, brains, positions, orders, snapshots) without cluttering the main UI.
 *
 * RATIONALE FOR SEPARATION:
 * - Isolates context menu logic from main UI rendering components
 * - Enables consistent menu behavior and styling across different data types
 * - Makes it easy to add/modify menu options per component type
 * - Provides centralized menu positioning, event handling, and state management
 * - Allows for easy menu theming, accessibility features, and future enhancements
 * - Separates user interaction logic from data display logic
 *
 * ARCHITECTURAL ROLE:
 * - Manages all right-click context menu interactions
 * - Coordinates between UI events and backend actions
 * - Handles menu state, positioning, and cleanup
 * - Provides consistent user experience across different data views
 * - Integrates with SignalR for real-time command execution
 *
 * CONTENTS:
 * - Context menu display, positioning, and lifecycle management
 * - Menu item handlers for different trading and monitoring actions
 * - Menu state management and automatic cleanup
 * - Event handling for menu triggers, selections, and external clicks
 * - Menu option filtering based on data state and permissions
 * - Integration with backend APIs for action execution
 */

let currentBrainInstance = '';
let currentMode = 'Autonomous';
let currentMarketTicker = '';
let currentMarketBrainLock = null;
let currentSnapshotTicker = '';

// Brain Context Menu Functions
function showBrainContextMenu(event, brainInstanceName) {
    event.stopPropagation(); // Prevent click from bubbling to document
    if (event.button === 2) event.preventDefault(); // Only prevent for right-click
    currentBrainInstance = brainInstanceName;
    const menu = document.getElementById('brainContextMenu');
    menu.style.left = event.pageX + 'px';
    menu.style.top = event.pageY + 'px';
    menu.style.display = 'block';

    // Update check marks for mode
    updateModeCheckMarks();
}

function hideBrainContextMenu() {
    document.getElementById('brainContextMenu').style.display = 'none';
}

function updateModeCheckMarks() {
    const menu = document.getElementById('brainContextMenu');
    const items = menu.querySelectorAll('.submenu-items li');
    items.forEach(item => {
        item.classList.remove('checked');
        if (item.textContent === currentMode) {
            item.classList.add('checked');
        }
    });
}

function handleWatchedMarkets() {
    logToBackend(`Watched Markets clicked for brain: ${currentBrainInstance}`);
    hideBrainContextMenu();
}

function handleChangeWatchSettings() {
    logToBackend(`Change Watch Settings clicked for brain: ${currentBrainInstance}`);
    hideBrainContextMenu();
}

function handleLogs() {
    logToBackend(`Logs clicked for brain: ${currentBrainInstance}`);
    hideBrainContextMenu();
}

function handleMode(mode) {
    logToBackend(`Change Mode to ${mode} clicked for brain: ${currentBrainInstance}`);
    currentMode = mode;
    updateModeCheckMarks();
    hideBrainContextMenu();
}

function handleReset(type) {
    logToBackend(`${type} Reset clicked for brain: ${currentBrainInstance}`);
    hideBrainContextMenu();
}

function handleStartUp() {
    logToBackend(`Start Up clicked for brain: ${currentBrainInstance}`);
    hideBrainContextMenu();
}

function handleShutdown(type) {
    logToBackend(`${type} Shutdown clicked for brain: ${currentBrainInstance}`);
    hideBrainContextMenu();
}

// Market Context Menu Functions
function showMarketContextMenu(event, marketTicker, brainInstanceName) {
    event.stopPropagation(); // Prevent click from bubbling to document
    if (event.button === 2) event.preventDefault(); // Only prevent for right-click
    currentMarketTicker = marketTicker;
    currentMarketBrainLock = brainInstanceName;
    const menu = document.getElementById('marketContextMenu');
    const menuItems = document.getElementById('marketMenuItems');

    // Build menu items based on brain lock status
    let items = `
        <li onclick="handleChart()">Chart</li>
        <li onclick="handleForceRefresh()">Force Refresh</li>
        <li onclick="handleMoveToQuarantine()">Move to Quarantine</li>
    `;

    if (!brainInstanceName) {
        items += `<li onclick="handleForceWatch()">Force Watch</li>`;
    } else {
        items += `<li onclick="handleTransferTo()">Transfer to...</li>`;
    }

    menuItems.innerHTML = items;

    menu.style.left = event.pageX + 'px';
    menu.style.top = event.pageY + 'px';
    menu.style.display = 'block';
}

function hideMarketContextMenu() {
    document.getElementById('marketContextMenu').style.display = 'none';
}

function handleBrainPerformance() {
    logToBackend(`Performance clicked for brain: ${currentBrainInstance}`);
    openPerformanceModal(currentBrainInstance);
    hideBrainContextMenu();
}

function openPerformanceModal(brainInstanceName) {
    const modal = document.getElementById('performanceModal');
    const title = document.getElementById('performanceTitle');
    const content = document.getElementById('performanceContent');

    title.textContent = `Performance - ${brainInstanceName}`;
    content.innerHTML = '<div class="loading"><i class="fas fa-spinner fa-spin"></i><div>Loading performance data...</div></div>';

    modal.style.display = 'flex';

    // Load performance data
    loadPerformanceData(brainInstanceName);
}

function closePerformanceModal() {
    document.getElementById('performanceModal').style.display = 'none';
}

function loadPerformanceData(brainInstanceName) {
    // For now, use the brainData if available
    const brainKey = brainInstanceName.toLowerCase();
    const brain = brainData[brainKey];

    if (!brain) {
        document.getElementById('performanceContent').innerHTML = '<div>No performance data available</div>';
        return;
    }

    // Build performance display based on GeneralPerformanceMetric structure
    let html = '<div style="display: grid; gap: 20px;">';

    // CPU Usage History
    if (brain.CpuUsageHistory && brain.CpuUsageHistory.length > 0) {
        html += createPerformanceSection('CPU Usage', brain.CpuUsageHistory, 'SpeedDial', '%');
    }

    // Event Queue History
    if (brain.EventQueueHistory && brain.EventQueueHistory.length > 0) {
        html += createPerformanceSection('Event Queue', brain.EventQueueHistory, 'Counter', 'count');
    }

    // Other histories...
    if (brain.TickerQueueHistory && brain.TickerQueueHistory.length > 0) {
        html += createPerformanceSection('Ticker Queue', brain.TickerQueueHistory, 'Counter', 'count');
    }

    if (brain.NotificationQueueHistory && brain.NotificationQueueHistory.length > 0) {
        html += createPerformanceSection('Notification Queue', brain.NotificationQueueHistory, 'Counter', 'count');
    }

    if (brain.OrderbookQueueHistory && brain.OrderbookQueueHistory.length > 0) {
        html += createPerformanceSection('Orderbook Queue', brain.OrderbookQueueHistory, 'Counter', 'count');
    }

    if (brain.MarketCountHistory && brain.MarketCountHistory.length > 0) {
        html += createPerformanceSection('Market Count', brain.MarketCountHistory, 'Counter', 'count');
    }

    if (brain.ErrorHistory && brain.ErrorHistory.length > 0) {
        html += createPerformanceSection('Errors', brain.ErrorHistory, 'Counter', 'count');
    }

    html += '</div>';

    document.getElementById('performanceContent').innerHTML = html;
}

function createPerformanceSection(name, history, visualType, unit) {
    const latest = history[history.length - 1];
    let controlHtml = '';

    switch (visualType) {
        case 'SpeedDial':
            // Create a simple gauge
            const percentage = Math.min(100, Math.max(0, latest.Value));
            controlHtml = `
                <div style="display: flex; align-items: center; gap: 10px;">
                    <div style="width: 100px; height: 100px; border-radius: 50%; background: conic-gradient(#28a745 0% ${percentage}%, #6c757d ${percentage}% 100%); display: flex; align-items: center; justify-content: center; color: white; font-weight: bold;">
                        ${latest.Value.toFixed(1)}${unit}
                    </div>
                    <div>
                        <div style="color: rgb(225, 221, 206);">${name}</div>
                        <div style="color: rgb(225, 221, 206); font-size: 12px;">Latest: ${formatDateTime(latest.Timestamp)}</div>
                    </div>
                </div>
            `;
            break;
        case 'Counter':
            controlHtml = `
                <div style="background: rgba(16, 16, 13, 0.9); border-radius: 15px; padding: 20px; border: 2px solid rgb(28, 51, 39);">
                    <div style="color: rgb(225, 221, 206); font-size: 24px; font-weight: bold; text-align: center;">${latest.Value}${unit}</div>
                    <div style="color: rgb(225, 221, 206); text-align: center;">${name}</div>
                    <div style="color: rgb(225, 221, 206); font-size: 12px; text-align: center;">${formatDateTime(latest.Timestamp)}</div>
                </div>
            `;
            break;
        case 'ProgressBar':
            const progress = Math.min(100, Math.max(0, latest.Value));
            controlHtml = `
                <div style="background: rgba(16, 16, 13, 0.9); border-radius: 15px; padding: 20px; border: 2px solid rgb(28, 51, 39);">
                    <div style="color: rgb(225, 221, 206); margin-bottom: 10px;">${name}</div>
                    <div style="width: 100%; height: 20px; background: rgba(28, 51, 39, 0.5); border-radius: 10px; overflow: hidden;">
                        <div style="width: ${progress}%; height: 100%; background: #28a745; transition: width 0.3s;"></div>
                    </div>
                    <div style="color: rgb(225, 221, 206); font-size: 12px; text-align: center; margin-top: 5px;">${latest.Value}${unit} - ${formatDateTime(latest.Timestamp)}</div>
                </div>
            `;
            break;
    }

    return `<div>${controlHtml}</div>`;
}

function handleChart() {
    logToBackend(`Chart clicked for market: ${currentMarketTicker}`);
    currentChartMarket = currentMarketTicker;
    openChartModal();
    renderChart();
    hideMarketContextMenu();

    // Automatically request data refresh when chart modal is opened
    setTimeout(() => {
        requestDataRefresh();
    }, 500); // Small delay to ensure modal is open
}

function handleForceRefresh() {
    logToBackend(`Force Refresh clicked for market: ${currentMarketTicker}`);
    hideMarketContextMenu();
}

function handleMoveToQuarantine() {
    logToBackend(`Move to Quarantine clicked for market: ${currentMarketTicker}`);
    hideMarketContextMenu();
}

function handleForceWatch() {
    logToBackend(`Force Watch clicked for market: ${currentMarketTicker}`);
    hideMarketContextMenu();
}

function handleTransferTo() {
    logToBackend(`Transfer to... clicked for market: ${currentMarketTicker} (brain: ${currentMarketBrainLock})`);
    hideMarketContextMenu();
}

// Positions Context Menu Functions
function showPositionsContextMenu(event) {
    event.stopPropagation();
    if (event.button === 2) event.preventDefault();
    const menu = document.getElementById('positionsContextMenu');
    menu.style.left = event.pageX + 'px';
    menu.style.top = event.pageY + 'px';
    menu.style.display = 'block';
}

function hidePositionsContextMenu() {
    document.getElementById('positionsContextMenu').style.display = 'none';
}

// Orders Context Menu Functions
function showOrdersContextMenu(event) {
    event.stopPropagation();
    if (event.button === 2) event.preventDefault();
    const menu = document.getElementById('ordersContextMenu');
    menu.style.left = event.pageX + 'px';
    menu.style.top = event.pageY + 'px';
    menu.style.display = 'block';
}

function hideOrdersContextMenu() {
    document.getElementById('ordersContextMenu').style.display = 'none';
}

// Snapshots Context Menu Functions
function showSnapshotContextMenu(event, marketTicker) {
    event.stopPropagation();
    if (event.button === 2) event.preventDefault();
    currentSnapshotTicker = marketTicker;
    const menu = document.getElementById('snapshotsContextMenu');
    menu.style.left = event.pageX + 'px';
    menu.style.top = event.pageY + 'px';
    menu.style.display = 'block';
}

function hideSnapshotsContextMenu() {
    document.getElementById('snapshotsContextMenu').style.display = 'none';
}

function handleSnapshotGroups() {
    logToBackend(`Groups clicked for snapshot: ${currentSnapshotTicker}`);
    hideSnapshotsContextMenu();
}

// Hide context menus on click outside
document.addEventListener('click', function(event) {
    const brainMenu = document.getElementById('brainContextMenu');
    const marketMenu = document.getElementById('marketContextMenu');
    const positionsMenu = document.getElementById('positionsContextMenu');
    const ordersMenu = document.getElementById('ordersContextMenu');
    const snapshotsMenu = document.getElementById('snapshotsContextMenu');
    const chartModal = document.getElementById('chartModal');

    if (!brainMenu.contains(event.target)) {
        hideBrainContextMenu();
    }
    if (!marketMenu.contains(event.target)) {
        hideMarketContextMenu();
    }
    if (!positionsMenu.contains(event.target)) {
        hidePositionsContextMenu();
    }
    if (!ordersMenu.contains(event.target)) {
        hideOrdersContextMenu();
    }
    if (!snapshotsMenu.contains(event.target)) {
        hideSnapshotsContextMenu();
    }

    // Close chart modal when clicking outside
    if (chartModal && chartModal.style.display === 'flex' && !chartModal.contains(event.target)) {
        closeChartModal();
    }

    // Close performance modal when clicking outside
    const performanceModal = document.getElementById('performanceModal');
    if (performanceModal && performanceModal.style.display === 'flex' && !performanceModal.contains(event.target)) {
        closePerformanceModal();
    }
});