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
        [Data.NonSerialized]
        public Map Map { get; set; }
        
        public Script(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Run one step of the script
        /// </summary>
        /// <param name="deltaTime">The game's delta time</param>
        /// <param name="context">An optional entity that is passed in to provide context for various scripts (A trigger would pass the colliding entity in here)</param>
        public abstract void Step(TimeSpan deltaTime, Entity context = null);
    }
}
