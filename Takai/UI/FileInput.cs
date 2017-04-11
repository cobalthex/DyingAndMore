using System;
using System.IO;
using System.Windows.Forms;

namespace Takai.UI
{
    public class FileInput : Static
    {
        protected TextInput textInput;
        protected Static pickerButton;

        public FileInput()
        {
            textInput = new TextInput();
            pickerButton = new Static()
            {
                Text = "..."
            };
            pickerButton.OnClick += delegate
            {
                var dlg = new OpenFileDialog();
            };

            AddChildren(textInput, pickerButton);
        }
    }
}
