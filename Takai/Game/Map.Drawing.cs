﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System;
using System.Collections.Generic;
using XnaEffect = Microsoft.Xna.Framework.Graphics.Effect;

namespace Takai.Game
{
    public struct Light
    {
        public Graphics.Sprite highlight;
        public Graphics.Sprite glow;
    }

    public partial class MapBaseClass
    {
        internal struct TrailVertex : IVertexType
        {
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new[] {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            });

            public Vector2 position;
            public Color color;
            public Vector3 texcoord; //z is for texture scaling correction (fake depth)

            public TrailVertex(Vector2 position, Color color, Vector3 texcoord)
            {
                this.position = position;
                this.color = color;
                this.texcoord = texcoord;
            }

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
        }

        //todo: should all of these gpu props be static?
        //data below not thread safe

        internal SpriteBatch spriteBatch;
        internal RenderTarget2D preRenderTarget;
        internal RenderTarget2D fluidsRenderTarget;
        internal RenderTarget2D reflectionRenderTarget; //the reflection mask
        internal RenderTarget2D reflectedRenderTarget; //draw all things that should be reflected here
        internal XnaEffect outlineEffect;
        internal XnaEffect fluidEffect;
        internal XnaEffect reflectionEffect; //writes color and reflection information to two render targets
        internal XnaEffect colorEffect;
        internal XnaEffect tilesEffect;
        internal XnaEffect circleEffect;
        internal XnaEffect basicEffect;
        internal XnaEffect lightmapEffect;

        internal TrailVertex[] trailVerts;
        internal DynamicVertexBuffer trailVBuffer;

        internal Texture2D tilesLayoutTexture;
        /// <summary>
        /// GPU stored version of <see cref="CollisionMask"/>
        /// </summary>
        internal Texture2D collisionMaskSDF;

        internal static readonly DepthStencilState StencilWrite = new DepthStencilState
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 1,
            DepthBufferEnable = false,
        };
        internal static readonly DepthStencilState StencilRead = new DepthStencilState
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.LessEqual,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 1,
            DepthBufferEnable = false,
        };

        internal VertexPositionColor[] fullScreenVerts = new[]
        {
            new VertexPositionColor(new Vector3(-1, -1, 0), Color.Transparent),
            new VertexPositionColor(new Vector3( 1, -1, 0), Color.Transparent),
            new VertexPositionColor(new Vector3(-1,  1, 0), Color.Transparent),
            new VertexPositionColor(new Vector3( 1,  1, 0), Color.Transparent)
        };

        internal static readonly RasterizerState WireframeRaster = new RasterizerState
        {
            FillMode = FillMode.WireFrame,
            CullMode = CullMode.None,
            MultiSampleAntiAlias = true,
        };

        internal static readonly RasterizerState ShapeRaster = new RasterizerState
        {
            CullMode = CullMode.None,
            MultiSampleAntiAlias = true,
        };

        internal static readonly BlendState MultiplyBlendState = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            AlphaBlendFunction = BlendFunction.Add
        };

        internal static readonly BlendState FluidBlendState = new BlendState
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };

        internal static readonly BlendState InvertBlendState = new BlendState
        {
            //source.RGB + (dest.RGB * (1 - source.A))

            AlphaBlendFunction = BlendFunction.Add,
            AlphaDestinationBlend = Blend.SourceAlpha,
            AlphaSourceBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorSourceBlend = Blend.SourceAlpha
        };

        //todo: curves (and volumetric curves) (things like rivers/flows)

        /// <summary>
        /// Create/load all of the graphics resources to draw this map
        /// </summary>
        public void InitializeGraphics()
        {
            if (Tileset.texture != null && CollisionMask == null)
                GenerateCollisionMaskCPU();

            if (Runtime.GraphicsDevice == null)
                return;

            if (spriteBatch == null)
            {
                spriteBatch = new SpriteBatch(Runtime.GraphicsDevice);

                OnViewportResized();

//todo: this needs to work with resize
#if OPENGL
                var surfaceFormat = SurfaceFormat.Color;
#else
                var surfaceFormat = SurfaceFormat.Single;
#endif
                tilesLayoutTexture = new Texture2D(Runtime.GraphicsDevice, (int)Util.NextPowerOf2((uint)Width), (int)Util.NextPowerOf2((uint)Height), false, surfaceFormat); //any 32 bit format should do (not unorm)
                PatchTileLayoutTexture(new Rectangle(0, 0, Width, Height));
            }

            //todo: don't hardcode these
            colorEffect = Data.Cache.Load<XnaEffect>("Shaders/Color.mgfx");
            tilesEffect = Data.Cache.Load<XnaEffect>("Shaders/Tiles.mgfx");
            circleEffect = Data.Cache.Load<XnaEffect>("Shaders/Circle.mgfx");
            basicEffect = Data.Cache.Load<XnaEffect>("Shaders/Basic.mgfx");
            outlineEffect = Data.Cache.Load<XnaEffect>("Shaders/Outline.mgfx");
            fluidEffect = Data.Cache.Load<XnaEffect>("Shaders/Fluid.mgfx");
            reflectionEffect = Data.Cache.Load<XnaEffect>("Shaders/Reflection.mgfx");
            lightmapEffect = Data.Cache.Load<XnaEffect>("Shaders/Lightmap.mgfx");

            trailVerts = new TrailVertex[0];
        }

        public void OnViewportResized()
        {
            var dispwidth = Runtime.GraphicsDevice.PresentationParameters.BackBufferWidth;
            var dispheight = Runtime.GraphicsDevice.PresentationParameters.BackBufferHeight;

            if (preRenderTarget != null)
            {
                preRenderTarget.Dispose();
                fluidsRenderTarget.Dispose();
                reflectionRenderTarget.Dispose();
                reflectedRenderTarget.Dispose();
            }
            
            //todo: some of the render targets may be able to be combined

            preRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, dispwidth, dispheight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents, true);
            fluidsRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, dispwidth, dispheight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents, true);
            reflectionRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, dispwidth, dispheight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents, true);
            reflectedRenderTarget = new RenderTarget2D(Runtime.GraphicsDevice, dispwidth, dispheight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents, true);
        }

        public void PatchTileLayoutTexture(Rectangle region)
        {
            region = Rectangle.Intersect(region, TileBounds);
            if (tilesLayoutTexture == null || region.Width < 1 || region.Height < 1)
                return;


            var buf = new uint[region.Width * region.Height];
            for (int y = 0; y < region.Height; ++y)
            {
                for (int x = 0; x < region.Width; ++x)
                    buf[x + y * region.Width] = (uint)Tiles[y + region.Y, x + region.X];
            }
            tilesLayoutTexture.SetData(0, region, buf, 0, buf.Length);
        }
    }

    public struct ScreenFade
    {
        public System.TimeSpan Duration { get; set; }
        public ColorCurve Colors { get; set; }
        public BlendState Blend { get; set; }
    }

    public partial class MapBaseInstance
    {
        public struct MapRenderStats
        {
            public int visibleInactiveFluids;
            public int visibleActiveFluids;
            public int visibleDecals;
            public int trailPointCount;
            public TimeSpan lastFrameDuration;
        }
        public MapRenderStats RenderStats => _renderStats;
        protected MapRenderStats _renderStats;
        internal System.Diagnostics.Stopwatch renderClock = new System.Diagnostics.Stopwatch();

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        public struct CircleVertex : IVertexType
        {
            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new[] {
                new VertexElement( 0, VertexElementFormat.HalfVector2, VertexElementUsage.Position, 0),
                new VertexElement( 4, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement( 8, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 2),
                new VertexElement(12, VertexElementFormat.Color,       VertexElementUsage.Color   , 0),
                new VertexElement(16, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0)
            });

            public HalfVector2 position;
            public HalfVector2 radiusThickness;
            public HalfVector2 dashLengthOffset; //use multiples of Pi (for offset) for smooth transition between beginning/end of circle
            public Color color;
            public HalfVector2 texcoord;

            public CircleVertex(Vector2 position, float radius, float thickness, float dashLength, float dashOffset, Color color, Vector2 texcoord)
            {
                this.position = new HalfVector2(position);
                this.radiusThickness = new HalfVector2(radius, thickness);
                this.dashLengthOffset = new HalfVector2(dashLength, dashOffset);
                this.color = color;
                this.texcoord = new HalfVector2(texcoord);
            }

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
        }

        protected struct RenderedLight
        {
            public Light light;
            public Vector2 position;
            public float angle;
            public TimeSpan spriteElapsedTime;
        }

        //a collection of primatives to draw next frame (for one frame)
        //todo: use dynamic vertex buffer
        protected VertexPositionColor[] renderedLines = new VertexPositionColor[256];
        protected int nextRenderedLineIndex;
        protected CircleVertex[] renderedCircles = new CircleVertex[64];
        protected int nextRenderedCircleIndex;

        protected List<RenderedLight> renderedLights = new List<RenderedLight>(128);

        protected int renderedTrailPointCount = 0;

        /// <summary>
        /// All of the available render settings customizations
        /// </summary>
        public class RenderSettings //class so that it can be reflected
        {
            public bool drawTiles;
            public bool drawTileCollisionMask;
            public bool drawEntities;
            public bool drawFluids;
            public bool drawReflections;
            public bool drawFluidReflectionMask;
            public bool drawDecals;
            public bool drawParticles;
            public bool drawLights;
            public bool drawTrails;
            public bool drawTrailMesh;
            public bool drawTriggers;
            public bool drawLines;
            public bool drawGrids;
            public bool drawSectorsOnGrid;
            public bool drawBordersAroundNonDrawingEntities;
            public bool drawEntityForwardVectors;
            public bool drawEntityHierarchies;
            public bool drawColliders;
            public bool drawPathHeuristic;
            public bool drawScreenEffects;
            public bool drawDebugInfo;

            public RenderSettings()
            {
                SetDefault();
            }

            public void SetDefault()
            {
                drawTileCollisionMask = false;
                drawFluidReflectionMask = false;
                drawTrailMesh = false;
                drawTriggers = false;
                drawGrids = false;
                drawSectorsOnGrid = false;
                drawBordersAroundNonDrawingEntities = false;
                drawEntityForwardVectors = false;
                drawEntityHierarchies = false;
                drawColliders = false;
                drawPathHeuristic = false;
                drawDebugInfo = false;

                drawTiles = true;
                drawEntities = true;
                drawFluids = true;
                drawReflections = true;
                drawDecals = true;
                drawParticles = true;
                drawLights = true;
                drawTrails = true;
                drawLines = true;
                drawScreenEffects = true;
            }

            public RenderSettings Clone()
            {
                return (RenderSettings)MemberwiseClone();
            }
        }

        [Data.Serializer.Ignored]
        public RenderSettings renderSettings = new RenderSettings();

        /// <summary>
        /// The current fade. Ignored if duration <= 0 (counts down)
        /// </summary>
        public ScreenFade currentScreenFade;
        public System.TimeSpan currentScreenFadeElapsedTime; //todo: refactor

        /// <summary>
        /// Draw a line next frame
        /// </summary>
        /// <param name="start">The start position of the line (in map space)</param>
        /// <param name="end">The end position of the line (in map space)</param>
        /// <param name="color">The color of the line</param>
        /// <param name="wingTipsSize">if >0 draw arrow wing tips with this length</param>
        /// <param name="wingTipAngle">The angle of the wing tips, if 0, uses default</param>
        public void DrawLine(Vector2 start, Vector2 end, Color color, float wingTipsSize = 0, float wingTipAngle = 0)
        {
            if (nextRenderedLineIndex >= renderedLines.Length - 6)
            {
                var newRenderedLines = new VertexPositionColor[renderedLines.Length * 2];
                renderedLines.CopyTo(newRenderedLines, 0);
                renderedLines = newRenderedLines;
            }

            renderedLines[nextRenderedLineIndex++] = new VertexPositionColor(new Vector3(start, 0), color);
            renderedLines[nextRenderedLineIndex++] = new VertexPositionColor(new Vector3(end, 0), color);

            if (wingTipsSize > 0)
            {
                var direction = Vector2.Normalize(end - start);

                var transform = wingTipAngle == 0 ? arrowWingTransform : Matrix.CreateRotationZ(wingTipAngle);

                renderedLines[nextRenderedLineIndex++] = new VertexPositionColor(new Vector3(end, 0), color);
                var z = end - (wingTipsSize * Vector2.TransformNormal(direction, transform));
                renderedLines[nextRenderedLineIndex++] = new VertexPositionColor(new Vector3(z, 0), color);

                renderedLines[nextRenderedLineIndex++] = new VertexPositionColor(new Vector3(end, 0), color);
                z = end - (wingTipsSize * Vector2.TransformNormal(direction, Matrix.Invert(transform)));
                renderedLines[nextRenderedLineIndex++] = new VertexPositionColor(new Vector3(z, 0), color);
            }
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
        public void DrawCircle(Vector2 center, float radius, Color color, float thickness = 2, float dashLength = 0, float dashOffset = 0)
        {
            if (nextRenderedCircleIndex >= renderedCircles.Length - 6)
            {
                var newRenderedCircles = new CircleVertex[renderedCircles.Length * 2];
                renderedCircles.CopyTo(newRenderedCircles, 0);
                renderedCircles = newRenderedCircles;
            }

            //switch to instancing? (not necessary for this amount of data)
            renderedCircles[nextRenderedCircleIndex++] = new CircleVertex(center + new Vector2(-radius), radius, thickness, dashLength, dashOffset, color, new Vector2(0));
            renderedCircles[nextRenderedCircleIndex++] = new CircleVertex(center + new Vector2(radius, -radius), radius, thickness, dashLength, dashOffset, color, new Vector2(1, 0));
            renderedCircles[nextRenderedCircleIndex++] = new CircleVertex(center + new Vector2(-radius, radius), radius, thickness, dashLength, dashOffset, color, new Vector2(0, 1));

            renderedCircles[nextRenderedCircleIndex++] = new CircleVertex(center + new Vector2(-radius, radius), radius, thickness, dashLength, dashOffset, color, new Vector2(0, 1));
            renderedCircles[nextRenderedCircleIndex++] = new CircleVertex(center + new Vector2(radius, -radius), radius, thickness, dashLength, dashOffset, color, new Vector2(1, 0));
            renderedCircles[nextRenderedCircleIndex++] = new CircleVertex(center + new Vector2(radius), radius, thickness, dashLength, dashOffset, color, new Vector2(1));
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
            DrawLine(position, tip, color);

            magnitude = MathHelper.Clamp(magnitude * 0.333f, 5, 30);
            DrawLine(tip, tip - (magnitude * Vector2.TransformNormal(direction, arrowWingTransform)), color);
            DrawLine(tip, tip - (magnitude * Vector2.TransformNormal(direction, Matrix.Invert(arrowWingTransform))), color);
        }

        public void DrawX(Vector2 position, float edgeLength, Color color)
        {
            DrawLine(position - new Vector2(edgeLength), position + new Vector2(edgeLength), color);
            DrawLine(position + new Vector2(-edgeLength, edgeLength), position + new Vector2(edgeLength, -edgeLength), color);
        }

        private List<EntityInstance> _drawEntsOutlined = new List<EntityInstance>();
        private HashSet<Trigger> _drawTriggers = new HashSet<Trigger>();

        public struct RenderContext
        {
            public SpriteBatch spriteBatch;

            public Camera camera;
            public Matrix cameraTransform;
            public Matrix projection;
            public Matrix viewTransform;
            public Rectangle visibleSectors;
            public Rectangle visibleRegion;
            public Rectangle visibleTiles;
        }

        public RenderContext GenerateRenderContext(Camera camera)
        {
            var visibleRegion = Rectangle.Intersect(camera.VisibleRegion, Class.Bounds);
            var cameraTransform = camera.Transform;
            var projection = Matrix.CreateOrthographicOffCenter(camera.Viewport, 0, -1);
            return new RenderContext
            {
                spriteBatch = Class.spriteBatch,
                camera = camera,
                cameraTransform = cameraTransform,
                projection = projection,
                viewTransform = cameraTransform * projection,
                visibleRegion = visibleRegion,
                visibleTiles = Rectangle.Intersect(
                    new Rectangle(
                        visibleRegion.X / Class.TileSize,
                        visibleRegion.Y / Class.TileSize,
                        visibleRegion.Width / Class.TileSize + 2,
                        visibleRegion.Height / Class.TileSize + 2
                    ),
                    Class.TileBounds
                ),
                visibleSectors = GetOverlappingSectors(visibleRegion)
            };
        }

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="camera">Where and what to draw</param>
        /// <param name="postEffect">An optional fullscreen post effect to render with</param>
        public virtual void Draw(Camera camera)
        {
            renderClock.Restart();
            _renderStats = new MapRenderStats
            {
                trailPointCount = renderedTrailPointCount
            };

            _drawEntsOutlined.Clear();

            var originalRt = Runtime.GraphicsDevice.GetRenderTargets(); //necessary?
            var context = GenerateRenderContext(camera);

            //Runtime.GraphicsDevice.Viewport = new Viewport(camera.Viewport);
            Runtime.GraphicsDevice.ScissorRectangle = camera.Viewport;
            Runtime.GraphicsDevice.SetRenderTarget(Class.reflectedRenderTarget);
            Runtime.GraphicsDevice.Clear(Color.Transparent);

            if (renderSettings.drawTrails || renderSettings.drawTrailMesh)
                DrawTrails(ref context);

            if (renderSettings.drawEntities)
                DrawEntities(ref context);

            if (renderSettings.drawParticles)
                DrawParticles(ref context);

            Runtime.GraphicsDevice.SetRenderTargets(Class.fluidsRenderTarget, Class.reflectionRenderTarget); //generates garbage?
            Runtime.GraphicsDevice.Clear(Color.Transparent);

            if (renderSettings.drawFluids ||
                renderSettings.drawFluidReflectionMask)
                DrawFluids(ref context);

            //main render
            Runtime.GraphicsDevice.SetRenderTargets(Class.preRenderTarget);

            if (renderSettings.drawTiles && Class.Tileset.texture != null)
                DrawTiles(ref context, Color.White);
            else
                Runtime.GraphicsDevice.Clear(ClearOptions.Stencil, Color.Transparent, 0, 1);

            if (renderSettings.drawPathHeuristic)
                DrawPathHeuristic(ref context);

            if (renderSettings.drawDecals)
                DrawDecals(ref context);

#region Present fluids + reflections

            if (renderSettings.drawFluidReflectionMask)
            {
                context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                context.spriteBatch.Draw(Class.reflectionRenderTarget, Vector2.Zero, Color.White);
                context.spriteBatch.End();
            }

            if (renderSettings.drawFluids)
            {
                //todo: reflections aren't scaled correctly

#if OPENGL
                //topdo: reflections dont work
                Class.fluidEffect.Parameters["Sampler+Mask"].SetValue(Class.reflectionRenderTarget);
                Class.fluidEffect.Parameters["Sampler+Reflection"].SetValue(renderSettings.drawReflections ? Class.reflectedRenderTarget : null);
#else
                Class.fluidEffect.Parameters["Mask"].SetValue(Class.reflectionRenderTarget);
                Class.fluidEffect.Parameters["Reflection"].SetValue(renderSettings.drawReflections ? Class.reflectedRenderTarget : null);
#endif

                context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, MapBaseClass.StencilRead, null, Class.fluidEffect);
                context.spriteBatch.Draw(Class.fluidsRenderTarget, Vector2.Zero, Color.White);
                context.spriteBatch.End();
            }

#endregion

#region present entities (and any other reflected objects)

            context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, MapBaseClass.StencilRead, null);
            context.spriteBatch.Draw(Class.reflectedRenderTarget, new Vector2(camera.Viewport.X, camera.Viewport.Y), Color.White);
            context.spriteBatch.End();

#endregion

            if (renderSettings.drawLights)
                DrawLights(ref context);

            if (renderSettings.drawTriggers)
                DrawTriggers(ref context);

            if (renderSettings.drawLines)
                DrawLines(ref context);

            if (renderSettings.drawGrids)
                DrawGrids(ref context);

            nextRenderedLineIndex = 0;
            nextRenderedCircleIndex = 0;
            renderedLights.Clear();

#region present

            Runtime.GraphicsDevice.SetRenderTargets(originalRt);

            context.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, camera.PostEffect);
            context.spriteBatch.Draw(Class.preRenderTarget, new Vector2(camera.Viewport.X, camera.Viewport.Y), Color.White);
            context.spriteBatch.End();

#endregion

            if (renderSettings.drawScreenEffects)
                DrawScreenEffects(ref context);

            _renderStats.lastFrameDuration = renderClock.Elapsed;
        }

        //todo: many of these iterate over visible sectors in a loop. Possible opportunity to combine

        public void DrawPathHeuristic(ref RenderContext c)
        {
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, c.cameraTransform);

            var mult = 360f / MaxHeuristic;
            for (var y = c.visibleTiles.Top; y < c.visibleTiles.Bottom; ++y)
            {
                for (var x = c.visibleTiles.Left; x < c.visibleTiles.Right; ++x)
                {
                    var path = NavInfo[y, x];
                    if (path.heuristic == uint.MaxValue)
                        continue;

                    Graphics.Primitives2D.DrawFill(
                        c.spriteBatch,
                        Graphics.ColorUtil.ColorFromHSL(path.heuristic * mult, 1, 0.8f, 0.5f),
                        new Rectangle(x * Class.TileSize, y * Class.TileSize, Class.TileSize, Class.TileSize)
                    );
                }
            }

            c.spriteBatch.End();
        }

        public void DrawTiles(ref RenderContext c, Color renderColor)
        {
            //todo: store in vbuf
            float width = Class.TileSize * Class.Width;
            float height = Class.TileSize * Class.Height;

            var sdfScale = new Vector2((float)Class.CollisionMaskSize.X / Class.collisionMaskSDF.Width,
                                       (float)Class.CollisionMaskSize.Y / Class.collisionMaskSDF.Height);

            var verts = new[]
            { // possibly clip  to size and transform uv coords
                 new VertexPositionColorTexture(new Vector3(0, 0, 0), renderColor, new Vector2(0, 0)),
                 new VertexPositionColorTexture(new Vector3(width, 0, 0), renderColor, new Vector2(1, 0)),
                 new VertexPositionColorTexture(new Vector3(0, height, 0), renderColor, new Vector2(0, 1)),
                 new VertexPositionColorTexture(new Vector3(width, height, 0), renderColor, new Vector2(1, 1)),
            };

            Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Runtime.GraphicsDevice.DepthStencilState = MapBaseClass.StencilWrite;
            Runtime.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            //Runtime.GraphicsDevice.ScissorRectangle = c.camera.Viewport;
            //Class.tilesEffect.Parameters["TileSDF"].SetValue(Class.collisionMaskSDF);
            //Class.tilesEffect.Parameters["SDFScale"].SetValue(sdfScale);
            Class.tilesEffect.Parameters["Transform"].SetValue(c.viewTransform);
            foreach (EffectPass pass in Class.tilesEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, verts, 0, 2);
            }

            if (renderSettings.drawTileCollisionMask && Class.CollisionMask != null)
            {
                verts[0].TextureCoordinate = new Vector2(0, 0);
                verts[1].TextureCoordinate = new Vector2(sdfScale.X, 0);
                verts[2].TextureCoordinate = new Vector2(0, sdfScale.Y);
                verts[3].TextureCoordinate = sdfScale;
                Class.basicEffect.Parameters["Transform"].SetValue(c.viewTransform);
                Runtime.GraphicsDevice.BlendState = MapBaseClass.InvertBlendState;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                foreach (EffectPass pass in Class.basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.Textures[0] = Class.collisionMaskSDF; //Tex
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, verts, 0, 2);
                }
            }

            Runtime.GraphicsDevice.ScissorRectangle = Runtime.GraphicsDevice.Viewport.Bounds;
        }

        public void DrawFluids(ref RenderContext c)
        {
            c.spriteBatch.Begin(SpriteSortMode.Deferred, MapBaseClass.FluidBlendState, null, null, null, Class.reflectionEffect, c.cameraTransform);

            //todo: might be double drawing in certain camera spots

            //inactive fluids
            for (var y = c.visibleSectors.Top; y < c.visibleSectors.Bottom; ++y)
            {
                for (var x = c.visibleSectors.Left; x < c.visibleSectors.Right; ++x)
                {
                    foreach (var fluid in Sectors[y, x].fluids)
                    {
                        ++_renderStats.visibleInactiveFluids;
#if OPENGL
                        Class.reflectionEffect.Parameters["Sampler+Reflection"].SetValue(fluid.Class.Reflection);
#else
                        Class.reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);
#endif

                        var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                        c.spriteBatch.Draw(fluid.Class.Texture, fluid.position, null, Color.White, 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
                    }
                }
            }
            //active fluids
            foreach (var fluid in LiveFluids)
            {
                ++_renderStats.visibleActiveFluids;
#if OPENGL
                        Class.reflectionEffect.Parameters["Sampler+Reflection"].SetValue(fluid.Class.Reflection);
#else
                        Class.reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);
#endif

                var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                c.spriteBatch.Draw(fluid.Class.Texture, fluid.position, null, Color.White, 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
            }

            c.spriteBatch.End();
        }

        public void DrawDecals(ref RenderContext c)
        {
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, MapBaseClass.StencilRead, null, null, c.cameraTransform);

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
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, MapBaseClass.StencilRead, null, null, c.cameraTransform);

            foreach (var ent in EnumerateEntitiesInSectors(c.visibleSectors))
            {
                var entPos = ent.WorldPosition;
                if (ent.OutlineColor.A > 0)
                    _drawEntsOutlined.Add(ent);
                else
                {
                    var angle = Util.Angle(ent.WorldForward);

                    foreach (var state in ent.ActiveAnimations)
                    {
                        var stateAngle = state.Class.AlwaysDrawUpright ? 0 : angle;
                        
                        state.Class?.Sprite?.Draw(
                            c.spriteBatch,
                            entPos,
                            stateAngle,
                            ent.TintColor,
                            ent.Transform.M33,
                            state.ElapsedTime
                        );

                        if (state.Class.Light.highlight != null || state.Class.Light.glow != null)
                        {
                            renderedLights.Add(new RenderedLight
                            {
                                light = state.Class.Light,
                                angle = stateAngle,
                                position = entPos,
                                spriteElapsedTime = state.ElapsedTime
                            });
                        }
                    }
                }

                //todo: this should test if sprite is null
                //currently this will never be hit
                if (renderSettings.drawBordersAroundNonDrawingEntities && !System.Linq.Enumerable.Any(ent.ActiveAnimations))
                {
                    var r = ent.Radius / (float)System.Math.Sqrt(2); //apply transform scale?
                    DrawLine(
                        Vector2.Transform(-new Vector2(r, r), ent.Transform),
                        Vector2.Transform( new Vector2(r, r), ent.Transform),
                        Color.Tomato
                    );
                    DrawLine(
                        Vector2.Transform(new Vector2(-r, r), ent.Transform),
                        Vector2.Transform(new Vector2(r, -r), ent.Transform),
                        Color.Tomato
                    );
                    DrawCircle(entPos, ent.Radius, Color.Tomato);
                }

                if (renderSettings.drawColliders)
                {
                    var rect = ent.AxisAlignedBounds;
                    var color = Color.LightBlue;
                    DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top), color);
                    DrawLine(new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom), color);
                    DrawLine(new Vector2(rect.Right, rect.Bottom), new Vector2(rect.Left, rect.Bottom), color);
                    DrawLine(new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Left, rect.Top), color);

                    //DrawCircle(entPos, ent.Radius, Color.Gold); //apply transform scale?
                }

                if (renderSettings.drawEntityForwardVectors)
                    DrawArrow(entPos, ent.WorldForward, ent.Radius * 1.3f, Color.Gold);

                if (renderSettings.drawEntityHierarchies && ent.WorldParent != null)
                    DrawLine(entPos, ent.WorldParent.WorldPosition, Color.LightGreen, 10);

                if (renderSettings.drawDebugInfo)
                {
                    //todo: defer this drawing and draw outside of stencil
                    var textPos = entPos + new Vector2(ent.Radius + 10);
                    //Class.DebugFont.Draw(c.spriteBatch, ent.GetDebugInfo(), textPos, Color.White);
                    //todo
                }
            }

            c.spriteBatch.End();

            //outlined entities
            c.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, MapBaseClass.StencilRead, null, Class.outlineEffect, c.cameraTransform);

            foreach (var ent in _drawEntsOutlined)
            {
                var angle = Util.Angle(ent.WorldForward);

                foreach (var state in ent.ActiveAnimations)
                {
                    if (state.Class?.Sprite == null)
                        continue;

                    var stateAngle = state.Class.AlwaysDrawUpright ? 0 : angle;

                    var sprite = state.Class.Sprite;
                    Class.outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / sprite.Texture.Width, 1.0f / sprite.Texture.Height));

                    var entPos = ent.WorldPosition;
                    state.Class?.Sprite?.Draw(
                        c.spriteBatch,
                        entPos,
                        stateAngle,
                        ent.OutlineColor,
                        ent.Transform.M33,
                        state.ElapsedTime
                    );

                    if (state.Class.Light.highlight != null || state.Class.Light.glow != null)
                    {
                        renderedLights.Add(new RenderedLight
                        {
                            light = state.Class.Light,
                            angle = stateAngle,
                            position = entPos,
                            spriteElapsedTime = state.ElapsedTime
                        });
                    }
                }
            }

            c.spriteBatch.End();
        }

        public void DrawParticles(ref RenderContext c)
        {
            foreach (var p in Particles)
            {
                c.spriteBatch.Begin(SpriteSortMode.Texture, p.Key.Blend, null, MapBaseClass.StencilRead, null, null, c.cameraTransform);

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

        public void DrawLights(ref RenderContext c)
        {
            //draw glow maps first, then highlights on top

            c.spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, null, null, null, null, c.cameraTransform); //todo: blend state
            foreach (var light in renderedLights)
                light.light.glow?.Draw(c.spriteBatch, light.position, light.angle, Color.White, 1, light.spriteElapsedTime);
            c.spriteBatch.End();

            c.spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, null, null, null, null, c.cameraTransform);
            foreach (var light in renderedLights)
                light.light.highlight?.Draw(c.spriteBatch, light.position, light.angle, Color.White, 1, light.spriteElapsedTime);
            c.spriteBatch.End();
        }

        public void DrawTrails(ref RenderContext c)
        {
            if (renderedTrailPointCount < 2)
                return;

            var vertexCount = renderedTrailPointCount * 2;
            if (Class.trailVerts.Length < vertexCount)
            {
                Class.trailVerts = new MapBaseClass.TrailVertex[vertexCount];
                Class.trailVBuffer?.Dispose();
                Class.trailVBuffer = new DynamicVertexBuffer(
                    Runtime.GraphicsDevice,
                    MapBaseClass.TrailVertex.VertexDeclaration,
                    Class.trailVerts.Length,
                    BufferUsage.WriteOnly
                );
            }

            //todo: batch by texture/render state

            int next = 0;
            foreach (var trail in Trails)
            {
                if (trail.Count < 2)
                    continue;

                float x = 0;
                float spriteWidth = 1;
                Vector2 spriteFrameSize = Vector2.One;
                Vector2 spriteFrame = new Vector2();
                if (trail.Class.Sprite != null)
                {
                    spriteWidth = trail.Class.Sprite.Texture.Width;
                    spriteFrameSize = trail.Class.Sprite.GetSizeUV();
                    spriteFrame = trail.Class.Sprite.GetFrameUV(trail.Class.Sprite.GetFrameIndex(ElapsedTime));
                }

                int widIndex = 1, colIndex = 1;
                for (int n = 0; n < trail.Count && next < Class.trailVerts.Length; ++n, next += 2)
                {
                    var i1 = (n + trail.TailIndex) % trail.AllPoints.Count;
                    int i2 = (i1 + 1) % trail.AllPoints.Count;

                    var t = n / (float)trail.Count;

                    var wid = trail.Class.Width.Evaluate(t, ref widIndex);
                    var col = trail.Class.Color.Evaluate(t, ref colIndex);

                    var p = trail.AllPoints[i1];
                    var norm = Util.Ortho(p.direction);

                    Class.trailVerts[next + 0] = new MapBaseClass.TrailVertex(
                        p.location - norm * wid,
                        col,
                        new Vector3(x + spriteFrame.X, spriteFrame.Y, 0/*todo*/)
                    );
                    Class.trailVerts[next + 1] = new MapBaseClass.TrailVertex(
                        p.location + norm * wid,
                        col,
                        new Vector3(x + spriteFrame.X, spriteFrame.Y + spriteFrameSize.Y, 0)
                    );

                    if (trail.Class.SpriteRenderStyle == TrailSpriteRenderStyle.Stretch)
                        x = t * spriteFrameSize.X * trail.Class.SpriteScale;
                    else
                        x += Vector2.Distance(p.location, trail.AllPoints[i2].location) / spriteWidth * trail.Class.SpriteScale;
                    //DrawArrow(p.location, p.direction, 3, Color.Gold);
                }
            }

            Class.trailVBuffer.SetData(Class.trailVerts, 0, vertexCount);
            Runtime.GraphicsDevice.SetVertexBuffer(Class.trailVBuffer);

            Runtime.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Runtime.GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicWrap;

            Class.basicEffect.Parameters["Transform"].SetValue(c.viewTransform);

            if (renderSettings.drawTrails)
            {
                foreach (EffectPass pass in Class.basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    next = 0;
                    foreach (var trail in Trails)
                    {
                        if (trail.Count < 2)
                            continue;

                        Runtime.GraphicsDevice.Textures[0] = trail.Class.Sprite?.Texture ?? Graphics.Primitives2D.Pixel;
                        Runtime.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, next, trail.Count * 2 - 2);
                        next += trail.Count * 2;
                    }
                }
            }

            if (renderSettings.drawTrailMesh)
            {
                Class.colorEffect.Parameters["Transform"].SetValue(c.viewTransform);
                Class.colorEffect.Parameters["ColorMultiplier"].SetValue(Vector4.One);
                Runtime.GraphicsDevice.RasterizerState = MapBaseClass.WireframeRaster;
                foreach (EffectPass pass in Class.colorEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    next = 0;
                    foreach (var trail in Trails)
                    {
                        if (trail.Count < 2)
                            continue;

                        Runtime.GraphicsDevice.Textures[0] = trail.Class.Sprite?.Texture;
                        Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Class.trailVerts, next, trail.Count * 2 - 2);
                        next += trail.Count * 2;
                    }
                }
            }
        }

        public void DrawLines(ref RenderContext c)
        {
            if (nextRenderedCircleIndex > 0)
            {
                Class.circleEffect.Parameters["Transform"].SetValue(c.viewTransform);

                Runtime.GraphicsDevice.RasterizerState = MapBaseClass.ShapeRaster;
                Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;

                foreach (EffectPass pass in Class.circleEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, renderedCircles, 0, nextRenderedCircleIndex / 3);
                }
            }

            if (nextRenderedLineIndex > 0)
            {
                Runtime.GraphicsDevice.RasterizerState = MapBaseClass.ShapeRaster;
                Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                Class.colorEffect.Parameters["Transform"].SetValue(Matrix.CreateTranslation(1, 1, 0) * c.viewTransform);
                Class.colorEffect.Parameters["ColorMultiplier"].SetValue(new Vector4(0, 0, 0, 1));

                //todo: switch to vertex buffer
                foreach (EffectPass pass in Class.colorEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, renderedLines, 0, nextRenderedLineIndex / 2);
                }

                Class.colorEffect.Parameters["Transform"].SetValue(c.viewTransform);
                Class.colorEffect.Parameters["ColorMultiplier"].SetValue(Vector4.One);
                foreach (EffectPass pass in Class.colorEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, renderedLines, 0, nextRenderedLineIndex / 2);
                }
            }
        }

        public void DrawGrids(ref RenderContext c)
        {
            Runtime.GraphicsDevice.RasterizerState = MapBaseClass.ShapeRaster;
            Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Class.colorEffect.Parameters["Transform"].SetValue(c.viewTransform);
            Class.colorEffect.Parameters["ColorMultiplier"].SetValue(Vector4.One);

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

            foreach (EffectPass pass in Class.colorEffect.CurrentTechnique.Passes)
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

        public void DrawScreenEffects(ref RenderContext c)
        {
            //fade effect
            if (currentScreenFadeElapsedTime < currentScreenFade.Duration)
            {
                var t = (float)(currentScreenFadeElapsedTime.TotalMilliseconds / currentScreenFade.Duration.TotalMilliseconds);
                var color = currentScreenFade.Colors.Evaluate(t);
                for (int i = 0; i < Class.fullScreenVerts.Length; ++i)
                    Class.fullScreenVerts[i].Color = color;

                Runtime.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;

                Class.colorEffect.Parameters["Transform"].SetValue(Matrix.Identity);
                Class.colorEffect.Parameters["ColorMultiplier"].SetValue(Vector4.One); //todo: use this instead
                foreach (EffectPass pass in Class.colorEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Class.fullScreenVerts, 0, 2);
                }
            }
        }
    }
}