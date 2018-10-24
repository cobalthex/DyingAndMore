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

            dropdownContainer = new Static(dropdown)
            {
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };
        }

        private void BindEvents() //make this virtual and put in Static? (would aleve some likely bugs with Clone())
        {
            dropdownContainer.Click += delegate (object sender, ClickEventArgs e)
            {
                ((Static)sender).RemoveFromParent();
            };

            list.SelectionChanged += delegate (object sender, SelectionChangedEventArgs e)
            {
                var childIndex = preview?.ChildIndex ?? -1;
                ReplaceChild(preview = list.Container.InternalChildren[SelectedIndex].Clone(), childIndex);
                preview.BindTo(SelectedItem);
                CloseDropDown();
                OnSelectedItemChanged(e);
            };
        }

        protected override void FinalizeClone()
        {
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

            GetRoot().AddChild(dropdownContainer);
        }

        public void CloseDropDown()
        {
            dropdownContainer.RemoveFromParent();
        }

        protected override void OnClick(ClickEventArgs e)
        {
            OpenDropdown();
            base.OnClick(e);
        }

        protected virtual void OnSelectedItemChanged(SelectionChangedEventArgs e) { }
    }
}
