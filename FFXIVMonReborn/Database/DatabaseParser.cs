using FFXIVMonReborn.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace FFXIVMonReborn.Database
{
    public class DatabaseParser
    {
        private Dictionary<int, Tuple<string, string>> ServerLobbyIpcType;
        private Dictionary<int, Tuple<string, string>> ClientLobbyIpcType;

        public Dictionary<int, Tuple<string, string>> ServerZoneIpcType = new Dictionary<int, Tuple<string, string>>();
        private Dictionary<int, Tuple<string, string>> ClientZoneIpcType = new Dictionary<int, Tuple<string, string>>();

        private Dictionary<int, Tuple<string, string>> ActorControlType = new Dictionary<int, Tuple<string, string>>();

        public Dictionary<int, string> ServerZoneStruct = new Dictionary<int, string>();
        public Dictionary<int, string> ClientZoneStruct = new Dictionary<int, string>();

        private string _ipcsString, _commonString, _serverZoneDefString, _clientZoneDefString, _commonActorControlString;

        public bool HasClientDefs = true;
        public bool HasCommonActorControl = true;

        public DatabaseParser(string ipcsString, string commonString, string serverZoneDefString, string clientZoneDefString, string commonActorControlString)
        {
            _ipcsString = ipcsString;
            _commonString = commonString;
            _serverZoneDefString = serverZoneDefString;

            if (clientZoneDefString != null)
                _clientZoneDefString = clientZoneDefString;
            else
                HasClientDefs = false;

            if (commonActorControlString != null)
                _commonActorControlString = commonActorControlString;
            else
                HasCommonActorControl = false;

            Reload();
        }

        public bool Reload()
        {
            ServerZoneIpcType.Clear();
            ClientZoneIpcType.Clear();
            ActorControlType.Clear();
            ServerZoneStruct.Clear();
            ClientZoneStruct.Clear();

            try
            {
                ParseIpcs(_ipcsString);
                ParseCommon(_commonString);
                ParseStructs(_serverZoneDefString, ref ServerZoneStruct, ServerZoneIpcType);

                if(HasClientDefs)
                    ParseStructs(_clientZoneDefString, ref ClientZoneStruct, ClientZoneIpcType);
                
                if(HasCommonActorControl)
                    ParseCommon(_commonActorControlString);

                return true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"[Database] Could not parse files.\n\n{exc}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                 
                #if DEBUG
                throw;
                #endif
                
                return false;
            }
        }

        public string GetServerZoneStruct(int opcode)
        {
            string structText;
            if (ServerZoneStruct.TryGetValue(opcode, out structText))
                return structText;
            else
                return null;
        }

        public string GetClientZoneStruct(int opcode)
        {
            string structText;
            if (ClientZoneStruct.TryGetValue(opcode, out structText))
                return structText;
            else
                return null;
        }

        public string GetActorControlTypeName(int opcode)
        {
            Tuple<string, string> output;
            if (ActorControlType.TryGetValue(opcode, out output))
                return $"ActorControl({output.Item1}:{opcode.ToString("X4")})";
            else
                return $"ActorControl(Unk:{opcode.ToString("X4")})";
        }

        public string GetServerZoneOpName(int opcode)
        {
            Tuple<string, string> output;
            if (ServerZoneIpcType.TryGetValue(opcode, out output))
                return output.Item1;
            else
                return "Unknown";
        }
                                
        public string GetServerZoneOpComment(int opcode)
        {
            Tuple<string, string> output;
            if (ServerZoneIpcType.TryGetValue(opcode, out output))
                return output.Item2;
            else
                return " - ";
        }

        public string GetClientZoneOpName(int opcode)
        {
            Tuple<string, string> output;
            if (ClientZoneIpcType.TryGetValue(opcode, out output))
                return output.Item1;
            else
                return "Unknown";
        }

        public string GetClientZoneOpComment(int opcode)
        {
            Tuple<string, string> output;
            if (ClientZoneIpcType.TryGetValue(opcode, out output))
                return output.Item2;
            else
                return " - ";
        }

        private Dictionary<int, Tuple<string, string>> ParseEnum(string data, string enumName)
        {
            Dictionary<int, Tuple<string, string>> output = new Dictionary<int, Tuple<string, string>>();

            var lines = Regex.Split(data, "\r\n|\r|\n");

            bool inComment = false;

            Regex r = new Regex($"({enumName})");
            Match m = r.Match(data);
            if (m.Success)
            {
                var lineNumber = data.Take(m.Index).Count(c => c == '\n') + 1;
                int at = 1;
                while (true)
                {
                    
                    var line = lines[lineNumber + at];

                    if (inComment)
                    {
                        if (line.Contains("*/"))
                            inComment = false;

                        at++;
                        continue;
                    }

                    if (line.Contains("/*") && line.Contains("*/"))
                    {
                        at++;
                        continue;
                    }
                        

                    if (line.Contains("/*"))
                    {
                        inComment = true;

                        at++;
                        continue;
                    }

                    if (line.Contains("}"))
                        break;
                    
                    if (line.Contains("{"))
                    {
                        at++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        at++;
                        continue;
                    }

                    int pos = 0;
                    while (!Char.IsLetter(line[pos]))
                    {
                        if (line[pos] == '/')
                            break;
                        ++pos;
                    }

                    var tempLine = line.Substring(pos);

                    if (tempLine[0] == '/')
                    {
                        at++;
                        continue;
                    }

                    var name = tempLine.Substring(0, tempLine.IndexOf(" "));
                    Debug.WriteLine(name);

                    int numStart = tempLine.IndexOf("=") + 4;
                    string num = "";

                    while (true)
                    {
                        if (tempLine[numStart] != ',')
                            num += tempLine[numStart];
                        else
                            break;

                        if (numStart == tempLine.Length - 1)
                            break;

                        numStart++;
                    }

                    int opcode = int.Parse(num, NumberStyles.HexNumber);

                    Debug.WriteLine(opcode.ToString("X4"));

                    string comment = "";
                    if (tempLine.Contains("//"))
                    {
                        pos = tempLine.LastIndexOf("//");
                        comment = tempLine.Substring(pos + 3);
                    }

                    Debug.WriteLine(comment);

                    try
                    {
                        output.Add(opcode, new Tuple<string, string>(name, comment));
                    }
                    catch (ArgumentException)
                    {
                        LogView.Instance?.WriteLine($"Duplicate Entry! Could not add {name} - {opcode.ToString("X4")}\n");
                    }

                    at++;
                }
            }

            return output;
        }

        private void ParseStructs(string data, ref Dictionary<int, string> dict, Dictionary<int, Tuple<string, string>> ipcTypes)
        {
            var lines = Regex.Split(data, "\r\n|\r|\n");

            foreach (var entry in ipcTypes)
            {
                Debug.WriteLine($"Looking for struct for {entry.Value.Item1}");

                Regex r = new Regex($"(FFXIVIpcBasePacket< ?{entry.Value.Item1} ?>)");
                Match m = r.Match(data);
                if (m.Success)
                {
                    var lineNumber = data.Take(m.Index).Count(c => c == '\n');

                    string structText = "";
                    while (true)
                    {
                        if (lines[lineNumber].IndexOf("};") != -1)
                            break;
                        structText += "\n" + lines[lineNumber];
                        lineNumber++;
                    }
                    structText += "\n" + lines[lineNumber];
                    Debug.WriteLine("\n\n" + structText);
                    dict.Add(entry.Key, structText);
                }
            }

            Debug.WriteLine("Parsed structs: " + dict.Count);
        }

        private void ParseCommon(string data)
        {
            ActorControlType = ParseEnum(data, "ActorControlType");
            Debug.WriteLine("Parsed ActorControlType: " + ActorControlType.Count);
        }

        private void ParseIpcs(string data)
        {
            ServerZoneIpcType = ParseEnum(data, "ServerZoneIpcType");
            ClientZoneIpcType = ParseEnum(data, "ClientZoneIpcType");

            Debug.WriteLine("Parsed ServerZoneIpcType: " + ServerZoneIpcType.Count);
            Debug.WriteLine("Parsed ClientZoneIpcType: " + ClientZoneIpcType.Count);
        }
    }
}
