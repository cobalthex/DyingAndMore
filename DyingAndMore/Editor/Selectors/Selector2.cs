using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.UI;

namespace DyingAndMore.Editor.Selectors
{
    class ScrollBar : Static
    {
        static int Margin = 4; //todo: apply correctly

        /// <summary>
        /// The size of the content. This determines how big the scroll thumb is
        /// </summary>
        public int ContentSize { get; set; } = 1;

        /// <summary>
        /// Where the content is scrolled to
        /// </summary>
        public int ContentPosition
        {
            get => contentPosition;
            set
            {
                if (Direction == Direction.Vertical)
                    contentPosition = MathHelper.Clamp(value, 0, ContentSize - (int)Size.Y);
                else if (Direction == Direction.Horizontal)
                    contentPosition = MathHelper.Clamp(value, 0, ContentSize - (int)Size.X);
                else
                    contentPosition = value;
            }
        }
        private int contentPosition = 0;

        /// <summary>
        /// Which direction the scrollbar moves
        /// </summary>
        public Direction Direction { get; set; } = Direction.Vertical;

        protected bool didPressThumb = false;
        protected Point thumbPressOffset;

        public ScrollBar()
        {
            OutlineColor = Color;
        }

        protected override bool UpdateSelf(GameTime time)
        {
            var mouse = InputState.MousePoint - AbsoluteBounds.Location;

            if (InputState.IsButtonUp(MouseButtons.Left))
                didPressThumb = false;

            if (didPressThumb)
            {
                if (Direction == Direction.Vertical)
                    ContentPosition = (int)((mouse.Y - thumbPressOffset.Y) * (ContentSize / Size.Y));
                else if (Direction == Direction.Horizontal)
                    ContentPosition = (int)((mouse.X - thumbPressOffset.X) * (ContentSize / Size.X));
                return false;
            }

            return base.UpdateSelf(time);
        }

        protected override void BeforePress(ClickEventArgs args)
        {
            var thumb = GetThumbBounds();
            if (thumb.Contains(args.position))
            {
                didPressThumb = true;
                thumbPressOffset = args.position.ToPoint() - thumb.Location;
            }
        }

        protected override void BeforeClick(ClickEventArgs args)
        {
            didPressThumb = false;
            base.BeforeClick(args);
        }

        protected int GetThumbSize()
        {
            switch (Direction)
            {
                case Direction.Vertical:
                    return (int)((Size.Y / ContentSize) * Size.Y);
                case Direction.Horizontal:
                    return (int)((Size.X / ContentSize) * Size.X);
                default:
                    return 1;
            }
        }

        protected int GetThumbOffset()
        {
            int size = GetThumbSize();

            switch (Direction)
            {
                case Direction.Vertical:
                    return MathHelper.Clamp((int)((ContentPosition / (float)ContentSize) * Size.Y), 10, (int)Size.Y - size);
                case Direction.Horizontal:
                    return MathHelper.Clamp((int)((ContentPosition / (float)ContentSize) * Size.X), 10, (int)Size.X - size);
                default:
                    return 1;
            }
        }

        protected Rectangle GetThumbBounds()
        {
            return new Rectangle(
                4,
                4 + GetThumbOffset(),
                (int)Size.X - 8,
                GetThumbSize() - 8
            );
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var bounds = AbsoluteBounds;
            Takai.Graphics.Primitives2D.DrawRect(spriteBatch, OutlineColor, bounds);
            bounds.Inflate(-1, -1);
            Takai.Graphics.Primitives2D.DrawRect(spriteBatch, OutlineColor, bounds);

            var thumb = GetThumbBounds();
            thumb.Offset(AbsoluteBounds.Location);
            Takai.Graphics.Primitives2D.DrawFill(spriteBatch, OutlineColor, thumb);
        }
    }

    class Selector2 : Takai.UI.Static
    {

    }
}
