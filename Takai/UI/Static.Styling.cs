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
                //ApplyStateStyle();
                ApplyStyle(force: true);
                //todo: this needs to clear styles when switching
            }
        }
        private string _style;
        private string lastStyleState;

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

        /// <summary>
        /// Apply a style, applying the default and/or focus state automatically
        /// </summary>
        /// <param name="state">The custom state to apply (Focus is automatically applied)</param>
        /// <param name="force">Apply this state even if its already applied</param>
        protected void ApplyStyle(string state = null, bool force = false)
        {
            if (lastStyleState == state && !force)
                return;

            lastStyleState = state;

            if (state == null)
                ApplyStyleRecursive(Style); //always apply?

            if (HasFocus)
                ApplyStyleRecursive(Style, "Focus"); //option to disable?

            if (state != null)
                ApplyStyleRecursive(Style, state);
        }

        /// <summary>
        /// Applies a style, setting proto/default base style recrusively
        /// </summary>
        /// <param name="style">The style name to apply</param>
        /// <param name="state">The state of that style</param>
        private void ApplyStyleRecursive(string style, string state = null)
        {
            System.Diagnostics.Debug.WriteLine($"{DebugId}: Applying style {style}+{state} - {lastStyleState}");

            var rules = GetStyles(style, state);
            if (rules == null)
            {
                if (style != DefaultStyleName)
                    ApplyStyleRecursive(DefaultStyleName, state);
                return;
            }

            if (style != DefaultStyleName)
            {
                if (!rules.TryGetValue("proto", out var proto))
                    proto = DefaultStyleName;

                if (proto is string sProto)
                    ApplyStyleRecursive(sProto, state);
            }

            ApplyStyleRules(rules);
        }

        protected virtual void ApplyStyleRules(Stylesheet styleRules)
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

            //todo: evaluate perf cost
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
                }
            }

            //merge proto styles into styles? more data but better perf: no lookups, no double setting
        }
    }
}


//StyleStates {Normal,Pressed,Hover,Focus}

//Key -> StyleStates
//


//todo: convert styles to typed on load