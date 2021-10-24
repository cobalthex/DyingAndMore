using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class ScrollEventArgs : UIEventArgs
    {
        public float Delta { get; set; }

        public ScrollEventArgs(Static source, float delta)
            : base(source)
        {
            Delta = delta;
        }
    }

    public class ScrollBar : Static
    {
        public const string HScrollEvent = "HScroll";
        public const string VScrollEvent = "VScroll";
        public static float DefaultSize = 20;

        /// <summary>
        /// The size of the content. This determines how big the scroll thumb is
        /// </summary>
        public float ContentSize
        {
            get => _contentSize;
            set
            {
                _contentSize = Math.Max(value, 0);
                ContentPosition = _contentPosition;
            }
        }
        private float _contentSize = 1;

        /// <summary>
        /// Is the scrollbar thumb visible
        /// </summary>
        [Data.Serializer.Ignored]
        public bool IsThumbVisible =>
            (Direction == Direction.Horizontal ? ContentArea.Width : ContentArea.Height) < ContentSize;

        /// <summary>
        /// Sprite to draw over the drag thumb for the scrollbar
        /// </summary>
        public Graphics.NinePatch ThumbSprite { get; set; }

        /// <summary>
        /// Where the content is scrolled to
        /// </summary>
        public float ContentPosition
        {
            get => _contentPosition;
            set
            {
                //use helper methods
                var size = Direction == Direction.Horizontal ? ContentArea.Width : ContentArea.Height;

                float newPosition;
                if (size > _contentSize)
                    newPosition = 0;
                else
                    newPosition = Util.Clamp(value, 0, ContentSize - size);

                if (newPosition != _contentPosition)
                {
                    var e = new ScrollEventArgs(this, newPosition - _contentPosition);
                    _contentPosition = newPosition;
                    BubbleEvent(Direction == Direction.Horizontal ? HScrollEvent : VScrollEvent, e);
                    InvalidateArrange();
                }
            }
        }
        private float _contentPosition = 0;

        /// <summary>
        /// Which direction the scrollbar moves
        /// </summary>
        public Direction Direction { get; set; } = Direction.Vertical;

        public override bool CanFocus => IsThumbVisible;

        public float ThumbSize { get; private set; }

        public float ThumbPosition { get; private set; }

        private float pressOffset;

        public bool AtBeginning()
        {
            return ContentPosition == 0;
        }

        public bool AtEnd()
        {
            //todo: use helper methods
            var size = Direction == Direction.Horizontal ? ContentArea.Width : ContentArea.Height;
            return ContentPosition >= ContentSize - size;
        }

        public ScrollBar()
        {
            BorderColor = Color;
            On(PressEvent, OnPress);
            On(DragEvent, OnDrag);
            On(ClickEvent, (Static Sender, UIEventArgs e) => UIEventResult.Handled);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (Direction == Direction.Horizontal)
                return new Vector2(float.IsInfinity(availableSize.X) ? DefaultSize : availableSize.X, DefaultSize);
            else
                return new Vector2(DefaultSize, float.IsInfinity(availableSize.Y) ? DefaultSize : availableSize.Y);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            var containerSize = GetContainerSize();
            ThumbSize = (containerSize / ContentSize) * containerSize;
            ThumbPosition = Util.Clamp((ContentPosition / ContentSize) * containerSize, 0, containerSize - ThumbSize);
            base.ArrangeOverride(availableSize);
        }

        public bool Scroll(int direction)
        {
            var cpos = ContentPosition;
            ContentPosition -= Math.Sign(direction) * (Font?.GetLineHeight(TextStyle) ?? 30);
            return (cpos != ContentPosition);
        }

        static UIEventResult OnPress(Static sender, UIEventArgs e)
        {
            //todo: compare against absolute bounds not dimensions

            var pea = (PointerEventArgs)e;
            var self = (ScrollBar)sender;

            if (pea.button != 0)
                return UIEventResult.Continue;

            var thumb = self.GetThumbBounds();

            if (!thumb.Contains(pea.position))
            {
                //todo: maybe scroll to mouse over time (smooth scroll)

                //center thumb around mouse
                if (self.Direction == Direction.Horizontal)
                    self.ContentPosition = (int)((pea.position.X - self.ThumbSize / 2) * (self.ContentSize / self.GetContainerSize()));
                else
                    self.ContentPosition = (int)((pea.position.Y - self.ThumbSize / 2) * (self.ContentSize / self.GetContainerSize()));

                self.pressOffset = self.ThumbSize / 2;
            }
            else
            {
                if (self.Direction == Direction.Horizontal)
                    self.pressOffset = pea.position.X - self.ThumbPosition;
                else
                    self.pressOffset = pea.position.Y - self.ThumbPosition;
            }

            return UIEventResult.Handled;
        }

        static UIEventResult OnDrag(Static sender, UIEventArgs e)
        {
            var dea = (DragEventArgs)e;
            var self = (ScrollBar)sender;

            if (dea.button != 0)
                return UIEventResult.Continue;

            if (self.Direction == Direction.Horizontal)
                self.ContentPosition = (int)((dea.position.X - self.pressOffset) * ((float)self.ContentSize / self.GetContainerSize()));
            else
                self.ContentPosition = (int)((dea.position.Y - self.pressOffset) * ((float)self.ContentSize / self.GetContainerSize()));

            return UIEventResult.Handled;
        }

        protected float GetContainerSize()
        {
            //todo: precalculate
            return Direction switch
            {
                Direction.Vertical => ContentArea.Height,
                Direction.Horizontal => ContentArea.Width,
                _ => 1,
            };
        }

        protected Rectangle GetThumbBounds()
        {
            //cache?
            return Direction switch
            {
                Direction.Vertical => new Rectangle(0, (int)ThumbPosition, ContentArea.Width, (int)ThumbSize),
                Direction.Horizontal => new Rectangle((int)ThumbPosition, 0, (int)ThumbSize, ContentArea.Height),
                _ => Rectangle.Empty,
            };
        }

        protected override bool HandleInput(GameTime time)
        {
            if (IsThumbVisible && HasFocus)
            {
                if (VisibleContentArea.Contains(InputState.MousePoint) && InputState.HasScrolled())
                {
                    Scroll(InputState.ScrollDelta());
                    return false;
                }

                if (IsThumbVisible)
                {
                    if (Direction == Direction.Vertical)
                    {
                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Up))
                        {
                            _contentPosition -= 1;// (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }

                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Down))
                        {
                            _contentPosition += 1;//(float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }
                    }

                    else if (Direction == Direction.Horizontal)
                    {
                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Left))
                        {
                            _contentPosition -= (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }

                        if (InputState.IsButtonDown(Microsoft.Xna.Framework.Input.Keys.Right))
                        {
                            _contentPosition += (float)(400 * time.ElapsedGameTime.TotalSeconds);
                            return false;
                        }
                    }
                    else if (InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.PageUp))
                    {
                        ContentPosition -= GetContainerSize() / 2;
                        return false;
                    }
                    else if (InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.PageDown))
                    {
                        ContentPosition += GetContainerSize() / 2;
                        return false;
                    }
                }
            }
            return base.HandleInput(time);
        }

        protected override void DrawSelf(DrawContext context)
        {
            if (!IsThumbVisible)
                return;

            var thumb = GetThumbBounds();
            thumb.Offset(OffsetContentArea.Location);
            thumb = Rectangle.Intersect(VisibleContentArea, thumb);
            Graphics.Primitives2D.DrawFill(context.spriteBatch, Color, thumb);
            ThumbSprite.Draw(context.spriteBatch, thumb, currentStateElapsedTime);
        }

        protected override void ApplyStyleOverride()
        {
            base.ApplyStyleOverride();

            // todo: need to track style start time to run animations

            // todo: hover style should only apply when over thumb
            // might need to override input handling to do

            var style = GenerateStyleSheet<ScrollBarStyleSheet>();
            if (style.ThumbSprite.HasValue) ThumbSprite = style.ThumbSprite.Value;
        }

        public struct ScrollBarStyleSheet : IStyleSheet<ScrollBarStyleSheet>
        {
            public string Name { get; set; }

            public NinePatch? ThumbSprite;

            public void LerpWith(ScrollBarStyleSheet other, float t)
            {
                throw new NotImplementedException();
            }

            public void MergeWith(ScrollBarStyleSheet other)
            {
                if (other.ThumbSprite.HasValue) ThumbSprite = other.ThumbSprite;
            }
        }
    }
}
