using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    /// <summary>
    /// Displays the full tree of <see cref="Display"/>'s hierarchy
    /// </summary>
    class UITree : Static
    {
        [Data.Serializer.Ignored]
        /// <summary>
        /// Allows for customization on how each element is displayed
        /// </summary>
        public System.Func<Static, string> DisplayString { get; set; } = (Static s) =>
        {
            var text = string.IsNullOrEmpty(s.Name) ? "(No name)" : s.Name;
            text += $" w:{s.Size.X} h:{s.Size.Y}";
            return text;
        };

        /// <summary>
        /// The UI element to display the hierarchy of
        /// </summary>
        public Static Display
        {
            get => _display;
            set
            {
                if (value == _display)
                    return;
                _display = value;

                RemoveAllChildren();

                var stack = new Stack<KeyValuePair<int, Static>>();
                stack.Push(new KeyValuePair<int, Static>(0, value));

                int y = 0;
                while (stack.Count > 0)
                {
                    var top = stack.Pop();
                    var elem = top.Value;
                    if (elem == this)
                        continue;

                    foreach (var child in elem.Children)
                        stack.Push(new KeyValuePair<int, Static>(top.Key + 1, child));

                    var disp = new Static
                    {
                        Name = elem.Name,
                        Text = DisplayString(elem),
                        Color = Color,
                        Font = Font,
                        Position = new Vector2(20 * top.Key, y)
                    };
                    disp.AutoSize();
                    y += (int)disp.Size.Y;
                    AddChild(disp);
                }
            }
        }
        private Static _display;
    }
}
