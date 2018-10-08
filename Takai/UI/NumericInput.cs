﻿using System;
using Microsoft.Xna.Framework;
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

                if (_value != newVal)
                {
                    _value = newVal;
                    OnValueChanged(EventArgs.Empty);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    onValueChangedCommandFn?.Invoke(this);
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
            get => _minimum;
            set
            {
                _minimum = value;
                if (Value < _minimum)
                    Value = _minimum;
            }
        }
        private NumericBaseType _minimum = NumericBaseType.MinValue;

        /// <summary>
        /// The maximum allowed value
        /// Setting this may affect Value
        /// </summary>
        public NumericBaseType Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                if (Value > _maximum)
                    Value = _maximum;
            }
        }
        private NumericBaseType _maximum = NumericBaseType.MaxValue;

        /// <summary>
        /// How much to increase or decrease the value by each step
        /// </summary>
        public NumericBaseType Increment { get; set; } = 1;

        /// <summary>
        /// called whenever this numeric's value has changed
        /// </summary>
        public event EventHandler ValueChanged;

        public string OnValueChangedCommand { get; set; }
        protected Command onValueChangedCommandFn;

        protected virtual void OnValueChanged(EventArgs e) { }

        protected override void BindCommandToThis(string command, Command commandFn)
        {
            base.BindCommandToThis(command, commandFn);

            if (command == OnValueChangedCommand)
                onValueChangedCommandFn = commandFn;
        }

        public virtual void IncrementValue(int scale = 1)
        {
            Value += Increment * scale;
        }
        public void DecrementValue(int scale = 1)
        {
            IncrementValue(-scale);
        }
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

        protected TextInput textInput = new TextInput
        {
            Text = "0",
            AllowLetters = false,
            AllowNumbers = true,
            AllowSpaces = false,
            AllowSpecialCharacters = false,
            MaxLength = 20,
            BorderColor = Color.Transparent,
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch
        };
        protected Static upButton = new Static
        {
            Text = "+",
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch
        };
        protected Static downButton = new Static
        {
            Text = "-",
            HorizontalAlignment = Alignment.Stretch,
            VerticalAlignment = Alignment.Stretch
        };

        public NumericInput()
        {
            Resize += delegate
            {
                var height = Size.Y;
                textInput.Size = new Vector2(Size.X - height * 2, height);
                upButton.Size = downButton.Size = new Vector2(height);
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

            upButton.Click += delegate
            {
                IncrementValue();
            };

            downButton.Click += delegate
            {
                DecrementValue();
            };

            AddChildren(textInput, upButton, downButton);
        }

        public override void IncrementValue(int scale = 1)
        {
            base.IncrementValue(scale);
            textInput.Text = Value.ToString();
        }

        protected override void OnValueChanged(EventArgs e)
        {
            textInput.Text = Value.ToString();
            textInput.ScrollPosition = 0;
            base.OnValueChanged(e);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var inputSz = textInput.Measure(availableSize);
            return inputSz + new Vector2(inputSz.Y * 2, 0);
        }

        protected override void OnResize(EventArgs e)
        {
            var btnSize = Size.Y;
            textInput.Size = new Vector2(Size.X - btnSize * 2, Size.Y);
            upButton.Size = downButton.Size = new Vector2(btnSize);
            base.OnResize(e);
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            var sz = availableSize.ToPoint(); //todo
            var buttonSize = (int)Math.Max(upButton.Bounds.Width, downButton.Bounds.Height);
            textInput.Reflow(new Rectangle(0, 0, sz.X - buttonSize * 2, sz.Y));
            upButton.Reflow(new Rectangle(sz.X - buttonSize * 2, 0, buttonSize, sz.Y));
            downButton.Reflow(new Rectangle(sz.X - buttonSize, 0, buttonSize, sz.Y));
        }

        protected override bool HandleInput(GameTime time)
        {
            if (HasFocus)
            {
                if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Up))
                {
                    IncrementValue();
                    return false;
                }

                if (Input.InputState.IsPress(Microsoft.Xna.Framework.Input.Keys.Down))
                {
                    DecrementValue();
                    return false;
                }
            }
            return base.HandleInput(time);
        }
    }
}
