using LibBundle;
using LibBundle.Records;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VisualBundle
{
    public partial class MainWindow : Window
    {
        public IndexContainer ic;
        private FileRecord moveF;
        private ItemModel moveD;
        private HashSet<BundleRecord> changed = new HashSet<BundleRecord>();
       
        public MainWindow()
        {
            InitializeComponent();
            Application.Current.DispatcherUnhandledException += OnUnhandledException;
        }

        public void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ew = new ErrorWindow();
            var t = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ew.ShowError))
            {
                CurrentUICulture = new System.Globalization.CultureInfo("en-US")
            };
            t.Start(e.Exception);
            e.Handled = true;
            if (ew.ShowDialog() != true)
                Close();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("LibBundle.dll"))
            {
                MessageBox.Show("File not found: LibBundle.dll", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            if (!File.Exists("oo2core_8_win64.dll"))
            {
                MessageBox.Show("File not found: oo2core_8_win64.dll", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            string indexPath;
            if (Environment.GetCommandLineArgs().Length > 1 && Path.GetFileName(Environment.GetCommandLineArgs()[1]) == "_.index.bin")
                indexPath = Environment.GetCommandLineArgs()[1];
            else
            {
                var ofd = new OpenFileDialog
                {
                    DefaultExt = "bin",
                    FileName = "_.index.bin",
                    Filter = "GGG Bundle index|_.index.bin"
                };
            S:
                if (ofd.ShowDialog() == true)
                {
                    if (ofd.SafeFileName != "_.index.bin")
                    {
                        MessageBox.Show("You must select _.index.bin!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        goto S;
                    }
                    indexPath = ofd.FileName;
                }
                else
                {
                    Close();
                    return;
                }
            }

            Environment.CurrentDirectory = Path.GetDirectoryName(indexPath);
            var root = new FolderModel();
            ic = new IndexContainer("_.index.bin");
            foreach (var b in ic.Bundles)
                if (File.Exists(b.Name))
                    BuildTree(root, b.Name, b);
            foreach (var tvi in root.ChildItems)
                View1.Items.Add(tvi);
        }

        private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
        {
            // handle auto now
            // no need anymore
            /*
            var tvi = e.OriginalSource as ItemModel;
            if (tvi.Items != null)
            {
                tvi.Items.Clear();
                foreach(var c in ((Dictionary<string, TreeViewItem>)tvi.Tag).Values)
                    tvi.Items.Add(c);
            }
            */
        }
        private ItemModel GetSelectedBundle()
        {
            return (ItemModel)View1.SelectedItem;
        }
        private ItemModel GetSelectedFile()
        {
            return (ItemModel)View2.SelectedItem;
        }
        private void OnTreeView1SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tvi = GetSelectedBundle();
            if (tvi == null) //No Selected
            {
                ButtonAdd.IsEnabled = false;
                return;
            }
            var br = tvi.Record as BundleRecord;
            if (br == null) //Selected Directory
                ButtonAdd.IsEnabled = false;
            else //Selected Bundle File
            {
                if (moveD != null)
                    MoveD(br);
                if (moveF != null)
                    MoveF(br);
                offsetView.Text = br.indexOffset.ToString();
                sizeView.Text = br.Size.ToString();
                noView.Text = br.bundleIndex.ToString();
                var root = new FolderModel();
                foreach (var f in br.Files)
                    BuildTree(root, ic.Hashes.ContainsKey(f.Hash) ? ic.Hashes[f.Hash] : null, f);
                View2.Items.Clear();
                foreach (var t in root.ChildItems)
                    View2.Items.Add(t);
                ButtonAdd.IsEnabled = true;
            }
        }

        private void OnTreeView2SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            
            var tvi = GetSelectedFile();
            if (tvi == null) //No Selected
            {
                ButtonExport.IsEnabled = false;
                ButtonReplace.IsEnabled = false;
                ButtonMove.IsEnabled = false;
                ButtonOpen.IsEnabled = false;
                return;
            }
            var fr = tvi.Record as FileRecord;
            if (fr == null) //Selected Directory
            {
                ButtonExport.IsEnabled = true;
                ButtonReplace.IsEnabled = false;
                ButtonMove.IsEnabled = true;
                ButtonOpen.IsEnabled = false;
            }
            else //Selected File
            {
                BOffsetView.Text = fr.Offset.ToString();
                IOffsetView.Text = fr.indexOffset.ToString();
                fSizeView.Text = fr.Size.ToString();
                ButtonExport.IsEnabled = true;
                ButtonReplace.IsEnabled = true;
                ButtonMove.IsEnabled = true;
                ButtonOpen.IsEnabled = true;
            }
            
        }

        public StackPanel TreeItem(string path, ImageSource icon)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Image { Source = icon, Width = 20, Height = 20 });
            sp.Children.Add(new TextBlock { Text = path, FontSize = 16 });
            return sp;
        }

        public void BuildTree(ItemModel root, string path, object file)
        {
            if (path == null) { return; }

            var paths = path.Split('/');
            ItemModel parent = root;

            for (int i = 0; i < paths.Length; i++)
            {
                var name = paths[i];
                var isFile = (i + 1 == paths.Length);
                var next = parent.GetChildItem(name);

                if (next is null)
                {//no exist node
                    // build new node
                    if (isFile)
                    {
                        next = new FileModel(name);
                        next.Record = file;
                    }
                    else
                    {
                        next = new FolderModel(name);
                    }
                    parent.AddChildItem(next);
                }
                parent = next;
            }

        }

        private void OnButtonExportClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null)
                return;
            var f = tvi.Record as FileRecord;
            if (f != null) //Selected File
            {
                var ofd = new SaveFileDialog
                {
                    FileName = Path.GetFileName(ic.Hashes[f.Hash]),
                    Filter = "All Files|*.*"
                };
                if (ofd.ShowDialog() == true)
                {
                    File.WriteAllBytes(ofd.FileName, f.Read());
                    MessageBox.Show("Saved: " + f.Size + " Bytes" + Environment.NewLine + ofd.FileName, "Done");
                }
            }
            else //Selected Directory
            {
                var ofd = new SaveFileDialog
                {
                    FileName = tvi.Name
                };
                if (ofd.ShowDialog() == true)
                {
                    var path = Path.GetFileNameWithoutExtension(ofd.FileName);
                    var fis = tvi.ChildItems;
                    MessageBox.Show("Exported " + ExportDir(fis, path).ToString() + " Files", "Done");
                }
            }
        }

        private int ExportDir(ICollection<ItemModel> fis, string path)
        {
            int count = 0;
            Directory.CreateDirectory(path);
            foreach (var fi in fis)
            {
                var fr = fi.Record as FileRecord;
                if (fr == null) // is directory
                {
                    Directory.CreateDirectory(path + "\\" + fi.Path);
                    count += ExportDir(fi.ChildItems, path + "\\" + fi.Name);
                }
                else // is file
                {
                    File.WriteAllBytes(path + "\\" + fi.Path, fr.Read());
                    count++;
                }
            }
            return count;
        }

        private void OnButtonReplaceClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null)
                return;
            var f = tvi.Record as FileRecord;
            if (f != null) //Selected File
            {
                var ofd = new OpenFileDialog { FileName = Path.GetFileName(ic.Hashes[f.Hash]) };
                if (ofd.ShowDialog() == true)
                {
                    f.Write(File.ReadAllBytes(ofd.FileName));
                    changed.Add(f.bundleRecord);
                    ButtonSave.IsEnabled = true;
                    MessageBox.Show("Imported " + f.Size.ToString() + " Bytes", "Done");
                }
            }
        }

        private void OnButtonMoveClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null)
                return;
            var f = tvi.Record as FileRecord;
            if (f != null) //Selected File
            {
                moveF = f;
                moveD = null;
            }
            else //Selected Directory
            {
                moveF = null;
                moveD = tvi;
            }
            MessageLabel.Text = "Select a bundle you wanna move to";
            View1.Background = Brushes.LightYellow;
        }

        private void OnButtonAddClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedBundle();
            if (tvi == null)
                return;
            var br = tvi.Record as BundleRecord;
            if (br != null) //Selected Bundle File
            {
                var fbd = new System.Windows.Forms.FolderBrowserDialog();
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fs = Directory.GetFiles(fbd.SelectedPath, "*", SearchOption.AllDirectories);
                    var paths = ic.Hashes.Values;
                    foreach (var f in fs)
                    {
                        var path = f.Remove(0, fbd.SelectedPath.Length + 1).Replace("\\", "/");
                        if (!paths.Contains(path))
                        {
                            MessageBox.Show("The index didn't define the file:" + Environment.NewLine + path, "Error");
                            return;
                        }
                    }
                    foreach (var f in fs)
                    {
                        var path = f.Remove(0, fbd.SelectedPath.Length + 1).Replace("\\", "/");
                        var fr = ic.FindFiles[IndexContainer.FNV1a64Hash(path)];
                        fr.Write(File.ReadAllBytes(f));
                        fr.Move(br);
                    }
                    changed.Add(br);
                    ButtonSave.IsEnabled = true;
                    MessageBox.Show("Added " + fs.Length.ToString() + " files to " + br.Name, "Done");
                }
            }
        }

        private void MoveF(BundleRecord br)
        {
            if (MessageBox.Show("Are you sure you want to move " + ic.Hashes[moveF.Hash] + " into " + br.Name + "?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                changed.Add(moveF.bundleRecord);
                changed.Add(br);
                moveF.Move(br);
                MessageBox.Show("Done!", "Done");
                ButtonSave.IsEnabled = true;
            }
            moveF = null;
            MessageLabel.Text = "";
            View1.Background = Brushes.White;
        }

        private void MoveD(BundleRecord br)
        {
            if (MessageBox.Show("Are you sure you want to move directory " + moveD.Name + " into " + br.Name + "?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                MessageBox.Show("Moved " + MoveDir(moveD.ChildItems, br).ToString() + " Files", "Done");
                changed.Add(br);
                ButtonSave.IsEnabled = true;
            }
            moveD = null;
            MessageLabel.Text = "";
            View1.Background = Brushes.White;
        }

        private int MoveDir(ICollection<ItemModel> fis, BundleRecord br)
        {
            int count = 0;
            foreach (var fi in fis)
            {
                var fr = fi.Record as FileRecord;
                if (fr == null) // is directory
                    count += MoveDir(fi.ChildItems, br);
                else // is file
                {
                    fr.Move(br);
                    changed.Add(fr.bundleRecord);
                    count++;
                }
            }
            return count;
        }

        private void OnButtonOpenClick(object sender, RoutedEventArgs e)
        {
            var tvi = View2.SelectedItem as ItemModel;
            if (tvi == null)
                return;
            var f = tvi.Record as FileRecord;
            if (f != null) //Selected File
            {
                var path = Path.GetTempPath() + Path.DirectorySeparatorChar + Path.GetFileName(ic.Hashes[f.Hash]);
                File.WriteAllBytes(path, f.Read());
                Process.Start(path).Exited += OnProcessExit;
            }
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            try
            {
                File.Delete((sender as Process).StartInfo.FileName);
            }
            catch (Exception) { }
        }

        private void OnButtonSaveClick(object sender, RoutedEventArgs e)
        {
            ButtonSave.IsEnabled = false;
            foreach (var br in changed)
                br.Save(br.Name);
            ic.Save("_.index.bin");
            MessageBox.Show("Success saved!" + Environment.NewLine + changed.Count.ToString() + " bundle files changed", "Done");
            changed.Clear();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ButtonSave.IsEnabled)
                if (MessageBox.Show("There are unsaved changes" + Environment.NewLine + "Are you sure you want to leave?", "Closing", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                    e.Cancel = true;
        }
    }
}