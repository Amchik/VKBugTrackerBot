using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.RequestParams;

namespace VKBugTrackerBot.Tests
{
    [TestFixture]
    public class VKCommandsTests
    {
        const Int32 BOOKMARKS_COUNT = 10;
        const Int32 BOOKMARKS_ON_PAGE = 3;

        public UserPreferences FakeUser;
        public UserPreferences FakeAdmin;

        public void GenerateFakeBookmarks(UserPreferences user, Int32 count)
        {
            for (Int32 i = 0; i < count; i++)
            {
                user.Bookmarks.Add(new Report
                {
                    Id = i,
                    Name = "Some bookmark #" + i,
                    Product = "Some product",
                    Tags = new[] { "Tag1" }
                });
            }
        }

        public MessagesSendParams InvokeCommand(UserPreferences user, String cmd, params String[] args)
        {
            MessagesSendParams p = null;
            VkCommandContext context = new VkCommandContext(user, null, cmd, args);
            context.InitWithFunc((@params) =>
            {
                p = @params;
                return null;
            });
            var method = CommandAttribute.FindMethods<VkCommandContext>(cmd, assembly: Assembly.GetAssembly(typeof(VkCommands))).First();
            var hasAdminRequired = CommandAttribute.HasAdminPermissionsRequired(method);
            if (hasAdminRequired.Value && !user.IsAdmin) Assert.Fail("User is not admin");
            var ctor = CommandAttribute.GetConstructorInfo(method);
            Object instance = ctor.Invoke(new[] { context });
            method.Invoke(instance, new Object[] { });
            return p;
        }

        [SetUp]
        public void SetUp()
        {
            FakeUser = new UserPreferences();
            GenerateFakeBookmarks(FakeUser, BOOKMARKS_COUNT);
            FakeAdmin = new UserPreferences
            {
                IsAdmin = true
            };
        }

        [Test]
        public void TestHelpCommandAsUser()
        {
            var p = InvokeCommand(FakeUser, "help");
            Assert.IsFalse(p.Message.Contains("ADMIN"));
        }

        [Test]
        public void TestHelpCommandAsAdmin()
        {
            var p = InvokeCommand(FakeAdmin, "help");
            Assert.IsTrue(p.Message.Contains("ADMIN"));
        }

        [Test]
        public void TestToggleCommands()
        {
            UserPreferences user = new UserPreferences();
            user.AllowNotification = false;
            user.DisableMessages = false;

            InvokeCommand(user, "toggleAll");
            Assert.IsTrue(user.DisableMessages);

            InvokeCommand(user, "toggleNotifications");
            Assert.IsTrue(user.AllowNotification);
        }

        [TestCase("Test-Product")]
        [TestCase("Test", "Product")]
        public void TestToggleProduct(params String[] args)
        {
            InvokeCommand(FakeUser, "toggleProduct", args);
            Assert.Contains(String.Join(" ", args), FakeUser.ProductsBlacklist);

            InvokeCommand(FakeUser, "toggleProduct", args);
            Assert.IsFalse(FakeUser.ProductsBlacklist.Contains(String.Join(" ", args)), $"FakeUser's product blacklist contains product");
        }

        [TestCase(false, false, false, new[] { "Test product" })]
        [TestCase(true, false, false, new[] { "Test product 1", "Test product 2" })]
        [TestCase(false, true, true, new String[] { })]
        public void TestStatus(Boolean messages, Boolean notifications, Boolean admin, String[] products)
        {
            UserPreferences user = new UserPreferences();
            user.IsAdmin = admin;
            user.DisableMessages = !messages;
            user.AllowNotification = notifications;
            user.ProductsBlacklist.Clear();
            user.ProductsBlacklist.AddRange(products);

            var p = InvokeCommand(user, "status");
            if (messages)
                Assert.IsTrue(p.Message.Contains("Messages"));
            else
                Assert.IsFalse(p.Message.Contains("Messages"));

            if (admin)
                Assert.IsTrue(p.Message.Contains("Admin"));
            else
                Assert.IsFalse(p.Message.Contains("Admin"));

            if (notifications)
                Assert.IsTrue(p.Message.Contains("Notifications"));
            else
                Assert.IsFalse(p.Message.Contains("Notifications"));

            foreach (var prod in products)
            {
                Assert.IsTrue(p.Message.Contains(prod));
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TestBookmarksCount(Int32 page)
        {
            var p = InvokeCommand(FakeUser, "bookmarks", page.ToString());
            var excepted = $"({BOOKMARKS_ON_PAGE * (page - 1) + 1}-{Math.Min(BOOKMARKS_COUNT, BOOKMARKS_ON_PAGE * page)}/{BOOKMARKS_COUNT})";
            Assert.IsTrue(p.Message.Contains(excepted), $"Excepted \"{excepted}\", but actual \"{p.Message}\"");
        }

        [Test]
        public void TestBookmarksNames()
        {
            var p = InvokeCommand(FakeUser, "bookmarks", "1");
            var kb = p.Keyboard.Buttons.ToArray();

            Assert.AreEqual("Some product: Some bookmark #0", kb[0].FirstOrDefault().Action.Label);
            Assert.AreEqual("Some product: Some bookmark #1", kb[1].FirstOrDefault().Action.Label);
            Assert.AreEqual("Some product: Some bookmark #2", kb[2].FirstOrDefault().Action.Label);
        }

        [Test]
        public void TestBookmarksKeyboard()
        {
            var p = InvokeCommand(FakeUser, "bookmarks", "1");
            var kb = p.Keyboard.Buttons;

            Assert.AreEqual(3, kb.Count(), "Excepted 3 lines");
            foreach (var buttons in kb)
            {
                Assert.AreEqual(2, buttons.Count(), "Excepted 2 buttons on line");

                var b1 = buttons.ElementAt(0);
                Assert.AreEqual(b1.Action.Type, KeyboardButtonActionType.OpenLink, "First button must be type open_link");

                var b2 = buttons.ElementAt(1);
                Assert.AreEqual(b2.Action.Type, KeyboardButtonActionType.Text, "Second button must be type text");
                Assert.AreEqual(b2.Color, KeyboardButtonColor.Negative, "Second button must be color negative");
            }
        }
    }
}