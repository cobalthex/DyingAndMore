using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;

#if !WINDOWS_PHONE
namespace Takai.States
{
    /// <summary>
    /// A simple state for testing the gamepad, keyboard, and mouse
    /// </summary>
    public class TestInputState : Takai.States.State
    {
        GraphicsDevice gd;
        SpriteBatch sb;

        int fntH = 0; //height of font
        int GP = 0; //gamepad index

        /// <summary>
        /// The font to use for drawing the screen
        /// </summary>
        public SpriteFont font
        {
            set
            {
                fnt = value;
                fntH = (int)fnt.MeasureString("|").Y;
            }
            get { return fnt; }
        }
        SpriteFont fnt;
        /// <summary>
        /// the color of the font
        /// </summary>
        public Color fontColor;

        /// <summary>
        /// Create a simple input testing screen
        /// </summary>
        /// <param name="font">THe font to use to draw the information</param>
        public TestInputState(SpriteFont font, Color FontColor)
            : base(Takai.States.StateType.Full)
        {
            fnt = font;
            fontColor = FontColor;
        }

        public override void Load()
        {
            gd = StateManager.game.GraphicsDevice;
            StateManager.game.IsMouseVisible = true;
            sb = new SpriteBatch(gd);

            fntH = (int)fnt.MeasureString("|").Y;
        }

        public override void Update(GameTime time)
        {
            if (!StateManager.game.IsActive)
                return;

            if (BtnPrs(Buttons.A)) //vibrate
                Microsoft.Xna.Framework.Input.GamePad.SetVibration((PlayerIndex)GP, InputCatalog.GPadState[GP].Triggers.Left, InputCatalog.GPadState[GP].Triggers.Right);
            else
                Microsoft.Xna.Framework.Input.GamePad.SetVibration((PlayerIndex)GP, 0, 0);

            //change index
            if (KeyPrs(Keys.D1) || KeyPrs(Keys.NumPad1)) GP = 0;
            else if (KeyPrs(Keys.D2) || KeyPrs(Keys.NumPad2)) GP = 1;
            else if (KeyPrs(Keys.D3) || KeyPrs(Keys.NumPad3)) GP = 2;
            else if (KeyPrs(Keys.D4) || KeyPrs(Keys.NumPad4)) GP = 3;
        }

        public override void Draw(GameTime time)
        {
            gd.Clear(Color.White);
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            int h = 10;
            if (InputCatalog.GPadState[GP].IsConnected) //test gamepad input
            {
                sb.DrawString(fnt, "Test gamepad [" + (GP + 1) + "] input:", new Vector2(10), fontColor); h += fntH;

                h += fntH;
                sb.DrawString(fnt, "Hold A and left/right trigger to vibrate", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Use 1234 to change gamepad", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                sb.DrawString(fnt, "A is " + (BtnPrs(Buttons.A) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "B is " + (BtnPrs(Buttons.B) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "X is " + (BtnPrs(Buttons.X) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Y is " + (BtnPrs(Buttons.Y) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                sb.DrawString(fnt, "DPad Left is " + (BtnPrs(Buttons.DPadLeft) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "DPad Right is " + (BtnPrs(Buttons.DPadRight) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "DPad Up is " + (BtnPrs(Buttons.DPadUp) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "DPad Down is " + (BtnPrs(Buttons.DPadDown) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                sb.DrawString(fnt, "Start is " + (BtnPrs(Buttons.Start) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Back is " + (BtnPrs(Buttons.Back) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Guide is " + (BtnPrs((Buttons)1024) ? "" : "not ") + "pressed*", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Big Button is " + (BtnPrs(Buttons.BigButton) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                sb.DrawString(fnt, "Left Bumper is " + (BtnPrs(Buttons.LeftShoulder) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Right Bumper is " + (BtnPrs(Buttons.RightShoulder) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                sb.DrawString(fnt, "Left Stick is " + (BtnPrs(Buttons.LeftStick) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Right Stick is " + (BtnPrs(Buttons.RightStick) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Left Stick value: " + InputCatalog.GPadState[GP].ThumbSticks.Left.X + " , " + InputCatalog.GPadState[GP].ThumbSticks.Left.Y, new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Right Stick value: " + InputCatalog.GPadState[GP].ThumbSticks.Right.X + " , " + InputCatalog.GPadState[GP].ThumbSticks.Right.Y, new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                sb.DrawString(fnt, "Left Trigger is " + (BtnPrs(Buttons.LeftTrigger) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Right Trigger is " + (BtnPrs(Buttons.RightTrigger) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Left Trigger value: " + InputCatalog.GPadState[GP].Triggers.Left, new Vector2(10, h), fontColor); h += fntH;
                sb.DrawString(fnt, "Right Trigger value: " + InputCatalog.GPadState[GP].Triggers.Right, new Vector2(10, h), fontColor); h += fntH;
            }
            else
            {
                sb.DrawString(fnt, "Gamepad [" + (GP + 1) + "] is not plugged in.", new Vector2(10), fontColor);
                sb.DrawString(fnt, "Use 1234 to change gamepad", new Vector2(10, 10 + fntH), fontColor);
            }

            int x = gd.Viewport.Width >> 1;
            h = 10 + (fntH << 1);

            sb.DrawString(fnt, "Test mouse input:", new Vector2(x, 10), fontColor);

#if WINDOWS
            //test mouse input
            sb.DrawString(fnt, "Left Button is " + (InputCatalog.MouseState.LeftButton == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            sb.DrawString(fnt, "Right Button is " + (InputCatalog.MouseState.RightButton == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            sb.DrawString(fnt, "Middle Button is " + (InputCatalog.MouseState.MiddleButton == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            sb.DrawString(fnt, "Button 4 is " + (InputCatalog.MouseState.XButton1 == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            sb.DrawString(fnt, "Button 5 is " + (InputCatalog.MouseState.XButton2 == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;

            h += fntH;
            sb.DrawString(fnt, "Mouse position: " + InputCatalog.MouseState.X + " , " + InputCatalog.MouseState.Y, new Vector2(x, h), fontColor); h += fntH;
            sb.DrawString(fnt, "Scroll wheel value: " + InputCatalog.MouseState.ScrollWheelValue, new Vector2(x, h), fontColor); h += fntH;
#endif

            //test keyboard input
            h += fntH << 1;
            sb.DrawString(fnt, "Test keyboard input:", new Vector2(x, h), fontColor);
            h += fntH << 1;

            Keys[] kz = InputCatalog.KBState.GetPressedKeys();
            if (kz.Length > 1)
            {
                for (int i = 1; i < kz.Length; i++)
                {
                    sb.DrawString(fnt, i + ". " + kz[i].ToString() + " is pressed", new Vector2(x, h), fontColor);
                    h += fntH;
                }
            }

            sb.End();
        }

        bool BtnPrs(Buttons b)
        {
            return InputCatalog.GPadState[GP].IsButtonDown(b);
        }

        bool KeyPrs(Keys k)
        {
            return InputCatalog.KBState[k] == KeyState.Down;
        }
    }
}
#endif