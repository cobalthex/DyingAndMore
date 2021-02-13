using System.Collections.Generic;
using Takai.UI;
using Takai.Data;
using Microsoft.Xna.Framework;
using Takai.Input;
using Microsoft.Xna.Framework.Input;

namespace DyingAndMore
{
    class ObjectClassDesigner : Static
    {
        string NewClassCommand = "CreateClass";
        string SaveClassCommand = "SaveClass";
        string OpenClassCommand = "OpenClass";

        List select;
        List editor;

        public IClassBase Class { get; private set; } //make rebind

        public ObjectClassDesigner()
        {
            var createButton = new Static("Create")
            {
                HorizontalAlignment = Alignment.Right,
            };
            createButton.EventCommands[ClickEvent] = NewClassCommand;

            var typeSelect = TypeSelect.FromType<IClassBase>();
            typeSelect.Bindings = new List<Binding>
            {
                new Binding(nameof(Class), nameof(TypeSelect.Instance), BindingDirection.TwoWay),
            };

            select = new List(
                new Static("New class"),
                typeSelect,
                createButton
            )
            {
                HorizontalAlignment = Alignment.Center,
                VerticalAlignment = Alignment.Center,
            };

            var sheet = new GeneratedUI(nameof(Class))
            {
                HorizontalAlignment = Alignment.Stretch,
            };
            editor = new List(
                new Static("Class Type")
                {
                    Bindings = new List<Binding>
                    {
                        new Binding(nameof(Class) + ":typename", "Text"),
                    }
                },
                sheet,
                new Divider(),
                new Static("Save")
                {
                    HorizontalAlignment = Alignment.Right,
                    EventCommands = new Dictionary<string, EventCommandBinding>
                    {
                        [ClickEvent] = SaveClassCommand,
                    },
                }
            )
            {
                IsEnabled = false,
            };

            editor.BindTo(this);
            select.BindTo(this);

            AddChildren(select, editor);

            CommandActions[NewClassCommand] = delegate (Static sender, object arg)
            {
                var self = (ObjectClassDesigner)sender;

                self.select.IsEnabled = false;
                self.editor.IsEnabled = true;
            };
            CommandActions[OpenClassCommand] = delegate (Static sender, object arg)
            {
                var self = (ObjectClassDesigner)sender;

                var dialog = new System.Windows.Forms.OpenFileDialog
                {
                    InitialDirectory = System.IO.Path.GetFullPath("Media/Content"),
                    Filter = "Takai definitions|*.tk",
                    SupportMultiDottedExtensions = true,
                    ValidateNames = true,
                    AutoUpgradeEnabled = true,
                    RestoreDirectory = true,
                    CheckFileExists = true,
                };
                dialog.CustomPlaces.Add(System.IO.Path.GetFullPath("Content"));

                using (dialog)
                {
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var obj = Cache.Load<IClassBase>(dialog.FileName);
                        Class = obj;
                    }
                }

                self.select.IsEnabled = false;
                self.editor.IsEnabled = true;
            };
            CommandActions[SaveClassCommand] = delegate (Static sender, object arg)
            {
                var self = (ObjectClassDesigner)sender;

                var dialog = new System.Windows.Forms.SaveFileDialog
                {
                    InitialDirectory = System.IO.Path.GetFullPath("Media/Content"),
                    Filter = "Takai definitions|*.tk",
                    OverwritePrompt = true,
                    SupportMultiDottedExtensions = true,
                    ValidateNames = true,
                    AutoUpgradeEnabled = true,
                    RestoreDirectory = true,
                };
                dialog.CustomPlaces.Add(System.IO.Path.GetFullPath("Content"));

                using (dialog)
                {
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        Serializer.TextSerialize(dialog.FileName, Class);
                }
            };
        }

        protected override void FinalizeClone()
        {
            base.FinalizeClone();
            select = (List)Children[0];
            editor = (List)Children[1];
        }

        protected override bool HandleInput(GameTime time)
        {
            if (InputState.IsPress(Keys.N))
            {
                editor.IsEnabled = false;
                Class = null;
                select.IsEnabled = true;

                return false;
            }
            if (InputState.IsPress(Keys.O))
            {
                BubbleCommand(OpenClassCommand);
                return false;
            }
            return base.HandleInput(time);
        }
    }
}
