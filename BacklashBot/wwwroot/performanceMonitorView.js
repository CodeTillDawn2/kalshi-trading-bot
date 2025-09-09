// PerformanceMonitorView.js
// Handles rendering and UI updates for the performance monitoring dashboard.
class PerformanceMonitorView {
    constructor(performanceViewModel) {
        this.performanceViewModel = performanceViewModel;
        this.elements = {
            performanceBody: document.querySelector('#performanceBody'),
            loadingOverlay: document.querySelector('#loadingOverlay'),
            allChartsContainer: document.querySelector('#allChartsContainer') // New container for all charts
        };
        this.chartInstances = new Map(); // Stores initialized Chart.js instances, keyed by serviceName_brainInstance
        this.serviceNames = [ // Explicit list of services we expect
            "MarketRefreshService",
            "BroadcastService",
            "KalshiAPIService",
            "KalshiWebSocketClient"
        ];

        // Color sets for differentiating BrainInstance lines on charts
        this.brainInstanceColorSets = [
            { // Set 1
                usage: { border: 'rgba(75, 192, 192, 1)', background: 'rgba(75, 192, 192, 0.2)' }, // Teal
                market: { border: 'rgba(54, 162, 235, 1)', background: 'rgba(54, 162, 235, 0.2)' }   // Blue
            },
            { // Set 2
                usage: { border: 'rgba(255, 159, 64, 1)', background: 'rgba(255, 159, 64, 0.2)' }, // Orange
                market: { border: 'rgba(255, 99, 132, 1)', background: 'rgba(255, 99, 132, 0.2)' }   // Red
            },
            { // Set 3
                usage: { border: 'rgba(153, 102, 255, 1)', background: 'rgba(153, 102, 255, 0.2)' },// Purple
                market: { border: 'rgba(255, 20, 147, 1)', background: 'rgba(255, 20, 147, 0.2)' }  // Deep Pink
            },
            { // Set 4
                usage: { border: 'rgba(255, 206, 86, 1)', background: 'rgba(255, 206, 86, 0.2)' }, // Yellow
                market: { border: 'rgba(75, 192, 75, 1)', background: 'rgba(75, 192, 75, 0.2)' }    // Green
            },
            { // Set 5
                usage: { border: 'rgba(128, 0, 128, 1)', background: 'rgba(128, 0, 128, 0.2)' },   // Dark Purple
                market: { border: 'rgba(0, 128, 128, 1)', background: 'rgba(0, 128, 128, 0.2)' }     // Dark Teal
            },
            { // Set 6
                usage: { border: 'rgba(210, 105, 30, 1)', background: 'rgba(210, 105, 30, 0.2)' }, // Chocolate
                market: { border: 'rgba(128, 128, 0, 1)', background: 'rgba(128, 128, 0, 0.2)' }   // Olive
            }
        ];

        this.performanceViewModel.setView(this);
        // Initial render will now also handle chart creation
        this.update();
    }

    // Helper to destroy existing charts before recreating them
    destroyAllCharts() {
        this.chartInstances.forEach(chart => chart.destroy());
        this.chartInstances.clear();
        this.elements.allChartsContainer.innerHTML = ''; // Clear the container
    }

    // Retrieves a color set for a BrainInstance based on its index.
    getColorSetForBrainInstance(index) {
        return this.brainInstanceColorSets[index % this.brainInstanceColorSets.length];
    }

    // Creates and updates a single chart for a given service and brain instance.
    createOrUpdateChart(serviceName, brainInstance, metricsForInstance) {
        const chartId = `${serviceName}-${brainInstance.replace(/[^a-zA-Z0-9]/g, '-')}-chart`; // Create a safe ID
        const chartKey = `${serviceName}_${brainInstance}`;
        let chart = this.chartInstances.get(chartKey);
        let canvas = document.getElementById(chartId);

        if (!canvas) {
            // Create container div, h3, and canvas if they don't exist
            const chartWrapper = document.createElement('div');
            chartWrapper.id = `${chartId}-wrapper`; // Unique wrapper ID
            chartWrapper.classList.add('chart-item'); // Add a common class for styling

            // Special handling for MarketRefreshService to be full-width
            if (serviceName === "MarketRefreshService") {
                chartWrapper.classList.add('full-width-chart');
            }

            const title = document.createElement('h3');
            title.textContent = `${serviceName} ${brainInstance !== 'Unknown' ? `(${brainInstance})` : ''}`;
            chartWrapper.appendChild(title);

            canvas = document.createElement('canvas');
            canvas.id = chartId;
            chartWrapper.appendChild(canvas);
            this.elements.allChartsContainer.appendChild(chartWrapper);

            // Initialize Chart.js instance
            const chartConfig = {
                type: 'line',
                data: {
                    datasets: [] // Datasets are dynamically populated
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        x: {
                            type: 'time',
                            time: {
                                unit: 'hour',
                                tooltipFormat: 'MMM d, h:mm a',
                                displayFormats: { hour: 'hA' }
                            },
                            title: { display: true, text: 'Time' },
                            ticks: { color: '#e0e0e0' }
                        },
                        yUsage: { // "Usage (%)" Y-axis
                            type: 'linear',
                            position: 'left',
                            title: { display: true, text: 'Usage (%)' },
                            ticks: { color: '#e0e0e0' },
                            grid: { color: '#444' },
                            beginAtZero: true,
                            suggestedMax: 100 // Ensures y-axis goes up to at least 100%
                        },
                        yMarketCount: { // "Market Count" Y-axis
                            type: 'linear',
                            position: 'right',
                            title: { display: true, text: 'Market Count' },
                            ticks: { color: '#e0e0e0' },
                            grid: { drawOnChartArea: false }, // Avoid double grid lines from right axis
                            beginAtZero: true
                        }
                    },
                    plugins: {
                        legend: { labels: { color: '#e0e0e0' } },
                        tooltip: { mode: 'index', intersect: false }
                    }
                }
            };
            chart = new Chart(canvas, chartConfig);
            this.chartInstances.set(chartKey, chart);
        }

        // Update datasets
        const colorSet = this.getColorSetForBrainInstance(Array.from(this.chartInstances.keys()).indexOf(chartKey)); // Use an index from all charts for color consistency
        chart.data.datasets = [
            // Dataset for Usage Percentage
            {
                label: `Usage (%)`, // No need for brain instance in label here
                data: metricsForInstance.map(m => ({ x: moment.utc(m.Timestamp).valueOf(), y: m.UsagePercentage })),
                borderColor: colorSet.usage.border,
                backgroundColor: colorSet.usage.background,
                yAxisID: 'yUsage',
                tension: 0.1
            },
            // Dataset for Market Count with half opacity
            {
                label: `Market Count`, // No need for brain instance in label here
                data: metricsForInstance.map(m => ({ x: moment.utc(m.Timestamp).valueOf(), y: m.MarketCount })),
                borderColor: colorSet.market.border,
                backgroundColor: colorSet.market.background.replace(/, 0\.2\)/, ', 0.1)'), // Half opacity background
                borderDash: [5, 5], // Dotted line for Market Count
                yAxisID: 'yMarketCount',
                tension: 0.1
            }
        ];
        chart.update('none'); // Update chart without animations to prevent "bounce"
    }

    // Updates the performance charts with the latest data.
    updatePerformanceCharts() {
        const allMetrics = this.performanceViewModel.performanceMetrics || [];

        // Identify all unique service-brainInstance combinations
        const uniqueServiceBrainInstances = new Set();
        allMetrics.forEach(m => {
            uniqueServiceBrainInstances.add(`${m.ServiceName}_${m.BrainInstance || "Unknown"}`);
        });

        // Destroy charts that no longer have data
        this.chartInstances.forEach((chart, key) => {
            if (!uniqueServiceBrainInstances.has(key)) {
                chart.destroy();
                this.chartInstances.delete(key);
                const wrapperId = `${key.split('_')[0]}-${key.split('_')[1].replace(/[^a-zA-Z0-9]/g, '-')}-chart-wrapper`;
                const wrapper = document.getElementById(wrapperId);
                if (wrapper) wrapper.remove();
            }
        });

        // Re-sort charts for consistent display (MarketRefreshService first, then others alphabetically)
        const sortedServiceBrainInstances = Array.from(uniqueServiceBrainInstances).sort((a, b) => {
            const [serviceA] = a.split('_');
            const [serviceB] = b.split('_');

            if (serviceA === "MarketRefreshService" && serviceB !== "MarketRefreshService") return -1;
            if (serviceA !== "MarketRefreshService" && serviceB === "MarketRefreshService") return 1;
            return a.localeCompare(b);
        });

        // Create/Update charts for existing and new combinations
        sortedServiceBrainInstances.forEach(key => {
            const [serviceName, brainInstance] = key.split('_');
            const metricsForInstance = allMetrics
                .filter(m => m.ServiceName === serviceName && (m.BrainInstance || "Unknown") === brainInstance)
                .sort((a, b) => moment.utc(a.Timestamp).valueOf() - moment.utc(b.Timestamp).valueOf());

            if (metricsForInstance.length > 0) {
                this.createOrUpdateChart(serviceName, brainInstance, metricsForInstance);
            }
        });
    }

    // Updates the performance summary table with the latest data.
    updatePerformanceTable() {
        if (!this.elements.performanceBody) return;

        const allMetrics = this.performanceViewModel.performanceMetrics || [];
        const latestMetricsMap = new Map();
        // Get the single latest metric entry for each service *and brain instance*
        allMetrics.forEach(metric => {
            const key = `${metric.ServiceName}_${metric.BrainInstance || "Unknown"}`;
            const existing = latestMetricsMap.get(key);
            if (!existing || moment.utc(metric.Timestamp).isAfter(moment.utc(existing.Timestamp))) {
                latestMetricsMap.set(key, metric);
            }
        });
        const latestMetricsArray = Array.from(latestMetricsMap.values())
            .sort((a, b) => {
                // Sort MarketRefreshService first, then others alphabetically by service and brain instance
                const nameA = a.ServiceName;
                const nameB = b.ServiceName;
                const instanceA = a.BrainInstance || "Unknown";
                const instanceB = b.BrainInstance || "Unknown";

                if (nameA === "MarketRefreshService" && nameB !== "MarketRefreshService") return -1;
                if (nameA !== "MarketRefreshService" && nameB === "MarketRefreshService") return 1;

                const serviceCompare = nameA.localeCompare(nameB);
                if (serviceCompare !== 0) return serviceCompare;
                return instanceA.localeCompare(instanceB);
            });

        let rows = latestMetricsArray.map(metric => `
            <tr>
                <td>${metric.ServiceName} ${metric.BrainInstance && metric.BrainInstance !== 'Unknown' ? `(${metric.BrainInstance})` : ''}</td>
                <td>${metric.LastExecutionTime != null ? (metric.LastExecutionTime / 1000).toFixed(2) : '--'}</td>
                <td>${metric.MarketCount != null ? metric.MarketCount : 0}</td>
                <td>${metric.UsagePercentage != null ? metric.UsagePercentage.toFixed(2) : '--'}</td>
                <td style="color: ${metric.IsUsageAcceptable ? 'var(--profit-color)' : 'var(--loss-color)'}">
                    ${metric.IsUsageAcceptable ? 'Yes' : 'No'}
                </td>
                <td>${metric.Timestamp ? moment.utc(metric.Timestamp).local().fromNow() : '--'}</td>
            </tr>
        `);

        // Helper to get unique latest items for supplemental tables by a given name/type field
        const getUniqueLatestByName = (items, nameField) => {
            const uniqueMap = new Map();
            // Iterate in reverse to prioritize items presumed to be "later" if there are duplicates by name/type
            for (let i = items.length - 1; i >= 0; i--) {
                const item = items[i];
                if (!uniqueMap.has(item[nameField])) {
                    uniqueMap.set(item[nameField], item);
                }
            }
            // Sort for consistent display order
            return Array.from(uniqueMap.values()).sort((a, b) => String(a[nameField]).localeCompare(String(b[nameField])));
        };

        const { semaphoreStatus = [], timerStatus = [], webSocketEventCounts = [], broadcastCounts = [], queueMetrics = {} } = this.performanceViewModel;

        if (semaphoreStatus.length > 0) {
            rows.push(`<tr><td colspan="6"><strong>Semaphore Counts (Latest Snapshot)</strong></td></tr>`);
            const uniqueSemaphores = getUniqueLatestByName(semaphoreStatus, 'name');
            rows = rows.concat(uniqueSemaphores.map(s => `<tr><td>${s.name}</td><td colspan="4">${s.count}</td><td>--</td></tr>`));
        }
        if (timerStatus.length > 0) {
            rows.push(`<tr><td colspan="6"><strong>Timer Status (Latest Snapshot)</strong></td></tr>`);
            const uniqueTimers = getUniqueLatestByName(timerStatus, 'name');
            rows = rows.concat(uniqueTimers.map(t => `<tr><td>${t.name}</td><td colspan="4">${t.isActive ? 'Active' : 'Inactive'}</td><td>--</td></tr>`));
        }
        if (webSocketEventCounts.length > 0) {
            rows.push(`<tr><td colspan="6"><strong>WebSocket Event Counts (Latest Snapshot)</strong></td></tr>`);
            const uniqueWsEvents = getUniqueLatestByName(webSocketEventCounts, 'eventType');
            rows = rows.concat(uniqueWsEvents.map(w => `<tr><td>${w.eventType}</td><td colspan="4">${w.count}</td><td>--</td></tr>`));
        }
        if (broadcastCounts.length > 0) {
            rows.push(`<tr><td colspan="6"><strong>Broadcast Event Counts (Latest Snapshot)</strong></td></tr>`);
            const uniqueBroadcastEvents = getUniqueLatestByName(broadcastCounts, 'eventType');
            rows = rows.concat(uniqueBroadcastEvents.map(b => `<tr><td>${b.eventType}</td><td colspan="4">${b.count}</td><td>--</td></tr>`));
        }
        if (Object.keys(queueMetrics).length > 0) {
            rows.push(`<tr><td colspan="6"><strong>Queue Metrics (Latest Snapshot)</strong></td></tr>`);
            rows.push(`<tr><td>Queued Subscription Updates</td><td colspan="4">${queueMetrics.queuedSubscriptionUpdates || 0}</td><td>--</td></tr>`);
            rows.push(`<tr><td>Order Book Messages</td><td colspan="4">${queueMetrics.orderBookMessages || 0}</td><td>--</td></tr>`);
            rows.push(`<tr><td>Pending Confirms</td><td colspan="4">${queueMetrics.pendingConfirms || 0}</td><td>--</td></tr>`);
        }
        this.elements.performanceBody.innerHTML = rows.length > 0 ? rows.join('') : '<tr><td colspan="6">No data available</td></tr>';
    }

    // Main update function for the view.
    update() {
        if (this.elements.loadingOverlay) {
            this.elements.loadingOverlay.style.display = this.performanceViewModel.isLoading ? 'flex' : 'none';
        }

        if (!this.performanceViewModel.isLoading) {
            this.updatePerformanceCharts();
            this.updatePerformanceTable();
        } else {
            // Ensure "Loading..." message is shown in the table body while loading.
            if (this.elements.performanceBody &&
                (this.elements.performanceBody.innerHTML.includes("No data available") ||
                    this.elements.performanceBody.innerHTML.trim() === "")) {
                this.elements.performanceBody.innerHTML = '<tr><td colspan="6">Loading...</td></tr>';
            }
        }
    }
}