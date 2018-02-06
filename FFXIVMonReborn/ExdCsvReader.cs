using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace FFXIVMonReborn
{
    public class ExdCsvReader
    {
        private List<string> _bnpcnames = new List<string>();
        private List<string> _placenames = new List<string>();
        private List<string> _actionnames = new List<string>();
        private Dictionary<int, string> _fatenames = new Dictionary<int, string>();

        public ExdCsvReader()
        {
            try
            {
                using (TextFieldParser parser = new TextFieldParser(@"exd\bnpcname.exh_en.csv"))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    int rowCount = 0;
                    while (!parser.EndOfData)
                    {
                        rowCount++;
                        string[] fields = parser.ReadFields();
                        int fCount = 0;
                        foreach (string field in fields)
                        {
                            fCount++;

                            if (fCount == 2)
                            {
                                _bnpcnames.Add(field);
                            }
                        }
                    }
                    Debug.WriteLine($"ExdCsvReader: {rowCount} bnpc names read");
                }

                using (TextFieldParser parser = new TextFieldParser(@"exd\placename.exh_en.csv"))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    int rowCount = 0;
                    while (!parser.EndOfData)
                    {
                        rowCount++;
                        string[] fields = parser.ReadFields();
                        int fCount = 0;
                        foreach (string field in fields)
                        {
                            fCount++;

                            if (fCount == 2)
                            {
                                _placenames.Add(field);
                            }
                        }
                    }
                    Debug.WriteLine($"ExdCsvReader: {rowCount} placenames read");
                }

                using (TextFieldParser parser = new TextFieldParser(@"exd\action.exh_en.csv"))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    int rowCount = 0;
                    while (!parser.EndOfData)
                    {
                        rowCount++;
                        string[] fields = parser.ReadFields();
                        int fCount = 0;
                        foreach (string field in fields)
                        {
                            fCount++;

                            if (fCount == 2)
                            {
                                _actionnames.Add(field);
                            }
                        }
                    }
                    Debug.WriteLine($"ExdCsvReader: {rowCount} actionnames read");
                }

                using (TextFieldParser parser = new TextFieldParser(@"exd\fate.exh_en.csv"))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    int rowCount = 0;
                    while (!parser.EndOfData)
                    {
                        //Processing row
                        rowCount++;
                        string[] fields = parser.ReadFields();
                        int fCount = 0;

                        int index = 0;
                        string name = "";
                        foreach (string field in fields)
                        {
                            fCount++;

                            if (fCount == 1)
                            {
                                int.TryParse(field, out index);
                            }

                            if (fCount == 26)
                            {
                                name = field;
                            }
                        }

                        if (rowCount != 1)
                        {
                            _fatenames.Add(index, name);
                        }
                    }
                    Debug.WriteLine($"ExdCsvReader: {rowCount} fatenames read");
                }
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("[ExdCsvReader] Failed to parse CSV sheets.", exc.ToString(), "Error").ShowDialog();
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
