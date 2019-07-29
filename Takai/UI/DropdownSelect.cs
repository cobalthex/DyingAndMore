
using Microsoft.Xna.Framework;

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

        public float DropdownMaxHeight { get; set; } = 300;

        public Static ItemTemplate { get => list.ItemTemplate; set => list.ItemTemplate = value; }

        public System.Collections.Generic.ICollection<T> Items => list.Items;

        public override bool CanFocus => true;

        public DropdownSelect()
        {
            BorderColor = Color.White;

            list = new ItemList<T>()
            {
                HorizontalAlignment = Alignment.Stretch
            };

            dropdown = new ScrollBox(list)
            {
                Name = "dropdown",
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
            list = (ItemList<T>)System.Linq.Enumerable.First(dropdown.EnumerableChildren);

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

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            if (dropdownContainer.Parent != null)
                OpenDropdown();
            base.ArrangeOverride(availableSize);
        }

        public virtual void OpenDropdown()
        {
            if (Items.Count < 1)
                return;

            dropdown.Size = new Vector2(MeasuredSize.X, System.Math.Min(list.MeasuredSize.Y, DropdownMaxHeight));

            var root = GetRoot();

            var end = new Vector2(VisibleContentArea.Right + dropdown.VisibleBounds.Width, VisibleContentArea.Bottom + dropdown.VisibleBounds.Height);
            if (end.X > root.VisibleContentArea.Width || end.Y > root.VisibleContentArea.Height)
                dropdown.Position = VisibleContentArea.Location.ToVector2() - new Vector2(0, dropdown.VisibleBounds.Height);
            else
                dropdown.Position = VisibleContentArea.Location.ToVector2() + new Vector2(0, MeasuredSize.Y); //todo: smarter placement

            root.AddChild(dropdownContainer);
        }

        public virtual void CloseDropDown()
        {
            dropdownContainer.RemoveFromParent();
        }

        protected override bool HandleInput(GameTime time)
        {
            if (dropdownContainer.Parent != null && Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                CloseDropDown();
                return false;
            }

            return base.HandleInput(time);
        }
    }
}
