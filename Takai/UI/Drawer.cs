using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    /// <summary>
    /// A draw that pulls out from the left or right side of the parent container, can be resized
    /// Uses HorizontalAlignment to determine split position (Only Start/End recognized)
    /// </summary>
    public class Drawer : Static
    {
        bool isSizing = false;
        Vector2 sizingOffset = Vector2.Zero;

        public float SplitterWidth { get; set; } = 8;
        public Color SplitterColor { get; set; } = Color.CornflowerBlue;

        public Drawer()
        {
            IsModal = true;

            On(PressEvent, delegate
            {
                isSizing = true;
                return UIEventResult.Handled;
            });

            On(ClickEvent, delegate
            {
                isSizing = false;
                return UIEventResult.Handled;
            });
        }

        protected override void OnChildReflow(Static child)
        {
            Reflow();
            base.OnChildReflow(child);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (Input.InputState.IsClick(Input.MouseButtons.Left) && //todo: this is hacky
                !VisibleContentArea.Contains(Input.InputState.MousePoint))
                IsEnabled = false;

            if (isSizing && Input.InputState.IsButtonDown(Input.MouseButtons.Left))
            {
                var maxx = Parent.ContentArea.Width;
                var mdx = Input.InputState.MouseDelta().X;
                switch (HorizontalAlignment)
                {
                    case Alignment.Left:
                        Size = new Vector2(MathHelper.Clamp(ContentArea.Width + mdx, SplitterWidth, maxx), float.NaN);
                        return false;
                    case Alignment.Right:
                        Size = new Vector2(MathHelper.Clamp(ContentArea.Width - mdx, SplitterWidth, maxx), float.NaN);
                        return false;
                }
                return false;
            }

            return base.HandleInput(time);
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            var container = new Rectangle(0, 0, (int)availableSize.X, (int)availableSize.Y);
            switch (HorizontalAlignment)
            {
                case Alignment.Left:
                    container.Width -= (int)SplitterWidth;
                    break;
                case Alignment.Right:
                    container.X += (int)SplitterWidth;
                    container.Width -= (int)SplitterWidth;
                    break;
            }

            foreach (var child in Children)
                child.Reflow(container);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //todo
            var splitBnds = new Rectangle(OffsetContentArea.X, OffsetContentArea.Y, (int)SplitterWidth, OffsetContentArea.Height);
            switch (HorizontalAlignment)
            {
                case Alignment.Left:
                    splitBnds.X = OffsetContentArea.Right - (int)SplitterWidth;
                    Graphics.Primitives2D.DrawFill(spriteBatch, SplitterColor, Rectangle.Intersect(VisibleBounds, splitBnds));
                    break;
                case Alignment.Right:
                    Graphics.Primitives2D.DrawFill(spriteBatch, SplitterColor, Rectangle.Intersect(VisibleBounds, splitBnds));
                    break;
            }

            base.DrawSelf(spriteBatch);
        }
    }
}
