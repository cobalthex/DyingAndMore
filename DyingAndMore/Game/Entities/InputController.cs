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

        //todo: unify scalar and button inputs

        public InputController()
        {
            ActionInputs[Action.Fire] = new List<ActionInput> { new ActionInput(ActionInputType.MouseButton, (int)MouseButtons.Left) };
        }

        public PlayerIndex player = PlayerIndex.One;

        public override void Think(System.TimeSpan DeltaTime)
        {
            var color = Color.MediumAquamarine;

            var trace = actor.Map.Trace(actor.Position, actor.Forward, 0, actor);
            if (trace.entity != null)
                color = Color.Tomato;

            actor.Map.DrawLine(actor.Position, actor.Position + actor.Forward * trace.distance, color);

            var d = Vector2.Zero;
            if (InputState.IsButtonDown(Keys.W))
                d -= Vector2.UnitY;
            if (InputState.IsButtonDown(Keys.A))
                d -= Vector2.UnitX;
            if (InputState.IsButtonDown(Keys.S))
                d += Vector2.UnitY;
            if (InputState.IsButtonDown(Keys.D))
                d += Vector2.UnitX;

            if (InputState.IsButtonDown(MouseButtons.Left))
                actor.Weapon?.Charge();
            if (InputState.IsClick(MouseButtons.Left))
                actor.Weapon?.Reset();

            actor.Accelerate(d);

            var dir = InputState.PolarMouseVector;
            dir.Normalize();
            actor.Forward = dir;
        }
    }
}
