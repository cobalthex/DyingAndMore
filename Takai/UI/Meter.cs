﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    /// <summary>
    /// Display a graphic in a variey of ways according to a value
    /// </summary>
    public class Meter : NumericBase
    {
        // 9 patch?

        public Graphics.Sprite Sprite { get; set; }

        /// <summary>
        /// Sprite is masked with this via red channel intensity.
        /// Note: this must share the same clip rect as Sprite
        /// </summary>
        public Graphics.Sprite Mask { get; set; }

        /// <summary>
        /// The range to pad around the cutoff value. If 0, <see cref="Value"/> acts as a cutoff
        /// If != 0, it behaves as a range and this is a padding factor (*2 for above and below <see cref="Value"/>)
        /// </summary>
        public float BandPass { get; set; } = 0;

        Effect maskEffect;
        SpriteBatch sbatch;

        public Meter()
        {
            maskEffect = Data.Cache.Load<Effect>("Shaders/AlphaTest.mgfx"); //todo: don't hard code
            sbatch = new SpriteBatch(Runtime.GraphicsDevice);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return Sprite == null ? Vector2.Zero : Sprite.Size.ToVector2();
        }

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);

            maskEffect.Parameters["Cutoff"].SetValue(NormalizedValue);
            maskEffect.Parameters["Range"].SetValue(BandPass);
#if OPENGL
            maskEffect.Parameters["Sampler+Mask"].SetValue(Mask.Texture);
#else
            maskEffect.Parameters["Mask"].SetValue(Mask.Texture);
            maskEffect.Parameters["MaskOffset"].SetValue(Mask.ClipRect.Location.ToVector2() /
                new Vector2(Mask.Texture.Width, Mask.Texture.Height));
#endif
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, maskEffect);
            DrawSprite(sbatch, Sprite, new Rectangle(0, 0, ContentArea.Width - 1, ContentArea.Height - 1));
            sbatch.End();
        }
    }
}
