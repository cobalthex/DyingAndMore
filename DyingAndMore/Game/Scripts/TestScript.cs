using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Scripts
{
    class TestScript : Takai.Game.Script
    {
        public TimeSpan totalTime;

        public Takai.Game.Entity victim;
        public Takai.Game.PathRider path = new Takai.Game.PathRider();

        public TestScript()
            : base("Test")
        {
            path.Path = Editor.PathsEditorMode.paths[0];
        }

        public override void Step(TimeSpan deltaTime, Takai.Game.Entity context = null)
        {
            path.Move(100 * (float)deltaTime.TotalSeconds);
            victim.Position = path.Position;
        }
    }
}
