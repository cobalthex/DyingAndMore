using System;
using Takai.Data;
using Takai.Game;

namespace DyingAndMore.Game
{
    //map spawn configurations? (akin to difficulty)

    /// <summary>
    /// A configuration for a game. Akin to a game mode
    /// </summary>
    public class GameConfiguration : Takai.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        //spawn settings
        //aggressiveness
        //ammo settings
        public bool AllowFriendlyFire { get; set; }

        //fixed vs adaptive difficulty
    }

    public class GameStory : Takai.INamedObject
    {
        public string Name { get; set; }
        public string File { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// All of the maps in the story
        /// </summary>
        public string[] MapFiles { get; set; } //todo: lazy load map class
    }

    public class MapChangedEventArgs : EventArgs
    {
        public MapInstance PreviousMap { get; set; }

        public MapChangedEventArgs(MapInstance previousMap)
        {
            PreviousMap = previousMap;
        }
    }

    /// <summary>
    /// The current game being played
    /// </summary>
    [Cache.AlwaysReload]
    public class Game
    {
        /// <summary>
        /// The current map
        /// </summary>
        public MapInstance Map
        {
            get => _map;
            set
            {
                var oldMap = _map;
                if (oldMap != value)
                {
                    _map = value;
                    MapChanged?.Invoke(this, new MapChangedEventArgs(oldMap));
                }
            }
        }
        private MapInstance _map;

        /// <summary>
        /// The current story this game is playing through. <see cref="Map"/> should be part of this story
        /// </summary>
        public GameStory Story { get; set; }

        //gampaign settings
        public GameConfiguration Configuration { get; set; } = new GameConfiguration();

        public event EventHandler<MapChangedEventArgs> MapChanged;

        public enum LoadMapResult
        {
            Success,
            NoStory,
            LastMapInStory,
            MapNotFound,
        }

        /// <summary>
        /// Load the next map in the story. Does nothing if cannot load another map
        /// </summary>
        /// <returns>The result of trying to load the next map</returns>
        public LoadMapResult LoadNextStoryMap()
        {
            if (Story == null)
                return LoadMapResult.NoStory;

            var index = -1;
            if (Map?.Class != null)
            {
                index = Array.IndexOf(Story.MapFiles, Map.Class.File);
                if (index < 0)
                    return LoadMapResult.MapNotFound;
            }

            if (index == Story.MapFiles.Length - 1)
                return LoadMapResult.LastMapInStory;

            try
            {
                var mapClass = Cache.Load<MapClass>(Story.MapFiles[index + 1]);
                mapClass.InitializeGraphics();
                Map = mapClass.Instantiate();
                return LoadMapResult.Success;
            }
            catch
            {
                return LoadMapResult.MapNotFound;
            }
        }
    }

    public class StorySelect : List
    {
        public event EventHandler<GameStory> StorySelected;

        public string Directory
        {
            get => _directory;
            set
            {
                RemoveAllChildren();

                AddChild(new Static("Select a story"));

                _directory = System.IO.Path.Combine(Cache.Root, value);
                foreach (var file in System.IO.Directory.EnumerateFiles(_directory, "*.story.tk", System.IO.SearchOption.AllDirectories))
                {
                    try
                    {
                        var story = Cache.Load<GameStory>(file);

                        var ui = new List
                        {
                            Direction = Direction.Vertical,
                            HorizontalAlignment = Alignment.Stretch,
                            Margin = 10,
                            BorderColor = Microsoft.Xna.Framework.Color.White,
                        };
                        ui.AddChild(new Static(story.Name));
                        ui.AddChild(new Static(story.Description));
                        ui.AddChild(new Static($"{story.MapFiles.Length} map{(story.MapFiles.Length == 1 ? "" : "s")}"));
                        ui.AutoSize(10);
                        ui.Click += delegate
                        {
                            StorySelected?.Invoke(this, story);
                        };
                        AddChild(ui);
                    }
                    catch { }
                }
            }
        }
        private string _directory;

        public StorySelect(string searchDirectory = "Stories")
        {
            Directory = searchDirectory;
        }
    }
}
