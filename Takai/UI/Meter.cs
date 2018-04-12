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
        public Graphics.Sprite Mask
        {
            get => _mask;
            set
            {
                _mask = value;
                maskEffect.Parameters["Mask"].SetValue(_mask.Texture);
            }
        }
        private Graphics.Sprite _mask;
        public float MaskValue { get; set; } = 1;

        //clip (display part of image based on value) ?

        public int RepeatCount { get; set; } = 1;
        public Vector2 RepeatOffset { get; set; }
        public float RepeatAngle { get; set; } = 0;

        Effect maskEffect;
        SpriteBatch sbatch;

        public Meter()
        {
            maskEffect = Data.Cache.Load<Effect>("Shaders/ValueMask.mgfx"); //todo: don't hard code
            sbatch = new SpriteBatch(Runtime.GraphicsDevice);
        }

        public override void AutoSize(float padding = 0)
        {
            Rectangle bounds = new Rectangle();
            Point drawPos = VisibleBounds.Location;
            for (int i = 0; i < RepeatCount; ++i)
            {
                bounds = Rectangle.Union(bounds, new Rectangle(drawPos.X, drawPos.Y, Sprite.Width, Sprite.Height));
                var offset = RepeatOffset;
                if (RepeatAngle != 0)
                    offset = Vector2.TransformNormal(offset, Matrix.CreateRotationZ(RepeatAngle));
                drawPos += offset.ToPoint();
            }
            bounds.Inflate((int)padding, (int)padding);

            Size = bounds.Size.ToVector2();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            bool isMasking = (MaskValue < 1 && Mask != null);

            if (isMasking)
            {
                maskEffect.Parameters["Cutoff"].SetValue(MaskValue);
                sbatch.Begin(SpriteSortMode.Deferred, null, null, null, null, maskEffect);
                spriteBatch = sbatch;
            }

            Point drawPos = VisibleBounds.Location;
            for (int i = 0; i < RepeatCount; ++i)
            {
                Runtime.GraphicsDevice.Textures[1] = Mask.Texture;
                Sprite.Draw(spriteBatch, Rectangle.Intersect(VisibleBounds, new Rectangle(drawPos.X, drawPos.Y, Sprite.Width, Sprite.Height)), 0);
                var offset = RepeatOffset;
                if (RepeatAngle != 0)
                    offset = Vector2.TransformNormal(offset, Matrix.CreateRotationZ(RepeatAngle));
                drawPos += offset.ToPoint();
            }

            if (isMasking)
                sbatch.End();
        }
    }
}
