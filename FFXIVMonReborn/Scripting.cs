using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace FFXIVMonReborn
{
    public class Scripting // Thanks to oatmeal
    {
        private readonly ScriptOptions scriptOptions;

        private List<Script<object>> scripts = new List<Script<object>>();

        public Scripting()
        {
            // Create a custom ScriptOptions instance
            scriptOptions = ScriptOptions.Default
                .WithReferences(
                    typeof(PacketListItem).GetTypeInfo().Assembly)
                .WithImports(
                    "FFXIVMonReborn");
        }

        public void LoadScripts(string[] files)
        {
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
        public readonly PacketListItem Packet;
        public readonly ExpandoObject PacketObj;
        public readonly ScriptDebugView Debug;

        public PacketEventArgs(PacketListItem packet, ExpandoObject packetobj, ScriptDebugView debugView)
        {
            this.Packet = packet;
            this.PacketObj = packetobj;
            this.Debug = debugView;
        }
    }
}