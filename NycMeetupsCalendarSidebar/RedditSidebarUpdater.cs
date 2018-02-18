using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using RedditSharp;

namespace NycMeetupsCalendarSidebar
{
    public class RedditSidebarUpdater
    {
        private const string SidebarStartText = "#Weekly Happy Hour";
        private const string TableHeader = "###Upcoming Meetups  \n\n";
        private readonly RedditCredentials _credentials;

        public RedditSidebarUpdater(RedditCredentials redditCredentials)
        {
            _credentials = redditCredentials;
        }

        public void UpdateSidebar(string tableText)
        {
            var token = GetAuthToken();
            var reddit = new Reddit(token) {RateLimit = WebAgent.RateLimitMode.Burst};

            var sub = reddit.GetSubreddit("nycmeetups");
            var sidebar = sub.Wiki.GetPage("config/sidebar").MarkdownContent;
            var startOfSidebar = sidebar.IndexOf(SidebarStartText);
            var newSidebar = TableHeader + tableText + "&nbsp; \n" + sidebar.Substring(startOfSidebar);

            var changesToSidebar = (newSidebar != sidebar &&
                                    newSidebar != sidebar.Replace("&amp;nbsp; \n#Weekly", "&nbsp; \n#Weekly"));
            if (changesToSidebar)
            {
                sub.Wiki.EditPage("config/sidebar", newSidebar);
            }
        }

        private string GetAuthToken()
        {
            var client = new HttpClient();
            var content =
                new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", _credentials.Username),
                    new KeyValuePair<string, string>("password", _credentials.Password)
                });
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_credentials.ClientId}:{_credentials.Secret}")));
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd("CalendarSidebarEvman182/0.1 by evman182");

            var response = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            var token = JsonConvert.DeserializeObject<PasswordGrant>(response).access_token;
            return token;
        }

        public string GetRedditTableText(Dictionary<DateTime, List<CalendarModels.Event>> calendar)
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
    }
    
    public class PasswordGrant
    {
        public string access_token { get; set; }
    }
}