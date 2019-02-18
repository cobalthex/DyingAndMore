using System;
using System.Reflection;
using System.Collections.Generic;

namespace Takai.Data
{
    public struct GetSet
    {
        /// <summary>
        /// The maximum number of nested object queries allowed.
        /// </summary>
        public static int MaxIndirection = 1;

        public Type type;
        public Func<object> get;
        public Action<object> set;

        private const BindingFlags LookupFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        public static GetSet GetMemberAccessors(object obj, string memberName)
        {
            if (memberName == null || obj == null)
                return new GetSet();

            var objType = obj.GetType();

            PropertyInfo prop;
            FieldInfo field;

            var indirections = memberName.Split(new[] { '.' }, MaxIndirection + 1);
            for (int i = 0; i < indirections.Length - 1; ++i)
            {
                try
                {
                    prop = objType.GetProperty(indirections[i], LookupFlags);
                }
                catch (AmbiguousMatchException)
                {
                    System.Diagnostics.Debug.WriteLine($"Ambiguous: {indirections[i]} ({memberName})");
                    prop = objType.GetProperty(indirections[i], LookupFlags | BindingFlags.DeclaredOnly);
                }
                if (prop != null)
                {
                    obj = prop.GetValue(obj);
                    if (obj == null)
                        objType = prop.PropertyType;
                    else
                        objType = obj.GetType();
                    continue;
                }

                field = objType.GetField(indirections[i], LookupFlags);
                if (field != null)
                {
                    obj = field.GetValue(obj);
                    if (obj == null)
                        objType = field.FieldType;
                    else
                        objType = obj.GetType();
                    continue;
                }

                return new GetSet(); //not found
            }

            memberName = indirections[indirections.Length - 1];

            var getset = new GetSet();

            //special case (can only be at the end of the indirection)
            if (memberName.Equals("@type", StringComparison.OrdinalIgnoreCase))
            {
                getset.type = typeof(Type);
                getset.get = () => objType;
                return getset;
            }
            else if (memberName.Equals("@typename", StringComparison.OrdinalIgnoreCase))
            {
                getset.type = typeof(string);
                getset.get = () => objType.Name;
                return getset;
            }

            prop = objType.GetProperty(memberName, LookupFlags);
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
            }
            else
            {
                field = objType.GetField(memberName, LookupFlags);
                if (field != null)
                {
                    getset.type = field.FieldType;
                    if (!field.IsInitOnly)
                        getset.set = (value) => field.SetValue(obj, value);
                    getset.get = () => field.GetValue(obj);
                }
            }

            return getset;
        }
    }

    public enum BindingDirection
    {
        OneWay, //source supplied only
        TwoWay,
    }

    public class Converter
    {
        //chain converters?

        public virtual object Convert(Type destType, object source)
        {
            return Serializer.Cast(destType, source);
        }
    }

    public class ConditionalConverter : Converter
    {
        /// <summary>
        /// The value to compare the incoming source value to.
        /// Returns true if the source equals this desired value
        /// </summary>
        public object DesiredValue { get; set; }

        public ConditionalConverter() { }
        public ConditionalConverter(object desiredValue)
        {
            DesiredValue = desiredValue;
        }

        //todo: more comparison operators

        public override object Convert(Type destType, object source)
        {
            return base.Convert(destType, source.Equals(DesiredValue));
        }
    }

    /// <summary>
    /// A converter that converts text to the specified case.
    /// Non text bindings are ignored
    /// </summary>
    public class TextCaseConverter : Converter
    {
        public enum TextCase
        {
            Unchanged,
            Lowercase,
            Uppercase,
            Sentence,
        }

        public TextCase DesiredCase { get; set; }

        public TextCaseConverter() { }
        public TextCaseConverter(TextCase desiredCase)
        {
            DesiredCase = desiredCase;
        }

        public override object Convert(Type destType, object source)
        {
            var target = base.Convert(destType, source);

            if (target is string s)
            {
                switch (DesiredCase)
                {
                    case TextCase.Lowercase:
                        return s.ToLower();
                    case TextCase.Uppercase:
                        return s.ToUpper();
                    case TextCase.Sentence:
                        return Util.ToSentenceCase(s);
                }
            }

            return target;
        }
    }

    /// <summary>
    /// Allows binding one object's property (source) to a target
    /// Source has priority if both are modified
    /// Bind to globals by prefixing with $ (e.g. $MapTime)
    /// </summary>
    public class Binding
    {
        public static readonly Converter DefaultConverter = new Converter();

        /// <summary>
        /// All global variables
        /// </summary>
        public static Dictionary<string, object> Globals { get; set; } = new Dictionary<string, object>();

        //todo: globals should be bindings

        public BindingDirection Direction { get; set; } = BindingDirection.OneWay;

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

        public Converter Converter { get; set; } = DefaultConverter;

        [Serializer.Ignored]
        GetSet sourceAccessors;
        [Serializer.Ignored]
        GetSet targetAccessors;

        object cachedValue;
        int cachedHash;

#if DEBUG
        private object sourceObject;
        private object targetObject;
#endif

        public Binding() { }
        public Binding(string source, string target, BindingDirection mode = BindingDirection.OneWay, object defaultValue = null)
        {
            Source = source;
            Target = target;
            Direction = mode;
            DefaultValue = defaultValue;
        }

        public Binding Clone()
        {
            return (Binding)MemberwiseClone();
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

#if DEBUG
            sourceObject = sourceObj;
            targetObject = targetObj;
#endif

            sourceAccessors = GetAccessors(Source, sourceObj);
            targetAccessors = GetAccessors(Target, targetObj);

            //set initial value
            if (targetAccessors.set != null)
            {
                var srcVal = sourceAccessors.get?.Invoke() ?? null;
                if (HasDefaultValue = (srcVal == null))
                    srcVal = DefaultValue;
                targetAccessors.set(Converter.Convert(targetAccessors.type, srcVal));
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
            var srcHash = srcVal == null ? 0 : srcVal.GetHashCode();
            var srcMatches = (srcHash == cachedHash && (srcVal == null ? cachedValue == null : srcVal.Equals(cachedValue)));

            //fuck boxing. srcVal!=cachedValue if value types

            if (!srcMatches)
            {
                var bindVal = srcVal;

                if (HasDefaultValue = (bindVal == null))
                    bindVal = DefaultValue;
                targetAccessors.set(Converter.Convert(targetAccessors.type, bindVal));
                System.Diagnostics.Debug.WriteLine($"Updated binding for source:{Source} ({cachedValue}) to target:{Target} ({cachedValue})");
                cachedValue = srcVal;
                cachedHash = srcHash;
                return true;
            }

            if (Direction == BindingDirection.TwoWay && targetAccessors.get != null && sourceAccessors.set != null)
            {
                var tgtVal = targetAccessors.get();
                var tgtHash = tgtVal == null ? 0 : tgtVal.GetHashCode();
                var tgtMatches = (tgtHash == cachedHash && (tgtVal == null ? cachedValue == null : tgtVal.Equals(cachedValue)));

                if (!tgtMatches)
                {
                    var bindVal = tgtVal;
                    sourceAccessors.set(Converter.Convert(sourceAccessors.type, bindVal));
                    System.Diagnostics.Debug.WriteLine($"Updated binding for target:{Target} ({cachedValue}) to source:{Source} ({cachedValue})");
                    cachedValue = tgtVal;
                    cachedHash = tgtHash;

                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{Source}{(Direction == BindingDirection.TwoWay ? "<" : "")}->{Target}";
        }
    }
}
