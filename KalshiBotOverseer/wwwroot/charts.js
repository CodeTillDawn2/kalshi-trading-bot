/**
 * CHART RENDERING AND DATA VISUALIZATION MODULE
 *
 * This file handles all chart rendering, data visualization, and technical analysis
 * display for the Kalshi Trading Bot Dashboard. It provides interactive charts
 * for market data analysis, real-time price visualization, and performance monitoring.
 *
 * RATIONALE FOR SEPARATION:
 * - Isolates chart-specific logic from general UI rendering components
 * - Enables easy integration and swapping of different charting libraries (Chart.js)
 * - Provides centralized chart configuration, theming, and styling
 * - Makes chart updates and technical indicators modular and reusable
 * - Allows for performance optimizations specific to charting operations
 * - Separates data preprocessing from visualization logic
 *
 * ARCHITECTURAL ROLE:
 * - Primary interface for all chart rendering in the dashboard
 * - Handles both main market charts and mini performance charts
 * - Manages chart lifecycle (creation, updates, destruction)
 * - Provides consistent theming and user interaction patterns
 * - Integrates with real-time data updates from SignalR
 *
 * CONTENTS:
 * - Main price chart rendering with time series data and technical indicators
 * - Secondary indicator charts (RSI, MACD, Bollinger Bands, Volume)
 * - Chart modal management and user controls
 * - Technical indicator calculations and overlay display
 * - Mini performance charts for brain instance metrics
 * - Chart data formatting, preprocessing, and validation
 * - Real-time chart updates and animation handling
 */

/**
 * Renders the main market chart with price data and technical indicators
 * Handles chart initialization, data loading, and user interactions
 */
function renderChart() {
    const titleElement = document.getElementById('chartTitle');
    const canvas = document.getElementById('marketChart');
    const secondaryCanvas = document.getElementById('secondaryChart');
    const placeholder = document.getElementById('chartPlaceholder');

    if (!currentChartMarket) {
        titleElement.textContent = 'Market Chart';
        placeholder.style.display = 'block';
        canvas.style.display = 'none';
        secondaryCanvas.style.display = 'none';
        return;
    }

    titleElement.textContent = `Chart for ${currentChartMarket}`;

    // Show placeholder while "loading"
    placeholder.style.display = 'block';
    canvas.style.display = 'none';
    secondaryCanvas.style.display = 'none';

    // Simulate loading delay for real-time data
    setTimeout(() => {
        placeholder.style.display = 'none';
        canvas.style.display = 'block';

        const timeframe = document.getElementById('chartTimeframe').value;
        const indicator = document.getElementById('secondaryIndicator').value;

        // Determine data points based on timeframe
        let dataPoints = 60; // default for 1h
        let timeUnit = 'minutes';
        let timeFormat = 'HH:mm';

        switch (timeframe) {
            case '15m': dataPoints = 15; break;
            case '3h': dataPoints = 180; break;
            case '1d': dataPoints = 1440; timeUnit = 'hours'; timeFormat = 'MM/DD HH:mm'; break;
            case '3d': dataPoints = 4320; timeUnit = 'hours'; timeFormat = 'MM/DD HH:mm'; break;
            case '1w': dataPoints = 10080; timeUnit = 'hours'; timeFormat = 'MM/DD'; break;
        }

        // Create sample chart data (placeholders)
        const ctx = canvas.getContext('2d');
        const now = new Date();

        // Sample price data points
        const labels = [];
        const prices = [];
        for (let i = dataPoints; i >= 0; i--) {
            const date = new Date(now.getTime() - (i * (timeUnit === 'minutes' ? 60000 : 3600000)));
            labels.push(date);
            prices.push(50 + Math.sin(i / (dataPoints / 6)) * 10 + Math.random() * 5); // Sample oscillating price
        }

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Price',
                    data: prices,
                    borderColor: 'rgb(28, 51, 39)',
                    backgroundColor: 'rgba(28, 51, 39, 0.1)',
                    tension: 0.1,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: {
                        type: 'time',
                        time: {
                            unit: timeUnit === 'minutes' ? 'minute' : 'hour',
                            displayFormats: {
                                minute: 'HH:mm',
                                hour: 'MMM dd HH:mm'
                            }
                        },
                        title: {
                            display: true,
                            text: 'Time'
                        },
                        ticks: {
                            color: '#e0e0e0'
                        },
                        grid: {
                            color: '#444'
                        }
                    },
                    y: {
                        title: {
                            display: true,
                            text: 'Price ($)'
                        },
                        ticks: {
                            color: '#e0e0e0'
                        },
                        grid: {
                            color: '#444'
                        },
                        beginAtZero: false
                    }
                },
                plugins: {
                    legend: {
                        labels: {
                            color: '#e0e0e0'
                        }
                    }
                },
                animation: {
                    duration: 1000
                }
            }
        });

        // Handle secondary chart
        if (indicator !== 'none') {
            secondaryCanvas.style.display = 'block';
            renderSecondaryChart(indicator, labels);
        } else {
            secondaryCanvas.style.display = 'none';
        }

        // Populate sample data for all the metrics
        updateChartMetrics();

        // Add event listeners for controls
        document.getElementById('chartTimeframe').addEventListener('change', renderChart);
        document.getElementById('secondaryIndicator').addEventListener('change', renderChart);
        document.getElementById('refreshChartBtn').addEventListener('click', renderChart);
    }, 1000); // 1 second delay to simulate loading
}

function renderSecondaryChart(indicator, labels) {
    const secondaryCanvas = document.getElementById('secondaryChart');
    const ctx = secondaryCanvas.getContext('2d');

    let data = [];
    let label = '';
    let color = 'rgb(54, 162, 235)';

    switch (indicator) {
        case 'volume':
            data = labels.map(() => Math.random() * 100 + 50);
            label = 'Volume';
            color = 'rgb(255, 159, 64)';
            break;
        case 'rsi':
            data = labels.map((_, i) => 50 + Math.sin(i / 10) * 20 + Math.random() * 10);
            label = 'RSI';
            color = 'rgb(75, 192, 192)';
            break;
        case 'macd':
            data = labels.map((_, i) => Math.sin(i / 15) * 5 + Math.random() * 2);
            label = 'MACD';
            color = 'rgb(153, 102, 255)';
            break;
        case 'bollinger':
            // For Bollinger bands, we'll show the middle band
            data = labels.map((_, i) => 50 + Math.sin(i / 20) * 3);
            label = 'Bollinger Middle';
            color = 'rgb(255, 99, 132)';
            break;
    }

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: label,
                data: data,
                borderColor: color,
                backgroundColor: color.replace('rgb', 'rgba').replace(')', ', 0.1)'),
                tension: 0.1,
                fill: false,
                pointRadius: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    type: 'time',
                    display: false,
                    time: {
                        unit: timeUnit === 'minutes' ? 'minute' : 'hour'
                    }
                },
                y: {
                    ticks: {
                        color: '#e0e0e0'
                    },
                    grid: {
                        color: '#444'
                    },
                    beginAtZero: false
                }
            },
            plugins: {
                legend: {
                    labels: {
                        color: '#e0e0e0'
                    }
                }
            },
            animation: {
                duration: 1000
            }
        }
    });
}

function updateChartMetrics() {
    // Price Information
    document.getElementById('allTimeHighYes').textContent = '52.5';
    document.getElementById('allTimeHighNo').textContent = '47.5';
    document.getElementById('recentHighYes').textContent = '51.2';
    document.getElementById('recentHighNo').textContent = '48.8';
    document.getElementById('currentPriceYes').textContent = '50.3';
    document.getElementById('currentPriceNo').textContent = '49.7';
    document.getElementById('recentLowYes').textContent = '49.1';
    document.getElementById('recentLowNo').textContent = '48.2';
    document.getElementById('allTimeLowYes').textContent = '45.0';
    document.getElementById('allTimeLowNo').textContent = '42.5';

    // Technical Indicators
    document.getElementById('rsiValue').textContent = '65.4';
    document.getElementById('macdValue').textContent = '0.8';
    document.getElementById('emaValue').textContent = '50.1';
    document.getElementById('bollingerValue').textContent = '49.5-50.8';
    document.getElementById('atrValue').textContent = '1.2';
    document.getElementById('vwapValue').textContent = '50.0';
    document.getElementById('stochasticValue').textContent = '78.5';
    document.getElementById('obvValue').textContent = '1250';

    // Market Information
    document.getElementById('marketCategory').textContent = 'Politics';
    document.getElementById('timeLeft').textContent = '2d 14h';
    document.getElementById('marketAge').textContent = '15d';
    document.getElementById('spreadValue').textContent = '0.6';

    // Flow & Momentum
    document.getElementById('topVelocityYes').textContent = '45/min';
    document.getElementById('topVelocityNo').textContent = '38/min';
    document.getElementById('tradeVolumeYes').textContent = '1250';
    document.getElementById('tradeVolumeNo').textContent = '980';
    document.getElementById('avgTradeSizeYes').textContent = '25';
    document.getElementById('avgTradeSizeNo').textContent = '22';

    // Position Information
    document.getElementById('positionSize').textContent = '100';
    document.getElementById('buyinPrice').textContent = '49.8';
    document.getElementById('positionROI').textContent = '+1.2%';
    document.getElementById('restingOrders').textContent = '3';
}

function openChartModal() {
    const modal = document.getElementById('chartModal');
    modal.style.display = 'flex';

    // Start auto-refresh every 30 seconds
    if (chartAutoRefreshInterval) {
        clearInterval(chartAutoRefreshInterval);
    }
    chartAutoRefreshInterval = setInterval(() => {
        requestDataRefresh();
    }, 30000); // 30 seconds
}

function closeChartModal() {
    const modal = document.getElementById('chartModal');
    modal.style.display = 'none';

    // Stop auto-refresh when modal is closed
    if (chartAutoRefreshInterval) {
        clearInterval(chartAutoRefreshInterval);
        chartAutoRefreshInterval = null;
    }
}

/**
 * Renders performance charts for brain instance cards
 * Creates mini charts showing CPU usage, queue depths, and error metrics
 * @param {Object} brainStatusMessage - Brain status data from SignalR
 */
function renderPerformanceChartsForBrains(brainStatusMessage) {
    try {
        const brainInstanceName = brainStatusMessage.BrainInstanceName;
        if (!brainInstanceName) {
            console.warn('[renderPerformanceChartsForBrains] Missing brain instance name');
            return;
        }

        const safeName = brainInstanceName.replace(/[^a-zA-Z0-9]/g, '_');

        // CPU Usage Performance Chart
        if (brainStatusMessage.CpuUsageHistory && Array.isArray(brainStatusMessage.CpuUsageHistory) && brainStatusMessage.CpuUsageHistory.length > 0) {
            renderPerformanceMiniChart(`cpu-chart-${safeName}`, brainStatusMessage.CpuUsageHistory, 'CPU %', '#28a745');
        }

        // Event Queue Depth Chart
        if (brainStatusMessage.EventQueueHistory && Array.isArray(brainStatusMessage.EventQueueHistory) && brainStatusMessage.EventQueueHistory.length > 0) {
            renderPerformanceMiniChart(`event-chart-${safeName}`, brainStatusMessage.EventQueueHistory, 'Events', '#ffc107');
        }

        // Orderbook Queue Depth Chart
        if (brainStatusMessage.OrderbookQueueHistory && Array.isArray(brainStatusMessage.OrderbookQueueHistory) && brainStatusMessage.OrderbookQueueHistory.length > 0) {
            renderPerformanceMiniChart(`orderbook-chart-${safeName}`, brainStatusMessage.OrderbookQueueHistory, 'Orders', '#dc3545');
        }

        // Error Rate Chart
        if (brainStatusMessage.ErrorHistory && Array.isArray(brainStatusMessage.ErrorHistory) && brainStatusMessage.ErrorHistory.length > 0) {
            renderPerformanceMiniChart(`error-chart-${safeName}`, brainStatusMessage.ErrorHistory, 'Errors', '#ff6b6b');
        } else if (brainStatusMessage.NotificationQueueHistory && Array.isArray(brainStatusMessage.NotificationQueueHistory) && brainStatusMessage.NotificationQueueHistory.length > 0) {
            // Fallback to NotificationQueueHistory if ErrorHistory is not available
            renderPerformanceMiniChart(`error-chart-${safeName}`, brainStatusMessage.NotificationQueueHistory, 'Errors', '#ff6b6b');
        }
    } catch (error) {
        console.error('[renderPerformanceChartsForBrains] Error rendering performance charts:', error);
    }
}

/**
 * Renders a mini chart for brain performance metrics
 * Creates small, efficient charts for displaying time-series performance data
 * @param {string} canvasId - ID of the canvas element to render into
 * @param {Array} historyData - Array of metric data points with timestamp and value
 * @param {string} label - Label for the chart data series
 * @param {string} color - Hex color code for the chart line
 */
function renderPerformanceMiniChart(canvasId, historyData, label, color) {
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.warn(`[renderMiniChart] Canvas element not found: ${canvasId}`);
            return;
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) {
            console.warn(`[renderMiniChart] Could not get canvas context for: ${canvasId}`);
            return;
        }

        // Validate history data
        if (!Array.isArray(historyData) || historyData.length === 0) {
            console.warn(`[renderMiniChart] Invalid or empty history data for: ${canvasId}`);
            return;
        }

        // Prepare data - ensure we have valid data points
        const validData = historyData.filter(item =>
            item && typeof item.Timestamp !== 'undefined' && typeof item.Value === 'number'
        );

        if (validData.length === 0) {
            console.warn(`[renderMiniChart] No valid data points found for: ${canvasId}`);
            return;
        }

        const data = validData.slice(-10); // Last 10 points for mini chart
        const labels = data.map(item => {
            try {
                return new Date(item.Timestamp);
            } catch (e) {
                console.warn(`[renderMiniChart] Invalid timestamp for ${canvasId}:`, item.Timestamp);
                return new Date();
            }
        });
        const values = data.map(item => item.Value);

        // Clear any existing chart
        if (canvas.chart) {
            canvas.chart.destroy();
        }

        canvas.chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: values,
                    borderColor: color,
                    backgroundColor: color.replace('rgb', 'rgba').replace(')', ', 0.1)'),
                    borderWidth: 1,
                    pointRadius: 0,
                    fill: true,
                    tension: 0.1
                }]
            },
            options: {
                responsive: false,
                maintainAspectRatio: false,
                scales: {
                    x: {
                        display: false
                    },
                    y: {
                        display: false
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                },
                animation: {
                    duration: 0 // Disable animation for performance
                }
            }
        });

        console.log(`[renderMiniChart] Successfully rendered chart for ${canvasId} with ${data.length} data points`);
    } catch (error) {
        console.error(`[renderMiniChart] Error rendering chart for ${canvasId}:`, error);
    }
}