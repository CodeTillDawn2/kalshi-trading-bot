// PerformanceMonitorViewModel.js
// Manages the state for the performance monitoring view.
class PerformanceMonitorViewModel {
    constructor() {
        this.performanceMetrics = []; // Stores historical performance data for services
        this.semaphoreStatus = [];    // Stores current semaphore statuses
        this.timerStatus = [];        // Stores current timer statuses
        this.webSocketEventCounts = []; // Stores WebSocket event counts
        this.broadcastCounts = [];    // Stores SignalR broadcast counts
        this.queueMetrics = {};       // Stores various queue lengths
        this.isLoading = true;        // Flag to indicate if data is currently being loaded
        this.performanceView = null;  // Reference to the view instance
    }

    setView(performanceView) {
        this.performanceView = performanceView;
    }

    // Called when the model's data has changed, signaling the view to update.
    onModelChanged() {
        if (this.performanceView) {
            this.performanceView.update();
        } else {
            console.warn("PerformanceMonitorView not set on ViewModel; cannot update view.");
        }
    }

    // Sets the loading state of the model.
    setLoading(loading) {
        this.isLoading = loading;
        this.onModelChanged(); // Trigger view update to reflect loading state change
    }
}