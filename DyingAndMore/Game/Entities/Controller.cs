namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// The base class for AI or player controllers
    /// </summary>
    public abstract class Controller
    {
        [Takai.Data.Serializer.Ignored]
        public virtual ActorInstance Actor { get;
            set; }

        public virtual Controller Clone()
        {
            var clone = (Controller)MemberwiseClone();
            clone.Actor = null;
            return clone;
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
