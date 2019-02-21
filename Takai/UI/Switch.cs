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

                _items = value;
                SetActiveItem(Value);
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

                _fallbackItem = value;
                SetActiveItem(Value);
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

                _value = value;
                SetActiveItem(_value);
            }
        }
        object _value;

        //finalize clone

        public Switch() { }

        protected void SetActiveItem(object newValue)
        {
            Static item = null;
            if (Value == null || Items == null || !Items.TryGetValue(Value, out item))
                item = FallbackItem;

            var oldIndex = IndexOf(item);
            if (Value == null || Items == null || !Items.TryGetValue(newValue, out item))
                item = FallbackItem;

            ReplaceChild(item, oldIndex);
        }
    }
}
