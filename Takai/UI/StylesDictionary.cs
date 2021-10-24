using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Takai.UI
{
    public class StylesDictionary
    {
        public static readonly StylesDictionary Default = new StylesDictionary();

        private readonly Dictionary<StyleHash, Dictionary<StyleHash[], IStyleSheet>> styles = new();

        /// <summary>
        /// styles to apply by automatically, before any other named styles
        /// </summary>
        private readonly Dictionary<Type, IStyleSheet> defaultStyles = new();

        private readonly Dictionary<int, IStyleSheet> renderedStyles = new();

        public int RenderCacheCount => renderedStyles.Count;

        /// <summary>
        /// Import a set of stylesheets
        /// </summary>
        /// <param name="styles">The stylesheets, by name</param>
        public void ImportStyleSheets(Dictionary<string, IStyleSheet> styleSheets)
        {
            // style names are individual words (no spaces allowed)
            // multiple styles (composite) can be defined, A B C, representing a style where all three are set
            // a style sheet can have multiple composites, separated by comma: A B, C D: stylesheet { }
            // Styles are stored as such
            // first style: [ style composite: { style sheet }
            // e.g. A B C, A C stores as A: [ A B C: { ... }, A C: { ... } ]

            foreach (var style in styleSheets)
            {
                var incomingType = style.Value.GetType();

                var splits = style.Key.Split(',');
                foreach (var split in splits)
                {
                    // lots of string alloc in here...
                    var parts = split.Trim().Split(new[] { ' ' });

                    int count = parts.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        var part = parts[i].Trim();
                        if (part.Length == 0 || part[0] == '`')
                        {
                            var typeName = part.Length == 0 ? part : part.Substring(1);
                            // create a helper method (StyleHash does the same thing) ?
                            var type = Data.Serializer.RegisteredTypes[typeName + nameof(StyleSheet)];
                            if (type != incomingType)
                            {
                                throw new InvalidOperationException("Type for default style does not match style name: " +
                                    $"named type={type.Name}, supplied style sheet type={incomingType.Name}");
                            }

                            if (defaultStyles.TryGetValue(type, out var defaultStyle))
                                MergeStyleSheet(defaultStyle.GetType(), defaultStyle, incomingType, style.Value);
                            else
                                defaultStyles[type] = style.Value;

                            // move these entries to the back to be 'pruned'
                            parts[i] = parts[count - 1];
                            --count;
                            --i;
                        }
                    }

                    if (count == 0)
                        continue;

                    // todo: need to correctly handle default (and typed default styles)
                    // they may be filtered out when style matching

                    var sorted = new StyleHash[count];
                    for (int i = 0; i < count; ++i)
                        sorted[i] = StyleHash.FromString(parts[i]);
                    Array.Sort(sorted);

                    if (!styles.TryGetValue(sorted[0], out var refdStyles))
                        refdStyles = styles[sorted[0]] = new Dictionary<StyleHash[], IStyleSheet>(ArrayEqualityComparer<StyleHash>.Compararer);

                    if (refdStyles.TryGetValue(sorted, out var existingStyle))
                    {
                        var existingType = existingStyle.GetType();
                        if (!MergeStyleSheet(existingType, existingStyle, incomingType, style.Value))
                        {
                            throw new InvalidOperationException($"Type mismatch merging '{string.Join(",", sorted)}': stored={existingType.Name}, merging={incomingType.Name}");
                        }
                    }
                    else
                        refdStyles[sorted] = style.Value;

                }
            }
        }

        private static bool MergeStyleSheet(
            Type existingType, 
            IStyleSheet existingStyle, 
            Type incomingType, 
            IStyleSheet incomingStyle)
        {
            if (incomingType != existingType)
                return false;

            var mergeFn = existingType.GetMethod(nameof(IStyleSheet<StyleSheet>.MergeWith));
            mergeFn.Invoke(existingStyle, new[] { incomingStyle });
            return true;
        }

        public TStyleSheet GenerateStyleSheet<TStyleSheet>(IList<StringHash> styleNames)
            where TStyleSheet : struct, IStyleSheet<TStyleSheet>
        {
            TStyleSheet style;
            if (defaultStyles.TryGetValue(typeof(TStyleSheet), out var _style))
                style = (TStyleSheet)_style;
            else
                style = default;

            foreach (var styleName in styleNames)
            {
                if (!styles.TryGetValue(new StyleHash(styleName, typeof(TStyleSheet)), out var refdStyles))
                    continue;

                foreach (var refdStyle in refdStyles)
                {
                    bool matched = true;
                    foreach (var part in refdStyle.Key)
                    {
                        if (!styleNames.Contains(part.stringHash))
                        {
                            matched = false;
                            break;
                        }
                    }

                    if (matched)
                        style.MergeWith((TStyleSheet)refdStyle.Value);
                }
            }

            return style;
        }

        private string DebugGetAllStoredStyleNames()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var sub in styles)
            {
                sb.AppendLine(sub.Key.ToString());
                foreach (var parts in sub.Value)
                {
                    sb.Append(' ');
                    foreach (var part in parts.Key)
                    {
                        sb.Append(' ');
                        sb.Append(part.ToString());
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }

    [DebuggerDisplay("{value} ({hash})")]
    public struct StringHash : IEquatable<StringHash>, IComparable<StringHash>
    {
        public string value;
        public int hash;

        public StringHash(string val)
        {
            value = val;
            hash = StringComparer.OrdinalIgnoreCase.GetHashCode(val);
        }

        public override string ToString() => value;
        public override int GetHashCode() => hash;

        public bool Equals(StringHash other) => hash == other.hash;

        public int CompareTo(StringHash other) => hash.CompareTo(other.hash);
    }

    [DebuggerDisplay("{stringHash}`{type.Name}")]
    struct StyleHash : IEquatable<StyleHash>, IComparable<StyleHash>
    {
        public StringHash stringHash;
        public Type type;

        public StyleHash(StringHash stringHash, Type type)
        {
            this.stringHash = stringHash;
            this.type = type;
        }

        public static StyleHash FromString(string input)
        {
            var backTick = input.IndexOf('`');
            if (backTick < 0)
                return new StyleHash(new StringHash(input), typeof(StyleSheet));

            var type = Data.Serializer.RegisteredTypes[input.Substring(backTick + 1) + nameof(StyleSheet)];
            return new StyleHash(new StringHash(input.Substring(0, backTick)), type);
        }

        public override string ToString() => $"{stringHash}`{type.Name}";
        public override int GetHashCode() => ArrayEqualityComparer<int>.CombineHashCodes(stringHash.hash, type.GetHashCode());

        public bool Equals(StyleHash other) => stringHash.Equals(other.stringHash) && type == other.type;

        public int CompareTo(StyleHash other)
        {
            var hash = stringHash.CompareTo(other.stringHash);
            if (hash == 0)
                return (int)(type.TypeHandle.Value.ToInt64() - other.type.TypeHandle.Value.ToInt64()); //gross
            return hash;
        }
    }
}
