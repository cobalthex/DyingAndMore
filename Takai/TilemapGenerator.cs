using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Takai
{
    enum Direction : short
    {
        Top,
        Left,
        Right,
        Bottom,
    }

    struct Edge
    {
        public int firstFilled;
        public int lastFilled;
        public int totalFilled;

        public override string ToString()
        {
            return $"({totalFilled})[{firstFilled} {lastFilled}]";
        }
    }

    public struct EdgeSimilarity
    {
        public List<short> top;
        public List<short> left;
        public List<short> right;
        public List<short> bottom;
    }

    /// <summary>
    /// Generate a tile-based map
    /// </summary>
    public class TilemapGenerator
    {
        public const short TILE_ANY = short.MinValue;
        public const short TILE_CLEAR = -1;
        public const short TILE_NONE = -2;
        public const short TILE_ERROR = -3;

        static readonly Point[] Directions =
        {
            new Point(0, -1), //up
            new Point(-1, 0), //left
            new Point(1, 0), //right
            new Point(0, 1) //down
        };

        public Texture2D TilemapTexture
        {
            get => _tilemapTexture;
            set
            {
                if (_tilemapTexture == value)
                    return;

                _tilemapTexture = value;
                if (_tilemapTexture != null)
                    PreSolve();
            }
        }
        private Texture2D _tilemapTexture;

        public Point TileSize
        {
            get => _tileSize;
            set
            {
                if (_tileSize == value)
                    return;

                _tileSize = value;
                if (_tilemapTexture != null && _tileSize != default)
                    PreSolve();
            }
        }
        private Point _tileSize;

        /// <summary>
        /// Tile adjacency graph. Each adjacency entry stores a list per direction of which tiles have compatible edges.
        /// All values shifted +1, index 0 is tile -1 (null tile)
        /// </summary>
        public EdgeSimilarity[] Adjacencies { get; private set; }

        public float SimilarityCutoff { get; set; } = 0.9f;

        Color[] texturePixels;
        short tilesPerRow;
        short totalTiles;

        public TilemapGenerator() { }
        public TilemapGenerator(Game.Tileset tileset)
        {
            TileSize = new Point(tileset.size);
            TilemapTexture = tileset.texture;
        }

        void PreSolve()
        {
            if (TilemapTexture.Width < TileSize.X || TilemapTexture.Height < TileSize.Y)
            {
                tilesPerRow = 0;
                totalTiles = 0;
                texturePixels = null;
                Adjacencies = null;
            }

            texturePixels = Util.EnsureSize(texturePixels, TilemapTexture.Width * TilemapTexture.Height);
            TilemapTexture.GetData(texturePixels);

            tilesPerRow = (short)(TilemapTexture.Width / TileSize.X);
            short rows = (short)(TilemapTexture.Height / TileSize.Y);
            totalTiles = (short)(rows * tilesPerRow); //no partial tiles

            //determine edge 'shape' for four edges for use in determining available adjacencies
            var edgeValues = new Edge[totalTiles * Directions.Length];
            for (int i = 0; i < totalTiles; ++i)
            {
                var tx = (i % tilesPerRow) * TileSize.X;
                var ty = (i / tilesPerRow) * TileSize.Y;

                var ei = i * 4;
                //top, left, right, bottom
                edgeValues[ei + 0] = EvaluateHorizontalEdge(ty, tx, tx + TileSize.X);
                edgeValues[ei + 1] = EvaluateVerticalEdge(tx, ty, ty + TileSize.Y);
                edgeValues[ei + 2] = EvaluateVerticalEdge(tx + TileSize.X - 1, ty, ty + TileSize.Y);
                edgeValues[ei + 3] = EvaluateHorizontalEdge(ty + TileSize.Y - 1, tx, tx + TileSize.X);
            }

            using (var file = new System.IO.StreamWriter("edges.txt"))
            {
                for (int i = 0; i < edgeValues.Length; i += 4)
                    file.WriteLine($"{(i / 4)}: ↑{edgeValues[i]} ←{edgeValues[i + 1]} →{edgeValues[i + 2]} ↓{edgeValues[i + 3]}");
            }

            //index 0 is null tile, all others shifted +1
            Adjacencies = new EdgeSimilarity[totalTiles + 1];
            for (int i = 0; i < Adjacencies.Length; ++i)
            {
                //todo: allow for similarity sorting
                Adjacencies[i].top = new List<short>();
                Adjacencies[i].left = new List<short>();
                Adjacencies[i].right = new List<short>();
                Adjacencies[i].bottom = new List<short>();
            }

            bool AreSimilar(Edge a, Edge b) // todo: make customizable
            {
                return (
                    Math.Abs(a.totalFilled - b.totalFilled) < 5 &&
                    Math.Abs(a.firstFilled - b.firstFilled) < 3 &&
                    Math.Abs(a.lastFilled - b.lastFilled) < 3
                );
            }

            for (short i = 1; i < totalTiles + 1; ++i) //hacky
            {
                var ei = (i - 1) * 4;

                //compare against null tile
                if (edgeValues[ei + 0].totalFilled == 0)
                {
                    Adjacencies[i].top.Add(0);
                    Adjacencies[0].bottom.Add(i);
                }
                if (edgeValues[ei + 1].totalFilled == 0)
                {
                    Adjacencies[i].left.Add(0);
                    Adjacencies[0].right.Add(i);
                }
                if (edgeValues[ei + 2].totalFilled == 0)
                {
                    Adjacencies[i].right.Add(0);
                    Adjacencies[0].left.Add(i);
                }
                if (edgeValues[ei + 3].totalFilled == 0)
                {
                    Adjacencies[i].bottom.Add(0);
                    Adjacencies[0].top.Add(i);
                }

                var right = edgeValues[ei + 2];
                var bottom = edgeValues[ei + 3];

                //self compares allowed
                for (short j = 1; j < totalTiles + 1; ++j)
                {
                    var ej = (j - 1) * 4;

                    if (AreSimilar(bottom, edgeValues[ej + 0])) //compare i.bottom with j.top
                    {
                        Adjacencies[i].bottom.Add(j);
                        Adjacencies[j].top.Add(i);
                    }

                    if (AreSimilar(right, edgeValues[ej + 1])) //compare i.right with j.left
                    {
                        Adjacencies[i].right.Add(j);
                        Adjacencies[j].left.Add(i);
                    }
                }
            }

            // todo: outputDebugInfo flag
            using (var file = new System.IO.StreamWriter("similarities.txt"))
            {
                for (int i = 0; i < Adjacencies.Length; ++i)
                {
                    file.WriteLine(i + ":");

                    file.Write("\t↑ ");
                    foreach (var adj in Adjacencies[i].top)
                        file.Write($" {adj}");
                    file.WriteLine();

                    file.Write("\t← ");
                    foreach (var adj in Adjacencies[i].left)
                        file.Write($" {adj}");
                    file.WriteLine();

                    file.Write("\t→ ");
                    foreach (var adj in Adjacencies[i].right)
                        file.Write($" {adj}");
                    file.WriteLine();

                    file.Write("\t↓ ");
                    foreach (var adj in Adjacencies[i].bottom)
                        file.Write($" {adj}");
                    file.WriteLine();
                }
            }
        }

        Edge EvaluateVerticalEdge(int x, int top, int bottom, byte alphaCutoff = 127)
        {
            var edge = new Edge 
            {
                firstFilled = bottom,
                lastFilled = top,
                totalFilled = 0 
            };

            for (int y = top; y < bottom; ++y)
            {
                if (texturePixels[y * TilemapTexture.Width + x].A >= alphaCutoff)
                {
                    ++edge.totalFilled;
                    edge.firstFilled = Math.Min(edge.firstFilled, y);
                    edge.lastFilled = Math.Max(edge.lastFilled, y);
                }
            }
            edge.firstFilled -= top;
            edge.lastFilled -= top;
            return edge;
        }
        Edge EvaluateHorizontalEdge(int y, int left, int right, byte alphaCutoff = 127)
        {
            var edge = new Edge
            {
                firstFilled = right,
                lastFilled = left,
                totalFilled = 0
            };

            for (int x = left; x < right; ++x)
            {
                if (texturePixels[y * TilemapTexture.Width + x].A >= alphaCutoff)
                {
                    ++edge.totalFilled;
                    edge.firstFilled = Math.Min(edge.firstFilled, x);
                    edge.lastFilled = Math.Max(edge.lastFilled, x);
                }
            }
            edge.firstFilled -= left;
            edge.lastFilled -= left;
            return edge;
        }

        public short[,] Solve(
            int rows,
            int columns,
            short topLeftValue = TILE_ANY,
            short? seed = null)
        {
            if (rows < 1 || columns < 1)
                throw new ArgumentException("rows nor columns can be < 1");

            if (topLeftValue == TILE_ANY)
            {
                topLeftValue = (short)(Math.Abs(seed.GetValueOrDefault()) % totalTiles);
            }

            var map = new short[rows, columns];
            for (int r = 0; r < rows; ++r)
                for (int c = 0; c < columns; ++c)
                    map[r, c] = TILE_NONE;
            map[0, 0] = topLeftValue;

            return Solve(map, false, seed);
        }

        public short[,] Solve(short[,] map, bool fillEmpty = true, short? seed = null)
        {
            var random = seed.HasValue ? new Random(seed.Value) : new Random();

            var rows = map.GetLength(0);
            var columns = map.GetLength(1);

            var available = new HashSet<short>();
            for (int r = 0; r < rows; ++r)
            {
                for (int c = 0; c < columns; ++c)
                {
                    if (map[r, c] > (fillEmpty ? -1 : TILE_NONE))
                        continue;

                    available.Clear();

                    if (c > 0)
                    {
                        var t = map[r, c - 1] + 1;
                        if (t >= -1)
                            available.UnionWith(Adjacencies[t].right);
                    }
                    if (r > 0)
                    {
                        var t = map[r - 1, c] + 1;
                        if (t >= -1)
                        {
                            if (available.Count > 0)
                                available.IntersectWith(Adjacencies[t].bottom);
                            else
                                available.UnionWith(Adjacencies[t].bottom);
                        }
                    }


                    // todo: these need to union if empty or intersect otherwise
                    //if (c > 0 && map[r, c - 1] >= -1)
                    //    available.UnionWith(Adjacencies[r * tilesPerRow + (c - 1)].right); //left->right
                    //if (c < columns - 1 && map[r, c + 1] >= -1)
                    //    available.IntersectWith(Adjacencies[r * tilesPerRow + (c + 1)].left); //right->left
                    //if (r > 0 && map[r - 1, c] >= -1)
                    //    available.IntersectWith(Adjacencies[(r - 1) * tilesPerRow].bottom); //up->bottom
                    //if (r < rows - 1 && map[r + 1, c] >= -1)
                    //    available.IntersectWith(Adjacencies[(r + 1) * tilesPerRow].top); //down->top

                    if (available.Count > 0)
                        map[r, c] = ChooseTile(available, random);
                    else
                        map[r, c] = TILE_ERROR;
                }
            }

            using (var file = new System.IO.StreamWriter("map.txt"))
            {
                for (int r = 0; r < rows; ++r)
                {
                    for (int c = 0; c < columns; ++c)
                        file.Write((c > 0 ? " " : "") + map[r, c]);
                    file.WriteLine();
                }       
            }

            return map;
        }

        short ChooseTile(HashSet<short> available, Random random)
        {
            short picked;
            while ((picked = System.Linq.Enumerable.ElementAt(available, random.Next(0, available.Count))) == 1);
            return (short)(picked - 1); //adj were all +1 for null tile
        }
    }
}
