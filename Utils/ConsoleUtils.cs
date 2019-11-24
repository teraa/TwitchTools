using System;

namespace TwitchToolsV2
{
    public static class ConsoleUtils
    {
        private static readonly object _lock = new object();

        public static void Write(string message, ConsoleColor color)
        {
            lock (_lock)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(message);
                Console.ForegroundColor = oldColor;
            }
        }

        public static bool GetAnswer(string question, bool defaultValue)
        {
            if (Console.IsInputRedirected)
                return defaultValue;

            Console.Write($"{question} [{(defaultValue ? "Y/n" : "y/N")}] ");

            var answer = Console.ReadLine();

            if (answer == null)
                return false;

            if (answer.Length == 0)
                return defaultValue;

            if (string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
