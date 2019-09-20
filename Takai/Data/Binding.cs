﻿using System;
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

        internal object cachedValue;
        internal int cachedHash;
        internal bool isCollection;

        private const BindingFlags LookupFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        public static GetSet GetMemberAccessors(object obj, string memberName, bool allowNonPublic = false)
        {
            if (memberName == null || obj == null)
                return new GetSet();

            var objType = obj.GetType();

            if (memberName.Equals("this", StringComparison.OrdinalIgnoreCase)) //experimental
            {
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

            //NOTE: if any part of the hierarchy changes, this binding will point to the old version
            //this is technically leaky and otherwise generally not desirable

            for (int i = 0; i < indirections.Length - 1; ++i)
            {
                try
                {
                    prop = objType.GetProperty(indirections[i], finalLookupFlags);
                }
                catch (AmbiguousMatchException)
                {
                    System.Diagnostics.Debug.WriteLine($"Ambiguous: {indirections[i]} ({memberName})");
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

                    //getset.get = prop.CanRead ? (Func<object>)prop.GetGetMethod(false).CreateDelegate(typeof(Func<object>), obj) : null;
                    //getset.set = prop.CanWrite ? (Action<object>)prop.GetSetMethod(false).CreateDelegate(typeof(Action<object>), obj) : null;
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
                    return getset;
                }
                else if (modifier[1].Equals("typename", StringComparison.OrdinalIgnoreCase))
                {
                    getset.type = typeof(string);
                    //getset.get = () => objType.Name;
                    getset.get = () => oget()?.GetType().Name; //live type (works w/ polymorphism)
                    return getset;
                }
                else if (modifier[1].Equals("hash", StringComparison.OrdinalIgnoreCase))
                {
                    getset.type = typeof(int);
                    getset.get = () => oget()?.GetHashCode() ?? 0;
                    return getset;
                }
                else
                    System.Diagnostics.Debug.WriteLine($"Ignoring unknown binding modifier {modifier[0]}:{modifier[1]}");
            }

            //getset.recursiveGets = recursives;
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

#if DEBUG
        private object sourceObject;
        private object targetObject;

        public static int TotalUpdateCount { get; private set; } = 0;
#endif

        delegate object CloneFn(object source);
        private static CloneFn cloneFn;
        static Binding()
        {
            var clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            cloneFn = (CloneFn)clone.CreateDelegate(typeof(CloneFn));
        }

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
        public virtual void BindTo(object sourceObj, object targetObj, bool allowNonPublic = false)
        {
            if (Source == null || Target == null)
                return;

#if DEBUG
            sourceObject = sourceObj;
            targetObject = targetObj;
#endif

            sourceAccessors = GetAccessors(Source, sourceObj, allowNonPublic);
            targetAccessors = GetAccessors(Target, targetObj, allowNonPublic);

            //set initial value
            if (targetAccessors.set != null)
            {
                var srcVal = sourceAccessors.get?.Invoke() ?? null;
                if (HasDefaultValue = (srcVal == null))
                    srcVal = DefaultValue;
                targetAccessors.set(Converter.Convert(targetAccessors.type, srcVal));
                sourceAccessors.cachedValue = targetAccessors.cachedValue = srcVal;// sourceAccessors.type.IsClass ? cloneFn(srcVal) : srcVal;
                sourceAccessors.isCollection = targetAccessors.isCollection 
                    = typeof(System.Collections.ICollection).IsInstanceOfType(srcVal);
                if (srcVal != null)
                    sourceAccessors.cachedHash = targetAccessors.cachedHash = srcVal.GetHashCode();
            }
        }

        public static GetSet GetAccessors(string binding, object obj, bool allowNonPublic = false)
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
            if (targetAccessors.set == null)
                return false;

            var srcVal = sourceAccessors.get?.Invoke() ?? null;
            var srcHash = srcVal == null ? 0 : srcVal.GetHashCode();
            var srcMatches = (srcHash == sourceAccessors.cachedHash &&
                             (srcVal == null ? sourceAccessors.cachedValue == null : //srcVal.Equals(sourceAccessors.cachedValue)));
                                   BothEqual(srcVal, sourceAccessors.cachedValue, sourceAccessors.isCollection)));

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

            if (Direction == BindingDirection.TwoWay && targetAccessors.get != null && sourceAccessors.set != null)
            {
                var tgtVal = targetAccessors.get();
                var tgtHash = tgtVal == null ? 0 : tgtVal.GetHashCode();
                var tgtMatches = (tgtHash == targetAccessors.cachedHash &&
                                 (tgtVal == null ? targetAccessors.cachedValue == null : 
                                    BothEqual(tgtVal, targetAccessors.cachedValue, targetAccessors.isCollection)));

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

        bool BothEqual(object a, object b, bool isCollection)
        {
            if (isCollection)
            {
                var ac = (System.Collections.ICollection)a;
                var bc = (System.Collections.ICollection)b;
                if (ac.Count != bc.Count)
                    return false;
                var ae = ac.GetEnumerator();
                var be = bc.GetEnumerator();
                for (var i = 0; i < ac.Count; ++i)
                {
                    ae.MoveNext();
                    be.MoveNext();
                    if (ae.Current != be.Current)
                        return false;
                }
                return true;
            }
            return a.Equals(b);
        }

        public override string ToString()
        {
            return $"{Source}{(Direction == BindingDirection.TwoWay ? "<" : "")}->{Target}";
        }
    }
}
