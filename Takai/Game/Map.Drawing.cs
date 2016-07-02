using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public partial class Map
    {
        protected struct DrawLine
        {
            public Color color;
            public Vector2 start, end;
        }

        protected GraphicsDevice GraphicsDevice { get; set; }
        protected SpriteBatch sbatch;
        protected DepthStencilState stencilWrite, stencilRead;
        protected AlphaTestEffect mapAlphaTest, metaAlphaTest;
        protected RenderTarget2D mainRenderTarget, postRenderTarget;
        protected Effect outlineEffect;

        public Texture2D TilesImage { get; set; }

        /// <summary>
        /// A set of lines to draw during the next frame
        /// </summary>
        protected List<DrawLine> debugLines = new List<DrawLine>(32);
        
        public Takai.Graphics.BitmapFont DebugFont { get; set; }
        public Texture2D decal;

        /// <summary>
        /// Create a new map
        /// </summary>
        /// <param name="GDevice">The graphics device to use for rendering the map</param>
        public Map(GraphicsDevice GDevice)
        {
            GraphicsDevice = GDevice;
            if (GDevice != null)
            {
                sbatch = new SpriteBatch(GDevice);

                stencilWrite = new DepthStencilState()
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.Always,
                    StencilPass = StencilOperation.Replace,
                    ReferenceStencil = 1,
                    DepthBufferEnable = false,
                };
                stencilRead = new DepthStencilState()
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.Equal,
                    StencilPass = StencilOperation.Keep,
                    ReferenceStencil = 1,
                    DepthBufferEnable = false,
                };

                var width = GDevice.PresentationParameters.BackBufferWidth;
                var height = GDevice.PresentationParameters.BackBufferHeight;

                var m = Matrix.CreateOrthographicOffCenter
                (
                    0,
                    width,
                    height,
                    0,
                    0, 1
                );

                mapAlphaTest = new AlphaTestEffect(GDevice) { Projection = m };
                mapAlphaTest.ReferenceAlpha = 1;

                metaAlphaTest = new AlphaTestEffect(GDevice) { Projection = m };
                metaAlphaTest.ReferenceAlpha = 128;

                mainRenderTarget = new RenderTarget2D(GDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
                postRenderTarget = new RenderTarget2D(GDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

                outlineEffect = Takai.AssetManager.Load<Effect>("Shaders/Outline.mgfx");
            }
        }

        /// <summary>
        /// Draw a line the next frame
        /// </summary>
        /// <param name="Start">The start position of the line</param>
        /// <param name="End">The end position of the line</param>
        /// <param name="Color">The color of the line</param>
        /// <remarks>Lines are world relative</remarks>
        public void DebugLine(Vector2 Start, Vector2 End, Color Color)
        {
            debugLines.Add(new DrawLine { color = Color, start = Start, end = End });
        }

        protected struct DebugProfilingInfo
        {
            public int visibleEnts;
            public int visibleInactiveBlobs;
            public int visibleActiveBlobs;
            public int visibleDecals;
        }

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="Sbatch">The spritebatch to use</param>
        /// <param name="Camera">The center of the visible area</param>
        /// <param name="Viewport">Where on screen to draw</param>
        /// <remarks>All rendering management handled internally</remarks>
        public void Draw(Vector2 Camera, Rectangle Viewport)
        {
            var lastRt = GraphicsDevice.GetRenderTargets();

            var outlined = new List<Entity>();

#if DEBUG
            DebugProfilingInfo dbgInfo = new DebugProfilingInfo();
#endif

            //map tiles

            GraphicsDevice.SetRenderTarget(mainRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack); //todo: replace with background

            sbatch.Begin(SpriteSortMode.Deferred, null, null, stencilWrite, null, mapAlphaTest);

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
                    if (tile < 0)
                        continue;

                    var vx = Viewport.X + (x * tileSize) - (int)half.X;

                    sbatch.Draw
                    (
                        TilesImage,
                        new Rectangle(vx, vy, tileSize, tileSize),
                        new Rectangle((tile % tilesPerRow) * tileSize, (tile / tilesPerRow) * tileSize, tileSize, tileSize),
                        Color.White
                    );
                }
            }

            sbatch.End();

            var view = new Vector2(Viewport.X, Viewport.Y) - half;

            //decals

            var rnd = new System.Random(2);
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead);

            for (int i = 0; i < 20; i++)
                sbatch.Draw(decal, view + new Vector2(600 + (i % 10) * 30, 40 + (i / 10) * rnd.Next(0, 4) * 40), Color.White);

            sbatch.End();

            //entities

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, stencilRead);

            foreach (var ent in ActiveEnts)
            {
                if (ent.Sprite != null)
                {
#if DEBUG
                    dbgInfo.visibleEnts++;
#endif

                    if (ent.OutlineColor.A > 0)
                        outlined.Add(ent);
                    else
                    {
                        var angle = (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                        ent.Sprite.Draw(sbatch, view + ent.Position, angle);
                    }
                }
            }

            sbatch.End();

            sbatch.Begin(SpriteSortMode.Deferred, null, null, stencilRead, null, outlineEffect);

            foreach (var ent in outlined)
            {
                outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / ent.Sprite.image.Width, 1.0f / ent.Sprite.image.Height));
                outlineEffect.Parameters["FrameSize"].SetValue(new Vector2(ent.Sprite.Width, ent.Sprite.Height));
                outlineEffect.Parameters["OutlineColor"].SetValue(ent.OutlineColor.ToVector4());
                var angle = (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                ent.Sprite.Draw(sbatch, view + ent.Position, angle);
            }

            sbatch.End();

            //visible sector region
            startX = System.Math.Max((startX / sectorSize) - 1, 0);
            startY = System.Math.Max((startY / sectorSize) - 1, 0);
            endX = System.Math.Min(1 + (endX - 1) / sectorSize, Width / sectorSize);
            endY = System.Math.Min(1 + (endY - 1) / sectorSize, Height / sectorSize);
            
            //metablobs (alpha tested)

            GraphicsDevice.SetRenderTarget(postRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack);
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //inactive blobs
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    foreach (var blob in Sectors[y, x].blobs)
                    {
#if DEBUG
                        dbgInfo.visibleInactiveBlobs++;
#endif
                        sbatch.Draw(blob.type.Texture, view + blob.position - new Vector2(blob.type.Texture.Width / 2, blob.type.Texture.Height / 2), Color.White);
                    }
                }
            }
            //active blobs
            foreach (var blob in ActiveBlobs)
            {
#if DEBUG
                dbgInfo.visibleActiveBlobs++;
#endif
                sbatch.Draw(blob.type.Texture, view + blob.position - new Vector2(blob.type.Texture.Width / 2, blob.type.Texture.Height / 2), Color.White);
            }

            sbatch.End();
            GraphicsDevice.SetRenderTarget(mainRenderTarget);

            //draw blobs onto map
            
            sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, stencilRead, null, metaAlphaTest);
            sbatch.Draw(postRenderTarget, Vector2.Zero, Color.White);
            sbatch.End();

            //final render

            GraphicsDevice.SetRenderTargets(lastRt);

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sbatch.Draw(mainRenderTarget, Vector2.Zero, Color.White);

            while (debugLines.Count > 0)
            {
                var line = debugLines[debugLines.Count - 1];
                Graphics.Primitives2D.DrawLine(sbatch, line.color, view + line.start, view + line.end);
                debugLines.RemoveAt(debugLines.Count - 1);
            }

#if DEBUG
            var dbgString = string.Format
            (
                "Visible\n======\nEnts: {0}\nInactive blobs: {1}\nActive Blobs: {2}\nDecals: {3}",
                dbgInfo.visibleEnts,
                dbgInfo.visibleInactiveBlobs,
                dbgInfo.visibleActiveBlobs,
                dbgInfo.visibleDecals
            );

            DebugFont.Draw(sbatch, dbgString, new Vector2(10, 30), Color.White);
#endif

            sbatch.End();
        }
    }
}
