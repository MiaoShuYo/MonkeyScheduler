using System;

namespace MonkeyScheduler.Logging
{
    public interface ILogFormatter
    {
        string Format(string level, string message, Exception exception = null);
    }
} 