using System;
using System.Diagnostics;

namespace Sample
{
    /// <summary>
    /// Demonstrates how to add logging aspect to any application service
    /// </summary>
    public class LoggingWrapper : IApplicationService
    {
        readonly IApplicationService _service;
        public LoggingWrapper(IApplicationService service)
        {
            _service = service;
        }

        static void WriteLine(ConsoleColor color, string text, params object[] args)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text, args);
            Console.ForegroundColor = oldColor;
        }

        public void Execute(ICommand cmd)
        {
            WriteLine(ConsoleColor.DarkCyan, "Command: " + cmd);
            try
            {
                var watch = Stopwatch.StartNew();
                _service.Execute(cmd);
                var ms = watch.ElapsedMilliseconds;
                WriteLine(ConsoleColor.DarkCyan, "  Completed in {0} ms", ms);
            }
            catch( Exception ex)
            {
                WriteLine(ConsoleColor.DarkRed, "Error: {0}", ex);
            }
        }
    }
}