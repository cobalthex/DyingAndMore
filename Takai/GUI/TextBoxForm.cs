#if WINDOWS
using System.Windows.Forms;

namespace Takai.GUI
{
    /// <summary>
    /// A windows specific text entry dialog
    /// </summary>
    public class TextBoxForm : Form
    {
        public System.Windows.Forms.TextBox entry;
        System.Windows.Forms.Button okButton;
        System.Windows.Forms.Button cancelButton;
        System.Windows.Forms.Label questionLabel;

        public DialogResult result = DialogResult.None;

        string dflTxt = "";

        /// <summary>
        /// Create a new text form entry dialog
        /// </summary>
        /// <param name="title">Title of the window</param>
        /// <param name="question">What to ask the user to input</param>
        /// <param name="defaultText">Optional default text to enter</param>
        /// <param name="hintText">Optional hint text</param>
        public TextBoxForm(string title, string question, string defaultText = "", string hintText = "")
        {
            dflTxt = hintText;
            Initialize();

            this.Text = title;

            if (defaultText != "")
            {
                entry.Text = defaultText;
                entry.ForeColor = System.Drawing.SystemColors.ControlText;
            }

            questionLabel.Text = question;
            questionLabel.Height = questionLabel.PreferredHeight;
            this.Size = new System.Drawing.Size(this.Size.Width, 100 + questionLabel.Height);
            this.entry.Focus();
        }

        void Initialize()
        {
            //creating form

            this.CenterToParent();
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MinimizeBox = false;
            this.AllowDrop = false;
            this.MaximizeBox = false;
            this.FormClosing += new FormClosingEventHandler(TextBoxForm_FormClosing);
            this.TopMost = true;

            //creating controls

            okButton = new System.Windows.Forms.Button();
            okButton.Text = "&OK";
            okButton.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Size = new System.Drawing.Size(80, 24);

            cancelButton = new System.Windows.Forms.Button();
            cancelButton.Text = "&Cancel";
            cancelButton.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Size = okButton.Size;

            entry = new System.Windows.Forms.TextBox();
            entry.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            entry.Text = dflTxt;
            entry.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;

            questionLabel = new System.Windows.Forms.Label();

            entry.GotFocus += new System.EventHandler(entry_GotFocus);
            entry.LostFocus += new System.EventHandler(entry_LostFocus);
            entry.KeyPress += new KeyPressEventHandler(entry_KeyPress);

            okButton.Click += new System.EventHandler(okButton_Click);
            cancelButton.Click += new System.EventHandler(cancelButton_Click);

            //setting position
            this.Size = new System.Drawing.Size(380, 140);

            int w = okButton.Width + cancelButton.Width + 10;

            okButton.Location = new System.Drawing.Point(C2C(w, ClientSize.Width), ClientSize.Height - 8 - okButton.Height);
            cancelButton.Location = new System.Drawing.Point((w - cancelButton.Width) + C2C(w, ClientSize.Width), okButton.Location.Y);
            questionLabel.SetBounds(10, 10, ClientSize.Width - 20, 20);
            entry.SetBounds(10, C2C(16, ClientSize.Height), ClientSize.Width - 20, 10);


            //adding to form

            Controls.Add(entry);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            Controls.Add(questionLabel);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            this.ResumeLayout(false);
        }

        private int GetTextHeight(System.Windows.Forms.Label tBox)
        {
            return TextRenderer.MeasureText(tBox.Text, tBox.Font, tBox.ClientSize,
                     TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
        }

        void cancelButton_Click(object sender, System.EventArgs e)
        {
            result = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        void okButton_Click(object sender, System.EventArgs e)
        {
            result = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        void entry_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                result = System.Windows.Forms.DialogResult.OK;
                e.Handled = true;
                this.Close();
            }
            //ctrl+a
            if ((e.KeyChar & 'a') > 0 && Control.ModifierKeys == Keys.Control)
                entry.SelectAll();
        }

        void TextBoxForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (entry.Text == dflTxt)
            {
                if (result == System.Windows.Forms.DialogResult.None)
                    result = System.Windows.Forms.DialogResult.Cancel;
                entry.Text = "";
            }
        }

        /// <summary>
        /// Center to client
        /// </summary>
        /// <param name="dim">dimension of object to center</param>
        /// <param name="rgn">dimension of container to center object to</param>
        /// <returns>centered dimension</returns>
        int C2C(int dim, int rgn)
        {
            return (rgn - dim) >> 1;
        }

        void entry_LostFocus(object sender, System.EventArgs e)
        {
            if (entry.Text == "")
            {
                entry.Text = dflTxt;
                entry.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            }
        }

        void entry_GotFocus(object sender, System.EventArgs e)
        {
            if (entry.Text == dflTxt)
            {
                entry.Text = "";
                entry.ForeColor = System.Drawing.SystemColors.ControlText;
            }
        }
    }
}
#else
namespace Takai.GUI
{
    /// <summary>
    /// A windows specific text entry dialog (does nothing on non windows platforms)
    /// </summary>
    public class TextBoxForm { }
}
#endif