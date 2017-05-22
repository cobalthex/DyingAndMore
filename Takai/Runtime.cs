using Xna = Microsoft.Xna.Framework;

namespace Takai
{
    public static class Runtime
    {
        public static Xna.Game Game { get; set; }

        public static Xna.Graphics.GraphicsDevice GraphicsDevice => Game.GraphicsDevice;
        public static bool HasFocus => Game.IsActive;

        public static bool IsExiting { get; set; }
    }
}
