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

        public float GripperWidth { get; set; } = 8;
        public Color GripperColor { get; set; } = Color.CornflowerBlue;

        public Drawer()
        {
            IsModal = true;

            On(PressEvent, delegate (Static sender, UIEventArgs e)
            {
                ((Drawer)sender).isSizing = true;
                return UIEventResult.Handled;
            });
        }
        
        protected override bool HandleInput(GameTime time)
        {
            if (Input.InputState.IsClick(Input.MouseButtons.Left) && //todo: this is hacky
                !VisibleContentArea.Contains(Input.InputState.MousePoint))
                IsEnabled = false;

            //this is wtf right now???
            if (isSizing)
            {
                if (Input.InputState.IsButtonDown(Input.MouseButtons.Left))
                {
                    var maxx = Parent.ContentArea.Width;
                    var mdx = Input.InputState.MouseDelta().X;
                    switch (HorizontalAlignment)
                    {
                        case Alignment.Left:
                            Size = new Vector2(MathHelper.Clamp(ContentArea.Width + mdx, GripperWidth, maxx), float.NaN);
                            return false;
                        case Alignment.Right:
                            Size = new Vector2(MathHelper.Clamp(ContentArea.Width - mdx, GripperWidth, maxx), float.NaN);
                            return false;
                    }
                    return false;
                }
                else
                    isSizing = false;
            }

            return base.HandleInput(time);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (!float.IsNaN(availableSize.X))
                availableSize.X = System.Math.Max(GripperWidth, availableSize.X - GripperWidth);
            return base.MeasureOverride(availableSize);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            var container = new Rectangle(0, 0, (int)availableSize.X, (int)availableSize.Y);
            switch (HorizontalAlignment)
            {
                case Alignment.Left:
                    container.Width -= (int)GripperWidth;
                    break;
                case Alignment.Right:
                    container.X += (int)GripperWidth;
                    container.Width -= (int)GripperWidth;
                    break;
            }

            foreach (var child in Children)
                child.Arrange(container);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //todo
            var gripBnd = new Rectangle(OffsetContentArea.X, OffsetContentArea.Y, (int)GripperWidth, OffsetContentArea.Height);
            switch (HorizontalAlignment)
            {
                case Alignment.Left:
                    gripBnd.X = OffsetContentArea.Right - (int)GripperWidth;
                    Graphics.Primitives2D.DrawFill(spriteBatch, GripperColor, Rectangle.Intersect(VisibleBounds, gripBnd));
                    break;
                case Alignment.Right:
                    Graphics.Primitives2D.DrawFill(spriteBatch, GripperColor, Rectangle.Intersect(VisibleBounds, gripBnd));
                    break;
            }

            base.DrawSelf(spriteBatch);
        }
    }
}
