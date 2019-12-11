using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TwitchTools.Utils
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

            return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase);
        }

        public static List<string> GetInputList(string message, string splitPattern)
        {
            if (!Console.IsInputRedirected)
                Console.WriteLine(message);

            var result = new List<string>();
            string line;
            while ((line = Console.ReadLine()) != null)
                result.AddRange(Regex.Split(line, splitPattern));

            return result;
        }
    }
}
