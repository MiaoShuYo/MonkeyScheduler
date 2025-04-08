using System;
using System.Text;

namespace MonkeyScheduler.Logging
{
    public class DefaultLogFormatter : ILogFormatter
    {
        private readonly string _format;
        private readonly bool _includeTimestamp;
        private readonly bool _includeException;

        public DefaultLogFormatter(string format = "{timestamp} [{level}] {message}", 
                                 bool includeTimestamp = true,
                                 bool includeException = true)
        {
            _format = format;
            _includeTimestamp = includeTimestamp;
            _includeException = includeException;
        }

        public string Format(string level, string message, Exception exception = null)
        {
            var result = new StringBuilder(_format);

            if (_includeTimestamp)
            {
                result.Replace("{timestamp}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            else
            {
                result.Replace("{timestamp}", string.Empty);
            }

            result.Replace("{level}", level);
            result.Replace("{message}", message);

            if (_includeException && exception != null)
            {
                result.AppendLine();
                result.AppendLine("Exception:");
                result.AppendLine(exception.ToString());
            }

            return result.ToString();
        }
    }
} 