using Microsoft.Xna.Framework;
using Takai.UI;

namespace DyingAndMore.NotGame
{
    public struct WizardTransition
    {
        public string name;
        // transition style
    }

    /// <summary>
    /// A wizard is composed of a number of frames (children) and allows for command based control of which frame is in focus.
    /// Frames can be in any order, and jumping between frames will always appear as if they are consecutive
    /// </summary>
    public class Wizard : Static
    {
        // todo: implement coroutines and this could probably be done without the need of a container

        public string TransitionCommand = "WizardTransition";

        private int _activeFrameIndex;
        public int ActiveFrameIndex
        {
            get => _activeFrameIndex;
            set
            {
                if (value == _activeFrameIndex)
                    return;

                var lastFrame = _activeFrameIndex;
                _activeFrameIndex = MathHelper.Clamp(value, 0, Children.Count);

                if (lastFrame == _activeFrameIndex)
                    return;
                
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        Static ActiveFrame => Children.Count == 0 ? null : Children[ActiveFrameIndex];
        //Static transitionFrame = null; // calculated?

        public Wizard()
        {
            SetupHandlers();
        }

        public Wizard(params Static[] children)
            : base(children)
        {
            SetupHandlers();
        }

        private void SetupHandlers()
        {
            CommandActions[TransitionCommand] = delegate (Static sender, object arg)
            {
                var self = (Wizard)sender;

                if (arg is string stateName)
                    self.TransitionTo(new WizardTransition { name = stateName });
                else if (arg is WizardTransition transition)
                    self.TransitionTo(transition);
                else
                    System.Diagnostics.Debug.WriteLine("Unknown transition command argument: " + arg);
            };
        }

        protected override bool InternalInsertChild(Static child, int index = -1, bool reflow = true, bool ignoreFocus = false)
        {
            bool didInsert = base.InternalInsertChild(child, index, reflow, ignoreFocus);
            if (didInsert)
            {
                if (Children.Count == 1)
                {
                    // should re-evaluate the active state
                    InvalidateMeasure();
                    InvalidateArrange();
                }
            }
            return didInsert;
        }
        protected override Static InternalRemoveChild(int index, bool reflow = true)
        {
            var removed = base.InternalRemoveChild(index, reflow);
            if (removed != null)
            {
                // should re-evaluate the active state
                InvalidateMeasure();
                InvalidateArrange();
            }
            return removed;
        }

        protected override Static InternalSwapChild(Static child, int index, bool reflow = true, bool ignoreFocus = false)
        {
            var swapped = base.InternalSwapChild(child, index, reflow, ignoreFocus);
            if (swapped != null)
            {
                // should re-evaluate the active state
                InvalidateMeasure();
                InvalidateArrange();
            }
            return swapped;
        }

        public void TransitionTo(WizardTransition transition)
        {
            if (transition.name == null)
                return;

            for (int i = 0; i < Children.Count; ++i)
            {
                if (Children[i].Name.Equals(transition.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    ActiveFrameIndex = i;
                    // todo: transition
                    break;
                }
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            return ActiveFrame?.Measure(availableSize) ?? Vector2.Zero;
        }

        protected override void ArrangeOverride(Vector2 availableSize)
        {
            foreach (var child in Children)
            {
                if (child == ActiveFrame)
                    child.Arrange(new Rectangle(0, 0, (int)availableSize.X, (int)availableSize.Y));
                else
                    child.Arrange(Rectangle.Empty);
            }
        }

        // todo: transition animation
    }
}
