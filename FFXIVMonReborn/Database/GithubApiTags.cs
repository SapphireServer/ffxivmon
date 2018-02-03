using Newtonsoft.Json;

namespace FFXIVMonReborn.Database
{
    public partial class GithubApiTags
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("zipball_url")]
        public string ZipballUrl { get; set; }

        [JsonProperty("tarball_url")]
        public string TarballUrl { get; set; }

        [JsonProperty("commit")]
        public Commit Commit { get; set; }
    }

    public partial class Commit
    {
        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public partial class GithubApiTags
    {
        public static GithubApiTags[] FromJson(string json) => JsonConvert.DeserializeObject<GithubApiTags[]>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this GithubApiTags[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
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
