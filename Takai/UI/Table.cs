using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        /// <summary>
        /// The background color for individual cells. Ignored if transparent
        /// </summary>
        public Color CellColor { get; set; }

        float[] columnWidths = new float[0], rowHeights = new float[0];

        System.Collections.Generic.HashSet<int> hStretches = new System.Collections.Generic.HashSet<int>();
        System.Collections.Generic.HashSet<int> vStretches =  new System.Collections.Generic.HashSet<int>();

        Vector2 usedArea = new Vector2();

        public Table() { }
        public Table(int columnCount, params Static[] children)
            : base(children)
        {
            ColumnCount = columnCount;
        }

        protected override void OnChildReflow(Static child)
        {
            if (IsAutoSized)
                Reflow();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            System.Array.Resize(ref columnWidths, ColumnCount);
            System.Array.Clear(columnWidths, 0, columnWidths.Length);
            System.Array.Resize(ref rowHeights, (int)System.Math.Ceiling(Children.Count / (float)ColumnCount)); //todo: use integer division
            System.Array.Clear(rowHeights, 0, rowHeights.Length);

            usedArea = Vector2.Zero;
            hStretches.Clear();
            vStretches.Clear();
            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                var csize = Children[i].Measure(new Vector2(InfiniteSize)); //needed up here for children to correctly size
                if (Children[i].HorizontalAlignment == Alignment.Stretch)
                {
                    hStretches.Add(i % ColumnCount);
                    csize.X = 0;
                }
                if (Children[i].VerticalAlignment == Alignment.Stretch)
                {
                    vStretches.Add(i / ColumnCount);
                    csize.Y = 0;
                }

                columnWidths[i % ColumnCount] = System.Math.Max(columnWidths[i % ColumnCount], csize.X);
                rowHeights[i / ColumnCount] = System.Math.Max(rowHeights[i / ColumnCount], csize.Y);
            }

            foreach (var col in columnWidths)
                usedArea.X += col;
            foreach (var row in rowHeights)
                usedArea.Y += row;

            usedArea += new Vector2(columnWidths.Length - 1, rowHeights.Length - 1) * Margin;

            return usedArea;
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            if (ColumnCount <= 0)
                return;

            if (hStretches.Count > 0)
            {
                float width = (availableSize.X - usedArea.X) / hStretches.Count;
                foreach (var col in hStretches)
                {
                    usedArea.X += System.Math.Max(0, width - columnWidths[col]);
                    columnWidths[col] = System.Math.Max(columnWidths[col], width); //use remaining width elsewhere?
                }
            }

            if (vStretches.Count > 0 && availableSize.Y > usedArea.Y)
            {
                float height = (availableSize.Y - usedArea.Y) / vStretches.Count;
                foreach (var row in vStretches)
                {
                    usedArea.Y += System.Math.Max(0, height - rowHeights[row]);
                    rowHeights[row] = System.Math.Max(rowHeights[row], height); //use remaining height elsewhere?
                }
            }

            var offset = Vector2.Zero;
            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

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
                    (int)offset.X,
                    (int)offset.Y,
                    (int)columnWidths[i % ColumnCount],
                    (int)rowHeights[i / ColumnCount]
                ));
                offset.X += columnWidths[i % ColumnCount];
            }

            NotifyChildReflow();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (CellColor.A > 0)
            {
                Vector2 offset = Vector2.Zero;
                for (int i = 0; i < Children.Count; ++i)
                {
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

                    var rect = new Rectangle(
                        OffsetContentArea.X + (int)offset.X,
                        OffsetContentArea.Y + (int)offset.Y,
                        (int)columnWidths[i % ColumnCount],
                        (int)rowHeights[i / ColumnCount]
                    );
                    Graphics.Primitives2D.DrawFill(spriteBatch, CellColor, Rectangle.Intersect(rect, VisibleContentArea));

                    offset.X += columnWidths[i % ColumnCount];
                }
            }
        }
    }
}
