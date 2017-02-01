using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;
using Takai.Input;
using Takai.Graphics;

namespace DyingAndMore.Editor
{
    partial class Editor : Takai.States.GameState
    {
        float startRotation, startScale;
        System.TimeSpan lastBlobTime = System.TimeSpan.Zero;

        void UpdateBlobsMode(GameTime Time)
        {
            if (Time.TotalGameTime > lastBlobTime + System.TimeSpan.FromMilliseconds(50))
            {
                if (InputState.IsButtonDown(MouseButtons.Left) && map.Bounds.Contains(currentWorldPos))
                {
                    var sel = selectors[(int)EditorMode.Blobs] as BlobSelector;
                    map.Spawn(sel.blobs[sel.SelectedItem], currentWorldPos, Vector2.Zero);
                }

                else if (InputState.IsButtonDown(MouseButtons.Right))
                {
                    var mapSz = new Vector2(map.Width, map.Height);
                    var start = Vector2.Clamp((currentWorldPos / map.SectorPixelSize) - Vector2.One, Vector2.Zero, mapSz).ToPoint();
                    var end = Vector2.Clamp((currentWorldPos / map.SectorPixelSize) + Vector2.One, Vector2.Zero, mapSz).ToPoint();

                    for (int y = start.Y; y < end.Y; y++)
                    {
                        for (int x = start.X; x < end.X; x++)
                        {
                            var sect = map.Sectors[y, x];
                            for (var i = 0; i < sect.blobs.Count; i++)
                            {
                                var blob = sect.blobs[i];

                                if (Vector2.DistanceSquared(blob.position, currentWorldPos) < blob.type.Radius * blob.type.Radius)
                                {
                                    sect.blobs[i] = sect.blobs[sect.blobs.Count - 1];
                                    sect.blobs.RemoveAt(sect.blobs.Count - 1);
                                    i--;
                                }
                            }
                        }
                    }
                }

                lastBlobTime = Time.TotalGameTime;
            }
        }

        void DrawBlobsMode()
        {

        }
    }
}