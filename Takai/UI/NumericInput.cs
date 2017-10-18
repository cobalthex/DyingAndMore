﻿using System;
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
            get => _value;
            set
            {
                var newVal = (value < Minimum ? Minimum : (value > Maximum ? Maximum : value));

                if (this._value != newVal)
                {
                    this._value = newVal;
                    OnValueChanged(EventArgs.Empty);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private NumericBaseType _value;

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
                if (Value < minimum)
                    Value = minimum;
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
                if (Value > maximum)
                    Value = maximum;
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
        public event EventHandler ValueChanged;

        protected virtual void OnValueChanged(EventArgs e) { }
    }

    /// <summary>
    /// An integer input selector
    /// </summary>
    public class NumericInput : NumericBase
    {
        public override BitmapFont Font
        {
            get => textInput.Font;
            set
            {
                textInput.Font = upButton.Font = downButton.Font = value;
            }
        }

        public override Color Color
        {
            get => textInput.Color;
            set
            {
                textInput.Color = upButton.Color = downButton.Color = value;
            }
        }

        protected TextInput textInput;
        protected Static upButton, downButton;

        public NumericInput()
        {
            Resize += delegate
            {
                var height = Size.Y;
                textInput.Size = new Vector2(Size.X - height * 2, height);
                upButton.Size = downButton.Size = new Vector2(height);
            };

            textInput = new TextInput()
            {
                Text = "0",
                AllowLetters = false,
                AllowNumbers = true,
                AllowSpaces = false,
                AllowSpecialCharacters = false,
                MaxLength = 20,
                BorderColor = Color.Transparent,
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

            BorderColor = Color;

            upButton = new Static()
            {
                Text = "+",
            };
            upButton.Click += delegate
            {
                Value += Increment;
                textInput.Text = Value.ToString();
            };

            downButton = new Static()
            {
                Text = "-",
            };
            downButton.Click += delegate
            {
                Value -= Increment;
                textInput.Text = Value.ToString();
            };

            AddChildren(textInput, upButton, downButton);
        }

        protected override void OnValueChanged(EventArgs e)
        {
            textInput.Text = Value.ToString();
            base.OnValueChanged(e);
        }

        public override void AutoSize(float padding = 0)
        {
            textInput.AutoSize(padding);
            var btnSize = textInput.Size.Y;
            upButton.Size = downButton.Size = new Vector2(btnSize);

            Size = textInput.Size + new Vector2(btnSize * 2, 0);
            base.AutoSize(padding);
        }

        protected override void OnResize(EventArgs e)
        {
            var btnSize = Size.Y;
            textInput.Size = new Vector2(Size.X - btnSize * 2, Size.Y);
            upButton.Size = downButton.Size = new Vector2(btnSize);
            upButton.Position = new Vector2(Size.X - btnSize * 2, 0);
            downButton.Position = new Vector2(Size.X - btnSize, 0);
            base.OnResize(e);
        }

        public override void Reflow()
        {
            upButton.Position = textInput.Position + new Vector2(textInput.Size.X, 0);
            downButton.Position = upButton.Position + new Vector2(upButton.Size.X, 0);
            base.Reflow();
        }
    }
}
