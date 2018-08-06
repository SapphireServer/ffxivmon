using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVMonReborn.Scripting
{
    public class ScriptingDataStorage
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        public void Store(string name, object data)
        {
            if (_data.ContainsKey(name))
                _data[name] = data;
            else
                _data.Add(name, data);
        }

        public object Get(string name)
        {
            if (_data.ContainsKey(name))
                return _data[name];

            return null;
        }

        public void Reset()
        {
            _data.Clear();
        }
    }
}
