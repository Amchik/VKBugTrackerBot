using System;
using System.Text.Json.Serialization;

namespace VKBugTrackerBot
{
    [Serializable]
    public class BookmarksEventPayload
    {
        public enum EventType
        {
            Add,
            Remove
        }

        public BookmarksEventPayload()
        {
        }
        public BookmarksEventPayload(EventType type, Int32 reportId)
        {
            Type = type;
            ReportId = reportId;
        }

        [JsonPropertyName("type")]
        public EventType Type { get; set; }

        [JsonPropertyName("reportId")]
        public Int32 ReportId { get; set; }
    }
}