# Performance Monitoring System - Complete Flow Chart

## Overview
This document provides a comprehensive view of all classes in the KalshiBot system that send performance metrics, showing their enablement status patterns and integration points.

## Legend
- ✅ **Compliant**: Uses proper enablement status parameter
- ⚠️ **Fixed**: Was non-compliant but has been corrected
- 🔄 **Conditional**: Uses type checking to call overloaded method
- 📊 **Direct**: Calls interface method directly
- 🎯 **Configuration**: Uses specific config flag for enablement

---

## Core Trading Strategies Classes

### 1. TradingOverseer.cs
**Location**: `TradingStrategies/Trading/Overseer/TradingOverseer.cs`
**Method**: `PostMetrics()`
**Pattern**: 🔄 Conditional with enablement status
```csharp
if (_performanceMonitor is PerformanceMonitor pm) {
    pm.RecordSimulationMetrics("TradingOverseer", metricsDict, _enablePerformanceMetrics);
} else {
    _performanceMonitor.RecordSimulationMetrics("TradingOverseer", metricsDict);
}
```
**Enablement Source**: `_enablePerformanceMetrics` (class-level flag)
**Status**: ✅ Compliant

### 2. StrategySimulation.cs
**Location**: `TradingStrategies/Trading/Overseer/StrategySimulation.cs`
**Method**: `GetDetailedPerformanceMetrics()`
**Pattern**: 🔄 Conditional with enablement status
```csharp
if (_performanceMonitor is PerformanceMonitor pm) {
    pm.RecordSimulationMetrics(Strategy.Name, metrics, _config.Simulation_EnablePerformanceMetrics);
} else {
    _performanceMonitor?.RecordSimulationMetrics(Strategy.Name, metrics);
}
```
**Enablement Source**: `_config.Simulation_EnablePerformanceMetrics`
**Status**: ✅ Compliant

### 3. SimulationEngine.cs
**Location**: `TradingStrategies/Trading/Overseer/SimulationEngine.cs`
**Methods**: `RunSimulation()`, `PostPatternDetectionMetrics()`
**Pattern**: 🔄 Conditional with enablement status
```csharp
if (_performanceMonitor is PerformanceMonitor pm) {
    pm.RecordSimulationMetrics("SimulationEngine", metrics, _enablePerformanceMetrics);
} else {
    _performanceMonitor.RecordSimulationMetrics("SimulationEngine", metrics);
}
```
**Enablement Source**: `_enablePerformanceMetrics` (class-level flag)
**Status**: ✅ Compliant

### 4. MarketTypeService.cs
**Location**: `TradingStrategies/Trading/Overseer/MarketTypeService.cs`
**Method**: `PostMetrics()`
**Pattern**: 🔄 Conditional with enablement status
```csharp
if (performanceMonitor is PerformanceMonitor pm) {
    pm.RecordSimulationMetrics("MarketTypeService", metrics, _enablePerformanceMetrics);
} else {
    performanceMonitor.RecordSimulationMetrics("MarketTypeService", metrics);
}
```
**Enablement Source**: `_enablePerformanceMetrics` (class-level flag)
**Status**: ✅ Compliant

### 5. EquityCalculator.cs
**Location**: `TradingStrategies/Trading/Overseer/EquityCalculator.cs`
**Method**: `PostMetrics()`
**Pattern**: 🔄 Conditional with enablement status
```csharp
if (performanceMonitor is PerformanceMonitor pm) {
    pm.RecordSimulationMetrics("EquityCalculator", metricsDict, _config.EquityCalculator_EnablePerformanceMetrics);
} else {
    performanceMonitor.RecordSimulationMetrics("EquityCalculator", metricsDict);
}
```
**Enablement Source**: `_config.EquityCalculator_EnablePerformanceMetrics`
**Status**: ✅ Compliant

### 6. StrategySelectionHelper.cs
**Location**: `TradingStrategies/Trading/Helpers/StrategySelectionHelper.cs`
**Method**: `PostMetrics()`
**Pattern**: 🔄 Conditional with enablement status
```csharp
if (performanceMonitor is PerformanceMonitor pm) {
    pm.RecordSimulationMetrics($"StrategySelectionHelper.{strategyType}", metricsDict, EnablePerformanceMetrics);
} else {
    performanceMonitor.RecordSimulationMetrics($"StrategySelectionHelper.{strategyType}", metricsDict);
}
```
**Enablement Source**: `EnablePerformanceMetrics` (static property from config)
**Status**: ✅ Compliant

---

## BacklashBot Classes

### 7. PatternSearch.cs
**Location**: `BacklashPatterns/PatternSearch.cs`
**Method**: Performance metric recording
**Pattern**: 📊 Direct interface call
```csharp
performanceMonitor.RecordSimulationMetrics("PatternSearch", metricsDict, _enablePerformanceMetrics);
```
**Enablement Source**: `_enablePerformanceMetrics` (class-level flag)
**Status**: ✅ Compliant

### 8. CentralPerformanceMonitor.cs
**Location**: `BacklashBot/Management/CentralPerformanceMonitor.cs`
**Methods**: All IPerformanceMonitor methods + ICentralPerformanceMonitor methods
**Pattern**: 🎯 Configuration-based enablement (Interface Implementation)
```csharp
RecordExecutionTime("BroadcastService", (long)totalBroadcastTimeMs, _executionConfig?.CentralPerformanceMonitor_EnableDatabaseMetrics ?? true);
```
**Enablement Source**: `_executionConfig?.CentralPerformanceMonitor_EnableDatabaseMetrics ?? true`
**Status**: ✅ Compliant (now implements IPerformanceMonitor via ICentralPerformanceMonitor inheritance)
**Category**: Interface Implementation (ICentralPerformanceMonitor : IPerformanceMonitor)

### 9. MarketProcessor.cs
**Location**: `TradingSimulator/MarketProcessor.cs`
**Methods**: `ProcessMarket()`, `ProcessBatch()`
**Pattern**: 🎯 Configuration-based enablement
```csharp
_performanceMonitor.RecordExecutionTime("ProcessMarket", (long)processingTime.TotalMilliseconds, _config.EnablePerformanceMetrics);
```
**Enablement Source**: `_config.EnablePerformanceMetrics`
**Status**: ✅ Compliant

### 10. WebSocketConnectionManager.cs
**Location**: `KalshiBotAPI/Websockets/WebSocketConnectionManager.cs`
**Methods**: Multiple metric recording methods
**Pattern**: 🎯 Configuration-based enablement
```csharp
_performanceMonitor.RecordExecutionTime($"WebSocketConnectionManager.{operation}", milliseconds, _enableMetrics);
```
**Enablement Source**: `_enableMetrics` (class-level flag)
**Status**: ✅ Compliant

### 11. KalshiAPIService.cs
**Location**: `KalshiBotAPI/KalshiAPI/KalshiAPIService.cs`
**Method**: Performance timing
**Pattern**: 🎯 Configuration-based enablement
```csharp
_performanceMonitor.RecordExecutionTime(methodName, elapsedMs, _enablePerformanceMetrics);
```
**Enablement Source**: `_enablePerformanceMetrics` (class-level flag)
**Status**: ✅ Compliant

### 12. CandlestickService.cs
**Location**: `BacklashBot/Services/CandlestickService.cs`
**Method**: Performance timing
**Pattern**: 🎯 Configuration-based enablement
```csharp
performanceMonitor.RecordExecutionTime(operationName, elapsedMilliseconds, _executionConfig.EnableCandlestickServicePerformanceMetrics);
```
**Enablement Source**: `_executionConfig.EnableCandlestickServicePerformanceMetrics`
**Status**: ✅ Compliant

### 13. KaslhiBotScopeManagerService.cs
**Location**: `BacklashBot/Services/KaslhiBotScopeManagerService.cs`
**Methods**: `InitializeScope()`, `ScopeLifetime()`
**Pattern**: 🎯 Configuration-based enablement
```csharp
_monitor.RecordExecutionTime("InitializeScope", stopwatch?.ElapsedMilliseconds ?? 0, _enableMetrics);
```
**Enablement Source**: `_enableMetrics` (class-level flag)
**Status**: ✅ Compliant

### 14. OrderbookChangeTracker.cs
**Location**: `BacklashBot/Services/OrderbookChangeTracker.cs`
**Methods**: Multiple metric recording methods
**Pattern**: 🎯 Configuration-based enablement
```csharp
_centralPerformanceMonitor.RecordExecutionTime($"OrderbookChangeTracker_{_marketTicker}_AverageEventProcessingTimeMs", avgEventProcessingTime, _enablePerformanceMetrics);
```
**Enablement Source**: `_enablePerformanceMetrics` (class-level flag)
**Status**: ✅ Compliant

### 15. WebSocketMonitorService.cs
**Location**: `BacklashBot/Services/WebSocketMonitorService.cs`
**Methods**: Multiple metric recording methods
**Pattern**: 🎯 Configuration-based enablement
```csharp
_centralPerformanceMonitor.RecordExecutionTime("WebSocketMonitor.ExchangeStatusCheck", responseTimeMs, _enableMetrics);
```
**Enablement Source**: `_enableMetrics` (class-level flag)
**Status**: ✅ Compliant

### 16. TradingSnapshotService.cs
**Location**: `BacklashBot/Services/TradingSnapshotService.cs`
**Methods**: `SaveSnapshotAsync()`, `LoadManySnapshots()`
**Pattern**: 🎯 Configuration-based enablement
```csharp
_centralPerformanceMonitor.RecordExecutionTime("TradingSnapshotService.SaveSnapshotAsync", stopwatch.ElapsedMilliseconds, _enablePerformanceMetrics);
```
**Enablement Source**: `_enablePerformanceMetrics` (class-level flag)
**Status**: ✅ Compliant

---

## Interface Implementations

### 17. PerformanceMonitor.cs
**Location**: `TradingStrategies/Trading/Overseer/PerformanceMonitor.cs`
**Methods**: All metric recording methods
**Pattern**: Interface implementation with both overloads
```csharp
public void RecordExecutionTime(string methodName, long milliseconds) {
    RecordExecutionTime(methodName, milliseconds, true);
}
public void RecordExecutionTime(string methodName, long milliseconds, bool metricsEnabled) {
    // Implementation
}
```
**Status**: ✅ Compliant (provides both interface methods)

### 18. PerformanceMetricsService.cs
**Location**: `BacklashOverseer/Services/PerformanceMetricsService.cs`
**Methods**: All metric recording methods
**Pattern**: Interface implementation with both overloads
```csharp
public void RecordExecutionTime(string methodName, long milliseconds) {
    // Implementation
}
public void RecordExecutionTime(string methodName, long milliseconds, bool metricsEnabled) {
    // Implementation
}
```
**Status**: ✅ Compliant (provides both interface methods)

---

## Configuration Sources

### TradingConfig.cs
- `Simulation_EnablePerformanceMetrics`
- `EquityCalculator_EnablePerformanceMetrics`
- `StrategySelectionHelper_EnablePerformanceMetrics`

### ExecutionConfig.cs
- `CentralPerformanceMonitor_EnableDatabaseMetrics`
- `EnableCandlestickServicePerformanceMetrics`

### Class-level Flags
- `_enablePerformanceMetrics`
- `_enableMetrics`

---

## Summary Statistics

| Category | Count | Status |
|----------|-------|--------|
| **Total Classes Sending Metrics** | 18 | All ✅ Compliant |
| **Using Conditional Pattern** | 6 | Trading Strategies classes |
| **Using Direct Interface Calls** | 1 | PatternSearch |
| **Using Configuration Flags** | 10 | BacklashBot services |
| **Fixed from Non-compliant** | 1 | CentralPerformanceMonitor |
| **Interface Implementations** | 3 | PerformanceMonitor, PerformanceMetricsService, CentralPerformanceMonitor |

## Key Patterns

1. **Conditional Type Checking**: Used by Trading Strategies classes to maintain backward compatibility
2. **Direct Interface Calls**: Used by classes that can guarantee interface availability
3. **Configuration-Based**: Used by service classes with specific enablement flags
4. **Dual Interface Methods**: Both basic and enablement-aware methods provided for compatibility

## Enablement Flow

```
Class Configuration → Enablement Flag → Interface Method → Implementation Check → Metric Recording
     ↓                        ↓              ↓              ↓              ↓
TradingConfig.cs    _enablePerformanceMetrics  Record*(..., enabled)  if(enabled)  Store Metric
ExecutionConfig.cs  _config.EnableMetrics      IPerformanceMonitor    && config    Database/File
Class Fields        true/false                 .RecordExecutionTime()  → Record     GUI Display
```

All classes now properly respect their enablement configuration and pass the status to the performance monitoring system, ensuring metrics are only collected when both the calling class and the monitoring system are configured to allow it.