using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Threading;

namespace VKBugTrackerBot
{
    public class BugTrackerParser : IDisposable
    {
        private const Int32 MAX_SAVED_REPORTS_ID = 50;

        private Cookie remixsid;
        private Boolean alive = false;

        private readonly List<String> reportIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:VKBugTracker.BugTrackerParser"/> class.
        /// </summary>
        /// <param name="remixsid">Cookie for auth on vk.com</param>
        public BugTrackerParser(String remixsid)
        {
            this.remixsid = new Cookie(nameof(remixsid), remixsid)
            {
                Domain = ".vk.com"
            };
            reportIds = new List<string>(51);
        }

        /// <summary>
        /// Occurs when a new report appears.
        /// </summary>
        public event EventHandler<Report> OnNewReport = (sender, e) => { };

        public void Dispose()
        {
            alive = false;
        }

        /// <summary>
        /// Start listener.
        /// </summary>
        /// <exception cref="InvalidOperationException">Listener already started</exception>
        public void Start() => Start(10000);

        /// <summary>
        /// Start listener.
        /// </summary>
        /// <param name="rate">Update rate (milliseconds)</param>
        /// <exception cref="InvalidOperationException">Listener already started</exception>
        public void Start(Int32 rate)
        {
            if (alive) throw new InvalidOperationException("Listener already started");
            alive = true;
            while (alive)
            {
                Thread.Sleep(rate);
                HttpWebRequest request = WebRequest.CreateHttp("https://vk.com/bugs");
                request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:73.0) Gecko/20100101 VKBugTrackerListener/1.0";
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(remixsid);
                HttpWebResponse res;
                try
                {
                    res = (HttpWebResponse)request.GetResponse();
                }
                catch { continue; }
                String body;
                using (StreamReader stream = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("windows-1251")))
                    body = stream.ReadToEnd();
                for (int i = 0; i < res.Cookies.Count; i++)
                {
                    Cookie elem = res.Cookies[i];
                    if (elem.Name == "remixsid")
                    {
                        remixsid.Value = elem.Value;
                        break;
                    }
                }
                Report report;
                try
                {
                    var html = new HtmlDocument();
                    html.LoadHtml(body);
                    HtmlNode rawReport, reportInfo;
                    try
                    {
                        rawReport = html.GetElementbyId("bt_reports").ChildNodes[0];
                        reportInfo = rawReport.ChildNodes[3];
                    }
                    catch (NullReferenceException)
                    {
                        MainClass.ReportError("[BugTrackerParser] Failed to parse report.");
                        MainClass.ReportInfo("Continue executing BugTrackerParser.");
                        continue;
                    }
                    var tags = reportInfo.ChildNodes[3].ChildNodes.Select(n => n.InnerText).ToList();
                    report = new Report
                    {
                        ReportID = rawReport.Id,
                        Name = reportInfo.ChildNodes[1].ChildNodes[0].InnerText.Replace("&quot;", "\""),
                        Status = reportInfo.ChildNodes[5].ChildNodes[3].ChildNodes[0].InnerText,
                        Product = tags[0]
                    };
                    tags.RemoveAt(0);
                    report.Tags = tags.Where(t => !String.IsNullOrWhiteSpace(t)).ToArray();
                    if (reportIds.Contains(report.ReportID)) continue;
                    if (reportIds.Count >= MAX_SAVED_REPORTS_ID)
                    {
                        reportIds.RemoveRange(0, reportIds.Count - (MAX_SAVED_REPORTS_ID - 1));
                    }
                }
                catch (Exception e)
                {
                    MainClass.ReportError($"Exception at BugTrackerParser: [{e.GetType().Name}] {e.Message}");
                    MainClass.ReportInfo("Continue executing BugTrackerParser.");
                    continue;
                }
                reportIds.Add(report.ReportID);
                OnNewReport?.Invoke(this, report);
            }
        }
    }
}
