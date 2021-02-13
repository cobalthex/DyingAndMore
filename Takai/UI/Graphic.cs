using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Graphic : Static
    {
        public string RestartCommand = "Restart";

        public Graphics.Sprite Sprite
        {
            get => _sprite;
            set
            {
                if (_sprite == value)
                    return;

                _sprite = value;
                InvalidateMeasure();
            }
        }
        private Graphics.Sprite _sprite;

        /// <summary>
        /// Stretch the sprite to fit the container, or center
        /// </summary>
        public bool StretchToFit { get; set; }

        /// <summary>
        /// If not transparent, draws and X if the <see cref="Sprite"/> is missing
        /// </summary>
        public Color MissingSpriteXColor { get; set; } = Color.Transparent;

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return (Sprite?.Size.ToVector2() ?? Vector2.Zero);
        }

        public Graphic()
        {
            CommandActions[RestartCommand] = delegate (Static sender, object arg)
            {
                Sprite.ElapsedTime = System.TimeSpan.Zero;
            };
        }

        public Graphic(Graphics.Sprite sprite)
            : this()
        {
            Sprite = sprite;
        }

        protected override void UpdateSelf(GameTime time)
        {
            if (Sprite != null)
                Sprite.ElapsedTime = time.TotalGameTime;
            base.UpdateSelf(time);
        }

        protected override void DrawSelf(DrawContext context)
        {
            //todo: modernize

            //todo: custom positioning/sizing
            if (Sprite?.Texture != null)
            {
                Rectangle bounds;
                if (StretchToFit)
                    bounds = new Rectangle(0, 0, ContentArea.Width, ContentArea.Height);
                else
                {
                    var size = new Point(
                        System.Math.Min(Sprite.Width, ContentArea.Width),
                        System.Math.Min(Sprite.Height, ContentArea.Height)
                    );
                    bounds = new Rectangle((ContentArea.Width - size.X) / 2, (ContentArea.Height - size.Y) / 2, size.X, size.Y);
                }
                DrawSprite(context.spriteBatch, Sprite, bounds);
            }
            else if (MissingSpriteXColor.A > 0)
            {
                var rect = VisibleContentArea;
                rect.Inflate(-8, -8);
                Graphics.Primitives2D.DrawX(context.spriteBatch, MissingSpriteXColor, rect);

                //todo: clip
            }

            base.DrawSelf(context);
        }

        protected override void ApplyStyleRules(Dictionary<string, object> styleRules)
        {
            base.ApplyStyleRules(styleRules);
            MissingSpriteXColor = GetStyleRule(styleRules, "MissingSpriteXColor", MissingSpriteXColor);
        }

        public override void DerivedDeserialize(Dictionary<string, object> props)
        {
            base.DerivedDeserialize(props);

            if (Sprite == null)
                return;

            //todo: this should go into measure

            //proportional scaling if only one property is given

            var hasWidth = props.TryGetValue("Width", out var _width);
            var hasHeight = props.TryGetValue("Height", out var _height);

            if (hasWidth && !hasHeight)
            {
                var width = Data.Serializer.Cast<float>(_width);
                Size = new Vector2(width, width * Sprite.Height / Sprite.Width);
            }
            else if (!hasWidth && hasHeight)
            {
                var height = Data.Serializer.Cast<float>(_height);
                Size = new Vector2(height * Sprite.Width / Sprite.Height, height);
            }
        }
    }
}
