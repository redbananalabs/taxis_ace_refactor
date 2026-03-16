using Microsoft.Extensions.Logging;

namespace TaxiDispatch.Configuration
{
    public sealed class SimpleFileLoggerProvider : ILoggerProvider
    {
        private readonly string _path;

        public SimpleFileLoggerProvider(string path)
        {
            _path = path;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SimpleFileLogger(_path,categoryName);
        }

        public void Dispose() { }
    }
}

