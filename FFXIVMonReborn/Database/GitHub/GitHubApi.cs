using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FFXIVMonReborn.Database.GitHub.Model;
using Newtonsoft.Json;


namespace FFXIVMonReborn.Database.GitHub
{
    public class GitHubApi
    {
        public readonly string Repository;

        public GitHubBranch[] Branches { get; private set; }
        public GitHubTag[] Tags { get; private set; }
        public GitHubCommit[] Commits { get; private set; }

        private HttpClient _client = new HttpClient();
        public static readonly string _cacheFolder = "downloaded";
        private readonly string _apiCacheFile = "ApiCache.json";

        public GitHubApi(string repo)
        {
            this.Repository = repo;

            Directory.CreateDirectory(_cacheFolder);
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("ffxivmon");

            Update();
        }

        private void Update()
        {
            LoadBranches();
            LoadTags();
            LoadCommits();
        }

        private void LoadBranches()
        {
            Branches = GitHubBranch.FromJson(Request($"/repos/{Repository}/branches"));
        }

        private void LoadTags()
        {
            Tags = GitHubTag.FromJson(Request($"/repos/{Repository}/tags"));

            Tags = Tags.OrderBy(tag => decimal.Parse(tag.Name.Substring(1), CultureInfo.InvariantCulture)).ToArray();
        }

        private void LoadCommits()
        {
            // TODO: This is hardcoded to master right now to make version switching obsolete and make it easier to use tagged versions
            Commits = GitHubCommit.FromJson(Request($"/repos/{Repository}/commits?sha=master"));
        }

        private string Request(string endpoint, bool ignoreCache = false)
        {
            var apiCachePath = Path.Combine(_cacheFolder, _apiCacheFile);

            Dictionary<string, string> cache;

            cache = File.Exists(apiCachePath) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(apiCachePath)) : new Dictionary<string, string>(); // Load or Create

            if (cache == null)
                throw new Exception($"ApiCache exists but could not load it. Please delete {Environment.CurrentDirectory + "/downloaded folder and relaunch FFXIVMonReborn."}");

            if (cache.ContainsKey(endpoint) && !ignoreCache)
                return cache[endpoint];

            var task = Task.Run(async () => await _client.GetAsync(new Uri(new Uri("https://api.github.com"), endpoint)));

            if (task.IsFaulted && task.Exception != null)
                throw task.Exception;
            var result = task.Result;

            if (!result.IsSuccessStatusCode)
                throw new Exception("Could not complete Request.");

            var responseResult = Task.Run(result.Content.ReadAsStringAsync).Result;
            cache[endpoint] = responseResult;
            File.WriteAllText(apiCachePath, JsonConvert.SerializeObject(cache));

            return responseResult;
        }

        public void ResetCache()
        {
            Directory.Delete(_cacheFolder, true);
            Directory.CreateDirectory(_cacheFolder);
            Update();
        }

        public string GetContent(string sourceCommitSha, string path, bool ignoreCache = false)
        {
            using (var wc = new WebClient())
            {
                var filePath = Path.Combine(_cacheFolder, sourceCommitSha, path.Substring(1));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                if (File.Exists(filePath) && !ignoreCache)
                    return File.ReadAllText(filePath);

                string content = null;
                try
                {
                    content = wc.DownloadString($"https://raw.githubusercontent.com/{Repository}/{sourceCommitSha}/{path}");
                }
                catch (WebException exc)
                {
                    var response = exc.Response as HttpWebResponse;

                    if (response.StatusCode != HttpStatusCode.NotFound)
                        throw;

                    Debug.WriteLine($"404 getting: {path} for {sourceCommitSha}");
                    return null;
                }

                if (!ignoreCache || !File.Exists(filePath))
                    File.WriteAllText(filePath, content);

                return content;
            }
        }
    }
}
