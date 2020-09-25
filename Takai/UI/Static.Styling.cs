using System;
using System.Reflection;
using System.Collections.Generic;

using Stylesheet = System.Collections.Generic.Dictionary<string, object>;

namespace Takai.UI
{
    public partial class Static
    {
        /// <summary>
        /// All available/known styles (Apply custom styles using <see cref="ApplyStyles(Stylesheet, Static)"/>)
        /// </summary>
        public static Dictionary<string, Stylesheet> Styles { get; set; }

        const string DefaultStyleName = nameof(Static);

        public string Style
        {
            get => _style;
            set
            {
                if (_style == value)
                    return;

                _style = value;
                ApplyStyles(GetStyles(_style));
                //todo: this needs to clear styles when switching
            }
        }
        private string _style;

        protected T GetStyleRule<T>(Stylesheet styleRules, string propName, T fallback)
        {
            //Cast?
            if (styleRules == null ||
                !styleRules.TryGetValue(propName, out var sProp))
                return fallback;

            if (sProp is T prop)
                return prop;

            if (sProp == null)
                return default;

            if (Data.Serializer.TryNumericCast(sProp, out var destNumber, sProp.GetType().GetTypeInfo(), typeof(T).GetTypeInfo()))
                return (T)destNumber;

            return fallback;
        }

        public static Stylesheet GetStyles(string styleName, string styleState = null)
        {
            Stylesheet styles = null;
            if (styleName != null)
            {
                styleName += (styleState != null ? ("+" + styleState) : null);
                Styles.TryGetValue(styleName, out styles); //todo: revamp
            }
            return styles;
        }


        public void ApplyStateStyle(string styleName = null)
        {
            ApplyStyles(GetStyles(styleName ?? Style));
            if (HasFocus)
                ApplyStyles(GetStyles(Style, "Focus"));
            if (HoveredElement == this)
            {
                ApplyStyles(GetStyles(Style, "Hover"));
                if (didPress.Data != 0) //should ideally only be mouse 0 and touch 0
                    ApplyStyles(GetStyles(Style, "Press"));
            }
        }

        public virtual void ApplyStyles(Stylesheet styleRules)
        {
            Color = GetStyleRule(styleRules, "Color", Color);
            Font = GetStyleRule(styleRules, "Font", Font);
            TextStyle = GetStyleRule(styleRules, "TextStyle", TextStyle);

            BorderColor = GetStyleRule(styleRules, "BorderColor", BorderColor);
            BackgroundColor = GetStyleRule(styleRules, "BackgroundColor", BackgroundColor);
            BackgroundSprite = GetStyleRule(styleRules, "BackgroundSprite", BackgroundSprite);
            if (BackgroundSprite.Sprite != null)
                BackgroundSprite.Sprite.ElapsedTime = TimeSpan.Zero;

            Padding = GetStyleRule(styleRules, "Padding", Padding);
            HorizontalAlignment = GetStyleRule(styleRules, "HorizontalAlignment", HorizontalAlignment);
            VerticalAlignment = GetStyleRule(styleRules, "VerticalAlignment", VerticalAlignment);
            Position = GetStyleRule(styleRules, "Position", Position);
            Size = GetStyleRule(styleRules, "Size", Size);

            //base on rules
        }

        public static void MergeStyleRules(Dictionary<string, Stylesheet> stylesheets)
        {
            if (Styles == null)
                Styles = new Dictionary<string, Stylesheet>(stylesheets.Count * 3 / 2);

            foreach (var rules in stylesheets)
            {
                var keys = rules.Key.Split(',');
                foreach (var _key in keys)
                {
                    //normalize spacing around +?
                    var key = _key.Trim();
                    if (Styles.TryGetValue(key, out var sheet))
                    {
                        foreach (var rule in rules.Value)
                            sheet[key] = rule.Value;
                    }
                    else
                        Styles.Add(key, rules.Value);

                    foreach (var state in new[] { "Hover", "Press", "Focus" })
                    {
                        var skey = key + "+" + state;
                        if (!Styles.ContainsKey(skey))
                            Styles.Add(skey, new Stylesheet());
                    }
                }
            }

            //todo: this sucks, use a trie/something

            //apply Default style and proto styles to all elements
            foreach (var rules in stylesheets)
            {
                var keys = rules.Key.Split(',');
                if (keys.Length == 0)
                    continue;

                var proto = DefaultStyleName;
                if (rules.Value.TryGetValue("proto", out object oProto))
                    proto = oProto as string ?? proto; //print error if not string?

                //need to enumerate keys?

                string state = null;
                var statePair = keys[0].Split('+');
                if (statePair[0] == proto)
                    continue;

                if (statePair.Length > 1)
                    state = statePair[1];

                var protoRules = GetStyles(proto, state);
                if (protoRules == null)
                    continue;

                foreach (var pRule in protoRules)
                {
                    if (!rules.Value.ContainsKey(pRule.Key))
                        rules.Value.Add(pRule.Key, pRule.Value);
                }
            }
        }
    }
}
