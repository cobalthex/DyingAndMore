using System;
using System.Reflection;
using System.Collections.Generic;

namespace Takai.Data
{
    public struct GetSet
    {
        /// <summary>
        /// The maximum number of nested object queries allowed.
        /// Nothing will be returned if &lt; 1
        /// </summary>
        public static int MaxIndirection = 2;

        public Type type;
        public Func<object> get;
        public Action<object> set;

        public static GetSet GetMemberAccessors(object obj, string memberName)
        {
            if (memberName == null || obj == null)
                return new GetSet();

            var objType = obj.GetType();

            PropertyInfo prop;
            FieldInfo field;

            var indirections = memberName.Split(new[] { '.' }, MaxIndirection);
            for (int i = 0; i < indirections.Length - 1; ++i)
            {
                prop = objType.GetProperty(indirections[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly);
                if (prop != null)
                {
                    obj = prop.GetValue(obj);
                    if (obj == null)
                        return new GetSet();

                    objType = obj.GetType();
                    continue;
                }

                field = objType.GetField(indirections[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    obj = field.GetValue(obj);
                    if (obj == null)
                        return new GetSet();

                    objType = obj.GetType();
                    continue;
                }

                return new GetSet(); //not found
            }

            memberName = indirections[indirections.Length - 1];

            var getset = new GetSet();

            //special case (can only be at the end)
            if (memberName.Equals("@type", StringComparison.OrdinalIgnoreCase))
            {
                getset.type = typeof(string);
                getset.get = () => objType.Name;
                return getset;
            }

            prop = objType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                //todo: get delegates working

                getset.type = prop.PropertyType;
                if (prop.CanRead)
                    getset.get = () => prop.GetValue(obj);
                if (prop.CanWrite)
                    getset.set = (value) => prop.SetValue(obj, value);

                //getset.get = prop.CanRead ? (Func<object>)prop.GetGetMethod(false).CreateDelegate(typeof(Func<object>), obj) : null;
                //getset.set = prop.CanWrite ? (Action<object>)prop.GetSetMethod(false).CreateDelegate(typeof(Action<object>), obj) : null;
                return getset;
            }

            field = objType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                getset.type = field.FieldType;
                if (!field.IsInitOnly)
                    getset.set = (value) => field.SetValue(obj, value);
                getset.get = () => field.GetValue(obj);
            }

            return getset;
        }
    }

    public enum BindingMode
    {
        OneWay, //source supplied only
        TwoWay,
    }

    /// <summary>
    /// Allows binding one object's property (source) to a target
    /// Source has priority if both are modified
    /// Bind to globals by prefixing with $ (e.g. $MapTime)
    /// </summary>
    public class Binding : ICloneable
    {
        /// <summary>
        /// All global variables
        /// </summary>
        public static Dictionary<string, object> Globals { get; set; } = new Dictionary<string, object>();

        //todo: globals should be bindings

        public BindingMode Mode { get; set; } = BindingMode.OneWay;

        /// <summary>
        /// The property from the source object to bind to
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// The property from the target object to bind to
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// The value used when there bound value is null
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Is this binding currently returning the default value?
        /// </summary>
        public bool HasDefaultValue { get; protected set; }

        /// <summary>
        /// If not null, the bound value must equal this to be displayed
        /// (DefaultValue is displayed if conditoin not met)
        /// </summary>
        public object ConditionObject { get; set; }

        GetSet sourceAccessors;
        GetSet targetAccessors;

        object cachedValue;
        int cachedHash;

        public Binding() { }
        public Binding(string source, string target, BindingMode mode = BindingMode.OneWay, object defaultValue = null)
        {
            Source = source;
            Target = target;
            Mode = mode;
            DefaultValue = defaultValue;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Bind to an object
        /// Does not call <see cref="Update"/>
        /// </summary>
        /// <param name="sourceObj">The backing object to bind against</param>
        /// <param name="targetObj">The object to send/recieve data to/from the source</param>
        public virtual void BindTo(object sourceObj, object targetObj)
        {
            if (Source == null || Target == null)
                return;

            sourceAccessors = GetAccessors(Source, sourceObj);
            targetAccessors = GetAccessors(Target, targetObj);

            //set initial value
            var srcVal = sourceAccessors.get?.Invoke() ?? null;
            if (targetAccessors.set != null)
            {
                if (ConditionObject != null && srcVal != ConditionObject)
                    srcVal = null;

                if (HasDefaultValue = (srcVal == null))
                    srcVal = DefaultValue;
                targetAccessors.set(Serializer.Cast(targetAccessors.type, srcVal));
                cachedValue = srcVal;
                if (srcVal != null)
                    cachedHash = srcVal.GetHashCode();
            }
        }

        public static GetSet GetAccessors(string binding, object obj)
        {
            if (obj == null)
                return new GetSet();

            GetSet getset;
            if (binding.StartsWith("$", StringComparison.OrdinalIgnoreCase))
            {
                var bindName = binding.Substring("$".Length);
                getset = new GetSet
                {
                    type = obj.GetType(),
                    get = delegate
                    {
                        Globals.TryGetValue(bindName, out var value);
                        return value;
                    },
                    set = (value) => Globals[bindName] = value
                };
            }
            else
                getset = GetSet.GetMemberAccessors(obj, binding);

            if (getset.get == null && getset.set == null)
                System.Diagnostics.Debug.WriteLine($"Binding '{binding}' does not exist in '{obj.GetType()}'");

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
                var bindVal = srcVal;

                if (ConditionObject != null && bindVal != ConditionObject)
                    bindVal = null;

                if (HasDefaultValue = (bindVal == null))
                    bindVal = DefaultValue;
                targetAccessors.set(Serializer.Cast(targetAccessors.type, bindVal));
                cachedValue = srcVal;
                cachedHash = srcHash;
                OnUpdated();

                //System.Diagnostics.Debug.WriteLine($"Updated binding for source:{SourceProperty} to target:{TargetProperty} = {cachedValue}");
                return true;
            }

            if (Mode == BindingMode.TwoWay && targetAccessors.get != null && sourceAccessors.set != null)
            {
                var tgtVal = targetAccessors.get();
                var tgtHash = tgtVal?.GetHashCode() ?? 0;
                var tgtMatches = (tgtHash == cachedHash && (tgtVal == null ? cachedValue == null : tgtVal.Equals(cachedValue)));

                if (!tgtMatches)
                {
                    var bindVal = tgtVal;

                    if (ConditionObject != null && bindVal != ConditionObject)
                        bindVal = null;

                    if (ConditionObject != null && bindVal != ConditionObject)
                        bindVal = DefaultValue;
                    sourceAccessors.set(Serializer.Cast(sourceAccessors.type, bindVal));
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
