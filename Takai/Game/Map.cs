using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A single section of the map, used for spacial calculations
    /// </summary>
    public class MapSector
    {
        public List<Entity> entities = new List<Entity>(); //needs to be able to handle addition/removal during traversal (todo: possibly look into other types)
    }

    /// <summary>
    /// A 2D tile based renderable map that uses a grid for spacial subdivision
    /// </summary>
    public class Map
    {
        public Texture2D TilesImage { get; set; }
        public bool[,] TilesMask { get; set; }

        public int TileSize
        {
            get { return tileSize; }
            set
            {
                tileSize = value;
                tilesPerRow = (TilesImage != null ? (TilesImage.Width / value) : 0);
                sectorPixelSize = sectionSize * value;
            }
        }
        private int tileSize;
        private int tilesPerRow;

        public int Width; //Width in tiles
        public int Height; //Height in tiles

        public const int sectionSize = 4; //The number of tiles in a map section
        private int sectorPixelSize = 0;

        /// <summary>
        /// All of the tiles in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        public ushort[,] Tiles { get; set; }
        /// <summary>
        /// All of the sectors in the map
        /// Typically inactive entities are stored here
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        public MapSector[,] Sectors { get; protected set; }
        /// <summary>
        /// The set of entities that are being updated
        /// Entities not in this list are not updated
        /// Automatically updated as the view changes
        /// </summary>
        public List<Entity> ActiveSet { get; protected set; } = new List<Entity>(128);
        /// <summary>
        /// The last viewport used in Update()
        /// Used for updating the active set
        /// Stores sector coordinates
        /// </summary>
        protected Rectangle lastView = Rectangle.Empty;

        public Takai.Graphics.BitmapFont fnt;

        /// <summary>
        /// Build the tiles mask
        /// </summary>
        /// <param name="Texture">The source texture to use</param>
        /// <param name="UseAlpha">Use alpha instead of color value</param>
        /// <remarks>Values with luma/alpha < 0.5 are off and all others are on</remarks>
        public void BuildMask(Texture2D Texture, bool UseAlpha = false)
        {
            Color[] pixels = new Color[Texture.Width * Texture.Height];
            Texture.GetData<Color>(pixels);

            TilesMask = new bool[Texture.Height, Texture.Width];
            for (var i = 0; i < pixels.Length; i++)
            {
                if (UseAlpha)
                    TilesMask[i / Texture.Width, i % Texture.Width] = pixels[i].A > 127;
                else
                    TilesMask[i / Texture.Width, i % Texture.Width] = (pixels[i].R + pixels[i].G + pixels[i].B) / 3 > 127;
            }
        }

        /// <summary>
        /// Create the spacial subdivisions for the map
        /// </summary>
        public void BuildSectors()
        {
            if (Tiles != null)
            {
                //round up
                var width = 1 + ((Tiles.GetLength(1) - 1) / sectionSize);
                var height = 1 + ((Tiles.GetLength(0) - 1) / sectionSize);

                Sectors = new MapSector[height, width];

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                        Sectors[y, x] = new MapSector();
                }
            }
        }

        /// <summary>
        /// Spawn an entity in the map
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to spawn</typeparam>
        /// <param name="Position">Where to spawn the entity</param>
        /// <param name="Direction">The direction the entity should face</param>
        /// <param name="LoadEntity">Load the entity, defaults to true</param>
        /// <returns>The entity spawned</returns>
        public TEntity SpawnEntity<TEntity>(Vector2 Position, Vector2 Direction, Vector2 Velocity, bool LoadEntity = true) where TEntity : Entity, new()
        {
            var ent = new TEntity();
            ent.Map = this;

            ent.Position = Position;
            ent.Direction = Direction;
            ent.Velocity = Velocity;

            //will be removed in next update if out of visible range
            //only inactive entities are placed in sectors
            ActiveSet.Add(ent);

            if (LoadEntity)
                ent.Load();
            
            return ent;
        }

        /// <summary>
        /// Remove an entity from the map
        /// </summary>
        /// <param name="Ent">The entity to remove</param>
        public void DestroyEntity(Entity Ent)
        {
            if (Ent.Section != null)
            {
                Ent.Section.entities.Remove(Ent);
                Ent.Section = null;
            }
            else
                ActiveSet.Remove(Ent);
            Ent.Unload();
        }

        /// <summary>
        /// Move an entity to a new position in the map
        /// All movement in the map should use this function to ensure correct spacial representation
        /// </summary>
        /// <param name="Ent">The entity to move</param>
        /// <param name="Position">The new position to move the entity to</param>
        public void MoveEntity(Entity Ent, Vector2 Position)
        {
            Ent.Position = Position;

            if (Ent.Section != null)
                Ent.Section.entities.Remove(Ent);

            var cell = (Position / sectorPixelSize).ToPoint();
            if (cell.Y < 0 || cell.Y >= Sectors.GetLength(0) || cell.X < 0 || cell.X >= Sectors.GetLength(1))
                Ent.Section = null;
            else
            {
                Sectors[cell.Y, cell.X].entities.Add(Ent);
                Ent.Section = Sectors[cell.Y, cell.X];
            }
        }

        /// <summary>
        /// Find all entities within a certain radius
        /// </summary>
        /// <param name="Position">The origin search point</param>
        /// <param name="Radius">The maximum search radius</param>
        public List<Entity> GetNearbyEntities(Vector2 Position, float Radius)
        {
            var radiusSq = Radius * Radius;
            var vr = new Vector2(Radius);

            var mapSz = new Vector2(Width, Height);
            var start = Vector2.Clamp((Position - vr) / sectorPixelSize, Vector2.Zero, mapSz).ToPoint();
            var end = (Vector2.Clamp((Position + vr) / sectorPixelSize, Vector2.Zero, mapSz) + Vector2.One).ToPoint();

            List<Entity> ents = new List<Entity>();
            for (int y = start.Y; y < end.Y; y++)
            {
                for (int x = start.X; x < end.X; x++)
                {
                    foreach (var ent in Sectors[y, x].entities)
                    {
                        if (Vector2.DistanceSquared(ent.Position, Position) < radiusSq)
                            ents.Add(ent);
                    }
                }
            }

            return ents;
        }

        /// <summary>
        /// Check if a point is 'inside' the map
        /// </summary>
        /// <param name="Point">The point to test</param>
        /// <returns>True if the point is in a navicable area</returns>
        public bool IsPointInMap(Vector2 Point)
        {
            return (Point.X >= 0 && Point.X < (Width * TileSize) && Point.Y >= 0 && Point.Y < (Height * TileSize));
        }

        /// <summary>
        /// Update the map state
        /// Updates the active set and then the contents of the active set
        /// </summary>
        /// <param name="Time"></param>
        /// <param name="Camera"></param>
        /// <param name="Viewport"></param>
        public void Update(GameTime Time, Vector2 Camera, Rectangle Viewport)
        {
            var half = Camera - (new Vector2(Viewport.Width, Viewport.Height) / 2);

            var startX = (int)half.X / sectorPixelSize;
            var startY = (int)half.Y / sectorPixelSize;

            var width = 1 + ((Viewport.Width - 1) / sectorPixelSize);
            var height = 1 + ((Viewport.Height - 1) / sectorPixelSize);

            var activeRect = new Rectangle(startX - 1, startY - 1, width + 2, height + 2);
            var mapRect = new Rectangle(0, 0, Width * tileSize, Height * tileSize);

            //update active entities
            for (int i = 0; i < ActiveSet.Count; i++)
            {
                var ent = ActiveSet[i];
                if (!ent.AlwaysActive && !activeRect.Contains(ent.Position / sectorPixelSize))
                {
                    //add to sector (todo: handle out of bounds)
                    Sectors[(int)ent.Position.Y / sectorPixelSize, (int)ent.Position.X / sectorPixelSize].entities.Add(ent);

                    //remove from active set (swap with last)
                    ActiveSet[i] = ActiveSet[ActiveSet.Count - 1];
                    ActiveSet.RemoveAt(ActiveSet.Count - 1);
                    i--;
                }
                else
                {
                    ent.Think(Time);

                    var targetPos = ent.Position + ent.Velocity * (float)Time.ElapsedGameTime.TotalSeconds;
                    if (!mapRect.Contains(targetPos))
                    {
                        ent.Velocity = Vector2.Zero;
                        ent.OnMapCollision((targetPos / tileSize).ToPoint());
                    }
                    else
                        ent.Position = targetPos;
                }
            }

            //add new entities to active set
            for (var y = System.Math.Max(activeRect.Top, 0); y < System.Math.Min(Height / sectionSize, activeRect.Bottom); y++)
            {
                for (var x = System.Math.Max(activeRect.Left, 0); x < System.Math.Min(Width / sectionSize, activeRect.Right); x++)
                {
                    ActiveSet.AddRange(Sectors[y, x].entities);
                    Sectors[y, x].entities.Clear();
                }
            }
        }

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="Sbatch">The spritebatch to use</param>
        /// <param name="Camera">The center of the visible area</param>
        /// <param name="Viewport">Where on screen to draw</param>
        public void Draw(SpriteBatch Sbatch, Vector2 Camera, Rectangle Viewport)
        {
            var half = Camera - (new Vector2(Viewport.Width, Viewport.Height) / 2);

            var startX = (int)half.X / tileSize;
            var startY = (int)half.Y / tileSize;

            int endX = startX + 1 + ((Viewport.Width - 1) / tileSize);
            int endY = startY + 1 + ((Viewport.Height - 1) / tileSize);

            startX = System.Math.Max(startX, 0);
            startY = System.Math.Max(startY, 0);

            endX = System.Math.Min(endX + 1, Width);
            endY = System.Math.Min(endY + 1, Height);
            
            for (var y = startY; y < endY; y++)
            {
                var vy = Viewport.Y + (y * tileSize) - (int)half.Y;

                for (var x = startX; x < endX; x++)
                {
                    var tile = Tiles[y, x];

                    var vx = Viewport.X + (x * tileSize) - (int)half.X;

                    Sbatch.Draw
                    (
                        TilesImage,
                        new Rectangle(vx, vy, tileSize, tileSize),
                        new Rectangle((tile % tilesPerRow) * tileSize, (tile / tilesPerRow) * tileSize, tileSize, tileSize),
                        Color.White
                    );
                }
            }

            var view = new Vector2(Viewport.X, Viewport.Y) - half;
            foreach (var ent in ActiveSet)
            {
                if (ent.Sprite != null)
                {
                    var angle = (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                    ent.Sprite.Draw(Sbatch, view + ent.Position, angle);
                }
            }

            fnt.Draw(Sbatch, ActiveSet.Count.ToString(), new Vector2(100), Color.CornflowerBlue);
        }
    }
}
