using System;
using System.Collections.Generic;

namespace Takai.UI
{
    public class KeyValueTable<TKey, TValue> : ItemList<KeyValuePair<TKey, TValue>>
    {
        public KeyValueTable()
        {
            var keyUI = new Static { Bindings = new List<Data.Binding> { new Data.Binding("Key", "Text") } };
            var valUI = new GeneratedUI("Value"); // not ideal

            ItemUI = new List(keyUI, valUI)
            {
                Direction = Direction.Horizontal,
            };
        }
    }
}
