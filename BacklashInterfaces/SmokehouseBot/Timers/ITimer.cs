using System.Timers;

namespace BacklashInterfaces.SmokehouseBot.Timers
{
    /// <summary>
    /// Interface for timer functionality, allowing dependency injection of timer implementations.
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// Occurs when the interval elapses.
        /// </summary>
        event ElapsedEventHandler Elapsed;

        /// <summary>
        /// Gets or sets the interval at which to raise the Elapsed event.
        /// </summary>
        double Interval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Timer should raise the Elapsed event each time the specified interval elapses or only after the first time.
        /// </summary>
        bool AutoReset { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Timer should raise the Elapsed event.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the timer.
        /// </summary>
        void Stop();

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 32-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the Timer was constructed, in milliseconds. Specify Timeout.Infinite to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the Timer was constructed, in milliseconds. Specify Timeout.Infinite to disable periodic signaling.</param>
        void Change(int dueTime, int period);
    }
}