﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public interface ITriggerFilter
    {
        bool CanTrigger(EntityInstance entity);
    }

    /// <summary>
    /// A region that can trigger commands when an entity enters the trigger region
    /// </summary>
    public class Trigger
    {

        /// <summary>
        /// A name to identify this trigger
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The rectangular area of this trigger
        /// </summary>
        public Rectangle Region { get; set; } = new Rectangle(0, 0, 1, 1);

        /// <summary>
        /// Restrict who can activate this trigger (by default, anyone)
        /// </summary>
        public ITriggerFilter Filter { get; set; }

        /// <summary>
        /// Commands that are executed when an entity enters this trigger region.
        /// Any <see cref="EntityCommand"/> that have the target as null will trigger against the entity entering
        /// </summary>
        public List<ICommand> OnEnterCommands { get; set; }

        /// <summary>
        /// Play effects when an entity enters the trigger region
        /// </summary>
        public EffectsClass OnEnterEffects { get; set; }

        /// <summary>
        /// How many times this trigger can be used, 0 for infinitex
        /// </summary>
        public int MaxUses { get; set; } = 0;

        /// <summary>
        /// How many times this trigger has been entered (by filtered entities)
        /// </summary>
        public int UseCount { get; set; } = 0;

        /// <summary>
        /// Entities currently inside this trigger region
        /// </summary>
        internal HashSet<EntityInstance> ContainedEntities { get; set; } = new HashSet<EntityInstance>(); //todo: coroutine?

        //todo: on map create, place any entities into triggers without(?) calling OnEnter

        /// <summary>
        /// Try and enter this trigger region. Trigger must be enabled. Does not check entity bounds
        /// </summary>
        /// <param name="entity">The entity to enter the trigger</param>
        /// <returns>True if the entity entered the trigger region</returns>
        public bool TryEnter(EntityInstance entity)
        {
            if ((MaxUses > 0 && UseCount >= MaxUses) ||
                (Filter != null && !Filter.CanTrigger(entity)) ||
                !ContainedEntities.Add(entity))
                return false;

            ++UseCount;

            if (OnEnterCommands != null)
            {
                foreach (var command in OnEnterCommands)
                {
                    if (command is EntityCommand ec && ec.Target == null)
                        ec.Invoke(entity);
                    else
                        command.Invoke(entity.Map);
                }
            }

            if (OnEnterEffects != null && entity.Map != null)
            {
                var fx = OnEnterEffects.Instantiate(null, entity);
                entity.Map.Spawn(fx);
            }

            System.Diagnostics.Debug.WriteLine($"{entity} entered trigger {Name}");
            return true;
        }

        /// <summary>
        /// Try and leave a trigger. Does not check entity bounds
        /// </summary>
        /// <param name="entity">The entity that is leaving</param>
        /// <returns>True if the entity was in the trigger</returns>
        public bool TryExit(EntityInstance entity)
        {
            if (ContainedEntities.Remove(entity))
            {
                System.Diagnostics.Debug.WriteLine($"{entity} left trigger {Name}");
                return true;
            }
            return false;
            //todo: on exit
        }

        /// <summary>
        /// Does this trigger currently contain the specified entity
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>True if the entity is colliding with this trigger region</returns>
        public bool Contains(EntityInstance entity)
        {
            return ContainedEntities.Contains(entity);
        }
    }
}
