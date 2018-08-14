using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace FFXIVMonReborn
{
    public class ExdDataCache
    {
        private List<string> _bnpcnames = new List<string>();
        private List<string> _placenames = new List<string>();
        private List<string> _actionnames = new List<string>();
        private Dictionary<int, string> _fatenames = new Dictionary<int, string>();

        public ExdDataCache()
        {
            void PopulateStringList(string sheetName, string fieldName, ref List<string> list)
            {
                var sheet = ExdReader.GetSheet(sheetName);
                if (sheet != null)
                {
                    for (var i = 0; i < sheet.Count; ++i)
                    {
                        list.Add(ExdReader.GetExdFieldAsString(sheetName, i, fieldName) ?? "");
                    }
                }
            }

            PopulateStringList("BNpcName", "Singular", ref _bnpcnames);
            PopulateStringList("PlaceName", "Name", ref _placenames);
            PopulateStringList("Action", "Name", ref _actionnames);

            var fateSheet = ExdReader.GetSheet("Fate");
            if (fateSheet != null)
            {
                for (var i = 0; i < fateSheet.Count; ++i)
                {
                    _fatenames.Add(i, ExdReader.GetExdFieldAsString("Fate", i, "Name") ?? "");
                }
            }
        }

        public string GetBnpcName(int id)
        {
            try
            {
                return _bnpcnames[id + 1];
            }
            catch
            {
                return "Unknown";
            }

        }

        public string GetPlacename(int id)
        {
            try
            {
                return _placenames[id + 1];
            }
            catch
            {
                return "Unknown";
            }
        }

        public string GetActionName(int id)
        {
            try
            {
                return _actionnames[id + 1];
            }
            catch
            {
                return "Unknown Action";
            }
        }

        public string GetFateName(int id)
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
