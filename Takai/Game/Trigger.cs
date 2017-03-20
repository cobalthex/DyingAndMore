namespace Takai.Game
{
    /// <summary>
    /// A region that can trigger scripts when entered or exited
    /// </summary>
    public class Trigger
    {
        public Microsoft.Xna.Framework.Rectangle Region { get; set; }
        
        /// <summary>
        /// A script that is stepped once when an entity enters the trigger region
        /// </summary>
        public Script EnterScript { get; set; }
    }
}
