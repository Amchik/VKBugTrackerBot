using System;
namespace VKBugTracker
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
