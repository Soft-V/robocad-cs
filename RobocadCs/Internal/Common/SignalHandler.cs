using System;

namespace RobocadCs.Internal.Common
{
    internal static class SignalHandler
    {
        public static void Register(Action stop)
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                try { stop(); } catch (Exception error) { Console.Error.WriteLine("Robot stop failed: " + error); }
                Environment.Exit(0);
            };
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                try { stop(); } catch (Exception error) { Console.Error.WriteLine("Robot stop failed: " + error); }
            };
        }
    }
}
