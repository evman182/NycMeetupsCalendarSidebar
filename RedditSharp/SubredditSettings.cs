using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedditSharp.Things;

namespace RedditSharp
{
    public class SubredditSettings
    {
        private const string SiteAdminUrl = "/api/site_admin";
        private const string DeleteHeaderImageUrl = "/api/delete_sr_header";

        private Reddit Reddit { get; set; }
        private IWebAgent WebAgent { get; set; }

        [JsonIgnore]
        public Subreddit Subreddit { get; set; }

        public SubredditSettings(Reddit reddit, Subreddit subreddit, IWebAgent webAgent)
        {
            Subreddit = subreddit;
            Reddit = reddit;
            WebAgent = webAgent;
            // Default settings, for use when reduced information is given
            AllowAsDefault = true;
            Domain = null;
            Sidebar = string.Empty;
            Language = "en";
            Title = Subreddit.DisplayName;
            WikiEditKarma = 100;
            WikiEditAge = 10;
            UseDomainCss = false;
            UseDomainSidebar = false;
            HeaderHoverText = string.Empty;
            NSFW = false;
            PublicDescription = string.Empty;
            WikiEditMode = WikiEditMode.None;
            SubredditType = SubredditType.Public;
            ShowThumbnails = true;
            ContentOptions = ContentOptions.All;
            SpamFilter = new SpamFilterSettings();
        }

        public SubredditSettings(Subreddit subreddit, Reddit reddit, JObject json, IWebAgent webAgent) : this(reddit, subreddit, webAgent)
        {
            var data = json["data"];
            AllowAsDefault = data["default_set"].ValueOrDefault<bool>();
            Domain = data["domain"].ValueOrDefault<string>();
            Sidebar = HttpUtility.HtmlDecode(data["description"].ValueOrDefault<string>() ?? string.Empty);
            Language = data["language"].ValueOrDefault<string>();
            Title = data["title"].ValueOrDefault<string>();
            SubmitText = data["submit_text"].ValueOrDefault<string>();
            SubmitLinkLabel = data["submit_link_label"].ValueOrDefault<string>();
            SubmitTextLabel = data["submit_text_label"].ValueOrDefault<string>();
            CollapseDeletedComments = data["collapse_deleted_comments"].ValueOrDefault<bool>();
            ExcludeBannedModqueue = data["exclude_banned_modqueue"].ValueOrDefault<bool>(); 
            WikiEditKarma = data["wiki_edit_karma"].ValueOrDefault<int>();
            HideAds = data["hide_ads"].ValueOrDefault<bool>();
            IsTrafficPublic = data["public_traffic"].ValueOrDefault<bool>();
            UseDomainCss = data["domain_css"].ValueOrDefault<bool>();
            UseDomainSidebar = data["domain_sidebar"].ValueOrDefault<bool>();
            HeaderHoverText = data["header_hover_text"].ValueOrDefault<string>();
            NSFW = data["over_18"].ValueOrDefault<bool>();
            PublicDescription = HttpUtility.HtmlDecode(data["public_description"].ValueOrDefault<string>() ?? string.Empty);
            SpamFilter = new SpamFilterSettings
            {
                LinkPostStrength = GetSpamFilterStrength(data["spam_links"].ValueOrDefault<string>()),
                SelfPostStrength = GetSpamFilterStrength(data["spam_selfposts"].ValueOrDefault<string>()),
                CommentStrength = GetSpamFilterStrength(data["spam_comments"].ValueOrDefault<string>())
            };
            if (data["wikimode"] != null)
            {
                var wikiMode = data["wikimode"].ValueOrDefault<string>();
                switch (wikiMode)
                {
                    case "disabled":
                        WikiEditMode = WikiEditMode.None;
                        break;
                    case "modonly":
                        WikiEditMode = WikiEditMode.Moderators;
                        break;
                    case "anyone":
                        WikiEditMode = WikiEditMode.All;
                        break;
                }
            }
            if (data["subreddit_type"] != null)
            {
                var type = data["subreddit_type"].ValueOrDefault<string>();
                switch (type)
                {
                    case "public":
                        SubredditType = SubredditType.Public;
                        break;
                    case "private":
                        SubredditType = SubredditType.Private;
                        break;
                    case "restricted":
                        SubredditType = SubredditType.Restricted;
                        break;
                    case "gold_restricted":
                        SubredditType = SubredditType.GoldRestricted;
                        break;
                    case "archived":
                        SubredditType = SubredditType.Archived;
                        break;
                    case "gold_only":
                        SubredditType = SubredditType.GoldOnly;
                        break;
                    case "employees_only":
                        SubredditType = SubredditType.EmployeesOnly;
                        break;
                }
            }
            ShowThumbnails = data["show_media"].ValueOrDefault<bool>();
            ShowMediaPreviews = data["show_media_preview"].ValueOrDefault<bool>();
            AllowImages = data["allow_images"].ValueOrDefault<bool>();
            MinutesToHideCommentScores = data["comment_score_hide_mins"].ValueOrDefault<int>(); 
            WikiEditAge = data["wiki_edit_age"].ValueOrDefault<int>();
            if (data["content_options"] != null)
            {
                var contentOptions = data["content_options"].ValueOrDefault<string>();
                switch (contentOptions)
                {
                    case "any":
                        ContentOptions = ContentOptions.All;
                        break;
                    case "link":
                        ContentOptions = ContentOptions.LinkOnly;
                        break;
                    case "self":
                        ContentOptions = ContentOptions.SelfOnly;
                        break;
                }
            }
            if (data["suggested_comment_sort"] != null)
            {
                var suggestedCommentSort = data["suggested_comment_sort"].ValueOrDefault<string>();
                switch (suggestedCommentSort)
                {
                    case "confidence":
                        SuggestedCommentSort = CommentSorts.Confidence;
                        break;

                    case "top":
                        SuggestedCommentSort = CommentSorts.Top;
                        break;

                    case "new":
                        SuggestedCommentSort = CommentSorts.New;
                        break;

                    case "controversial":
                        SuggestedCommentSort = CommentSorts.Controversial;
                        break;

                    case "old":
                        SuggestedCommentSort = CommentSorts.Old;
                        break;

                    case "random":
                        SuggestedCommentSort = CommentSorts.Random;
                        break;

                    case "qa":
                        SuggestedCommentSort = CommentSorts.Qa;
                        break;
                }
            }
        }

        public bool AllowAsDefault { get; set; }
        public string Domain { get; set; }
        public string Sidebar { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public int WikiEditKarma { get; set; }
        public string SubmitText { get; set; }
        public string SubmitLinkLabel { get; set; }
        public string SubmitTextLabel { get; set; }
        public bool CollapseDeletedComments { get; set; }
        public CommentSorts SuggestedCommentSort { get; set; }
        public bool HideAds { get; set; }
        public bool IsTrafficPublic { get; set; }
        public bool ExcludeBannedModqueue { get; set; }
        public bool UseDomainCss { get; set; }
        public bool UseDomainSidebar { get; set; }
        public string HeaderHoverText { get; set; }
        public bool NSFW { get; set; }
        public string PublicDescription { get; set; }
        public WikiEditMode WikiEditMode { get; set; }
        public SubredditType SubredditType { get; set; }
        public bool ShowThumbnails { get; set; }
        public bool ShowMediaPreviews { get; set; }
        public bool AllowImages { get; set; }
        public int MinutesToHideCommentScores { get; set; }
        public int WikiEditAge { get; set; }
        public ContentOptions ContentOptions { get; set; }
        public SpamFilterSettings SpamFilter { get; set; }

        public void UpdateSettings()
        {
            var request = WebAgent.CreatePost(SiteAdminUrl);
            var stream = request.GetRequestStream();
            string link_type;
            string type;
            string wikimode;
            string suggested_comment_sort;

            switch (ContentOptions)
            {
                case ContentOptions.All:
                    link_type = "any";
                    break;
                case ContentOptions.LinkOnly:
                    link_type = "link";
                    break;
                default:
                    link_type = "self";
                    break;
            }
            switch (SubredditType)
            {
                case SubredditType.Public:
                    type = "public";
                    break;
                case SubredditType.Private:
                    type = "private";
                    break;
                case SubredditType.GoldRestricted:
                    type = "gold_restricted";
                    break;
                case SubredditType.Archived:
                    type = "archived";
                    break;
                case SubredditType.GoldOnly:
                    type = "gold_only";
                    break;
                case SubredditType.EmployeesOnly:
                    type = "employees_only";
                    break;
                default:
                    type = "restricted";
                    break;
            }
            switch (WikiEditMode)
            {
                case WikiEditMode.All:
                    wikimode = "anyone";
                    break;
                case WikiEditMode.Moderators:
                    wikimode = "modonly";
                    break;
                default:
                    wikimode = "disabled";
                    break;
            }
            switch (SuggestedCommentSort)
            {
                case CommentSorts.Confidence:
                    suggested_comment_sort = "confidence";
                    break;

                case CommentSorts.Top:
                    suggested_comment_sort = "top";
                    break;

                case CommentSorts.New:
                    suggested_comment_sort = "new";
                    break;

                case CommentSorts.Controversial:
                    suggested_comment_sort = "controversial";
                    break;

                case CommentSorts.Old:
                    suggested_comment_sort = "old";
                    break;

                case CommentSorts.Random:
                    suggested_comment_sort = "random";
                    break;

                case CommentSorts.Qa:
                    suggested_comment_sort = "qa";
                    break;
                default:
                    suggested_comment_sort = string.Empty;
                    break;

            }

            WebAgent.WritePostBody(stream, new
            {
                allow_top = AllowAsDefault,
                allow_images = AllowImages,
                collapse_deleted_comments = CollapseDeletedComments,
                comment_score_hide_mins = MinutesToHideCommentScores,
                description = Sidebar,
                exclude_banned_modqueue = ExcludeBannedModqueue,
                domain = Domain,
                hide_ads = HideAds,
                lang = Language,
                link_type,
                over_18 = NSFW,
                public_description = PublicDescription,
                public_traffic = IsTrafficPublic,
                show_media = ShowThumbnails,
                show_media_preview = ShowMediaPreviews,
                sr = Subreddit.FullName,
                submit_link_label = SubmitLinkLabel,
                submit_text = SubmitText,
                submit_text_label = SubmitTextLabel,
                suggested_comment_sort,
                title = Title,
                type,
                uh = Reddit.User.Modhash,
                wiki_edit_age = WikiEditAge,
                wiki_edit_karma = WikiEditKarma,
                wikimode,
                spam_links = SpamFilter == null ? null : SpamFilter.LinkPostStrength.ToString().ToLowerInvariant(),
                spam_selfposts = SpamFilter == null ? null : SpamFilter.SelfPostStrength.ToString().ToLowerInvariant(),
                spam_comments = SpamFilter == null ? null : SpamFilter.CommentStrength.ToString().ToLowerInvariant(),
                api_type = "json"
            }, "header-title", HeaderHoverText);
            stream.Close();
            var response = request.GetResponse();
            var data = WebAgent.GetResponseString(response.GetResponseStream());
        }

        /// <summary>
        /// Resets the subreddit's header image to the Reddit logo
        /// </summary>
        public void ResetHeaderImage()
        {
            var request = WebAgent.CreatePost(DeleteHeaderImageUrl);
            var stream = request.GetRequestStream();
            WebAgent.WritePostBody(stream, new
            {
                uh = Reddit.User.Modhash,
                r = Subreddit.Name
            });
            stream.Close();
            var response = request.GetResponse();
            var data = WebAgent.GetResponseString(response.GetResponseStream());
        }

        private SpamFilterStrength GetSpamFilterStrength(string rawValue)
        {
            switch(rawValue)
            {
                case "low":
                    return SpamFilterStrength.Low;
                case "high":
                    return SpamFilterStrength.High;
                case "all":
                    return SpamFilterStrength.All;
                default:
                    return SpamFilterStrength.High;
            }
        }
    }

    public enum WikiEditMode
    {
        None,
        Moderators,
        All
    }

    public enum SubredditType
    {
        Public,
        Restricted,
        Private,
        GoldRestricted,
        Archived,
        GoldOnly,
        EmployeesOnly
    }

    public enum ContentOptions
    {
        All,
        LinkOnly,
        SelfOnly
    }

    public enum SpamFilterStrength
    {
        Low,
        High,
        All
    }

    public enum CommentSorts
    {
        Confidence,
        Top,
        New,
        Controversial,
        Old,
        Random,
        Qa
    }
}
