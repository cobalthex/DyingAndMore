using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Graphics;

namespace Takai.UI
{
    public abstract class NumericBase : Static
    {
        /// <summary>
        /// The current value of this input
        /// Calculated on the fly. Returns Minimum if failed to parse
        /// </summary>
        public int Value
        {
            get
            {
                if (int.TryParse(Text, out int val))
                    return MathHelper.Clamp(val, Minimum, Maximum);
                return Minimum;
            }
            set
            {
                this.value = MathHelper.Clamp(value, Minimum, Maximum);
                Text = value.ToString();
            }
        }
        protected int value;

        /// <summary>
        /// The normalized value (from 0 to 1) of this range
        /// </summary>
        public float NormalizedValue
        {
            get => (Value - Minimum) / (float)(Maximum - Minimum);
            set => Value = (int)(value * (Maximum - Minimum)) + Minimum;
        }

        /// <summary>
        /// The minimum allowed value
        /// Setting this may affect Value
        /// </summary>
        public int Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                Value = MathHelper.Max(Value, minimum);
            }
        }

        /// <summary>
        /// The maximum allowed value
        /// Setting this may affect Value
        /// </summary>
        public int Maximum
        {
            get => maximum;
            set
            {
                maximum = value;
                Value = MathHelper.Min(Value, maximum);
            }
        }
        protected int minimum = int.MinValue;
        protected int maximum = int.MaxValue;

        /// <summary>
        /// How much to increase or decrease the value by each step
        /// </summary>
        public int Increment { get; set; } = 1;
    }

    /// <summary>
    /// An integer input selector
    /// </summary>
    public class NumericInput : NumericBase
    {
        /// <summary>
        /// Attempt to set the text of this input. Must be a valid number to be set
        /// </summary>
        public override string Text
        {
            get => textInput.Text;
            set
            {
                if (int.TryParse(value, out this.value))
                    textInput.Text = value;
            }
        }

        public override BitmapFont Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                textInput.Font = upButton.Font = downButton.Font = value;
            }
        }

        public override Color Color
        {
            get => base.Color;
            set
            {
                base.Color = value;
                textInput.Color = upButton.Color = downButton.Color = value;
            }
        }

        protected TextInput textInput;
        protected Static upButton, downButton;

        public NumericInput()
        {
            textInput = new TextInput()
            {
                Text = "0",
                AllowLetters = false,
                AllowNumbers = true,
                AllowSpaces = false,
                AllowSpecialCharacters = false,
                MaxLength = 20,
                OutlineColor = Color.Transparent
            };
            textInput.TextChanged += delegate
            {
                if (int.TryParse(textInput.Text, out int val))
                {
                    var clamped = MathHelper.Clamp(val, Minimum, Maximum);
                    if (val != clamped)
                    {
                        textInput.Text = clamped.ToString();
                        value = val;
                    }
                }
            };

            upButton = new Static()
            {
                Text = "+",
            };
            upButton.Click += delegate
            {
                if (value < Maximum)
                    Value += Increment;
            };

            downButton = new Static()
            {
                Text = "-",
            };
            downButton.Click += delegate
            {
                if (value > Minimum)
                    Value -= Increment;
            };

            AddChildren(textInput, upButton, downButton);

            Resize += delegate
            {
                var height = Size.Y;
                textInput.Size = new Vector2(Size.X - height * 2, height);
                upButton.Size = downButton.Size = new Vector2(height);
            };

            OutlineColor = Color;
        }

        public override void AutoSize(float padding = 0)
        {
            textInput.AutoSize(padding);
            var btnSize = textInput.Size.Y;
            upButton.Size = downButton.Size = new Vector2(btnSize);

            Size = textInput.Size + new Vector2(btnSize * 2, 0);
        }

        public override void Reflow()
        {
            upButton.Position = textInput.Position + new Vector2(textInput.Size.X, 0);
            downButton.Position = upButton.Position + new Vector2(upButton.Size.X, 0);
            base.Reflow();
        }
    }
}
