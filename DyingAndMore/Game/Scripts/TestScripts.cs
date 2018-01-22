using System;
using System.Linq;
using System.Collections.Generic;
using Takai.Game;
using Microsoft.Xna.Framework;

namespace DyingAndMore.Game.Scripts
{
    class NavigateToPlayer : EntityScript
    {
        EntityInstance player;


        public static readonly Point[] NavigationDirections =
        {
            new Point(-1, -1),
            new Point( 0, -1),
            new Point( 1, -1),
            new Point(-1,  0),
            new Point( 1,  0),
            new Point(-1,  1),
            new Point( 0,  1),
            new Point( 1,  1),
        };


        public NavigateToPlayer(EntityInstance entity)
            : base("NavigateToPlayer", entity) { }

        public override void OnSpawn()
        {
        }

        List<Point> minimums = new List<Point>(8);
        public override void Step(TimeSpan deltaTime)
        {
        }
    }
}
