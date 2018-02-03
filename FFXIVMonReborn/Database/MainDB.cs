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
    public class MainDB
    {
        private Dictionary<int, Tuple<string, string>> ServerLobbyIpcType;
        private Dictionary<int, Tuple<string, string>> ClientLobbyIpcType;

        private Dictionary<int, Tuple<string, string>> ServerZoneIpcType = new Dictionary<int, Tuple<string, string>>();
        private Dictionary<int, Tuple<string, string>> ClientZoneIpcType = new Dictionary<int, Tuple<string, string>>();

        private Dictionary<int, Tuple<string, string>> ActorControlType = new Dictionary<int, Tuple<string, string>>();

        private Dictionary<int, string> ServerZoneStruct = new Dictionary<int, string>();

        private string _ipcsString, _commonString, _serverZoneDefString;

        public MainDB(string ipcsString, string commonString, string serverZoneDefString)
        {
            _ipcsString = ipcsString;
            _commonString = commonString;
            _serverZoneDefString = serverZoneDefString;

            Reload();
        }

        public bool Reload()
        {
            ServerZoneIpcType.Clear();
            ClientZoneIpcType.Clear();;
            ActorControlType.Clear();
            ServerZoneStruct.Clear();

            try
            {
                ParseIpcs(_ipcsString);
                ParseCommon(_commonString);
                ParseServerZoneStructs(_serverZoneDefString);
                return true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"[Database] Could not parse files.\n\n{exc}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            Regex r = new Regex($"({enumName})");
            Match m = r.Match(data);
            if (m.Success)
            {
                var lineNumber = data.Take(m.Index).Count(c => c == '\n') + 1;
                int at = 1;
                while (true)
                {
                    var line = lines[lineNumber + at];

                    if (line.Contains("}"))
                        break;

                    if (line == "")
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
                        MessageBox.Show(
                            $"[Database] Duplicate Entry! Could not add {name} - {opcode.ToString("X4")}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    at++;
                }
            }

            return output;
        }

        private void ParseServerZoneStructs(string data)
        {
            var lines = Regex.Split(data, "\r\n|\r|\n");

            foreach (var entry in ServerZoneIpcType)
            {
                Regex r = new Regex($"({entry.Value.Item1})");
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
                    ServerZoneStruct.Add(entry.Key, structText);
                }
            }

            Debug.WriteLine("Parsed ServerZoneStructs: " + ServerZoneStruct.Count);
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
