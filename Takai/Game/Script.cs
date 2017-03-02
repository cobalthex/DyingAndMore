using System;

namespace Takai.Game
{
    /// <summary>
    /// A script that allows for custom behaviors in a game
    /// </summary>
    public abstract class Script
    {
        /// <summary>
        /// A unique name to refer to this script by
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The map this script is running on
        /// </summary>
        public Map Map { get; set; }

        /// <summary>
        /// An entity that this script is optionally attached to
        /// </summary>
        public Entity Entity { get; set; }

        public Script(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Run one step of the script
        /// </summary>
        /// <param name="DeltaTime">How much tim ehas elapsed since the last update (in map time)</param>
        public abstract void Step(TimeSpan DeltaTime);
    }
}
