using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKBugTrackerBot
{
    internal sealed class VkBot : IDisposable
    {
        private readonly VkApi api;
        private readonly Dictionary<Int64, UserPreferences> users;
        private readonly UInt64 groupId;
        private Boolean alive;

        public VkBot(String accessToken, UInt64 groupId)
        {
            api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = accessToken
            });
            users = new Dictionary<Int64, UserPreferences>();
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
            String rep = $"[{report.Product}] {report.Name}\n" +
                $"{String.Join(", ", report.Tags)}\n" +
                $"https://vk.com/{report.ReportID.Replace("report", "")}";
            var ids = users.Where(u => !u.Value.ProductsBlacklist.Contains(report.Product) && !u.Value.DisableMessages).Select(u => u.Key);
            if (!ids.Any()) return;
            api.Messages.SendToUserIds(new MessagesSendParams
            {
                Message = rep,
                UserIds = ids,
                RandomId = new Random().Next()
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
                if (!users.ContainsKey((Int64)msg.PeerId)) users[(Int64)msg.PeerId] = new UserPreferences();
                var user = users[(Int64)msg.PeerId];
                if (msg.Text.FirstOrDefault() == '/')
                {
                    String[] args = msg.Text.Split(' ');
                    switch (args.FirstOrDefault())
                    {
                        case "/toggleAll":
                            user.DisableMessages = !user.DisableMessages;
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"[OK] DisableMessages={user.DisableMessages}"
                            });
                            break;

                        case "/toggleNotifications":
                            user.DisableMessages = !user.AllowNotification;
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"[OK] AllowNotification={user.DisableMessages}"
                            });
                            break;

                        case "/toggleProduct":
                            if (args.Length < 2)
                            {
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"[FAIL] Keine Argumente [1..] angegeben"
                                });
                                break;
                            }
                            String pr = String.Join(" ", args, 1, args.Length - 1);
                            if (user.ProductsBlacklist.Contains(pr))
                            {
                                user.ProductsBlacklist.Remove(pr);
                            }
                            else
                            {
                                user.ProductsBlacklist.Add(pr);
                            }
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"[OK] Blacklisted[\"{pr}\"]={user.ProductsBlacklist.Contains(pr)}"
                            });
                            break;

                        case "/status":
                            {
                                var bl = String.Join(", ", user.ProductsBlacklist.Select(t => $"\"{t}\""));
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"[OK] IsAdmin={user.IsAdmin}\n" +
                                        $"AllowNotification={user.AllowNotification}\n" +
                                        $"DisableMessages={user.DisableMessages}\n" +
                                        $"Blacklisted=[{bl}]"
                                });
                            }
                            break;

                        case "/send":
                            if (!user.IsAdmin)
                            {
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"[FAIL] Berechtigungen verweigert."
                                });
                                return;
                            }
                            if (args.Length < 2)
                            {
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"[FAIL] Keine Argumente [1..] angegeben"
                                });
                            }
                            String snd = String.Join(" ", args, 1, args.Length - 1);
                            SendNotification(snd);
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"[OK] \\keiner"
                            });
                            break;

                        case "/admin":
                            {
                                if (!user.IsAdmin)
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = $"[FAIL] Berechtigungen verweigert."
                                    });
                                    return;
                                }
                                Int32 toAdmin;
                                try
                                {
                                    toAdmin = Convert.ToInt32(args[1]);
                                }
                                catch (FormatException)
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = $"[FAIL] Argument [1] not given"
                                    });
                                    return;
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = $"[FAIL] Invalid argument [1] (expected: <int32>, actual: <string>)"
                                    });
                                    return;
                                }
                                if (!users.TryGetValue(toAdmin, out UserPreferences v))
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = $"[FAIL] [id{toAdmin}|User] not joined!"
                                    });
                                    return;
                                }
                                v.IsAdmin = !v.IsAdmin;
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"[OK] [[id{toAdmin}|{toAdmin}]]IsAdmin={v.IsAdmin}"
                                });
                            }
                            break;

                        case "/help":
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"[OK] /toggleAll - nachrichten umschalten\n" +
                                    "/toggleNotifications - benachrichtigungen umschalten\n" +
                                    "/toggleProduct - produktanzeige umschalten\n" +
                                    "/status - unfos über dich\n" +
                                    "/send - benachrichtigung senden\n" +
                                    "/help - zeige diese nachricht\n\n"
                            });
                            break;

                        default:
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = "[FAIL] Nicht gefunden."
                            });
                            break;
                    }
                }
            }
        }
    }
}
