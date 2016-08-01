using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;

namespace Tool
{
    public class DefTab
    {
        public string File { get; set; }
        public object Source { get; set; }

        public bool IsUnsaved { get; set; }
    }

    public partial class MainWindow : Window
    {
        public string WorkingDirectory { get; set; } = "Defs";

        public List<System.Type> CreatableTypes { get; set; }

        public MainWindow()
        {
            CreatableTypes = new List<System.Type>();
            foreach (var ty in Takai.Data.Serializer.RegisteredTypes)
            {
                if (System.Attribute.IsDefined(ty.Value, typeof(Takai.Data.DesignerCreatableAttribute)) && ty.Value.GetConstructor(System.Type.EmptyTypes) != null)
                    CreatableTypes.Add(ty.Value);
            }

            InitializeComponent();

            Takai.Data.Serializer.Serializers[typeof(Microsoft.Xna.Framework.Graphics.Texture2D)] = new Takai.Data.CustomTypeSerializer
            {
                Serialize = (object Source) =>
                {
                    return null;
                },
                Deserialize = (object Source) =>
                {
                    return null;
                }
            };

            LoadTree(WorkingDirectory, null, 2);
            //todo: filesystem watcher
            //todo: folder right click to add new item/make new folder
        }
        
        /// <summary>
        /// Load one or more levels of a tree
        /// </summary>
        /// <param name="BaseDirectory">The base directory to start searching from</param>
        /// <param name="Source">The item source to use (TreeViewItem.Items or TreeView.Items)</param>
        /// <param name="LoadLevels">How many levels to load</param>
        public void LoadTree(string BaseDirectory, ItemCollection Source = null, int LoadLevels = 1)
        {
            if (LoadLevels < 1)
                return;

            Source = Source ?? treeView.Items;

            Source.Clear();
            try
            {
                foreach (string dir in Directory.GetDirectories(BaseDirectory))
                {
                    TreeViewItem item = new TreeViewItem();
                    item.Margin = new Thickness(2);
                    item.Header = dir.Substring(dir.LastIndexOf('\\') + 1);
                    item.Tag = dir;

                    LoadTree(dir, item.Items, LoadLevels - 1);
                    if (item.Items.Count == 0)
                        item.Items.Add(null);
                    else
                        item.IsExpanded = true;

                    item.FontWeight = FontWeights.Normal;
                    item.Expanded += new RoutedEventHandler(ExpandTree);
                    Source.Add(item);
                }
                foreach (string file in Directory.GetFiles(BaseDirectory))
                {
                    TreeViewItem item = new TreeViewItem();
                    item.Margin = new Thickness(2);
                    item.Header = file.Substring(file.LastIndexOf('\\') + 1);
                    item.Tag = file;
                    item.FontWeight = FontWeights.Normal;
                    item.MouseDoubleClick += Item_DoubleClick;
                    Source.Add(item);
                }
            }
            catch { }
        }
        public void ExpandTree(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == null)
                LoadTree(item.Tag.ToString(), item.Items);
        }
        
        public void OpenFile(string File)
        {
            //check if already open
            for (var i = 0; i < tabs.Items.Count; i++)
            {
                if (((DefTab)tabs.Items[i]).File == File)
                {
                    tabs.SelectedIndex = i;
                    return;
                }
            }

            try
            {
                object source;
                using (var stream = new System.IO.StreamReader(File))
                    source = Takai.Data.Serializer.TextDeserialize(stream);

                if (source != null)
                    tabs.SelectedIndex = tabs.Items.Add(new DefTab { File = File, Source = source, IsUnsaved = false });
            }
            catch (System.Exception expt)
            {
                MessageBox.Show(expt.Message, "Error loading " + File, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Confirm saving before exiting/closing. Does not actually close tab
        /// </summary>
        /// <param name="Tab">The tab to save</param>
        /// <returns>True if the tab was saved/not, false if cancelled</returns>
        public bool ConfirmCanClose(DefTab Tab)
        {
            if (Tab != null && Tab.IsUnsaved && Tab.Source != null)
            {
                var prompt = (string.IsNullOrWhiteSpace(Tab.File) ? string.Format("The {0} is unsaved", Tab.Source.GetType().Name) : Tab.File + " is unsaved")
                            + "\nDo you wish to save it before closing?";

                var mb = MessageBox.Show(prompt, "Save before closing?", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (mb == MessageBoxResult.Cancel)
                    return false;

                if (mb == MessageBoxResult.Yes)
                    Save(Tab);
            }
            return true;
        }

        public void Save(DefTab Tab)
        {
            if (Tab == null)
                return;

            if (string.IsNullOrWhiteSpace(Tab.File))
                SaveAs(Tab);
            else
            {
                using (var stream = new System.IO.StreamWriter(Tab.File))
                    Takai.Data.Serializer.TextSerialize(stream, Tab.Source);
                Tab.IsUnsaved = false;
            }
        }

        public void SaveAs(DefTab Tab)
        {
            if (Tab == null)
                return;

            var dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.InitialDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(Tab.File) ? WorkingDirectory : Path.GetDirectoryName(Tab.File));
            dlg.FileName = Path.GetFileName(Tab.File);

            if (dlg.ShowDialog() == true)
            {
                using (var stream = new System.IO.StreamWriter(dlg.FileName))
                    Takai.Data.Serializer.TextSerialize(stream, Tab.Source);

                Tab.File = dlg.FileName;
                Tab.IsUnsaved = false;
            }
        }
        
        private void Item_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var file = ((TreeViewItem)sender).Tag.ToString();
            OpenFile(file);
        }
    }
}
