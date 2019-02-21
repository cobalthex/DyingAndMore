namespace Takai.UI
{
    /// <summary>
    /// A tab panel that allows toggling between pages
    /// </summary>
    public class TabPanel : List
    {
        public Static TabBar
        {
            get => _tabBar;
            set
            {
                if (value == _tabBar)
                    return;

                if (value == null)
                    throw new System.ArgumentNullException("TabBar cannot be null");

                value.ReplaceAllChildren(_tabBar.Children);
                _tabBar = value;
            }
        }
        private Static _tabBar = new List { Direction = Direction.Horizontal, Margin = 10 };

        //current tab, etc
        public int TabIndex
        {
            get => _tabIndex;
            set
            {
                if (value == _tabIndex)
                    return;

                if (value >= TabBar.Children.Count)
                    throw new System.ArgumentOutOfRangeException("Tab index cannot be more han the number of tabs");

                var lastTabIndex = _tabIndex;
                _tabIndex = value;

                if (lastTabIndex >= 0)
                    Children[lastTabIndex + 1].IsEnabled = false;
                if (_tabIndex >= 0)
                    Children[_tabIndex + 1].IsEnabled = true;
                Reflow();
            }
        }
        private int _tabIndex;

        public TabPanel()
        {
            Direction = Direction.Vertical;
            base.InternalInsertChild(TabBar);
        }

        public TabPanel(params Static[] children)
            : this()
        {
            AddChildren(TabBar);

            On("_SelectTab", delegate (Static sender, UIEventArgs e)
            {
                var sce = (SelectionChangedEventArgs)e;
                TabIndex = sce.newIndex;
                return UIEventResult.Handled;
            });
        }

        protected override void FinalizeClone()
        {
            base.FinalizeClone();
        }

        public override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            var tabHeader = new Static(child.Name);
            tabHeader.On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                BubbleEvent(sender, "_SelectTab", new SelectionChangedEventArgs(sender, -1, sender.IndexOfParent));
                return UIEventResult.Handled;
            });
            TabBar.InsertChild(tabHeader, index);

            if (Children.Count > 1)
                child.IsEnabled = false;
            return base.InternalInsertChild(child, index, reflow, ignoreFocus);
        }

        public override bool InternalRemoveChildIndex(int index)
        {
            TabBar.RemoveChildAt(index);
            return base.InternalRemoveChildIndex(index + 1);
        }
    }
}
