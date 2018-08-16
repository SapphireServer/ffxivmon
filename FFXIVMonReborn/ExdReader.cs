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
        public static ARealmReversed ARealmReversed { get; private set; }

        #region Init
        public static void Init(string path)
        {
            GamePath = path;
            ARealmReversed = new SaintCoinach.ARealmReversed(path, SaintCoinach.Ex.Language.English);
        }
        #endregion

        #region Helpers
        public static SaintCoinach.Ex.ISheet GetSheet(string sheetName)
        {
            if (ARealmReversed == null)
            {
                Init(GamePath);
            }
            return ARealmReversed.GameData.GetSheet(sheetName);
        }

        private static int _GetColIndexByName(SaintCoinach.Ex.ISheet sheet, string colName)
        {
            var rSheet = sheet as SaintCoinach.Ex.Relational.IRelationalSheet;
            if (rSheet != null)
            {
                foreach (var exCol in rSheet.Header.Columns)
                {
                    if (exCol.Name == colName)
                    {
                        return exCol.Index;
                    }
                }
            }
            new ExtendedErrorView($"Unable to find column {colName} in EXD sheet {sheet.Name}", "", "FFXIVMon Reborn");
            System.Diagnostics.Debug.WriteLine($"Unable to find column {colName} in EXD sheet {sheet.Name}");
            return -1;
        }

        private static object _GetExdField(SaintCoinach.Ex.ISheet sheet, int row, string colName)
        {
            int col = _GetColIndexByName(sheet, colName);
            if (col == -1)
                return null;

            return _GetExdField(sheet, row, col);
        }

        private static object _GetExdField(SaintCoinach.Ex.ISheet sheet, int row, int col)
        {
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
            return field;
        }
        #endregion
        #region ISheet

        public static T GetExdField<T>(SaintCoinach.Ex.ISheet sheet, int row, int col)
        {
            var field = _GetExdField(sheet, row, col);
            if (field is T)
                return (T)field;
            return default(T);
        }

        public static T GetExdField<T>(SaintCoinach.Ex.ISheet sheet, int row, string colName)
        {
            var field = _GetExdField(sheet, row, colName);
            if (field is T)
                return (T)field;
            return default(T);
        }

        public static string GetExdFieldAsString(SaintCoinach.Ex.ISheet sheet, int row, int col)
        {
            var field = _GetExdField(sheet, row, col);
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
        #endregion
        #region Named sheet
        public static T GetExdField<T>(string sheetName, int row, int col)
        {
            var sheet = GetSheet(sheetName);
            if (sheet == null)
                return default(T);

            var field = _GetExdField(sheet, row, col);

            if (field is T)
                return (T)field;

            return default(T);
        }

        public static T GetExdField<T>(string sheetName, int row, string colName)
        {
            var sheet = GetSheet(sheetName);
            if (sheet == null)
                return default(T);

            var field = _GetExdField(sheet, row, colName);
            if (field is T)
                return (T)field;

            return default(T);
        }

        public static string GetExdFieldAsString(string sheetName, int row, int col)
        {
            var sheet = GetSheet(sheetName);
            if (sheet == null)
                return null;

            return GetExdFieldAsString(sheet, row, col);
        }

        public static string GetExdFieldAsString(string sheetName, int row, string colName)
        {
            var sheet = GetSheet(sheetName);
            if (sheet == null)
                return null;

            int col = _GetColIndexByName(sheet, colName);
            if (col == -1)
                return null;

            return GetExdFieldAsString(sheet, row, col);
        }
        #endregion
    }
}
