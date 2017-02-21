using Microsoft.Xna.Framework;
using Takai.Input;

namespace DyingAndMore.Editor
{
    class BlobsEditorMode : EditorMode
    {
        System.TimeSpan lastBlobTime = System.TimeSpan.Zero;

        Selectors.BlobSelector selector;

        public BlobsEditorMode(Editor editor)
            : base("Blobs", editor)
        {
            selector = new Selectors.BlobSelector(editor);
            selector.Load();
        }

        public override void OpenConfigurator(bool DidClickOpen)
        {
            selector.DidClickOpen = DidClickOpen;
            Takai.Runtime.GameManager.PushState(selector);
        }

        public override void Update(GameTime time)
        {
            var currentWorldPos = editor.Camera.ScreenToWorld(InputState.MouseVector);

            if (time.TotalGameTime > lastBlobTime + System.TimeSpan.FromMilliseconds(50))
            {
                if (InputState.IsButtonDown(MouseButtons.Left) && editor.Map.Bounds.Contains(currentWorldPos))
                    editor.Map.Spawn(selector.blobs[selector.SelectedItem], currentWorldPos, Vector2.Zero);

                else if (InputState.IsButtonDown(MouseButtons.Right))
                {
                    var mapSz = new Vector2(editor.Map.Width, editor.Map.Height);
                    var start = Vector2.Clamp((currentWorldPos / editor.Map.SectorPixelSize) - Vector2.One, Vector2.Zero, mapSz).ToPoint();
                    var end = Vector2.Clamp((currentWorldPos / editor.Map.SectorPixelSize) + Vector2.One, Vector2.Zero, mapSz).ToPoint();

                    for (int y = start.Y; y < end.Y; ++y)
                    {
                        for (int x = start.X; x < end.X; ++x)
                        {
                            var sect = editor.Map.Sectors[y, x];
                            for (var i = 0; i < sect.blobs.Count; ++i)
                            {
                                var blob = sect.blobs[i];

                                if (Vector2.DistanceSquared(blob.position, currentWorldPos) < blob.type.Radius * blob.type.Radius)
                                {
                                    sect.blobs[i] = sect.blobs[sect.blobs.Count - 1];
                                    sect.blobs.RemoveAt(sect.blobs.Count - 1);
                                    --i;
                                }
                            }
                        }
                    }
                }

                lastBlobTime = time.TotalGameTime;
            }
        }
    }
}