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
        internal XnaEffect basicEffect;
        internal RasterizerState shapeRaster;

        internal VertexPositionColorTexture[] trailVerts;
        internal DynamicVertexBuffer trailVbuffer;

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
                preRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.DiscardContents);
                fluidsRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.DiscardContents);
                reflectionRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.DiscardContents);
                reflectedRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 1, RenderTargetUsage.DiscardContents);
                //todo: some of the render targets may be able to be combined
            }

            lineEffect = Data.Cache.Load<XnaEffect>("Shaders/Line.mgfx");
            circleEffect = Data.Cache.Load<XnaEffect>("Shaders/Circle.mgfx");
            basicEffect = Data.Cache.Load<XnaEffect>("Shaders/Basic.mgfx");
            outlineEffect = Data.Cache.Load<XnaEffect>("Shaders/Outline.mgfx");
            fluidEffect = Data.Cache.Load<XnaEffect>("Shaders/Fluid.mgfx");
            reflectionEffect = Data.Cache.Load<XnaEffect>("Shaders/Reflection.mgfx");

            trailVerts = new VertexPositionColorTexture[512];
            trailVbuffer = new DynamicVertexBuffer(Runtime.GraphicsDevice, VertexPositionColorTexture.VertexDeclaration, trailVerts.Length, BufferUsage.WriteOnly);
        }
    }

    public partial class MapInstance
    {
        public struct MapRenderStats
        {
            public int visibleInactiveFluids;
            public int visibleActiveFluids;
            public int visibleDecals;
        }
        public MapRenderStats RenderStats => _renderStats;
        protected MapRenderStats _renderStats;

        //a collection of primatives to draw next frame (for one frame)
        protected List<VertexPositionColor> renderedLines = new List<VertexPositionColor>(32);
        protected List<VertexPositionColorTexture> renderedCircles = new List<VertexPositionColorTexture>(32);

        protected List<TrailInstance> renderedTrails = new List<TrailInstance>(32);
        protected int renderedTrailPointCount = 0;

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
            public bool drawTrails;
            public bool drawTriggers;
            public bool drawLines;
            public bool drawGrids;
            public bool drawSectorsOnGrid;
            public bool drawBordersAroundNonDrawingEntities;
            public bool drawEntityForwardVectors;
            public bool drawColliders;
            public bool drawPathHeuristic;
            public bool drawDebugInfo;

            public static readonly RenderSettings Default = new RenderSettings
            {
                drawTiles = true,
                drawEntities = true,
                drawFluids = true,
                drawReflections = true,
                drawDecals = true,
                drawParticles = true,
                drawTrails = true,
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

        public void DrawTrail(TrailInstance trail)
        {
            if (trail.Points.Count < 2)
                return;

            renderedTrails.Add(trail);
            renderedTrailPointCount += trail.Points.Count;
        }

        static readonly Matrix arrowWingTransform = Matrix.CreateRotationZ(120);
        /// <summary>
        /// Draw an arrow facing away from a point
        /// </summary>
        /// <param name="position">The origin of the tail of the arrow</param>
        /// <param name="direction">The direction the arrow is facing</param>
        /// <param name="magnitude">How big the arrow should be</param>
        public void DrawArrow(Vector2 position, Vector2 direction, float magnitude, Color color)
        {
            magnitude = System.Math.Max(magnitude, 8);
            var tip = position + (direction * magnitude);
            DrawLine(position, tip, Color.Yellow);

            magnitude = MathHelper.Clamp(magnitude * 0.333f, 5, 30);
            DrawLine(tip, tip - (magnitude * Vector2.Transform(direction, arrowWingTransform)), color);
            DrawLine(tip, tip - (magnitude * Vector2.Transform(direction, Matrix.Invert(arrowWingTransform))), color);
        }


        private List<EntityInstance> _drawEntsOutlined = new List<EntityInstance>();
        private HashSet<Trigger> _drawTriggers = new HashSet<Trigger>();

        public struct RenderContext
        {
            public SpriteBatch spriteBatch;

            public Camera camera;
            public Matrix cameraTransform;
            public Rectangle visibleSectors;
            public Rectangle visibleRegion;
            public Rectangle visibleTiles;
        }

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

            _renderStats = new MapRenderStats();

            _drawEntsOutlined.Clear();

            var originalRt = Runtime.GraphicsDevice.GetRenderTargets();

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

            RenderContext context = new RenderContext
            {
                spriteBatch = Class.spriteBatch,
                camera = camera,
                cameraTransform = camera.Transform,
                visibleRegion = visibleRegion,
                visibleTiles = visibleTiles,
                visibleSectors = visibleSectors
            };

            Runtime.GraphicsDevice.SetRenderTarget(Class.reflectedRenderTarget);
            Runtime.GraphicsDevice.Clear(Color.Transparent);

            if (renderSettings.drawEntities)
                DrawEntities(ref context);

            if (renderSettings.drawTrails)
                DrawTrails(ref context);

            if (renderSettings.drawParticles)
                DrawParticles(ref context);

            Runtime.GraphicsDevice.SetRenderTargets(Class.fluidsRenderTarget, Class.reflectionRenderTarget);
            Runtime.GraphicsDevice.Clear(Color.Transparent);

            if (renderSettings.drawFluids ||
                renderSettings.drawFluidReflectionMask)
                DrawFluids(ref context);

            //main render
            Runtime.GraphicsDevice.SetRenderTargets(Class.preRenderTarget);

            if (renderSettings.drawPathHeuristic)
                DrawPathHeuristic(ref context);
            else if (renderSettings.drawTiles)
                DrawTiles(ref context);
            else
                Runtime.GraphicsDevice.Clear(ClearOptions.Stencil, Color.Transparent, 0, 1);

            if (renderSettings.drawDecals)
                DrawDecals(ref context);

            #region Present fluids + reflections

            if (renderSettings.drawFluidReflectionMask)
            {
                context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                context.spriteBatch.Draw(Class.reflectionRenderTarget, Vector2.Zero, Color.White);
                context.spriteBatch.End();
            }

            if (renderSettings.drawFluids)
            {
                //todo: transform correctly

                context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, Class.stencilRead, null, Class.fluidEffect);
                Class.fluidEffect.Parameters["Mask"].SetValue(Class.reflectionRenderTarget);
                Class.fluidEffect.Parameters["Reflection"].SetValue(renderSettings.drawReflections ? Class.reflectedRenderTarget : null);
                context.spriteBatch.Draw(Class.fluidsRenderTarget, Vector2.Zero, Color.White);
                context.spriteBatch.End();
            }

            #endregion

            #region present entities (and any other reflected objects)

            context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, Class.stencilRead, null);
            context.spriteBatch.Draw(Class.reflectedRenderTarget, Vector2.Zero, Color.White);
            context.spriteBatch.End();

            #endregion

            if (renderSettings.drawTriggers)
                DrawTriggers(ref context);

            if (renderSettings.drawLines)
                DrawLines(ref context);

            if (renderSettings.drawGrids)
                DrawGrids(ref context);

            renderedLines.Clear();
            renderedCircles.Clear();
            renderedTrails.Clear();
            renderedTrailPointCount = 0;

            #region present

            Runtime.GraphicsDevice.SetRenderTargets(originalRt);

            context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, postEffect);
            context.spriteBatch.Draw(Class.preRenderTarget, Vector2.Zero, Color.White);
            context.spriteBatch.End();

            #endregion
        }

        //todo: many of these iterate over visible sectors in a loop. Possible opportunity to combine

        public void DrawPathHeuristic(ref RenderContext c)
        {
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, Class.stencilWrite, null, null, c.cameraTransform);

            var mult = 360f / MaxHeuristic;
            for (var y = c.visibleTiles.Top; y < c.visibleTiles.Bottom; ++y)
            {
                for (var x = c.visibleTiles.Left; x < c.visibleTiles.Right; ++x)
                {
                    var path = PathInfo[y, x];
                    if (path.heuristic == uint.MaxValue)
                        continue;

                    Graphics.Primitives2D.DrawFill(
                        c.spriteBatch,
                        Util.ColorFromHSL(path.heuristic * mult, 1, 0.8f, 1),
                        new Rectangle(x * Class.TileSize, y * Class.TileSize, Class.TileSize, Class.TileSize)
                    );
                }
            }

            c.spriteBatch.End();
        }

        public void DrawTiles(ref RenderContext c)
        {
            var camViewport = c.camera.Viewport;
            var projection = Matrix.CreateOrthographicOffCenter(
                camViewport.Left,
                camViewport.Right,
                camViewport.Bottom,
                camViewport.Top,
                0, 1
            );
            Class.mapAlphaTest.Projection = projection;
            Class.mapAlphaTest.View = c.cameraTransform;
            c.spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, Class.stencilWrite, null, Class.mapAlphaTest);

            for (var y = c.visibleTiles.Top; y < c.visibleTiles.Bottom; ++y)
            {
                for (var x = c.visibleTiles.Left; x < c.visibleTiles.Right; ++x)
                {
                    var tile = Class.Tiles[y, x];
                    if (tile < 0)
                        continue;

                    c.spriteBatch.Draw
                    (
                        Class.TilesImage,
                        new Vector2(x * Class.TileSize, y * Class.TileSize),
                        new Rectangle((tile % Class.TilesPerRow) * Class.TileSize, (tile / Class.TilesPerRow) * Class.TileSize, Class.TileSize, Class.TileSize),
                        Color.White
                    );
                }
            }

            c.spriteBatch.End();
        }

        public void DrawFluids(ref RenderContext c)
        {
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, Class.reflectionEffect, c.cameraTransform);

            //inactive fluids
            for (var y = c.visibleSectors.Top; y < c.visibleSectors.Bottom; ++y)
            {
                for (var x = c.visibleSectors.Left; x < c.visibleSectors.Right; ++x)
                {
                    foreach (var fluid in Sectors[y, x].fluids)
                    {
                        ++_renderStats.visibleInactiveFluids;
                        Class.reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);

                        var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                        c.spriteBatch.Draw(fluid.Class.Texture, fluid.position, null, new Color(1, 1, 1, fluid.Class.Alpha), 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
                    }
                }
            }
            //active fluids
            foreach (var fluid in LiveFluids)
            {
                ++_renderStats.visibleActiveFluids;
                Class.reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);

                var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                c.spriteBatch.Draw(fluid.Class.Texture, fluid.position, null, new Color(1, 1, 1, fluid.Class.Alpha), 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
            }

            c.spriteBatch.End();
        }

        public void DrawDecals(ref RenderContext c)
        {
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, Class.stencilRead, null, null, c.cameraTransform);

            for (var y = c.visibleSectors.Top; y < c.visibleSectors.Bottom; ++y)
            {
                for (var x = c.visibleSectors.Left; x < c.visibleSectors.Right; ++x)
                {
                    foreach (var decal in Sectors[y, x].decals)
                    {
                        c.spriteBatch.Draw
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
                        ++_renderStats.visibleDecals;
                    }
                }
            }

            c.spriteBatch.End();
        }

        public void DrawEntities(ref RenderContext c)
        {
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, Class.stencilRead, null, null, c.cameraTransform);

            foreach (var ent in activeEntities)
            {
                if (ent.OutlineColor.A > 0)
                    _drawEntsOutlined.Add(ent);
                else
                {
                    var angle = ent.Class.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Forward.Y, ent.Forward.X);

                    foreach (var state in ent.ActiveAnimations)
                    {
                        state.Class?.Sprite?.Draw(
                            c.spriteBatch,
                            ent.Position,
                            angle,
                            Color.White,
                            1,
                            state.ElapsedTime
                        );
                    }
                }

                if (renderSettings.drawBordersAroundNonDrawingEntities && !System.Linq.Enumerable.Any(ent.ActiveAnimations))
                {
                    Matrix transform = new Matrix(ent.Forward.X, ent.Forward.Y, 0, 0,
                                                    -ent.Forward.Y, ent.Forward.X, 0, 0,
                                                    0, 0, 1, 0,
                                                    0, 0, 0, 1);

                    var iEntRadius = (int)ent.Radius;
                    Rectangle rect = new Rectangle(-iEntRadius, -iEntRadius, iEntRadius * 2, iEntRadius * 2);
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

                if (renderSettings.drawColliders)
                {
                    var rect = ent.AxisAlignedBounds;
                    var color = Color.LightBlue;
                    DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top), color);
                    DrawLine(new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom), color);
                    DrawLine(new Vector2(rect.Right, rect.Bottom), new Vector2(rect.Left, rect.Bottom), color);
                    DrawLine(new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Left, rect.Top), color);

                    DrawCircle(ent.Position, ent.Radius, Color.Gold);
                }

                if (renderSettings.drawEntityForwardVectors)
                    DrawArrow(ent.Position, ent.Forward, ent.Radius * 1.3f, Color.Gold);
            }

            c.spriteBatch.End();

            //outlined entities
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, Class.stencilRead, null, Class.outlineEffect, c.cameraTransform);

            foreach (var ent in _drawEntsOutlined)
            {
                var angle = ent.Class.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Forward.Y, ent.Forward.X);

                foreach (var state in ent.ActiveAnimations)
                {
                    if (state.Class?.Sprite == null)
                        continue;

                    var sprite = state.Class.Sprite;
                    Class.outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / sprite.Texture.Width, 1.0f / sprite.Texture.Height));
                    Class.outlineEffect.Parameters["FrameSize"].SetValue(new Vector2(sprite.Width, sprite.Height));
                    sprite.Draw(
                        c.spriteBatch,
                        ent.Position,
                        angle,
                        ent.OutlineColor,
                        1,
                        state.ElapsedTime
                    );
                }
            }

            c.spriteBatch.End();
        }

        public void DrawParticles(ref RenderContext c)
        {
            foreach (var p in Particles)
            {
                c.spriteBatch.Begin(SpriteSortMode.BackToFront, p.Key.Blend, null, Class.stencilRead, null, null, c.cameraTransform);

                for (int i = 0; i < p.Value.Count; ++i)
                {
                    if (p.Value[i].spawnTime == System.TimeSpan.Zero)
                        continue;

                    p.Key.Sprite.Draw
                    (
                        c.spriteBatch,
                        p.Value[i].position,
                        p.Value[i].angle + p.Value[i].spin + p.Value[i].spawnAngle,
                        p.Value[i].color,
                        p.Value[i].scale,
                        ElapsedTime - p.Value[i].spawnTime
                    );

                    if (renderSettings.drawColliders)
                        DrawCircle(p.Value[i].position, p.Key.Radius * p.Value[i].scale, new Color(0.8f, 0.9f, 1, 0.5f));
                }

                c.spriteBatch.End();
            }
        }

        RasterizerState rz = new RasterizerState
        {
            FillMode = FillMode.WireFrame,
        };
        public void DrawTrails(ref RenderContext c)
        {
            //auto taper widths?
            if (renderedTrailPointCount < 2)
                return;

            if (Class.trailVerts.Length < renderedTrailPointCount * 2)
            {
                Class.trailVerts = new VertexPositionColorTexture[renderedTrailPointCount * 2];
                Class.trailVbuffer = new DynamicVertexBuffer(Runtime.GraphicsDevice, VertexPositionColorTexture.VertexDeclaration, Class.trailVerts.Length, BufferUsage.WriteOnly);
            }

            foreach (var trail in renderedTrails)
            {
                for (int i = trail.TailIndex, n = 0; i != trail.HeadIndex && n < trail.Points.Count; ++n)
                {
                    var i2 = (i + 1) % trail.Points.Count;

                    var dir = Vector2.Normalize(trail.Points[i2].location - trail.Points[i].location);
                    var norm = new Vector2(dir.Y, -dir.X);

                    float w = trail.Points[i].width;
                    if (trail.Class.AutoTaper)
                        w *= n / (float)trail.Points.Count;

                    Class.trailVerts[n * 2 + 0] = new VertexPositionColorTexture(new Vector3(trail.Points[i].location - norm * w, 0), trail.Class.Color, new Vector2(0, 0));
                    Class.trailVerts[n * 2 + 1] = new VertexPositionColorTexture(new Vector3(trail.Points[i].location + norm * w, 0), trail.Class.Color, new Vector2(0, 1));

                    //if (n < trail.Points.Count - 1)
                    //    DrawLine(trail.Points[i1].location, trail.Points[i2].location, Color.Orange);
                    i = (i + 1) % trail.Points.Count;
                }

                //var v = Class.trailVerts[Class.trailVerts.Length - 2];
                //Class.trailVerts[Class.trailVerts.Length - 2] = Class.trailVerts[Class.trailVerts.Length - 1];
                //Class.trailVerts[Class.trailVerts.Length - 1] = v;
            }

            Class.trailVbuffer.SetData(Class.trailVerts);
            Runtime.GraphicsDevice.SetVertexBuffer(Class.trailVbuffer);

            Runtime.GraphicsDevice.RasterizerState = Class.shapeRaster;
            Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            var camViewport = c.camera.Viewport;
            var cameraTransform = c.cameraTransform * Matrix.CreateOrthographicOffCenter(
                camViewport.Left,
                camViewport.Right,
                camViewport.Bottom,
                camViewport.Top,
                0, 1
            );
            Class.basicEffect.Parameters["Transform"].SetValue(cameraTransform);

            foreach (EffectPass pass in Class.basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int next = 0;
                foreach (var trail in renderedTrails)
                {
                    Runtime.GraphicsDevice.Textures[0] = trail.Class.Sprite?.Texture ?? Graphics.Primitives2D.Pixel;
                    Runtime.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, next, trail.Points.Count * 2 - 2);
                    next += trail.Points.Count * 2;
                }
            }

            //draw mesh
            /*Class.lineEffect.Parameters["Transform"].SetValue(cameraTransform);
            Runtime.GraphicsDevice.RasterizerState = rz;
            foreach (EffectPass pass in Class.lineEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int next = 0;
                foreach (var trail in renderedTrails)
                {
                    Runtime.GraphicsDevice.Textures[0] = trail.Class.Sprite?.Texture;
                    Runtime.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, next, trail.Points.Count * 2 - 2);
                    next += trail.Points.Count * 2;
                }
            }*/
        }

        public void DrawLines(ref RenderContext c)
        {
            var camViewport = c.camera.Viewport;
            if (renderedCircles.Count > 0)
            {
                var circleTransform = c.cameraTransform * Matrix.CreateOrthographicOffCenter( //todo: precalc
                    camViewport.Left,
                    camViewport.Right,
                    camViewport.Bottom,
                    camViewport.Top,
                    0, 1
                );
                Class.circleEffect.Parameters["Transform"].SetValue(circleTransform);

                Runtime.GraphicsDevice.RasterizerState = Class.shapeRaster;
                Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;

                foreach (EffectPass pass in Class.circleEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, renderedCircles.ToArray(), 0, renderedCircles.Count / 3);
                }
            }

            if (renderedLines.Count > 0)
            {
                Runtime.GraphicsDevice.RasterizerState = Class.shapeRaster;
                Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                var lineTransform = c.cameraTransform * Matrix.CreateOrthographicOffCenter(
                    camViewport.Left,
                    camViewport.Right,
                    camViewport.Bottom,
                    camViewport.Top,
                    0, 1
                );
                Class.lineEffect.Parameters["Transform"].SetValue(lineTransform);

                var blackLines = new VertexPositionColor[renderedLines.Count];
                for (int i = 0; i < renderedLines.Count; ++i)
                {
                    blackLines[i].Color = new Color(Color.Black, renderedLines[i].Color.A);
                    blackLines[i].Position = renderedLines[i].Position + new Vector3(1, 1, 0);
                };
                foreach (EffectPass pass in Class.lineEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, blackLines, 0, blackLines.Length / 2);
                }

                foreach (EffectPass pass in Class.lineEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, renderedLines.ToArray(), 0, renderedLines.Count / 2);
                }
            }
        }

        public void DrawGrids(ref RenderContext c)
        {
            var camViewport = c.camera.Viewport;
            Runtime.GraphicsDevice.RasterizerState = Class.shapeRaster;
            Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            var viewProjection = Matrix.CreateOrthographicOffCenter(
                camViewport.Left,
                camViewport.Right,
                camViewport.Bottom,
                camViewport.Top,
                0, 1
            );
            Class.lineEffect.Parameters["Transform"].SetValue(c.cameraTransform * viewProjection);

            var grids = new VertexPositionColor[c.visibleTiles.Width * 2 + c.visibleTiles.Height * 2 + 4];
            var gridColor = new Color(Color.Gray, 0.3f);
            var sectorColor = renderSettings.drawSectorsOnGrid ? new Color(Color.MediumAquamarine, 0.65f) : gridColor;
            var cameraOffset = -new Vector2(c.visibleRegion.X % Class.TileSize, c.visibleRegion.Y % Class.TileSize);
            for (int i = 0; i <= c.visibleTiles.Width; ++i)
            {
                var n = i * 2;
                grids[n].Position = new Vector3(cameraOffset.X + c.visibleRegion.X + i * Class.TileSize, c.visibleRegion.Top, 0);
                grids[n].Color = (int)grids[n].Position.X % Class.SectorPixelSize < Class.TileSize ? sectorColor : gridColor;
                grids[n + 1] = grids[n];
                grids[n + 1].Position.Y += c.visibleRegion.Height;
            }
            for (int i = 0; i <= c.visibleTiles.Height; ++i)
            {
                var n = c.visibleTiles.Width * 2 + i * 2 + 2;
                grids[n].Position = new Vector3(c.visibleRegion.Left, cameraOffset.Y + c.visibleRegion.Y + i * Class.TileSize, 0);
                grids[n].Color = (int)grids[n].Position.Y % Class.SectorPixelSize < Class.TileSize ? sectorColor : gridColor;
                grids[n + 1] = grids[n];
                grids[n + 1].Position.X += c.visibleRegion.Width;
            }

            foreach (EffectPass pass in Class.lineEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, grids, 0, grids.Length / 2);
            }
        }

        public void DrawTriggers(ref RenderContext c)
        {
            _drawTriggers.Clear();
            for (var y = c.visibleSectors.Top; y < c.visibleSectors.Bottom; ++y)
            {
                for (var x = c.visibleSectors.Left; x < c.visibleSectors.Right; ++x)
                    _drawTriggers.UnionWith(Sectors[y, x].triggers);
            }

            c.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, null, c.cameraTransform);
            foreach (var trigger in _drawTriggers)
                Graphics.Primitives2D.DrawFill(c.spriteBatch, new Color(Color.LimeGreen, 0.25f), trigger.Region);
            c.spriteBatch.End();
        }
    }
}
;