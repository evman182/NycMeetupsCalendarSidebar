namespace NycMeetupsCalendarSidebar
{
    public class RedditCredentials
    {
        public string Username { get; }
        public string Password { get; }
        public string ClientId { get; }
        public string Secret { get; }

        public RedditCredentials(string username, string password, string clientId, string secret)
        {
            Username = username;
            Password = password;
            ClientId = clientId;
            Secret = secret;
        }
    }
}