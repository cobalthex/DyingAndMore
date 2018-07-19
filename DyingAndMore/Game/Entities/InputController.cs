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

        _MOVEMENT_,

        MoveLineal, //forward/backward
        MoveLateral, //left/right (of forward)
        MoveX, //relative to camera (1 is right, -1 is left)
        MoveY, //relative to camera (1 is down, -1 is up)

        FaceX,
        FaceY,

        _WEAPONS_,

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

    public struct InputBinding2D
    {
        public InputBinding horiziontal;
        public InputBinding vertical;
    }

    public class InputMap
    {
        public PlayerIndex Player { get; set; } = PlayerIndex.One;

        [Takai.Data.Serializer.Ignored]
        public Dictionary<Keys, InputBinding> Keys { get; set; }
        [Takai.Data.Serializer.Ignored]
        public Dictionary<Buttons, InputBinding> GamepadButtons { get; set; }
        [Takai.Data.Serializer.Ignored]
        public Dictionary<MouseButtons, InputBinding> MouseButtons { get; set; }

        public InputBinding2D MousePosition { get; set; }

        public InputBinding2D GamepadLeftThumbstick { get; set; }
        public InputBinding2D GamepadRightThumbstick { get; set; }

        public InputBinding GamepadLeftTrigger { get; set; }
        public InputBinding GamepadRightTrigger { get; set; }

        //thumbsticks, triggers, touchpad

        [Takai.Data.Serializer.Ignored]
        public Dictionary<InputAction, InputBinding> CurrentInputs { get; set; } = new Dictionary<InputAction, InputBinding>();

        //todo: serialization

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
            if (GamepadButtons != null)
            {
                foreach (var binding in GamepadButtons)
                {
                    if (InputState.IsButtonDown(binding.Key, Player))
                        CurrentInputs[binding.Value.action] = binding.Value;
                }
            }
            if (MouseButtons != null)
            {
                foreach (var binding in MouseButtons)
                {
                    if (InputState.IsButtonDown(binding.Key))
                        CurrentInputs[binding.Value.action] = binding.Value;
                }
            }
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
            //todo, turn towards
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

            Vector2 moveDirection;
            //todo: needs ability to lerp between multiple directions
            //maybe isolate heading movement from cardinal movement and scale heading by forward later

            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveLineal, out binding))
                moveDirection = Actor.Forward * binding.magnitude;
            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveLateral, out binding))
                Actor.Accelerate(Takai.Util.Ortho(Actor.Forward) * binding.magnitude);

            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveX, out binding))
                Actor.Accelerate(Vector2.UnitX * binding.magnitude);
            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveY, out binding))
                Actor.Accelerate(Vector2.UnitY * binding.magnitude);

            Inputs.CurrentInputs.Clear();
        }

        public override void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime)
        {
            if (collider is WeaponPickupInstance wpi)
                Actor.Weapon = wpi.ApplyTo(Actor.Weapon);
        }
    }
}
