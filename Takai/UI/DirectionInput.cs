using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Graphics;
using Takai.Input;

namespace Takai.UI
{
    public class DirectionInput : Static
    {
        /// <summary>
        /// The value of the input, always normalized
        /// </summary>
        public Vector2 Direction
        {
            get => _direction;
            set
            {
                _direction = Vector2.Normalize(value);
            }
        }
        private Vector2 _direction;

        protected Vector2 virtualDirection;

        public float SnapAngle { get; set; } = MathHelper.PiOver4;

        public DirectionInput() : this(Vector2.UnitX) { }
        public DirectionInput(Vector2 value)
        {
            Direction = value;

            On(Static.DragEvent, OnTouch);
            On(Static.PressEvent, OnTouch);
        }

        UIEventResult OnTouch(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;
            if (pea.device != DeviceType.Mouse && pea.device != DeviceType.Touch &&
                pea.button != 0)
                return UIEventResult.Continue;

            var dir = pea.position - (new Vector2(ContentArea.Width, ContentArea.Height) / 2) - Padding;
            if (InputState.IsMod(KeyMod.Shift))
                dir = Util.Direction((float)Math.Round(dir.Angle() / SnapAngle) * SnapAngle);

            ((DirectionInput)sender).Direction = dir;

            return UIEventResult.Handled;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(100);
        }

        protected override bool HandleInput(GameTime time)
        {
            return base.HandleInput(time);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            var center = new Vector2(ContentArea.Width / 2, ContentArea.Height / 2);
            var tip = center + Direction * center;
            var dot = tip;

            DrawLine(spriteBatch, Color, center, tip);
            DrawFill(spriteBatch, Color, new Rectangle((int)dot.X - 2, (int)dot.Y - 2, 4, 4));

            //todo: draw circle
        }
    }
}
