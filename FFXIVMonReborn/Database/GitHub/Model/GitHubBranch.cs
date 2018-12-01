using System;
using Newtonsoft.Json;

namespace FFXIVMonReborn.Database.GitHub.Model
{
    public partial class GitHubBranch
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("commit")]
        public GitHubCommit.GitHubCommitInfo.GitHubCommitTree Commit { get; set; }

        [JsonProperty("protected")]
        public bool Protected { get; set; }

        [JsonProperty("protection_url")]
        public Uri ProtectionUrl { get; set; }

        public override string ToString() => $"{Name} - {Commit.Sha}";
    }

    public partial class GitHubBranch
    {
        public static GitHubBranch[] FromJson(string json) => JsonConvert.DeserializeObject<GitHubBranch[]>(json, new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        });
    }
}