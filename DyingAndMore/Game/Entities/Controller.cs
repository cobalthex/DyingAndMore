namespace DyingAndMore.Game.Entities
{
    /// <summary>
    /// The base class for AI or player controllers
    /// </summary>
    abstract class Controller
    {
        [Takai.Data.NonSerialized]
        public Actor actor;

        public abstract void Think(Microsoft.Xna.Framework.GameTime Time);
    }
}
