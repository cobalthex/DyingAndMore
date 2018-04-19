using Microsoft.Xna.Framework;
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
        public float Value { get; set; } = 1;

        Effect maskEffect;
        SpriteBatch sbatch;

        public Meter()
        {
            maskEffect = Data.Cache.Load<Effect>("Shaders/ValueMask.mgfx"); //todo: don't hard code
            sbatch = new SpriteBatch(Runtime.GraphicsDevice);
        }

        public override void AutoSize(float padding = 0)
        {
            Size = (Sprite?.Size.ToVector2() ?? new Vector2(1)) + new Vector2(padding);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            bool isMasking = (Value < 1 && Mask != null);

            sbatch.Begin(SpriteSortMode.Deferred, null, null, null, null, maskEffect);
            maskEffect.Parameters["Cutoff"].SetValue(Value);
            maskEffect.Parameters["Mask"].SetValue(Mask.Texture);
            var bounds = VisibleBounds; // Rectangle.Intersect(VisibleBounds, new Rectangle( X, (int)Position.Y, Sprite.Width, Sprite.Height));
            Sprite.Draw(spriteBatch, bounds, 0, Color, Sprite.ElapsedTime);
            sbatch.End();
        }
    }
}
