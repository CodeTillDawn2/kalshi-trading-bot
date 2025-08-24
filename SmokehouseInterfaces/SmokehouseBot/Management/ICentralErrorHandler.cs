using SmokehouseDTOs;
using System.Collections.Concurrent;

namespace SmokehouseBot.Management.Interfaces
{
    public interface ICentralErrorHandler
    {
        ConcurrentQueue<ErrorHandlerTaskInfo> Warnings { get; }
        ConcurrentQueue<ErrorHandlerTaskInfo> Errors { get; }
        public bool CatastrophicErrorAlreadyDetected { get; set; }
        DateTime LastSuccessfulSnapshot { get; set; }
        Task<bool> HandleErrors();
        Task<bool> CheckInternetConnection();
        void AddWarning(Exception ex, string identifier, string message = null);
        void AddError(Exception ex, string identifier, string message = null);
        long WarningCount { get; set; }
        long ErrorCount { get; set; }
    }
}