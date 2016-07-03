using Microsoft.Xna.Framework;

namespace Takai.Game
{
    /// <summary>
    /// The basic entity. All actors and objects inherit from this
    /// </summary>
    public class Entity
    {
        internal System.Collections.Generic.Dictionary<System.Type, Component> components = new System.Collections.Generic.Dictionary<System.Type, Component>();
        
        /// <summary>
        /// The current position of the entity
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// The (normalized) direction the entity is facing
        /// </summary>
        /// <remarks>This vector should always be normalized</remarks>
        public Vector2 Direction { get; set; } = Vector2.UnitX;
        /// <summary>
        /// The direction the entity is moving
        /// </summary>
        public Vector2 Velocity { get; set; } = Vector2.Zero;

        /// <summary>
        /// The map the entity is in, null if none
        /// </summary>
        public Map Map { get; internal set; } = null;
        /// <summary>
        /// The section the entity is located in in the map, null if none or if in the active set
        /// </summary>
        public MapSector Sector { get; internal set; } = null;

        /// <summary>
        /// Determines if the entity is thinking (alive)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Determines if this entity is always part of the active set and therefore always updated
        /// </summary>
        /// <remarks>This is typically used for things like projectiles</remarks>
        public bool AlwaysActive { get; set; } = false;
        
        /// <summary>
        /// The radius of this entity. Used mainly for broad-phase collision
        /// </summary>
        public float Radius
        {
            get
            {
                return radius;
            }
            set
            {
                radius = value;
                RadiusSq = value * value;
            }
        }
        public float RadiusSq { get; private set; }
        private float radius = 1;

        /// <summary>
        /// On collision, should this entity try to 'uncollide' with the entity
        /// </summary>
        /// <example>Triggers would have this set to false</example>
        public bool IsPhysical { get; set; } = true;

        /// <summary>
        /// Trace (raycast) queries should ignore this entity
        /// </summary>
        public bool IgnoreTrace { get; set; } = false;

        /// <summary>
        /// The sprite for this entity (may be null for things like triggers)
        /// Can be updated by components
        /// </summary>
        public Graphics.Graphic Sprite { get; set; } = null; //todo: maybe replace with simpler graphic

        /// <summary>
        /// Draw an outline around the sprite. If A is 0, ignored
        /// </summary>
        public Color OutlineColor { get; set; } = Color.Transparent;

        public Entity() { }
        
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

        /// <summary>
        /// Called when there is a collision between this entity and another entity
        /// </summary>
        /// <param name="Collider">The entity collided with</param>
        public virtual void OnEntityCollision(Entity Collider)
        {
        }

        /// <summary>
        /// Called when there is a collision between this entity and the map
        /// </summary>
        /// <param name="Tile">The tile on the map where the collision occurred</param>
        public virtual void OnMapCollision(Point Tile)
        {
        }

        /// <summary>
        /// Called when there is a collision between this entity and a blob
        /// </summary>
        /// <param name="Type">The type of blob collided with</param>
        public virtual void OnBlobCollision(BlobType Type)
        {

        }
    }
}
