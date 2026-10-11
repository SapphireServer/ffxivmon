using FFXIVMonReborn.DataModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace FFXIVMonReborn.Scripting
{


    public class ScriptingProvider
    {
        private readonly ScriptOptions scriptOptions;
        private readonly List<Script<object>> scripts = new List<Script<object>>();
        public ScriptingDataStorage DataStorage = new ScriptingDataStorage();

        public ScriptingProvider()
        {
            scriptOptions = ScriptOptions.Default
                .WithReferences(
                    typeof(object).Assembly,
                    typeof(PacketEntry).Assembly)
                .WithImports(
                    "System",
                    "System.Collections.Generic",
                    "FFXIVMonReborn");
        }

        public async Task LoadScriptsAsync(string path)
        {
            if (!Directory.Exists(path)) return;
            string[] files = Directory.GetFiles(path);
            await LoadScriptsAsync(files);
        }

        public async Task LoadScriptsAsync(string[] files)
        {
            DataStorage.Reset();
            scripts.Clear();

            foreach (string filePath in files)
            {
                string contents = await File.ReadAllTextAsync(filePath);

                Script<object> script = CSharpScript.Create(contents, scriptOptions, typeof(PacketEventArgs));

                await Task.Run(() =>
                {
                    var compilation = script.GetCompilation();
                    var diagnostics = compilation.GetDiagnostics();

                    if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        throw new CompilationErrorException("Script syntax error", diagnostics);
                    }
                });

                scripts.Add(script);
            }
        }

        public async Task ExecuteScriptsAsync(object sender, PacketEventArgs eventArgs)
        {
            eventArgs.DataStorage = DataStorage;

            foreach (Script<object> script in scripts)
            {
                await script.RunAsync(eventArgs);
            }
        }
    }

    public class PacketEventArgs : EventArgs
    {
        // Used by event handlers
        public readonly PacketEntry Packet;
        public readonly ExpandoObject PacketObj;
        public readonly LogView Debug;
        public ScriptingDataStorage DataStorage;

        public PacketEventArgs(PacketEntry packet, ExpandoObject packetobj, LogView debugView)
        {
            this.Packet = packet;
            this.PacketObj = packetobj;
            this.Debug = debugView;
        }
    }
}