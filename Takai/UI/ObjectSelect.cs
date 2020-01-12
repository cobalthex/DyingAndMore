using System.Collections.Generic;

namespace Takai.UI
{
    /// <summary>
    /// Allows for selection of a class/instance object and be able to retrieve either the instance or class
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <typeparam name="TInstance"></typeparam>
    public class ObjectSelect<TClass, TInstance> : DropdownSelect<TClass>
        where TClass : Data.IClass<TInstance>
        where TInstance : Data.IInstance<TClass>
        //todo: there may be a way to do this with only TClass being IClassBase
    {
        /// <summary>
        /// An instance of the selected weapon. Created on demand (and cached)
        /// </summary>
        public TInstance Instance
        {
            get
            {
                if (!hasInstance)
                {
                    hasInstance = true;
                    if (EqualityComparer<TClass>.Default.Equals(SelectedItem, default(TClass)))
                        _instance = default(TInstance);
                    else
                        _instance = SelectedItem.Instantiate();
                }
                return _instance;
            }
            set
            {
                if (EqualityComparer<TInstance>.Default.Equals(_instance, value))
                    return;

                if (EqualityComparer<TInstance>.Default.Equals(value, default(TInstance)))
                    SelectedIndex = -1;
                else
                    SelectedItem = value.Class;

                _instance = value;
                hasInstance = true;
            }
        }
        private TInstance _instance;
        bool hasInstance = false;

        public ObjectSelect()
        {
            ItemUI = new Static
            {
                Bindings = new List<Data.Binding>
                {
                    new Data.Binding("Name", "Text", Data.BindingDirection.OneWay, "(None)")
                }
            };

            On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (ObjectSelect<TClass, TInstance>)sender;
                self._instance = default(TInstance);
                self.hasInstance = false;
                return UIEventResult.Handled;
            });
        }
    }
}