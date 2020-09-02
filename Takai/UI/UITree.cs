using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    /// <summary>
    /// Displays the full tree of <see cref="Root"/>'s hierarchy
    /// </summary>
    public class UITree : Static
    {
        [Data.Serializer.Ignored]
        /// <summary>
        /// Allows for customization on how each element is displayed
        /// </summary>
        public Func<Static, string> DisplayString { get; set; } = (Static s) =>
        {
            return s.ToString();
        };

        /// <summary>
        /// The size of each indent level for nested elements
        /// </summary>
        public int Indent { get; set; } = 10;

        /// <summary>
        /// The UI element to display the hierarchy of
        /// </summary>
        public Static Root
        {
            get => _root;
            set
            {
                if (value == _root)
                    return;
                _root = value;
                rows.Clear();
                maxWidth = 0;

                var stack = new Stack<(int depth, Static element)>();
                stack.Push((0, value));

                while (stack.Count > 0)
                {
                    var (depth, element) = stack.Pop();
                    var elem = element;
                    if (elem == this)
                        continue;

                    foreach (var child in elem.Children)
                        stack.Push((depth + 1, child));

                    var str = DisplayString(elem);
                    rows.Add((str, depth));
                    if (Font != null)
                        maxWidth = Math.Max(maxWidth, Font.MeasureString(str, TextStyle).X + (Indent * depth));
                }
            }
        }
        private Static _root;

        /// <summary>
        /// Automatically update this tree with the current root (useful for inspection of live tree)
        /// </summary>
        public bool AutoRoot { get; set; } = false;

        private readonly List<(string text, int indent)> rows = new List<(string text, int indent)>();
        private float maxWidth;

        public UITree()
        {

        }

        public UITree(Static root)
        {
            Root = root;
        }

        protected override void OnParentChanged(Static oldParent)
        {
            if (AutoRoot)
                Root = GetRoot();
            base.OnParentChanged(oldParent);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(maxWidth, rows.Count * Font.GetLineHeight(TextStyle));
        }

        protected override void DrawSelf(DrawContext context)
        {
            if (Font == null)
                return;

            var drawText = new Graphics.DrawTextOptions(
                "",
                Font,
                TextStyle, 
                Color, 
                (OffsetContentArea.Location - VisibleContentArea.Location).ToVector2()
            );

            for (int y = 0; y < rows.Count; ++y)
            {
                var row = rows[y];
                drawText.text = row.text;
                drawText.position = new Vector2(row.indent * Indent, y * Font.GetLineHeight(TextStyle));
                context.textRenderer.Draw(drawText);
            }
        }
    }
}
