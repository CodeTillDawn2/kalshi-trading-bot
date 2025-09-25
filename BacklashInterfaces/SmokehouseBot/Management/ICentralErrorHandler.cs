using BacklashDTOs;
using System.Collections.Concurrent;

namespace BacklashBot.Management.Interfaces
{
    /// <summary>
    /// Defines the contract for a centralized error handling service that manages system errors,
    /// warnings, and connectivity checks throughout the trading bot application.
    /// </summary>
    public interface ICentralErrorHandler
    {
        /// <summary>
        /// Gets the queue of warning messages and their associated task information.
        /// </summary>
        ConcurrentQueue<ErrorHandlerTaskInfo> Warnings { get; }

        /// <summary>
        /// Gets the queue of error messages and their associated task information.
        /// </summary>
        ConcurrentQueue<ErrorHandlerTaskInfo> Errors { get; }

        /// <summary>
        /// Gets or sets a value indicating whether a catastrophic error has already been detected.
        /// </summary>
        /// <value><c>true</c> if a catastrophic error was detected; otherwise, <c>false</c>.</value>
        bool CatastrophicErrorAlreadyDetected { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last successful snapshot.
        /// </summary>
        DateTime LastSuccessfulSnapshot { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last error occurrence.
        /// </summary>
        DateTime LastErrorDate { get; set; }

        /// <summary>
        /// Gets or sets the total count of warnings recorded.
        /// </summary>
        long WarningCount { get; set; }

        /// <summary>
        /// Gets or sets the total count of errors recorded.
        /// </summary>
        long ErrorCount { get; set; }

        /// <summary>
        /// Handles accumulated errors and performs necessary recovery or logging operations.
        /// </summary>
        /// <returns><c>true</c> if errors were handled successfully; otherwise, <c>false</c>.</returns>
        Task<bool> HandleErrors();

        /// <summary>
        /// Checks the internet connection status to ensure system connectivity.
        /// </summary>
        /// <returns><c>true</c> if internet connection is available; otherwise, <c>false</c>.</returns>
        Task<bool> CheckInternetConnection();

        /// <summary>
        /// Adds a warning to the error handler with the specified exception, identifier, and optional message.
        /// </summary>
        /// <param name="ex">The exception that caused the warning.</param>
        /// <param name="identifier">A unique identifier for the warning source.</param>
        /// <param name="message">An optional additional message describing the warning.</param>
        void AddWarning(Exception ex, string identifier, string? message = null);

        /// <summary>
        /// Adds an error to the error handler with the specified exception, identifier, and optional message.
        /// </summary>
        /// <param name="ex">The exception that caused the error.</param>
        /// <param name="identifier">A unique identifier for the error source.</param>
        /// <param name="message">An optional additional message describing the error.</param>
        void AddError(Exception ex, string identifier, string? message = null);
    }
}
