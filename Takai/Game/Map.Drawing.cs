using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Takai.Game
{
    public struct MapProfilingInfo
    {
        public int visibleEnts;
        public int visibleInactiveBlobs;
        public int visibleActiveBlobs;
        public int visibleDecals;
    }

    public partial class Map
    {
        protected GraphicsDevice GraphicsDevice { get; set; }
        protected SpriteBatch sbatch;
        protected RenderTarget2D preRenderTarget;
        protected RenderTarget2D blobsRenderTarget;
        protected RenderTarget2D reflectionRenderTarget; //the reflection mask
        protected RenderTarget2D reflectedRenderTarget; //draw all things that should be reflected here
        protected DepthStencilState stencilWrite, stencilRead;
        protected AlphaTestEffect mapAlphaTest;
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
        
        /// <summary>
        /// Configurable debug options
        /// </summary>
        public struct DebugOptions
        {
            public bool showBlobReflectionMask;
            public bool showOnlyReflections;
        }
        
        public DebugOptions debugOptions;
        
        /// <summary>
        /// Draw a line the next frame
        /// </summary>
        /// <param name="Start">The start position of the line</param>
        /// <param name="End">The end position of the line</param>
        /// <param name="Color">The color of the line</param>
        /// <remarks>Lines are world relative</remarks>
        public void DrawLine(Vector2 Start, Vector2 End, Color Color)
        {
            debugLines.Add(new VertexPositionColor { Position = new Vector3(Start, 0), Color = Color });
            debugLines.Add(new VertexPositionColor { Position = new Vector3(End, 0), Color = Color });
        }

        public MapProfilingInfo ProfilingInfo { get { return profilingInfo; } }
        protected MapProfilingInfo profilingInfo;
        
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

                mapAlphaTest = new AlphaTestEffect(GDevice);
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
        
        //todo: separate debug text
        //todo: curves (and volumetric curves) (things like rivers/flows)

        /// <summary>
        /// Draw the map, centered around the Camera
        /// </summary>
        /// <param name="Camera">The top-left corner of the visible area</param>
        /// <param name="Viewport">Where on screen to draw</param>
        /// <param name="PostEffect">An optional fullscreen post effect to render with</param>
        /// <param name="DrawSectorEntities">Draw entities in sectors. Typically used for debugging/map editing</param>
        /// <remarks>All rendering management handled internally</remarks>
        public void Draw(Matrix Transform, Rectangle Viewport, Effect PostEffect = null)
        {
            profilingInfo = new MapProfilingInfo();

            #region setup

            var invTransform = Matrix.Invert(Transform);

            var invTranslation = invTransform.Translation;
            var scale = invTransform.Scale;
            var rotation = Transform.Rotation;

            var scaleWidth = (int)(Viewport.Width * scale.Z);
            var scaleHeight = (int)(Viewport.Height * scale.Z);
            
            var originalRt = GraphicsDevice.GetRenderTargets();

            var outlined = new List<Entity>();
            
            var startX = (int)invTranslation.X / tileSize;
            var startY = (int)invTranslation.Y / tileSize;

            int endX = startX + 1 + ((scaleWidth - 1) / tileSize);
            int endY = startY + 1 + ((scaleHeight - 1) / tileSize);

            startX = System.Math.Max(startX, 0);
            startY = System.Math.Max(startY, 0);

            endX = System.Math.Min(endX + 1, Width);
            endY = System.Math.Min(endY + 1, Height);

            //visible sector region
            int sStartX = System.Math.Max((startX / SectorSize) - 1, 0);
            int sStartY = System.Math.Max((startY / SectorSize) - 1, 0);
            int sEndX = System.Math.Min(1 + (endX - 1) / SectorSize, Width / SectorSize);
            int sEndY = System.Math.Min(1 + (endY - 1) / SectorSize, Height / SectorSize);

            #endregion

            #region entities

            GraphicsDevice.SetRenderTarget(reflectedRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack);
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, null, Transform);
            
            foreach (var ent in ActiveEnts)
            {
                if (ent.Sprite?.Texture != null)
                {
                    profilingInfo.visibleEnts++;

                    if (ent.OutlineColor.A > 0)
                        outlined.Add(ent);
                    else
                    {
                        var angle = ent.AlwaysUpright ? 0 : (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                        ent.Sprite.Draw(sbatch, ent.Position, angle);
                    }
                }
#if DEBUG
                else
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
#endif
            }

            sbatch.End();

            //outlined entities
            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, outlineEffect, Transform);

            foreach (var ent in outlined)
            {
                outlineEffect.Parameters["TexNormSize"].SetValue(new Vector2(1.0f / ent.Sprite.Texture.Width, 1.0f / ent.Sprite.Texture.Height));
                outlineEffect.Parameters["FrameSize"].SetValue(new Vector2(ent.Sprite.Width, ent.Sprite.Height));

                var angle = ent.AlwaysUpright ? 0 : (float)System.Math.Atan2(ent.Direction.Y, ent.Direction.X);
                ent.Sprite.Draw(sbatch, ent.Position, angle, ent.OutlineColor);
            }

            sbatch.End();

            #endregion

            #region Particles

            foreach (var p in Particles)
            {
                sbatch.Begin(SpriteSortMode.BackToFront, p.Key.BlendMode, null, stencilRead, null, null, Transform);

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
                        p.Value[i].color
                    );
                }

                sbatch.End();
            }

            #endregion
            
            #region blobs

            GraphicsDevice.SetRenderTargets(blobsRenderTarget, reflectionRenderTarget);
            GraphicsDevice.Clear(Color.TransparentBlack);
            sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, reflectionEffect, Transform);

            //inactive blobs
            for (var y = sStartY; y < sEndY; y++)
            {
                for (var x = sStartX; x < sEndX; x++)
                {
                    foreach (var blob in Sectors[y, x].blobs)
                    {
                        profilingInfo.visibleInactiveBlobs++;
                        reflectionEffect.Parameters["Reflection"].SetValue(blob.type.Reflection);
                        sbatch.Draw(blob.type.Texture, blob.position - new Vector2(blob.type.Texture.Width / 2, blob.type.Texture.Height / 2), Color.White);
                    }
                }
            }
            //active blobs
            foreach (var blob in ActiveBlobs)
            {
                profilingInfo.visibleActiveBlobs++;
                reflectionEffect.Parameters["Reflection"].SetValue(blob.type.Reflection);
                sbatch.Draw(blob.type.Texture, blob.position - new Vector2(blob.type.Texture.Width / 2, blob.type.Texture.Height / 2), Color.White);
            }

            sbatch.End();

            #endregion

            //main render
            GraphicsDevice.SetRenderTargets(preRenderTarget);

            #region tiles

            var projection = Matrix.CreateOrthographicOffCenter(Viewport, 0, 1);
            mapAlphaTest.Projection = projection;
            mapAlphaTest.View = Transform;
            sbatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, stencilWrite, null, mapAlphaTest);

            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var tile = Tiles[y, x];
                    if (tile < 0)
                        continue;
                    
                    sbatch.Draw
                    (
                        TilesImage,
                        new Vector2(x * tileSize, y * tileSize),
                        new Rectangle((tile % TilesPerRow) * tileSize, (tile / TilesPerRow) * tileSize, tileSize, tileSize),
                        Color.White
                    );
                }
            }

            sbatch.End();

            #endregion

            #region decals

            sbatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, stencilRead, null, null, Transform);
            
            for (var y = sStartY; y < sEndY; y++)
            {
                for (var x = sStartX; x < sEndX; x++)
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

            #endregion

            #region blob effects

            //draw blobs onto map (with reflections)
            if (debugOptions.showBlobReflectionMask)
            {
                sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                sbatch.Draw(reflectionRenderTarget, Vector2.Zero, Color.White);
                sbatch.End();
            }
            else
            {
                //todo: transform correctly

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

            #endregion

            #region lines

            if (debugLines.Count > 0)
            {
                GraphicsDevice.RasterizerState = lineRaster;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                GraphicsDevice.DepthStencilState = DepthStencilState.None;
                var viewProjection = Matrix.CreateOrthographicOffCenter(GraphicsDevice.Viewport.Bounds, 0, 1);
                lineEffect.Parameters["Transform"].SetValue(Transform * viewProjection);
                foreach (EffectPass pass in lineEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, debugLines.ToArray(), 0, debugLines.Count / 2);
                }
                debugLines.Clear();
            }

            #endregion

            #region present
            
            GraphicsDevice.SetRenderTargets(originalRt);
            
            sbatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, PostEffect);
            sbatch.Draw(preRenderTarget, Vector2.Zero, Color.White);
            sbatch.End();

            #endregion
        }
    }
}
