using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VKBugTrackerBot
{
    internal static class MainClass
    {
        private static Dictionary<String, String> GetArguments(String[] args)
        {
            Dictionary<String, String> answer = new Dictionary<String, String>();
            foreach (List<char> arg in args.Where(arg => arg.StartsWith("--", StringComparison.Ordinal)).Select(s => s.ToList()))
            {
                arg.RemoveRange(0, 2);
                if (!arg.Contains('='))
                {
                    answer[new String(arg.ToArray())] = "True";
                }
                String[] str = new String(arg.ToArray()).Split("=".ToArray(), 2);
                if (str.Length != 2 || str[0] == "") continue;
                answer[str[0]] = str[1];
            }
            return answer;
        }

        private static void ReportInfo(String message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("INF: ");
            Console.ResetColor();
            Console.Write(message + Environment.NewLine);
        }
        private static void ReportError(String message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERR! ");
            Console.ResetColor();
            Console.Write(message + Environment.NewLine);
        }

        private static Int32 Main(String[] args)
        {
            var code = MainAsync(GetArguments(args)).GetAwaiter().GetResult();
            ReportInfo($"Exiting with code {code}.");
            return code;
        }

        private static async Task<Int32> MainAsync(Dictionary<String, String> args)
        {
            if (!args.ContainsKey("remixsid") || !args.ContainsKey("access-token") || !args.ContainsKey("group-id"))
            {
                ReportError("One of arguments(3) doesn't given.\n" +
                    "\tremixsid <string> | access-token <string> | group-id <unsigned int64>");
                return 1;
            }
            UInt64 group_id;
            try
            {
                group_id = Convert.ToUInt64(args["group-id"]);
            }
            catch
            {
                ReportError("Argument [\"group-id\"] is invalid.\n" +
                    "\tgroup-id <unsigned int64>, but provided <string>");
                return 1;
            }
            var t = new BugTrackerParser(args["remixsid"]);
            var d = new VkBot(args["access-token"], group_id);
            t.OnNewReport += (_, e) =>
            {
                d.SendReport(e);
            };
            new Thread(() => t.Start()).Start();
            new Thread(() => d.Start()).Start();
            await Task.Delay(-1);
            return 0;
        }
    }
}
