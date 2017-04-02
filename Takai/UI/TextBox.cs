using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class TextBox : Element
    {
        /// <summary>
        /// Allow spaces in the textbox
        /// </summary>
        bool AllowSpaces { get; set; } = true;

        /// <summary>
        /// Allow special characters ($~!@#, etc), by default only A-Z a-z 0-9 _ are allowed, in the textbox
        /// </summary>
        bool AllowSpecialCharacters { get; set; } = true;

        /// <summary>
        /// The maximum number of characters allowed in this textbox
        /// </summary>
        uint MaxLength { get; set; } = 0x10000;

        /// <summary>
        /// The textbox scrolled position
        /// </summary>
        public int ScrollPos { get; set; } = 0;

        /// <summary>
        /// The position of the caret. Text is inserted at the caret
        /// </summary>
        public int Caret { get; set; } = 0;

        public override bool CanFocus => true;

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                Caret = value.Length;
            }
        }

        public override bool Update(GameTime time)
        {
            if (HasFocus)
            {
                var keys = InputState.GetPressedKeys();
                foreach (var key in keys)
                {
                    if (!InputState.IsPress(key))
                        continue;

                    if (key == Keys.Back && Caret > 0)
                    {
                        base.Text = Text.Remove(Caret - 1, 1);
                        --Caret;
                    }

                    else if (key == Keys.Delete && Caret < Text.Length)
                        base.Text = Text.Remove(Caret, 1);

                    else if (AllowSpaces && key == Keys.Space)
                        InsertAtCaret(' ');

                    else if (key >= Keys.A && key < Keys.Z)
                    {
                        if (InputState.IsMod(KeyMod.Shift))
                            InsertAtCaret((char)((key - Keys.A) + 'A'));
                        else
                            InsertAtCaret((char)((key - Keys.A) + 'a'));
                    }

                    else if (key >= Keys.D0 && key < Keys.D9)
                        InsertAtCaret((char)((key - Keys.D0) + '0'));

                    else if (key >= Keys.NumPad0 && key < Keys.NumPad9)
                        InsertAtCaret((char)((key - Keys.NumPad0) + '0'));
                }
            }

            return base.Update(time);
        }

        void InsertAtCaret(char ch)
        {
            if (Text.Length >= MaxLength)
                return;

            base.Text = Text.Insert(Caret, ch.ToString());
            ++Caret;

            var textWidth = Font.MeasureString(Text, 0, Caret).X;
            ScrollPos = (int)MathHelper.Clamp(textWidth, textSize.X - Size.X, 0);

            //todo: handle delete (paging)
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
                Text,
                0, -1,
                new Rectangle(
                    AbsoluteBounds.X + 2,
                    AbsoluteBounds.Y + ((AbsoluteBounds.Height - size.Y) / 2),
                    size.X,
                    size.Y
                ),
                new Point(-ScrollPos, 0),
                Color
            );

            if (HasFocus && System.Environment.TickCount % 1000 < 500)
            {
                var x = Font.MeasureString(Text, 0, Caret).X - ScrollPos + AbsoluteBounds.X + 3;
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
