using System;
using System.Reflection;
using System.Collections.Generic;

using StylesheetOld = System.Collections.Generic.Dictionary<string, object>;
using Microsoft.Xna.Framework;
using Takai.Graphics;
using System.Runtime.CompilerServices;
using Takai.Data;

namespace Takai.UI
{
    public partial class Static
    {
        /// <summary>
        /// All available/known styles (Apply custom styles using <see cref="ApplyStyles(StylesheetOld, Static)"/>)
        /// </summary>
        public static Dictionary<string, StylesheetOld> Styles { get; set; }

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

        public StyleSheet Stylez { get; set; }

        public DynamicBitSet32 StyleStates { get; } = new DynamicBitSet32(3); // See DefaultStyleStates for preset slots

        public static StyleSheet BaseStyle = new StyleSheet
        {
            color = Color.White,
            font = null, // needs to be set at runtime
            textStyle = new TextStyle(),
            borderColor = Color.Transparent,
            backgroundColor = Color.Transparent,
            backgroundSprite = null,

            // don't mandate these by default
            horizontalAlignment = null,
            verticalAlignment = null,
            position = null,
            size = null,
            padding = null,
        };

        /// <summary>
        /// Apply a style, applying the default and/or focus state automatically
        /// </summary>
        /// <param name="state">The custom state to apply (Focus is automatically applied)</param>
        /// <param name="force">Apply this state even if its already applied</param>
        protected void ApplyStyle(bool force = false)
        {
            StyleSheet style = BaseStyle;
            style.MergeWith(Stylez);

            // TESTING
            if (StyleStates[(byte)DefaultStyleStates.Focus])
                style.MergeWith(new StyleSheet { borderColor = Color.Gold });
            if (StyleStates[(byte)DefaultStyleStates.Hover])
                style.MergeWith(new StyleSheet { backgroundColor = Color.DarkSlateBlue });
            if (StyleStates[(byte)DefaultStyleStates.Press])
                style.MergeWith(new StyleSheet { backgroundColor = Color.DeepSkyBlue });

            if (style.color.HasValue) Color = style.color.Value;
            if (style.font != null) Font = style.font;
            if (style.textStyle.HasValue) TextStyle = style.textStyle.Value;
            if (style.borderColor.HasValue) BorderColor = style.borderColor.Value;
            if (style.backgroundColor.HasValue) BackgroundColor = style.backgroundColor.Value;
            if (style.backgroundSprite != null) BackgroundSprite = style.backgroundSprite;
            if (style.horizontalAlignment.HasValue) HorizontalAlignment = style.horizontalAlignment.Value;
            if (style.verticalAlignment.HasValue) VerticalAlignment = style.verticalAlignment.Value;
            if (style.position.HasValue) Position = style.position.Value;
            if (style.size.HasValue) Size = style.size.Value;
            if (style.padding.HasValue) Padding = style.padding.Value;

            // todo: store list of style states to apply (maybe make enum system (register ID, slot 1 << ID)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T GetStyleValue<T>(T? value, T fallback) where T : struct =>
            value.GetValueOrDefault(fallback);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T GetStyleValue<T>(T value, T fallback) where T : class =>
            value ?? fallback;

        public virtual void ApplyStyleSheet(StyleSheet styleSheet)
        {
            Color = GetStyleValue(styleSheet.color, Color);
            Font = GetStyleValue(styleSheet.font, Font);
            TextStyle = GetStyleValue(styleSheet.textStyle, TextStyle);

            BorderColor = GetStyleValue(styleSheet.borderColor, BorderColor);
            BackgroundColor = GetStyleValue(styleSheet.backgroundColor, BackgroundColor);
            BackgroundSprite = GetStyleValue(styleSheet.backgroundSprite, BackgroundSprite);
            if (BackgroundSprite.Sprite != null)
                BackgroundSprite.Sprite.ElapsedTime = TimeSpan.Zero;

            Padding = GetStyleValue(styleSheet.padding, Padding);
            HorizontalAlignment = GetStyleValue(styleSheet.horizontalAlignment, HorizontalAlignment);
            VerticalAlignment = GetStyleValue(styleSheet.verticalAlignment, VerticalAlignment);
            Position = GetStyleValue(styleSheet.position, Position);
            Size = GetStyleValue(styleSheet.size, Size);

            //todo: evaluate perf cost
        }


        public static void MergeStyleRules(IEnumerable<KeyValuePair<string, StylesheetOld>> stylesheets)
        {
            if (Styles == null)
                Styles = new Dictionary<string, StylesheetOld>();

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

        public static Dictionary<string, IStyleSheet> StylesDictionary { get; } = new Dictionary<string, UI.IStyleSheet>();

        protected TStyleSheet GetStyleSheet<TStyleSheet>(string styleName) where TStyleSheet : struct, IStyleSheet<TStyleSheet>
        {
            var thisType = GetType();
            if (thisType != typeof(Static))
                styleName = $"{styleName}.{thisType.Name}";

            if (StylesDictionary.TryGetValue(styleName, out var style))
                return (TStyleSheet)style;

            return default; //todo: default style type
        }

        protected virtual void ApplyStyleSheet()
        {
            BaseStyle = GetStyleSheet<StyleSheet>("ZippidyDooDa");
        }
    }

    public interface IStyleSheet : IReferenceable { }

    public interface IStyleSheet<TStyleSheet> : IStyleSheet where TStyleSheet : struct
    {
        /// <summary>
        /// Lerp between this and another stylesheet. If a value is missing, the other is taken
        /// </summary>
        /// <param name="other">The style to lerp with</param>
        /// <param name="t">0.0->1.0 from this->other</param>
        void LerpWith(TStyleSheet other, float t);

        /// <summary>
        /// Merge two styles, overwritting the current with <see cref="other"/> if set
        /// </summary>
        /// <param name="other">The style to merge from</param>
        void MergeWith(TStyleSheet other);
    }

    public struct StyleSheet : IReferenceable, IStyleSheet<StyleSheet>
    {
        public string Name { get; set; }

        public IStyleSheet<StyleSheet> proto;
     
        public Color? color;
        public Font font;
        public TextStyle? textStyle;
        
        public Color? borderColor;
        public Color? backgroundColor;
        public Sprite backgroundSprite;

        public Alignment? horizontalAlignment;
        public Alignment? verticalAlignment;

        public Vector2? position;
        public Vector2? size;
        public Vector2? padding;

        public void LerpWith(StyleSheet other, float t)
        {
            throw new NotImplementedException(); // todo
        }

        public void MergeWith(StyleSheet other)
        {
            if (other.color.HasValue) color = other.color;
            if (other.font != null) font = other.font;
            if (other.textStyle.HasValue) textStyle = other.textStyle;
            if (other.borderColor.HasValue) borderColor = other.borderColor;
            if (other.backgroundColor.HasValue) backgroundColor = other.backgroundColor;
            if (other.backgroundSprite != null) backgroundSprite = other.backgroundSprite;
            if (other.horizontalAlignment.HasValue) horizontalAlignment = other.horizontalAlignment;
            if (other.verticalAlignment.HasValue) verticalAlignment = other.verticalAlignment;
            if (other.position.HasValue) position = other.position;
            if (other.size.HasValue) size = other.size;
            if (other.padding.HasValue) padding = other.padding;
        }
    }

    public enum DefaultStyleStates : byte
    {
        Focus = 0,
        Hover = 1,
        Press = 2,
    }
}