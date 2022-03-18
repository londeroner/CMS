using Serilog.Events;
using Serilog.Formatting;
using System.IO;

namespace LoggingService
{
    public class CustomTextFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (!logEvent.Level.HasFlag(LogEventLevel.Information))
            {
                output.Write($"{SEPARATOR}[{logEvent.Timestamp}]: {logEvent.Level}{SEPARATOR}");
                foreach (var item in logEvent.Properties) 
                    output.WriteLine($"{item.Key + " : " + item.Value}");

                if (logEvent.Exception is not null)
                {
                    output.Write($"{SEPARATOR_EXCEPTION}Exception: {logEvent.Exception}\n" +
                        $"StackTrace: {logEvent.Exception.StackTrace}\n" +
                        $"Message: {logEvent.Exception.Message}\n" +
                        $"Source: {logEvent.Exception.Source}\n" +
                        $"Inner Exception: {logEvent.Exception.InnerException}");
                }
                output.Write(SEPARATOR);
            }
        }

        private const string SEPARATOR = "\n----------------------------------------------------\n";
        private const string SEPARATOR_EXCEPTION = "\n------------------EXCEPTION_DETAIL------------------\n";
    }
}
