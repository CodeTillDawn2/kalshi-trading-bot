/**
 * USER INTERFACE RENDERING MODULE
 *
 * This file contains all functions responsible for rendering and updating the
 * user interface components of the Kalshi Trading Bot Dashboard. It handles
 * the creation of HTML elements, data visualization, and UI state management.
 *
 * RATIONALE FOR SEPARATION:
 * - Isolates UI rendering logic from data management and business logic
 * - Enables easy UI refactoring and theming changes
 * - Makes UI components reusable across different parts of the application
 * - Allows for easier testing of UI behavior
 * - Separates presentation concerns from data concerns
 *
 * CONTENTS:
 * - Market data rendering (cards, grids, pagination)
 * - Brain instance visualization and status display
 * - Chart rendering and data visualization
 * - Position and order table generation
 * - Snapshot data display
 * - UI state management and updates
 * - Tab switching and navigation
 * - Filtering and sorting UI updates
 */

// MARKET RENDERING FUNCTIONS

/**
 * Renders the markets grid with filtering, sorting, and pagination
 * Displays market cards in a responsive grid layout
 */
function renderMarkets() {
    const container = document.getElementById('marketsContainer');
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const brainLockFilter = document.getElementById('brainLockFilter').value;
    const sortBy = document.getElementById('sortSelect').value;

    filteredData = marketData.filter(item => {
        const matchesSearch = !searchTerm ||
            item.market_ticker.toLowerCase().includes(searchTerm) ||
            (item.market?.title || '').toLowerCase().includes(searchTerm) ||
            (item.market?.subtitle || '').toLowerCase().includes(searchTerm);

        const matchesBrainLock = !brainLockFilter ||
            (brainLockFilter === 'none' && !(item.brainInstanceName || item.BrainInstanceName)) ||
            (item.brainInstanceName || item.BrainInstanceName) == brainLockFilter;

        return matchesSearch && matchesBrainLock;
    });

    // Sort data
    filteredData.sort((a, b) => {
        let aVal, bVal;

        switch (sortBy) {
            case 'interestScore':
                aVal = a.interestScore || 0;
                bVal = b.interestScore || 0;
                break;
            case 'websocketEvents':
                aVal = a.averageWebsocketEventsPerMinute || 0;
                bVal = b.averageWebsocketEventsPerMinute || 0;
                break;
            case 'lastWatched':
                aVal = new Date(a.lastWatched || 0);
                bVal = new Date(b.lastWatched || 0);
                break;
            case 'brainLock':
                aVal = (a.brainInstanceName || a.BrainInstanceName) || '';
                bVal = (b.brainInstanceName || b.BrainInstanceName) || '';
                break;
            case 'title':
                aVal = a.market?.title || '';
                bVal = b.market?.title || '';
                break;
            default: // ticker
                aVal = a.market_ticker;
                bVal = b.market_ticker;
        }

        if (sortOrder === 'asc') {
            return aVal > bVal ? 1 : -1;
        } else {
            return aVal < bVal ? 1 : -1;
        }
    });

    if (filteredData.length === 0) {
        container.innerHTML = '<div class="empty-state"><i class="fas fa-search"></i><div>No markets match your filters</div></div>';
        document.getElementById('paginationControls').style.display = 'none';
        return;
    }

    // Calculate pagination
    const totalPages = Math.ceil(filteredData.length / pageSize);
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = Math.min(startIndex + pageSize, filteredData.length);
    const pageData = filteredData.slice(startIndex, endIndex);

    const marketCards = pageData.map(item => createMarketCard(item)).filter(card => card !== '');
    container.innerHTML = `
        <div class="market-grid">
            ${marketCards.join('')}
        </div>
    `;

    // Update pagination controls
    updatePaginationControls(totalPages, startIndex + 1, endIndex, filteredData.length);
}

/**
 * Creates HTML for a single market card
 * @param {Object} item - Market data object
 * @returns {string} - HTML string for the market card
 */
function createMarketCard(item) {
    // Skip rendering if market data is missing or incomplete
    if (!item.market || !item.market.title || item.market.title.trim() === '') {
        return ''; // Don't render cards with missing market data
    }

    const market = item.market;
    const statusClass = market.status === 'active' ? 'status-active' : 'status-closed';
    const categoryInfo = getCategoryIcon(market.category);

    // Use yes_sub_title if subtitle is blank
    const displaySubtitle = (market.subtitle && market.subtitle.trim()) ? market.subtitle : (market.yes_sub_title || 'No Subtitle');

    // Escape strings for safe use in HTML attributes
    const escapedMarketTicker = item.market_ticker.replace(/'/g, "\\'").replace(/"/g, '\\"');
    const escapedBrainName = (item.brainInstanceName || item.BrainInstanceName) ? (item.brainInstanceName || item.BrainInstanceName).replace(/'/g, "\\'").replace(/"/g, '\\"') : null;

    const cardHtml = `
        <div class="market-card" onclick="showMarketContextMenu(event, '${escapedMarketTicker}', ${escapedBrainName ? `'${escapedBrainName}'` : 'null'})" oncontextmenu="showMarketContextMenu(event, '${escapedMarketTicker}', ${escapedBrainName ? `'${escapedBrainName}'` : 'null'})">
            <!-- Title Group at Top -->
            <div class="title-group">
                <div class="market-ticker" title="${market.title.replace(/"/g, '"')}">
                    <i class="${categoryInfo.icon}" style="color: ${categoryInfo.color}; font-size: 14px;" title="${(market.category || 'Unknown Category').replace(/"/g, '"')}"></i>
                    <span class="title-text">${market.title.replace(/</g, '<').replace(/>/g, '>')}</span>
                </div>
                <div class="market-subtitle" title="${displaySubtitle.replace(/"/g, '"')}">${displaySubtitle.replace(/</g, '<').replace(/>/g, '>')}</div>
            </div>

            <!-- Status and Brain Lock -->
            <div class="status-section">
                <span class="market-status ${statusClass}">${market.status || 'Unknown'}</span>
                ${(item.brainInstanceName || item.BrainInstanceName) ? `<div class="brain-instance">${(item.brainInstanceName || item.BrainInstanceName).replace(/</g, '<').replace(/>/g, '>')}</div>` : '<div class="brain-instance no-brain">No Brain</div>'}
            </div>

            <!-- Metrics Section -->
            <div class="market-metrics">
                <div class="metric-item">
                    <div class="metric-label">Interest</div>
                    <div class="metric-value ${item.interestScore > 0 ? 'positive' : ''}">
                        ${item.interestScore ? item.interestScore.toFixed(1) : 'N/A'}
                    </div>
                </div>
                <div class="metric-item">
                    <div class="metric-label">Events/Min</div>
                    <div class="metric-value">
                        ${item.averageWebsocketEventsPerMinute ? item.averageWebsocketEventsPerMinute.toFixed(0) : 'N/A'}
                    </div>
                </div>
                <div class="metric-item">
                    <div class="metric-label">Last Seen</div>
                    <div class="metric-value" style="font-size: 10px;" title="${item.lastWatched ? formatDateTime(item.lastWatched) : 'Never'}">
                        ${item.lastWatched ? formatDateTime(item.lastWatched) : 'Never'}
                    </div>
                </div>
            </div>

            <!-- Market Details -->
            <div class="market-details">
                <div style="margin-top: 4px; font-size: 9px; color: #cccccc; line-height: 1.2;">
                    Vol: ${market.volume ? market.volume.toLocaleString() : 0} |
                    Liq: ${market.liquidity ? market.liquidity.toLocaleString() : 0} |
                    Price: ${market.last_price || 0}
                </div>
            </div>
        </div>
    `;

    console.log('DEBUG: Generated card HTML for', (item.brainInstanceName || item.BrainInstanceName), ':', cardHtml.substring(0, 200) + '...');
    return cardHtml;
}

// PAGINATION MANAGEMENT

/**
 * Updates pagination controls display and state
 * @param {number} totalPages - Total number of pages
 * @param {number} startItem - First item number on current page
 * @param {number} endItem - Last item number on current page
 * @param {number} totalItems - Total number of items
 */
function updatePaginationControls(totalPages, startItem, endItem, totalItems) {
    const controls = document.getElementById('paginationControls');
    const info = document.getElementById('paginationInfo');
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('currentPageInfo');

    if (totalPages <= 1) {
        controls.style.display = 'none';
        return;
    }

    controls.style.display = 'flex';
    info.textContent = `Showing ${startItem}-${endItem} of ${totalItems} markets`;
    pageInfo.textContent = `Page ${currentPage} of ${totalPages}`;

    prevBtn.disabled = currentPage <= 1;
    nextBtn.disabled = currentPage >= totalPages;
}

/**
 * Changes the current page and re-renders the markets
 * @param {number} direction - Direction to change page (-1 for previous, 1 for next)
 */
function changePage(direction) {
    currentPage += direction;
    renderMarkets();
}

/**
 * Updates page size and resets to first page
 */
function changePageSize() {
    pageSize = parseInt(document.getElementById('pageSizeSelect').value);
    currentPage = 1;
    renderMarkets();
}

// BRAIN RENDERING FUNCTIONS

/**
 * Renders the brain instances grid
 * Displays brain cards with real-time status information
 */
function renderBrains() {
    const container = document.getElementById('brainsContainer');

    if (!brainData || typeof brainData !== 'object') {
        container.innerHTML = '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Invalid brain data received</div></div>';
        return;
    }

    // Convert brainData object to array of brain objects
    const brainArray = Object.values(brainData);
    if (brainArray.length === 0) {
        container.innerHTML = '<div class="empty-state"><i class="fas fa-brain"></i><div>No brains found</div></div>';
        return;
    }

    try {
        logWithTimestamp('debug', 'renderBrains: keys present in checkInData',
            { keys: Object.keys(checkInData || {}) });

        // Backend has already filtered the data, just use what's provided
        const filteredBrainData = brainArray;
        console.log('DEBUG: Brain data from backend:', filteredBrainData.length, 'brains');

        container.innerHTML = `
    <div class="brain-grid">
        ${filteredBrainData.map(item => createBrainCard(item)).join('')}
    </div>
`;
        // ensure the tab is actually visible if user is on Brains
        if (currentTab === 'brainlocks') {
            const tab = document.getElementById('brainlocks-tab');
            if (tab) {
                tab.style.display = 'block';
                tab.style.visibility = 'visible';
            }
        }
        console.log('DEBUG: Brains rendered successfully');
    } catch (error) {
        console.error('DEBUG: Error rendering brains:', error);
        container.innerHTML = '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Error rendering brains: ' + error.message + '</div></div>';
    }
}

/**
 * Creates HTML for a single brain card
 * @param {Object} item - Brain instance data object
 * @returns {string} - HTML string for the brain card
 */
function createBrainCard(item) {
    // Handle both camelCase and PascalCase property names
    const brainInstanceName = item.brainInstanceName || item.BrainInstanceName;
    const normalizedName = brainInstanceName?.toLowerCase();
    console.log('DEBUG: createBrainCard called for:', brainInstanceName, 'normalized:', normalizedName);
    console.log('DEBUG: checkInData keys:', Object.keys(checkInData));
    console.log('DEBUG: checkInData for normalized name:', checkInData[normalizedName]);

    if (!item || !brainInstanceName) {
        console.error('DEBUG: Invalid item or missing brainInstanceName:', item);
        return '<div class="error">Invalid brain data</div>';
    }

    const brainCheckInData = checkInData[normalizedName];
    let statusText = 'Inactive';
    let statusColor = '#dc3545';
    let hasRecentCheckIn = false;
    let cardClass = 'brain-card inactive';

    console.log('DEBUG: brainCheckInData found:', !!brainCheckInData);
    if (brainCheckInData) {
        console.log('DEBUG: brainCheckInData content:', brainCheckInData);
    }

    if (brainCheckInData && brainCheckInData.lastCheckIn) {
        const timeSinceCheckIn = new Date() - new Date(brainCheckInData.lastCheckIn);
        console.log('DEBUG: timeSinceCheckIn:', timeSinceCheckIn);
        if (timeSinceCheckIn < 300000) {
            statusText = 'Active';
            statusColor = '#28a745';
            hasRecentCheckIn = true;
            cardClass = 'brain-card'; // Remove inactive
        } else {
            statusText = 'Offline';
            statusColor = '#6c757d';
        }
    } else if (brainCheckInData) {
        // No lastCheckIn but data exists
        statusText = 'Unknown';
        statusColor = '#6c757d';
    }

    // Apply color logic based on check-in and snapshot status
    if (!hasRecentCheckIn) {
        cardClass = 'brain-card inactive';
        statusText = 'Inactive';
        statusColor = '#dc3545';
    } else {
        // Check if snapshot is stale (>3 minutes before last check-in)
        if (brainCheckInData.lastSnapshot && brainCheckInData.lastCheckIn) {
            const checkInTime = new Date(brainCheckInData.lastCheckIn);
            const snapshotTime = new Date(brainCheckInData.lastSnapshot);
            const diff = checkInTime - snapshotTime;
            if (diff > 180000) { // 3 minutes in milliseconds
                cardClass = 'brain-card warning';
                statusText = 'Stale Snapshot';
                statusColor = '#dc3545';
            }
        }
    }

    const escapedBrainName = brainInstanceName.replace(/'/g, "\\'").replace(/"/g, '\\"');

    const cardHtml = `
            <div class="${cardClass}" onclick="showBrainContextMenu(event, '${escapedBrainName}')" oncontextmenu="showBrainContextMenu(event, '${escapedBrainName}')">
                <div class="brain-header">
                    <div class="brain-id">
                        <i class="fas fa-brain"></i>
                        ${brainInstanceName}
                    </div>
                    <div style="font-size: 14px; color: #cccccc;">
                        ${hasRecentCheckIn ? checkInData[normalizedName]?.marketCount + ' markets' : 'Not Connected'}
                    </div>
                    <div id="status-indicator-${brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_')}" style="font-size: 12px; font-weight: 600; color: ${statusColor};">
                        ${statusText}
                    </div>
                </div>

                <div class="brain-stats">
                    <div class="stat-item">
                        <span class="stat-value">${hasRecentCheckIn ? checkInData[normalizedName]?.marketCount : item.marketCount || '--'}</span>
                        <div class="stat-label">Current Markets</div>
                    </div>
                    <div class="stat-item">
                        <span class="stat-value">${item.marketCount || '--'}</span>
                        <div class="stat-label">Target Watches</div>
                    </div>
                    <div class="stat-item">
                        <span class="stat-value" id="last-checkin-${brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_')}">${hasRecentCheckIn ? formatDateTime(checkInData[normalizedName]?.lastCheckIn) : 'Never'}</span>
                        <div class="stat-label">Last Check In</div>
                    </div>
                    <div class="stat-item">
                        <span class="stat-value" id="last-snapshot-${brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_')}">${hasRecentCheckIn ? formatDateTime(checkInData[normalizedName]?.lastSnapshot) : 'Never'}</span>
                        <div class="stat-label">Last Snapshot</div>
                    </div>
                    <div class="stat-item">
                        <canvas id="error-chart-${brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_')}" width="60" height="30" style="background: rgba(28, 51, 39, 0.1); border-radius: 4px;"></canvas>
                        <div class="stat-label" style="font-size: 10px;">Error Graph</div>
                    </div>
                    <div class="stat-item">
                        <span class="stat-value">${item.mode || 'Autonomous'}</span>
                        <div class="stat-label">Mode</div>
                    </div>
                    <div class="stat-item">
                        <canvas id="cpu-chart-${brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_')}" width="60" height="30" style="background: rgba(28, 51, 39, 0.1); border-radius: 4px;"></canvas>
                        <div class="stat-label" style="font-size: 10px;">CPU Usage</div>
                    </div>
                    <div class="stat-item">
                        <canvas id="event-chart-${brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_')}" width="60" height="30" style="background: rgba(28, 51, 39, 0.1); border-radius: 4px;"></canvas>
                        <div class="stat-label" style="font-size: 10px;">Event Queue</div>
                    </div>
                    <div class="stat-item">
                        <canvas id="orderbook-chart-${brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_')}" width="60" height="30" style="background: rgba(28, 51, 39, 0.1); border-radius: 4px;"></canvas>
                        <div class="stat-label" style="font-size: 10px;">Orderbook Queue</div>
                    </div>
                </div>
            </div>
        `;

    return cardHtml;
}

/**
 * NAVIGATION AND TAB MANAGEMENT
 */

// Active tab refresh timer
let activeTabRefreshTimer = null;

/**
 * Clears the active tab refresh timer
 */
function clearActiveTabRefreshTimer() {
    if (activeTabRefreshTimer) {
        clearInterval(activeTabRefreshTimer);
        activeTabRefreshTimer = null;
    }
}

/**
 * Immediately refreshes data for the specified tab
 * @param {string} tabName - Name of the tab to refresh
 */
function refreshTabData(tabName) {
    switch (tabName) {
        case 'markets':
            loadMarketWatchData();
            loadAccountData();
            break;
        case 'brainlocks':
            loadBrainsData();
            loadAccountData();
            break;
        case 'positions':
            loadPositionsData();
            loadAccountData();
            break;
        case 'orders':
            loadOrdersData();
            loadAccountData();
            break;
        case 'snapshots':
            loadSnapshotsData();
            loadAccountData();
            break;
    }
    logWithTimestamp('info', `Immediate refresh triggered for ${tabName} tab`);
}

/**
 * Sets up refresh timer for the currently active tab
 * @param {string} tabName - Name of the active tab
 */
function setupActiveTabRefresh(tabName) {
    // Clear existing timer
    clearActiveTabRefreshTimer();

    let refreshFunction;

    // Determine which data to refresh based on active tab
    switch (tabName) {
        case 'markets':
            refreshFunction = () => {
                loadMarketWatchData();
                loadAccountData(); // Account data is always relevant
            };
            break;
        case 'brainlocks':
            refreshFunction = () => {
                loadBrainsData();
                loadAccountData(); // Account data is always relevant
            };
            break;
        case 'positions':
            refreshFunction = () => {
                loadPositionsData();
                loadAccountData(); // Account data is always relevant
            };
            break;
        case 'orders':
            refreshFunction = () => {
                loadOrdersData();
                loadAccountData(); // Account data is always relevant
            };
            break;
        case 'snapshots':
            refreshFunction = () => {
                loadSnapshotsData();
                loadAccountData(); // Account data is always relevant
            };
            break;
        default:
            return; // No refresh for unknown tabs
    }

    // Set up the refresh timer using the configured interval
    const refreshInterval = CONFIG.REFRESH_INTERVALS.GLOBAL; // Use the global interval (30 seconds)
    if (refreshInterval > 0) {
        activeTabRefreshTimer = setInterval(refreshFunction, refreshInterval);
        logWithTimestamp('info', `Set up active tab refresh for ${tabName}: ${refreshInterval}ms`);
    }
}

/**
 * Switches between different tabs in the application
 * @param {string} tabName - Name of the tab to switch to
 */
function switchTab(tabName) {
    previousTab = currentTab;
    currentTab = tabName;

    // Update tab buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    const activeBtn = document.querySelector(`[onclick="switchTab('${tabName}')"]`);
    if (activeBtn) {
        activeBtn.classList.add('active');
    } else {
        console.error('Could not find tab button for:', tabName);
    }

    // Update tab content - hide all first, then show the selected one
    const allTabs = document.querySelectorAll('.tab-content');
    allTabs.forEach(tab => {
        tab.style.display = 'none';
        tab.style.visibility = 'hidden';
    });

    const targetTab = document.getElementById(tabName + '-tab');
    if (targetTab) {
        targetTab.style.display = 'block';
        targetTab.style.visibility = 'visible';
    } else {
        console.error('Could not find tab content for:', tabName + '-tab');
    }

    // Immediately refresh data for the newly selected tab
    refreshTabData(tabName);

    // Set up refresh timer for the newly active tab
    setupActiveTabRefresh(tabName);

    // If switching to snapshots tab and data is already loaded, render it
    if (tabName === 'snapshots' && snapshotsData && snapshotsData.length > 0) {
        renderSnapshots();
    }

    // Close chart modal when switching tabs
    if (tabName !== 'markets') {
        closeChartModal();
    }
}

// FILTERING AND SORTING

/**
 * Applies market filters and re-renders the markets grid
 */
function filterMarkets() {
    currentPage = 1; // Reset to first page when filtering
    renderMarkets();
}

/**
 * Applies market sorting and re-renders the markets grid
 */
function sortMarkets() {
    currentPage = 1; // Reset to first page when sorting
    renderMarkets();
}

/**
 * Toggles sort order between ascending and descending
 */
function toggleSortOrder() {
    const btn = document.getElementById('sortOrderBtn');
    sortOrder = sortOrder === 'desc' ? 'asc' : 'desc';

    btn.innerHTML = sortOrder === 'desc'
        ? '<i class="fas fa-sort-amount-down"></i> Descending'
        : '<i class="fas fa-sort-amount-up"></i> Ascending';

    btn.classList.toggle('active', sortOrder === 'desc');
    currentPage = 1; // Reset to first page when changing sort order
    renderMarkets();
}

/**
 * Filters markets by brain instance and switches to markets tab
 * @param {string} brainInstanceName - Name of the brain instance to filter by
 */
function drillIntoBrainLock(brainInstanceName) {
    // Filter markets by brain instance name
    document.getElementById('brainLockFilter').value = brainInstanceName;
    switchTab('markets');
    filterMarkets();
}

// POSITIONS MANAGEMENT

/**
 * Renders the positions table with filtering and sorting
 */
function renderPositions() {
    const container = document.getElementById('positionsContainer');
    const searchTerm = document.getElementById('positionsSearchInput').value.toLowerCase();
    const sortBy = document.getElementById('positionsSortSelect').value;

    let filteredPositions = positionsData.filter(item => {
        return !searchTerm || item.ticker.toLowerCase().includes(searchTerm);
    });

    // Sort data
    filteredPositions.sort((a, b) => {
        let aVal, bVal;

        switch (sortBy) {
            case 'position':
                aVal = a.position || 0;
                bVal = b.position || 0;
                break;
            case 'totalTraded':
                aVal = a.totalTraded || 0;
                bVal = b.totalTraded || 0;
                break;
            case 'realizedPnl':
                aVal = a.realizedPnl || 0;
                bVal = b.realizedPnl || 0;
                break;
            case 'lastUpdatedUTC':
                aVal = new Date(a.lastUpdatedUTC || 0);
                bVal = new Date(b.lastUpdatedUTC || 0);
                break;
            default: // ticker
                aVal = a.ticker;
                bVal = b.ticker;
        }

        if (positionsSortOrder === 'asc') {
            return aVal > bVal ? 1 : -1;
        } else {
            return aVal < bVal ? 1 : -1;
        }
    });

    if (filteredPositions.length === 0) {
        container.innerHTML = '<div class="empty-state"><i class="fas fa-chart-line"></i><div>No positions match your filters</div></div>';
        return;
    }

    container.innerHTML = `
        <div style="overflow-x: auto;">
            <table style="width: 100%; border-collapse: collapse; background: rgba(16, 16, 13, 0.9); border-radius: 10px; overflow: hidden;">
                <thead>
                    <tr style="background: rgba(28, 51, 39, 0.8);">
                        <th style="padding: 12px; text-align: left; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Ticker</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Position</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Total Traded</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Market Exposure</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Realized P&L</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Resting Orders</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Fees Paid</th>
                        <th style="padding: 12px; text-align: left; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Last Updated</th>
                    </tr>
                </thead>
                <tbody>
                    ${filteredPositions.map(item => createPositionRow(item)).join('')}
                </tbody>
            </table>
        </div>
    `;
}

/**
 * Creates HTML for a single position table row
 * @param {Object} item - Position data object
 * @returns {string} - HTML string for the table row
 */
function createPositionRow(item) {
    const pnlClass = (item.realizedPnl || 0) >= 0 ? 'positive' : 'negative';
    const lastUpdated = item.lastUpdatedUTC ? formatDateTime(item.lastUpdatedUTC) : 'Never';

    // Convert cents to dollars for monetary values
    const totalTradedDollars = ((item.totalTraded || 0) / 100).toFixed(2);
    const marketExposureDollars = ((item.marketExposure || 0) / 100).toFixed(2);
    const realizedPnlDollars = ((item.realizedPnl || 0) / 100).toFixed(2);
    const feesPaidDollars = ((item.feesPaid || 0) / 100).toFixed(2);

    return `
        <tr style="border-bottom: 1px solid rgba(28, 51, 39, 0.3);" onclick="showPositionsContextMenu(event)" oncontextmenu="showPositionsContextMenu(event)">
            <td style="padding: 12px; color: rgb(225, 221, 206); font-weight: 600;">${item.ticker}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${item.position || 0}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${totalTradedDollars}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${marketExposureDollars}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206); ${pnlClass === 'positive' ? 'color: #28a745;' : 'color: #dc3545;'}">${realizedPnlDollars}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${item.restingOrdersCount || 0}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${feesPaidDollars}</td>
            <td style="padding: 12px; color: rgb(225, 221, 206); font-size: 12px;">${lastUpdated}</td>
        </tr>
    `;
}

/**
 * Applies position filters and re-renders the positions table
 */
function filterPositions() {
    renderPositions();
}

/**
 * Applies position sorting and re-renders the positions table
 */
function sortPositions() {
    renderPositions();
}

/**
 * Toggles position sort order between ascending and descending
 */
function togglePositionsSortOrder() {
    const btn = document.getElementById('positionsSortOrderBtn');
    positionsSortOrder = positionsSortOrder === 'desc' ? 'asc' : 'desc';

    btn.innerHTML = positionsSortOrder === 'desc'
        ? '<i class="fas fa-sort-amount-down"></i> Descending'
        : '<i class="fas fa-sort-amount-up"></i> Ascending';

    btn.classList.toggle('active', positionsSortOrder === 'desc');
    renderPositions();
}

// ORDERS MANAGEMENT

/**
 * Renders the orders table with filtering and sorting
 */
function renderOrders() {
    const container = document.getElementById('ordersContainer');
    const searchTerm = document.getElementById('ordersSearchInput').value.toLowerCase();
    const statusFilter = document.getElementById('ordersStatusFilter').value;
    const sideFilter = document.getElementById('ordersSideFilter').value;
    const sortBy = document.getElementById('ordersSortSelect').value;
    const showComplete = document.getElementById('showCompleteOrders').checked;

    let filteredOrders = ordersData.filter(item => {
        // Show Complete checkbox logic - when unchecked, hide completed orders
        if (!showComplete) {
            const statusLower = item.status?.toLowerCase();
            if (statusLower === 'filled' || statusLower === 'cancelled' || statusLower === 'expired') {
                return false;
            }
        }

        const matchesSearch = !searchTerm ||
            item.marketTicker.toLowerCase().includes(searchTerm);

        const matchesStatus = !statusFilter || item.status?.toLowerCase() === statusFilter;
        const matchesSide = !sideFilter || item.side?.toLowerCase() === sideFilter;

        return matchesSearch && matchesStatus && matchesSide;
    });

    // Sort data
    filteredOrders.sort((a, b) => {
        let aVal, bVal;

        switch (sortBy) {
            case 'orderId':
                aVal = a.orderId || 0;
                bVal = b.orderId || 0;
                break;
            case 'side':
                aVal = a.side || '';
                bVal = b.side || '';
                break;
            case 'quantity':
                aVal = a.quantity || 0;
                bVal = b.quantity || 0;
                break;
            case 'price':
                aVal = a.price || 0;
                bVal = b.price || 0;
                break;
            case 'status':
                aVal = a.status || '';
                bVal = b.status || '';
                break;
            case 'createdAt':
                aVal = new Date(a.createdAt || 0);
                bVal = new Date(b.createdAt || 0);
                break;
            default: // marketTicker
                aVal = a.marketTicker;
                bVal = b.marketTicker;
        }

        if (ordersSortOrder === 'asc') {
            return aVal > bVal ? 1 : -1;
        } else {
            return aVal < bVal ? 1 : -1;
        }
    });

    if (filteredOrders.length === 0) {
        container.innerHTML = '<div class="empty-state"><i class="fas fa-list"></i><div>No orders match your filters</div></div>';
        return;
    }

    container.innerHTML = `
        <div style="overflow-x: auto;">
            <table style="width: 100%; border-collapse: collapse; background: rgba(16, 16, 13, 0.9); border-radius: 10px; overflow: hidden;">
                <thead>
                    <tr style="background: rgba(28, 51, 39, 0.8);">
                        <th style="padding: 12px; text-align: left; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Market Ticker</th>
                        <th style="padding: 12px; text-align: center; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Side</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Quantity</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Filled</th>
                        <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Price</th>
                        <th style="padding: 12px; text-align: center; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Status</th>
                        <th style="padding: 12px; text-align: left; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Created At</th>
                    </tr>
                </thead>
                <tbody>
                    ${filteredOrders.map(item => createOrderRow(item)).join('')}
                </tbody>
            </table>
        </div>
    `;
}

/**
 * Creates HTML for a single order table row
 * @param {Object} item - Order data object
 * @returns {string} - HTML string for the table row
 */
function createOrderRow(item) {
    const sideClass = item.side?.toLowerCase() === 'yes' ? 'positive' : 'negative';
    const statusClass = getStatusClass(item.status);
    const createdAt = item.createdAt ? formatDateTime(item.createdAt) : 'Never';
    const priceDollars = ((item.price || 0) / 100).toFixed(2);

    return `
        <tr style="border-bottom: 1px solid rgba(28, 51, 39, 0.3);" onclick="showOrdersContextMenu(event)" oncontextmenu="showOrdersContextMenu(event)">
            <td style="padding: 12px; color: rgb(225, 221, 206); font-weight: 600;">${item.marketTicker}</td>
            <td style="padding: 12px; text-align: center; color: rgb(225, 221, 206); ${sideClass === 'positive' ? 'color: #28a745;' : 'color: #dc3545;'} font-weight: 600;">${item.side?.toUpperCase() || 'N/A'}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${item.quantity || 0}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${item.quantityFilled || 0}</td>
            <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${priceDollars}</td>
            <td style="padding: 12px; text-align: center; color: rgb(225, 221, 206);">
                <span style="background: ${getStatusColor(item.status)}; color: white; padding: 4px 8px; border-radius: 12px; font-size: 12px; font-weight: 600;">
                    ${item.status?.toUpperCase() || 'UNKNOWN'}
                </span>
            </td>
            <td style="padding: 12px; color: rgb(225, 221, 206); font-size: 12px;">${createdAt}</td>
        </tr>
    `;
}

/**
 * Applies order filters and re-renders the orders table
 */
function filterOrders() {
    renderOrders();
}

/**
 * Applies order sorting and re-renders the orders table
 */
function sortOrders() {
    renderOrders();
}

/**
 * Toggles order sort order between ascending and descending
 */
function toggleOrdersSortOrder() {
    const btn = document.getElementById('ordersSortOrderBtn');
    ordersSortOrder = ordersSortOrder === 'desc' ? 'asc' : 'desc';

    btn.innerHTML = ordersSortOrder === 'desc'
        ? '<i class="fas fa-sort-amount-down"></i> Descending'
        : '<i class="fas fa-sort-amount-up"></i> Ascending';

    btn.classList.toggle('active', ordersSortOrder === 'desc');
    renderOrders();
}

// SNAPSHOTS MANAGEMENT

/**
 * Renders the snapshots table with filtering and sorting
 */
function renderSnapshots() {
    const container = document.getElementById('snapshotsContainer');
    const searchTerm = document.getElementById('snapshotsSearchInput').value.toLowerCase();
    const sortBy = document.getElementById('snapshotsSortSelect').value;

    // Check if snapshotsData is loaded and is an array
    if (!snapshotsData || !Array.isArray(snapshotsData)) {
        container.innerHTML = '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Snapshots data not loaded or invalid.</div></div>';
        return;
    }

    let filteredSnapshots;
    try {
        filteredSnapshots = snapshotsData.filter(item => {
            return !searchTerm ||
                (item.marketTicker && item.marketTicker.toLowerCase().includes(searchTerm)) ||
                (item.title && item.title.toLowerCase().includes(searchTerm));
        });
    } catch (error) {
        console.error('Error filtering snapshots data:', error);
        container.innerHTML = '<div class="error"><i class="fas fa-exclamation-triangle"></i><div>Error filtering snapshots data: ' + error.message + '</div></div>';
        return;
    }

    // Sort data
    filteredSnapshots.sort((a, b) => {
        let aVal, bVal;

        switch (sortBy) {
            case 'marketTicker':
                aVal = a.marketTicker;
                bVal = b.marketTicker;
                break;
            case 'title':
                aVal = a.title;
                bVal = b.title;
                break;
            case 'recordedHoursPercentage':
                aVal = a.recordedHoursPercentage || 0;
                bVal = b.recordedHoursPercentage || 0;
                break;
            case 'groupCount':
                aVal = a.groupCount;
                bVal = b.groupCount;
                break;
            case 'recordedEnd':
                aVal = a.recordedEnd ? 1 : 0;
                bVal = b.recordedEnd ? 1 : 0;
                break;
            default:
                aVal = a.marketTicker;
                bVal = b.marketTicker;
        }

        if (snapshotsSortOrder === 'asc') {
            return aVal > bVal ? 1 : -1;
        } else {
            return aVal < bVal ? 1 : -1;
        }
    });

    if (filteredSnapshots.length === 0) {
        container.innerHTML = '<div class="empty-state"><i class="fas fa-camera"></i><div>No snapshots match your filters</div></div>';
        return;
    }

    try {
        // Generate rows outside template literal
        let rowsHtml = '';
        for (let i = 0; i < filteredSnapshots.length; i++) {
            const item = filteredSnapshots[i];
            try {
                const rowHtml = createSnapshotRow(item);
                rowsHtml += rowHtml;
            } catch (error) {
                console.error('Error creating row for item', i, ':', error);
                rowsHtml += `<tr><td colspan="8" style="color: red;">Error rendering row ${i}: ${error.message}</td></tr>`;
            }
        }

        const tableHtml = `
            <div style="overflow-x: auto;">
                <table style="width: 100%; border-collapse: collapse; background: rgba(16, 16, 13, 0.9); border-radius: 10px; overflow: hidden;">
                    <thead>
                        <tr style="background: rgba(28, 51, 39, 0.8);">
                            <th style="padding: 12px; text-align: left; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Market Ticker</th>
                            <th style="padding: 12px; text-align: left; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Title</th>
                            <th style="padding: 12px; text-align: left; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Subtitle</th>
                            <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Recorded Hours</th>
                            <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Market Hours</th>
                            <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Recorded %</th>
                            <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Groups</th>
                            <th style="padding: 12px; text-align: center; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Recorded End</th>
                            <th style="padding: 12px; text-align: right; color: rgb(225, 221, 206); font-weight: 600; border-bottom: 2px solid rgb(28, 51, 39);">Avg Liquidity</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rowsHtml}
                    </tbody>
                </table>
            </div>
        `;

        if (!container) {
            console.error('Container element not found!');
            return;
        }

        try {
            container.innerHTML = tableHtml;
        } catch (error) {
            console.error('Error setting container HTML:', error);
        }
    } catch (error) {
        console.error('Error rendering table:', error);
        // Fallback: show simple list
        container.innerHTML = `
            <div style="padding: 20px; background: rgba(16, 16, 13, 0.9); border-radius: 10px;">
                <h3 style="color: rgb(225, 221, 206); margin-bottom: 15px;">Snapshots Data (${filteredSnapshots.length} items)</h3>
                <div style="max-height: 400px; overflow-y: auto;">
                    ${filteredSnapshots.map((item, index) => `
                        <div style="padding: 10px; margin-bottom: 5px; background: rgba(28, 51, 39, 0.3); border-radius: 5px; color: rgb(225, 221, 206);">
                            <strong>${index + 1}. ${item.MarketTicker || 'N/A'}</strong> - ${item.Title || 'N/A'}
                        </div>
                    `).join('')}
                </div>
                <div style="margin-top: 15px; color: #dc3545;">Table rendering failed: ${error.message}</div>
            </div>
        `;
    }
}

/**
 * Creates HTML for a single snapshot table row
 * @param {Object} item - Snapshot data object
 * @returns {string} - HTML string for the table row
 */
function createSnapshotRow(item) {
    try {
        const recordedEndIcon = item.recordedEnd ? '<i class="fas fa-check" style="color: #28a745;"></i>' : '<i class="fas fa-times" style="color: #dc3545;"></i>';

        // Escape the market ticker for safe use in HTML attributes
        const escapedMarketTicker = (item.marketTicker || 'N/A').replace(/'/g, "\\'").replace(/"/g, '\\"');

        const rowHtml = `
            <tr style="border-bottom: 1px solid rgba(28, 51, 39, 0.3);" onclick="showSnapshotContextMenu(event, '${escapedMarketTicker}')" oncontextmenu="showSnapshotContextMenu(event, '${escapedMarketTicker}')">
                <td style="padding: 12px; color: rgb(225, 221, 206); font-weight: 600;">${(item.marketTicker || 'N/A').replace(/</g, '<').replace(/>/g, '>')}</td>
                <td style="padding: 12px; color: rgb(225, 221, 206);">${(item.title || 'N/A').replace(/</g, '<').replace(/>/g, '>')}</td>
                <td style="padding: 12px; color: rgb(225, 221, 206);">${(item.subtitle || item.yes_sub_title || 'N/A').replace(/</g, '<').replace(/>/g, '>')}</td>
                <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${typeof item.recordedHours === 'number' ? item.recordedHours.toFixed(2) : 'N/A'}</td>
                <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${typeof item.marketHours === 'number' ? item.marketHours.toFixed(2) : 'N/A'}</td>
                <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${typeof item.recordedHoursPercentage === 'number' ? item.recordedHoursPercentage.toFixed(2) + '%' : 'N/A'}</td>
                <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${item.groupCount || 'N/A'}</td>
                <td style="padding: 12px; text-align: center; color: rgb(225, 221, 206);">${recordedEndIcon}</td>
                <td style="padding: 12px; text-align: right; color: rgb(225, 221, 206);">${typeof item.averageLiquidity === 'number' ? item.averageLiquidity.toFixed(2) : 'N/A'}</td>
            </tr>
        `;

        return rowHtml;
    } catch (error) {
        console.error('Error in createSnapshotRow:', error);
        return `
            <tr style="border-bottom: 1px solid rgba(28, 51, 39, 0.3);">
                <td colspan="8" style="padding: 12px; color: #dc3545;">Error rendering row: ${error.message}</td>
            </tr>
        `;
    }
}

/**
 * Applies snapshot filters and re-renders the snapshots table
 */
function filterSnapshots() {
    renderSnapshots();
}

/**
 * Applies snapshot sorting and re-renders the snapshots table
 */
function sortSnapshots() {
    renderSnapshots();
}

/**
 * Toggles snapshot sort order between ascending and descending
 */
function toggleSnapshotsSortOrder() {
    const btn = document.getElementById('snapshotsSortOrderBtn');
    snapshotsSortOrder = snapshotsSortOrder === 'desc' ? 'asc' : 'desc';

    btn.innerHTML = snapshotsSortOrder === 'desc'
        ? '<i class="fas fa-sort-amount-down"></i> Descending'
        : '<i class="fas fa-sort-amount-up"></i> Ascending';

    btn.classList.toggle('active', snapshotsSortOrder === 'desc');
    renderSnapshots();
}