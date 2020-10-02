using LibBundle;
using LibBundle.Records;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VisualBundle
{
    public partial class MainWindow : Window
    {
        public IndexContainer ic;
        private FileRecord moveF;
        private ItemModel moveD;
        public readonly HashSet<BundleRecord> changed = new HashSet<BundleRecord>();
        public List<BundleRecord> loadedBundles = new List<BundleRecord>();
        public string filtered = "";

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.DispatcherUnhandledException += OnUnhandledException;
        }

        public void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ew = new ErrorWindow();
            var t = new Thread(new ParameterizedThreadStart(ew.ShowError))
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
            ic = new IndexContainer("_.index.bin");
            UpdateBundleList();
        }

        public void UpdateBundleList()
        {
            var root = new FolderModel("Bundles2");
            loadedBundles.Clear();
            View2.Items.Clear();
            foreach (var b in ic.Bundles)
                if (File.Exists(b.Name))
                    BuildTree(root, b.Name, b);
            View1.Items.Clear();
            View1.Items.Add(root);
            View1.ItemContainerGenerator.ContainerFromIndex(0)?.SetValue(TreeViewItem.IsExpandedProperty, true);
            ButtonReplaceAll.IsEnabled = true;
        }

        public ItemModel GetSelectedBundle()
        {
            return (ItemModel)View1.SelectedItem;
        }

        public ItemModel GetSelectedFile()
        {
            return (ItemModel)View2.SelectedItem;
        }

        private void OnView1GotFocus(object sender, RoutedEventArgs e)
        {
            var v2 = GetSelectedFile();
            if (v2 != null)
                SetUnselected(v2);
        }

        public void SetUnselected(ItemModel item, Stack<ItemModel> stack = null)
        {
            if (stack == null)
                stack = new Stack<ItemModel>();
            if (item.Parent == null)
            {
                TreeViewItem tvi = (TreeViewItem)View2.ItemContainerGenerator.ContainerFromItem(item); ;
                while (stack.Count > 0)
                    tvi = (TreeViewItem)tvi.ItemContainerGenerator.ContainerFromItem(stack.Pop());
                tvi.IsSelected = false;
            }
            else
            {
                stack.Push(item);
                SetUnselected(item.Parent, stack);
            }
        }

        private void OnTreeView1SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tvi = GetSelectedBundle();
            if (tvi == null) //No Selected
            {
                View2.Items.Clear();
                ButtonAdd.IsEnabled = false;
                ButtonExport.IsEnabled = false;
                offsetView.Text = "";
                sizeView.Text = "";
                noView.Text = "";
                return;
            }
            var br = tvi.Record as BundleRecord;
            if (br == null) //Selected Directory
            {
                View2.Items.Clear();
                ButtonAdd.IsEnabled = false;
                ButtonExport.IsEnabled = true;
                offsetView.Text = "";
                sizeView.Text = "";
                noView.Text = "";
            }
            else //Selected Bundle File
            {
                if (moveD != null)
                    MoveD(br);
                if (moveF != null)
                    MoveF(br);
                offsetView.Text = br.indexOffset.ToString();
                sizeView.Text = br.UncompressedSize.ToString();
                noView.Text = br.bundleIndex.ToString();
                var root = new FolderModel("Bundles2");
                foreach (var f in br.Files)
                    BuildTree(root, ic.Hashes.ContainsKey(f.Hash) ? ic.Hashes[f.Hash] : null, f);
                View2.Items.Clear();
                View2.Items.Add(root);
                View2.ItemContainerGenerator.ContainerFromIndex(0)?.SetValue(TreeViewItem.IsExpandedProperty, true);
                ButtonAdd.IsEnabled = true;
                ButtonExport.IsEnabled = true;
            }
        }

        private void OnTreeView2SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null) //No Selected
            {
                ButtonReplace.IsEnabled = false;
                ButtonMove.IsEnabled = false;
                ButtonOpen.IsEnabled = false;
                BOffsetView.Text = "";
                IOffsetView.Text = "";
                fSizeView.Text = "";
                return;
            }
            var fr = tvi.Record as FileRecord;
            if (fr == null) //Selected Directory
            {
                ButtonExport.IsEnabled = true;
                ButtonReplace.IsEnabled = false;
                ButtonMove.IsEnabled = true;
                ButtonOpen.IsEnabled = false;
                BOffsetView.Text = "";
                IOffsetView.Text = "";
                fSizeView.Text = "";
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

        public void BuildTree(ItemModel root, string path, object file)
        {
            if (path == null) return;
            if (file is BundleRecord)
            {
                foreach (var f in ((BundleRecord)file).Files)
                    if (ic.Hashes[f.Hash].ToLower().Contains(filtered))
                    {
                        loadedBundles.Add((BundleRecord)file);
                        goto S;
                    }
                return;
            }
            else if (!path.ToLower().Contains(filtered)) return;
         S:
            var SplittedPath = path.Split('/');
            ItemModel parent = root;

            for (int i = 0; i < SplittedPath.Length; i++)
            {
                var name = SplittedPath[i];
                var isFile = (i + 1 == SplittedPath.Length);
                var next = parent.GetChildItem(name);

                if (next is null)
                { //No exist node, Build a new node
                    if (isFile)
                        next = new FileModel(name) { Record = file };
                    else
                        next = new FolderModel(name);
                    parent.AddChildItem(next);
                }
                parent = next;
            }
        }

        private void OnButtonExportClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null)
            {
                tvi = GetSelectedBundle();
                if (tvi == null)
                    return;
                //View1
                var b = tvi.Record as BundleRecord;
                if (b != null) //Selected Bundle File
                {
                    tvi = View2.Items[0] as FolderModel;
                    var sfd = new SaveFileDialog
                    {
                        FileName = tvi.Name + ".dir"
                    };
                    if (sfd.ShowDialog() == true)
                    {
                        var path = Path.GetDirectoryName(sfd.FileName) + "\\" + Path.GetFileNameWithoutExtension(sfd.FileName);
                        var fis = tvi.ChildItems;
                        var s = ((BundleRecord)GetSelectedBundle().Record).Bundle.Read();
                        MessageBox.Show("Exported " + ExportDir(fis, path, s).ToString() + " Files", "Done");
                        s.Close();
                    }
                }
                else //Selected Directory
                {
                    var sfd = new SaveFileDialog
                    {
                        FileName = "Bundles2.dir"
                    };
                    if (sfd.ShowDialog() == true)
                    {
                        var path = Path.GetDirectoryName(sfd.FileName) + "\\" + Path.GetFileNameWithoutExtension(sfd.FileName);
                        var fis = tvi.ChildItems;
                        MessageBox.Show("Exported " + ExportBundleDir(fis, path).ToString() + " Files", "Done");
                    }
                }
            }
            else //View2
            {
                var f = tvi.Record as FileRecord;
                if (f != null) //Selected File
                {
                    var sfd = new SaveFileDialog
                    {
                        FileName = Path.GetFileName(ic.Hashes[f.Hash]),
                        Filter = "All Files|*.*"
                    };
                    if (sfd.ShowDialog() == true)
                    {
                        File.WriteAllBytes(sfd.FileName, f.Read());
                        MessageBox.Show("Saved: " + f.Size + " Bytes" + Environment.NewLine + sfd.FileName, "Done");
                    }
                }
                else //Selected Directory
                {
                    var sfd = new SaveFileDialog
                    {
                        FileName = tvi.Name + ".dir"
                    };
                    if (sfd.ShowDialog() == true)
                    {
                        var path = Path.GetDirectoryName(sfd.FileName) + "\\" + Path.GetFileNameWithoutExtension(sfd.FileName);
                        var fis = tvi.ChildItems;
                        var s = ((BundleRecord)GetSelectedBundle().Record).Bundle.Read();
                        MessageBox.Show("Exported " + ExportDir(fis, path, s).ToString() + " Files", "Done");
                        s.Close();
                    }
                }
            }
        }

        public int ExportDir(ICollection<ItemModel> fis, string path, Stream stream)
        {
            int count = 0;
            Directory.CreateDirectory(path);
            foreach (var fi in fis)
            {
                var fr = fi.Record as FileRecord;
                if (fr == null) // is directory
                {
                    Directory.CreateDirectory(path + "\\" + fi.Name);
                    count += ExportDir(fi.ChildItems, path + "\\" + fi.Name, stream);
                }
                else // is file
                {
                    File.WriteAllBytes(path + "\\" + fi.Name, fr.Read(stream));
                    count++;
                }
            }
            return count;
        }

        public int ExportBundleDir(ICollection<ItemModel> fis, string path)
        {
            int count = 0;
            foreach (var fi in fis)
            {
                var br = fi.Record as BundleRecord;
                if (br == null) // is directory
                    count += ExportBundleDir(fi.ChildItems, path);
                else // is bundle
                {
                    var root = new FolderModel("Bundles2");
                    foreach (var f in br.Files)
                        BuildTree(root, ic.Hashes.ContainsKey(f.Hash) ? ic.Hashes[f.Hash] : null, f);
                    var s = br.Bundle.Read();
                    count += ExportDir(root.ChildItems, path, s);
                    s.Close();
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
                var fbd = OpenBundles2Dialog();
                if (fbd.ShowDialog() == true)
                {
                    var Bundles2_path = Path.GetDirectoryName(fbd.FileName);
                    var fs = Directory.GetFiles(Bundles2_path, "*", SearchOption.AllDirectories);
                    var paths = ic.Hashes.Values;
                    foreach (var f in fs)
                    {
                        var path = f.Remove(0, Bundles2_path.Length + 1).Replace("\\", "/");
                        if (!paths.Contains(path))
                        {
                            MessageBox.Show("The index didn't define the file:" + Environment.NewLine + path, "Error");
                            return;
                        }
                    }
                    foreach (var f in fs)
                    {
                        var path = f.Remove(0, Bundles2_path.Length + 1).Replace("\\", "/");
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
                MessageLabel.Text = "Moving . . .";
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
                MessageLabel.Text = "Moving . . .";
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
            var sw = new SavingWindow();
            var t = new Thread(new ThreadStart(() => {
                try
                {
                    var i = 1;
                    var text = "Saving {0} / " + (changed.Count + 1).ToString() + " bundles . . .";
                    foreach (var br in changed)
                    {
                        Dispatcher.Invoke(() => { sw.TextBlock1.Text = string.Format(text, i); });
                        br.Save(br.Name);
                        i++;
                    }
                    Dispatcher.Invoke(() => { sw.TextBlock1.Text = string.Format(text, i); });
                    ic.Save("_.index.bin");
                    sw.Closing -= sw.OnClosing;
                    Dispatcher.Invoke(sw.Close);
                } catch (Exception ex)
                {
                    Dispatcher.Invoke(() => {
                        var ew = new ErrorWindow();
                        var tr = new Thread(new ParameterizedThreadStart(ew.ShowError))
                        {
                            CurrentUICulture = new System.Globalization.CultureInfo("en-US")
                        };
                        tr.Start(ex);
                        if (ew.ShowDialog() != true) Close();
                    });
                }
            }));
            t.Start();
            sw.ShowDialog();
            MessageBox.Show("Success saved!" + Environment.NewLine + changed.Count.ToString() + " bundle files changed", "Done");
            changed.Clear();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ButtonSave.IsEnabled)
                if (MessageBox.Show("There are unsaved changes" + Environment.NewLine + "Are you sure you want to leave?", "Closing", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                    e.Cancel = true;
        }

        private void ButtonReplaceAllClick(object sender, RoutedEventArgs e)
        {
            var fbd = OpenBundles2Dialog();
            if (fbd.ShowDialog() == true)
            {
                if (MessageBox.Show(
                    "This will replace all files to every loaded bundles (doesn't contain which were filtered)." + Environment.NewLine
                    + "And bundles which weren't loaded won't be changed." + Environment.NewLine
                    + "Are you sure you want to do this?",
                    "Replace All Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.OK)
                {
                    var Bundles2_path = Path.GetDirectoryName(fbd.FileName);
                    var fs = Directory.GetFiles(Bundles2_path, "*", SearchOption.AllDirectories);
                    var paths = ic.Hashes.Values;
                    var count = 0;
                    var size = 0;
                    foreach (var f in fs)
                    {
                        var path = f.Remove(0, Bundles2_path.Length + 1).Replace("\\", "/");
                        if (paths.Contains(path))
                        {
                            var fr = ic.FindFiles[IndexContainer.FNV1a64Hash(path)];
                            var br = fr.bundleRecord;
                            if (loadedBundles.Contains(br))
                            {
                                fr.Write(File.ReadAllBytes(f));
                                changed.Add(br);
                                ++count;
                                size += fr.Size;
                            }
                        }
                    }
                    ButtonSave.IsEnabled = true;
                    MessageBox.Show("Imported " + count.ToString() + " Files." + Environment.NewLine
                        + "Total " + size.ToString() + " Bytes.", "Done");
                }
            }
        }

        private void OnButtonFilterClick(object sender, RoutedEventArgs e)
        {
            filtered = TextBoxFilter.Text.ToLower();
            UpdateBundleList();
        }

        private void OnFilterKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!e.IsRepeat && e.Key == System.Windows.Input.Key.Enter)
                OnButtonFilterClick(sender, null);
        }

        public OpenFileDialog OpenBundles2Dialog()
        {
            var ofd = new OpenFileDialog()
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                Title = "Go Into Bundles2 Folder And Click Open",
                FileName = "(Go Into Bundles2 Folder And Click Open)"
            };
            return ofd;
        }
    }
}