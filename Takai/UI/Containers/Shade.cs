using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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

        protected override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalInsertChild(child, index < 0 ? -1 : index + 1, reflow, ignoreFocus);
        }
        protected override Static InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalSwapChild(null, index + 1, reflow, ignoreFocus);
        }
        protected override Static InternalRemoveChild(int index, bool reflow = true)
        {
            return base.InternalRemoveChild(index + 1, reflow);
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
            
            //actualContentSize = desiredContentSize; //testing

            return new Vector2(measured.X, actualContentSize);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            TitleUI.Arrange(new Rectangle(0, 0, (int)availableSize.X, (int)TitleUI.MeasuredSize.Y));

            var container = new Rectangle(
                0,
                (int)(TitleUI.MeasuredSize.Y + Padding.Y),
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
                //LogBuffer.Append(Name, actualContentSize, desiredContentSize);
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
}
