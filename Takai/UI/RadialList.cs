using Microsoft.Xna.Framework;

namespace Takai.UI
{
    /// <summary>
    /// Draw the items in a circle
    /// </summary>
    public class RadialList : List
    {
        /// <summary>
        /// The distance from the center of this element to the centers of its children
        /// </summary>
        public float Radius { get; set; } = 50;

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            throw new System.NotImplementedException(); //todo

            if (Children.Count == 0)
                return;

            var center = Size / 2;

            var thetaScale = MathHelper.TwoPi / Children.Count;

            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                var vbnd = Children[i].ContentArea;
                var ccenter = new Vector2(vbnd.Width / 2, vbnd.Height / 2);

                var pos = Util.Direction(i * thetaScale) * Radius;
                Children[i].Position = center - ccenter + pos;
            }

            foreach (var child in Children)
            {
                if (child.IsEnabled)
                    child.Reflow();
            }
        }
    }
}
