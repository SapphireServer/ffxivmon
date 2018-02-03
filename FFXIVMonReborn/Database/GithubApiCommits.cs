// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using FFXIV;
//
//    var data = GithubApiCommits.FromJson(jsonString);

namespace FFXIVMonReborn.Database.Commits
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public partial class GithubApiCommits
    {
        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("commit")]
        public Commit Commit { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("comments_url")]
        public string CommentsUrl { get; set; }

        [JsonProperty("author")]
        public GithubApiCommitAuthor Author { get; set; }

        [JsonProperty("committer")]
        public GithubApiCommitAuthor Committer { get; set; }

        [JsonProperty("parents")]
        public Parent[] Parents { get; set; }
    }

    public partial class GithubApiCommitAuthor
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty("gravatar_id")]
        public string GravatarId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("followers_url")]
        public string FollowersUrl { get; set; }

        [JsonProperty("following_url")]
        public string FollowingUrl { get; set; }

        [JsonProperty("gists_url")]
        public string GistsUrl { get; set; }

        [JsonProperty("starred_url")]
        public string StarredUrl { get; set; }

        [JsonProperty("subscriptions_url")]
        public string SubscriptionsUrl { get; set; }

        [JsonProperty("organizations_url")]
        public string OrganizationsUrl { get; set; }

        [JsonProperty("repos_url")]
        public string ReposUrl { get; set; }

        [JsonProperty("events_url")]
        public string EventsUrl { get; set; }

        [JsonProperty("received_events_url")]
        public string ReceivedEventsUrl { get; set; }

        [JsonProperty("type")]
        public string PurpleType { get; set; }

        [JsonProperty("site_admin")]
        public bool SiteAdmin { get; set; }
    }

    public partial class Commit
    {
        [JsonProperty("author")]
        public CommitAuthor Author { get; set; }

        [JsonProperty("committer")]
        public CommitAuthor Committer { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("tree")]
        public Tree Tree { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("comment_count")]
        public long CommentCount { get; set; }

        [JsonProperty("verification")]
        public Verification Verification { get; set; }
    }

    public partial class CommitAuthor
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("date")]
        public System.DateTime Date { get; set; }
    }

    public partial class Tree
    {
        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public partial class Verification
    {
        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }
    }

    public partial class Parent
    {
        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
    }

    public partial class GithubApiCommits
    {
        public static GithubApiCommits[] FromJson(string json) => JsonConvert.DeserializeObject<GithubApiCommits[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this GithubApiCommits[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
