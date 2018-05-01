using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Input;

namespace DyingAndMore.Game.Entities
{
    class InputController : Controller
    {
        public enum Action
        {
            Unknown,
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,
            Fire,
        }

        public enum ActionInputType
        {
            None,
            Keyboard,
            MouseButton,
            JoystickButton,
        }

        public struct ActionInput
        {
            public ActionInputType type;
            public int which;

            public ActionInput(ActionInputType type, int which)
            {
                this.type = type;
                this.which = which;
            }
        }

        public bool IsActive(ActionInput action)
        {
            switch (action.type)
            {
                case ActionInputType.Keyboard:
                    return InputState.IsButtonDown((Keys)action.which);
                case ActionInputType.MouseButton:
                    return InputState.IsButtonDown((MouseButtons)action.which);
                default:
                    return false;
            }
        }
        public bool Released(ActionInput action)
        {
            switch (action.type)
            {
                case ActionInputType.Keyboard:
                    return InputState.IsClick((Keys)action.which);
                case ActionInputType.MouseButton:
                    return InputState.IsClick((MouseButtons)action.which);
                default:
                    return false;
            }
        }

        public Dictionary<Action, List<ActionInput>> ActionInputs { get; set; }
            = new Dictionary<Action, List<ActionInput>>();

        public PlayerIndex player = PlayerIndex.One;

        public InputController()
        {
            ActionInputs[Action.Fire] = new List<ActionInput> { new ActionInput(ActionInputType.MouseButton, (int)MouseButtons.Left) };
        }

        public override void Think(System.TimeSpan deltaTime)
        {
            if (GameInstance.Current != null && !GameInstance.Current.GameplaySettings.isPlayerInputEnabled)
                return;

            var d = Vector2.Zero;
            if (InputState.IsButtonDown(Keys.W))
                d -= Vector2.UnitY;
            if (InputState.IsButtonDown(Keys.A))
                d -= Vector2.UnitX;
            if (InputState.IsButtonDown(Keys.S))
                d += Vector2.UnitY;
            if (InputState.IsButtonDown(Keys.D))
                d += Vector2.UnitX;

            var sticks = InputState.Thumbsticks(player);
            if (sticks.Left != Vector2.Zero)
                d = Vector2.Normalize(sticks.Left) * new Vector2(1, -1);

            if (sticks.Right != Vector2.Zero)
                Actor.TurnTowards(Vector2.Normalize(sticks.Right) * new Vector2(1, -1));

            if (InputState.IsButtonDown(MouseButtons.Left) ||
                InputState.IsButtonDown(Buttons.RightTrigger, player) ||
                InputState.IsButtonDown(Keys.Space))
                Actor.Weapon?.TryUse();

            Actor.Accelerate(d);

            if (InputState.MouseDelta() != Vector2.Zero)
            {
                var dir = InputState.PolarMouseVector;
                dir.Normalize();
                Actor.Forward = dir;
            }
        }
    }
}
