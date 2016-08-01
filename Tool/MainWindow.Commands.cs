using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;

namespace Tool
{
    public partial class MainWindow : Window
    {
        public void CNew(object sender, ExecutedRoutedEventArgs e)
        {
            var ty = e.Parameter as System.Type;
            if (ty == null)
                return;

            tabs.SelectedIndex = tabs.Items.Add(new DefTab { File = null, Source = System.Activator.CreateInstance(ty), IsUnsaved = true });
        }

        private void CShowNew(object sender, ExecutedRoutedEventArgs e)
        {
            fileMenu.IsSubmenuOpen = true;
            newMenu.IsSubmenuOpen = true;
        }
        private void CDuplicate(object sender, ExecutedRoutedEventArgs e)
        {
            var item = tabs.SelectedItem as DefTab;
            if (item == null)
                return;

            tabs.SelectedIndex = tabs.Items.Add(new DefTab { File = null, Source = NClone.Clone.ObjectGraph(item.Source), IsUnsaved = true });
        }

        private void COpen(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.InitialDirectory = Path.GetFullPath(WorkingDirectory);
            dlg.Filter = "Defs files (*.tk)|*.tk|All Files (*.*)|*.*";

            if (dlg.ShowDialog() == true)
                OpenFile(dlg.FileName);
        }
        private void CIsDocOpen(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = tabs.SelectedItem != null && ((DefTab)tabs.SelectedItem).Source != null;
        }

        private void CSave(object sender, ExecutedRoutedEventArgs e)
        {
            var tab = (e.Parameter ?? tabs.SelectedItem) as DefTab;
            Save(tab);
        }
        private void CSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var tab = (e.Parameter ?? tabs.SelectedItem) as DefTab;
            SaveAs(tab);
        }
        private void CClose(object sender, ExecutedRoutedEventArgs e)
        {
            var tab = (e.Parameter ?? tabs.SelectedItem) as DefTab;
            if (ConfirmCanClose(tab))
                tabs.Items.Remove(tab);
        }
        private void CCloseAll(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (DefTab tab in tabs.Items)
            {
                if (!ConfirmCanClose(tab))
                    return;
            }
        }

        private void CExit(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (DefTab tab in tabs.Items)
            {
                if (!ConfirmCanClose(tab))
                    return;
            }

            Close();
        }
    }
    
    public static class UICommands
    {
        public static readonly RoutedUICommand ShowNew = new RoutedUICommand
        (
            "_New",
            "New",
            typeof(UICommands),
            new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control) }
        );

        public static readonly RoutedUICommand New = new RoutedUICommand();

        public static readonly RoutedUICommand Duplicate = new RoutedUICommand
        (
            "_Duplicate",
            "Duplicate",
            typeof(UICommands),
            new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control) } //todo: not working
        );

        public static readonly RoutedUICommand SaveAs = new RoutedUICommand
        (
            "Save _As",
            "SaveAs",
            typeof(UICommands),
            new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) } //todo: not working
        );

        public static readonly RoutedUICommand Close = new RoutedUICommand
        (
            "_Close",
            "Close",
            typeof(UICommands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control) }
        );

        public static readonly RoutedUICommand CloseAll = new RoutedUICommand
        (
            "_Close All",
            "CloseAll",
            typeof(UICommands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Shift) }
        );

        public static readonly RoutedUICommand Exit = new RoutedUICommand
        (
            "E_xit",
            "Exit",
            typeof(UICommands),
            new InputGestureCollection { new KeyGesture(Key.Q, ModifierKeys.Control) }
        );
    }
}