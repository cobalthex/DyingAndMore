﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    /// <summary>
    /// Display a graphic in a variey of ways according to a value
    /// </summary>
    public class Meter : Static
    {
        public Graphics.Sprite Sprite { get; set; }

        /// <summary>
        /// Mask the graphic with this
        /// </summary>
        public Graphics.Sprite Mask { get; set; }

        /// <summary>
        /// The cutoff value for the mask (display anything less than this)
        /// Or, see <seealso cref="Range"/>
        /// </summary>
        public float Value { get; set; } = 1;
        /// <summary>
        /// THe range to pad around the cutoff value. If 0, <see cref="Value"/> acts as a cutoff
        /// If != 0, it behaves as a range and this is a padding factor (*2 for above and below <see cref="Value"/>)
        /// </summary>
        public float Range { get; set; } = 0;

        public string ValueBinding { get; set; }

        Effect maskEffect;
        SpriteBatch sbatch;

        public Meter()
        {
            maskEffect = Data.Cache.Load<Effect>("Shaders/AlphaTest.mgfx"); //todo: don't hard code
            sbatch = new SpriteBatch(Runtime.GraphicsDevice);
        }

        public override void AutoSize(float padding = 0)
        {
            Size = (Sprite?.Size.ToVector2() ?? new Vector2(1)) + new Vector2(padding);
        }

        protected override void UpdateSelf(GameTime time)
        {
            if (ValueBinding != null && Data.DataModel.Values.TryGetValue(ValueBinding, out var bindValue))
            {
                Value = System.Convert.ToSingle(bindValue);
            }

            base.UpdateSelf(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            maskEffect.Parameters["Cutoff"].SetValue(Value);
            maskEffect.Parameters["Range"].SetValue(Range);
            maskEffect.Parameters["Mask"].SetValue(Mask.Texture);
            sbatch.Begin(SpriteSortMode.Deferred, null, null, null, null, maskEffect);
            Sprite.Draw(sbatch, VisibleBounds, 0, Color, Sprite.ElapsedTime);
            sbatch.End();
        }
    }
}
