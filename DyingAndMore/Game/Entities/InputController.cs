using System;
using Microsoft.Xna.Framework;
using Takai.Game;

namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// Available actions that the player can take
    /// Format: Verb adjective/noun
    /// </summary>
    /// <remarks>__NAMES__ are for auto-generating configuration UI</remarks>
    public enum InputAction
    {
        None,

        __MOVEMENT__,

        MoveLineal, //move parallel to the actor's forward direction
        MoveLateral, //move perpendicular to the actor's forward direction
        MoveX, //relative to camera (1 is right, -1 is left)
        MoveY, //relative to camera (1 is down, -1 is up)

        FaceX,
        FaceY,

        __WEAPONS__,

        FirePrimaryWeapon,
        FireSecondaryWeapon,

        ZoomCamera
    }

    class InputController : Controller
    {
        /// <summary>
        /// The inputs to read from. Must be updated externally
        /// Cleared after reading
        /// </summary>
        public Takai.Input.InputMap<InputAction> Inputs { get; set; }

        public InputController()
        {
        }

        public override void Think(TimeSpan deltaTime)
        {
            if (Inputs == null)
                return;

            float magnitude;

            if (Inputs.CurrentInputs.TryGetValue(InputAction.FirePrimaryWeapon, out magnitude))
                Actor.Weapon?.TryUse();

            Vector2 moveDirection = new Vector2();

            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveX, out magnitude))
                moveDirection.X += magnitude;
            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveY, out magnitude))
                moveDirection.Y += magnitude;

            //read lineal movements
            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveLineal, out magnitude))
                moveDirection += (Actor.Forward * magnitude);
            if (Inputs.CurrentInputs.TryGetValue(InputAction.MoveLateral, out magnitude))
                moveDirection += (Takai.Util.Ortho(Actor.Forward) * magnitude);
            //transform by camera? (inverse)

            var faceDirection = new Vector2();

            if (Inputs.CurrentInputs.TryGetValue(InputAction.FaceX, out magnitude))
                faceDirection.X = magnitude;
            if (Inputs.CurrentInputs.TryGetValue(InputAction.FaceY, out magnitude))
                faceDirection.Y = magnitude;

            if (faceDirection != Vector2.Zero)
                Actor.TurnTowards(Vector2.Normalize(faceDirection));
            Actor.Accelerate(moveDirection);

            Inputs.CurrentInputs.Clear();
        }

        public override void OnEntityCollision(EntityInstance collider, CollisionManifold collision, TimeSpan deltaTime)
        {
            if (collider is WeaponPickupInstance wpi)
                Actor.Weapon = wpi.ApplyTo(Actor.Weapon);
        }
    }
}
