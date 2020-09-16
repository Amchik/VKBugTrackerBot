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
                            Message = $"Report {report.Name} (#{report.Id}) added to bookmarks"
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
                    switch (args.FirstOrDefault())
                    {
#if DEBUG
                        case "/shutup":
                            EnableReports = !EnableReports;
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"Reports show: {EnableReports}"
                            });
                            break;
#endif
                        case "/bookmarks":
                            Int32 startIndex = 0;
                            if (args.Length == 2)
                            {
                                try
                                {
                                    startIndex = (Int32.Parse(args[1]) - 1) * 3;
                                }
                                catch
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = $"Использование: /bookmarks [page (int)]"
                                    });
                                    break;
                                }
                            }
                            KeyboardBuilder kb = new KeyboardBuilder(false);
                            kb.SetInline();
                            foreach (var bookmark in user.Bookmarks.Skip(startIndex).Take(3))
                            {
                                kb.AddButton(new MessageKeyboardButtonAction
                                {
                                    Type = KeyboardButtonActionType.OpenLink,
                                    Label = new String($"{bookmark.Product}: {bookmark.Name}".Take(40).ToArray()),
                                    Link = new Uri($"https://vk.com/bug{bookmark.Id}"),
                                    Payload = ""
                                });
                                kb.AddButton(new MessageKeyboardButtonAction
                                {
                                    Type = KeyboardButtonActionType.Text,
                                    Label = REMOVE_FROM_BOOKMARKS,
                                    Payload = JsonSerializer.Serialize(new BookmarksEventPayload(BookmarksEventPayload.EventType.Remove, bookmark.Id))
                                }, KeyboardButtonColor.Negative);
                                kb.AddLine();
                            }
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"Закладки ({startIndex}-{Math.Min(startIndex + 3, user.Bookmarks.Count)}/{user.Bookmarks.Count})",
                                Keyboard = kb.Build()
                            });
                            break;

                        case "/toggleAll":
                            user.DisableMessages = !user.DisableMessages;
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = "Service alerts " + (!user.DisableMessages ? "enabled" : "disabled")
                            });
                            break;

                        case "/toggleNotifications":
                            user.AllowNotification = !user.AllowNotification;
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = "Notifications " + (user.AllowNotification ? "enabled" : "disabled")
                            });
                            break;

                        case "/toggleProduct":
                            if (args.Length < 2)
                            {
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"Usage: /toggleProduct <product name>"
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
                                Message = $"{pr} is " + (user.ProductsBlacklist.Contains(pr) ? "added" : "removed") + " from blacklist."
                            });
                            break;

                        case "/status":
                            {
                                var bl = String.Join(", ", user.ProductsBlacklist);
                                StringBuilder sb = new StringBuilder();
                                if (user.IsAdmin) sb.Append("Admin ");
                                if (user.AllowNotification) sb.Append("Notifications ");
                                if (!user.DisableMessages) sb.Append("Messages ");
                                sb.AppendLine();
                                sb.Append("Blacklisted: ");
                                sb.Append(bl);
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = sb.ToString()
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
                                    Message = $"Unkown command"
                                });
                                return;
                            }
                            if (args.Length < 2)
                            {
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"Usage: /send <message>"
                                });
                            }
                            String snd = String.Join(" ", args, 1, args.Length - 1);
                            SendNotification(snd);
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = $"Ok"
                            });
                            break;

                        case "/admin":
                            {
                                const String USAGE = "Usage: /admin <user id>";
                                if (!user.IsAdmin)
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = $"Unkown command"
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
                                        Message = USAGE
                                    });
                                    return;
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = USAGE
                                    });
                                    return;
                                }
                                if (!users.TryGetValue(toAdmin, out UserPreferences v))
                                {
                                    api.Messages.Send(new MessagesSendParams
                                    {
                                        PeerId = msg.PeerId,
                                        RandomId = new Random().Next(),
                                        Message = $"Error: [id{toAdmin}|User] not joined"
                                    });
                                    return;
                                }
                                v.IsAdmin = !v.IsAdmin;
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = $"[[id{toAdmin}|{toAdmin}]] is now " + (v.IsAdmin ? "admin" : "not admin")
                                });
                            }
                            break;

                        case "/help":
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine("--- COMMANDS ---");
                                sb.AppendLine("/toggleAll - toggle all messages");
                                sb.AppendLine("/toggleNotifications - toggle service messages");
                                sb.AppendLine("/toggleProduct - toggle notifications from product");
                                sb.AppendLine("/status - info about you");
                                sb.AppendLine("/bookmarks - show bookmarks");
                                sb.AppendLine("/help - show this message");
                                if (user.IsAdmin)
                                {
                                    sb.AppendLine("--- ADMIN COMMANDS ---");
                                    sb.AppendLine("/send - Send a service message");
                                    sb.AppendLine("/admin - Grant/revoke admin privileges");
                                }
                                api.Messages.Send(new MessagesSendParams
                                {
                                    PeerId = msg.PeerId,
                                    RandomId = new Random().Next(),
                                    Message = sb.ToString()
                                });
                                break;
                            }

                        default:
                            api.Messages.Send(new MessagesSendParams
                            {
                                PeerId = msg.PeerId,
                                RandomId = new Random().Next(),
                                Message = "Unkown command"
                            });
                            break;
                    }
                }
            }
        }
    }
}
