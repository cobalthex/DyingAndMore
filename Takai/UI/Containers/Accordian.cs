using System.Collections.Generic;

namespace Takai.UI
{
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

        protected Shade MakeShade(Static child)
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

            return shade;
        }

        protected override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            return base.InternalInsertChild(MakeShade(child), index, reflow, ignoreFocus);
        }

        protected override Static InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            if (index < 0 || index >= Children.Count)
                return null;

            return base.InternalSwapChild(MakeShade(child), index, reflow, ignoreFocus);
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
