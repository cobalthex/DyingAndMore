using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public class DropdownSelect<T> : Static
    {
        protected Static dropdownContainer;
        protected ScrollBox dropdown;
        protected ItemList<T> list;
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

        public Static ItemTemplate { get => list.ItemTemplate; set => list.ItemTemplate = value; }

        public System.Collections.Generic.ICollection<T> Items => list.Items;

        public override bool CanFocus => true;

        public DropdownSelect()
        {
            list = new ItemList<T>()
            {
                HorizontalAlignment = Alignment.Stretch
            };

            dropdown = new ScrollBox(list)
            {
                BorderColor = Color.White,
                BackgroundColor = new Color(32, 0, 128)
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

            dropdown.On(SelectionChangedEvent, OnSelectionChanged);
            //On(SelectionChangedEvent, OnSelectionChanged);

            On(ParentChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                //System.Diagnostics.Debugger.Break();
                return UIEventResult.Handled;
            });
        }

        public override void BindTo(object source)
        {
            //internal UI elements have their own bindings
            BindToThis(source);
        }

        protected UIEventResult OnSelectionChanged(Static sender, UIEventArgs e)
        {
            var childIndex = preview?.ChildIndex ?? -1;
            if (SelectedIndex >= 0)
            {
                ReplaceChild(preview = list.Container.Children[SelectedIndex].CloneHierarchy(), childIndex);
                preview.BindTo(SelectedItem);
            }
            else
                preview.BindTo(null);

            BubbleEvent(this, SelectionChangedEvent, e);
            return UIEventResult.Handled; //the dropdown is not part of the main tree
        }

        protected override void FinalizeClone()
        {
            dropdownContainer = dropdownContainer.CloneHierarchy();
            dropdown = (ScrollBox)dropdownContainer.Children[0];
            list = (ItemList<T>)dropdown.EnumerableChildren[0];

            var previewChildIndex = preview?.ChildIndex ?? -1;
            if (previewChildIndex >= 0)
            {
                ReplaceChild(preview = list.Container.Children[list.SelectedIndex].CloneHierarchy(), previewChildIndex);
                preview.BindTo(list.SelectedItem);
            }
            else
                preview = null;

            //rebind the dropdown events
            dropdown.Off(SelectionChangedEvent);
            dropdown.On(SelectionChangedEvent, OnSelectionChanged);

            base.FinalizeClone();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(200, 20);
        }

        public void OpenDropdown()
        {
            dropdown.Size = new Vector2(MeasuredSize.X, System.Math.Max(list.Size.Y, 200));
            list.Reflow();

            var root = GetRoot();

            var end = new Vector2(VisibleContentArea.Right + dropdown.VisibleBounds.Width, VisibleContentArea.Bottom + dropdown.VisibleBounds.Height);
            if (end.X > root.VisibleContentArea.Width || end.Y > root.VisibleContentArea.Height)
                dropdown.Position = VisibleContentArea.Location.ToVector2() - new Vector2(0, dropdown.VisibleBounds.Height);
            else
                dropdown.Position = VisibleContentArea.Location.ToVector2() + new Vector2(0, MeasuredSize.Y); //todo: smarter placement

            root.AddChild(dropdownContainer);
        }

        public void CloseDropDown()
        {
            dropdownContainer.RemoveFromParent();
        }
    }
}
