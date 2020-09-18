using Microsoft.Xna.Framework;

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
        private NumericBaseType _minimum = 0;

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
        /// The range of values that this numeric element can represent
        /// </summary>
        public NumericBaseType Range => System.Math.Abs(Maximum - Minimum);

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
                Text = "0",
                Style = "NumericInput.TextInput",
                AllowLetters = false,
                AllowNumbers = true,
                AllowSpaces = false,
                AllowSpecialCharacters = false,
                MaxLength = 20,
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

            var upButton = new Static
            {
                Text = "+",
                Style = "NumericInput.Button",
                HorizontalAlignment = Alignment.Stretch,
                VerticalAlignment = Alignment.Stretch
            };
            var downButton = new Static
            {
                Text = "-",
                Style = "NumericInput.Button",
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

            On(DragEvent, delegate (Static sender, UIEventArgs e)
            {
                var dea = (DragEventArgs)e;
                var sign = System.Math.Sign(dea.delta.Y);
                ((NumericInput)sender).DecrementValue(sign);

                //wrap mouse when dragging
#if WINDOWS
                if (dea.position.Y < 0 || dea.position.Y >= sender.ContentArea.Height)
                {
                    var y = (int)dea.position.Y % sender.ContentArea.Height;
                    y = y < 0 ? sender.ContentArea.Height + y : y;

                    Microsoft.Xna.Framework.Input.Mouse.SetPosition(
                        Input.InputState.MousePoint.X,
                        sender.OffsetContentArea.Y + y
                    );
                }
#endif

                return UIEventResult.Handled;
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
            return new Vector2(150, inputSz.Y);
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            var buttonSize = textInput.MeasuredSize.Y; //todo: use button measured size
            var shift = (int)(availableSize.X - buttonSize * (Children.Count - 1));
            //todo: hacky
            Children[0].Arrange(new Rectangle(0, 0, shift, (int)availableSize.Y));
            Children[1].Arrange(new Rectangle(shift, 0, (int)buttonSize, (int)availableSize.Y));
            Children[2].Arrange(new Rectangle(shift + (int)buttonSize, 0, (int)buttonSize, (int)availableSize.Y));
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
