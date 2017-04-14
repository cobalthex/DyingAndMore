using System.Windows.Forms;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public enum DialogMode
    {
        Open,
        Save
    }

    public class FileInput : Static
    {
        public static Color InvalidFileOutlineColor = Color.Tomato;

        public override string Text
        {
            get => textInput.Text;
            set
            {
                textInput.Text = value;
                ValidateFile();
            }
        }

        /// <summary>
        /// The mode of this file input, by default, open
        /// </summary>
        public DialogMode Mode { get; set; } = DialogMode.Open;

        /// <summary>
        /// On input, show a little marker whether or not the file exists
        /// </summary>
        public bool VerifyFileExists { get; set; } = true;

        public string InitialDirectory { get; set; } = "";
        public string Filter { get; set; } = null;

        public override Graphics.BitmapFont Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                textInput.Font = pickerButton.Font = value;
            }
        }

        public override Color Color
        {
            get => base.Color;
            set
            {
                base.Color = value;
                textInput.Color = pickerButton.Color = value;
            }
        }

        protected TextInput textInput;
        protected Static pickerButton;

        private Color lastOutlineColor;
        private bool fileWasInvalid = false;

        public FileInput()
        {
            textInput = new TextInput()
            {
                OutlineColor = Color.Transparent
            };
            textInput.OnInput += delegate { ValidateFile(); };

            pickerButton = new Static()
            {
                Text = "..."
            };
            pickerButton.OnClick += delegate
            {
                FileDialog dialog = null;
                if (Mode == DialogMode.Open)
                    dialog = new OpenFileDialog();
                else if (Mode == DialogMode.Save)
                    dialog = new SaveFileDialog();

                dialog.FileName = Text;
                dialog.AutoUpgradeEnabled = true;
                dialog.RestoreDirectory = true;
                dialog.InitialDirectory = InitialDirectory;
                dialog.Filter = Filter;
                dialog.ValidateNames = true;
                dialog.CheckFileExists = VerifyFileExists;

                using (dialog)
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        Text = dialog.FileName;
                    }
                }
            };

            AddChildren(textInput, pickerButton);

            OnResize += delegate
            {
                var height = Size.Y;
                textInput.Size = new Vector2(Size.X - height, height);
                pickerButton.Size = new Vector2(height);
            };

            OutlineColor = Color;
        }

        public override void AutoSize(float padding = 0)
        {
            textInput.AutoSize(padding);
            var btnSize = textInput.Size.Y;
            pickerButton.Size = new Vector2(btnSize + 10, btnSize);

            Size = textInput.Size + new Vector2(btnSize + 10, 0);
        }

        public override void Reflow()
        {
            pickerButton.Position = textInput.Position + new Vector2(textInput.Size.X, 0);
            base.Reflow();
        }

        protected void ValidateFile()
        {
            if (VerifyFileExists && Text.Length > 0 && !System.IO.File.Exists(Text))
            {
                lastOutlineColor = OutlineColor;
                OutlineColor = InvalidFileOutlineColor;
                fileWasInvalid = true;
            }
            else if (fileWasInvalid)
                OutlineColor = lastOutlineColor;
        }
    }
}
