using Microsoft.Extensions.Logging;


namespace TaxiDispatch.Configuration
{
    public sealed class SimpleFileLogger : ILogger
    {
        private readonly string _path;
        private readonly string _categoryName;
        private static readonly object _lock = new();

        public SimpleFileLogger(string path, string categoryName)
        {
            _path = path;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {

            if (formatter == null) return;

            var message = formatter(state, exception);

            var logLine =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} " +
                $"[{logLevel}] " +
                $"{_categoryName} - " +
                $"{message}";

            if (exception != null)
            {
                logLine += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                File.AppendAllText(_path, logLine + Environment.NewLine);
            }

            //lock (_lock)
            //{
            //    File.AppendAllText(
            //        _path,
            //        $"{DateTime.Now:O} [{logLevel}] {formatter(state, exception)}{Environment.NewLine}"
            //    );
            //}
        }
    }

}

