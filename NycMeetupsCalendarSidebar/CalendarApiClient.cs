using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;


namespace NycMeetupsCalendarSidebar
{
    public class CalendarApiClient
    {
        private readonly string _calendarId;
        private readonly CalendarService _service;

        static string[] Scopes = { CalendarService.Scope.Calendar };
        static string ApplicationName = "rnycmeetups calendar updater";

        public CalendarApiClient(string calendarId, string clientSecretPath, string credentialsPath)
        {
            _calendarId = calendarId;
            var credential = GetApiCredential(clientSecretPath, credentialsPath);
            _service = GetCalendarService(credential);
        }

        public void LoadEventsToCalendar(Dictionary<DateTime, List<CalendarModels.Event>> calendar)
        {
            var events = GetEventsForUpcomingEightDays();
            DeleteEvents(events);
            LoadNewEvents(calendar);
        }

        private void LoadNewEvents(Dictionary<DateTime, List<CalendarModels.Event>> calendar)
        {
            var gcalEvents = GetGoogleCalendarEvents(calendar);
            foreach (var gcalEvent in gcalEvents)
            {
                var insertRequest = _service.Events.Insert(gcalEvent, _calendarId);
                insertRequest.Execute();
            }
        }

        private static List<Event> GetGoogleCalendarEvents(Dictionary<DateTime, List<CalendarModels.Event>> calendar)
        {
            var gcalEvents = new List<Event>();
            foreach (var kvp in calendar)
            {
                var date = kvp.Key.ToString("yyyy-MM-dd");
                foreach (var meetupEvent in kvp.Value)
                {

                    var gcalEvent = new Event
                    {
                        Start = new EventDateTime {Date = date},
                        End = new EventDateTime {Date = date},
                        Summary = meetupEvent.Title,
                        Description = meetupEvent.Url
                    };
                    gcalEvents.Add(gcalEvent);
                }
            }
            return gcalEvents;
        }

        private void DeleteEvents(Events events)
        {
            foreach (var eventItem in events.Items)
            {
                var deleteRequest = _service.Events.Delete(_calendarId, eventItem.Id);
                deleteRequest.Execute();
            }
        }

        private Events GetEventsForUpcomingEightDays()
        {
            var request = _service.Events.List(_calendarId);
            var today = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).Date;
            request.TimeMin = today;
            request.TimeMax = today.AddDays(8);
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = request.Execute();
            return events;
        }

        private static CalendarService GetCalendarService(UserCredential credential)
        {
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            return service;
        }

        private static UserCredential GetApiCredential(string clientSecretPath, string credentialsPath)
        {
            UserCredential credential;
            using (var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credentialsPath, true)).Result;
            }
            return credential;
        }
    }
}