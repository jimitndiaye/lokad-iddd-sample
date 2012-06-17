using System;

namespace Sample
{
    public sealed class LoggingWrapper : IApplicationService
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
            _service.Execute(cmd);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  Success");
            Console.ForegroundColor = ConsoleColor.DarkGray;

        }
    }
}