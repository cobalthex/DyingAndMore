using System.IO;
using System.Text.RegularExpressions;
using P = System.IO.Path;

namespace Takai.UI
{
    public class FileList : ItemList<string>
    {
        public string FilterRegex { get; set; }

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
        /// The root directory allowed to search
        /// </summary>
        public string BasePath
        {
            get => _basePath;
            set
            {
                _basePath = P.GetFullPath(value);
                if (!Path.StartsWith(_basePath))
                    Path = _basePath;
            }
        }
        private string _basePath = P.GetFullPath(".");

        [Data.Serializer.Ignored]
        public string SelectedFile => P.Combine(Path, SelectedItem);

        protected void RefreshList(string path)
        {
            Items.Clear();

            if (Path != BasePath)
                Items.Add("« Previous");

            //display folders first
            foreach (var entry in Directory.EnumerateDirectories(path))
                Items.Add(P.GetFileName(entry) + P.DirectorySeparatorChar);

            var regex = new Regex(FilterRegex ?? "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (var entry in Directory.EnumerateFiles(path))
            {
                if (regex.IsMatch(entry))
                    Items.Add("X");// P.GetFileName(entry));
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e.newIndex < 0)
                return;

            var entry = Items[e.newIndex];
            if (entry == "« Previous")
                Path = Directory.GetParent(Path).FullName;
            else if (entry[entry.Length - 1] == P.DirectorySeparatorChar)
                Path = P.Combine(Path, entry);
        }
    }
}
