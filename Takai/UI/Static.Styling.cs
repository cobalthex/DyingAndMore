using System;
using System.Collections.Generic;
using System.Linq;

namespace Takai.UI
{
    public partial class Static
    {
        private bool isStyleValid = true;

        /// <summary>
        /// A space separated list of the current styles. Don't call directly
        /// Set individual styles via <see cref="SetStyle(string, bool)"/>
        /// </summary>
        public string Styles // string b/c most elements will have a single style -- TODO re-evaluate
        {
            get => string.Join(" ", _styleHashes.Select(s => s.value)); // evaluate where this is called
            set
            {
                _styleHashes = (value ?? "").Split(' ').Distinct().Select(s => new StringHash(s)).ToList();
                InvalidateStyle();
            }
        }
        private List<StringHash> _styleHashes = new();

        // custom style object for setting properties?

        protected TimeSpan currentStateElapsedTime;

        /// <summary>
        /// Set or cleer a style state
        /// </summary>
        /// <param name="style">The name of style to set, it should be in PascalCase</param>
        /// <param name="add">set or clear the style? (setting twice is idempotent)</param>
        protected void SetStyle(string style, bool add)
        {
            var stringHash = new StringHash(style);
            if (add && _styleHashes.FindIndex(st => st.Equals(stringHash)) < 0)
            {
                _styleHashes.Add(stringHash);
                InvalidateStyle();
                return;
            }

            if (!add && _styleHashes.Remove(stringHash))
                InvalidateStyle();
        }

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
            //System.Diagnostics.Debug.WriteLine($"Syling {DebugId} with states {string.Join(",", styleStates)}");
            ApplyStyleOverride();
            isStyleValid = true;
            currentStateElapsedTime = TimeSpan.Zero;
        }

        /// <summary>
        /// Apply a style, overridable to support custom IStyleSheets
        /// </summary>
        protected virtual void ApplyStyleOverride()
        {
            var style = GenerateStyleSheet<StyleSheet>();

            // todo: explicit optional type for ref types

            if (style.Color.HasValue) Color = style.Color.Value;
            if (style.Font != null) Font = style.Font;
            if (style.TextStyle.HasValue) TextStyle = style.TextStyle.Value;
            if (style.BorderColor.HasValue) BorderColor = style.BorderColor.Value;
            if (style.BackgroundColor.HasValue) BackgroundColor = style.BackgroundColor.Value;
            if (style.BackgroundSprite.HasValue) BackgroundSprite = style.BackgroundSprite.Value;
            if (style.HorizontalAlignment.HasValue) HorizontalAlignment = style.HorizontalAlignment.Value;
            if (style.VerticalAlignment.HasValue) VerticalAlignment = style.VerticalAlignment.Value;
            if (style.Position.HasValue) Position = style.Position.Value;
            if (style.Size.HasValue) Size = style.Size.Value;
            if (style.Padding.HasValue) Padding = style.Padding.Value;
        }

        protected TStyleSheet GenerateStyleSheet<TStyleSheet>()
            where TStyleSheet : struct, IStyleSheet<TStyleSheet>
            => StylesDictionary.Default.GenerateStyleSheet<TStyleSheet>(_styleHashes);
    }
}