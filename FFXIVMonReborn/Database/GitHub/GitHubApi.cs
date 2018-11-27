using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using FFXIVMonReborn.Database.GitHub.Model;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using RestSharp;

namespace FFXIVMonReborn.Database.GitHub
{
    public class GitHubApi
    {
        public readonly string Repository;
        
        public GitHubBranch[] Branches { get; private set; }
        public GitHubTag[] Tags { get; private set; }
        public GitHubCommit[] Commits { get; private set; }

        private RestClient _client = new RestClient("https://api.github.com");
        private readonly string _cacheFolder = "downloaded";
        private readonly string _apiCacheFile = "ApiCache.json";

        public GitHubApi(string repo)
        {
            this.Repository = repo;
            
            Update();

            if (!Directory.Exists(_cacheFolder))
                Directory.CreateDirectory(_cacheFolder);
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
            Commits = GitHubCommit.FromJson(Request($"/repos/{Repository}/commits"));
        }

        private string Request(string endpoint, bool ignoreCache = false)
        {
            var apiCachePath = Path.Combine(_cacheFolder, _apiCacheFile);
            Directory.CreateDirectory(_cacheFolder);
            
            if (!File.Exists(apiCachePath))
                File.Create(apiCachePath);

            var cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(apiCachePath)) ?? new Dictionary<string, string>(); // Load or create

            if (cache.ContainsKey(endpoint) && !ignoreCache)
                return cache[endpoint];
            
            var request = new RestRequest(endpoint);
            var result = _client.Execute(request);

            if (result.ResponseStatus != ResponseStatus.Completed && result.StatusCode != HttpStatusCode.OK)
                throw new Exception("Could not complete Request.");

            if (!ignoreCache)
            {
                cache[endpoint] = result.Content;
                File.WriteAllText(apiCachePath, JsonConvert.SerializeObject(cache));
            }
            
            return result.Content;
        }

        public void ResetCache()
        {
            Directory.Delete(_cacheFolder, true);
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
                
                if(!ignoreCache)
                    File.WriteAllText(filePath, content);

                return content;
            }
        }
    }
}