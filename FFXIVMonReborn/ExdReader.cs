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
                _Exds.ActiveLanguage = SaintCoinach.Ex.Language.English;
            }
            catch (Exception e)
            {
                new ExtendedErrorView(e.Message, "Unable to init EXD data", "FFXIVMon Reborn").ShowDialog();
                return false;
            }
            return true;
        }

        public static T GetExdField<T>(string sheetName, int row, int col)
        {
            if (_Exds == null)
            {
                if (!Init(GamePath))
                    return default(T);
            }
            var sheet = _Exds.GetSheet(sheetName);

            object field = null;
            try
            {
                field = sheet[(int)row][(int)col];
            }
            catch (Exception e)
            {
                try
                {
                    foreach (SaintCoinach.Ex.IRow exRow in sheet)
                    {
                        if (exRow.Key == row)
                        {
                            field = exRow.GetRaw((int)col);
                            break;
                        }
                    }
                }
                catch (Exception ee)
                {
                    System.Diagnostics.Debug.WriteLine(ee.Message);
                }
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            if (field is T)
                return (T)field;

            return default(T);
        }

        public static string GetExdFieldAsString(string sheetName, int row, int col)
        {
            if (_Exds == null)
            {
                if (!Init(GamePath))
                    return null;
            }
            var sheet = _Exds.GetSheet(sheetName);

            object field = null;
            try
            {
                field = sheet[row][col];
            }
            catch (Exception e)
            {
                try
                {
                    foreach (SaintCoinach.Ex.IRow exRow in sheet)
                    {
                        if (exRow.Key == row)
                        {
                            field = exRow.GetRaw(col);
                            break;
                        }
                    }
                }
                catch (Exception ee)
                {
                    System.Diagnostics.Debug.WriteLine(ee.Message);
                }
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            if (field == null)
                return null;

            if (field is IDictionary<string, object>)
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
