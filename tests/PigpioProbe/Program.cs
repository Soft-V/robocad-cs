using System;
using System.Globalization;
using RobocadCs.Internal.Common;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 2) return 2;
        var pwm = new PigpioPwm(args[0], args[1], 100, 0.15);
        int[] gpios = { 4, 18, 17, 27, 23, 22, 24, 25, 7, 5 };
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            string[] fields = line.Split(' ');
            try
            {
                if (fields[0] == "set")
                    pwm.SetDutycycle(int.Parse(fields[1], CultureInfo.InvariantCulture),
                                     double.Parse(fields[2], CultureInfo.InvariantCulture));
                else if (fields[0] == "stop") pwm.Stop(gpios);
                else if (fields[0] == "stop-used") pwm.Stop();
                else return 2;
                Console.WriteLine("ok");
            }
            catch (Exception error)
            {
                Console.WriteLine("error: " + error.Message.Replace('\n', ' ').Replace('\r', ' '));
            }
        }
        return 0;
    }
}
