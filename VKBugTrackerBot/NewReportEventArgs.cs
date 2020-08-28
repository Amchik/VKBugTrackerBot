using System;

namespace VKBugTrackerBot
{
    public class NewReportEventArgs : EventArgs
    {
        public Report Report { get; }

        public NewReportEventArgs(Report report)
        {
            Report = report;
        }
    }
}
