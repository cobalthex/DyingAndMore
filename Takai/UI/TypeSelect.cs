using System;
using System.Collections.Generic;
using System.Reflection;

namespace Takai.UI
{
    //A dropdown that allows picking from a list of types and constructing an instance
    public class TypeSelect : DropdownSelect<(string name, Type type)>
    {
        public object Instance
        {
            get
            {
                if (SelectedIndex >= 0 && SelectedItem.type != null)
                    return (_instance ?? (_instance = Activator.CreateInstance(SelectedItem.type)));
                else
                    return null;
            }
            set
            {
                if (value == _instance)
                    return;

                _instance = value;

                if (value == null)
                    SelectedIndex = -1;
                else
                {
                    var type = value.GetType();
                    for (int i = 0; i < Items.Count; ++i)
                    {
                        if (Items[i].type == type)
                        {
                            SelectedIndex = i;
                            return;
                        }
                    }
                    SelectedIndex = -1;
                }

            }
        }
        private object _instance;

        public TypeSelect()
        {
            ItemUI = new Static
            {
                HorizontalAlignment = Alignment.Stretch,
                Bindings = new List<Data.Binding>
                {
                    new Data.Binding("Item1", "Text")
                }
            };

            On(SelectionChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (TypeSelect)sender;
                self._instance = null;
                return UIEventResult.Handled;
            });
        }

        public TypeSelect(Type type) : this()
        {
            AddTypeTree(type);
        }

        public static TypeSelect FromType<T>()
        {
            var sel = new TypeSelect();
            sel.AddTypeTree(typeof(T));
            return sel;
        }

        public void AddTypeTree<T>()
        {
            AddTypeTree(typeof(T));
        }
        
        (string name, Type type) GetNameType(Type type)
        {
            var dna = type.GetCustomAttribute<DisplayNameAttribute>();
            if (dna != null)
                return (dna.Name, type);
            return (Util.ToSentenceCase(type.Name), type);
        }

        /// <summary>
        /// Add a type and all of its subclasses to the list
        /// Ignores any abstract classes
        /// </summary>
        /// <param name="type">The type to add. Can be abstract</param>
        public void AddTypeTree(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsAbstract)
                Items.Add(GetNameType(type));

            foreach (var rtype in Data.Serializer.RegisteredTypes)
            {
                var rti = rtype.Value.GetTypeInfo();
                if (!rti.IsAbstract && typeInfo.IsAssignableFrom(rtype.Value))
                    Items.Add(GetNameType(rtype.Value));
            }
        }


        public void AddTypeTreeByAttribute<T>() where T : Attribute
        {
            AddTypeTreeByAttribute(typeof(T));
        }

        /// <summary>
        /// Add a type and all of its subclasses to the list
        /// Ignores any abstract classes
        /// </summary>
        /// <param name="type">The type to add. Can be abstract</param>
        public void AddTypeTreeByAttribute(Type attributeType)
        {
            foreach (var rtype in Data.Serializer.RegisteredTypes)
            {
                var rti = rtype.Value.GetTypeInfo();
                if (!rti.IsAbstract && rtype.Value.IsDefined(attributeType, true))
                    Items.Add(GetNameType(rtype.Value));
            }
        }
    }
}
