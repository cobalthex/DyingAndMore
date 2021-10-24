using Microsoft.Xna.Framework;
using Takai.Data;
using Takai.Graphics;

using Xna = Microsoft.Xna.Framework;

namespace Takai.UI
{
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

    public struct StyleSheet : IStyleSheet<StyleSheet>
    {
        public string Name { get; set; }

        public Font Font;
        public TextStyle? TextStyle;

        public Color? Color;
        public Color? BorderColor;
        public Color? BackgroundColor;
        public NinePatch? BackgroundSprite;

        public Alignment? HorizontalAlignment;
        public Alignment? VerticalAlignment;

        public Vector2? Position;
        public Vector2? Size;
        public Vector2? Padding;

        public void LerpWith(StyleSheet other, float t)
        {
            TextStyle = Util.TryLerp(TextStyle, other.TextStyle, Graphics.TextStyle.Lerp, t);

            Color = Util.TryLerp(Color, other.Color, Xna.Color.Lerp, t);
            BorderColor = Util.TryLerp(BorderColor, other.BorderColor, Xna.Color.Lerp, t);
            BackgroundColor = Util.TryLerp(BackgroundColor, other.BackgroundColor, Xna.Color.Lerp, t);

            Position = Util.TryLerp(Position, other.Position, Vector2.Lerp, t);
            Size = Util.TryLerp(Size, other.Size, Vector2.Lerp, t);
            Padding = Util.TryLerp(Padding, other.Padding, Vector2.Lerp, t);
        }

        public void MergeWith(StyleSheet other)
        {
            if (other.Color.HasValue) Color = other.Color;
            if (other.Font != null) Font = other.Font;
            if (other.TextStyle.HasValue) TextStyle = other.TextStyle;
            if (other.BorderColor.HasValue) BorderColor = other.BorderColor;
            if (other.BackgroundColor.HasValue) BackgroundColor = other.BackgroundColor;
            if (other.BackgroundSprite.HasValue) BackgroundSprite = other.BackgroundSprite;
            if (other.HorizontalAlignment.HasValue) HorizontalAlignment = other.HorizontalAlignment;
            if (other.VerticalAlignment.HasValue) VerticalAlignment = other.VerticalAlignment;
            if (other.Position.HasValue) Position = other.Position;
            if (other.Size.HasValue) Size = other.Size;
            if (other.Padding.HasValue) Padding = other.Padding;
        }
    }
}