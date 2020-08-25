using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Takai.UI
{
    public class VisibilityChangedEventArgs : UIEventArgs
    {
        public bool IsMinimized { get; set; }

        public VisibilityChangedEventArgs(Static source, bool isMinimized)
            : base(source)
        {
            IsMinimized = isMinimized;
        }
    }

    /// <summary>
    /// A collapsable element, displaying only a title when collapsed
    /// </summary>
    public class Shade : Static
    {
        public const string VisibilityChangedEvent = "VisibilityChanged";


        //direction

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized == value)
                    return;

                _isMinimized = value;
                BubbleEvent(VisibilityChangedEvent, new VisibilityChangedEventArgs(this, _isMinimized));
                InvalidateMeasure();
            }
        }
        private bool _isMinimized;

        private int headerSize; //includes margin

        private float desiredContentSize; //for animation
        private float actualContentSize;

        public Shade()
        {
            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                if (e.Source == sender) //prevents annoying clickthroughs
                    ((Shade)sender).IsMinimized ^= true;
                return UIEventResult.Handled;
            });
        }
        public Shade(params Static[] children)
            : this()
        {
            AddChildren(children);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            //todo: animation
            var measured = base.MeasureOverride(availableSize) + new Vector2(0, Padding.Y);
            if (IsMinimized)
                measured.Y = 0;

            var header = Font.MeasureString(Text);
            headerSize = (int)(header.Y + Padding.Y);
            measured = new Vector2(MathHelper.Max(header.X, measured.X), header.Y + measured.Y);

            desiredContentSize = measured.Y;

            return new Vector2(measured.X, actualContentSize); //round
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            var container = new Rectangle(0, headerSize, (int)availableSize.X, Math.Max(0, (int)availableSize.Y - headerSize));
            foreach (var child in Children)
                child.Arrange(container);
        }

        protected override void UpdateSelf(GameTime time)
        {
            if (actualContentSize != desiredContentSize)
            {
                var sign = Math.Sign(desiredContentSize - actualContentSize);
                var abs = Math.Abs(desiredContentSize - actualContentSize);
                actualContentSize += sign * Math.Min(abs, 3000 * (float)time.ElapsedGameTime.TotalSeconds);
                InvalidateMeasure();
            }

            base.UpdateSelf(time);
        }

        internal void MinimizeNoEvent() //for use in accordian
        {
            if (_isMinimized)
                return;

            _isMinimized = true;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// A list of shades, where opening one closes all of the others
    /// </summary>
    public class Accordian : List
    {
        /// <summary>
        /// Should items be automatically collapsed when adding to the accordian?
        /// Does not affect items already in the accordian
        /// </summary>
        public bool InitiallyCollapsed { get; set; } = true;

        //collapse new vs first?

        public Accordian() 
        {
            On(Shade.VisibilityChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (Accordian)sender;
                var vcea = (VisibilityChangedEventArgs)e;
                if (!vcea.IsMinimized)
                {
                    foreach (var child in self.Children)
                    {
                        if (child != vcea.Source)
                            ((Shade)child).MinimizeNoEvent();
                    }
                }
                return UIEventResult.Handled;
            });
        }

        public Accordian(params Static[] children)
            : this()
        {
            AddChildren(children);
        }

        public override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            var shade = new Shade(child)
            {
                IsMinimized = InitiallyCollapsed
            };
            var binding = new Data.Binding("Name", "Text");
            binding.BindTo(child, shade);
            shade.Bindings = new List<Data.Binding> { binding };

            return base.InternalInsertChild(shade, index, reflow, ignoreFocus);
        }

        public override void BindTo(object source, Dictionary<string, object> customBindProps = null)
        {
            foreach (var child in Children)
            {
                //assumes correct hierarchy
                child.Bindings[0].BindTo(child.Children[0], child);
                child.Children[0].BindTo(source, customBindProps);
            }
        }
    }
}
