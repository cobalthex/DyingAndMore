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

        public override void Think(System.TimeSpan deltaTime)
        {
            var color = Color.MediumAquamarine;

            var trace = Actor.Map.Trace(Actor.Position, Actor.Forward, 0, Actor);
            if (trace.entity != null)
                color = Color.Tomato;

            Actor.Map.DrawLine(Actor.Position, Actor.Position + Actor.Forward * trace.distance, color);

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
                Actor.Weapon?.TryFire();
            if (InputState.IsClick(MouseButtons.Left))
                Actor.Weapon?.Reset();

            Actor.Accelerate(d);

            var dir = InputState.PolarMouseVector;
            dir.Normalize();
            Actor.Forward = dir;
        }
    }
}
