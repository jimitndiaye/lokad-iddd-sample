using System;

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

        public void Execute(ICommand cmd)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Command: " + cmd);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            try
            {
                _service.Execute(cmd);
            }
            catch( Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(ex);
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
        }
    }
}