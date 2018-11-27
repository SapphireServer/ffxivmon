using Newtonsoft.Json;

namespace FFXIVMonReborn.Database.GitHub.Model
{
    public partial class GitHubTag
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("zipball_url")]
        public string ZipballUrl { get; set; }

        [JsonProperty("tarball_url")]
        public string TarballUrl { get; set; }

        public class GitHubTagCommit
        {
            [JsonProperty("sha")]
            public string Sha { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }
        }
        
        [JsonProperty("commit")]
        public GitHubTagCommit TagCommit { get; set; }
        
        public override string ToString() => Name;
    }

    public partial class GitHubTag
    {
        public static GitHubTag[] FromJson(string json) => JsonConvert.DeserializeObject<GitHubTag[]>(json, new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        });
    }
}
