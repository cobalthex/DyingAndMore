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
        public int Caret { get; set; } = 0;

        public override bool CanFocus => true;

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                UpdateVisibleText();
                Caret = value.Length;
                lastInputTick = System.Environment.TickCount;
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
        /// When the last character was inputted (in system ticks)
        /// </summary>
        protected int lastInputTick;

        public TextInput()
        {
            ignoreSpaceKey = true;
        }

        protected override void BeforePress(ClickEventArgs args)
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

        public override bool Update(GameTime time)
        {
            if (HasFocus && !(InputState.IsMod(KeyMod.Alt) ||
                              InputState.IsMod(KeyMod.Control) ||
                              InputState.IsMod(KeyMod.Windows)))
            {

                var keys = InputState.GetPressedKeys();
                foreach (var key in keys)
                {
                    if (!InputState.IsPress(key))
                        continue;

                    if (key == Keys.Back && Caret > 0)
                    {
                        base.Text = Text.Remove(Caret - 1, 1);
                        UpdateVisibleText();
                        --Caret;
                        UpdateScrollPosition();
                    }

                    else if (key == Keys.Delete && Caret < Text.Length)
                    {
                        base.Text = Text.Remove(Caret, 1);
                        UpdateVisibleText();
                    }

                    else if (AllowSpaces && key == Keys.Space)
                        InsertAtCaret(' ');

                    else if (key >= Keys.A && key <= Keys.Z)
                    {
                        if (InputState.IsMod(KeyMod.Shift))
                            InsertAtCaret((char)((key - Keys.A) + 'A'));
                        else
                            InsertAtCaret((char)((key - Keys.A) + 'a'));
                    }

                    else if (AllowNumbers && key >= Keys.D0 && key <= Keys.D9)
                        InsertAtCaret((char)((key - Keys.D0) + '0'));

                    else if (AllowNumbers && key >= Keys.NumPad0 && key <= Keys.NumPad9)
                        InsertAtCaret((char)((key - Keys.NumPad0) + '0'));
                }
            }

            return base.Update(time);
        }


        protected void UpdateVisibleText()
        {
            visibleText = IsPassword ? new string(PasswordChar, base.Text.Length) : base.Text;
        }

        void UpdateScrollPosition()
        {
            var textWidth = Font.MeasureString(Text, 0, Caret).X;

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

            UpdateScrollPosition();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Font == null)
                return;

            Graphics.Primitives2D.DrawRect(spriteBatch, HasFocus ? FocusOutlineColor : Color, AbsoluteBounds);

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
            if (HasFocus && (System.Math.Abs(lastInputTick - tickCount) < 500 || tickCount % 1000 < 500))
            {
                var x = Font.MeasureString(visibleText, 0, Caret).X - ScrollPosition + AbsoluteBounds.X + 3;
                Graphics.Primitives2D.DrawLine(
                    spriteBatch,
                    Color,
                    new Vector2(x, AbsoluteBounds.Top + 4),
                    new Vector2(x, AbsoluteBounds.Bottom - 4)
                );
            }

            foreach (var child in Children)
                child.Draw(spriteBatch);
        }
    }
}
