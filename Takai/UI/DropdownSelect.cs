using Microsoft.Xna.Framework;
using System;

namespace Takai.UI
{
    public class DropdownSelect<T> : Static
    {
        protected Static dropdownContainer;
        protected ScrollBox dropdown;
        public ItemList<T> list;
        protected Static preview;

        public T SelectedItem
        {
            get => list.SelectedItem;
            set
            {
                list.SelectedItem = value;
            }
        }
        public int SelectedIndex
        {
            get => list.SelectedIndex;
            set
            {
                list.SelectedIndex = value;
            }
        }

        public static Range<int> DropdownHeight { get; set; } = new Range<int>(120, 300);

        public Static ItemUI { get => list.ItemUI; set => list.ItemUI = value; }

        public System.Collections.Generic.IList<T> Items => list.Items;

        public override bool CanFocus => true;

        public bool AllowDefaultValue
        {
            get => _allowDefaultValue;
            set
            {
                if (value == _allowDefaultValue)
                    return;

                _allowDefaultValue = value;
                if (_allowDefaultValue)
                    Items.Insert(0, default);
                else
                    Items.Remove(default);
            }
        }
        bool _allowDefaultValue;

        public DropdownSelect()
        {
            Style = "DropdownSelect";

            list = new ItemList<T>()
            {
                HorizontalAlignment = Alignment.Stretch,
            };
            list.Container.Style = "Dropdown.List";

            dropdown = new ScrollBox(list)
            {
                Style = "Dropdown"
            };

            dropdownContainer = new Static(dropdown)
            {
                BackgroundColor = new Color(0, 0, 0, 127),
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };
            dropdownContainer.On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                sender.RemoveFromParent();
                return UIEventResult.Handled;
            });

            On(ClickEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (DropdownSelect<T>)sender;
                if (self.dropdownContainer.Parent != null)
                    self.CloseDropDown();
                else
                    self.OpenDropdown();

                return UIEventResult.Handled;
            });

            dropdown.On(SelectionChangedEvent, OnSelectionChanged_Dropdown);

            On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                ((DropdownSelect<T>)sender).CloseDropDown();
                return UIEventResult.Continue;
            });
        }

        public override void BindTo(object source, System.Collections.Generic.Dictionary<string, object> customBindProps = null)
        {
            //internal UI elements have their own bindings
            BindToThis(source, customBindProps);
        }

        protected UIEventResult OnSelectionChanged_Dropdown(Static sender, UIEventArgs e)
        {
            //clones must bind directly to this

            //todo: arrow keying through dropdown entries sometimes leaves preview blank
            // (fills in once dropdown is closed)
            var sea = (SelectionChangedEventArgs)e;

            var childIndex = preview?.ChildIndex ?? -1;
            if (sea.newIndex >= 0)
            {
                ReplaceChild(preview = list.Container.Children[sea.newIndex].CloneHierarchy(), childIndex);
                preview.Arrange(new Rectangle(0, 0, ContentArea.Width, ContentArea.Height)); //todo: still needs work
                preview.BindTo(list.Items[sea.newIndex]);
            }
            else if (preview != null)
                preview.BindTo(null);

            BubbleEvent(this, SelectionChangedEvent, e);
            return UIEventResult.Handled; //the dropdown is not part of the main tree
        }

        protected override void FinalizeClone()
        {
            dropdownContainer = dropdownContainer.CloneHierarchy();
            dropdown = (ScrollBox)dropdownContainer.Children[0];
            list = (ItemList<T>)System.Linq.Enumerable.First(dropdown.EnumerableChildren);
            
            var previewChildIndex = preview?.ChildIndex ?? -1;
            if (previewChildIndex >= 0)
            {
                ReplaceChild(preview = list.Container.Children[list.SelectedIndex].CloneHierarchy(), previewChildIndex);
                preview.BindTo(list.SelectedItem);
            }
            else
                preview = null;

            dropdown.Off(SelectionChangedEvent);
            dropdown.On(SelectionChangedEvent, OnSelectionChanged_Dropdown);

            base.FinalizeClone();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            dropdownContainer.Measure(new Vector2(InfiniteSize));
            return new Vector2(list.MeasuredSize.X, 20); //todo
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            if (dropdownContainer.Parent != null)
            {
                var root = GetRoot();
             
                var dsz = new Point(
                    (int)availableSize.X,
                    (int)dropdownContainer.MeasuredSize.Y
                );
                dsz.Y = MathHelper.Clamp(
                    Math.Min(dsz.Y, DropdownHeight.max), 
                    DropdownHeight.min, 
                    root.OffsetContentArea.Height - OffsetContentArea.Bottom
                );

                var dpos = new Point(OffsetContentArea.Left - (int)Padding.X, OffsetContentArea.Bottom);
                
                //keep on-screen
                dpos.X = MathHelper.Clamp(dpos.X, 0, root.OffsetContentArea.Width - dsz.X);
                dpos.Y = MathHelper.Clamp(dpos.Y, 0, root.OffsetContentArea.Height - dsz.Y);

                dropdownContainer.Arrange(root.OffsetContentArea);
                dropdown.Measure(dsz.ToVector2()); //todo: this is not working
                dropdown.Arrange(new Rectangle(dpos.X, dpos.Y, dsz.X, dsz.Y));
            }
            base.ArrangeOverride(availableSize);
        }

        public virtual void OpenDropdown()
        {
            if (Items.Count < 1)
                return;

            var root = GetRoot();
            root.AddChild(dropdownContainer);
            InvalidateArrange();
            // arrange now?
        }

        public virtual void CloseDropDown()
        {
            dropdownContainer.RemoveFromParent();
        }

        protected override bool HandleInput(GameTime time)
        {
            if (dropdownContainer.Parent != null &&
                (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Escape) ||
                Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Space) ||
                Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Enter)))
                CloseDropDown();

            else if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Up))
                --SelectedIndex;
            else if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Down))
                ++SelectedIndex;
            else if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Home))
                SelectedIndex = -1;
            else if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.End))
                SelectedIndex = Items.Count - 1;
            else
                return base.HandleInput(time);

            return false;
        }
    }
}
