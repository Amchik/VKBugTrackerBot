using System;
using System.Text.Json.Serialization;

namespace VKBugTrackerBot
{
    [Serializable]
    public sealed class Report
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        [JsonPropertyName("name")]
        public String Name { get; set; }
        /// <summary>
        /// Gets the product string.
        /// </summary>
        /// <value>The product.</value>
        [JsonPropertyName("product")]
        public String Product { get; set; }
        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <value>The tags.</value>
        [JsonPropertyName("tags")]
        public String[] Tags { get; set; }
        /// <summary>
        /// Gets the status string.
        /// </summary>
        /// <value>The status.</value>
        [JsonPropertyName("status")]
        public String Status { get; set; }

        /// <summary>
        /// Gets the report identifier string.
        /// </summary>
        /// <value>The report identifier string.</value>
        /// <example>
        /// bugreport208755
        /// </example>
        [Obsolete("This property returns string value. Use Id for integer value", false)]
        [JsonIgnore]
        public String ReportID { get => $"bugreport{Id}"; }

        /// <summary>
        /// Gets the report id.
        /// </summary>
        /// <value>The report id.</value>
        [JsonPropertyName("id")]
        public Int32 Id { get; set; }

        public override Int32 GetHashCode()
        {
            return this.Id;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || obj as Report == null)
            {
                return false;
            }

            Report r = (Report)obj;

            return this.Id == r.Id && this.Name == r.Name && this.Product == r.Product;
        }
    }
}
