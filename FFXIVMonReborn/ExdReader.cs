using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaintCoinach;

namespace FFXIVMonReborn
{
    public static class ExdReader
    {
        public static string GamePath { get; private set; }
        private static ARealmReversed _ARealmReversed;
        private static SaintCoinach.Ex.ExCollection _Exds;

        public static bool Init(string path)
        {
            try
            {
                GamePath = path;
                _ARealmReversed = new SaintCoinach.ARealmReversed(path, SaintCoinach.Ex.Language.English);
                _Exds = new SaintCoinach.Ex.ExCollection(_ARealmReversed.Packs);
            }
            catch (Exception e)
            {
                new ExtendedErrorView(e.Message, "Unable to init EXD data", "FFXIVMon Reborn").ShowDialog();
                return false;
            }
            return true;
        }

        public static string GetExdField(string sheetName, int row, int col)
        {
            if (_Exds == null)
            {
                if (!Init(GamePath))
                    return null;
            }
            var sheet = _Exds.GetSheet(sheetName);
            if (!sheet.ContainsRow(row))
                return null;

            var field = sheet[row][col];

            if (field is UInt32)
                return ((UInt32)field).ToString();
            else if (field is Boolean)
                return ((Boolean)field).ToString();
            else if (field is Byte)
                return ((Byte)field).ToString();
            else if (field is SByte)
                return ((SByte
               )field).ToString();
            else if (field is Int16)
                return ((Int16)field).ToString();
            else if (field is Int32)
                return ((Int32)field).ToString();
            else if (field is Int64)
                return ((Int64)field).ToString();
            else if (field is UInt16)
                return ((UInt16)field).ToString();
            else if (field is UInt32)
                return ((UInt32)field).ToString();
            else if (field is UInt64)
                return ((UInt64)field).ToString();
            else if (field is Single)
                return ((Single)field).ToString();
            else if (field is Double)
                return ((Double)field).ToString();
            else if (field is IDictionary<string, object>)
            {
                // taken from SaintCoinach
                string s;
                s = ",\"";
                var isFirst = true;
                foreach (var kvp in (IDictionary<string, object>)field)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        s += ",";
                    s += string.Format("[{0},", kvp.Key);
                    if (kvp.Value != null)
                        s += (kvp.Value.ToString().Replace("\"", "\"\""));
                    s += "]";
                }
                s += "\"";
                return s;
            }
            return field.ToString();
        }
    }
}
