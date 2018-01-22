using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Takai.Game
{
    /// <summary>
    /// A sound in the game. Can be a sound effect, song, or other. Has information about captioning
    /// </summary>
    public class SoundClass : IObjectClass<SoundInstance>
    {
        //todo: use Xact? Audio.Cue/etc

        //todo: streaming audio support (probably will need to use DynamicSoundEffectInstance

        public string Name { get; set; }
        [Data.Serializer.Ignored]
        public string File { get; set; }

        /// <summary>
        /// The audio to play
        /// </summary>
        public SoundEffect Sound { get; set; }
        /// <summary>
        /// The dialogue-only subtitle
        /// </summary>
        public string Subtitle { get; set; }
        /// <summary>
        /// Any other sound cue/etc.
        /// </summary>
        public string Caption { get; set; }

        public float Gain { get; set; } = 1;

        public SoundInstance Instantiate()
        {
            return new SoundInstance(this);
        }
    }

    public struct SoundInstance : IObjectInstance<SoundClass>
    {
        public SoundClass Class { get; set; }

        public SoundEffectInstance Instance { get; set; }

        public Vector2 Position { get; set; }
        public Vector2 Forward { get; set; }
        public Vector2 Velocity { get; set; } //this does not update position

        public SoundInstance(SoundClass @class)
        {
            Class = @class;

            if (Class != null)
                Instance = Class.Sound?.CreateInstance();
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
    }
}
