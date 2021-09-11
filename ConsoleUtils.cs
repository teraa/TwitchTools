using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TwitchTools
{
    public static class ConsoleUtils
    {
        public static void Error(string message)
        {
            Console.Error.WriteLine($"Error: {message}");
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

            var regex = new Regex(splitPattern, RegexOptions.Compiled);
            var result = new List<string>();
            string? line;
            while ((line = Console.ReadLine()) != null)
                result.AddRange(regex.Split(line));

            return result;
        }
    }
}
