using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

using XnaEffect = Microsoft.Xna.Framework.Graphics.Effect;

namespace Takai.Game
{
    public partial class MapClass
    {
        //todo: should all of these gpu props be static?

        internal SpriteBatch spriteBatch;
        internal RenderTarget2D preRenderTarget;
        internal RenderTarget2D fluidsRenderTarget;
        internal RenderTarget2D reflectionRenderTarget; //the reflection mask
        internal RenderTarget2D reflectedRenderTarget; //draw all things that should be reflected here
        internal DepthStencilState stencilWrite;
        internal DepthStencilState stencilRead;
        internal AlphaTestEffect mapAlphaTest;
        internal XnaEffect outlineEffect;
        internal XnaEffect fluidEffect;
        internal XnaEffect reflectionEffect; //writes color and reflection information to two render targets

        internal XnaEffect lineEffect;
        internal XnaEffect circleEffect;
        internal RasterizerState shapeRaster;

        public Texture2D TilesImage { get; set; }

        //todo: curves (and volumetric curves) (things like rivers/flows)

        /// <summary>
        /// Create/load all of the graphics resources to draw this map
        /// </summary>
        public void InitializeGraphics()
        {
            if (Runtime.GraphicsDevice == null)
                return;

            if (spriteBatch == null)
            {
                spriteBatch = new SpriteBatch(Runtime.GraphicsDevice);

                shapeRaster = new RasterizerState()
                {
                    CullMode = CullMode.None,
                    MultiSampleAntiAlias = true,
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

                var width = Runtime.GraphicsDevice.PresentationParameters.BackBufferWidth;
                var height = Runtime.GraphicsDevice.PresentationParameters.BackBufferHeight;

                mapAlphaTest = new AlphaTestEffect(Runtime.GraphicsDevice)
                {
                    ReferenceAlpha = 1
                };
                preRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
                fluidsRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                reflectionRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                reflectedRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
                //todo: some of the render targets may be able to be combined
            }

            //must be 'reloaded' or will be deleted if Cache.TrackReferences is called

            lineEffect = Data.Cache.Load<XnaEffect>("Shaders/Line.mgfx");
            circleEffect = Data.Cache.Load<XnaEffect>("Shaders/Circle.mgfx");
            outlineEffect = Data.Cache.Load<XnaEffect>("Shaders/Outline.mgfx");
            fluidEffect = Data.Cache.Load<XnaEffect>("Shaders/Fluid.mgfx");
            reflectionEffect = Data.Cache.Load<XnaEffect>("Shaders/Reflection.mgfx");
        }
    }

    public partial class MapInstance
    {
        public struct MapProfilingInfo
        {
            public int visibleEnts;
            public int visibleInactiveFluids;
            public int visibleActiveFluids;
            public int visibleDecals;
        }
        [Data.Serializer.Ignored]
        public MapProfilingInfo ProfilingInfo { get { return profilingInfo; } }
        protected MapProfilingInfo profilingInfo;

        //a collection of primatives to draw next frame (for one frame)
        protected List<VertexPositionColor> renderedLines = new List<VertexPositionColor>(32);
        protected List<VertexPositionColorTexture> renderedCircles = new List<VertexPositionColorTexture>(32);

        /// <summary>
        /// All of the available render settings customizations
        /// </summary>
        public class RenderSettings
        {
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
            public bool drawSectorsOnGrid;
            public bool drawBordersAroundNonDrawingEntities;
            public bool drawEntBoundingBoxes;
            public bool drawPathHeuristic;

            public static readonly RenderSettings Default = new RenderSettings
            {
                drawTiles = true,
                drawEntities = true,
                drawFluids = true,
                drawReflections = true,
                drawDecals = true,
                drawParticles = true,
                drawLines = true,
            };
        }

        [Data.Serializer.Ignored]
        public RenderSettings renderSettings = RenderSettings.Default;

        /// <summary>
        /// Draw a line next frame
        /// </summary>
        /// <param name="start">The start position of the line (in map space)</param>
        /// <param name="end">The end position of the line (in map space)</param>
        /// <param name="color">The color of the line</param>
        public void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            renderedLines.Add(new VertexPositionColor(new Vector3(start, 0), color));
            renderedLines.Add(new VertexPositionColor(new Vector3(end, 0), color));
        }

        /// <summary>
        /// Draw an outlined rectangle next frame
        /// </summary>
        /// <param name="rect">the region to draw (in map space)</param>
        /// <param name="color">The color to use</param>
        public void DrawRect(Rectangle rect, Color color)
        {
            DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top), color);
            DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Left, rect.Bottom), color);
            DrawLine(new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom), color);
            DrawLine(new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Right, rect.Bottom), color);
        }

        /// <summary>
        /// Draw an outlined circle next frame
        /// </summary>
        /// <param name="center">The center of the circle (in map space)</param>
        /// <param name="radius">The radius of the circle</param>
        /// <param name="color">The color to use</param>
        public void DrawCircle(Vector2 center, float radius, Color color)
        {
            renderedCircles.Add(new VertexPositionColorTexture(new Vector3(center + new Vector2(-radius), 0), color, new Vector2(0)));
            renderedCircles.Add(new VertexPositionColorTexture(new Vector3(center + new Vector2(radius, -radius), 0), color, new Vector2(1, 0)));
            renderedCircles.Add(new VertexPositionColorTexture(new Vector3(center + new Vector2(-radius, radius), 0), color, new Vector2(0, 1)));

            renderedCircles.Add(new VertexPositionColorTexture(new Vector3(center + new Vector2(-radius, radius), 0), color, new Vector2(0, 1)));
            renderedCircles.Add(new VertexPositionColorTexture(new Vector3(center + new Vector2(radius, -radius), 0), color, new Vector2(1, 0)));
            renderedCircles.Add(new VertexPositionColorTexture(new Vector3(center + new Vector2(radius), 0), color, new Vector2(1)));
        }

        static readonly Matrix arrowWingTransform = Matrix.CreateRotationZ(120);
        /// <summary>
        /// Draw an arrow facing away from a point
        /// </summary>
        /// <param name="Position">The origin of the tail of the arrow</param>
        /// <param name="Direction">The direction the arrow is facing</param>
        /// <param name="Magnitude">How big the arrow should be</param>
        public void DrawArrow(Vector2 Position, Vector2 Direction, float Magnitude, Color color)
        {
            var tip = Position + (Direction * Magnitude);
            DrawLine(Position, tip, Color.Yellow);

            Magnitude = MathHelper.Clamp(Magnitude * 0.333f, 5, 30);
            DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, arrowWingTransform)), color);
            DrawLine(tip, tip - (Magnitude * Vector2.Transform(Direction, Matrix.Invert(arrowWingTransform))), color);
        }


        private List<EntityInstance> _drawEntsOutlined = new List<EntityInstance>();
        private HashSet<Trigger> _drawTriggers = new HashSet<Trigger>();

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="camera">The top-left corner of the visible area</param>
        /// <param name="Viewport">Where on screen to draw</param>
        /// <param name="Class.PostEffect">An optional fullscreen post effect to render with</param>
        /// <param name="DrawSectorEntities">Draw entities in sectors. Typically used for debugging/map editing</param>
        /// <remarks>All rendering management handled internally</remarks>
        public void Draw(Camera camera = null, XnaEffect postEffect = null)
        {
            if (camera == null)
                camera = ActiveCamera;

            //todo: break out into separate functions

            profilingInfo = new MapProfilingInfo();

            #region setup

            _drawEntsOutlined.Clear();

            var originalRt = Runtime.GraphicsDevice.GetRenderTargets();

            var cameraTransform = camera.Transform;

            var visibleRegion = Rectangle.Intersect(camera.VisibleRegion, Class.Bounds);
            var visibleTiles = Rectangle.Intersect(
                new Rectangle(
                    visibleRegion.X / Class.TileSize,
                    visibleRegion.Y / Class.TileSize,
                    visibleRegion.Width / Class.TileSize + 2,
                    visibleRegion.Height / Class.TileSize + 2
                ),
                Class.TileBounds
            );
            var visibleSectors = GetOverlappingSectors(visibleRegion);

            #endregion

            Runtime.GraphicsDevice.SetRenderTarget(Class.reflectedRenderTarget);
            Runtime.GraphicsDevice.Clear(Color.TransparentBlack);

            if (renderSettings.drawEntities)
            {
                Class.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, Class.stencilRead, null, null, cameraTransform);

                foreach (var ent in activeEntities)
                {
                    if (ent.OutlineColor.A > 0)
                        _drawEntsOutlined.Add(ent);
                    else
                    {
                        var angle = ent.Class.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Forward.Y, ent.Forward.X);

                        foreach (var state in ent.ActiveAnimations)
                        {
                            if (state.Class == null)
                                continue;

                            //todo: revisit (explicit base+overlay)

                            state.Class.Sprite.Draw(
                                Class.spriteBatch,
                                ent.Position,
                                angle,
                                Color.White,
                                1,
                                state.ElapsedTime
                            );
                        }

                        ++profilingInfo.visibleEnts;
                    }

                    if (renderSettings.drawBordersAroundNonDrawingEntities && !System.Linq.Enumerable.Any(ent.ActiveAnimations))
                    {
                        Matrix transform = new Matrix(ent.Forward.X, ent.Forward.Y, 0, 0,
                                                     -ent.Forward.Y, ent.Forward.X, 0, 0,
                                                      0, 0, 1, 0,
                                                      0, 0, 0, 1);

                        Rectangle rect = new Rectangle(new Point(-(int)ent.Radius), new Point((int)ent.Radius * 2));
                        var tl = ent.Position + Vector2.TransformNormal(new Vector2(rect.Left, rect.Top), transform);
                        var tr = ent.Position + Vector2.TransformNormal(new Vector2(rect.Right, rect.Top), transform);
                        var bl = ent.Position + Vector2.TransformNormal(new Vector2(rect.Left, rect.Bottom), transform);
                        var br = ent.Position + Vector2.TransformNormal(new Vector2(rect.Right, rect.Bottom), transform);

                        var color = ent.OutlineColor.A == 0 ? Color.Cyan : ent.OutlineColor;
                        DrawLine(tl, tr, color);
                        DrawLine(tr, br, color);
                        DrawLine(br, bl, color);
                        DrawLine(bl, tl, color);

                        DrawLine(tl, br, color);
                        DrawLine(bl, tr, color);
                    }

                    if (renderSettings.drawEntBoundingBoxes)
                    {
                        var rect = ent.AxisAlignedBounds;
                        var color = Color.LightBlue;
                        DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top), color);
                        DrawLine(new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom), color);
                        DrawLine(new Vector2(rect.Right, rect.Bottom), new Vector2(rect.Left, rect.Bottom), color);
                        DrawLine(new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Left, rect.Top), color);

                        DrawCircle(ent.Position, ent.Radius, Color.Gold);
                    }
                }

                Class.spriteBatch.End();

                //outlined entities
                Class.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, Class.stencilRead, null, Class.outlineEffect, cameraTransform);

                foreach (var ent in _drawEntsOutlined)
                {
                    //Class.outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / sprite.Texture.Width, 1.0f / sprite.Texture.Height));
                    //Class.outlineEffect.Parameters["FrameSize"].SetValue(new Vector2(sprite.Width, sprite.Height));

                    var angle = ent.Class.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Forward.Y, ent.Forward.X);

                    foreach (var state in ent.ActiveAnimations)
                    {
                        if (state.Class == null)
                            continue;

                        state.Class.Sprite.Draw(
                            Class.spriteBatch,
                            ent.Position,
                            angle,
                            Color.White,
                            1,
                            state.ElapsedTime
                        );
                    }
                }

                Class.spriteBatch.End();
            }

            if (renderSettings.drawParticles)
            {
                foreach (var p in Particles)
                {
                    Class.spriteBatch.Begin(SpriteSortMode.BackToFront, p.Key.Blend, null, Class.stencilRead, null, null, cameraTransform);

                    for (int i = 0; i < p.Value.Count; ++i)
                    {
                        if (p.Value[i].time == System.TimeSpan.Zero)
                            continue;

                        p.Key.Graphic.Draw
                        (
                            Class.spriteBatch,
                            p.Value[i].position,
                            p.Value[i].angle,
                            p.Value[i].color,
                            p.Value[i].scale,
                            ElapsedTime - p.Value[i].time
                        );
                    }

                    Class.spriteBatch.End();
                }
            }

            Runtime.GraphicsDevice.SetRenderTargets(Class.fluidsRenderTarget, Class.reflectionRenderTarget);
            Runtime.GraphicsDevice.Clear(Color.TransparentBlack);

            if (renderSettings.drawFluids ||
                renderSettings.drawFluidReflectionMask)
            {
                Class.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, Class.reflectionEffect, cameraTransform);

                //inactive fluids
                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                    {
                        foreach (var fluid in Sectors[y, x].fluids)
                        {
                            ++profilingInfo.visibleInactiveFluids;
                            Class.reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);

                            var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                            Class.spriteBatch.Draw(fluid.Class.Texture, fluid.position, null, new Color(1, 1, 1, fluid.Class.Alpha), 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
                        }
                    }
                }
                //active fluids
                foreach (var fluid in LiveFluids)
                {
                    ++profilingInfo.visibleActiveFluids;
                    Class.reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);

                    var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                    Class.spriteBatch.Draw(fluid.Class.Texture, fluid.position, null, new Color(1, 1, 1, fluid.Class.Alpha), 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
                }

                Class.spriteBatch.End();
            }

            //main render
            Runtime.GraphicsDevice.SetRenderTargets(Class.preRenderTarget);

            if (renderSettings.drawPathHeuristic)
            {
                Class.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, Class.stencilWrite, null, null, cameraTransform);

                var mult = 360f / Class.MaxHeuristic;
                for (var y = visibleTiles.Top; y < visibleTiles.Bottom; ++y)
                {
                    for (var x = visibleTiles.Left; x < visibleTiles.Right; ++x)
                    {
                        var path = Class.PathInfo[y, x];
                        if (path.heuristic == uint.MaxValue)
                            continue;

                        Graphics.Primitives2D.DrawFill(
                            Class.spriteBatch,
                            Util.ColorFromHSL(path.heuristic * mult, 1, 0.8f, 1),
                            new Rectangle(x * Class.TileSize, y * Class.TileSize, Class.TileSize, Class.TileSize)
                        );
                    }
                }

                Class.spriteBatch.End();
            }
            else if (renderSettings.drawTiles)
            {
                var projection = Matrix.CreateOrthographicOffCenter(camera.Viewport, 0, 1);
                Class.mapAlphaTest.Projection = projection;
                Class.mapAlphaTest.View = cameraTransform;
                Class.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, Class.stencilWrite, null, Class.mapAlphaTest);

                for (var y = visibleTiles.Top; y < visibleTiles.Bottom; ++y)
                {
                    for (var x = visibleTiles.Left; x < visibleTiles.Right; ++x)
                    {
                        var tile = Class.Tiles[y, x];
                        if (tile < 0)
                            continue;

                        Class.spriteBatch.Draw
                        (
                            Class.TilesImage,
                            new Vector2(x * Class.TileSize, y * Class.TileSize),
                            new Rectangle((tile % Class.TilesPerRow) * Class.TileSize, (tile / Class.TilesPerRow) * Class.TileSize, Class.TileSize, Class.TileSize),
                            Color.White
                        );
                    }
                }

                Class.spriteBatch.End();
            }

            if (renderSettings.drawDecals)
            {
                Class.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, Class.stencilRead, null, null, cameraTransform);

                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                    {
                        foreach (var decal in Sectors[y, x].decals)
                        {
                            Class.spriteBatch.Draw
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
                            ++profilingInfo.visibleDecals;
                        }
                    }
                }
                Class.spriteBatch.End();
            }

            #region Present fluids + reflections

            if (renderSettings.drawFluidReflectionMask)
            {
                Class.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                Class.spriteBatch.Draw(Class.reflectionRenderTarget, Vector2.Zero, Color.White);
                Class.spriteBatch.End();
            }

            if (renderSettings.drawFluids)
            {
                //todo: transform correctly

                Class.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, Class.stencilRead, null, Class.fluidEffect);
                Class.fluidEffect.Parameters["Mask"].SetValue(Class.reflectionRenderTarget);
                Class.fluidEffect.Parameters["Reflection"].SetValue(renderSettings.drawReflections ? Class.reflectedRenderTarget : null);
                Class.spriteBatch.Draw(Class.fluidsRenderTarget, Vector2.Zero, Color.White);
                Class.spriteBatch.End();
            }

            #endregion

            #region present entities (and any other reflected objects)

            Class.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, Class.stencilRead);
            Class.spriteBatch.Draw(Class.reflectedRenderTarget, Vector2.Zero, Color.White);
            Class.spriteBatch.End();

            #endregion

            if (renderSettings.drawTriggers)
            {
                _drawTriggers.Clear();
                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                        _drawTriggers.UnionWith(Sectors[y, x].triggers);
                }

                Class.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, null, cameraTransform);
                foreach (var trigger in _drawTriggers)
                    Graphics.Primitives2D.DrawFill(Class.spriteBatch, new Color(Color.LimeGreen, 0.25f), trigger.Region);
                Class.spriteBatch.End();
            }

            if (renderSettings.drawLines)
            {
                if (renderedCircles.Count > 0)
                {
                    Runtime.GraphicsDevice.RasterizerState = Class.shapeRaster;
                    Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    var circleTransform = cameraTransform * Matrix.CreateOrthographicOffCenter(camera.Viewport, 0, 1);
                    Class.circleEffect.Parameters["Transform"].SetValue(circleTransform);

                    foreach (EffectPass pass in Class.circleEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, renderedCircles.ToArray(), 0, renderedCircles.Count / 3);
                    }

                    renderedCircles.Clear();
                }

                if (renderedLines.Count > 0)
                {
                    Runtime.GraphicsDevice.RasterizerState = Class.shapeRaster;
                    Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    var lineTransform = cameraTransform * Matrix.CreateOrthographicOffCenter(camera.Viewport, 0, 1);
                    Class.lineEffect.Parameters["Transform"].SetValue(lineTransform);

                    var blackLines = renderedLines.ConvertAll(line =>
                    {
                        line.Color = new Color(Color.Black, line.Color.A);
                        line.Position += new Vector3(1, 1, 0);
                        return line;
                    });
                    foreach (EffectPass pass in Class.lineEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, blackLines.ToArray(), 0, blackLines.Count / 2);
                    }

                    foreach (EffectPass pass in Class.lineEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, renderedLines.ToArray(), 0, renderedLines.Count / 2);
                    }
                    renderedLines.Clear();
                }
            }

            if (renderSettings.drawGrids)
            {
                Runtime.GraphicsDevice.RasterizerState = Class.shapeRaster;
                Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                var viewProjection = Matrix.CreateOrthographicOffCenter(camera.Viewport, 0, 1);
                Class.lineEffect.Parameters["Transform"].SetValue(cameraTransform * viewProjection);

                var grids = new VertexPositionColor[visibleTiles.Width * 2 + visibleTiles.Height * 2 + 4];
                var gridColor = new Color(Color.Gray, 0.3f);
                var sectorColor = renderSettings.drawSectorsOnGrid ? new Color(Color.MediumAquamarine, 0.65f) : gridColor;
                var cameraOffset = -new Vector2(visibleRegion.X % Class.TileSize, visibleRegion.Y % Class.TileSize);
                for (int i = 0; i <= visibleTiles.Width; ++i)
                {
                    var n = i * 2;
                    grids[n].Position = new Vector3(cameraOffset.X + visibleRegion.X + i * Class.TileSize, visibleRegion.Top, 0);
                    grids[n].Color = (int)grids[n].Position.X % Class.SectorPixelSize < Class.TileSize ? sectorColor : gridColor;
                    grids[n + 1] = grids[n];
                    grids[n + 1].Position.Y += visibleRegion.Height;
                }
                for (int i = 0; i <= visibleTiles.Height; ++i)
                {
                    var n = visibleTiles.Width * 2 + i * 2 + 2;
                    grids[n].Position = new Vector3(visibleRegion.Left, cameraOffset.Y + visibleRegion.Y + i * Class.TileSize, 0);
                    grids[n].Color = (int)grids[n].Position.Y % Class.SectorPixelSize < Class.TileSize ? sectorColor : gridColor;
                    grids[n + 1] = grids[n];
                    grids[n + 1].Position.X += visibleRegion.Width;
                }

                foreach (EffectPass pass in Class.lineEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, grids, 0, grids.Length / 2);
                }
            }

            #region present

            Runtime.GraphicsDevice.SetRenderTargets(originalRt);

            Class.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, postEffect);
            Class.spriteBatch.Draw(Class.preRenderTarget, Vector2.Zero, Color.White);
            Class.spriteBatch.End();

            #endregion
        }
    }
}
;