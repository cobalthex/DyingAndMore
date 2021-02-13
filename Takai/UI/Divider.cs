using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public class Divider : Static
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

        public Divider() : this(Direction.Horizontal) { }
        public Divider(Direction direction) : base()
        {
            if (direction == Direction.Horizontal)
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

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return Vector2.One;
        }

        protected override void DrawSelf(DrawContext context)
        {
            Graphics.Primitives2D.DrawFill(context.spriteBatch, Color, VisibleContentArea);
        }
    }
}
