using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.UI;

namespace DyingAndMore.UI
{
    class Divider : Static
    {
        public Direction Direction
        {
            get => _direction;
            set
            {
                if (_direction == value)
                    return;

                _direction = value;
                if (_direction == Direction.Horizontal)
                {
                    HorizontalAlignment = Alignment.Stretch;
                    VerticalAlignment = Alignment.Center;
                }
                else
                {
                    HorizontalAlignment = Alignment.Center;
                    VerticalAlignment = Alignment.Stretch;
                }
            }
        }
        private Direction _direction;

        public Divider() : base()
        {
            HorizontalAlignment = Alignment.Stretch;
            VerticalAlignment = Alignment.Center;

            Color = new Color(Color, 127);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return Vector2.One;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, Color, VisibleContentArea);
        }
    }
}
