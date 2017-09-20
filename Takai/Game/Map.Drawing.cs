
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

using XnaEffect = Microsoft.Xna.Framework.Graphics.Effect;

namespace Takai.Game
{
    public partial class Map
    {
        protected struct MapText
        {
            public string text;
            public Vector2 position;
        }

        protected SpriteBatch sbatch;
        protected RenderTarget2D preRenderTarget;
        protected RenderTarget2D fluidsRenderTarget;
        protected RenderTarget2D reflectionRenderTarget; //the reflection mask
        protected RenderTarget2D reflectedRenderTarget; //draw all things that should be reflected here
        protected DepthStencilState stencilWrite, stencilRead;
        protected AlphaTestEffect mapAlphaTest;
        protected XnaEffect outlineEffect;
        protected XnaEffect fluidEffect;
        protected XnaEffect reflectionEffect; //writes color and reflection information to two render targets

        public Texture2D TilesImage { get; set; }

        //a collection of primatives to draw next frame (for one frame)
        protected List<VertexPositionColor> renderedLines = new List<VertexPositionColor>(32);
        protected List<VertexPositionColorTexture> renderedCircles = new List<VertexPositionColorTexture>(32);
        protected List<MapText> renderedText = new List<MapText>(32);

        protected XnaEffect lineEffect;
        protected XnaEffect circleEffect;
        protected RasterizerState shapeRaster;

        /// <summary>
        /// All of the available render settings customizations
        /// </summary>
        public class RenderSettings
        {
            public bool DrawTiles;
            public bool DrawEntities;
            public bool DrawFluids;
            public bool DrawReflections;
            public bool DrawFluidReflectionMask;
            public bool DrawDecals;
            public bool DrawParticles;
            public bool DrawTriggers;
            public bool DrawLines;
            public bool DrawGrids;
            public bool DrawSectorsOnGrid;
            public bool DrawBordersAroundNonDrawingEntities;
            public bool DrawEntBoundingBoxes;

            public static readonly RenderSettings Default = new RenderSettings
            {
                DrawTiles = true,
                DrawEntities = true,
                DrawFluids = true,
                DrawReflections = true,
                DrawDecals = true,
                DrawParticles = true,
                DrawLines = true,
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

        /// <summary>
        /// Create a new map
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use for rendering the map</param>
        public void InitializeGraphics()
        {
            if (Runtime.GraphicsDevice != null)
            {
                sbatch = new SpriteBatch(Runtime.GraphicsDevice);

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

                lineEffect = AssetManager.Load<XnaEffect>("Shaders/DX11/Line.mgfx");
                circleEffect = AssetManager.Load<XnaEffect>("Shaders/DX11/Circle.mgfx");
                outlineEffect = AssetManager.Load<XnaEffect>("Shaders/DX11/Outline.mgfx");
                fluidEffect = AssetManager.Load<XnaEffect>("Shaders/DX11/Fluid.mgfx");
                reflectionEffect = AssetManager.Load<XnaEffect>("Shaders/DX11/Reflection.mgfx");
            }
        }

        //todo: curves (and volumetric curves) (things like rivers/flows)

        private List<EntityInstance> _drawEntsOutlined = new List<EntityInstance>();
        private HashSet<Trigger> _drawTriggers = new HashSet<Trigger>();

        [Data.Serializer.Ignored]
        public string debugOut;

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="Camera">The top-left corner of the visible area</param>
        /// <param name="Viewport">Where on screen to draw</param>
        /// <param name="PostEffect">An optional fullscreen post effect to render with</param>
        /// <param name="DrawSectorEntities">Draw entities in sectors. Typically used for debugging/map editing</param>
        /// <remarks>All rendering management handled internally</remarks>
        public void Draw(Camera Camera = null, XnaEffect PostEffect = null)
        {
            if (Camera == null)
                Camera = ActiveCamera;

            profilingInfo = new MapProfilingInfo();

            #region setup

            _drawEntsOutlined.Clear();

            var originalRt = Runtime.GraphicsDevice.GetRenderTargets();

            var cameraTransform = Camera.Transform;

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

            Runtime.GraphicsDevice.SetRenderTarget(reflectedRenderTarget);
            Runtime.GraphicsDevice.Clear(Color.TransparentBlack);

            if (renderSettings.DrawEntities)
            {
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, null, cameraTransform);

                foreach (var ent in ActiveEnts)
                {
                    if (ent.State.Instance?.Class?.Sprite?.Texture != null)
                    {
                        if (ent.OutlineColor.A > 0)
                            _drawEntsOutlined.Add(ent);
                        else
                        {
                            var angle = ent.Class.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Forward.Y, ent.Forward.X);

                            ent.State.Instance.Class.Sprite.Draw(
                                sbatch,
                                ent.Position,
                                angle,
                                Color.White,
                                1,
                                ent.State.Instance.ElapsedTime
                            );
                        }
                        ++profilingInfo.visibleEnts;
                    }
                    else if (renderSettings.DrawBordersAroundNonDrawingEntities)
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

                    if (renderSettings.DrawEntBoundingBoxes)
                    {
                        var rect = ent.AxisAlignedBounds;
                        var color = Color.LightBlue;
                        DrawLine(new Vector2(rect.Left, rect.Top), new Vector2(rect.Right, rect.Top), color);
                        DrawLine(new Vector2(rect.Right, rect.Top), new Vector2(rect.Right, rect.Bottom), color);
                        DrawLine(new Vector2(rect.Right, rect.Bottom), new Vector2(rect.Left, rect.Bottom), color);
                        DrawLine(new Vector2(rect.Left, rect.Bottom), new Vector2(rect.Left, rect.Top), color);
                    }
                }

                sbatch.End();

                //outlined entities
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, outlineEffect, cameraTransform);

                foreach (var ent in _drawEntsOutlined)
                {
                    //all sprites should be valid

                    //outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / sprite.Texture.Width, 1.0f / sprite.Texture.Height));
                    //outlineEffect.Parameters["FrameSize"].SetValue(new Vector2(sprite.Width, sprite.Height));

                    var angle = ent.Class.AlwaysDrawUpright ? 0 : (float)System.Math.Atan2(ent.Forward.Y, ent.Forward.X);

                    var entPos = ent.Position + (ent.Parent != null ? ent.Parent.Position : Vector2.Zero);
                    ent.State.Instance.Class.Sprite.Draw(
                        sbatch,
                        ent.Position,
                        angle,
                        Color.White,
                        1,
                        ent.State.Instance.ElapsedTime
                    );
                }

                sbatch.End();
            }

            if (renderSettings.DrawParticles)
            {
                foreach (var p in Particles)
                {
                    sbatch.Begin(SpriteSortMode.BackToFront, p.Key.Blend, null, stencilRead, null, null, cameraTransform);

                    for (int i = 0; i < p.Value.Count; ++i)
                    {
                        if (p.Value[i].time == System.TimeSpan.Zero)
                            continue;

                        p.Key.Graphic.Draw
                        (
                            sbatch,
                            p.Value[i].position,
                            p.Value[i].angle,
                            p.Value[i].color,
                            p.Value[i].scale,
                            ElapsedTime - p.Value[i].time
                        );
                    }

                    sbatch.End();
                }
            }

            Runtime.GraphicsDevice.SetRenderTargets(fluidsRenderTarget, reflectionRenderTarget);
            Runtime.GraphicsDevice.Clear(Color.TransparentBlack);

            if (renderSettings.DrawFluids ||
                renderSettings.DrawFluidReflectionMask)
            {
                sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, reflectionEffect, cameraTransform);

                //inactive fluids
                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                    {
                        foreach (var fluid in Sectors[y, x].fluids)
                        {
                            ++profilingInfo.visibleInactiveFluids;
                            reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);

                            var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                            sbatch.Draw(fluid.Class.Texture, fluid.position, null, new Color(1, 1, 1, fluid.Class.Alpha), 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
                        }
                    }
                }
                //active fluids
                foreach (var fluid in ActiveFluids)
                {
                    ++profilingInfo.visibleActiveFluids;
                    reflectionEffect.Parameters["Reflection"].SetValue(fluid.Class.Reflection);

                    var sz = new Vector2(fluid.Class.Texture.Width / 2, fluid.Class.Texture.Height / 2);
                    sbatch.Draw(fluid.Class.Texture, fluid.position, null, new Color(1, 1, 1, fluid.Class.Alpha), 0, sz, fluid.Class.Scale, SpriteEffects.None, 0);
                }

                sbatch.End();
            }

            //main render
            Runtime.GraphicsDevice.SetRenderTargets(preRenderTarget);

            if (renderSettings.DrawTiles)
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

            if (renderSettings.DrawDecals)
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
                            ++profilingInfo.visibleDecals;
                        }
                    }
                }
                sbatch.End();
            }

            #region Present fluids + reflections

            if (renderSettings.DrawFluidReflectionMask)
            {
                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                sbatch.Draw(reflectionRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }

            if (renderSettings.DrawFluids)
            {
                //todo: transform correctly

                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, stencilRead, null, fluidEffect);
                fluidEffect.Parameters["Mask"].SetValue(reflectionRenderTarget);
                fluidEffect.Parameters["Reflection"].SetValue(renderSettings.DrawReflections ? reflectedRenderTarget : null);
                sbatch.Draw(fluidsRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }

            #endregion

            #region present entities (and any other reflected objects)

            sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, stencilRead);
            sbatch.Draw(reflectedRenderTarget, Vector2.Zero, Color.White);
            sbatch.End();

            #endregion

            if (renderSettings.DrawTriggers)
            {
                _drawTriggers.Clear();
                for (var y = visibleSectors.Top; y < visibleSectors.Bottom; ++y)
                {
                    for (var x = visibleSectors.Left; x < visibleSectors.Right; ++x)
                        _drawTriggers.UnionWith(Sectors[y, x].triggers);
                }

                sbatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, null, cameraTransform);
                foreach (var trigger in _drawTriggers)
                    Graphics.Primitives2D.DrawFill(sbatch, new Color(Color.LimeGreen, 0.25f), trigger.Region);
                sbatch.End();
            }

            if (renderSettings.DrawLines)
            {
                if (renderedCircles.Count > 0)
                {
                    Runtime.GraphicsDevice.RasterizerState = shapeRaster;
                    Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    var circleTransform = cameraTransform * Matrix.CreateOrthographicOffCenter(Camera.Viewport, 0, 1);
                    circleEffect.Parameters["Transform"].SetValue(circleTransform);

                    foreach (EffectPass pass in circleEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, renderedCircles.ToArray(), 0, renderedCircles.Count / 3);
                    }

                    renderedCircles.Clear();
                }

                if (renderedLines.Count > 0)
                {
                    Runtime.GraphicsDevice.RasterizerState = shapeRaster;
                    Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                    Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    var lineTransform = cameraTransform * Matrix.CreateOrthographicOffCenter(Camera.Viewport, 0, 1);
                    lineEffect.Parameters["Transform"].SetValue(lineTransform);

                    var blackLines = renderedLines.ConvertAll(line =>
                    {
                        line.Color = new Color(Color.Black, line.Color.A);
                        line.Position += new Vector3(1, 1, 0);
                        return line;
                    });
                    foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, blackLines.ToArray(), 0, blackLines.Count / 2);
                    }

                    foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, renderedLines.ToArray(), 0, renderedLines.Count / 2);
                    }
                    renderedLines.Clear();
                }
            }

            if (renderSettings.DrawGrids)
            {
                Runtime.GraphicsDevice.RasterizerState = shapeRaster;
                Runtime.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                Runtime.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                var viewProjection = Matrix.CreateOrthographicOffCenter(Camera.Viewport, 0, 1);
                lineEffect.Parameters["Transform"].SetValue(cameraTransform * viewProjection);

                var grids = new VertexPositionColor[visibleTiles.Width * 2 + visibleTiles.Height * 2 + 4];
                var gridColor = new Color(Color.Gray, 0.3f);
                var sectorColor = renderSettings.DrawSectorsOnGrid ? new Color(Color.MediumAquamarine, 0.65f) : gridColor;
                var cameraOffset = -new Vector2(visibleRegion.X % TileSize, visibleRegion.Y % TileSize);
                for (int i = 0; i <= visibleTiles.Width; ++i)
                {
                    var n = i * 2;
                    grids[n].Position = new Vector3(cameraOffset.X + visibleRegion.X + i * TileSize, visibleRegion.Top, 0);
                    grids[n].Color = (int)grids[n].Position.X % SectorPixelSize < TileSize ? sectorColor : gridColor;
                    grids[n + 1] = grids[n];
                    grids[n + 1].Position.Y += visibleRegion.Height;
                }
                for (int i = 0; i <= visibleTiles.Height; ++i)
                {
                    var n = visibleTiles.Width * 2 + i * 2 + 2;
                    grids[n].Position = new Vector3(visibleRegion.Left, cameraOffset.Y + visibleRegion.Y + i * TileSize, 0);
                    grids[n].Color = (int)grids[n].Position.Y % SectorPixelSize < TileSize ? sectorColor : gridColor;
                    grids[n + 1] = grids[n];
                    grids[n + 1].Position.X += visibleRegion.Width;
                }

                foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Runtime.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, grids, 0, grids.Length / 2);
                }
            }

            #region present

            Runtime.GraphicsDevice.SetRenderTargets(originalRt);

            sbatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, PostEffect);
            sbatch.Draw(preRenderTarget, Vector2.Zero, Color.White);
            sbatch.End();

            #endregion
        }
    }
}
;