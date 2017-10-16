namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// The base class for AI or player controllers
    /// </summary>
    abstract class Controller : System.ICloneable
    {
        [Takai.Data.Serializer.Ignored]
        public virtual ActorInstance Actor { get; set; }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// One frame of time to control the actor
        /// </summary>
        /// <param name="deltaTime">How long since the last Think cycle</param>
        public abstract void Think(System.TimeSpan deltaTime);

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
