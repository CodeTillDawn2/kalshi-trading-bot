using BacklashDTOs;
using System.Collections.Concurrent;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>ICentralErrorHandler</summary>
    /// <summary>ICentralErrorHandler</summary>
    public interface ICentralErrorHandler
    /// <summary>Gets or sets the Errors.</summary>
    /// <summary>Gets or sets the Warnings.</summary>
    {
        /// <summary>Gets or sets the LastErrorDate.</summary>
        /// <summary>Gets or sets the CatastrophicErrorAlreadyDetected.</summary>
        ConcurrentQueue<ErrorHandlerTaskInfo> Warnings { get; }
        /// <summary>AddWarning</summary>
        /// <summary>Gets or sets the LastErrorDate.</summary>
        ConcurrentQueue<ErrorHandlerTaskInfo> Errors { get; }
        /// <summary>Gets or sets the ErrorCount.</summary>
        /// <summary>CheckInternetConnection</summary>
        public bool CatastrophicErrorAlreadyDetected { get; set; }
        /// <summary>AddError</summary>
        DateTime LastSuccessfulSnapshot { get; set; }
        /// <summary>Gets or sets the ErrorCount.</summary>
        DateTime LastErrorDate { get; set; }
        Task<bool> HandleErrors();
        Task<bool> CheckInternetConnection();
        void AddWarning(Exception ex, string identifier, string? message = null);
        void AddError(Exception ex, string identifier, string? message = null);
        long WarningCount { get; set; }
        long ErrorCount { get; set; }
    }
}
