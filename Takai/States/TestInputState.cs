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
        public Graphics.BitmapFont Font
        {
            set
            {
                fnt = value;
                fntH = (int)fnt.MeasureString("|").Y;
            }
            get { return fnt; }
        }
        Graphics.BitmapFont fnt;
        /// <summary>
        /// the color of the font
        /// </summary>
        public Color fontColor;

        /// <summary>
        /// Create a simple input testing screen
        /// </summary>
        /// <param name="font">THe font to use to draw the information</param>
        public TestInputState(Graphics.BitmapFont Font, Color FontColor)
            : base(Takai.States.StateType.Full)
        {
            this.Font = Font;
            fontColor = FontColor;
        }

        public override void Load()
        {
            gd = StateManager.game.GraphicsDevice;
            StateManager.game.IsMouseVisible = true;
            sb = new SpriteBatch(gd);
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
                fnt.Draw(sb, "Test gamepad [" + (GP + 1) + "] input:", new Vector2(10), fontColor); h += fntH;

                h += fntH;
                fnt.Draw(sb, "Hold A and left/right trigger to vibrate", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Use 1234 to change gamepad", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                fnt.Draw(sb, "A is " + (BtnPrs(Buttons.A) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "B is " + (BtnPrs(Buttons.B) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "X is " + (BtnPrs(Buttons.X) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Y is " + (BtnPrs(Buttons.Y) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                fnt.Draw(sb, "DPad Left is " + (BtnPrs(Buttons.DPadLeft) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "DPad Right is " + (BtnPrs(Buttons.DPadRight) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "DPad Up is " + (BtnPrs(Buttons.DPadUp) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "DPad Down is " + (BtnPrs(Buttons.DPadDown) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                fnt.Draw(sb, "Start is " + (BtnPrs(Buttons.Start) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Back is " + (BtnPrs(Buttons.Back) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Guide is " + (BtnPrs((Buttons)1024) ? "" : "not ") + "pressed*", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Big Button is " + (BtnPrs(Buttons.BigButton) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                fnt.Draw(sb, "Left Bumper is " + (BtnPrs(Buttons.LeftShoulder) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Right Bumper is " + (BtnPrs(Buttons.RightShoulder) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                fnt.Draw(sb, "Left Stick is " + (BtnPrs(Buttons.LeftStick) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Right Stick is " + (BtnPrs(Buttons.RightStick) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Left Stick value: " + InputCatalog.GPadState[GP].ThumbSticks.Left.X + " , " + InputCatalog.GPadState[GP].ThumbSticks.Left.Y, new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Right Stick value: " + InputCatalog.GPadState[GP].ThumbSticks.Right.X + " , " + InputCatalog.GPadState[GP].ThumbSticks.Right.Y, new Vector2(10, h), fontColor); h += fntH;

                h += fntH;
                fnt.Draw(sb, "Left Trigger is " + (BtnPrs(Buttons.LeftTrigger) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Right Trigger is " + (BtnPrs(Buttons.RightTrigger) ? "" : "not ") + "pressed", new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Left Trigger value: " + InputCatalog.GPadState[GP].Triggers.Left, new Vector2(10, h), fontColor); h += fntH;
                fnt.Draw(sb, "Right Trigger value: " + InputCatalog.GPadState[GP].Triggers.Right, new Vector2(10, h), fontColor); h += fntH;
            }
            else
            {
                fnt.Draw(sb, "Gamepad [" + (GP + 1) + "] is not plugged in.", new Vector2(10), fontColor);
                fnt.Draw(sb, "Use 1234 to change gamepad", new Vector2(10, 10 + fntH), fontColor);
            }

            int x = gd.Viewport.Width >> 1;
            h = 10 + (fntH << 1);

            fnt.Draw(sb, "Test mouse input:", new Vector2(x, 10), fontColor);

#if WINDOWS
            //test mouse input
            fnt.Draw(sb, "Left Button is " + (InputCatalog.MouseState.LeftButton == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            fnt.Draw(sb, "Right Button is " + (InputCatalog.MouseState.RightButton == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            fnt.Draw(sb, "Middle Button is " + (InputCatalog.MouseState.MiddleButton == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            fnt.Draw(sb, "Button 4 is " + (InputCatalog.MouseState.XButton1 == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;
            fnt.Draw(sb, "Button 5 is " + (InputCatalog.MouseState.XButton2 == ButtonState.Pressed ? "" : "not ") + "pressed", new Vector2(x, h), fontColor); h += fntH;

            h += fntH;
            fnt.Draw(sb, "Mouse position: " + InputCatalog.MouseState.X + " , " + InputCatalog.MouseState.Y, new Vector2(x, h), fontColor); h += fntH;
            fnt.Draw(sb, "Scroll wheel value: " + InputCatalog.MouseState.ScrollWheelValue, new Vector2(x, h), fontColor); h += fntH;
#endif

            //test keyboard input
            h += fntH << 1;
            fnt.Draw(sb, "Test keyboard input:", new Vector2(x, h), fontColor);
            h += fntH << 1;

            Keys[] kz = InputCatalog.KBState.GetPressedKeys();
            for (int i = 0; i < kz.Length; i++)
            {
                fnt.Draw(sb, i + ". " + kz[i].ToString() + " is pressed", new Vector2(x, h), fontColor);
                h += fntH;
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