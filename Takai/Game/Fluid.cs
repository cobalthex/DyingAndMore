using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai.Game
{
    /// <summary>
    /// A single type of Fluid
    /// This struct defines the graphics for the Fluid and physical properties that can affect the game
    /// </summary>
    public class FluidClass : Data.INamedClass<FluidInstance>
    {
        public string Name { get; set; }

        [Data.Serializer.Ignored]
        public string File { get; set; } = null;

        /// <summary>
        /// The texture to render the Fluid with
        /// </summary>
        public Texture2D Texture { get; set; }
        /// <summary>
        /// A reflection map, controlling reflection of entities, similar to a normal map
        /// Set to null or alpha = 0 for no reflection
        /// </summary>
        public Texture2D Reflection { get; set; }

        /// <summary>
        /// The scale of the textures. Does not affect radius
        /// </summary>
        public float Scale { get; set; } = 1;

        /// <summary>
        /// The radius of an individual Fluid
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// Drag affects both how quickly the Fluid stops moving and how much resistance there is to entities moving through it
        /// </summary>
        public float Drag { get; set; }

        /// <summary>
        /// An effect played at the location of any entity currently colliding with this fluid
        /// </summary>
        public EffectsClass EntityCollisionEffect { get; set; }

        /// <summary>
        /// Does this fluid represent blood/guts/etc
        /// </summary>
        public bool ContainsGore { get; set; }

        //todo: magnetism/cohesion, combinations (n small bloods group into one medium blood, etc)

        public FluidInstance Instantiate()
        {
            return new FluidInstance()
            {
                Class = this
            };
        }
    }

    /// <summary>
    /// A single fluid, rendered as a meta-blob
    /// Fluids can have physics per their fluid type
    /// Fluids can be spawned with a velocity which is decreased by their drag over time. Once the velocity reaches zero, the fluid is considered inactive (permanently)
    /// </summary>
    public struct FluidInstance : Data.IInstance<FluidClass>
    {
        public FluidClass Class { get; set; }
        public Vector2 position;
        public Vector2 velocity;
    }
}
