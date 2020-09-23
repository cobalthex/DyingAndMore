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

        protected Static tabBar; //todo: make modifyable?

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
                InvalidateMeasure();

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

        /// <summary>
        /// The active tab, or null if none
        /// </summary>
        public Static ActiveTab => TabIndex < 0 ? null : Children[TabIndex + 1];

        public bool NavigateWithNumKeys { get; set; } = false; 

        public TabPanel()
        {
            Direction = Direction.Vertical;

            tabBar = new Catalog
            {
                Direction = Direction.Horizontal,
                HorizontalAlignment = Alignment.Stretch,
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
            base.FinalizeClone();
            tabBar = Children[0];

            for (int i = 0; i < tabBar.Children.Count; ++i)
            {
                if (i + 1 >= Children.Count)
                    break; //throw?

                tabBar.Children[i].BindTo(Children[i + 1]);
            }
        }

        public override void BindTo(object source, Dictionary<string, object> customBindProps = null)
        {
            BindToThis(source, customBindProps);
            for (int i = 1; i < Children.Count; ++i)
                Children[i].BindTo(source, customBindProps);
        }

        protected override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
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

        protected override Static InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            if (child == null)
                tabBar.RemoveChildAt(index);
            else
                tabBar.Children[index].BindTo(child);

            return base.InternalSwapChild(child, index + 1, reflow, ignoreFocus);
        }

        protected override Static InternalRemoveChild(int index, bool reflow = true)
        {
            return base.InternalRemoveChild(index + 1, reflow);
        }

        protected override bool HandleInput(GameTime time)
        {
            //ctrl+(shift+)tab?

            if (NavigateWithNumKeys)
            {
                var pk = Input.InputState.GetPressedKeys();
                var tab = -1;
                foreach (var k in pk)
                {
                    if (k >= Microsoft.Xna.Framework.Input.Keys.D1 &&
                        k <= Microsoft.Xna.Framework.Input.Keys.D9)
                        tab = (k - Microsoft.Xna.Framework.Input.Keys.D1);

                    else if (k >= Microsoft.Xna.Framework.Input.Keys.NumPad1 &&
                        k <= Microsoft.Xna.Framework.Input.Keys.NumPad9)
                        tab = (k - Microsoft.Xna.Framework.Input.Keys.NumPad1);
                }
                if (tab >= 0 && tab < Children.Count - 1)
                {
                    TabIndex = tab;
                    return false;
                }
            }

            return base.HandleInput(time);
        }
    }
}
