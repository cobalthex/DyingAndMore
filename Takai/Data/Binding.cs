using System;
using System.Reflection;

namespace Takai.Data
{
    public struct GetSet
    {
        public Type type;
        public Func<object> get;
        public Action<object> set;

        public static GetSet GetMemberAccessors(string memberName, object obj)
        {
            var getset = new GetSet();

            if (obj == null)
                return getset;

            var type = obj.GetType();

            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                //todo: get delegates working
                getset.type = prop.PropertyType;
                getset.get = () => prop.GetValue(obj);
                getset.set = (value) => prop.SetValue(obj, value);
                //getset.get = prop.CanRead ? (Func<object>)prop.GetGetMethod(false).CreateDelegate(typeof(Func<object>), obj) : null;
                //getset.set = prop.CanWrite ? (Action<object>)prop.GetSetMethod(false).CreateDelegate(typeof(Action<object>), obj) : null;
                return getset;
            }

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                throw new NotImplementedException(); //todo
            }

            return getset;
        }
    }

    public enum BindingMode
    {
        OneWay, //source supplied only
        TwoTway,
    }

    /// <summary>
    /// Allows binding one object's property (source) to a target
    /// Source has priority if both are modified
    /// </summary>
    public class Binding : ICloneable
    {
        public BindingMode Mode { get; set; } = BindingMode.OneWay;

        public string SourceProperty { get; set; }
        public string TargetProperty { get; set; }

        //fallback value?

        GetSet sourceAccessors;
        GetSet targetAccessors;

        object cachedValue;
        int cachedHash;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public virtual void BindTo(object source, object target)
        {
            sourceAccessors = GetAccessors(SourceProperty, source);
            targetAccessors = GetAccessors(TargetProperty, target);
            Update();

            //todo: need to clear values of nulls
        }

        public static GetSet GetAccessors(string binding, object obj)
        {
            //todo: message when binding not found

            GetSet getset;

            if (binding.StartsWith("global.", StringComparison.OrdinalIgnoreCase))
            {
                var bindName = binding.Substring("global.".Length);
                getset = new GetSet
                {
                    type = obj.GetType(),
                    get = delegate
                    {
                        DataModel.Globals.TryGetValue(bindName, out var value);
                        return value;
                    },
                    set = (value) => DataModel.Globals[bindName] = value
                };
            }
            else
                getset = GetSet.GetMemberAccessors(binding, obj);

            if (getset.get == null && getset.set == null)
                System.Diagnostics.Debug.WriteLine($"UI binding '{binding}' does not exist in '{obj.GetType()}'");

            return getset;
        }

        /// <summary>
        /// Check to see if the binding needs update and perform the update
        /// </summary>
        /// <returns>True if the binding's value was updated</returns>
        public bool Update()
        {
            if (targetAccessors.set == null)
                return false;

            var srcVal = sourceAccessors.get?.Invoke() ?? null;
            var srcHash = srcVal?.GetHashCode() ?? 0;
            var srcMatches = (srcHash == cachedHash && (srcVal == null ? cachedValue == null : srcVal.Equals(cachedValue)));

            //todo: optimize casts? necessary?

            if (!srcMatches)
            {
                targetAccessors.set(Serializer.Cast(targetAccessors.type, srcVal));
                cachedValue = srcVal;
                cachedHash = srcHash;
                OnUpdated();

                //System.Diagnostics.Debug.WriteLine($"Updated binding for source:{SourceProperty} to target:{TargetProperty} = {cachedValue}");
                return true;
            }

            if (Mode == BindingMode.TwoTway && targetAccessors.get != null && sourceAccessors.set != null)
            {
                var tgtVal = targetAccessors.get();
                var tgtHash = tgtVal?.GetHashCode() ?? 0;
                var tgtMatches = (tgtHash == cachedHash && (tgtVal == null ? cachedValue == null : tgtVal.Equals(cachedValue)));

                if (!tgtMatches)
                {
                    sourceAccessors.set(Serializer.Cast(sourceAccessors.type, tgtVal));
                    cachedValue = tgtVal;
                    cachedHash = tgtHash;
                    OnUpdated();

                    //System.Diagnostics.Debug.WriteLine($"Updated binding for target:{TargetProperty} to source:{SourceProperty} = {cachedValue}");
                    return true;
                }
            }
            return false;
        }

        protected virtual void OnUpdated() { }
    }
}
