using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// The basic entity. All actors and objects inherit from this
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Generator for unique entity IDs
        /// </summary>
        internal uint EntIdCounter = 1;

        internal System.Collections.Generic.Dictionary<System.Type, Component> components = new System.Collections.Generic.Dictionary<System.Type, Component>();

        /// <summary>
        /// A unique ID for this entity
        /// </summary>
        public uint Id { get; internal set; }

        /// <summary>
        /// Determines if the entity is thinking (alive)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Direction { get; set; } = Vector2.UnitX; //should remain normalized
        public float FieldOfView { get; set; } = MathHelper.PiOver4 * 3;

        public MapSection Section { get; internal set; } = null;

        public Entity()
        {
            Id = EntIdCounter;
            EntIdCounter++;
        }
        
        /// <summary>
        /// Add a component
        /// </summary>
        /// <typeparam name="TComponent">The type of component to add</typeparam>
        /// <returns>The component added</returns>
        public TComponent AddComponent<TComponent>() where TComponent : Component, new()
        {
            if (!components.ContainsKey(typeof(TComponent)))
                components.Add(typeof(TComponent), new TComponent { Entity = this });
            return GetComponent<TComponent>();
        }
        /// <summary>
        /// Remove a component
        /// </summary>
        /// <typeparam name="TComponent">The type of component to remove</typeparam>
        /// <returns>True if the component was removed, false if not (for example, if the component did not exist)</returns>
        public bool RemoveComponent<TComponent>() where TComponent : Component
        {
            return components.Remove(typeof(TComponent));
        }
        /// <summary>
        /// Get a component
        /// </summary>
        /// <typeparam name="TComponent">The type of component to search for</typeparam>
        /// <returns>The component or null if not found</returns>
        public TComponent GetComponent<TComponent>() where TComponent : Component
        {
            Component tc = null;
            components.TryGetValue(typeof(TComponent), out tc);
            return (TComponent)tc;
        }

        /// <summary>
        /// Called when the entity is loaded. Loads all components
        /// </summary>
        public virtual void Load()
        {
            foreach (var behavior in components)
                behavior.Value.Load();
        }
        /// <summary>
        /// Called when the entity is unloaded. Unloads all components
        /// </summary>
        public virtual void Unload()
        {
            foreach (var behavior in components)
                behavior.Value.Unload();
        }

        /// <summary>
        /// The basic think function for this entity, called once a frame
        /// </summary>
        /// <param name="Time">Frame time</param>
        public virtual void Think(GameTime Time)
        {
            foreach (var behavior in components)
            {
                if (!behavior.Value.IsEnabled)
                    continue;

                behavior.Value.Think(Time);
            }
        }

        #region Helpers

        /// <summary>
        /// Is this entity facing a point
        /// </summary>
        /// <param name="Point">The point to check</param>
        /// <returns>True if this entity is facing Point</returns>
        public bool IsFacing(Vector2 Point)
        {
            var diff = Point - Position;
            diff.Normalize();

            var dot = Vector2.Dot(Direction, diff);

            return (dot > (1 - (FieldOfView / MathHelper.Pi)));
        }

        /// <summary>
        /// Is this entity behind another (The other entity cannot see this one)
        /// </summary>
        /// <param name="Ent">The entity to check</param>
        /// <returns>True if this entity is behind Ent</returns>
        public bool IsBehind(Entity Ent)
        {
            var diff = Ent.Position - Position;
            diff.Normalize();

            var dot = Vector2.Dot(diff, Ent.Direction);
            return (dot > (Ent.FieldOfView / MathHelper.Pi) - 1);
        }

        #endregion
    }
}
