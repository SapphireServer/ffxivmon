using System.Collections.Generic;
using System.Linq;
using Lumina;
using Lumina.Excel.GeneratedSheets;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace FFXIVMonReborn
{
    public class ExdDataCache
    {
        private Dictionary<uint, string> _bnpcNames = new();
        private Dictionary<uint, string> _placeNames = new();
        private Dictionary<uint, string> _actionNames = new();
        private Dictionary<uint, string> _fateNames = new();

        public ExdDataCache(string gamePath)
        {
            GameData data = new GameData(gamePath);
            _bnpcNames = data.Excel.GetSheet<BNpcName>().ToDictionary(row => row.RowId, row => row.Singular.ToString());
            _placeNames = data.Excel.GetSheet<PlaceName>().ToDictionary(row => row.RowId, row => row.Name.ToString());
            _actionNames = data.Excel.GetSheet<Action>().ToDictionary(row => row.RowId, row => row.Name.ToString());
            _fateNames = data.Excel.GetSheet<Fate>().ToDictionary(row => row.RowId, row => row.Name.ToString());
        }

        public string GetBnpcName(uint id)
        {
            if (!_bnpcNames.TryGetValue(id, out var name))
                name = "Unknown";
            return name;
        }

        public string GetPlacename(uint id)
        {
            if (!_placeNames.TryGetValue(id, out var name))
                name = "Unknown";
            return name;
        }

        public string GetActionName(uint id)
        {
            if (!_actionNames.TryGetValue(id, out var name))
                name = "Unknown";
            return name;
        }

        public string GetFateName(uint id)
        {
            if (!_fateNames.TryGetValue(id, out var name))
                name = "Unknown";
            return name;
        }
    }
}
