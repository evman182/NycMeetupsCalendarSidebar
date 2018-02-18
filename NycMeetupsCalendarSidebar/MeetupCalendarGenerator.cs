using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace NycMeetupsCalendarSidebar
{
    public static class MeetupCalendarGenerator
    {
        public static Dictionary<DateTime, List<CalendarModels.Event>> GenerateCalendar()
        {

            var calendar = GenerateEmptyCalendarForNextSevenDays();
            var jsonEvents = GetEventsFromReddit(calendar);
            PopulateCalendar(jsonEvents, calendar);
            return calendar;
        }

        private static Dictionary<DateTime, List<CalendarModels.Event>> GenerateEmptyCalendarForNextSevenDays()
        {
            var today = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).Date;
            var calendar = Enumerable.Range(0, 7).Select(d => today.AddDays(d)).ToList().ToDictionary(d => d, d => new List<CalendarModels.Event>());
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
}