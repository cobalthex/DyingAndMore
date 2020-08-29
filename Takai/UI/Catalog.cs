using System.CodeDom;
using System.Collections.Generic;
using System.Xml.Schema;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Catalog : Static
    {
        /// <summary>
        /// Spacing between items
        /// </summary>
        public Vector2 Margin
        {
            get => _margin;
            set
            {
                if (_margin == value)
                    return;

                _margin = value;
                InvalidateMeasure();
            }
        }
        private Vector2 _margin;

        /// <summary>
        /// Which direction should items flow
        /// (horizontal = rows vs vertical = columns)
        /// </summary>
        public Direction Direction
        {
            get => _direction;
            set
            {
                if (_direction == value)
                    return;

                _direction = value;
                InvalidateMeasure();
            }
        }
        private Direction _direction = Direction.Horizontal;

        //todo: shared size? (only measure using MeasuredSize)
        //must manually calculate size?

        //justify option?

        public Catalog() { }
        public Catalog(params Static[] children)
            : base(children) { }

        public override void ApplyStyles(Dictionary<string, object> styleRules)
        {
            base.ApplyStyles(styleRules);
            Margin = GetStyleRule(styleRules, "Margin", Margin);
            Direction = GetStyleRule(styleRules, "Direction", Direction);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            //todo: item alignments

            var usedSize = new Vector2();
            var lateralMax = 0f;
            foreach (var child in Children)
            {
                if (!child.IsEnabled)
                    continue;

                var itemSize = child.Measure(new Vector2(InfiniteSize));
                if (Direction == Direction.Horizontal)
                {
                    if (usedSize.X > 0 && usedSize.X + itemSize.X > availableSize.X)
                        usedSize = new Vector2(0, usedSize.Y + lateralMax + Margin.Y);

                    child.Arrange(new Rectangle(usedSize.ToPoint(), itemSize.ToPoint()));
                    usedSize.X += itemSize.X + Margin.X;
                    lateralMax = System.Math.Max(lateralMax, itemSize.Y);
                }
                else
                {
                    if (usedSize.Y > 0 && usedSize.Y + itemSize.Y > availableSize.Y)
                        usedSize = new Vector2(usedSize.X + lateralMax + Margin.X, 0);

                    child.Arrange(new Rectangle(usedSize.ToPoint(), itemSize.ToPoint()));
                    usedSize.Y += itemSize.Y + Margin.Y;
                    lateralMax = System.Math.Max(lateralMax, itemSize.X);
                }
            
            }
            
            if (Direction == Direction.Horizontal)
                usedSize += new Vector2(-Margin.X, lateralMax);
            else
                usedSize += new Vector2(lateralMax, -Margin.Y);

            return usedSize;

            //todo: bounds may be affected by Stretch which would prove wrong here
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            //todo: item alignments

            var offset = new Vector2();
            var lateralMax = 0f;
            foreach (var child in Children)
            {
                if (!child.IsEnabled)
                    continue;

                var itemSize = child.MeasuredSize; 
                if (Direction == Direction.Horizontal)
                {
                    if (offset.X > 0 && offset.X + itemSize.X > availableSize.X)
                        offset = new Vector2(0, offset.Y + lateralMax + Margin.Y);

                    child.Arrange(new Rectangle(offset.ToPoint(), itemSize.ToPoint()));
                    offset.X += itemSize.X + Margin.X;
                    lateralMax = System.Math.Max(lateralMax, itemSize.Y);
                }
                else
                {
                    if (offset.Y > 0 && offset.Y + itemSize.Y > availableSize.Y)
                        offset = new Vector2(offset.X + lateralMax + Margin.X, 0);

                    child.Arrange(new Rectangle(offset.ToPoint(), itemSize.ToPoint()));
                    offset.Y += itemSize.Y + Margin.Y;
                    lateralMax = System.Math.Max(lateralMax, itemSize.X);
                }
            }
        }
    }
}
