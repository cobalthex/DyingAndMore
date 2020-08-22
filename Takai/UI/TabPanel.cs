using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Takai.Data;

namespace Takai.UI
{
    /// <summary>
    /// A tab panel that allows toggling between pages
    /// </summary>
    public class TabPanel : List
    {
        public static string SelectTabCommand = "SelectTab";

        Static tabBar;

        //current tab, etc
        public int TabIndex
        {
            get => _tabIndex;
            set
            {
                if (value == _tabIndex)
                    return;

                if (value >= tabBar.Children.Count)
                    throw new ArgumentOutOfRangeException("Tab index cannot be more han the number of tabs");

                var lastTabIndex = _tabIndex;
                _tabIndex = value;

                if (lastTabIndex >= 0)
                {
                    Children[lastTabIndex + 1].IsEnabled = false;
                    tabBar.Children[lastTabIndex].Style = "TabPanel.TabHeader";
                }
                if (_tabIndex >= 0)
                {
                    Children[_tabIndex + 1].IsEnabled = true;
                    tabBar.Children[_tabIndex].Style = "TabPanel.TabHeader.Active";
                }
            }
        }
        private int _tabIndex = -1;

        public TabPanel()
        {
            Direction = Direction.Vertical;

            tabBar = new List
            {
                Name = "tabbar",
                Direction = Direction.Horizontal,
                Style = "TabPanel.TabBar",
            };
            base.InternalInsertChild(tabBar);

            tabBar.On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                if (e.Source == sender)
                    return UIEventResult.Continue;

                sender.BubbleCommand(SelectTabCommand, e.Source.ChildIndex);
                return UIEventResult.Handled;
            });

            CommandActions[SelectTabCommand] = delegate (Static sender, object arg)
            {
                ((TabPanel)sender).TabIndex = (int)arg;
                sender.InvalidateArrange();
                //emit changed event?
            };
        }

        public TabPanel(params Static[] children)
            : this()
        {
            AddChildren(children);
        }

        protected override void FinalizeClone()
        {
            tabBar = Children[0];
            base.FinalizeClone();
        }

        public override void BindTo(object source, Dictionary<string, object> customBindProps = null)
        {
            BindToThis(source, customBindProps);
            for (int i = 1; i < Children.Count; ++i)
                Children[i].BindTo(source, customBindProps);
        }

        public override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            var tabHeader = new Static
            {
                Style = "TabPanel.TabHeader",
                Bindings = new List<Binding>
                {
                    new Binding("Name", "Text")
                }
            };
            tabHeader.BindTo(child);
            tabBar.InsertChild(tabHeader, index);

            child.IsEnabled = false;
            var didInsert = base.InternalInsertChild(child, index < 0 ? -1 : index + 1, reflow, ignoreFocus);
            if (didInsert)
                TabIndex = Math.Min(0, tabBar.Children.Count);
            else
                tabBar.RemoveChild(tabHeader);
            return didInsert;
        }

        public override bool InternalRemoveChildIndex(int index, bool reflow = true)
        {
            tabBar.RemoveChildAt(index);
            return base.InternalRemoveChildIndex(index + 1, reflow);
        }
    }
}
