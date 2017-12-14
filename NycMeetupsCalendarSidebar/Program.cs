using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using RedditSharp;

namespace NycMeetupsCalendarSidebar
{
    class Program
    {
        private const string SidebarStartText = "#Weekly Happy Hour";
        private const string TableHeader = "###Upcoming Meetups  \n\n";

        private static readonly string Username = ConfigurationManager.AppSettings["username"];
        private static readonly string Password = ConfigurationManager.AppSettings["password"];
        private static readonly string ClientId = ConfigurationManager.AppSettings["ClientId"];
        private static readonly string Secret   = ConfigurationManager.AppSettings["secret"];

        static void Main()
        {
            var calendar = GenerateCalendar();
            var tableText = GetRedditTableText(calendar);
            
            var token = GetAuthToken();
            var reddit = new Reddit(token);
            reddit.RateLimit = WebAgent.RateLimitMode.Burst;
            
            var sub = reddit.GetSubreddit("nycmeetups");
            var sidebar = sub.Wiki.GetPage("config/sidebar").MarkdownContent;
            var startOfSidebar = sidebar.IndexOf(SidebarStartText);
            var newSidebar = TableHeader + tableText + "&nbsp; \n" + sidebar.Substring(startOfSidebar);

            var changesToSidebar = (newSidebar != sidebar);
            if (changesToSidebar)
            {
                sub.Wiki.EditPage("config/sidebar", newSidebar);
            }
        }

        private static string GetAuthToken()
        {
            var client = new HttpClient();
            var content =
                new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", Username),
                    new KeyValuePair<string, string>("password", Password)
                });
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ClientId}:{Secret}")));
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd("CalendarSidebarEvman182/0.1 by evman182");

            var response = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            var token = JsonConvert.DeserializeObject<PasswordGrant>(response).access_token;
            return token;
        }

        private static string GetRedditTableText(Dictionary<DateTime, List<CalendarModels.Event>> calendar)
        {
            var tableText = "Date|Event" + Environment.NewLine +
                            ":-:|:-:" + Environment.NewLine;

            foreach (var kvp in calendar.OrderBy(k => k.Key))
            {
                foreach (var redditEvent in kvp.Value.OrderBy(x => x.CreateDate))
                {
                    var date = kvp.Key.ToString("dd MMM");
                    var title = redditEvent.Title;
                    var url = redditEvent.Url;

                    tableText += $"{date}|[{title}]({url})" + Environment.NewLine;
                }
            }
            return tableText;
        }

        private static Dictionary<DateTime, List<CalendarModels.Event>> GenerateCalendar()
        {
            
            var calendar = GenerateEmptyCalendarForNextSevenDays();
            var jsonEvents = GetEventsFromReddit(calendar);
            PopulateCalendar(jsonEvents, calendar);
            return calendar;
        }

        private static Dictionary<DateTime, List<CalendarModels.Event>> GenerateEmptyCalendarForNextSevenDays()
        {
            var today = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"))
                .Date;
            var calendar = Enumerable.Range(0, 7)
                .Select(d => today.AddDays(d))
                .ToList()
                .ToDictionary(d => d, d => new List<CalendarModels.Event>());
            return calendar;
        }

        private static void PopulateCalendar(List<CalendarModels.Post> jsonEvents, Dictionary<DateTime, List<CalendarModels.Event>> calendar)
        {
            foreach (var e in jsonEvents)
            {
                var datePart = Regex.Match(e.link_flair_text, "[01][0-9]/[0-3][0-9]");
                if (datePart.Success)
                {
                    var correctKey = GetDateTimeKeyForEvent(calendar, datePart);
                    if (correctKey != default(DateTime))
                        calendar[correctKey].Add(new CalendarModels.Event { Url = e.url, Title = e.title, CreateDate = ConvertCreatedUtcToInt(e.created_utc) });
                }
            }
        }

        private static int ConvertCreatedUtcToInt(string createdUtc)
        {
            
            var indexOfPeriod = createdUtc.IndexOf('.');
            var intString = createdUtc.Substring(0, indexOfPeriod);
            var createdInt = int.Parse(intString);
            return createdInt;
        }

        private static DateTime GetDateTimeKeyForEvent(Dictionary<DateTime, List<CalendarModels.Event>> calendar, Match datePart)
        {
            string monthDate = datePart.Value;
            var month = int.Parse(monthDate.Substring(0, 2));
            var day = int.Parse(monthDate.Substring(3, 2));
            var correctKey = calendar.Keys.SingleOrDefault(k => k.Month == month && k.Day == day);
            return correctKey;
        }

        private static List<CalendarModels.Post> GetEventsFromReddit(Dictionary<DateTime, List<CalendarModels.Event>> calendar)
        {
            var searchString = string.Join("+OR+",
                calendar.Keys.Select(r => HttpUtility.UrlEncode("flair:" + r.ToString("MM/dd"))));
            var searchUrl =
                $"https://www.reddit.com/r/nycmeetups/search.json?q={searchString}&sort=relevance&restrict_sr=on&t=month";
            var wc = new WebClient();
            var json = wc.DownloadString(searchUrl);
            var jsonEvents = JsonConvert.DeserializeObject<CalendarModels.Listing>(json).data.children.Select(d => d.data).ToList();
            return jsonEvents;
        }
    }

    public class PasswordGrant
    {
        public string access_token { get; set; }
    }
}
