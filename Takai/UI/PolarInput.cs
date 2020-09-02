using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;

namespace Takai.UI
{
    /// <summary>
    /// A directional input that can be of length between 0 and 1
    /// </summary>
    public class PolarInput
        : Static
    {
        /// <summary>
        /// The value of the input, always normalized
        /// </summary>
        public Vector2 Value
        {
            get => _value;
            set
            {
                if (value == Vector2.Zero)
                {
                    _value = _norm = Vector2.Zero;
                    tip = center;
                }
                else
                {
                    float vlen = value.Length();
                    _norm = value / vlen;
                    _value = _norm * Math.Min(vlen, 1);
                    //store length?

                    tip = center + (DisplayNormalizedValue ? NormalizedValue : Value) * center;
                    BubbleEvent(ValueChangedEvent, new UIEventArgs(this));
                }
            }
        }
        private Vector2 _value;
        
        /// <summary>
        /// The value, always at length 1 (or 0)
        /// </summary>
        public Vector2 NormalizedValue
        {
            get => _norm;
            set
            {
                Value = value;
            }
        }
        private Vector2 _norm;

        public bool DisplayNormalizedValue { get; set; } = false;

        public float SnapAngle { get; set; } = MathHelper.PiOver4;

        public PolarInput() : this(Vector2.UnitX) { }
        public PolarInput(Vector2 value)
        {
            Value = value;

            On(DragEvent, OnTouch);
            On(PressEvent, OnTouch);
        }

        UIEventResult OnTouch(Static sender, UIEventArgs e)
        {
            var pea = (PointerEventArgs)e;
            if (pea.device != DeviceType.Mouse && pea.device != DeviceType.Touch &&
                pea.button != 0)
                return UIEventResult.Continue;

            var radius = (new Vector2(ContentArea.Width, ContentArea.Height) / 2);
            var dir = pea.position - radius - Padding;
            if (InputState.IsMod(KeyMod.Shift))
                dir = Util.Direction((float)Math.Round(dir.Angle() / SnapAngle) * SnapAngle);

            dir /= radius;
            ((PolarInput)sender).Value = dir;

            return UIEventResult.Handled;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return new Vector2(100);
        }

        Vector2 center, tip;
        protected override void ArrangeOverride(Vector2 availableSize)
        {
            center = new Vector2(ContentArea.Width / 2, ContentArea.Height / 2);
            base.ArrangeOverride(availableSize);
        }

        protected override bool HandleInput(GameTime time)
        {
            return base.HandleInput(time);
        }

        protected override void DrawSelf(DrawContext context)
        {
            base.DrawSelf(context);

            var dot = tip;
            DrawLine(context.spriteBatch, Color, center, tip);
            DrawFill(context.spriteBatch, Color, new Rectangle((int)dot.X - 2, (int)dot.Y - 2, 4, 4));

            //todo: draw circle
        }
    }
}
