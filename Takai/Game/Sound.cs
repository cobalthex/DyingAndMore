using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Takai.Game
{
    /// <summary>
    /// A sound in the game. Can be a sound effect, song, or other. Has information about captioning
    /// </summary>
    public class SoundClass : INamedClass<SoundInstance>
    {
        public string Name { get; set; }
        [Data.Serializer.Ignored]
        public string File { get; set; }

        /// <summary>
        /// The audio to play
        /// </summary>
        public Takai.Data.ISoundSource Source { get; set; }
        /// <summary>
        /// The dialogue-only subtitle
        /// </summary>
        public string Subtitle { get; set; }
        /// <summary>
        /// Any other sound cue/etc.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// How loud the sound is, affects spacial placement
        /// </summary>
        public float Gain { get; set; } = 1;

        public bool DestroyIfOwnerDies { get; set; } = true;

        public SoundInstance Instantiate()
        {
            return new SoundInstance(this);
        }

        public SoundInstance Instantiate(EntityInstance owner)
        {
            return new SoundInstance(this, owner);
        }
    }

    public struct SoundInstance : IInstance<SoundClass>
    {
        public SoundClass Class { get; set; }

        public SoundEffectInstance Instance { get; set; }

        /// <summary>
        /// The owner of this sound. This sound will follow its owner and may be destroyed if <see cref="SoundClass.DestroyIfOwnerDies"/> is true
        /// </summary>
        public EntityInstance Owner { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Forward { get; set; }
        public Vector2 Velocity { get; set; }

        //attach to entity for movement?

        public SoundInstance(SoundClass @class)
        {
            Class = @class;
            Owner = null;

            if (Class != null)
                Instance = Class.Source?.Instantiate();
            else
                Instance = null;

            if (Instance != null)
            {
                Instance.Volume = Class.Gain;
            }

            Position = Vector2.Zero;
            Forward = Vector2.UnitX;
            Velocity = Vector2.Zero;
        }

        public SoundInstance(SoundClass @class, EntityInstance instance)
        {
            Class = @class;
            Owner = instance;

            if (Class != null)
                Instance = Class.Source?.Instantiate();
            else
                Instance = null;

            if (Instance != null)
            {
                Instance.Volume = Class.Gain;
            }

            Position = instance.Position;
            Forward = instance.Forward;
            Velocity = instance.Velocity;
        }
    }
}
