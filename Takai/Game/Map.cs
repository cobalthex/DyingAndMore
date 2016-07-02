using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    /// <summary>
    /// A single type of blob
    /// This struct defines the graphics for the blob and physical properties that can affect the game
    /// </summary>
    public class BlobType
    {
        /// <summary>
        /// The texture to render the blob with
        /// </summary>
        public Texture2D Texture { get; set; }
        /// <summary>
        /// The radius of an individual blob
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// Drag affects both how quickly the blob stops moving and how much resistance there is to entities moving through it
        /// </summary>
        public float Drag { get; set; }
    }

    /// <summary>
    /// A single blob, rendered as a metablob
    /// Blobs can have physics per their blob type
    /// Blobs can be spawned with a velocity which is decreased by their drag over time. Once the velocity reaches zero, the blob is considered inactive (permanently)
    /// </summary>
    public struct Blob
    {
        public BlobType type;
        public Vector2 position;
        public Vector2 velocity;
    }

    /// <summary>
    /// A single sector of the map, used for spacial calculations
    /// Sectors typically contain inactive entities that are not visible as well as dummy objects (inactive blobs, decals). 
    /// </summary>
    public class MapSector
    {
        public List<Entity> entities = new List<Entity>();
        public List<Blob> blobs = new List<Blob>();
    }
    
    /// <summary>
    /// A 2D tile based renderable map that uses a grid for spacial subdivision
    /// </summary>
    public partial class Map
    {        
        public bool[] TilesMask { get; set; }

        public int TileSize
        {
            get { return tileSize; }
            set
            {
                tileSize = value;
                tilesPerRow = (TilesImage != null ? (TilesImage.Width / value) : 0);
                sectorPixelSize = sectorSize * value;
            }
        }
        private int tileSize;
        private int tilesPerRow;
        private int diagonalLengthSq; //used for collision detection (raycast if speedSq > this)

        /// <summary>
        /// The horizontal size of the map in tiles
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// The vertical size of the map in tiles
        /// </summary>
        public int Height { get; set; }

        public const int sectorSize = 4; //The number of tiles in a map sector
        private int sectorPixelSize = 0;

        /// <summary>
        /// All of the tiles in the map
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        public short[,] Tiles { get; set; }
        /// <summary>
        /// All of the sectors in the map
        /// Typically inactive entities are stored here
        /// </summary>
        /// <remarks>Size is Height,Width</remarks>
        public MapSector[,] Sectors { get; protected set; }
        protected Point NumSectors { get; private set; }

        /// <summary>
        /// The set of live entities that are being updated.
        /// Entities not in this list are not updated
        /// Automatically updated as the view changes
        /// </summary>
        public List<Entity> ActiveEnts { get; protected set; } = new List<Entity>(128);
        /// <summary>
        /// The list of live blobs. Once the blobs' velocity is zero, they are removed from this and not re-added
        /// </summary>
        public List<Blob> ActiveBlobs { get; protected set; } = new List<Blob>(32);

        /// <summary>
        /// Load tile data from a CSV
        /// </summary>
        /// <remarks>Assumes csv is well formed (all rows are the same length)</remarks>
        public static Map FromCsv(GraphicsDevice GDevice, string File, bool BuildSectors = true)
        {
            Map m = new Map(GDevice);

            var lines = System.IO.File.ReadAllLines(File);
            for (int y = 0; y < lines.Length; y++)
            {
                if (string.IsNullOrWhiteSpace(lines[y]))
                    continue;

                var split = lines[y].Split(',');

                if (m.Tiles == null)
                {
                    m.Width = split.Length;
                    m.Height = lines.Length;
                    m.Tiles = new short[lines.Length, split.Length];
                }

                for (int x = 0; x < split.Length; x++)
                    m.Tiles[y, x] = short.Parse(split[x]);
            }

            if (BuildSectors)
                m.BuildSectors();

            return m;
        }

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

            TilesMask = new bool[Texture.Height * Texture.Width];
            for (var i = 0; i < pixels.Length; i++)
            {
                if (UseAlpha)
                    TilesMask[i] = pixels[i].A > 127;
                else
                    TilesMask[i] = (pixels[i].R + pixels[i].G + pixels[i].B) / 3 > 127;
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
                var width = 1 + ((Tiles.GetLength(1) - 1) / sectorSize);
                var height = 1 + ((Tiles.GetLength(0) - 1) / sectorSize);

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
            ActiveEnts.Add(ent);

            if (LoadEntity)
                ent.Load();
            
            return ent;
        }

        /// <summary>
        /// Spawn a single blob onto the map
        /// </summary>
        /// <param name="Position">The position of the blob</param>
        /// <param name="Velocity">The blob's initial velocity</param>
        /// <param name="Type">The blob's type</param>
        public void SpawnBlob(Vector2 Position, Vector2 Velocity, BlobType Type)
        {
            //todo: don't spawn blobs outside the map (position + radius)

            if (Velocity == Vector2.Zero)
            {
                
                var sector = Vector2.Clamp(Position / sectorPixelSize, Vector2.Zero, new Vector2(Sectors.GetLength(1) - 1, Sectors.GetLength(0) - 1)).ToPoint();
                Sectors[sector.Y, sector.X].blobs.Add(new Blob { position = Position, velocity = Velocity, type = Type });
            }
            else
                ActiveBlobs.Add(new Blob { position = Position, velocity = Velocity, type = Type });
        }

        /// <summary>
        /// Remove an entity from the map
        /// </summary>
        /// <param name="Ent">The entity to remove</param>
        /// <remarks>Will be marked for deletion to be deleted in the next Update cycle</remarks>
        public void Destroy(Entity Ent)
        {
            Ent.Map = null;
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

            var deltaT = (float)Time.ElapsedGameTime.TotalSeconds;

            //update active blobs
            for (int i = 0; i < ActiveBlobs.Count; i++)
            {
                var blob = ActiveBlobs[i];
                var deltaV = blob.velocity * deltaT;
                blob.position += deltaV;
                blob.velocity -= deltaV * blob.type.Drag;
                
                //todo: maybe add collision detection for better fluid simulation (drag increases when colliding)

                if (System.Math.Abs(blob.velocity.X) < 1 && System.Math.Abs(blob.velocity.Y) < 1)
                {
                    SpawnBlob(blob.position, Vector2.Zero, blob.type); //this will move the blob to the static area of the map
                    ActiveBlobs[i] = ActiveBlobs[ActiveBlobs.Count - 1];
                    ActiveBlobs.RemoveAt(ActiveBlobs.Count - 1);
                    i--;
                }
                else
                    ActiveBlobs[i] = blob;
            }

            //update active entities
            for (int i = 0; i < ActiveEnts.Count; i++)
            {
                var ent = ActiveEnts[i];
                if (!ent.AlwaysActive && !activeRect.Contains(ent.Position / sectorPixelSize))
                {
                    //ents outside the map are deleted
                    if (mapRect.Contains((ent.Position / tileSize).ToPoint()))
                        Sectors[(int)ent.Position.Y / sectorPixelSize, (int)ent.Position.X / sectorPixelSize].entities.Add(ent);
                    else
                    {
                        ent.Map = null;
                        ent.Unload();
                        continue;
                    }

                    //remove from active set (swap with last)
                    ActiveEnts[i] = ActiveEnts[ActiveEnts.Count - 1];
                    ActiveEnts.RemoveAt(ActiveEnts.Count - 1);
                    i--;
                }
                else
                {
                    if (!ent.IsEnabled)
                        continue;

                    ent.Think(Time);

                    var targetPos = ent.Position + ent.Velocity * deltaT;
                    var targetCell = (targetPos / tileSize).ToPoint();
                    var cellPos = new Point((int)targetPos.X % tileSize, (int)targetPos.Y % tileSize);

                    short tile;
                    if (!mapRect.Contains(targetPos) || (tile = Tiles[targetCell.Y, targetCell.X]) < 0)
                    // || !TilesMask[(tile / tilesPerRow) + cellPos.Y, (tile % tileSize) + cellPos.X])
                    {
                        ent.OnMapCollision(targetCell);

                        if (ent.IsPhysical)
                            ent.Velocity = Vector2.Zero;
                    }

                    else if (ent.Velocity != Vector2.Zero)
                    {
                        for (int j = 0; j < ActiveEnts.Count; j++)
                        {
                            if (i != j && Vector2.DistanceSquared(ent.Position, ActiveEnts[j].Position) < ent.RadiusSq + ActiveEnts[j].RadiusSq)
                                ent.OnEntityCollision(ActiveEnts[j]);
                        }
                    }
                    
                    ent.Position += ent.Velocity * deltaT;
                }
                
                if (ent.Map == null)
                {
                    if (ent.Sector != null)
                    {
                        ent.Sector.entities.Remove(ent);
                        ent.Sector = null;
                    }
                    else
                        ActiveEnts.Remove(ent);
                    ent.Unload();
                }
            }

            //add new entities to active set (will be updated next frame)
            for (var y = System.Math.Max(activeRect.Top, 0); y < System.Math.Min(Height / sectorSize, activeRect.Bottom); y++)
            {
                for (var x = System.Math.Max(activeRect.Left, 0); x < System.Math.Min(Width / sectorSize, activeRect.Right); x++)
                {
                    ActiveEnts.AddRange(Sectors[y, x].entities);
                    Sectors[y, x].entities.Clear();
                }
            }
        }
    }
}
