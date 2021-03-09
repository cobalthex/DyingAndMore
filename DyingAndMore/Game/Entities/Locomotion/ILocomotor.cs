using System;

namespace DyingAndMore.Game.Entities
{
    public enum LocomotionResult
    {
        Continue,
        Finished, // can be either success or failure
    }

    /// <summary>
    /// Provides an AI with the ability to move
    /// These are managed by tasks
    /// </summary>
    public interface ILocomotor
    {
        /// <summary>
        /// Move the AI
        /// </summary>
        /// <param name="deltaTime">time since last frame</param>
        /// <param name="actor">The actor to move</param>
        /// <returns>The state of this locomotor</returns>
        LocomotionResult Move(TimeSpan deltaTime, AIController ai);
    }


    ///// future locomotors
    //navigate behind entity (for assassination)
    //navigate to entity (powerup/weapon/etc)
    //teleport
    //wander
    //follow path
}
