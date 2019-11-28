using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using FFXIVMonReborn.DataModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace FFXIVMonReborn.Scripting
{
    public class ScriptingProvider // Thanks to oatmeal
    {
        private readonly ScriptOptions scriptOptions;

        private List<Script<object>> scripts = new List<Script<object>>();

        public ScriptingDataStorage DataStorage = new ScriptingDataStorage();

        public ScriptingProvider()
        {
            // Create a custom ScriptOptions instance
            scriptOptions = ScriptOptions.Default
                .WithReferences(
                    typeof(PacketEntry).GetTypeInfo().Assembly)
                .WithImports(
                    "FFXIVMonReborn");
        }

        public void LoadScripts(string[] files)
        {
            DataStorage.Reset();
            scripts.Clear();

            // Get each path
            foreach (string filePath in files)
            {
                // Load the file's contents as text
                string contents = File.ReadAllText(filePath);

                // Create a new Script instance from the contents
                Script<object> script = CSharpScript.Create(contents, scriptOptions, typeof(PacketEventArgs));

                // Compile it
                script.Compile();

                // Add the Script to the List
                scripts.Add(script);
            }
        }

        public void LoadScripts(string path)
        {
            DataStorage.Reset();
            scripts.Clear();

            // Get all files on the path
            string[] files = Directory.GetFiles(path);

            // Get each path
            foreach (string filePath in files)
            {
                // Load the file's contents as text
                string contents = File.ReadAllText(filePath);

                // Create a new Script instance from the contents
                Script<object> script = CSharpScript.Create(contents, scriptOptions, typeof(PacketEventArgs));

                // Compile it
                script.Compile();

                // Add the Script to the List
                scripts.Add(script);
            }
        }

        public void ExecuteScripts(object sender, PacketEventArgs eventArgs)
        {
            eventArgs.DataStorage = DataStorage;

            // Get each script
            foreach (Script<object> script in scripts)
            {
                // Execute it
                script.RunAsync(eventArgs).Wait();
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