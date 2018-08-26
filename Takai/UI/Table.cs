using Microsoft.Xna.Framework;

namespace Takai.Data
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
        public Table(params Static[] children)
            : base(children) { }

        public override void Reflow()
        {
            if (ColumnCount <= 0)
                return;

            float[] colWidths = new float[ColumnCount];
            for (int i = 0; i < Children.Count; ++i)
            {
                if (Children[i].IsEnabled)
                    colWidths[i % ColumnCount] = System.Math.Max(colWidths[i % ColumnCount], Children[i].Size.X);
            }

            //todo: base.Reflow() overrides stretch, need to move elsewhere (into method maybe?)

            var offset = Vector2.Zero;
            float rowHeight = 0;
            for (int i = 0; i < Children.Count; ++i)
            {
                if (!Children[i].IsEnabled)
                    continue;

                if (i % ColumnCount == 0)
                {
                    offset.X = 0;
                    offset.Y += rowHeight + Margin.Y;
                    rowHeight = 0;
                }

                Children[i].Position = offset;
                rowHeight = System.Math.Max(rowHeight, Children[i].Size.Y);
                offset.X += colWidths[i % ColumnCount] + Margin.X;
            }

            base.Reflow();
        }
    }
}
