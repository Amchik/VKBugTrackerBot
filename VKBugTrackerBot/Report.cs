using System;
namespace VKBugTrackerBot
{
    public sealed class Report
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public String Name { get; set; }
        /// <summary>
        /// Gets the product string.
        /// </summary>
        /// <value>The product.</value>
        public String Product { get; set; }
        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public String[] Tags { get; set; }
        /// <summary>
        /// Gets the status string.
        /// </summary>
        /// <value>The status.</value>
        public String Status { get; set; }

        /// <summary>
        /// Gets the report identifier.
        /// </summary>
        /// <value>The report identifier.</value>
        // Example: bugreport208755
        public String ReportID { get; set; }
    }
}
