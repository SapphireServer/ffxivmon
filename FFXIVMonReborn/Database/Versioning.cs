using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using FFXIVMonReborn.Database.GitHub;
using FFXIVMonReborn.Database.GitHub.Model;
using FFXIVMonReborn.Properties;

namespace FFXIVMonReborn.Database
{
    public class Versioning
    {
        private FileSystemWatcher _watcher;
        
        public event EventHandler LocalDbChanged;

        public GitHubApi Api;

        public Versioning()
        {
            try
            {
                Api = new GitHubApi(Properties.Settings.Default.Repo);
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("[Versioning] Failed to connect to GitHub.", exc.ToString(), "Error")
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
            if(_watcher != null)
                _watcher.EnableRaisingEvents = false;
            
            try
            {
                Api.ResetCache();
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("[Versioning] Failed to reset definitions.", exc.ToString(), "Error")
                    .ShowDialog();
                #if DEBUG
                throw;
                #endif
            }
        }

        public MainDB GetDatabaseForVersion(int version)
        {
            if (Api.Tags.Length > version && version >= 0)
            {
                return new MainDB(Api.GetContent(Api.Tags[version].TagCommit.Sha, "/src/common/Network/PacketDef/Ipcs.h"),
                    Api.GetContent(Api.Tags[version].TagCommit.Sha, "/src/common/Common.h"),
                    Api.GetContent(Api.Tags[version].TagCommit.Sha, "/src/common/Network/PacketDef/Zone/ServerZoneDef.h"),
                    Api.GetContent(Api.Tags[version].TagCommit.Sha, "/src/common/Network/PacketDef/Zone/ClientZoneDef.h"),
                    Api.GetContent(Api.Tags[version].TagCommit.Sha, "/src/common/Network/CommonActorControl.h"));
            }
            else
            {
                return new MainDB(Api.GetContent(Api.Commits[Api.Commits.Length - 1].Sha, "/src/common/Network/PacketDef/Ipcs.h"),
                    Api.GetContent(Api.Commits[Api.Commits.Length - 1].Sha, "/src/common/Common.h"),
                    Api.GetContent(Api.Commits[Api.Commits.Length - 1].Sha, "/src/common/Network/PacketDef/Zone/ServerZoneDef.h"),
                    Api.GetContent(Api.Commits[Api.Commits.Length - 1].Sha, "/src/common/Network/PacketDef/Zone/ClientZoneDef.h"),
                    Api.GetContent(Api.Commits[Api.Commits.Length - 1].Sha, "/src/common/Network/CommonActorControl.h"));
            }
        }

        public string GetVersionInfo(int version)
        {
            if (Api.Tags.Length > version && version >= 0)
            {
                return $"Tagged Version: {version} - {Api.Tags[version].Name} - {Api.Tags[version].TagCommit.Sha}";
            }
            else
            {
                return $"Untagged Version: {Api.Commits[Api.Commits.Length - 1].Sha} - {Api.Commits[Api.Commits.Length - 1].CommitInfo.Message} by {Api.Commits[Api.Commits.Length - 1].CommitInfo.Author.Name}";
            }
        }
    }
}
