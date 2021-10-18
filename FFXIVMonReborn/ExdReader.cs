using Lumina;
using Lumina.Excel;

namespace FFXIVMonReborn
{
    public static class ExdReader
    {
        public static string GamePath { get; private set; }
        public static GameData ARealmReversed { get; private set; }

        #region Init

        public static void Init(string path)
        {
            GamePath = path;
            ARealmReversed = new GameData(path);
        }

        #endregion

        public static ExcelSheet<T> GetSheet<T>() where T: ExcelRow
        {
            if (ARealmReversed == null)
            {
                Init(GamePath);
            }

            return ARealmReversed.GetExcelSheet<T>();
        }
    }
}