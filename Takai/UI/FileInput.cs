using System.IO;
#if WINDOWS
using System.Windows.Forms;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.UI
{
    public enum DialogMode
    {
        Open,
        Save
    }

    public class FileInputBase : List
    {
        //todo: directory support

        /// <summary>
        /// The mode of this file input, by default, open
        /// </summary>
        public DialogMode Mode { get; set; } = DialogMode.Open;

        /// <summary>
        /// On input, show a little marker whether or not the file exists
        /// </summary>
        public bool VerifyFileExists { get; set; } = true;

        public virtual string InitialDirectory { get; set; } = "";
        public virtual string Filter { get; set; } = null;

        /// <summary>
        /// Called whenever a valid file name is entered
        /// </summary>
        public System.EventHandler FileSelected;
        protected virtual void OnFileSelected(System.EventArgs e) { }
    }

    public class FileInput : FileInputBase
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

        public FileInput()
        {
            Direction = Direction.Horizontal;

            textInput.TextChanged += delegate { ValidateFile(); };

            pickerButton.Click += delegate
            {
#if WINDOWS
#if DEBUG
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
#elif WINDOWS_UAP
                //todo
#endif
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
    }

    public class FileSelect : FileInputBase
    {
        public override string InitialDirectory
        {
            get => base.InitialDirectory;
            set
            {
                base.InitialDirectory = value;
                RefreshList();
            }
        }

        public override string Filter
        {
            get => base.Filter;
            set
            {
                base.Filter = value;
                RefreshList();
            }
        }

        /// <summary>
        /// Load files in subdirectories of the InitialDirectory (recursive)
        /// </summary>
        public bool LoadSubdirectories { get; set; } = true;

        protected void RefreshList()
        {
            RemoveAllChildren();

            foreach (var file in Directory.EnumerateFiles(InitialDirectory, Filter, LoadSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {

            }
        }
    }
}
