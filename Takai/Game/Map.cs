using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A single section of the map, used for spacial calculations
    /// </summary>
    public class MapSection
    {
        public Dictionary<uint, Entity> entities;
    }

    public class Map
    {
        public Texture2D TilesImage { get; set; }
        public Texture2D MaskImage { get; set; }

        public int TileSize
        {
            get { return tileSize; }
            set
            {
                tileSize = value;
                tilesPerRow = (TilesImage != null ? (TilesImage.Width / value) : 0);
            }
        }
        private int tileSize;
        private int tilesPerRow;

        public int Width; //Width in tiles
        public int Height; //Height in tiles

        public const int sectionSize = 4; //The number of tiles in a map section
        public ushort[,] Tiles { get; set; }
        public MapSection[,] Sections { get; protected set; }

        /// <summary>
        /// Spawn an entity in themap
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to spawn</typeparam>
        /// <param name="Position">Where to spawn the entity</param>
        /// <returns>The entity spawned</returns>
        public TEntity Spawn<TEntity>(Vector2 Position, Vector2 Direction) where TEntity : Entity, new()
        {
            var ent = new TEntity();
            ent.Position = Position;
            ent.Direction = Direction;
            SetEntitySection(ent, Position);            

            return ent;
        }
        
        protected void SetEntitySection(Entity Ent, Vector2 Position)
        {
            if (Ent.Section != null)
                Ent.Section.entities.Remove(Ent.Id);

            var cell = (Position / (sectionSize * tileSize)).ToPoint();
            Sections[cell.Y, cell.X].entities[Ent.Id] = Ent;
            Ent.Section = Sections[cell.Y, cell.X];
        }

        public void Draw(SpriteBatch Sbatch, Vector2 Camera, Rectangle Viewport)
        {
            int width = Viewport.Right / tileSize;
            int height = Viewport.Right / tileSize;

            var startX = ((int)Camera.X - (Viewport.Width / 2)) / tileSize;
            var startY = ((int)Camera.Y - (Viewport.Height / 2)) / tileSize;

            startX = System.Math.Max(startX, 0);
            startY = System.Math.Max(startY, 0);

            width = System.Math.Min(width, Width);
            height = System.Math.Min(height, Height);

            for (var y = startY; y < height; y++)
            {
                for (var x = startX; x < width; x++)
                {
                    var tile = Tiles[y, x];

                    Sbatch.Draw
                    (
                        TilesImage,
                        new Rectangle(Viewport.X + (x * tileSize), Viewport.Y + (y * tileSize), tileSize, tileSize),
                        new Rectangle((tile % tilesPerRow) * tileSize, (tile / tilesPerRow) * tileSize, tileSize, tileSize),
                        Color.White
                    );
                }
            }

            startX /= sectionSize;
            startY /= sectionSize;
            width /= sectionSize;
            height /= sectionSize;

            for (var y = startY; y < height; y++)
            {
                for (var x = startX; x < width; x++)
                {
                    var section = Sections[y, x];
                    foreach (var ent in section.entities)
                    {
                        //draw all entities
                    }
                }
            }
        }
    }
}
