using System.IO;
#if WINDOWS
using System.Windows.Forms;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public class FileInputBase : List
    {
        /// <summary>
        /// The file, validated
        /// </summary>
        public virtual string Value
        {
            get => _value;
            set
            {
                if (value == _value)
                    return;

                _value = (RelativeRoot != null && value != null ? Data.Cache.GetRelativePath(value) : value);
                IsFileValid = !VerifyFileExists || value == null || value.Length == 0 || File.Exists(value);

                BubbleEvent(ValueChangedEvent, new UIEventArgs(this));
            }
        }
        private string _value;

        public bool IsFileValid { get; private set; }


        /// <summary>
        /// If not null, use relative file paths, rooted at this value.
        /// Defaults to "" (Cache.ContentRoot)
        /// </summary>
        public string RelativeRoot { get; set; } = string.Empty;


        //todo: directory support

        /// <summary>
        /// On input, show a little marker whether or not the file exists
        /// </summary>
        public bool VerifyFileExists { get; set; } = true;
    }

    public enum DialogMode
    {
        Open,
        Save
    }

    public class FileInput : FileInputBase
    {
        public static Color InvalidFileOutlineColor = Color.Tomato;

        public override string Value
        {
            get => base.Value;
            set
            {
                if (base.Value == value)
                    return;

                base.Value = value;
                textInput.Text = base.Value;
                DisplayValidation();
            }
        }

        public override Graphics.Font Font
        {
            get => textInput.Font;
            set
            {
                textInput.Font = pickerButton.Font = value;
            }
        }
        public override Graphics.TextStyle TextStyle
        {
            get => textInput.TextStyle;
            set
            {
                if (textInput != null)
                    textInput.TextStyle = pickerButton.TextStyle = value;
            }
        }

        public override Color Color
        {
            get => textInput.Color;
            set
            {
                if (textInput != null)
                    textInput.Color = pickerButton.Color = value;
            }
        }

        protected TextInput textInput = new TextInput
        {
            Styles = "FileInput.TextInput",
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch,
            BorderColor = Color.Transparent
        };
        protected Static pickerButton = new Static
        {
            Styles = "FileInput.PickerButton",
            VerticalAlignment = Alignment.Stretch,
            Text = "...",
            Padding = new Vector2(5, 0)
        };

        public virtual string InitialDirectory { get; set; } = "";
        public virtual string Filter { get; set; } = null;

        /// <summary>
        /// The mode of this file input, by default, open
        /// </summary>
        public DialogMode Mode { get; set; } = DialogMode.Open;

        private Color lastBorderColor;

        public FileInput()
        {
            Direction = Direction.Horizontal;

            On(TextChangedEvent, delegate
            {
                Value = textInput.Text;
                return UIEventResult.Handled;
            });

            pickerButton.EventCommands[ClickEvent] = "OpenFileSelector";
            AddChildren(textInput, pickerButton);

            CommandActions["OpenFileSelector"] = DoOpenFileSelector;
        }

        protected void DoOpenFileSelector(Static source, object startingDir)
        {
            string sStartingDir = startingDir as string ?? InitialDirectory;
#if WINDOWS
#if DEBUG
            if (Input.InputState.IsMod(Input.KeyMod.Alt))
            {
                System.Diagnostics.Process.Start(Path.GetDirectoryName(Text));
                return;
            }
#endif

            FileDialog dialog = null;
            if (Mode == DialogMode.Open)
                dialog = new OpenFileDialog();
            else if (Mode == DialogMode.Save)
                dialog = new SaveFileDialog();

            dialog.FileName = Text;
            dialog.AutoUpgradeEnabled = true;
            dialog.RestoreDirectory = true;
            dialog.InitialDirectory = sStartingDir;
            dialog.Filter = Filter;
            dialog.ValidateNames = true;
            dialog.CheckFileExists = VerifyFileExists;
            dialog.CustomPlaces.Add(Path.GetFullPath("Content"));

            using (dialog)
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                    Value = dialog.FileName;
            }
#elif WINDOWS_UAP
                //todo
#endif
        }

        protected void DisplayValidation()
        {
            if (IsFileValid)
            {
                BorderColor = lastBorderColor;
                BubbleEvent(ValueChangedEvent, new UIEventArgs(this));
            }
            else
            {
                lastBorderColor = BorderColor;
                BorderColor = InvalidFileOutlineColor;
            }
        }
    }
}
