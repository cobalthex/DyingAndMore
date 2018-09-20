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
        }

        protected override void OnPress(ClickEventArgs e)
        {
            isSizing = true;
        }
        protected override void OnClick(ClickEventArgs e)
        {
            isSizing = false;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (Input.InputState.IsClick(Input.MouseButtons.Left) && //todo: this is hacky
                !VisibleBounds.Contains(Input.InputState.MousePoint))
                IsEnabled = false;

            if (isSizing && Input.InputState.IsButtonDown(Input.MouseButtons.Left))
            {
                var mdx = Input.InputState.MouseDelta().X;
                switch (HorizontalAlignment)
                {
                    case Alignment.Left:
                        Size = new Vector2(System.Math.Max(SplitterWidth, Size.X + mdx), 0);
                        return false;
                    case Alignment.Right:
                        Size = new Vector2(System.Math.Max(SplitterWidth, Size.X - mdx), 0);
                        return false;
                }
            }

            return base.HandleInput(time);
        }

        public override void Reflow(Rectangle container)
        {
            AdjustToContainer(container);
            container = AbsoluteDimensions;

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
            var splitBnds = new Rectangle(AbsoluteDimensions.X, AbsoluteDimensions.Y, (int)SplitterWidth, AbsoluteDimensions.Height);
            switch (HorizontalAlignment)
            {
                case Alignment.Left:
                    splitBnds.X = AbsoluteDimensions.Right - (int)SplitterWidth;
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
