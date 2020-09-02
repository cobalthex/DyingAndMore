using System.IO;
using System.Text.RegularExpressions;
using P = System.IO.Path;

namespace Takai.UI
{
    //todo: broken in Android

    //todo: convert to use FileInputBase
    public class FileList : ItemList<FileSystemInfo>
    {
        public string FilterRegex { get; set; }

        /// <summary>
        /// The current directory to list the contents of
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
#if ANDROID
                _path = value;
#else
                _path = P.GetFullPath(value);
#endif
                if (!_path.StartsWith(_basePath))
                    _path = _basePath;
                RefreshList(_path);
            }
        }
        private string _path;

        /// <summary>
        /// The root most directory allowed to search
        /// (cannot navigate below this directory)
        /// </summary>
        public string BasePath
        {
            get => _basePath;
            set
            {
#if ANDROID
                _basePath = value;
#else
                _basePath = P.GetFullPath(value);
#endif
                if (Path == null || !Path.StartsWith(_basePath))
                    Path = _basePath;
            }
        }
        private string _basePath = P.GetFullPath(".");

        [Data.Serializer.Ignored]
        public string SelectedFile => SelectedItem == null ? null : P.Combine(Path, SelectedItem.Name);

        public FileList()
        {
            var template = new List
            {
                HorizontalAlignment = Alignment.Stretch, //disable if no-intrinsic sized elements (see Static::Measure)
                Direction = Direction.Horizontal,
                Padding = new Microsoft.Xna.Framework.Vector2(10),
            };
            template.AddChild(new Static
            {
                Bindings = new System.Collections.Generic.List<Data.Binding> {
                    new Data.Binding("Name", "Text")
                }
            });
            template.AddChild(new Static(P.DirectorySeparatorChar.ToString())
            {
                Bindings = new System.Collections.Generic.List<Data.Binding> {
                    new Data.Binding(":type", "IsEnabled")
                    {
                        Converter = new Data.ConditionalConverter(typeof(DirectoryInfo))
                    }
                }
            });

#if !ANDROID
            template.AddChild(new Static { Size = new Microsoft.Xna.Framework.Vector2(10, 1) });
            template.AddChild(new Static
            {
                HorizontalAlignment = Alignment.Right,
                Color = Microsoft.Xna.Framework.Color.DimGray,
                Bindings = new System.Collections.Generic.List<Data.Binding> {
                    new Data.Binding("LastWriteTime", "Text")
                }
            });
#endif
            ItemUI = template;

            On(SelectionChangedEvent, OnSelectionChanged);
        }

        protected void RefreshList(string path)
        {
            var regex = new Regex(FilterRegex ?? "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Items.Clear();

#if ANDROID
            var assets = Takai.Data.Cache.Assets;
            var allFiles = Takai.Data.Cache.Assets.List(path + "/");
            foreach (var file in allFiles)
            {
                var fullPath = P.Combine(path, file);
                if (file.IndexOf('.') == -1) //likely directory
                {
                    Items.Add(new DirectoryInfo(fullPath));
                }
                if (regex.IsMatch(file))
                    Items.Add(new FileInfo(fullPath));
            }
#else
            //display folders first
            foreach (var entry in Directory.EnumerateDirectories(path))
                Items.Add(new DirectoryInfo(entry));

            foreach (var entry in Directory.EnumerateFiles(path))
            {
                if (regex.IsMatch(entry))
                    Items.Add(new FileInfo(entry));
            }
#endif

            if (Path != BasePath)
            {
                var prevItem = ItemUI.CloneHierarchy();
                prevItem.On("Click", delegate (Static sender, UIEventArgs e)
                {
                    Path = Directory.GetParent(Path).FullName;
                    return UIEventResult.Handled;
                });

                //don't hard code this
                foreach (var child in prevItem.FindChildrenWithBinding("Name", null))
                    child.Text = "..";
                Container.InsertChild(prevItem, 0);
            }
        }

        protected UIEventResult OnSelectionChanged(Static sender, UIEventArgs e)
        {
            var sce = (SelectionChangedEventArgs)e;

            if (sce.newIndex < 0)
                return UIEventResult.Continue;

            var index = sce.newIndex;
            if (Path != BasePath)
                --index;

            var entry = Items[index];
            if (entry is DirectoryInfo di)
                Path = P.Combine(Path, di.Name);

            return UIEventResult.Continue;
        }
    }
}
