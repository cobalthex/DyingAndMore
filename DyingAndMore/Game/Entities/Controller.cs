namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// The base class for AI or player controllers
    /// </summary>
    abstract class Controller
    {
        [Takai.Data.Serializer.Ignored]
        public Actor actor;

        /// <summary>
        /// One frame of time to control the actor
        /// </summary>
        /// <param name="DeltaTime">How long since the last Think cycle</param>
        public abstract void Think(System.TimeSpan DeltaTime);
    }
}
