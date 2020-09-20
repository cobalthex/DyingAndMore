using System.Collections.Generic;

namespace Takai.UI
{
    /// <summary>
    /// A UI element that displays a specific child based on a condition
    /// </summary>
    public class Switch : Static
    {
        public Dictionary<object, Static> Items
        {
            get => _items;
            set
            {
                if (value == _items)
                    return;

                SetActiveItem(Value, Value);
                _items = value;
            }
        }
        Dictionary<object, Static> _items;

        public Static FallbackItem
        {
            get => _fallbackItem;
            set
            {
                if (value == _fallbackItem)
                    return;

                SetActiveItem(Value, Value);
                _fallbackItem = value;
            }
        }
        private Static _fallbackItem;

        public object Value
        {
            get => _value;
            set
            {
                if (value == _value)
                    return;

                SetActiveItem(_value, value);
                _value = value;
            }
        }
        object _value;

        public Switch() { }

        protected override void FinalizeClone()
        {
            var newItems = new Dictionary<object, Static>(Items.Count);
            foreach (var item in Items)
                newItems[item.Key] = item.Value.CloneHierarchy();
            Items = newItems;

            base.FinalizeClone();
        }

        void SetActiveItem(object oldValue, object newValue)
        {
            Static item = null;
            if (oldValue == null || Items == null || !Items.TryGetValue(oldValue, out item))
                item = FallbackItem;
            var oldIndex = IndexOf(item);

            if (newValue == null || Items == null || !Items.TryGetValue(newValue, out item))
                item = FallbackItem;

            if (item == null)
                return;

            if (oldIndex < 0)
                AddChild(item);
            else
                ReplaceChild(item, oldIndex);
        }
    }
}
