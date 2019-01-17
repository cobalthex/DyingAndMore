using System.IO;
using System.Text.RegularExpressions;
using P = System.IO.Path;

namespace Takai.UI
{
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
                _path = P.GetFullPath(value);
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
                _basePath = P.GetFullPath(value);
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
                HorizontalAlignment = Alignment.Stretch,
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
                    new Data.Binding("@type", "IsEnabled")
                    {
                        Converter = new Data.ConditionalConverter(typeof(DirectoryInfo))
                    }
                }
            });
            template.AddChild(new Static { Size = new Microsoft.Xna.Framework.Vector2(10, 1) });
            template.AddChild(new Static
            {
                HorizontalAlignment = Alignment.Right,
                Color = Microsoft.Xna.Framework.Color.DimGray,
                Bindings = new System.Collections.Generic.List<Data.Binding> {
                    new Data.Binding("LastWriteTime", "Text")
                }
            });
            ItemTemplate = template;

            On(SelectionChangedEvent, OnSelectionChanged);
        }

        protected void RefreshList(string path)
        {
            Items.Clear();

            //display folders first
            foreach (var entry in Directory.EnumerateDirectories(path))
                Items.Add(new DirectoryInfo(entry));

            var regex = new Regex(FilterRegex ?? "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (var entry in Directory.EnumerateFiles(path))
            {
                if (regex.IsMatch(entry))
                    Items.Add(new FileInfo(entry));
            }

            if (Path != BasePath)
            {
                var prevItem = ItemTemplate.CloneHierarchy();
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
