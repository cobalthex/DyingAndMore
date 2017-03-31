using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class TextBox : Element
    {
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

                    else if (key == Keys.Space)
                        InsertAtCaret(' ');

                    else if (key >= Keys.A && key < Keys.Z)
                        InsertAtCaret((char)((key - Keys.A) + 'a'));

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
            base.Text = Text.Insert(Caret, ch.ToString());
            ++Caret;

            var textLen = Font.MeasureString(Text, 0, Caret).X;
            ScrollPos = (int)MathHelper.Clamp(textLen, textSize.X - Size.X, 0);

            //todo: handle delete (paging)
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Graphics.Primitives2D.DrawRect(spriteBatch, HasFocus ? Color.RoyalBlue : Color, AbsoluteBounds);

            var size = new Point(
                MathHelper.Min(AbsoluteBounds.Width, (int)textSize.X),
                MathHelper.Min(AbsoluteBounds.Height, (int)textSize.Y)
            );

            Font?.Draw(
                spriteBatch,
                Text,
                0, -1,
                new Rectangle(
                    AbsoluteBounds.X + 2,
                    AbsoluteBounds.Y + ((AbsoluteBounds.Height - size.Y) / 2),
                    size.X - 4,
                    size.Y
                ),
                new Point(-ScrollPos, 0),
                Color
            );

            var x = Font.MeasureString(Text, 0, Caret).X - ScrollPos + AbsoluteBounds.X;
            Graphics.Primitives2D.DrawLine(
                spriteBatch,
                Color,
                new Vector2(x, AbsoluteBounds.Top + 4),
                new Vector2(x, AbsoluteBounds.Bottom - 4)
            );

            foreach (var child in Children)
                child.Draw(spriteBatch);
        }
    }
}
