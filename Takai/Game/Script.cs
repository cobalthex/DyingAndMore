﻿using System;

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
        [Data.Serializer.Ignored]
        public string Name { get; }

        /// <summary>
        /// The map this script is running on
        /// </summary>
        [Data.Serializer.Ignored]
        public Map Map { get; set; }

        /// <summary>
        /// When this script was started
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Called when the script is activated
        /// </summary>
        public virtual void OnSpawn() { }

        /// <summary>
        /// Called when the script is removed. Called before removal
        /// </summary>
        public virtual void OnDestroy() { }

        public Script(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Run one step of the script
        /// </summary>
        /// <param name="deltaTime">The game's delta time</param>
        public abstract void Step(TimeSpan deltaTime);
    }

    public abstract class EntityScript
    {
        /// <summary>
        /// The entity that this script is attached to
        /// </summary>
        public EntityInstance Entity { get; set; }
    }
}
