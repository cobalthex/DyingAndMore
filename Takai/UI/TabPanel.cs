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

                value.ReplaceAllChildren(_tabBar.InternalChildren);
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
                    InternalChildren[lastTabIndex + 1].IsEnabled = false;
                if (_tabIndex >= 0)
                    InternalChildren[_tabIndex + 1].IsEnabled = true;
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
        }

        protected override void FinalizeClone()
        {
            base.FinalizeClone();
        }

        public override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            var tabHeader = new Static(child.Name);
            tabHeader.Click += delegate (object sender, ClickEventArgs e)
            {
                var tab = (Static)sender;
                ((TabPanel)(tab.Parent.Parent)).TabIndex = tab.ChildIndex; //todo: this is fragile
            };
            TabBar.InsertChild(tabHeader, index);

            if (InternalChildren.Count > 1)
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
