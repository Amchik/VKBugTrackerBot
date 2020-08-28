using System;
using System.Collections.Generic;

namespace VKBugTrackerBot
{
    public class UserPreferences
    {
        public Boolean IsAdmin { get; set; }

        public List<String> ProductsBlacklist { get; }
        public Boolean AllowNotification { get; set; }
        public Boolean DisableMessages { get; set; }

        public UserPreferences()
        {
            IsAdmin = false;
            ProductsBlacklist = new List<String>();
            AllowNotification = true;
            DisableMessages = false;
        }
    }
}
