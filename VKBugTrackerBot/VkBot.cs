using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Model.Template;
using VkNet.Model.Template.Carousel;

namespace VKBugTrackerBot
{
    internal sealed class VkBot : IDisposable
    {
        public const String SAVE_TO_BOOKMARKS = "Сохранить в закладках";
        public const String REMOVE_FROM_BOOKMARKS = "Удалить из закладок";
        public const Int32 LAST_REPORTS_COUNT = 50;

#if DEBUG
        public static Boolean EnableReports = true;
#endif

        private readonly VkApi api;
        private readonly Dictionary<Int64, UserPreferences> users;
        private readonly List<Report> lastReports;
        private readonly UInt64 groupId;
        private Boolean alive;

        public IReadOnlyDictionary<Int64, UserPreferences> Users => users;

        public VkBot()
        {
        }

        public VkBot(String accessToken, UInt64 groupId)
        {
            api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = accessToken
            });
            users = new Dictionary<Int64, UserPreferences>();
            lastReports = new List<Report>();
            this.groupId = groupId;

            foreach (var user in MainClass.Config.Admins)
            {
                users[user] = new UserPreferences
                {
                    IsAdmin = true
                };
            }
        }

        public void Dispose()
        {
            alive = false;
        }

        public void SendReport(Report report)
        {
#if DEBUG
            if (!EnableReports) return;
#endif
            if (lastReports.Count >= LAST_REPORTS_COUNT)
            {
                lastReports.RemoveRange(0, lastReports.Count - LAST_REPORTS_COUNT + 1);
            }
            lastReports.Add(report);
            String rep = $"[{report.Product}] {report.Name}\n" +
                $"{String.Join(", ", report.Tags)}\n" +
                $"https://vk.com/bug{report.Id}";
            var ids = users.Where(u => !u.Value.ProductsBlacklist.Contains(report.Product) && !u.Value.DisableMessages).Select(u => u.Key);
            if (!ids.Any()) return;
            api.Messages.SendToUserIds(new MessagesSendParams
            {
                Message = rep,
                UserIds = ids,
#if !DEBUG
                RandomId = report.Id,
#else
                RandomId = new Random().Next(),
#endif
                Keyboard = new MessageKeyboard
                {
                    Inline = true,
                    OneTime = false,
                    Buttons = new MessageKeyboardButton[][]
                    {
                        new MessageKeyboardButton[]
                        {
                            new MessageKeyboardButton
                            {
                                Action = new MessageKeyboardButtonAction
                                {
                                    Type = KeyboardButtonActionType.OpenLink,
                                    Label = "Открыть отчёт",
                                    Link = new Uri($"https://vk.com/bug{report.Id}")
                                }
                            },
                            new MessageKeyboardButton
                            {
                                Action = new MessageKeyboardButtonAction
                                {
                                    Type = KeyboardButtonActionType.Text,
                                    Label = SAVE_TO_BOOKMARKS,
                                    Payload = JsonSerializer.Serialize(new BookmarksEventPayload(BookmarksEventPayload.EventType.Add, report.Id))
                                },
                                Color = KeyboardButtonColor.Primary
                            }
                        }
                    }
                }
            });
        }

        public void SendNotification(String message)
        {
            var ids = users.Where(u => u.Value.AllowNotification && !u.Value.DisableMessages).Select(u => u.Key);
            if (!ids.Any()) return;
            api.Messages.SendToUserIds(new MessagesSendParams
            {
                Message = message,
                UserIds = ids,
                RandomId = new Random().Next()
            });
        }

        public void Start()
        {
            var lp = api.Messages.GetLongPollServer(needPts: true, groupId: groupId);
            UInt64 ts = Convert.ToUInt64(lp.Pts);
            alive = true;
            while (alive)
            {
                Thread.Sleep(600);
                var e = api.Messages.GetLongPollHistory(new MessagesGetLongPollHistoryParams
                {
                    Pts = ts
                });
                if (e.NewPts == ts) continue;
                ts = e.NewPts;
                var msg = e.Messages.FirstOrDefault();
                if (msg == null) continue;
                if (msg.FromId < 0) continue;
                if (!users.ContainsKey((Int64)msg.PeerId)) users[(Int64)msg.PeerId] = new UserPreferences();
                var user = users[(Int64)msg.PeerId];
                if (msg.Text == SAVE_TO_BOOKMARKS || msg.Text == REMOVE_FROM_BOOKMARKS)
                {
                    BookmarksEventPayload eventPayload;
                    try
                    {
                        eventPayload = JsonSerializer.Deserialize<BookmarksEventPayload>(msg.Payload);
                    }
                    catch
                    {
                        continue;
                    }
                    if (eventPayload.Type == BookmarksEventPayload.EventType.Add)
                    {
                        Report report = lastReports.Find(r => r.Id == eventPayload.ReportId);
                        if (report == null)
                        {
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = "Report outdated"
                            });
                            continue;
                        }
                        user.Bookmarks.Add(report);
                        api.Messages.Send(new MessagesSendParams
                        {
                            PeerId = msg.PeerId,
                            RandomId = new Random().Next(),
                            Message = $"Report \"{report.Name}\" (#{report.Id}) added to bookmarks"
                        });
                    }
                    else
                    {
                        user.Bookmarks.RemoveWhere(r => r.Id == eventPayload.ReportId);
                        api.Messages.Send(new MessagesSendParams
                        {
                            PeerId = msg.PeerId,
                            RandomId = new Random().Next(),
                            Message = "Report removed from bookmarks"
                        });
                    }
                }
                if (msg.Text.FirstOrDefault() == '/')
                {
                    String[] args = msg.Text.Split(' ');
                    String cmd = new String(args.First().Skip(1).ToArray());
                    args = args.Skip(1).ToArray();
                    VkCommandContext context = new VkCommandContext(user, msg, cmd, args).InitWithVkApi(api);
                    var method = CommandAttribute.FindMethods<VkCommandContext>(cmd).FirstOrDefault();
                    if (method == null) continue; // TODO:
                    var hasAdminRequired = CommandAttribute.HasAdminPermissionsRequired(method);
                    if (hasAdminRequired.Value && !user.IsAdmin) continue; // TODO: see 199
                    var ctor = CommandAttribute.GetConstructorInfo(method);
                    Object instance;
                    try
                    {
                        instance = ctor.Invoke(new[] { context });
                    }
                    catch (Exception ex)
                    {
                        MainClass.ReportError($"[VKBOT] Failed to create instance from ctor: [{ex.GetType().Name}] {ex.Message}");
                        continue;
                    }
                    try
                    {
                        method.Invoke(instance, new Object[] { });
                    }
                    catch (Exception ex)
                    {
                        MainClass.ReportError($"[VKBOT] Unhandled exception while invoking method: [{ex.GetType().Name}] {ex.Message}");
                    }
                }
            }
        }
    }
}
