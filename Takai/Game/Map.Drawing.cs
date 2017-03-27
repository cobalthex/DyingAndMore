using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public partial class Map
    {
        protected GraphicsDevice GraphicsDevice { get; set; }
        protected SpriteBatch sbatch;
        protected RenderTarget2D preRenderTarget;
        protected RenderTarget2D fluidsRenderTarget;
        protected RenderTarget2D reflectionRenderTarget; //the reflection mask
        protected RenderTarget2D reflectedRenderTarget; //draw all things that should be reflected here
        protected DepthStencilState stencilWrite, stencilRead;
        protected AlphaTestEffect mapAlphaTest;
        protected Effect outlineEffect;
        protected Effect fluidEffect;
        protected Effect reflectionEffect; //writes color and reflection information to two render targets

        public Texture2D TilesImage { get; set; }

        /// <summary>
        /// A set of lines to draw during the next frame (map relative coordinates)
        /// </summary>
        protected List<VertexPositionColor> debugLines = new List<VertexPositionColor>(32);
        protected Effect lineEffect;
        protected RasterizerState lineRaster;

        /// <summary>
        /// Configurable render settings (primarily for debugging)
        /// </summary>
        public class MapRenderSettings
        {
            //todo: maybe convert to enum/packed vector

            public bool drawTiles;
            public bool drawEntities;
            public bool drawFluids;
            public bool drawReflections;
            public bool drawFluidReflectionMask;
            public bool drawDecals;
            public bool drawParticles;
            public bool drawTriggers;
            public bool drawLines;
            public bool drawGrids;
            public bool drawBordersAroundNonDrawingEntities;
        }

        [Data.NonSerialized]
        public MapRenderSettings renderSettings = new MapRenderSettings()
        {
            drawTiles = true,
            drawEntities = true,
            drawFluids = true,
            drawReflections = true,
            drawFluidReflectionMask = false,
            drawDecals = true,
            drawParticles = true,
            drawTriggers = false,
            drawLines = true,
            drawGrids = false,
            drawBordersAroundNonDrawingEntities = false
        };

        /// <summary>
        /// Draw a line next frame
        /// </summary>
        /// <param name="start">The start position of the line</param>
        /// <param name="end">The end position of the line</param>
        /// <param name="color">The color of the line</param>
        /// <remarks>Lines are world relative</remarks>
        public void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            debugLines.Add(new VertexPositionColor { Position = new Vector3(start, 0), Color = color });
            debugLines.Add(new VertexPositionColor { Position = new Vector3(end, 0), Color = color });
        }

        /// <summary>
        /// Draw an outlined rectangle next frame
        /// </summary>
        /// <param name="rect">the region to draw</param>
        /// <param name="color">The color to use</param>
        public void DrawRect(Rectangle rect, Color color)
        {
            DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top), color);
            DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Left, rect.Bottom), color);
            DrawLine(new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom), color);
            DrawLine(new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Right, rect.Bottom), color);
        }

        public struct MapProfilingInfo
        {
            public int visibleEnts;
            public int visibleInactiveFluids;
            public int visibleActiveFluids;
            public int visibleDecals;
        }
        [Data.NonSerialized]
        public MapProfilingInfo ProfilingInfo { get { return profilingInfo; } }
        protected MapProfilingInfo profilingInfo;

        public Map(GraphicsDevice gDevice)
        {
            InitializeGraphics(gDevice);
        }

        /// <summary>
        /// Create a new map
        /// </summary>
        /// <param name="gDevice">The graphics device to use for rendering the map</param>
        public void InitializeGraphics(GraphicsDevice gDevice)
        {
            GraphicsDevice = gDevice;
            if (gDevice != null)
            {
                sbatch = new SpriteBatch(gDevice);
                lineEffect = Takai.AssetManager.Load<Effect>("Shaders/DX11/Line.mgfx");
                lineRaster = new RasterizerState()
                {
                    CullMode = CullMode.None,
                    MultiSampleAntiAlias = true
                };
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

                var width = gDevice.PresentationParameters.BackBufferWidth;
                var height = gDevice.PresentationParameters.BackBufferHeight;

                mapAlphaTest = new AlphaTestEffect(gDevice)
                {
                    ReferenceAlpha = 1
                };
                preRenderTarget = new RenderTarget2D(gDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
                fluidsRenderTarget = new RenderTarget2D(gDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                reflectionRenderTarget = new RenderTarget2D(gDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                reflectedRenderTarget = new RenderTarget2D(gDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                //todo: some of the render targets may be able to be combined

                outlineEffect = Takai.AssetManager.Load<Effect>("Shaders/DX11/Outline.mgfx");
                fluidEffect = Takai.AssetManager.Load<Effect>("Shaders/DX11/Fluid.mgfx");
                reflectionEffect = Takai.AssetManager.Load<Effect>("Shaders/DX11/Reflection.mgfx");
            }
        }

        //todo: separate debug text
        //todo: curves (and volumetric curves) (things like rivers/flows)

        private List<Entity> _drawEntsOutlined = new List<Entity>();

        public string debugOut;

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="Camera">The top-left corner of the visible area</param>
        /// <param name="Viewport">Where on screen to draw</param>
        /// <param name="PostEffect">An optional fullscreen post effect to render with</param>
        /// <param name="DrawSectorEntities">Draw entities in sectors. Typically used for debugging/map editing</param>
        /// <remarks>All rendering management handled internally</remarks>
        public void Draw(Camera Camera = null, Effect PostEffect = null)
        {
            if (Camera == null)
                Camera = ActiveCamera;

            profilingInfo = new MapProfilingInfo();

            #region setup

            _drawEntsOutlined.Clear();

            var originalRt = GraphicsDevice.GetRenderTargets();

            var cameraTransform = Camera.Transform;
            var cameraViewTransform = cameraTransform * Matrix.CreateOrthographicOffCenter(Camera.Viewport, 0, 1);

            var visibleRegion = Rectangle.Intersect(Camera.VisibleRegion, Bounds);
            var visibleTiles = Rectangle.Intersect(
                new Rectangle(
                    visibleRegion.X / TileSize,
                    visibleRegion.Y / tileSize,
                    visibleRegion.Width / tileSize + 2,
                    visibleRegion.Height / tileSize + 2
                ),
                new Rectangle(0, 0, Width, Height)
            );
            var visibleSectors = Rectangle.Intersect(
                new Rectangle(
                    visibleTiles.X / SectorSize - 1,
                    visibleTiles.Y / SectorSize - 1,
                    visibleTiles.Width / SectorSize + 3,
                    visibleTiles.Height / SectorSize + 3
                ),
                new Rectangle(0, 0, Sectors.GetLength(1), Sectors.GetLength(0))
            );

            #endregion

            GraphicsDevice.SetRenderTarget(reflectedRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack);

            if (renderSettings.drawEntities)
            {
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, null, cameraTransform);

                foreach (var ent in ActiveEnts)
                {
                    bool didDraw = false;
                    foreach (var sprite in ent.Sprites)
                    {
                        if (sprite?.Texture == null)
                            continue;

                        didDraw = true;
                        profilingInfo.visibleEnts++;

                        if (ent.OutlineColor.A > 0)
                            _drawEntsOutlined.Add(ent);
                        else
                        {
                            var angle = ent.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                            sprite.Draw(sbatch, ent.Position, angle);
                            //todo: draw all overlays
                        }
                    }

                    if (renderSettings.drawBordersAroundNonDrawingEntities && !didDraw)
                    {
                        Matrix transform = new Matrix(ent.Direction.X, ent.Direction.Y, 0, 0,
                                                     -ent.Direction.Y, ent.Direction.X, 0, 0,
                                                      0, 0, 1, 0,
                                                      0, 0, 0, 1);

                        Rectangle rect = new Rectangle(new Vector2(-ent.Radius).ToPoint(), new Vector2(ent.Radius * 2).ToPoint());

                        var tl = ent.Position + Vector2.TransformNormal(new Vector2(rect.Left, rect.Top), transform);
                        var tr = ent.Position + Vector2.TransformNormal(new Vector2(rect.Right, rect.Top), transform);
                        var bl = ent.Position + Vector2.TransformNormal(new Vector2(rect.Left, rect.Bottom), transform);
                        var br = ent.Position + Vector2.TransformNormal(new Vector2(rect.Right, rect.Bottom), transform);

                        DrawLine(tl, tr, Color.Salmon);
                        DrawLine(tr, br, Color.Salmon);
                        DrawLine(br, bl, Color.Salmon);
                        DrawLine(bl, tl, Color.Salmon);

                        DrawLine(tl, br, Color.Salmon);
                        DrawLine(bl, tr, Color.Salmon);
                    }
                }

                sbatch.End();

                //outlined entities
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, outlineEffect, cameraTransform);

                foreach (var ent in _drawEntsOutlined)
                {
                    bool first = true;
                    foreach (var sprite in ent.Sprites)
                    {
                        if (sprite?.Texture == null)
                            continue;

                        //outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / sprite.Texture.Width, 1.0f / sprite.Texture.Height));
                        //outlineEffect.Parameters["FrameSize"].SetValue(new Vector2(sprite.Width, sprite.Height));

                        var angle = ent.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                        sprite.Draw(sbatch, ent.Position, angle, first ? ent.OutlineColor : Color.Transparent);

                        first = false;
                    }
                }

                sbatch.End();
            }

            if (renderSettings.drawParticles)
            {
                foreach (var p in Particles)
                {
                    sbatch.Begin(SpriteSortMode.BackToFront, p.Key.BlendMode, null, stencilRead, null, null, cameraTransform);

                    for (int i = 0; i < p.Value.Count; i++)
                    {
                        if (p.Value[i].time == System.TimeSpan.Zero)
                            continue;

                        var sz = p.Key.Graphic.Size.ToVector2();
                        sz *= p.Value[i].scale;

                        p.Key.Graphic.Draw
                        (
                            sbatch,
                            new Rectangle(p.Value[i].position.ToPoint(), sz.ToPoint()),
                            p.Value[i].rotation,
                            p.Value[i].color,
                            ElapsedTime - p.Value[i].time
                        );
                    }

                    sbatch.End();
                }
            }

            GraphicsDevice.SetRenderTargets(fluidsRenderTarget, reflectionRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack);

            if (renderSettings.drawFluids || renderSettings.drawFluidReflectionMask)
            {
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, reflectionEffect, cameraTransform);

                //inactive fluids
                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                    {
                        foreach (var fluid in Sectors[y, x].Fluids)
                        {
                            profilingInfo.visibleInactiveFluids++;
                            reflectionEffect.Parameters["Reflection"].SetValue(fluid.type.Reflection);
                            sbatch.Draw(fluid.type.Texture, fluid.position - new Vector2(fluid.type.Texture.Width / 2, fluid.type.Texture.Height / 2), new Color(1, 1, 1, fluid.type.Alpha));
                        }
                    }
                }
                //active fluids
                foreach (var fluid in ActiveFluids)
                {
                    profilingInfo.visibleActiveFluids++;
                    reflectionEffect.Parameters["Reflection"].SetValue(fluid.type.Reflection);
                    sbatch.Draw(fluid.type.Texture, fluid.position - new Vector2(fluid.type.Texture.Width / 2, fluid.type.Texture.Height / 2), new Color(1, 1, 1, fluid.type.Alpha));
                }

                sbatch.End();
            }

            //main render
            GraphicsDevice.SetRenderTargets(preRenderTarget);

            if (renderSettings.drawTiles)
            {
                var projection = Matrix.CreateOrthographicOffCenter(Camera.Viewport, 0, 1);
                mapAlphaTest.Projection = projection;
                mapAlphaTest.View = cameraTransform;
                sbatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, stencilWrite, null, mapAlphaTest);

                for (var y = visibleTiles.Top; y < visibleTiles.Bottom; ++y)
                {
                    for (var x = visibleTiles.Left; x < visibleTiles.Right; ++x)
                    {
                        var tile = Tiles[y, x];
                        if (tile < 0)
                            continue;

                        sbatch.Draw
                        (
                            TilesImage,
                            new Vector2(x * TileSize, y * TileSize),
                            new Rectangle((tile % TilesPerRow) * TileSize, (tile / TilesPerRow) * TileSize, TileSize, TileSize),
                            Color.White
                        );
                    }
                }

                sbatch.End();
            }

            if (renderSettings.drawDecals)
            {
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, null, cameraTransform);

                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                    {
                        foreach (var decal in Sectors[y, x].decals)
                        {
                            sbatch.Draw
                            (
                                decal.texture,
                                decal.position,
                                null,
                                Color.White,
                                decal.angle,
                                new Vector2(decal.texture.Width / 2, decal.texture.Height / 2),
                                decal.scale,
                                SpriteEffects.None,
                                0
                            );
                            profilingInfo.visibleDecals++;
                        }
                    }
                }
                sbatch.End();
            }

            #region Present fluids + reflections

            if (renderSettings.drawFluidReflectionMask)
            {
                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                sbatch.Draw(reflectionRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }

            if (renderSettings.drawFluids)
            {
                //todo: transform correctly

                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, stencilRead, null, fluidEffect);
                fluidEffect.Parameters["Mask"].SetValue(reflectionRenderTarget);
                fluidEffect.Parameters["Reflection"].SetValue(renderSettings.drawReflections ? reflectedRenderTarget : null);
                sbatch.Draw(fluidsRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }

            #endregion

            //draw entities (and any other reflected objects)
            sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, stencilRead);
            sbatch.Draw(reflectedRenderTarget, Vector2.Zero, Color.White);
            sbatch.End();

            if (renderSettings.drawTriggers)
            {
                sbatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, null, cameraTransform);
                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                    {
                        foreach (var trigger in Sectors[y, x].triggers)
                            Graphics.Primitives2D.DrawFill(sbatch, new Color(Color.Aqua, 0.25f), trigger.Region);
                    }
                }
                sbatch.End();
            }

            if (renderSettings.drawLines)
            {
                if (debugLines.Count > 0)
                {
                    GraphicsDevice.RasterizerState = lineRaster;
                    GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    lineEffect.Parameters["Transform"].SetValue(cameraViewTransform);

                    var blackLines = debugLines.ConvertAll(line => { line.Color = Color.Black; line.Position += new Vector3(1, 1, 0); return line; });
                    foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, blackLines.ToArray(), 0, blackLines.Count / 2);
                    }

                    foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, debugLines.ToArray(), 0, debugLines.Count / 2);
                    }
                    debugLines.Clear();
                }
            }

            if (renderSettings.drawGrids)
            {
                GraphicsDevice.RasterizerState = lineRaster;
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                GraphicsDevice.DepthStencilState = DepthStencilState.None;
                var viewProjection = Matrix.CreateOrthographicOffCenter(Camera.Viewport, 0, 1);
                lineEffect.Parameters["Transform"].SetValue(cameraTransform * viewProjection);

                var grids = new VertexPositionColor[visibleTiles.Width * 2 + visibleTiles.Height * 2];
                var gridColor = new Color(Color.Gray, 0.3f);
                var sectorColor = new Color(Color.Gray, 0.65f);
                var cameraOffset = -new Vector2(visibleRegion.X % TileSize, visibleRegion.Y % TileSize);
                for (int i = 0; i < visibleTiles.Width; ++i)
                {
                    var n = i * 2;
                    grids[n].Position = new Vector3(cameraOffset.X + visibleRegion.X + i * TileSize, visibleRegion.Top, 0);
                    grids[n].Color = (int)grids[n].Position.X % SectorPixelSize < TileSize ? sectorColor : gridColor;
                    grids[n + 1] = grids[n];
                    grids[n + 1].Position.Y += visibleRegion.Height;
                }
                for (int i = 0; i < visibleTiles.Height; ++i)
                {
                    var n = visibleTiles.Width * 2 + i * 2;
                    grids[n].Position = new Vector3(visibleRegion.Left, cameraOffset.Y + visibleRegion.Y + i * TileSize, 0);
                    grids[n].Color = (int)grids[n].Position.Y % SectorPixelSize < TileSize ? sectorColor : gridColor;
                    grids[n + 1] = grids[n];
                    grids[n + 1].Position.X += visibleRegion.Width;
                }

                foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, grids, 0, grids.Length / 2);
                }
            }

            #region present

            GraphicsDevice.SetRenderTargets(originalRt);

            sbatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, PostEffect);
            sbatch.Draw(preRenderTarget, Vector2.Zero, Color.White);
            sbatch.End();

            #endregion
        }
    }
}
;