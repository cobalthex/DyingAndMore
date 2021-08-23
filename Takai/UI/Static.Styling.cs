using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Takai.Graphics;
using Takai.Data;

namespace Takai.UI
{
    public partial class Static
    {
        private bool isStyleValid = true;

        const string DefaultStyleName = nameof(Static);

        public string Style
        {
            get => _styleName;
            set
            {
                if (_styleName == value)
                    return;

                _styleName = value;
                InvalidateStyle();
            }
        }
        private string _styleName;

        // custom style object for setting properties?

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

        protected void InvalidateStyle()
        {
            if (!isStyleValid)
                return;

            isStyleValid = false;
            restyleQueue.Add(this);
        }

        /// <summary>
        /// Generate and apply the current style state (Calculated)
        /// Called by <see cref="PerformReflows"/>
        /// </summary>
        public void ApplyStyle()
        {
            ApplyStyleOverride();
            isStyleValid = true;
        }

        protected virtual void ApplyStyleOverride()
        {
            StyleSheet style = BaseStyle;

            style.MergeWith(GetStyleSheet<StyleSheet>(Style));

            // store these style states in a DynamicBitSet?

            // todo: extending this is too manual

            if (HasFocus)
                style.MergeWith(GetStyleSheet<StyleSheet>(Style, nameof(DefaultStyleStates.Focus)));

            if (HoveredElement == this)
            {
                style.MergeWith(GetStyleSheet<StyleSheet>(Style, nameof(DefaultStyleStates.Hover)));

                if (didPress.Data != 0)
                    style.MergeWith(GetStyleSheet<StyleSheet>(Style, nameof(DefaultStyleStates.Press)));
            }

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
        }

        public static Dictionary<string, IStyleSheet> StylesDictionary { get; } = new Dictionary<string, UI.IStyleSheet>();

        /// <summary>
        /// Import a set of stylesheets
        /// </summary>
        /// <param name="styles">The stylesheets, by name, see remarks</param>
        /// <remarks>Style names are in the format of StyleName[+DerivedType]@State, e.g. TestList+List@Hover</remarks>
        public static void ImportStyleSheets(Dictionary<string, IStyleSheet> styles)
        {
            foreach (var style in styles)
            {
                // TODO: merge (might have to be reflection based)
                StylesDictionary[style.Key] = style.Value;
            }
        }

        protected TStyleSheet GetStyleSheet<TStyleSheet>(string styleName, string styleState = null) 
            where TStyleSheet : struct, IStyleSheet<TStyleSheet>
        {
            var sheetType = typeof(TStyleSheet).Name.Replace("StyleSheet", "");
            if (sheetType.Length > 0)
                styleName = $"{styleName}+{sheetType}";
            if (styleState != null)
                styleName += "@" + styleState;

            if (styleName != null && StylesDictionary.TryGetValue(styleName, out var style))
                return (TStyleSheet)style;

            return default; //todo: default style type
        }
    }

    public interface IStyleSheet { }

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

    public struct StyleSheet : IStyleSheet<StyleSheet>
    {
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

    [Flags]
    public enum DefaultStyleStates
    {
        Focus = 0,
        Hover = 1,
        Press = 2,
    }
}