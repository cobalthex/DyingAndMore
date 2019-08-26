
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
                BorderColor = Color.White,
                BackgroundColor = new Color(32, 0, 128),
                Padding = new Vector2(2)
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
                CloseDropDown();
                return UIEventResult.Continue;
            });
        }

        public override void BindTo(object source)
        {
            //internal UI elements have their own bindings
            BindToThis(source);
        }

        protected UIEventResult OnSelectionChanged_Dropdown(Static sender, UIEventArgs e)
        {
            //clones must bind directly to this

            //todo: arrow keying through dropdown entries sometimes leaves preview blank
            // (fills in once dropdown is closed)

            var childIndex = preview?.ChildIndex ?? -1;
            if (SelectedIndex >= 0)
            {
                ReplaceChild(preview = list.Container.Children[SelectedIndex].CloneHierarchy(), childIndex);
                preview.BindTo(list.Items[SelectedIndex]);
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

            dropdown.Off(SelectionChangedEvent);
            dropdown.On(SelectionChangedEvent, OnSelectionChanged_Dropdown);

            base.FinalizeClone();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (dropdownContainer.Parent != null)
                dropdown.Measure(new Vector2(InfiniteSize));
            return new Vector2(200, 20);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            if (dropdownContainer.Parent != null)
            {
                dropdownContainer.Arrange(GetRoot().OffsetContentArea);

                var dpos = new Vector2(VisibleContentArea.X, VisibleContentArea.Y + MeasuredSize.Y);
                var dsz = new Vector2(
                    System.Math.Max(list.MeasuredSize.X, MeasuredSize.X),
                    System.Math.Min(list.MeasuredSize.Y, DropdownMaxHeight)
                );

                dpos.X = MathHelper.Clamp(dpos.X, 0, dropdownContainer.OffsetContentArea.Width - dsz.X);
                dpos.Y = MathHelper.Clamp(dpos.Y, 0, dropdownContainer.OffsetContentArea.Height - dsz.Y);

                dropdown.Position = dpos;
                dropdown.Size = dsz;
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
        }

        public virtual void CloseDropDown()
        {
            //todo: some hierarchy fuckup around here
            dropdownContainer.RemoveFromParent();
        }

        protected override bool HandleInput(GameTime time)
        {
            if (dropdownContainer.Parent != null && 
                (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Escape) ||
                Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Space) ||
                Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Enter)))
            {
                CloseDropDown();
                return false;
            }

            return base.HandleInput(time);
        }
    }
}
