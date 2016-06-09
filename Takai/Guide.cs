using Microsoft.Xna.Framework;

namespace Takai
{
    /// <summary>
    /// A cross platform simplification of the XNA and Win32 dialogs
    /// </summary>
    public static class Guide
    {
        /// <summary>
        /// The icon to use in a message box
        /// </summary>
        public enum MessageBoxIcon
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Alert = 3,
        }

        /// <summary>
        /// The object that called one of the functions sender
        /// </summary>
        private static System.Collections.Generic.List<object> senders = new System.Collections.Generic.List<object>();

        /// <summary>
        /// Show a message box
        /// </summary>
        /// <param name="Title">The title of the message box</param>
        /// <param name="Description">The description for the prompt</param>
        /// <param name="Buttons">Buttons to show</param>
        /// <param name="DefaultButton">The 0 based button to be default</param>
        /// <param name="Icon">The icon to associate with this dialog</param>
        /// <param name="Sender">The sender that this belongs to</param>
        public static void ShowMessageBox(string Title, string Description, string[] Buttons, int DefaultButton, MessageBoxIcon Icon, object Sender)
        {
            if (Sender != null && !senders.Contains(Sender))
                senders.Add(Sender);

#if WINDOWS
            System.Threading.ThreadStart strt = delegate
            {
                if (_isMboxVisible)
                    return;

                _isMboxVisible = true;
                _hasReported = false;
                _mboxButtonCount = Buttons.Length;
                //convert to standard windows dialog buttons/icons

                System.Windows.Forms.MessageBoxButtons mbb = System.Windows.Forms.MessageBoxButtons.OK;
                if (Buttons.Length == 2 && Buttons[0].Equals("ok", System.StringComparison.CurrentCultureIgnoreCase))
                    mbb = System.Windows.Forms.MessageBoxButtons.OKCancel;
                else if (Buttons[0].Equals("retry", System.StringComparison.CurrentCultureIgnoreCase))
                    mbb = System.Windows.Forms.MessageBoxButtons.RetryCancel;
                else if (Buttons.Length == 2 && Buttons[0].Equals("yes", System.StringComparison.CurrentCultureIgnoreCase))
                    mbb = System.Windows.Forms.MessageBoxButtons.YesNo;
                else if (Buttons.Length == 3 && Buttons[0].Equals("yes", System.StringComparison.CurrentCultureIgnoreCase))
                    mbb = System.Windows.Forms.MessageBoxButtons.YesNoCancel;
                else if (Buttons[0].Equals("abort", System.StringComparison.CurrentCultureIgnoreCase))
                    mbb = System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore;

                System.Windows.Forms.MessageBoxIcon mbi = System.Windows.Forms.MessageBoxIcon.None;
                if (Icon == MessageBoxIcon.Error)
                    mbi = System.Windows.Forms.MessageBoxIcon.Error;
                else if (Icon == MessageBoxIcon.Warning)
                    mbi = System.Windows.Forms.MessageBoxIcon.Warning;
                else if (Icon == MessageBoxIcon.Alert)
                    mbi = System.Windows.Forms.MessageBoxIcon.Asterisk;

                _mboxResult = System.Windows.Forms.MessageBox.Show(Description, Title, mbb, mbi, (System.Windows.Forms.MessageBoxDefaultButton)((DefaultButton > 2 ? 2 : DefaultButton) << 8));
                _isMboxVisible = false;

            };
            new System.Threading.Thread(strt).Start();
#else
            if (!gs.Guide.IsVisible)
                _status = gs.Guide.BeginShowMessageBox(Title.Length > 255 ? Title.Substring(0, 255) : Title, Description.Length > 255 ? Description.Substring(0, 255) : Description,
                    Buttons, DefaultButton, (gs.MessageBoxIcon)Icon, MboxCallBack, null);
#endif
        }

        /// <summary>
        /// Show a message box
        /// </summary>
        /// <param name="Title">The title of the message box</param>
        /// <param name="Description">The description for the prompt</param>
        /// <param name="Buttons">Buttons to show</param>
        /// <param name="DefaultButton">The 0 based button to be default</param>
        /// <param name="Icon">The icon to associate with this dialog</param>
        /// <param name="Player">The player calling this function)</param>
        /// <param name="Sender">The sender to specify who this belongs to</param>
        public static void ShowMessageBox(PlayerIndex Player, string Title, string Description, string[] Buttons, int DefaultButton, MessageBoxIcon Icon, object Sender)
        {
            if (Sender != null && !senders.Contains(Sender))
                senders.Add(Sender);

#if WINDOWS
            ShowMessageBox(Title, Description, Buttons, DefaultButton, Icon, Sender);
#else
            if (!gs.Guide.IsVisible)
                _status = gs.Guide.BeginShowMessageBox(Title.Length > 255 ? Title.Substring(0, 255) : Title, Description.Length > 255 ? Description.Substring(0, 255) : Description,
                    Buttons, DefaultButton, (gs.MessageBoxIcon)Icon, MboxCallBack, null);
#endif
        }

        /// <summary>
        /// Use a message box to show an exception
        /// </summary>
        /// <param name="Exception">The exception to show</param>
        /// <param name="Message">The human readable mesage of the error (not including technical info)</param>
        /// <param name="Title">The human readable title for the message dialog</param>
        public static void ShowError(System.Exception Exception, string Title, string Message)
        {
#if WINDOWS
            var frame = new System.Diagnostics.StackTrace(Exception, true).GetFrame(0);
            ShowMessageBox(Title, string.Format("{0}\n\n[ Line {1} - {2} ]", Message, frame.GetFileLineNumber(), frame.GetFileName()), MessageBoxButtons.Ok, 0, MessageBoxIcon.Error, null);
#else
            ShowMessageBox(Title, string.Format("{0}\n\n{1}\n\n{2}", Message, Exception.Message, Exception.StackTrace), MessageBoxButtons.Ok, 0, MessageBoxIcon.Error, null);
#endif
        }

        /// <summary>
        /// Show a keyboard input dialog
        /// </summary>
        /// <param name="Title">The title of the dialog</param>
        /// <param name="Description">The description for the prompt</param>
        /// <param name="DefaultText">The default text in the input field</param>
        /// <param name="Callback">An optional callback to be called when this is closed</param>
        /// <param name="Sender">The sender to specify who this belongs to</param>
        public static void ShowKeyboard(string Title, string Description, string DefaultText, object Sender)
        {
            if (Sender != null && !senders.Contains(Sender))
                senders.Add(Sender);

            ShowKeyboard(PlayerIndex.One, Title, Description, DefaultText, Sender);
        }

        /// <summary>
        /// Show a keyboard input dialog
        /// </summary>
        /// <param name="Player">The player to show for</param>
        /// <param name="Title">The title of the dialog</param>
        /// <param name="Description">The description for the prompt</param>
        /// <param name="DefaultText">The default text in the input field</param>
        /// <param name="Callback">An optional callback to be called when this is closed</param>
        /// <param name="Sender">The sender to specify who this belongs to</param>
        public static void ShowKeyboard(PlayerIndex Player, string Title, string Description, string DefaultText, object Sender)
        {
            if (Sender != null && !senders.Contains(Sender))
                senders.Add(Sender);

#if WINDOWS
            System.Threading.ThreadStart strt = delegate
            {
                if (_isMboxVisible)
                    return;

                _isMboxVisible = true;
                _hasReported = false;
                _keyInputDialog = new GUI.TextBoxForm(Title, Description, DefaultText, "");
                if (_keyInputDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    _keyInputDialog = null;
                _isMboxVisible = false;
            };
            new System.Threading.Thread(strt).Start();
#else
            if (!gs.Guide.IsVisible)
                _status = gs.Guide.BeginShowKeyboardInput(Player, Title, Description, DefaultText, KeyCallback, null);
#endif
        }

        /// <summary>
        /// Get the result from the message box
        /// </summary>
        /// <param name="Result">The button pressed, null if dialog still visible</param>
        public static int? GetMessageBoxResult(object Sender)
        {
            if (Sender == null || !senders.Contains(Sender))
                return null;

#if WINDOWS
            if (_isMboxVisible || _hasReported)
                return null;

            senders.Remove(Sender);
            int rslt = 0;
            if (_mboxResult == System.Windows.Forms.DialogResult.Cancel || _mboxResult == System.Windows.Forms.DialogResult.Ignore)
                rslt = _mboxButtonCount - 1;
            else if (_mboxResult == System.Windows.Forms.DialogResult.Retry || _mboxResult == System.Windows.Forms.DialogResult.No)
                rslt = 1;

            _hasReported = true;
            return rslt;
#else
            if (_status != null && _status.IsCompleted)
            {
                _status = null;
                senders.Remove(Sender);
                return _mboxResult;
            }
            return null;
#endif
        }

        /// <summary>
        /// Get the result from the keyboard dialog (if dialog cancel closed, null is returned)
        /// </summary>
        /// <param name="Result">The string entered, null if the dialog is still open</param>
        public static string GetKeyboardResult(object Sender)
        {
            if (Sender == null || !senders.Contains(Sender))
                return null;

#if WINDOWS
            if (_isMboxVisible || _hasReported)
                return null;

            senders.Remove(Sender);
            _hasReported = true;
            var text = _keyInputDialog != null ? _keyInputDialog.entry.Text : null;
            _keyInputDialog = null;
            return text;
#else
            if (_status != null && _status.IsCompleted)
            {
                _status = null;
                senders.Remove(Sender);
                return _keyResult;
            }
            return null;
#endif

        }

        /// <summary>
        /// Show the marketplace (does nothing on windows)
        /// </summary>
        public static void ShowMarket()
        {
#if !WINDOWS
            gs.Guide.ShowMarketplace(PlayerIndex.One);
#endif
        }

        /// <summary>
        /// Show the marketplace (does nothing on windows)
        /// </summary>
        /// <param name="Player">The player to show for</param>
        public static void ShowMarket(PlayerIndex Player)
        {
#if !WINDOWS
            gs.Guide.ShowMarketplace(Player);
#endif
        }

        /// <summary>
        /// Is the game in trial mode, in debug mode and on PC, setting this will affect simulated trial mode
        /// </summary>
        public static bool isTrial
        {
#if WINDOWS
            get { return _isTrial; }
            set { _isTrial = value; }
#else
            get { return gs.Guide.IsTrialMode; }
            set
            {
#if DEBUG
                gs.Guide.SimulateTrialMode = value;
#endif
            }
#endif
        }

        /// <summary>
        /// Is the guide or any dialog visible?
        /// </summary>
        public static bool isVisible
        {
            get
            {
#if WINDOWS
                return _isMboxVisible;
#else
                return gs.Guide.IsVisible;
#endif
            }
        }

#if WINDOWS
        private static bool _isTrial = false;
        private static bool _isMboxVisible = false;
        private static int _mboxButtonCount = 0;
        private static bool _hasReported = true;

        private static System.Windows.Forms.DialogResult _mboxResult = System.Windows.Forms.DialogResult.Cancel;
        private static GUI.TextBoxForm _keyInputDialog = null;
#else
        private static int? _mboxResult = null;
        private static string _keyResult = string.Empty;
        private static System.IAsyncResult _status = null;

        private static void KeyCallback(System.IAsyncResult Result)
        {
            _keyResult = gs.Guide.EndShowKeyboardInput(Result);
        }

        private static void MboxCallBack(System.IAsyncResult Result)
        {
            _mboxResult = gs.Guide.EndShowMessageBox(Result);
        }
#endif

        /// <summary>
        /// Predefined message buttons
        /// </summary>
        public static class MessageBoxButtons
        {
            /// <summary>
            /// A single Ok button
            /// </summary>
            public static readonly string[] Ok = new[] { "Ok" };
            /// <summary>
            /// Ok and Cancel buttons
            /// </summary>
            public static readonly string[] OkCancel = new[] { "Ok", "Cancel" };
            /// <summary>
            /// Retry and Cancel buttons
            /// </summary>
            public static readonly string[] RetryCancel = new[] { "Retry", "Cancel" };
            /// <summary>
            /// Yes and No buttons
            /// </summary>
            public static readonly string[] YesNo = new[] { "Yes", "No" };
            /// <summary>
            /// Yes, No, and Cancel buttons
            /// </summary>
            public static readonly string[] YesNoCancel = new[] { "Yes", "No", "Cancel" };
            /// <summary>
            /// Abort, Retry, and Ignore Buttons
            /// </summary>
            public static readonly string[] AbortRetryIgnore = new[] { "Abort", "Retry", "Ignore" };
        }
    }
}
