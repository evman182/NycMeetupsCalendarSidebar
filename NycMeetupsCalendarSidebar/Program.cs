using System.Configuration;

namespace NycMeetupsCalendarSidebar
{
    class Program
    {
        private static readonly string Username = ConfigurationManager.AppSettings["username"];
        private static readonly string Password = ConfigurationManager.AppSettings["password"];
        private static readonly string ClientId = ConfigurationManager.AppSettings["ClientId"];
        private static readonly string Secret   = ConfigurationManager.AppSettings["secret"];
        private static readonly string GoogleCalendarId = ConfigurationManager.AppSettings["calendarId"];
        private static readonly string GoogleClientSecretPath = ConfigurationManager.AppSettings["GoogleClientSecretPath"];
        private static readonly string GoogleCredentialsPath = ConfigurationManager.AppSettings["GoogleCredentialsPath"];

        static void Main()
        {
            var calendar = MeetupCalendarGenerator.GenerateCalendar();

            var calendarApiClient = new CalendarApiClient(GoogleCalendarId, GoogleClientSecretPath, GoogleCredentialsPath);
            calendarApiClient.LoadEventsToCalendar(calendar);

            var redditCredentials = new RedditCredentials(Username, Password, ClientId, Secret);
            var redditUpdater = new RedditSidebarUpdater(redditCredentials);
            var tableText = redditUpdater.GetRedditTableText(calendar);
            redditUpdater.UpdateSidebar(tableText);
        }
    }
}
