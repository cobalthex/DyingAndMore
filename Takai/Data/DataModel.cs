using System;
using System.Collections.Generic;

namespace Takai.Data
{
    public interface IBindTarget
    {
        object GetValue(string name); //should look up in globals too. Return null if not found
    }

    /// <summary>
    /// Stores bound actions and key-value pairs for binding to UI, game state events, etc.
    /// </summary>
    public static class DataModel
    {
        public static Dictionary<string, object> Globals { get; set; } = new Dictionary<string, object>();

        //bound actions
    }
}
