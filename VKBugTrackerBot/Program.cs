using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VKBugTrackerBot
{
    internal static class MainClass
    {
        public static BotConfig Config { get; private set; }

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
            var code = MainAsync(args).GetAwaiter().GetResult();
            ReportInfo($"Exiting with code {code}.");
            return code;
        }

        private static async Task<Int32> MainAsync(String[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (!File.Exists("config.json"))
            {
                ReportError("Failed to find 'config.json'.");
                ReportInfo("Generating config...");
                try
                {
                    await File.WriteAllTextAsync("config.json",
                        BotConfig.CreateEmpty().Serialize());
                }
                catch (Exception e)
                {
                    ReportError($"Failed to generate config: [{e.GetType().Name}] {e.Message}");
                    return 1;
                }
                ReportInfo("New config stored at 'config.json'. Edit it and run again.");
                return 2;
            }
            String rawConfig;
            try
            {
                rawConfig = File.ReadAllText("config.json");
            }
            catch (Exception e)
            {
                ReportError($"Failed to open 'config.json': [{e.GetType().Name}] {e.Message}");
                return 3;
            }
            try
            {
                Config = BotConfig.Deserialize(rawConfig);
            }
            catch (Exception e)
            {
                ReportError($"Failed to read 'config.json': [{e.GetType().Name}] {e.Message}");
                return 4;
            }
            var t = new BugTrackerParser(Config.RemixSid);
            var d = new VkBot(Config.Token, Config.GroupId);
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
