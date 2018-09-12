using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public class Table : Static
    {
        /// <summary>
        /// Spacing between items
        /// </summary>
        public Vector2 Margin { get; set; } = Vector2.Zero;

        /// <summary>
        /// The number of columns, data is divided by the columns
        /// If zero, reflow ignored
        /// </summary>
        public int ColumnCount { get; set; } = 1;

        public Table() { }
        public Table(int columnCount, params Static[] children)
            : base(children)
        {
            ColumnCount = columnCount;
        }

        public override void Reflow(Rectangle container)
        {
            FitToContainer(container);

            if (ColumnCount <= 0)
                return;

            float[] colWidths = new float[ColumnCount];
            float[] rowHeights = new float[(int)System.Math.Ceiling(Children.Count / (float)ColumnCount)]; //todo: integer only
            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                var bounds = Children[i].AbsoluteBounds;
                if (Children[i].HorizontalAlignment == Alignment.Stretch)
                    bounds.Width = 1;
                if (Children[i].HorizontalAlignment == Alignment.Stretch)
                    bounds.Height = 1;

                //todo: centering (maybe right) broken and doesnt correctly clip/position

                colWidths[i % ColumnCount] = System.Math.Max(colWidths[i % ColumnCount], Children[i].Position.X + bounds.Width);
                rowHeights[i / ColumnCount] = System.Math.Max(rowHeights[i / ColumnCount], Children[i].Position.Y + bounds.Height);
            }

            var offset = Vector2.Zero;
            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    ;// continue;

                if (i > 0)
                {
                    if (i % ColumnCount == 0)
                    {
                        offset.X = 0;
                        offset.Y += rowHeights[(i - 1) / ColumnCount] + Margin.Y;
                    }
                    else
                        offset.X += Margin.X;
                }

                Children[i].Reflow(new Rectangle(
                    (int)offset.X + AbsoluteDimensions.X,
                    (int)offset.Y + AbsoluteDimensions.Y,
                    (int)colWidths[i % ColumnCount],
                    (int)rowHeights[i / ColumnCount]
                ));
                offset.X += colWidths[i % ColumnCount];
            }
        }
    }
}
