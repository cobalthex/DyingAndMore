using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace Takai.Data
{
    //todo: late bound bindings (live bindings?, store all nested layers?)
    public struct GetSet
    {
        /// <summary>
        /// The maximum number of nested object queries allowed.
        /// </summary>
        public static int MaxIndirection = 1;

        public Type type;
        public Func<object> get;
        public Action<object> set;

        internal object cachedValue;
        internal int cachedHash;

        private const BindingFlags LookupFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        public static GetSet GetMemberAccessors(object obj, string memberName, bool allowNonPublic = false)
        {
            if (memberName == null || obj == null)
                return new GetSet();

            var objType = obj.GetType();

            if (memberName.Equals("this", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("Using 'this' for bindings is experimental and hacky");
                return new GetSet
                {
                    type = objType,
                    set = delegate (object value)
                    {
                        Serializer.ApplyObject(obj, value); //hacky
                    },
                    get = () => obj
                };
            }

            PropertyInfo prop;
            FieldInfo field;

            var finalLookupFlags = LookupFlags | (allowNonPublic ? BindingFlags.NonPublic : 0);

            var indirections = memberName.Split(new[] { '.' }, MaxIndirection + 1);

            //todo: get returns the nested most object
            //set: each layer sets value and rebinds children recursively. some trickiness here

            for (int i = 0; i < indirections.Length - 1; ++i)
            {
                try
                {
                    prop = objType.GetProperty(indirections[i], finalLookupFlags);
                }
                catch (AmbiguousMatchException)
                {
                    //System.Diagnostics.Debug.WriteLine($"Ambiguous: {indirections[i]} ({memberName})");
                    prop = objType.GetProperty(indirections[i], finalLookupFlags | BindingFlags.DeclaredOnly);
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

                field = objType.GetField(indirections[i], finalLookupFlags);
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

            if (obj == null)
                return new GetSet();

            memberName = indirections[indirections.Length - 1];

            var getset = new GetSet();

            var modifier = memberName.Split(new[] { ':' }, 2); //modifiers only allowed on child most object
            memberName = modifier[0];

            if (memberName.Length > 0)
            {
                prop = objType.GetProperty(memberName, finalLookupFlags);
                if (prop != null)
                {
                    //todo: get delegates working

                    getset.type = prop.PropertyType;
                    if (prop.CanRead)
                        getset.get = () => prop.GetValue(obj);
                    if (prop.CanWrite)
                        getset.set = (value) => prop.SetValue(obj, value);
                }
                else
                {
                    field = objType.GetField(memberName, finalLookupFlags);
                    if (field != null)
                    {
                        getset.type = field.FieldType;
                        getset.get = () => field.GetValue(obj);
                        if (!field.IsInitOnly)
                            getset.set = (value) => field.SetValue(obj, value);
                    }
                }
            }
            else
            {
                getset.type = objType;
                getset.get = () => obj;
            }


            if (modifier.Length > 1 && getset.get != null)
            {
                var oget = getset.get; //explicitly allow throwing
                if (modifier[1].Equals("type", StringComparison.OrdinalIgnoreCase))
                {
                    getset.type = typeof(Type);
                    //getset.get = () => objType;
                    getset.get = () => oget()?.GetType(); //live type (works w/ polymorphism)
                    getset.set = null;
                    return getset;
                }
                else if (modifier[1].Equals("typename", StringComparison.OrdinalIgnoreCase))
                {
                    getset.type = typeof(string);
                    //getset.get = () => objType.Name;
                    getset.get = () => oget()?.GetType().Name; //live type (works w/ polymorphism)
                    getset.set = null;
                    return getset;
                }
                else if (modifier[1].Equals("hash", StringComparison.OrdinalIgnoreCase))
                {
                    getset.type = typeof(int);
                    getset.get = () => oget()?.GetHashCode() ?? 0;
                    getset.set = null;
                    return getset;
                }
                else if (modifier[1].Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    getset.type = typeof(string);
                    getset.get = () => oget()?.ToString() ?? "[null]";
                    getset.set = null;
                    return getset;
                }
                else if (modifier[1].Equals("slist", StringComparison.OrdinalIgnoreCase))
                {
                    getset.type = typeof(string);
                    getset.get = () =>
                    {
                        var sb = new StringBuilder();
                        var v = oget();
                        if (v != null && v is System.Collections.IEnumerable e)
                        {
                            bool first = true;
                            foreach (var i in e)
                            {
                                if (!first)
                                    sb.Append(",");

                                first = false;
                                sb.Append(i);
                            }
                        }
                        return sb.ToString();
                    };
                    getset.set = null;
                    return getset;
                }
                else if (modifier[1].Equals("count", StringComparison.OrdinalIgnoreCase))
                {
                    //todo
                }
                else
                    System.Diagnostics.Debug.WriteLine($"Ignoring unknown binding modifier {modifier[0]}:{modifier[1]}");
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

        /// <summary>
        /// Invert the logic condition (!=)
        /// </summary>
        public bool Negate { get; set; }

        public ConditionalConverter() { }
        public ConditionalConverter(object desiredValue)
        {
            DesiredValue = desiredValue;
        }

        //todo: more comparison operators

        public override object Convert(Type destType, object source)
        {
            if (source == null)
                return Negate ^ (source == DesiredValue); //todo: verify
            return base.Convert(destType, Negate ^ source.Equals(DesiredValue));
        }
    }

    /// <summary>
    /// Basic math converter (+ - * /)
    /// Subtraction is negative addition
    /// Division is fracional multiplication
    /// </summary>
    public class BasicMathConverter : Converter
    {
        /// <summary>
        /// The right hand side of the operation
        /// </summary>
        public double Factor { get; set; }

        /// <summary>
        /// Multiply the factor instead of adding 
        /// </summary>
        public bool Multiply { get; set; }

        public BasicMathConverter() { }
        public BasicMathConverter(float factor, bool multiply)
        {
            Factor = factor;
            Multiply = multiply;
        }

        //todo: more comparison operators

        public override object Convert(Type destType, object source)
        {
            var x = System.Convert.ToDouble(source);
            double y;
            if (Multiply)
                y = x * Factor;
            else
                y = x + Factor;
            return base.Convert(destType, y);
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
    /// Format the source value with the specified format string
    /// Returns the output as a string
    /// </summary>
    public class StringFormatConverter : Converter
    {
        public Converter PreConverter { get; set; }

        public string Format { get; set; }

        public StringFormatConverter() { }
        public StringFormatConverter(string format)
        {
            Format = format;
        }

        public override object Convert(Type destType, object source)
        {
            return string.Format(Format, PreConverter == null ? source : PreConverter.Convert(source.GetType(), source));
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

#if DEBUG
        private object sourceObject;
        private object targetObject;

        public static int TotalUpdateCount { get; private set; } = 0;
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
        /// <param name="allowNonPublic">Custom properties to bind to, allows for things like :index in lists</param>
        public virtual void BindTo(object sourceObj, object targetObj, Dictionary<string, object> customBindProps = null, bool allowNonPublic = false)
        {
            if (Source == null || Target == null)
                return;

#if DEBUG
            sourceObject = sourceObj;
            targetObject = targetObj;
#endif

            sourceAccessors = GetAccessors(Source, sourceObj, customBindProps, allowNonPublic);
            targetAccessors = GetAccessors(Target, targetObj, customBindProps, allowNonPublic);

            //set initial value
            if (targetAccessors.set != null)
            {
                var srcVal = sourceAccessors.get?.Invoke() ?? null;
                if (HasDefaultValue = (srcVal == null))
                    srcVal = DefaultValue;
                targetAccessors.set(Converter.Convert(targetAccessors.type, srcVal));
                sourceAccessors.cachedValue = targetAccessors.cachedValue = srcVal;// sourceAccessors.type.IsClass ? cloneFn(srcVal) : srcVal;
                if (srcVal != null)
                    sourceAccessors.cachedHash = targetAccessors.cachedHash = srcVal.GetHashCode();
            }
        }

        public static GetSet GetAccessors(string binding, object obj, Dictionary<string, object> customBindProps = null, bool allowNonPublic = false)
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
            else if (customBindProps != null && customBindProps.TryGetValue(binding, out var customBind))
            {
                getset = new GetSet
                {
                    type = customBind.GetType(),
                    get = () => customBind
                    //currently readonly (todo?)
                };
            }
            else
                getset = GetSet.GetMemberAccessors(obj, binding, allowNonPublic);

            //todo: magic properties set by binder (e.g. index counters for lists, something like :index)

            if (getset.get == null && getset.set == null)
                System.Diagnostics.Debug.WriteLine($"Binding '{binding}' does not exist in '{obj.GetType()}'");

            return getset;
        }

        internal bool isTargetDirty = false; //for hacky shit (usually forcing collections to update)

        /// <summary>
        /// Check to see if the binding needs update and perform the update
        /// </summary>
        /// <returns>True if the binding's value was updated</returns>
        public bool Update()
        {
            if (targetAccessors.set != null)
            {
                var srcVal = sourceAccessors.get?.Invoke() ?? null;
                var srcHash = srcVal == null ? 0 : srcVal.GetHashCode();
                var srcMatches = (srcHash == sourceAccessors.cachedHash &&
                                 (srcVal == null ? sourceAccessors.cachedValue == null : srcVal.Equals(sourceAccessors.cachedValue)));

                if (!srcMatches)
                {
                    var bindVal = srcVal;
                    if (HasDefaultValue = (bindVal == null))
                        bindVal = DefaultValue;

                    bindVal = Converter.Convert(targetAccessors.type, bindVal);
                    targetAccessors.set(bindVal);

                    sourceAccessors.cachedValue = srcVal;
                    sourceAccessors.cachedHash = srcHash;
                    targetAccessors.cachedValue = bindVal;
                    targetAccessors.cachedHash = bindVal == null ? 0 : bindVal.GetHashCode();
                    //System.Diagnostics.Debug.WriteLine($"Updated binding for source:{Source} ({sourceAccessors.cachedValue}) to target:{Target} ({srcVal})");
#if DEBUG
                    ++TotalUpdateCount;
#endif
                    return true;
                }
            }

            if (Direction == BindingDirection.TwoWay && targetAccessors.get != null && sourceAccessors.set != null)
            {
                var tgtVal = targetAccessors.get();
                var tgtHash = tgtVal == null ? 0 : tgtVal.GetHashCode();
                var tgtMatches = (tgtHash == targetAccessors.cachedHash &&
                                 (tgtVal == null ? targetAccessors.cachedValue == null : tgtVal.Equals(targetAccessors.cachedValue)));

                if (!tgtMatches || isTargetDirty)
                {
                    var bindVal = Converter.Convert(sourceAccessors.type, tgtVal);
                    sourceAccessors.set(bindVal);

                    targetAccessors.cachedValue = tgtVal;
                    targetAccessors.cachedHash = tgtHash;
                    sourceAccessors.cachedValue = bindVal;
                    sourceAccessors.cachedHash = bindVal == null ? 0 : bindVal.GetHashCode();
                    //System.Diagnostics.Debug.WriteLine($"Updated binding for target:{Target} ({targetAccessors.cachedValue}) to source:{Source} ({tgtVal})");
#if DEBUG
                    ++TotalUpdateCount;
#endif
                    isTargetDirty = false;
                    return true;
                }
            }

            /* todo: current limitation,
                bindings are not weak bindings, nested objects will bind to specific objects, not members; e.g.
                var a, b;
                obj = a;
                BindTo(obj.prop);
                obj = b;
                //binding still points to a.prop not b.prop
            */

            return false;
        }

        public override string ToString()
        {
            return $"{Source}{(Direction == BindingDirection.TwoWay ? "<" : "")}->{Target}";
        }
    }
}
