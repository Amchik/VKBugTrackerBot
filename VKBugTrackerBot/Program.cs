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
        public static BugTrackerParser BugTrackerParser { get; private set; }
        public static VkBot VkBot { get; private set; }

        private static void PrintTime()
        {
            Console.Write($"[{DateTime.Now.ToString("u")}] ");
        }

        public static void ReportInfo(String message)
        {
            PrintTime();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("INF: ");
            Console.ResetColor();
            Console.Write(message + Environment.NewLine);
        }
        public static void ReportError(String message)
        {
            PrintTime();
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
            BugTrackerParser = new BugTrackerParser(Config.RemixSid);
            VkBot = new VkBot(Config.Token, Config.GroupId);
            BugTrackerParser.OnNewReport += (_, e) =>
            {
                VkBot.SendReport(e);
            };
            // new Thread(() => BugTrackerParser.Start()).Start();
            new Thread(() => VkBot.Start()).Start();
            await Task.Delay(-1);
            return 0;
        }
    }
}
