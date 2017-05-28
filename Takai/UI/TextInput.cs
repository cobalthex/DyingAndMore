using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class TextInput : Static
    {
        /// <summary>
        /// The char to draw characters in the password field with
        /// </summary>
        public static char PasswordChar = '*';

        /// <summary>
        /// How long to wait before repeating key presses
        /// </summary>
        public static System.TimeSpan InitialKeyRepeatDelay = System.TimeSpan.FromMilliseconds(500);
        /// <summary>
        /// How long to wait between each key repetition
        /// </summary>
        public static int KeyRepeatDelayInMSec = 50;

        /// <summary>
        /// The maximum number of characters allowed in this textbox
        /// </summary>
        public uint MaxLength { get; set; } = 0x10000;

        /// <summary>
        /// The textbox scrolled position
        /// </summary>
        [Data.Serializer.Ignored]
        public int ScrollPosition { get; set; } = 0;

        /// <summary>
        /// The position of the caret. Text is inserted at the caret
        /// </summary>
        [Data.Serializer.Ignored]
        public int Caret
        {
            get => caret;
            set
            {
                caret = MathHelper.Clamp(value, 0, Text.Length);
                UpdateScrollPosition();
                lastInputTick = System.Environment.TickCount;
            }
        }
        private int caret = 0;

        public override bool CanFocus => true;

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                UpdateVisibleText();
                Caret = value.Length;
            }
        }
        /// <summary>
        /// The actual text of this element, base.Text is affected by pass
        /// </summary>
        protected string visibleText = "";

        /// <summary>
        /// Hide visual input
        /// </summary>
        public bool IsPassword { get; set; } = false;

        /// <summary>
        /// Allow spaces in the textbox
        /// </summary>
        public bool AllowSpaces { get; set; } = true;

        /// <summary>
        /// Allow special characters ($~!@#, etc)
        /// </summary>
        public bool AllowSpecialCharacters { get; set; } = true;

        /// <summary>
        /// Allow numbers (0-9)
        /// </summary>
        public bool AllowNumbers { get; set; } = true;

        /// <summary>
        /// Allow A-Z
        /// </summary>
        public bool AllowLetters { get; set; } = true;

        [Data.Serializer.Ignored]
        public event System.EventHandler TextChanged = null;
        protected virtual void OnTextChanged(System.EventArgs e) { }

        /// <summary>
        /// When the last character was inputted (in system ticks)
        /// </summary>
        protected int lastInputTick;

        /// <summary>
        /// The last key that was repeated, Keys.None if none.
        /// Used for repetitive input
        /// </summary>
        protected Keys lastRepeatKey;
        /// <summary>
        /// When the last key-repeat was hit
        /// </summary>
        protected System.TimeSpan keyRepeatTime;

        public TextInput()
        {
            ignoreSpaceKey = true;
            OutlineColor = Color;
        }

        protected override void OnPress(ClickEventArgs args)
        {
            var x = args.position.X + ScrollPosition;
            var width = 0;
            for (int i = 0; i < Text.Length; ++i)
            {
                var cw = (int)Font.MeasureString(Text, i, 1).X;
                if (width + (cw / 2) > x)
                {
                    Caret = i;
                    return;
                }
                width += cw;
            }
            Caret = Text.Length;
        }

        public override void AutoSize(float padding = 0)
        {
            Size = new Vector2(
                200,
                (Font?.MaxCharHeight ?? 10) + padding
            );
        }

        protected int FindNextWordStart()
        {
            int pos = Caret;
            for (; pos < Text.Length; ++pos)
            {
                if (!char.IsWhiteSpace(Text[pos]))
                    break;
            }
            for (; pos < Text.Length; ++pos)
            {
                if (!(char.IsSymbol(Text[pos]) || char.IsPunctuation(Text[pos])))
                    break;
            }
            for (; pos < Text.Length; ++pos)
            {
                if (!char.IsLetterOrDigit(Text[pos]))
                    break;
            }
            return pos;
        }
        protected int FindPreviousWordStart()
        {
            int pos = Caret;
            for (; pos > 0; --pos)
            {
                if (!char.IsWhiteSpace(Text[pos - 1]))
                    break;
            }
            for (; pos > 0; --pos)
            {
                if (!(char.IsSymbol(Text[pos - 1]) || char.IsPunctuation(Text[pos - 1])))
                    break;
            }
            for (; pos > 0; --pos)
            {
                if (!char.IsLetterOrDigit(Text[pos - 1]))
                    break;
            }
            return pos;
        }

        protected override bool HandleInput(GameTime time)
        {
            if (HasFocus && !(InputState.IsMod(KeyMod.Alt) ||
                              InputState.IsMod(KeyMod.Windows)))
            {
                bool isCtrl  = InputState.IsMod(KeyMod.Control);
                bool isShift = InputState.IsMod(KeyMod.Shift);

                var keys = InputState.GetPressedKeys();

                //key repetition
                if (keys.Length > 0)
                {
                    var newKey = keys[keys.Length - 1];
                    if (newKey != lastRepeatKey)
                    {
                        lastRepeatKey = newKey;
                        keyRepeatTime = time.TotalGameTime;
                    }
                }
                else
                    lastRepeatKey = Keys.None;

                //todo: multiple key presses messing up delay timing of repeat

                foreach (var key in keys)
                {
                    if (!InputState.IsPress(key) && //ignore held keys
                        ((lastRepeatKey != Keys.None && key != lastRepeatKey) || //and ignore other keys if there is a repeat
                            (time.TotalGameTime < keyRepeatTime + InitialKeyRepeatDelay || //or the repeat hasn't started yet
                             System.Environment.TickCount < lastInputTick + KeyRepeatDelayInMSec))) //or the repeat is delayed
                        continue;

                    if (key == Keys.Left && Caret > 0)
                    {
                        if (isCtrl)
                            Caret = FindPreviousWordStart();
                        else
                            --Caret;
                    }
                    else if (key == Keys.Right && Caret < Text.Length)
                    {
                        if (isCtrl)
                            Caret = FindNextWordStart();
                        else
                            ++Caret;
                    }

                    else if (key == Keys.Home)
                        Caret = 0;

                    else if (key == Keys.End)
                        Caret = Text.Length;

                    else if (key == Keys.Back && Caret > 0)
                    {
                        if (isCtrl)
                        {
                            var pos = FindPreviousWordStart();
                            base.Text = Text.Remove(pos, caret - pos);
                            Caret = pos;
                        }
                        else
                        {
                            base.Text = Text.Remove(Caret - 1, 1);
                            --Caret;
                        }
                        UpdateVisibleText();
                    }

                    else if (key == Keys.Delete && Caret < Text.Length)
                    {
                        if (isCtrl)
                        {
                            var pos = FindNextWordStart();
                            base.Text = Text.Remove(pos, caret - pos);
                        }
                        else
                            base.Text = Text.Remove(Caret, 1);
                        UpdateVisibleText();
                    }

                    else if (AllowSpaces && key == Keys.Space)
                        InsertAtCaret(' '); //autocomplete when ctrl?

                    else if (!isCtrl && AllowLetters && key >= Keys.A && key <= Keys.Z)
                    {
                        if (isShift)
                            InsertAtCaret((char)((key - Keys.A) + 'A'));
                        else
                            InsertAtCaret((char)((key - Keys.A) + 'a'));
                    }

                    else if (!isCtrl && AllowNumbers && key >= Keys.D0 && key <= Keys.D9)
                        InsertAtCaret((char)((key - Keys.D0) + '0'));

                    else if (!isCtrl && AllowNumbers && key >= Keys.NumPad0 && key <= Keys.NumPad9)
                        InsertAtCaret((char)((key - Keys.NumPad0) + '0'));

                    else if (!isCtrl && (AllowNumbers || AllowSpecialCharacters) &&
                             key == Keys.OemPlus)
                        InsertAtCaret('+');

                    else if (!isCtrl && (AllowNumbers || AllowSpecialCharacters) &&
                             key == Keys.OemMinus)
                        InsertAtCaret('-');

                    else if (AllowSpecialCharacters)
                    {
                        if (key == Keys.OemQuestion)
                            InsertAtCaret(isShift ? '?' : '/');
                        else if (key == Keys.OemComma)
                            InsertAtCaret(isShift ? '<' : ',');
                        else if (key == Keys.OemPeriod)
                            InsertAtCaret(isShift ? '>' : '.');
                    }
                }
            }

            return base.HandleInput(time); //todo: check for changes and return false if any
        }


        protected void UpdateVisibleText()
        {
            visibleText = IsPassword ? new string(PasswordChar, base.Text.Length) : base.Text;
            OnTextChanged(System.EventArgs.Empty);
            TextChanged?.Invoke(this, System.EventArgs.Empty);
        }

        void UpdateScrollPosition()
        {
            var textWidth = Font?.MeasureString(Text, 0, Caret).X ?? 0;

            if (textWidth < ScrollPosition)
                ScrollPosition -= (int)Size.X;

            ScrollPosition = (int)MathHelper.Clamp(textWidth, textSize.X - Size.X, 0);
        }

        void InsertAtCaret(char ch)
        {
            if (Text.Length >= MaxLength)
                return;

            base.Text = Text.Insert(Caret, ch.ToString());
            UpdateVisibleText();
            ++Caret;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Font != null)
            {
                var size = new Point(
                    MathHelper.Min(AbsoluteBounds.Width - 4, (int)textSize.X),
                    MathHelper.Min(AbsoluteBounds.Height, (int)textSize.Y)
                );

                Font?.Draw(
                    spriteBatch,
                    visibleText,
                    0, -1,
                    new Rectangle(
                        AbsoluteBounds.X + 2,
                        AbsoluteBounds.Y + ((AbsoluteBounds.Height - size.Y) / 2),
                        size.X,
                        size.Y
                    ),
                    new Point(-ScrollPosition, 0),
                    Color
                );

                var tickCount = System.Environment.TickCount;
                if (HasFocus && (System.Math.Abs(lastInputTick - tickCount) < 500 || tickCount % 650 < 325))
                {
                    var x = Font.MeasureString(visibleText, 0, Caret).X - ScrollPosition + AbsoluteBounds.X + 3;
                    Graphics.Primitives2D.DrawLine(
                        spriteBatch,
                        Color,
                        new Vector2(x, AbsoluteBounds.Top + 4),
                        new Vector2(x, AbsoluteBounds.Bottom - 4)
                    );
                }
            }
        }
    }
}
