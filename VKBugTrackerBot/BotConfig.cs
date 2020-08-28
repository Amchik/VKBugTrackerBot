using System;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text.Json;

namespace VKBugTrackerBot
{
    [Serializable]
    public class BotConfig : ISerializable
    {
        public static BotConfig CreateEmpty()
        {
            return new BotConfig
            {
                RemixSid = "see_readme",
                Token = "see_readme",
                GroupId = 0,
                Admins = new List<Int64>()
            };
        }

        public static BotConfig Deserialize(String json)
        {
            return JsonSerializer.Deserialize<BotConfig>(json);
        }

        public String Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        [JsonPropertyName("remixsid")]
        public String RemixSid { get; set; }

        [JsonPropertyName("access-token")]
        public String Token { get; set; }

        [JsonPropertyName("group-id")]
        public UInt64 GroupId { get; set; }

        [JsonPropertyName("admins")]
        public List<Int64> Admins { get; set; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("remixsid", RemixSid);
            info.AddValue("access-token", Token);
            info.AddValue("group-id", GroupId);
            info.AddValue("admins", Admins);
        }
    }
}