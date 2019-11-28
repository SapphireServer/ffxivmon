using System;
using System.IO;
using FFXIVMonReborn.Database.GitHub;
using FFXIVMonReborn.Database.GitHub.Model;
using Xunit;
using Xunit.Sdk;

namespace FFXIVMonReborn.Tests
{
    public class GitHubTestFixture : IDisposable
    {
        private const string TestRepository = "SapphireServer/Sapphire";
        
        public GitHubApi Api { get; }

        public GitHubTestFixture()
        {
            Api = new GitHubApi(TestRepository);
        }

        public void Dispose()
        {
            Directory.Delete("downloaded", true);
        }
    }
    
    public class GitHubTests : IClassFixture<GitHubTestFixture>
    {

        private readonly GitHubApi _api;

        public GitHubTests(GitHubTestFixture fixture)
        {
            _api = fixture.Api;
        }
        
        [Fact]
        public void TestResetCache()
        {
            var ex = Record.Exception(() => _api.ResetCache());
            
            Assert.Null(ex);
        }
        
        [Fact]
        public void TestTags()
        {
            Assert.NotNull(_api.Tags);
            
            Assert.NotEmpty(_api.Tags);
        }
        
        [Fact]
        public void TestBranches()
        {
            Assert.NotNull(_api.Branches);
            
            Assert.NotEmpty(_api.Branches);
        }
        
        [Fact]
        public void TestCommits()
        {
            Assert.NotNull(_api.Commits);
            
            Assert.NotEmpty(_api.Commits);
        }
        
        [Fact]
        public void TestGetContent()
        {
            var content = _api.GetContent(_api.Commits[0].Sha, "CMakeLists.txt");
            
            Assert.NotNull(content);
        }
    }
}