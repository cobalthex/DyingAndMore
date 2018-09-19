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
        //todo: modal, allow click outside

        public Static Splitter { get; protected set; } = new Static
        {
            BackgroundColor = Color.CornflowerBlue,
            Size = new Vector2(8, 1),
            VerticalAlignment = Alignment.Stretch
        };

        public Drawer()
        {
            IsModal = true;
            AddChild(Splitter);
        }

        protected override bool HandleInput(GameTime time)
        {
            if (Input.InputState.IsClick(Input.MouseButtons.Left) && //todo: this is hacky
                !VisibleBounds.Contains(Input.InputState.MousePoint))
                IsEnabled = false;

            return base.HandleInput(time);
        }

        public override void Reflow(Rectangle container)
        {
            switch (HorizontalAlignment)
            {
                case Alignment.Left:
                    Splitter.IsEnabled = true;
                    container.Width -= Splitter.AbsoluteBounds.Width;
                    break;
                case Alignment.Right:
                    Splitter.IsEnabled = true;
                    container.X += Splitter.AbsoluteBounds.Width;
                    container.Width -= Splitter.AbsoluteBounds.Width;
                    break;
                default:
                    Splitter.IsEnabled = false;
                    break;
            }
            Splitter.HorizontalAlignment = HorizontalAlignment;
            Splitter.Reflow(container);

            AdjustToContainer(container);
            foreach (var child in Children)
            {
                if (child == Splitter)
                    continue;

                child.Reflow(AbsoluteDimensions);
            }
        }
    }
}
