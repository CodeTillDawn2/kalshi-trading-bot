namespace BacklashBot.Configuration
{
    public class LoggingConfig
    {
        public string SqlDatabaseLogLevel { get; set; } = "Information";
        public string ConsoleLogLevel { get; set; } = "Debug";
        public string Environment { get; set; } = "dev";
        public bool StoreWebSocketEvents { get; set; } = true;
    }
}
