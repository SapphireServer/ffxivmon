using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Microsoft.VisualBasic.FileIO;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace FFXIVMonReborn
{
    public class ExdDataCache
    {
        private Dictionary<uint, string> _bnpcnames = new();
        private Dictionary<uint, string> _placenames = new();
        private Dictionary<uint, string> _actionnames = new();
        private Dictionary<uint, string> _fatenames = new();

        public ExdDataCache()
        {
            void PopulateStringList<T>(string fieldName, ref Dictionary<uint, string> list) where T: ExcelRow
            {
                var sheet = ExdReader.GetSheet<T>();
                if (sheet != null)
                {
                    var prop = typeof(T).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                    foreach (var excelRow in sheet)
                    {
                        list.Add(excelRow.RowId, prop.GetValue(excelRow).ToString());
                    }
                }
            }

            PopulateStringList<BNpcName>("Singular", ref _bnpcnames);
            PopulateStringList<PlaceName>("Name", ref _placenames);
            PopulateStringList<Action>("Name", ref _actionnames);
            PopulateStringList<Fate>("Name", ref _fatenames);
        }

        public string GetBnpcName(uint id)
        {
            try
            {
                return _bnpcnames[id];
            }
            catch
            {
                return "Unknown";
            }

        }

        public string GetPlacename(uint id)
        {
            try
            {
                return _placenames[id];
            }
            catch
            {
                return "Unknown";
            }
        }

        public string GetActionName(uint id)
        {
            try
            {
                return _actionnames[id];
            }
            catch
            {
                return "Unknown Action";
            }
        }

        public string GetFateName(uint id)
        {
            string name;
            if (_fatenames.TryGetValue(id, out name))
            {
                return name;
            }
            else
            {
                return "Unknown Fate";
            }
        }
    }
}