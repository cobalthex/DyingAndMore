using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Data;

namespace Takai.UI
{
    /// <summary>
    /// Ignore this field when generating a prop sheet (Serializer.Ignored also behaves the same)
    /// </summary>
    public class HiddenAttribute : Attribute { }

    /// <summary>
    /// when generating a UI for this type, 
    /// </summary>
    public class CustomEditorAttribute : Attribute
    {
        /// <summary>
        /// tye name of the editor type to use
        /// </summary>
        public string EditorName { get; set; }

        public CustomEditorAttribute(string editorName)
        {
            EditorName = editorName;
        }
    }

    // build from derived types attribute

    public class LoadFromFileAttribute : Attribute { }

    /// <summary>
    /// Generate A UI whenever the binding to 'this' is updated
    /// </summary>
    public class GeneratedUI : Static
    {
        public string BindSource { get; set; }

        private Binding proxyBinding;

        /// <summary>
        /// Proxy object to update the UI when the bind source updates.
        /// Bindings are set up to forward the bind source object to this, which in turn re-renders the UI
        /// </summary>
        private object BindProxy
        {
            set
            {
                if (value == null)
                {
                    RemoveAllChildren();
                    return;
                }

                var ui = GeneratePropSheet(value.GetType(), value, false);
                ui.HorizontalAlignment = Alignment.Stretch;
                ReplaceAllChildren(ui);
            }
        }

        public GeneratedUI()
        {
        }
        public GeneratedUI(string bindSource) : this()
        {
            if (string.IsNullOrEmpty(bindSource))
                throw new ArgumentNullException(nameof(BindSource) + " cannot be null");

            BindSource = bindSource;

            proxyBinding = new Binding(BindSource, nameof(BindProxy));
        }

        public override void BindTo(object source, Dictionary<string, object> customBindProps = null)
        {
            base.BindTo(source, customBindProps);
            proxyBinding.BindTo(source, this, null, true);
        }

        protected override void UpdateSelf(GameTime time)
        {
            base.UpdateSelf(time);
            proxyBinding.Update();
        }
    }

    public partial class Static
    {
        /// <summary>
        /// Get the UI editor for a specific type, bound to <see cref="obj"/>
        /// </summary>
        /// <param name="obj">the object with type and binding source</param>
        /// <returns>THe editor, null if there is no associated editor</returns>
        public static Static GetEditor(object obj, CustomEditorAttribute customEditor = null)
        {
            return null;
        }

        public static Static GeneratePropSheet<T>(T obj = default, bool inlineSmallStructs = true)
        {
            return GeneratePropSheet(typeof(T), obj, inlineSmallStructs);
        }
        public static Static GeneratePropSheet(object obj, bool inlineSmallStructs = true)
        {
            if (obj == null)
                return null;
            return GeneratePropSheet(obj.GetType(), obj, inlineSmallStructs);
        }

        internal static Static GeneratePropSheet(Type type, object obj, bool inlineSmallStructs = true)
        {
            var customEditor = type.GetTypeInfo().GetCustomAttribute<CustomEditorAttribute>();
            if (customEditor != null)
            {
                var editor = Cache.Load<Static>($"UI/Types/custom/{customEditor.EditorName}.ui.tk").CloneHierarchy();
                editor.BorderColor = Color.SteelBlue;
                editor.Padding = new Vector2(10);
                editor.BindTo(obj);
                return editor;
            }

            if (obj == default)
            {
                if (type.IsAbstract || (!type.IsValueType && type.GetConstructor(new Type[] { }) == null))
                    return null;
                obj = Activator.CreateInstance(type);
            }

            //todo: item spacing (can't use list margin without nesting)

            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);

            //var root = new Table {
            //    ColumnCount = 2,
            //    Margin = new Vector2(5),
            //    Padding = new Vector2(5),
            //    BackgroundColor = new Color(1, 1, 1, 0.1f),
            //};
            var root = new List
            {
                BorderColor = Color.SteelBlue,
                Padding = new Vector2(10),
            };

            int memCount = 0;
            //todo: move these into type handlers
            foreach (var member in members)
            {
                //helps avoid cyclical issues + unwanted UI elements
                if (member.IsDefined(typeof(Serializer.IgnoredAttribute)) ||
                    member.IsDefined(typeof(HiddenAttribute)))
                    continue;

                Type memberType;
                object memberVal;
                {
                    var fieldInfo = member as FieldInfo;
                    var propInfo = member as PropertyInfo;
                    if (fieldInfo != null)
                    {
                        if (fieldInfo.IsInitOnly || !fieldInfo.IsPublic)
                            continue;
                        memberType = fieldInfo.FieldType;
                        memberVal = fieldInfo.GetValue(obj);
                    }
                    else if (propInfo != null)
                    {
                        if (!propInfo.CanWrite || !propInfo.SetMethod.IsPublic)
                            continue;
                        memberType = propInfo.PropertyType;
                        memberVal = propInfo.GetValue(obj);
                    }
                    else
                        continue;
                }

                if (memberType == typeof(Type))
                    continue;

                ++memCount;

                customEditor = member.GetCustomAttribute<CustomEditorAttribute>();
                if (customEditor != null)
                {
                    var editor = Cache.Load<Static>($"UI/Types/custom/{customEditor.EditorName}.ui.tk").CloneHierarchy();
                    editor.BorderColor = Color.SteelBlue;
                    editor.BindTo(memberVal);
                    root.AddChild(editor);
                    continue;
                }

                // because checkboxes have their own text field, use that instead of the label
                if (memberType == typeof(bool))
                {
                    var cbox = new CheckBox
                    {
                        Text = Util.ToSentenceCase(member.Name),
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, nameof(CheckBox.IsChecked), BindingDirection.TwoWay)
                        }
                    };
                    cbox.BindTo(obj);
                    root.AddChild(cbox);
                    continue;
                }

                //label
                var label = Util.ToSentenceCase(member.Name);
#if DEBUG
                if (memberType.IsGenericType)
                {
                    label += $" ({memberType.Name}<"; // remove `n ?
                    var targs = memberType.GenericTypeArguments;
                    for (int i = 0; i < targs.Length; ++i)
                        label += (i > 0 ? "," : "") + targs[i];
                    label += ">)";
                }
                else
                    label += $" ({memberType.Name})";
#endif
                root.AddChild(new Static(label)
                {
                    VerticalAlignment = Alignment.Center,
                });

                if (typeof(ISerializeExternally).IsAssignableFrom(memberType) || member.IsDefined(typeof(LoadFromFileAttribute)))
                {
                    var input = new FileInput
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, nameof(FileInput.Value), BindingDirection.TwoWay)
                        }
                    };
                    input.BindTo(obj);
                    root.AddChild(input);
                }

                else if (memberType.IsGenericType)
                {
                    var genericType = memberType.GetGenericTypeDefinition();
                    if (genericType == typeof(List<>)) //todo: make this work for any ienumerable
                    {
                        var itemType = memberType.GenericTypeArguments[0];

                        var uiType = typeof(ItemList<>).MakeGenericType(memberType.GenericTypeArguments);
                        var ui = (Static)Activator.CreateInstance(uiType);

                        var itemUI = GeneratePropSheet(itemType, default, inlineSmallStructs);
                        uiType.GetProperty("ItemUI", BindingFlags.Public | BindingFlags.Instance)
                            .SetValue(ui, itemUI);

                        //var addItemUI = new List(itemUI, new Static("+") { EventCommands = { [ClickEvent] = "AddItem" }, VerticalAlignment = Alignment.Bottom })
                        //{
                        //    Direction = Direction.Horizontal,
                        //    Margin = 5
                        //};
                        //uiType.GetProperty("AddItemUI", BindingFlags.Public | BindingFlags.Instance)
                        //    .SetValue(ui, addItemUI);

                        ui.HorizontalAlignment = Alignment.Stretch;
                        ui.Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Items", BindingDirection.TwoWay)
                        };
                        ui.BindTo(obj);
                        root.AddChild(ui);
                    }
                    else if (genericType == typeof(Range<>))
                    {
                        var ui = new List
                        {
                            Style = "Range",
                            Direction = Direction.Horizontal,
                        };

                        var itemType = memberType.GenericTypeArguments[0];
                        var editor = GeneratePropSheet(itemType);
                        ui.AddChild(editor.CloneHierarchy());
                        ui.AddChild(new Static("-"));
                        ui.AddChild(editor);

                        ui.BindTo(obj);
                        root.AddChild(ui);
                    }
                    else if (genericType == typeof(Dictionary<,>))
                    {
                        var uiType = typeof(KeyValueTable<,>).MakeGenericType(memberType.GenericTypeArguments);
                        var ui = (Static)Activator.CreateInstance(uiType);

                        //todo: key UI
                        var itemUI = GeneratePropSheet(memberType.GenericTypeArguments[1], default, inlineSmallStructs);
                        uiType.GetProperty("ItemUI", BindingFlags.Public | BindingFlags.Instance)
                            .SetValue(ui, itemUI);

                        //var addItemUI = new List(itemUI, new Static("+") { EventCommands = { [ClickEvent] = "AddItem" }, VerticalAlignment = Alignment.Bottom })
                        //{
                        //    Direction = Direction.Horizontal,
                        //    Margin = 5
                        //};
                        //uiType.GetProperty("AddItemUI", BindingFlags.Public | BindingFlags.Instance)
                        //    .SetValue(ui, addItemUI);

                        ui.HorizontalAlignment = Alignment.Stretch;
                        ui.Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Items", BindingDirection.TwoWay)
                        };
                        ui.BindTo(obj);
                        root.AddChild(ui);
                    }
                    else
                        root.AddChild(new Static($"({genericType.Name}<{(string.Join(",", (object[])memberType.GenericTypeArguments))}>)"));
                }
                else if (memberType.IsArray)
                {
                    var itemType = memberType.GetElementType();

                    var uiType = typeof(ItemList<>).MakeGenericType(itemType);
                    var ui = (Static)Activator.CreateInstance(uiType);

                    var itemUI = GeneratePropSheet(itemType, default, inlineSmallStructs);
                    uiType.GetProperty("ItemUI", BindingFlags.Public | BindingFlags.Instance)
                        .SetValue(ui, itemUI);

                    ui.HorizontalAlignment = Alignment.Stretch;
                    ui.Bindings = new List<Binding>
                    {
                        new Binding(member.Name, "Items", BindingDirection.TwoWay)
                    };
                    ui.BindTo(obj);
                    root.AddChild(ui);
                }
                else if (memberType.IsEnum)
                {
                    var enumSelect = typeof(EnumSelect<>);
                    var enums = (Static)Activator.CreateInstance(enumSelect.MakeGenericType(memberType));
                    enums.Bindings = new List<Binding>
                    {
                        new Binding(member.Name, nameof(EnumSelect<Enum>.Value /* generic doesn't matter */), BindingDirection.TwoWay)
                    };
                    enums.BindTo(obj);
                    root.AddChild(enums);
                }
                else if (memberType.IsInterface)
                {
                    root.AddChild(new TypeSelect(memberType));
                    // add nested generated UI for selected type? (w/ combo)
                }
                else if (memberType == typeof(Texture2D))
                {
                    var ui = new List
                    {
                        Style = "Box",
                        Direction = Direction.Horizontal,
                        HorizontalAlignment = Alignment.Stretch,
                    };
                   ui.AddChildren(new FileInput
                    {
                        HorizontalAlignment = Alignment.Stretch,
                        VerticalAlignment = Alignment.Center,
                        Filter = "Textures|*.dds;*.png;*.jpg;*.bmp;*.gif",
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, nameof(FileInput.Value), BindingDirection.TwoWay)
                        },
                    },
                    new Graphic
                    {
                        Style = "Texture2D.Preview",
                        Sprite = new Graphics.Sprite(),
                        Size = new Vector2(128),
                    });
                    ui.BindTo(obj);
                    ui.On(ValueChangedEvent, delegate (Static sender, UIEventArgs e)
                    {
                        var file = ((FileInputBase)e.Source).Value;
                        // gross hacky leaky abstraction
                        ((Graphic)sender.Children[1]).Sprite = Cache.Load<Texture2D>(file);
                        return UIEventResult.Handled;
                    });
                    root.AddChild(ui);
                }
                else if (Serializer.IsInt(memberType) || Serializer.IsFloat(memberType))
                {
                    var ui = new NumericInput
                    {
                        //set min/max ?
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Value", BindingDirection.TwoWay),
                        },
                    };
                    ui.BindTo(obj);
                    root.AddChild(ui);
                }
                else if (memberType == typeof(string))
                {
                    var ui = new TextInput
                    {
                        Bindings = new List<Binding>
                        {
                            new Binding(member.Name, "Text", BindingDirection.TwoWay),
                        },
                    };
                    ui.BindTo(obj);
                    root.AddChild(ui);
                }
                else if (typeof(Static).IsAssignableFrom(memberType))
                {
                    root.AddChild(new Static("[[ UI ]]")); //todo
                }
                else
                {
                    try
                    {
                        var editor = Cache.Load<Static>($"UI/Types/{memberType.Name}.ui.tk").CloneHierarchy();
                        editor.BindTo(memberVal);
                        root.AddChild(editor);
                        continue;
                    }
                    catch 
                    {
                        root.AddChild(new Static($"[No editor found for '{memberType}']"));
                    }

                    var nested = GeneratePropSheet(memberType, memberVal, inlineSmallStructs);
                    if (nested != null)
                        root.AddChild(nested);
                }
            }

            root.Direction = inlineSmallStructs && type.IsValueType && memCount <= 4 ? Direction.Horizontal : Direction.Vertical;
            return root;
        }

    }
}
