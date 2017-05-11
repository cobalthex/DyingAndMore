using Microsoft.Xna.Framework;
using Takai.Input;

namespace Takai.UI
{
    public class ScrollBar : Static
    {
        static int ThumbMargin = 4; //todo: apply correctly

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

        public override bool CanFocus => true;

        public ScrollBar()
        {
            OutlineColor = Color;
        }

        protected override bool UpdateSelf(GameTime time)
        {
            if (!base.UpdateSelf(time))
                return false;

            if (Size.Y < ContentSize)
            {
                if (didPress)
                {
                    var mouse = InputState.MousePoint - AbsoluteBounds.Location;
                    var deltaMouse = InputState.MouseDelta();

                    if (didPressThumb)
                    {
                        if (Direction == Direction.Vertical)
                            ContentPosition += (int)(deltaMouse.Y * ((float)ContentSize / GetContainerSize()));
                        else if (Direction == Direction.Horizontal)
                            ContentPosition += (int)(deltaMouse.X * ((float)ContentSize / GetContainerSize()));
                        return false;
                    }
                }
                else
                    didPressThumb = false;
            }
            return true;
        }

        protected override void BeforePress(ClickEventArgs args)
        {
            var thumb = GetThumbBounds();
            if (!thumb.Contains(args.position))
            {
                //center thumb around mouse
                if (Direction == Direction.Vertical)
                    ContentPosition = (int)((args.position.Y - ThumbMargin - GetThumbSize() / 2) * ((float)ContentSize / GetContainerSize()));
                else if (Direction == Direction.Horizontal)
                    ContentPosition = (int)((args.position.X - ThumbMargin - GetThumbSize() / 2) * ((float)ContentSize / GetContainerSize()));
            }
            didPressThumb = true;
        }

        protected int GetContainerSize()
        {
            //todo: precalculate
            switch (Direction)
            {
                case Direction.Vertical:
                    return (int)Size.Y - (ThumbMargin * 2);
                case Direction.Horizontal:
                    return (int)Size.X - (ThumbMargin * 2);
                default:
                    return 1;
            }
        }

        protected int GetThumbSize()
        {
            //todo: precalculate
            switch (Direction)
            {
                case Direction.Vertical:
                    return (int)((Size.Y / ContentSize) * GetContainerSize());
                case Direction.Horizontal:
                    return (int)((Size.X / ContentSize) * GetContainerSize());
                default:
                    return 1;
            }
        }

        protected int GetThumbOffset()
        {
            int containerSize = GetContainerSize();
            int size = GetThumbSize();

            return ThumbMargin + MathHelper.Clamp((int)((ContentPosition / (float)ContentSize) * containerSize), 0, containerSize - size);
        }

        protected Rectangle GetThumbBounds()
        {
            switch (Direction)
            {
                case Direction.Vertical:
                    return new Rectangle(ThumbMargin, GetThumbOffset(), (int)Size.X - (ThumbMargin * 2), GetThumbSize());
                case Direction.Horizontal:
                    return new Rectangle(GetThumbOffset(), ThumbMargin, GetThumbSize(), (int)Size.Y - (ThumbMargin * 2));
                default:
                    return Rectangle.Empty;
            }
        }

        protected override void DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            var bounds = AbsoluteBounds;
            Takai.Graphics.Primitives2D.DrawRect(spriteBatch, OutlineColor, bounds);
            bounds.Inflate(-1, -1);
            Takai.Graphics.Primitives2D.DrawRect(spriteBatch, OutlineColor, bounds);

            if (Size.Y < ContentSize)
            {
                var thumb = GetThumbBounds();
                thumb.Offset(AbsoluteBounds.Location);
                Takai.Graphics.Primitives2D.DrawFill(spriteBatch, OutlineColor, thumb);
            }
        }
    }
}
