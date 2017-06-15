using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Graphics;

using NumericBaseType = System.Int64;

namespace Takai.UI
{
    public abstract class NumericBase : Static
    {
        /// <summary>
        /// The current value of this input
        /// Calculated on the fly. Returns Minimum if failed to parse
        /// </summary>
        public NumericBaseType Value
        {
            get
            {
                if (NumericBaseType.TryParse(Text, out var val))
                    return (val < Minimum ? Minimum : (val > Maximum ? Maximum : val));
                return Minimum;
            }
            set
            {
                this.value = (value < Minimum ? Minimum : (value > Maximum ? Maximum : value));

                if (this.value == value)
                    return;

                OnValueChanged(System.EventArgs.Empty);
                ValueChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }
        private NumericBaseType value;

        /// <summary>
        /// The normalized value (from 0 to 1) of this range
        /// </summary>
        public float NormalizedValue
        {
            get => (Value - Minimum) / (float)(Maximum - Minimum);
            set => Value = (NumericBaseType)(value * (Maximum - Minimum)) + Minimum;
        }

        /// <summary>
        /// The minimum allowed value
        /// Setting this may affect Value
        /// </summary>
        public NumericBaseType Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                Value = (value > minimum ? value : minimum);
            }
        }

        /// <summary>
        /// The maximum allowed value
        /// Setting this may affect Value
        /// </summary>
        public NumericBaseType Maximum
        {
            get => maximum;
            set
            {
                maximum = value;
                Value = (value > maximum ? maximum : value);
            }
        }
        protected NumericBaseType minimum = NumericBaseType.MinValue;
        protected NumericBaseType maximum = NumericBaseType.MaxValue;

        /// <summary>
        /// How much to increase or decrease the value by each step
        /// </summary>
        public NumericBaseType Increment { get; set; } = 1;

        /// <summary>
        /// called whenever this numeric's value has changed
        /// </summary>
        public event System.EventHandler ValueChanged;

        protected virtual void OnValueChanged(System.EventArgs e) { }
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
                if (NumericBaseType.TryParse(value, out var val))
                {
                    Value = val;
                    textInput.Text = value;
                }
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
                if (NumericBaseType.TryParse(textInput.Text, out var val))
                {
                    Value = val;
                    if (Value != val)
                        textInput.Text = Value.ToString();
                }
            };

            upButton = new Static()
            {
                Text = "+",
            };
            upButton.Click += delegate
            {
                Value += Increment;
            };

            downButton = new Static()
            {
                Text = "-",
            };
            downButton.Click += delegate
            {
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
