using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// A region that can trigger commands when an entity enters the trigger region
    /// </summary>
    public class TriggerClass : IClass<TriggerInstance>
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
        /// How many times this trigger can be used, 0 for infinite
        /// </summary>
        public int MaxUses { get; set; } = 0;

        public List<Command> OnEnterCommands { get; set; }

        public TriggerInstance Instantiate()
        {
            return new TriggerInstance(this);
        }

        //todo: entity filters
    }

    public class TriggerInstance : IInstance<TriggerClass>
    {
        public TriggerClass Class { get; set; }

        /// <summary>
        /// How many times this trigger has been entered (by filtered entities)
        /// </summary>
        public int UseCount { get; set; } = 0;

        /// <summary>
        /// Entities currently inside this trigger region
        /// </summary>
        internal HashSet<EntityInstance> ContainedEntities { get; set; } = new HashSet<EntityInstance>(); //todo: coroutine?

        //todo: on map create, place any entities into triggers without(?) calling OnEnter

        public TriggerInstance() : this(null) { }
        public TriggerInstance(TriggerClass @class)
        {
            Class = @class;
        }

        //Check to see if an entity can enter this trigger and do so. Does not check bounds
        internal bool Enter(EntityInstance entity)
        {
            if ((Class.MaxUses > 0 && UseCount >= Class.MaxUses) || !ContainedEntities.Add(entity))
                return false;

            ++UseCount;

            if (Class.OnEnterCommands != null)
            {
                foreach (var command in Class.OnEnterCommands)
                    command.Invoke();
            }

            System.Diagnostics.Debug.WriteLine($"{entity} entered trigger {Class.Name}");
            return true;
        }
        internal bool Exit(EntityInstance entity)
        {
            return ContainedEntities.Remove(entity);
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
