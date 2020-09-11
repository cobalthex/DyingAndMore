using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class Table : Static
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
        private Vector2 _margin = new Vector2(0);

        /// <summary>
        /// The number of columns, data is divided by the columns.
        /// If zero, reflow ignored.
        ///
        /// Note: disabled children are counted as occupied cells (but do not take up space)
        /// </summary>
        public int ColumnCount { get; set; } = 1;

        /// <summary>
        /// The background color for individual cells. Ignored if transparent
        /// </summary>
        public Color CellColor { get; set; }

        float[] columnWidths = new float[0];
        float[] rowHeights = new float[0];

        HashSet<int> hStretches = new HashSet<int>();
        HashSet<int> vStretches =  new HashSet<int>();

        Vector2 unstretchedArea = new Vector2();

        public Table() { }
        public Table(int columnCount, params Static[] children)
            : base(children)
        {
            ColumnCount = columnCount;
        }

        protected override void OnChildRemeasure(Static child)
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        public override void ApplyStyles(Dictionary<string, object> styleRules)
        {
            base.ApplyStyles(styleRules);
            Margin = GetStyleRule(styleRules, "Margin", Margin);
            CellColor = GetStyleRule(styleRules, "CellColor", CellColor);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (ColumnCount <= 0)
                return new Vector2();

            if (ColumnCount > columnWidths.Length)
                columnWidths = new float[ColumnCount];
            else
                System.Array.Clear(columnWidths, 0, ColumnCount);

            var rowCount = Util.CeilDiv(Children.Count, ColumnCount);
            if (rowCount > rowHeights.Length)
                rowHeights = new float[rowCount];
            else
                System.Array.Clear(rowHeights, 0, rowCount);

            var measuredArea = new Vector2();
            unstretchedArea = Vector2.Zero;
            hStretches.Clear();
            vStretches.Clear();

            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                var csize = Children[i].Measure(availableSize);
                if (Children[i].HorizontalAlignment == Alignment.Stretch)
                    hStretches.Add(i % ColumnCount);
                if (Children[i].VerticalAlignment == Alignment.Stretch)
                    vStretches.Add(i / ColumnCount);

                columnWidths[i % ColumnCount] = System.Math.Max(columnWidths[i % ColumnCount], csize.X);
                rowHeights[i / ColumnCount] = System.Math.Max(rowHeights[i / ColumnCount], csize.Y);
            }

            for (int i = 0; i < columnWidths.Length; ++i)
            {
                measuredArea.X += columnWidths[i];
                if (!hStretches.Contains(i))
                    unstretchedArea.X += columnWidths[i];
            }
            for (int i = 0; i < rowHeights.Length; ++i)
            {
                measuredArea.Y += rowHeights[i];
                if (!vStretches.Contains(i))
                    unstretchedArea.Y += rowHeights[i];
            }

            var margins = new Vector2(columnWidths.Length - 1, rowHeights.Length - 1) * Margin;
            unstretchedArea += margins;

            return measuredArea + margins;
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            if (ColumnCount <= 0)
                return;

            if (hStretches.Count > 0)
            {
                float width = (availableSize.X - unstretchedArea.X) / hStretches.Count;
                foreach (var col in hStretches)
                    columnWidths[col] = width; //use remaining width elsewhere?
            }

            if (vStretches.Count > 0 && availableSize.Y > unstretchedArea.Y)
            {
                float height = (availableSize.Y - unstretchedArea.Y) / vStretches.Count;
                foreach (var row in vStretches)
                    rowHeights[row] = height; //use remaining height elsewhere?
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

                Children[i].Arrange(new Rectangle(
                    (int)offset.X,
                    (int)offset.Y,
                    (int)columnWidths[i % ColumnCount],
                    (int)rowHeights[i / ColumnCount]
                ));
                offset.X += columnWidths[i % ColumnCount];
            }
        }

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);

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

                    DrawFill(context.spriteBatch, CellColor, new Rectangle(
                        (int)offset.X,
                        (int)offset.Y,
                        (int)columnWidths[i % ColumnCount],
                        (int)rowHeights[i / ColumnCount]
                    ));

                    offset.X += columnWidths[i % ColumnCount];
                }
            }
        }
    }
}
