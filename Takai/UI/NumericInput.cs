﻿using Microsoft.Xna.Framework;

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
                    BubbleEvent(ValueChangedEvent, new UIEventArgs(this));
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
        TextInput textInput;

        public NumericInput()
        {
            textInput = new TextInput
            {
                Name = "clarq",
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

            On(TextChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (NumericInput)sender;
                var input = (TextInput)e.Source;
                if (NumericBaseType.TryParse(input.Text, out var val))
                {
                    self.Value = val;
                    if (self.Value != val)
                        input.Text = self.Value.ToString();
                }
                return UIEventResult.Continue;
            });

            BorderColor = Color;

            var upButton = new Static
            {
                Text = "+",
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };
            var downButton = new Static
            {
                Text = "-",
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };

            upButton.EventCommands[ClickEvent] = "IncrementValue";
            downButton.EventCommands[ClickEvent] = "DecrementValue";

            CommandActions["IncrementValue"] = delegate (Static sender, object arg)
            {
                ((NumericInput)sender).IncrementValue();
            };
            CommandActions["DecrementValue"] = delegate (Static sender, object arg)
            {
                ((NumericInput)sender).DecrementValue();
            };

            On(ValueChangedEvent, delegate (Static sender, UIEventArgs e)
            {
                var self = (NumericInput)sender;
                self.textInput.Text = self.Value.ToString();
                self.textInput.ScrollPosition = 0;
                return UIEventResult.Continue;
            });

            AddChildren(textInput, upButton, downButton);
        }

        protected override void FinalizeClone()
        {
            textInput = (TextInput)Children[textInput.ChildIndex];
            base.FinalizeClone();
        }

        public override void IncrementValue(int scale = 1)
        {
            base.IncrementValue(scale);
            textInput.Text = Value.ToString();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var inputSz = textInput.Measure(availableSize);
            return inputSz + new Vector2(inputSz.Y * (Children.Count - 1), 0);
        }

        protected override void ReflowOverride(Vector2 availableSize)
        {
            var buttonSize = textInput.MeasuredSize.Y;
            var shift = (int)(availableSize.X - buttonSize * (Children.Count - 1));
            int n = 0;
            foreach (var child in Children)
            {
                if (child == textInput)
                    child.Reflow(new Rectangle(0, 0, shift, (int)availableSize.Y));
                else
                    child.Reflow(new Rectangle(shift + (int)(n++ * buttonSize), 0, (int)buttonSize, (int)availableSize.Y));
            }

            //todo: this is ugly
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
