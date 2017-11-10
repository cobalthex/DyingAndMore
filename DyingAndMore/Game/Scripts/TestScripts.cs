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

        public NavigateToPlayer(EntityInstance entity)
            : base("NavigateToPlayer", entity) { }

        public override void OnSpawn()
        {
            player = GameInstance.Current.players?.FirstOrDefault();
        }

        List<Point> minimums = new List<Point>(8);
        public override void Step(TimeSpan deltaTime)
        {
            if (!Entity.IsAlive || player == null)
                return;

            var ppos = (player.Position / Map.Class.TileSize).ToPoint();
            var epos = (Entity.Position / Map.Class.TileSize).ToPoint();

            if (epos == ppos)
                return;

            var ph = Map.Class.PathInfo[ppos.Y, ppos.X].heuristic;

            var min = uint.MaxValue;
            minimums.Clear();
            foreach (var dir in MapClass.NavigationDirections)
            {
                var pos = epos + dir;
                if (!Map.Class.TileBounds.Contains(pos))
                    continue;
                var h = (uint)Math.Abs(Map.Class.PathInfo[pos.Y, pos.X].heuristic - ph);
                if (h < min)
                {
                    minimums.Clear();
                    minimums.Add(dir);
                    min = h;
                }
                else if (h == min)
                    minimums.Add(dir);
            }

            var next = minimums[RandomRange.RandomGenerator.Next(minimums.Count)];
            Entity.Velocity = next.ToVector2() * 100; //move force
        }
    }
}
