using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using FFXIVMonReborn.Database.Commits;
using FFXIVMonReborn.Properties;

namespace FFXIVMonReborn.Database
{
    public class Versioning
    {
        public GithubApiTags[] Versions;
        private GithubApiCommits _latestCommit;

        private FileSystemWatcher _watcher;
        
        public event EventHandler LocalDbChanged;   

        public Versioning()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "downloaded")))
                {
                    Properties.Settings.Default.Repo = Resources.DefaultRepo;
                    Properties.Settings.Default.Save();

                    MessageBox.Show(
                        $"Could not find downloaded database - repo reset to {Resources.DefaultRepo}, change via Options -> Set Repository.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Information);

                    Directory.CreateDirectory("downloaded");
                    Versions = GetTags(Properties.Settings.Default.Repo);
                    _latestCommit = GetLatestCommit();
                }
                else
                {
                    Versions = GetTags(Properties.Settings.Default.Repo);
                    _latestCommit = GetLatestCommit();
                    return;
                }

                foreach (var version in Versions)
                {
                    DownloadDefinitions(version.Commit.Sha);
                }

                DownloadDefinitions(_latestCommit.Sha);
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("[Versioning] Failed to reset definitions.", exc.ToString(), "Error")
                    .ShowDialog();
            }
        }

        public void StartWatcher()
        {
            _watcher = new FileSystemWatcher
            {
                Path = Path.Combine(Environment.CurrentDirectory, "downloaded"),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.h",
                IncludeSubdirectories = true
            };

            _watcher.Changed += WatcherOnChanged;
            _watcher.EnableRaisingEvents = true;
        }

        public void StopWatcher()
        {
            _watcher.EnableRaisingEvents = false;
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            LocalDbChanged?.Invoke(sender, fileSystemEventArgs);
        }

        public void ForceReset()
        {
            _watcher.EnableRaisingEvents = false;
            
            try
            {
                Directory.Delete(Path.Combine(Environment.CurrentDirectory, "downloaded"), true);

                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "downloaded"));
                
                Versions = GetTags(Properties.Settings.Default.Repo);

                foreach (var version in Versions)
                {
                    DownloadDefinitions(version.Commit.Sha);
                }

                _latestCommit = GetLatestCommit();
                DownloadDefinitions(_latestCommit.Sha);
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("[Versioning] Failed to reset definitions.", exc.ToString(), "Error")
                    .ShowDialog();
            }
        }

        public MainDB GetDatabaseForVersion(int version)
        {
            if (Versions.Length > version && version >= 0)
            {
                return new MainDB(GetIpcs(Versions[version].Commit.Sha), GetCommon(Versions[version].Commit.Sha), GetServerZoneDef(Versions[version].Commit.Sha));
            }
            else
            {
                return new MainDB(GetIpcs(_latestCommit.Sha), GetCommon(_latestCommit.Sha), GetServerZoneDef(_latestCommit.Sha));
            }
        }

        public string GetVersionInfo(int version)
        {
            if (Versions.Length > version && version >= 0)
            {
                return $"Tagged Version: {version} - {Versions[version].Name} - {Versions[version].Commit.Sha}";
            }
            else
            {
                return $"Untagged Version: {_latestCommit.Sha} - {_latestCommit.Commit.Message} by {_latestCommit.Commit.Author.Name}";
            }
        }

        private GithubApiCommits GetLatestCommit()
        {
            if (File.Exists(Path.Combine("downloaded", "commits.json")))
                return Commits.GithubApiCommits.FromJson(File.ReadAllText(Path.Combine("downloaded", "commits.json")))[0];

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "XIVMon");
                var result =
                    client.DownloadString($"https://api.github.com/repos/{Properties.Settings.Default.Repo}/commits");
                File.WriteAllText(Path.Combine("downloaded", "commits.json"), result);

                return GithubApiCommits.FromJson(result)[0];
            }
        }

        private string GetCommon(string commit)
        {
            return Util.FileWaitReadAllText(Path.Combine("downloaded", commit, "Common.h"));
        }

        private string GetIpcs(string commit)
        {
            return Util.FileWaitReadAllText(Path.Combine("downloaded", commit, "Ipcs.h"));
        }

        private string GetServerZoneDef(string commit)
        {
            return Util.FileWaitReadAllText(Path.Combine("downloaded", commit, "ServerZoneDef.h"));
        }

        private void DownloadDefinitions(string commit)
        {
            try
            {
                DownloadFile(commit, "/src/common/Network/PacketDef/Ipcs.h", "Ipcs.h");
                DownloadFile(commit, "/src/common/Common.h", "Common.h");
                DownloadFile(commit, "/src/common/Network/PacketDef/Zone/ServerZoneDef.h",
                    "ServerZoneDef.h");
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"[Versioning] Could not download files.\n\n{exc}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void DownloadFile(string commit, string path, string fileName)
        {
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "downloaded", commit)))
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "downloaded", commit));
                

            using (WebClient client = new WebClient())
            {
                Debug.WriteLine($"Downloading https://raw.githubusercontent.com/{Properties.Settings.Default.Repo}/{commit}{path}");
                client.DownloadFile($"https://raw.githubusercontent.com/{Properties.Settings.Default.Repo}/{commit}{path}", Path.Combine(Environment.CurrentDirectory, "downloaded", commit, fileName));
            }
        }


        private static GithubApiTags[] GetTags(string repo)
        {
            if (File.Exists(Path.Combine("downloaded", "tags.json")))
                return GithubApiTags.FromJson(File.ReadAllText(Path.Combine("downloaded", "tags.json")));

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "XIVMon");

                string result = client.DownloadString($"https://api.github.com/repos/{repo}/tags");

                File.WriteAllText(Path.Combine("downloaded", "tags.json"), result);

                return GithubApiTags.FromJson(result);
            }
        }
    }
}
