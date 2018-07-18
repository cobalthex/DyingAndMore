using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Takai.Game;
using Takai.Input;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Available actions that the player can take
    /// Format: Verb adjective/noun
    /// </summary>
    public enum InputAction
    {
        None,
        MoveLineal,
        MoveLateral,
        //move up,down,left,right
        FirePrimaryWeapon,
        FireSecondaryWeapon,
    }

    public struct InputBinding
    {
        public InputAction action;
        public float magnitude;

        public InputBinding(InputAction action, float magnitude)
        {
            this.action = action;
            this.magnitude = magnitude;
        }
    }

    public class InputMap
    {
        public PlayerIndex Player { get; set; } = PlayerIndex.One;

        [Takai.Data.Serializer.Ignored]
        public Dictionary<Keys, InputBinding> Keys { get; set; }
        [Takai.Data.Serializer.Ignored]
        public Dictionary<Buttons, InputBinding> Gamepads { get; set; }
        [Takai.Data.Serializer.Ignored]
        public Dictionary<MouseButtons, InputBinding> Mice { get; set; }

        //thumbsticks, triggers, touchpad

        [Takai.Data.Serializer.Ignored]
        public Dictionary<InputAction, InputBinding> CurrentInputs { get; set; } = new Dictionary<InputAction, InputBinding>();

        public void Update()
        {
            if (Keys != null)
            {
                foreach (var key in InputState.GetPressedKeys())
                {
                    if (Keys.TryGetValue(key, out var binding))
                        CurrentInputs[binding.action] = binding;
                }
            }
            if (Gamepads != null)
            {
                foreach (var binding in Gamepads)
                {
                    if (InputState.IsButtonDown(binding.Key, Player))
                        CurrentInputs[binding.Value.action] = binding.Value;
                }
            }

            //mice
        }
    }

    class InputController : Controller
    {
        public InputMap Inputs { get; set; }

        public InputController()
        {
        }

        public override void Think(TimeSpan deltaTime)
        {
            //read input state

            //if ((GameInstance.Current != null && !GameInstance.Current.GameplaySettings.isPlayerInputEnabled) ||
            //    !Takai.Runtime.HasFocus)
            //    return;

            //var d = Vector2.Zero;
            //if (InputState.IsButtonDown(Keys.W))
            //    d -= Vector2.UnitY;
            //if (InputState.IsButtonDown(Keys.A))
            //    d -= Vector2.UnitX;
            //if (InputState.IsButtonDown(Keys.S))
            //    d += Vector2.UnitY;
            //if (InputState.IsButtonDown(Keys.D))
            //    d += Vector2.UnitX;

            //var sticks = InputState.Thumbsticks(Player);
            //if (sticks.Left != Vector2.Zero)
            //    d = Vector2.Normalize(sticks.Left) * new Vector2(1, -1);

            //if (sticks.Right != Vector2.Zero)
            //    Actor.TurnTowards(Vector2.Normalize(sticks.Right) * new Vector2(1, -1));

            //if (InputState.IsButtonDown(MouseButtons.Left) ||
            //    InputState.IsButtonDown(Buttons.RightTrigger, Player) ||
            //    InputState.IsButtonDown(Keys.Space))
            //    Actor.Weapon?.TryUse();

            //Actor.Accelerate(d);

            //todo
            if (InputState.MouseDelta() != Vector2.Zero)
            {
                var dir = InputState.PolarMouseVector;
                dir.Normalize();
                Actor.Forward = dir;
            }

            if (Inputs == null)
                return;

            InputBinding binding;
            if (Inputs.CurrentInputs.TryGetValue(InputAction.FirePrimaryWeapon, out binding))
                Actor.Weapon?.TryUse();

            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveLineal, out binding))
                Actor.Accelerate(Actor.Forward * binding.magnitude);

            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveLateral, out binding))
                Actor.Accelerate(Takai.Util.Ortho(Actor.Forward) * binding.magnitude);

            Inputs.CurrentInputs.Clear();
        }

        public override void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime)
        {
            if (collider is WeaponPickupInstance wpi)
                Actor.Weapon = wpi.ApplyTo(Actor.Weapon);
        }
    }
}
