using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public enum DialogMode
    {
        Open,
        Save
    }

    public class FileInput : List
    {
        //todo: directory support

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
            BorderColor = Color.Transparent
        };
        protected Static pickerButton = new Static
        {
            Text = "..."
        };

        private Color lastOutlineColor;
        private bool fileWasInvalid = false;

        /// <summary>
        /// Called whenever a valid file name is entered
        /// </summary>
        public System.EventHandler FileSelected;
        protected virtual void OnFileSelected(System.EventArgs e) { }

        public FileInput()
        {
            Direction = Direction.Horizontal;

            textInput.TextChanged += delegate { ValidateFile(); };

            pickerButton.Click += delegate
            {
#if WINDOWS && DEBUG
                if (Takai.Input.InputState.IsMod(Input.KeyMod.Alt))
                {
                    System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(Text));
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

            BorderColor = Color;
        }

        public override void AutoSize(float padding = 0)
        {
            textInput.AutoSize(padding);
            var btnSize = textInput.Size.Y;
            pickerButton.Size = new Vector2(btnSize);

            Size = textInput.Size + new Vector2(btnSize, 0);
            base.AutoSize(padding);
        }

        protected override void OnResize(System.EventArgs e)
        {
            var btnSize = Size.Y;
            textInput.Size = new Vector2(Size.X - btnSize, Size.Y);
            pickerButton.Size = new Vector2(btnSize);
            pickerButton.Position = new Vector2(Size.X - btnSize, 0);
            base.OnResize(e);
        }

        protected void ValidateFile()
        {
            bool isFileValid = !VerifyFileExists;
            if (VerifyFileExists && Text.Length > 0 && !(isFileValid = System.IO.File.Exists(Text)))
            {
                lastOutlineColor = BorderColor;
                BorderColor = InvalidFileOutlineColor;
                fileWasInvalid = true;
            }
            else if (fileWasInvalid)
                BorderColor = lastOutlineColor;

            if (isFileValid)
            {
                OnFileSelected(System.EventArgs.Empty);
                FileSelected?.Invoke(this, System.EventArgs.Empty);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) { }
    }
}
