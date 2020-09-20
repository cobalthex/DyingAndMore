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

        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (_isCollapsed == value)
                    return;

                _isCollapsed = value;
                BubbleEvent(VisibilityChangedEvent, new VisibilityChangedEventArgs(this, _isCollapsed));
                InvalidateMeasure();
            }
        }
        private bool _isCollapsed;

        private float desiredContentSize; //for animation
        private float actualContentSize;

        public Static TitleUI
        {
            get => _titleUI;
            set
            {
                if (_titleUI == value)
                    return;

                //validate not null?

                _titleUI = value;
                _titleUI?.BindTo(this);
                base.InternalSwapChild(_titleUI, 0);
            }
        }
        Static _titleUI = new Static
        {
            HorizontalAlignment = Alignment.Stretch,
            Bindings = new List<Data.Binding>
            {
                new Data.Binding("Name", "Text")
            }
        };

        public Shade()
        {
            base.InternalInsertChild(TitleUI, 0);
            TitleUI.BindTo(this);

            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (Shade)sender;
                if (e.Source == sender) //prevents annoying click-throughs
                    self.IsCollapsed ^= true;
                return UIEventResult.Handled;
            });
        }
        public Shade(params Static[] children)
            : this()
        {
            AddChildren(children);
        }

        protected override void FinalizeClone()
        {
            base.FinalizeClone();
            _titleUI = Children[0];
            TitleUI.BindTo(this);
        }

        public override void BindTo(object source, Dictionary<string, object> customBindProps = null)
        {
            for (int i = 1; i < Children.Count; ++i)
                Children[i].BindTo(source, customBindProps);
        }

        public override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalInsertChild(child, index < 0 ? -1 : index + 1, reflow, ignoreFocus);
        }
        public override bool InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalSwapChild(null, index + 1, reflow, ignoreFocus);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            //todo: animation
            var measured = base.MeasureOverride(availableSize) + new Vector2(0, Padding.Y);
            if (IsCollapsed)
                measured.Y = 0;

            var header = TitleUI.Measure(availableSize);

            measured = new Vector2(MathHelper.Max(header.X, measured.X), header.Y + measured.Y);
            desiredContentSize = measured.Y;

            return new Vector2(measured.X, actualContentSize);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            TitleUI.Arrange(new Rectangle(0, 0, (int)availableSize.X, (int)TitleUI.MeasuredSize.Y));

            var container = new Rectangle(
                0,
                (int)TitleUI.MeasuredSize.Y,
                (int)availableSize.X,
                Math.Max(0, (int)availableSize.Y - (int)TitleUI.MeasuredSize.Y)
            );
            for (int i = 1; i < Children.Count; ++i)
                Children[i].Arrange(container);
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

        internal void CollapseNoEvent() //for use in accordian
        {
            if (_isCollapsed)
                return;

            _isCollapsed = true;
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

        public Static ShadeTitleUI
        {
            get => _shadeTitleUI;
            set
            {
                if (_shadeTitleUI == value)
                    return;

                _shadeTitleUI = value;
                if (_shadeTitleUI != null)
                {
                    foreach (var child in Children)
                        ((Shade)child).TitleUI = _shadeTitleUI;
                }
            }
        }

        private Static _shadeTitleUI = null;

        //collapse new vs first?

        public Accordian() 
        {
            On(Shade.VisibilityChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                if (e.Source.Parent != sender)
                    return UIEventResult.Continue;

                var self = (Accordian)sender;
                var vcea = (VisibilityChangedEventArgs)e;
                if (!vcea.IsMinimized)
                {
                    foreach (var child in self.Children)
                    {
                        if (child != vcea.Source)
                            ((Shade)child).CollapseNoEvent();
                    }
                }
                self.InvalidateMeasure();
                self.InvalidateArrange();
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
                IsCollapsed = InitiallyCollapsed,
                HorizontalAlignment = Alignment.Stretch
            };
            var binding = new Data.Binding("Name", "Name");
            binding.BindTo(child, shade);
            shade.Bindings = new List<Data.Binding> { binding };

            if (ShadeTitleUI != null)
                shade.TitleUI = ShadeTitleUI.CloneHierarchy();

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
