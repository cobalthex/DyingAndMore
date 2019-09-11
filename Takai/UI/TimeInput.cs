using System;
using Microsoft.Xna.Framework;

namespace Takai.UI
{
    public class DurationInput : List
    {
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;

		public int Minutes
        {
            get => Duration.Minutes;
            set => Duration = new TimeSpan(0, 0, value, Seconds, Milliseconds);
        }

        public int Seconds
        {
            get => Duration.Seconds;
            set => Duration = new TimeSpan(0, 0, Minutes, value, Milliseconds);
        }

        public int Milliseconds
        {
            get => Duration.Milliseconds;
            set => Duration = new TimeSpan(0, 0, Minutes, Seconds, value);
        }

        public bool ShowMinutes
        {
            get => _showMinutes;
            set
            {
                if (value == _showMinutes)
                    return;

                _showMinutes = value;
                minutesContainer.IsEnabled = _showMinutes;
            }
        }
        private bool _showMinutes = true;
        private Static minutesContainer;

        public bool ShowSeconds
        {
            get => _showSeconds;
            set
            {
                if (value == _showSeconds)
                    return;

                _showSeconds = value;
                secondsContainer.IsEnabled = _showSeconds;
            }
        }
        private bool _showSeconds = true;
        private Static secondsContainer;

        public bool ShowMilliseconds
        {
            get => _showMilliseconds;
            set
            {
                if (value == _showMilliseconds)
                    return;

                _showMilliseconds = value;
                millisecondsContainer.IsEnabled = _showMilliseconds;
            }
        }
        private bool _showMilliseconds;
        private Static millisecondsContainer;

        public DurationInput()
        {
            Direction = Direction.Horizontal;
            Margin = 10;


            var minutes = new NumericInput
            {
                Minimum = 0,
                Maximum = int.MaxValue,
				Size = new Vector2(80, 20), //todo: some default input size here
				Bindings = new System.Collections.Generic.List<Data.Binding>
                {
					new Data.Binding("Minutes", "Value", Data.BindingDirection.TwoWay)
                }
            };

            var seconds = new NumericInput
            {
                Minimum = 0,
                Maximum = int.MaxValue,
				Size = new Vector2(80, 20),
                Bindings = new System.Collections.Generic.List<Data.Binding>
                {
                    new Data.Binding("Seconds", "Value", Data.BindingDirection.TwoWay)
                }
            };

            var milliseconds = new NumericInput
            {
                Minimum = 0,
                Maximum = int.MaxValue,
				Size = new Vector2(80, 20),
                Bindings = new System.Collections.Generic.List<Data.Binding>
                {
                    new Data.Binding("Milliseconds", "Value", Data.BindingDirection.TwoWay)
                }
            };

            minutesContainer = new List(minutes, new Static("min") { HorizontalAlignment = Alignment.Center }) { Direction = Direction.Vertical };
            secondsContainer = new List(seconds, new Static("sec") { HorizontalAlignment = Alignment.Center }) { Direction = Direction.Vertical };
            millisecondsContainer = new List(milliseconds, new Static("msec") { HorizontalAlignment = Alignment.Center }) { Direction = Direction.Vertical };

            AddChildren(minutesContainer, secondsContainer, millisecondsContainer);

			//todo: move labels to to containers with counters to hide/show combo
        }

        public override void BindTo(object source)
        {
            BindToThis(source);
            minutesContainer.BindTo(this);
            secondsContainer.BindTo(this);
            millisecondsContainer.BindTo(this);
        }

        protected override void UpdateSelf(GameTime time)
        {
            base.UpdateSelf(time);
        }
    }
}
