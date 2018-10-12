﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Graphic : Static
    {
        public Graphics.Sprite Sprite { get; set; }

        /// <summary>
        /// Draw an X in the graphic if the sprite is missing
        /// </summary>
        public bool DrawXIfMissingSprite { get; set; } = false;

        System.TimeSpan elapsedTime;

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return (Sprite == null ? Vector2.Zero : Sprite.Size.ToVector2());
        }

        protected override void UpdateSelf(GameTime time)
        {
            elapsedTime = time.TotalGameTime;
            base.UpdateSelf(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //todo: modernize

            //todo: custom positioning/sizing
            if (Sprite?.Texture != null)
            {
                var clip = VisibleContentArea;
                clip.X -= OffsetContentArea.X;
                clip.Y -= OffsetContentArea.Y;
                var dest = VisibleContentArea;
                dest.X += clip.X / 2;
                dest.Y += clip.Y / 2;
                Sprite.Draw(spriteBatch, dest, clip, 0, Color.White, elapsedTime);
            }
            else if (DrawXIfMissingSprite)
            {
                var rect = VisibleContentArea;
                rect.Inflate(-4, -4);
                Graphics.Primitives2D.DrawX(spriteBatch, Color.Tomato, rect);
            }

            base.DrawSelf(spriteBatch);
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
