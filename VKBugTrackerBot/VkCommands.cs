using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;

namespace VKBugTrackerBot
{
    public class VkCommands : CommandsModule<VkCommandContext>
    {
        public VkCommands(VkCommandContext context) : base(context)
        {
        }

        [Command("bookmarks")]
        public void BookmarksCommand()
        {
            Int32 startIndex = 0;
            if (Context.Arguments.Length == 1)
            {
                try
                {
                    startIndex = (Int32.Parse(Context.Arguments[0]) - 1) * 3;
                }
                catch
                {
                    Context.Reply(new MessagesSendParams
                    {
                        RandomId = new Random().Next(),
                        Message = $"Использование: /bookmarks [page (int)]"
                    });
                    return;
                }
            }
            KeyboardBuilder kb = new KeyboardBuilder(false);
            kb.SetInline();
            foreach (var bookmark in Context.User.Bookmarks.Skip(startIndex).Take(3))
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
                    Label = VkBot.REMOVE_FROM_BOOKMARKS,
                    Payload = JsonSerializer.Serialize(new BookmarksEventPayload(BookmarksEventPayload.EventType.Remove, bookmark.Id))
                }, KeyboardButtonColor.Negative);
                kb.AddLine();
            }
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = $"Закладки ({startIndex}-{Math.Min(startIndex + 3, Context.User.Bookmarks.Count)}/{Context.User.Bookmarks.Count})",
                Keyboard = kb.Build()
            });
        }

        [Command("toggleAll")]
        public void ToggleAllCommand()
        {
            Context.User.DisableMessages = !Context.User.DisableMessages;
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = "Service alerts " + (!Context.User.DisableMessages ? "enabled" : "disabled")
            });
        }

        [Command("toggleNotifications")]
        public void ToggleNotificationsCommand()
        {
            Context.User.AllowNotification = !Context.User.AllowNotification;
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = "Notifications " + (Context.User.AllowNotification ? "enabled" : "disabled")
            });
        }

        [Command("toggleProduct")]
        public void ToggleProductCommand()
        {
            if (Context.Arguments.Length < 1)
            {
                Context.Reply(new MessagesSendParams
                {
                    RandomId = new Random().Next(),
                    Message = $"Usage: /toggleProduct <product name>"
                });
                return;
            }
            String pr = String.Join(" ", Context.Arguments);
            if (Context.User.ProductsBlacklist.Contains(pr))
            {
                Context.User.ProductsBlacklist.Remove(pr);
            }
            else
            {
                Context.User.ProductsBlacklist.Add(pr);
            }
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = $"{pr} is " + (Context.User.ProductsBlacklist.Contains(pr) ? "added" : "removed") + " from blacklist."
            });
        }

        [Command("status")]
        public void StatusCommand()
        {
            var bl = String.Join(", ", Context.User.ProductsBlacklist);
            StringBuilder sb = new StringBuilder();
            if (Context.User.IsAdmin) sb.Append("Admin ");
            if (Context.User.AllowNotification) sb.Append("Notifications ");
            if (!Context.User.DisableMessages) sb.Append("Messages ");
            sb.AppendLine();
            sb.Append("Blacklisted: ");
            sb.Append(bl);
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = sb.ToString()
            });
        }

        [Command("send", true)]
        public void SendCommand()
        {
            if (Context.Arguments.Length < 1)
            {
                Context.Reply(new MessagesSendParams
                {
                    RandomId = new Random().Next(),
                    Message = $"Usage: /send <message>"
                });
            }
            String snd = String.Join(" ", Context.Arguments);
            MainClass.VkBot.SendNotification(snd);
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = $"Ok"
            });
        }

        [Command("admin", true)]
        public void AdminCommand()
        {
            const String USAGE = "Usage: /admin <user id>";
            if (!Context.User.IsAdmin)
            {
                Context.Reply(new MessagesSendParams
                {
                    RandomId = new Random().Next(),
                    Message = $"Unkown command"
                });
                return;
            }
            Int32 toAdmin;
            try
            {
                toAdmin = Convert.ToInt32(Context.Arguments[0]);
            }
            catch (FormatException)
            {
                Context.Reply(new MessagesSendParams
                {
                    RandomId = new Random().Next(),
                    Message = USAGE
                });
                return;
            }
            catch (ArgumentOutOfRangeException)
            {
                Context.Reply(new MessagesSendParams
                {
                    RandomId = new Random().Next(),
                    Message = USAGE
                });
                return;
            }
            if (!MainClass.VkBot.Users.TryGetValue(toAdmin, out UserPreferences v))
            {
                Context.Reply(new MessagesSendParams
                {
                    RandomId = new Random().Next(),
                    Message = $"Error: [id{toAdmin}|User] not joined"
                });
                return;
            }
            v.IsAdmin = !v.IsAdmin;
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = $"[[id{toAdmin}|{toAdmin}]] is now " + (v.IsAdmin ? "admin" : "not admin")
            });
        }

        [Command("help")]
        public void HelpCommand()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--- COMMANDS ---");
            sb.AppendLine("/toggleAll - toggle all messages");
            sb.AppendLine("/toggleNotifications - toggle service messages");
            sb.AppendLine("/toggleProduct - toggle notifications from product");
            sb.AppendLine("/status - info about you");
            sb.AppendLine("/bookmarks - show bookmarks");
            sb.AppendLine("/help - show this message");
            if (Context.User.IsAdmin)
            {
                sb.AppendLine("--- ADMIN COMMANDS ---");
                sb.AppendLine("/send - Send a service message");
                sb.AppendLine("/admin - Grant/revoke admin privileges");
            }
            Context.Reply(new MessagesSendParams
            {
                RandomId = new Random().Next(),
                Message = sb.ToString()
            });
        }
    }
}
