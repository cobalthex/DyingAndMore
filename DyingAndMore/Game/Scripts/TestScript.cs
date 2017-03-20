using System;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Scripts
{
    class TestScript : Takai.Game.Script
    {
        public TimeSpan elapsedTime;
        public TimeSpan totalTime;

        public Takai.Game.Entity victim;

        public TestScript()
            : base("Test")
        {
        }

        public override void Step(TimeSpan deltaTime, Takai.Game.Entity context = null)
        {
            var amount = (float)(elapsedTime.TotalSeconds / totalTime.TotalSeconds);
            victim.Position = Editor.PathsEditorMode.paths[0].Evaluate(amount);

            elapsedTime += deltaTime;
        }
    }
}
