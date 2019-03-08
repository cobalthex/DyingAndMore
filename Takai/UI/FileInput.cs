using System.IO;
#if WINDOWS
using System.Windows.Forms;
#endif
using Microsoft.Xna.Framework;

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
                _value = value;
                OnFileSelected(System.EventArgs.Empty);
                FileSelected?.Invoke(this, System.EventArgs.Empty);
            }
        }
        private string _value;

        public bool IsFileValid => !VerifyFileExists || Value.Length == 0 || File.Exists(Value);

        //todo: directory support

        /// <summary>
        /// On input, show a little marker whether or not the file exists
        /// </summary>
        public bool VerifyFileExists { get; set; } = true;

        /// <summary>
        /// Called whenever a valid file name is entered
        /// </summary>
        public event System.EventHandler FileSelected = null;
        protected virtual void OnFileSelected(System.EventArgs e) { }
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

        public override Graphics.BitmapFont Font
        {
            get => textInput.Font;
            set
            {
                textInput.Font = pickerButton.Font = value;
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
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch,
            BorderColor = Color.Transparent
        };
        protected Static pickerButton = new Static
        {
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

            BorderColor = Color;
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
            bool isValid = IsFileValid;
            if (isValid)
            {
                BorderColor = lastBorderColor;
                OnFileSelected(System.EventArgs.Empty);
            }
            else
            {
                lastBorderColor = BorderColor;
                BorderColor = InvalidFileOutlineColor;
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var sz = textInput.Measure(availableSize);
            return sz + new Vector2(sz.Y, 0);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            var sz = availableSize.ToPoint();
            //todo: pickerButton bounds?
            textInput.Arrange(new Rectangle(0, 0, sz.X - (int)pickerButton.MeasuredSize.X, sz.Y));
            pickerButton.Arrange(new Rectangle(sz.X - (int)pickerButton.MeasuredSize.X, 0, (int)pickerButton.MeasuredSize.X, sz.Y));
        }
    }
}
