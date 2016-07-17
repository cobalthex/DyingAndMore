using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public partial class Map
    {
        protected GraphicsDevice GraphicsDevice { get; set; }
        protected SpriteBatch sbatch;
        protected DepthStencilState stencilWrite, stencilRead;
        protected AlphaTestEffect mapAlphaTest;
        protected RenderTarget2D preRenderTarget;
        protected RenderTarget2D blobsRenderTarget;
        protected RenderTarget2D reflectionRenderTarget; //the reflection mask
        protected RenderTarget2D reflectedRenderTarget; //draw all things that should be reflected here
        protected Effect outlineEffect;
        protected Effect blobEffect;
        protected Effect reflectionEffect; //writes color and reflection information to two render targets

        public Texture2D TilesImage { get; set; }

        /// <summary>
        /// A set of lines to draw during the next frame
        /// </summary>
        protected List<VertexPositionColor> debugLines = new List<VertexPositionColor>(32);
        protected Effect lineEffect;
        protected RasterizerState lineRaster;
        
        public Takai.Graphics.BitmapFont DebugFont { get; set; }

        /// <summary>
        /// Configurable debug options
        /// </summary>
        public struct DebugOptions
        {
            public bool showProfileInfo;
            public bool showBlobReflectionMask;
            public bool showOnlyReflections;
            public bool showEntInfo;
        }
        
        public DebugOptions debugOptions;
        
        /// <summary>
        /// Draw a line the next frame
        /// </summary>
        /// <param name="Start">The start position of the line</param>
        /// <param name="End">The end position of the line</param>
        /// <param name="Color">The color of the line</param>
        /// <remarks>Lines are world relative</remarks>
        public void DebugLine(Vector2 Start, Vector2 End, Color Color)
        {
            debugLines.Add(new VertexPositionColor { Position = new Vector3(Start, 0), Color = Color });
            debugLines.Add(new VertexPositionColor { Position = new Vector3(End, 0), Color = Color });
        }

        protected struct DebugProfilingInfo
        {
            public int visibleEnts;
            public int visibleInactiveBlobs;
            public int visibleActiveBlobs;
            public int visibleDecals;
        }


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
                lineEffect = Takai.AssetManager.Load<Effect>("Shaders/Line.mgfx");
                lineRaster = new RasterizerState();
                lineRaster.CullMode = CullMode.None;
                lineRaster.MultiSampleAntiAlias = true;

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

                preRenderTarget = new RenderTarget2D(GDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
                blobsRenderTarget = new RenderTarget2D(GDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                reflectionRenderTarget = new RenderTarget2D(GDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                reflectedRenderTarget = new RenderTarget2D(GDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                //todo: some of the render targets may be able to be combined

                outlineEffect = Takai.AssetManager.Load<Effect>("Shaders/Outline.mgfx");
                blobEffect = Takai.AssetManager.Load<Effect>("Shaders/Blob.mgfx");
                reflectionEffect = Takai.AssetManager.Load<Effect>("Shaders/Reflection.mgfx");
            }
        }

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="Camera">The top-left corner of the visible area</param>
        /// <param name="Viewport">Where on screen to draw</param>
        /// <param name="PostEffect">An optional fullscreen post effect to render with</param>
        /// <param name="DrawSectorEntities">Draw entities in sectors. Typically used for debugging/map editing</param>
        /// <remarks>All rendering management handled internally</remarks>
        public void Draw(Vector2 ViewStart, Rectangle Viewport, Effect PostEffect = null, bool DrawSectorEntities = false)
        {
            var originalRt = GraphicsDevice.GetRenderTargets();

            var outlined = new List<Entity>();

            DebugProfilingInfo dbgInfo = new DebugProfilingInfo();
            
            var view = new Vector2(Viewport.X, Viewport.Y) - ViewStart;

            var startX = (int)ViewStart.X / tileSize;
            var startY = (int)ViewStart.Y / tileSize;

            int endX = startX + 1 + ((Viewport.Width - 1) / tileSize);
            int endY = startY + 1 + ((Viewport.Height - 1) / tileSize);

            startX = System.Math.Max(startX, 0);
            startY = System.Math.Max(startY, 0);

            endX = System.Math.Min(endX + 1, Width);
            endY = System.Math.Min(endY + 1, Height);

            //visible sector region
            int sStartX = System.Math.Max((startX / sectorSize) - 1, 0);
            int sStartY = System.Math.Max((startY / sectorSize) - 1, 0);
            int sEndX = System.Math.Min(1 + (endX - 1) / sectorSize, Width / sectorSize);
            int sEndY = System.Math.Min(1 + (endY - 1) / sectorSize, Height / sectorSize);

            //entities
            GraphicsDevice.SetRenderTarget(reflectedRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack);
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead);

            if (DrawSectorEntities)
            {
                for (var y = sStartY; y < sEndY; y++)
                {
                    for (var x = sStartX; x < sEndX; x++)
                    {
                        foreach (var ent in Sectors[y, x].entities)
                        {
                            if (ent.Sprite != null)
                            {
                                dbgInfo.visibleEnts++;

                                if (ent.OutlineColor.A > 0)
                                    outlined.Add(ent);
                                else
                                {
                                    var angle = (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                                    ent.Sprite.Draw(sbatch, view + ent.Position, angle);
                                }
                            }

                            if (debugOptions.showEntInfo)
                                DrawEntInfo(ent, view);
                        }
                    }
                }
            }

            foreach (var ent in ActiveEnts)
            {
                if (ent.Sprite != null)
                {
                    dbgInfo.visibleEnts++;

                    if (ent.OutlineColor.A > 0)
                        outlined.Add(ent);
                    else
                    {
                        var angle = (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                        ent.Sprite.Draw(sbatch, view + ent.Position, angle);
                    }
                }

                if (debugOptions.showEntInfo)
                    DrawEntInfo(ent, view);
            }

            sbatch.End();

            //outlined entities
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, outlineEffect);

            foreach (var ent in outlined)
            {
                outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / ent.Sprite.Texture.Width, 1.0f / ent.Sprite.Texture.Height));
                outlineEffect.Parameters["FrameSize"].SetValue(new Vector2(ent.Sprite.Width, ent.Sprite.Height));
                var angle = (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                ent.Sprite.Draw(sbatch, view + ent.Position, angle, ent.OutlineColor);
            }

            sbatch.End();

            //metablobs (alpha tested)
            GraphicsDevice.SetRenderTargets(blobsRenderTarget, reflectionRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack);
            sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, reflectionEffect);

            //inactive blobs
            for (var y = sStartY; y < sEndY; y++)
            {
                for (var x = sStartX; x < sEndX; x++)
                {
                    foreach (var blob in Sectors[y, x].blobs)
                    {
                        dbgInfo.visibleInactiveBlobs++;
                        reflectionEffect.Parameters["Reflection"].SetValue(blob.type.Reflection);
                        sbatch.Draw(blob.type.Texture, view + blob.position - new Vector2(blob.type.Texture.Width / 2, blob.type.Texture.Height / 2), Color.White);
                    }
                }
            }
            //active blobs
            foreach (var blob in ActiveBlobs)
            {
                dbgInfo.visibleActiveBlobs++;
                reflectionEffect.Parameters["Reflection"].SetValue(blob.type.Reflection);
                sbatch.Draw(blob.type.Texture, view + blob.position - new Vector2(blob.type.Texture.Width / 2, blob.type.Texture.Height / 2), Color.White);
            }

            sbatch.End();

            //main render
            GraphicsDevice.SetRenderTargets(preRenderTarget);

            //map tiles
            sbatch.Begin(SpriteSortMode.Deferred, null, null, stencilWrite, null, mapAlphaTest);

            for (var y = startY; y < endY; y++)
            {
                var vy = Viewport.Y + (y * tileSize) - (int)ViewStart.Y;

                for (var x = startX; x < endX; x++)
                {
                    var tile = Tiles[y, x];
                    if (tile < 0)
                        continue;

                    var vx = Viewport.X + (x * tileSize) - (int)ViewStart.X;

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

            //decals
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead);

            for (var y = sStartY; y < sEndY; y++)
            {
                for (var x = sStartX; x < sEndX; x++)
                {
                    foreach (var decal in Sectors[y, x].decals)
                    {
                        sbatch.Draw
                        (
                            decal.texture,
                            view + decal.position,
                            null,
                            Color.White,
                            decal.angle,
                            new Vector2(decal.texture.Width / 2, decal.texture.Height / 2),
                            1,
                            SpriteEffects.None,
                            0
                        );
                        dbgInfo.visibleDecals++;
                    }
                }
            }
            sbatch.End();

            //draw blobs onto map (with reflections)
            if (debugOptions.showBlobReflectionMask)
            {
                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                sbatch.Draw(reflectionRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }
            else
            {
                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, stencilRead, null, blobEffect);
                blobEffect.Parameters["Mask"].SetValue(reflectionRenderTarget);
                blobEffect.Parameters["Reflection"].SetValue(reflectedRenderTarget);
                sbatch.Draw(blobsRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }

            if (!debugOptions.showOnlyReflections)
            {
                //draw entities (and any other reflected objects)
                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, stencilRead);
                sbatch.Draw(reflectedRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }

            //debug lines
            if (debugLines.Count > 0)
            {
                GraphicsDevice.RasterizerState = lineRaster;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                var projection = Matrix.CreateOrthographicOffCenter(GraphicsDevice.Viewport.Bounds, 0, 1);
                var offset = Matrix.CreateTranslation(view.X, view.Y, 0);
                lineEffect.Parameters["Transform"].SetValue(offset * projection);
                foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, debugLines.ToArray(), 0, debugLines.Count / 2);
                }
                debugLines.Clear();
            }

            //present
            GraphicsDevice.SetRenderTargets(originalRt);
            
            sbatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, PostEffect);
            sbatch.Draw(preRenderTarget, Vector2.Zero, Color.White);
            sbatch.End();

            //draw debug info

            if (debugOptions.showProfileInfo)
            {
                sbatch.Begin(SpriteSortMode.Deferred);

                var dbgString = string.Format
                (
                    "Visible\n======\nEnts: {0}\nInactive blobs: {1}\nActive Blobs: {2}\nDecals: {3}",
                    dbgInfo.visibleEnts,
                    dbgInfo.visibleInactiveBlobs,
                    dbgInfo.visibleActiveBlobs,
                    dbgInfo.visibleDecals
                );

                DebugFont.Draw(sbatch, dbgString, new Vector2(10, 30), Color.White);

                sbatch.End();
            }
        }

        protected void DrawEntInfo(Entity Ent, Vector2 View)
        {
            //draw bounding box
            DebugLine(new Vector2(Ent.Position.X - Ent.Radius, Ent.Position.Y - Ent.Radius), new Vector2(Ent.Position.X + Ent.Radius, Ent.Position.Y - Ent.Radius), Color.LightGreen);
            DebugLine(new Vector2(Ent.Position.X + Ent.Radius, Ent.Position.Y - Ent.Radius), new Vector2(Ent.Position.X + Ent.Radius, Ent.Position.Y + Ent.Radius), Color.LightGreen);
            DebugLine(new Vector2(Ent.Position.X + Ent.Radius, Ent.Position.Y + Ent.Radius), new Vector2(Ent.Position.X - Ent.Radius, Ent.Position.Y + Ent.Radius), Color.LightGreen);
            DebugLine(new Vector2(Ent.Position.X - Ent.Radius, Ent.Position.Y + Ent.Radius), new Vector2(Ent.Position.X - Ent.Radius, Ent.Position.Y - Ent.Radius), Color.LightGreen);

            //draw direction arrow
            var angle = (float)System.Math.Atan2(Ent.Direction.Y, Ent.Direction.X);
            var tip = Ent.Position + (Ent.Direction * Ent.Radius * 1.5f);
            DebugLine(Ent.Position, tip, Color.Yellow);
            float theta = MathHelper.ToRadians(150);

            float r = MathHelper.Clamp(Ent.Radius * 0.5f, 5, 30);
            DebugLine(tip, tip + (r * new Vector2((float)System.Math.Cos(angle + theta), (float)System.Math.Sin(angle + theta))), Color.Yellow);
            DebugLine(tip, tip + (r * new Vector2((float)System.Math.Cos(angle - theta), (float)System.Math.Sin(angle - theta))), Color.Yellow);

            //draw ent info string
            var str = string.Format("{0:N1},{1:N1}", Ent.Position.X, Ent.Position.Y);
            var sz = DebugFont.MeasureString(str);
            DebugFont.Draw(sbatch, str, View + Ent.Position + new Vector2(sz.X / -2, Ent.Radius + 5), Color.White);
        }
    }
}
