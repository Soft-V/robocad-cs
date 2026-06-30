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
                try { stop(); } catch { }
                Environment.Exit(0);
            };
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                try { stop(); } catch { }
            };
        }
    }
}
