using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
            set => list.SelectedItem = value;
        }
        public int SelectedIndex
        {
            get => list.SelectedIndex;
            set => list.SelectedIndex = value;
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

            dropdownContainer = new Static
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };

            On("Click", delegate (Static sender, UIEventArgs e)
            {
                if (dropdownContainer.Parent != null)
                    CloseDropDown();
                else
                    OpenDropdown();

                return UIEventResult.Handled;
            });

            On("SelectionChanged", delegate (Static sender, UIEventArgs e)
            {
                var sourceList = (ItemList<T>)e.Source;

                var childIndex = preview?.ChildIndex ?? -1;
                ReplaceChild(preview = sourceList.Container.Children[sourceList.SelectedIndex].Clone(), childIndex);
                preview.BindTo(sourceList.SelectedItem);
                CloseDropDown();
                return UIEventResult.Continue;
            });
        }

        protected override void FinalizeClone()
        {
            list = (ItemList<T>)list?.Clone();
            dropdown = (ScrollBox)dropdown?.Clone();
            preview = preview?.Clone();

            base.FinalizeClone();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(200, 20);
        }

        public void OpenDropdown()
        {
            dropdown.Size = new Vector2(MeasuredSize.X, System.Math.Max(list.Size.Y, 200));

            var root = GetRoot();

            var end = new Vector2(VisibleContentArea.Right, VisibleContentArea.Bottom) + dropdown.Size;
            if (end.X > root.VisibleContentArea.Width || end.Y > root.VisibleContentArea.Height)
                dropdown.Position = VisibleContentArea.Location.ToVector2() - new Vector2(0, dropdown.Size.Y);
            else
                dropdown.Position = VisibleContentArea.Location.ToVector2() + new Vector2(0, MeasuredSize.Y); //todo: smarter placement

            dropdownContainer.AddChild(dropdown);
            GetRoot().AddChild(dropdownContainer);
        }

        public void CloseDropDown()
        {
            dropdownContainer.RemoveFromParent();
        }
    }
}
