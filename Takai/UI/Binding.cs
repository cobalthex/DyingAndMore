using System;
using System.Reflection;

namespace Takai.UI
{
    public enum BindingMode
    {
        OneWay, //read only
        TwoTway,
    }

    /// <summary>
    /// Allows binding a UI property to an object property
    /// </summary>
    public class Binding
    {
        public BindingMode Mode { get; set; } = BindingMode.TwoTway;

        public string ObjectProperty
        {
            get => _objectProperty;
            set
            {
                _objectProperty = value;
                objectAccessors = GetBinding(value);
            }
        }
        string _objectProperty;

        public string UIProperty
        {
            get => _uiProperty;
            set
            {
                _uiProperty = value;
                uiAccessors = GetBinding(value);
            }
        }
        string _uiProperty;

        Util.GetSet objectAccessors;
        Util.GetSet uiAccessors;

        object cachedValue;
        int cachedHash;

        protected Util.GetSet GetBinding(string binding)
        {
            //todo: message when binding not found

            Util.GetSet getset;

            if (binding.StartsWith("global.", System.StringComparison.OrdinalIgnoreCase))
            {
                var bindName = binding.Substring("global.".Length);
                getset = new Util.GetSet
                {
                    get = delegate
                    {
                        Data.DataModel.Globals.TryGetValue(bindName, out var value);
                        return value;
                    },
                    set = (value) => Data.DataModel.Globals[bindName] = value
                };
            }
            else
                getset = Util.GetMemberAccessors(binding, CurrentBindTarget);

            if (getset.get == null && getset.set == null)
                System.Diagnostics.Debug.WriteLine($"UI binding '{binding}' does not exist in '{CurrentBindTarget.GetType()}'");

            return getset;
        }

        public void Update()
        {
            if (objectAccessors.get == null ||
                uiAccessors.set == null)
                return;

            var objVal = objectAccessors.get();
            var objHash = objVal.GetHashCode();


        }
    }
}
