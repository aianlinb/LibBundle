using LibBundle;
using LibBundle.Records;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace VisualBundle
{
    public partial class MainWindow : Window
    {
        public IndexContainer ic;
        public readonly HashSet<BundleRecord> changed = new HashSet<BundleRecord>();
        public List<BundleRecord> loadedBundles = new List<BundleRecord>();
        public BackgroundWindow CurrentBackground;
        public string filtered = "";
        public static BitmapFrame file = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("file")));
        public static BitmapFrame dir = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("dir")));
        public static BitmapFrame notexist = BitmapFrame.Create(new MemoryStream((byte[])Properties.Resources.ResourceManager.GetObject("notexist")));

        public MainWindow()
        {
            Application.Current.DispatcherUnhandledException += OnUnhandledException;
            InitializeComponent();
        }

        public void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var ew = new ErrorWindow();
            var t = new Thread(new ParameterizedThreadStart(ew.ShowError))
            {
                CurrentCulture = new System.Globalization.CultureInfo("en-US"),
                CurrentUICulture = new System.Globalization.CultureInfo("en-US")
            };
            t.Start(e.Exception);
            e.Handled = true;
            if (ew.ShowDialog() != true)
            {
                if (CurrentBackground != null)
                {
                    CurrentBackground.Closing -= CurrentBackground.OnClosing;
                    CurrentBackground.Close();
                } 
                Close();
            }
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
                if (ofd.ShowDialog() == true)
                    indexPath = ofd.FileName;
                else
                {
                    Close();
                    return;
                }
            }
            if (Path.GetFileName(indexPath) != "_.index.bin")
            {
                MessageBox.Show("You must select _.index.bin!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            Environment.CurrentDirectory = Path.GetDirectoryName(indexPath);
            ic = new IndexContainer("_.index.bin");
            UpdateBundleList();
        }

        public async void UpdateBundleList()
        {
            var root = new FolderModel("Bundles2");
            loadedBundles.Clear();
            View2.Items.Clear();
            foreach (var b in ic.Bundles)
                if (File.Exists(b.Name))
                    BuildTree(root, b.Name, b);
                else if (ShowAll.IsChecked == true)
                    BuildNotExistTree(root, b.Name, b);
            View1.Items.Clear();
            View1.Items.Add(root);
            ButtonAdd.IsEnabled = true;
            await System.Threading.Tasks.Task.Delay(1); //Update UI
            View1.ItemContainerGenerator.ContainerFromItem(root)?.SetValue(TreeViewItem.IsExpandedProperty, true);
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
                ButtonExport.IsEnabled = true;
                offsetView.Text = "";
                sizeView.Text = "";
                noView.Text = "";
            }
            else //Selected Bundle File
            {
                offsetView.Text = "";
                sizeView.Text = br.UncompressedSize.ToString();
                noView.Text = br.bundleIndex.ToString();
                var root = new FolderModel("Bundles2");
                foreach (var f in br.Files)
                    BuildTree(root, f.path, f);
                View2.Items.Clear();
                View2.Items.Add(root);
                View2.ItemContainerGenerator.ContainerFromIndex(0)?.SetValue(TreeViewItem.IsExpandedProperty, true);
                if (tvi is FileModel)
                    ButtonExport.IsEnabled = true;
                else
                    ButtonExport.IsEnabled = false;
            }
        }

        private void OnTreeView2SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null) //No Selected
            {
                ButtonReplace.IsEnabled = false;
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
                ButtonOpen.IsEnabled = false;
                BOffsetView.Text = "";
                IOffsetView.Text = "";
                fSizeView.Text = "";
            }
            else //Selected File
            {
                BOffsetView.Text = fr.Offset.ToString();
                IOffsetView.Text = "";
                fSizeView.Text = fr.Size.ToString();
                ButtonExport.IsEnabled = true;
                ButtonReplace.IsEnabled = true;
                ButtonOpen.IsEnabled = true;
            }
        }

        public void BuildTree(ItemModel root, string path, object file)
        {
            if (file is BundleRecord record)
            {
                foreach (var f in record.Files)
                    if (f.path.ToLower().Contains(filtered))
                    {
                        loadedBundles.Add(record);
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

        public void BuildNotExistTree(ItemModel root, string path, object file)
        {
            foreach (var f in ((BundleRecord)file).Files)
                if (f.path.ToLower().Contains(filtered))
                    goto S;
            return;
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
                        next = new NotExistModel(name) { Record = file };
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
                if (tvi is NotExistModel)
                {
                    MessageBox.Show("This bundle wasn't loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
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
                        RunBackground(() => {
                            Dispatcher.Invoke(() => { CurrentBackground.Message.Text = "Exporting . . ."; });
                            var s = ((BundleRecord)Dispatcher.Invoke(GetSelectedBundle).Record).Bundle.Read();
                            MessageBox.Show("Exported " + ExportDir(fis, path, s).ToString() + " Files", "Done");
                            s.Close();
                        });
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
                        RunBackground(() => {
                            Dispatcher.Invoke(() => { CurrentBackground.Message.Text = "Exporting . . ."; });
                            MessageBox.Show("Exported " + ExportBundleDir(fis, path).ToString() + " Files", "Done");
                        });
                    }
                }
            }
            else //View2
            {
                if (GetSelectedBundle() is NotExistModel)
                {
                    MessageBox.Show("This bundle wasn't loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var f = tvi.Record as FileRecord;
                if (f != null) //Selected File
                {
                    var sfd = new SaveFileDialog
                    {
                        FileName = Path.GetFileName(f.path),
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
                        RunBackground(() => {
                            Dispatcher.Invoke(() => { CurrentBackground.Message.Text = "Exporting . . ."; });
                            var s = ((BundleRecord)Dispatcher.Invoke(GetSelectedBundle).Record).Bundle.Read();
                            MessageBox.Show("Exported " + ExportDir(fis, path, s).ToString() + " Files", "Done");
                            s.Close();
                        });
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
                if (fi is NotExistModel)
                    continue;
                var br = fi.Record as BundleRecord;
                if (br == null) // is directory
                    count += ExportBundleDir(fi.ChildItems, path);
                else // is bundle
                {
                    var root = new FolderModel("Bundles2");
                    foreach (var f in br.Files)
                        BuildTree(root, f.path, f);
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
                if (GetSelectedBundle() is NotExistModel)
                {
                    MessageBox.Show("This bundle wasn't loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var ofd = new OpenFileDialog { FileName = Path.GetFileName(f.path) };
                if (ofd.ShowDialog() == true)
                {
                    f.Write(File.ReadAllBytes(ofd.FileName));
                    changed.Add(f.bundleRecord);
                    ButtonSave.IsEnabled = true;
                    MessageBox.Show("Imported " + f.Size.ToString() + " Bytes", "Done");
                }
            }
        }
#if Deprecation
        private void OnButtonMoveClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null)
                return;
            var f = tvi.Record as FileRecord;
            if (GetSelectedBundle() is NotExistModel)
            {
                MessageBox.Show("This bundle wasn't loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
            View1.Background = System.Windows.Media.Brushes.LightYellow;
        }
#endif
        private void OnButtonAddClick(object sender, RoutedEventArgs e)
        {
            BundleRecord bundleToSave = (BundleRecord)(((ItemModel)View1.SelectedItem)?.Record);
            if (MessageBox.Show(
                    (bundleToSave == null ? "This will import all files to the smallest of all loaded bundles (doesn't contain which were filtered)." : "This will import all files to \"" + bundleToSave.Name + "\"") + Environment.NewLine
                    + "All files to be imported must be defined by the _.index.bin." + Environment.NewLine
                    + "Are you sure you want to do this?",
                    "Import Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                var fbd = new OpenFolderDialog();
                if (fbd.ShowDialog() == true)
                {
                    var fileNames = Directory.GetFiles(fbd.DirectoryPath, "*", SearchOption.AllDirectories);
                    RunBackground(() =>
                    {
                        Dispatcher.Invoke(() => { CurrentBackground.Message.Text = "Checking files . . ."; });
                        foreach (var f in fileNames)
                        {
                            var path = f.Remove(0, fbd.DirectoryPath.Length + 1).Replace("\\", "/");
                            if (!ic.Paths.Contains(path))
                            {
                                MessageBox.Show("The index didn't define the file:" + Environment.NewLine + path, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        if (bundleToSave == null)
                            bundleToSave = ic.GetSmallestBundle(loadedBundles);

                        string str = "Imported {0}/" + fileNames.Length.ToString() + " Files";
                        int count = 0;
                        foreach (var f in fileNames)
                        {
                            var path = f.Remove(0, fbd.DirectoryPath.Length + 1).Replace("\\", "/");
                            var fr = ic.FindFiles[IndexContainer.FNV1a64Hash(path)];
                            fr.Write(File.ReadAllBytes(f));
                            fr.Move(bundleToSave);
                            ++count;
                            Dispatcher.Invoke(() => { CurrentBackground.Message.Text = string.Format(str, count); });
                        }
                        if (count > 0)
                            changed.Add(bundleToSave);
                    });
                    ButtonSave.IsEnabled = true;
                    MessageBox.Show("Imported " + fileNames.Length.ToString() + " files into " + bundleToSave.Name, "Done");
                }
            }
        }
#if Deprecation
        private void OnButtonMoveClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null)
                return;
            var f = tvi.Record as FileRecord;
            if (GetSelectedBundle() is NotExistModel)
            {
                MessageBox.Show("This bundle wasn't loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
            View1.Background = System.Windows.Media.Brushes.LightYellow;
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
            View1.Background = System.Windows.Media.Brushes.White;
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
            View1.Background = System.Windows.Media.Brushes.White;
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
#endif
        private void OnButtonOpenClick(object sender, RoutedEventArgs e)
        {
            var tvi = GetSelectedFile();
            if (tvi == null)
                return;
            if (tvi is NotExistModel)
            {
                MessageBox.Show("This bundle wasn't loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var f = tvi.Record as FileRecord;
            if (f != null) //Selected File
            {
                var path = Path.GetTempPath() + Path.GetFileName(f.path);
                File.WriteAllBytes(path, f.Read());
                try
                {
                    Process.Start(path).Exited += OnProcessExit;
                } catch (System.ComponentModel.Win32Exception)
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\""));
                }
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
            if (changed.Count > 0)
            {
                RunBackground(() => {
                    var i = 1;
                    var text = "Saving {0} / " + (changed.Count + 1).ToString() + " bundles . . .";
                    foreach (var br in changed)
                    {
                        Dispatcher.Invoke(() => { CurrentBackground.Message.Text = string.Format(text, i); });
                    S:
                        if (!File.Exists(br.Name))
                        {
                            if (MessageBox.Show("File Not Found:" + Environment.NewLine + Path.GetFullPath(br.Name) + "Please put the bundle to the path and click OK", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
                                goto S;
                            else
                            {
                                MessageBox.Show(" Bundles Changed" + "Please restore the backup", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                Close();
                                return;
                            }
                        }
                        br.Save(br.Name);
                        i++;
                    }
                    Dispatcher.Invoke(() => { CurrentBackground.Message.Text = string.Format(text, i); });
                    ic.Save("_.index.bin");
                });
            }
            MessageBox.Show("Success saved!" + Environment.NewLine + changed.Count.ToString() + " bundle files changed" + Environment.NewLine + "Remember to replace all bundles and _index.bin into the ggpk.", "Done");
            changed.Clear();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ButtonSave.IsEnabled)
                if (MessageBox.Show("There are unsaved changes" + Environment.NewLine + "Are you sure you want to leave?", "Closing", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Cancel)
                    e.Cancel = true;
        }
#if Deprecation
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
                    var count = 0;
                    var size = 0;
                    RunBackground(() =>
                    {
                        var Bundles2_path = Path.GetDirectoryName(fbd.FileName);
                        var fs = Directory.GetFiles(Bundles2_path, "*", SearchOption.AllDirectories);
                        foreach (var f in fs)
                        {
                            var path = f.Remove(0, Bundles2_path.Length + 1).Replace("\\", "/");
                            if (!ic.FindFiles.TryGetValue(IndexContainer.FNV1a64Hash(path), out FileRecord fr))
                                continue;
                            var br = fr.bundleRecord;
                            if (loadedBundles.Contains(br))
                            {
                                fr.Write(File.ReadAllBytes(f));
                                changed.Add(br);
                                ++count;
                                size += fr.Size;
                                Dispatcher.Invoke(() => { CurrentBackground.Message.Text = "Replaced " + count.ToString() + " Files"; });
                            }
                        }
                    });
                    ButtonSave.IsEnabled = true;
                    MessageBox.Show("Imported " + count.ToString() + " Files." + Environment.NewLine
                        + "Total " + size.ToString() + " Bytes.", "Done");
                }
            }
        }
#endif
        private void OnButtonFilterClick(object sender, RoutedEventArgs e)
        {
            ButtonFilter.IsEnabled = false;
            filtered = TextBoxFilter.Text.ToLower();
            UpdateBundleList();
        }

        private void OnFilterKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!e.IsRepeat && e.Key == System.Windows.Input.Key.Enter)
                OnButtonFilterClick(sender, null);
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBoxFilter.Text == filtered)
                ButtonFilter.IsEnabled = false;
            else
                ButtonFilter.IsEnabled = true;
        }

        public void RunBackground(Action action)
        {
            CurrentBackground = new BackgroundWindow();
            var t = new System.Threading.Tasks.Task(new Action(() => {
                try
                {
                    action();
                    CurrentBackground.Closing -= CurrentBackground.OnClosing;
                    Dispatcher.Invoke(CurrentBackground.Close);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => {
                        var ew = new ErrorWindow();
                        var tr = new Thread(new ParameterizedThreadStart(ew.ShowError))
                        {
                            CurrentCulture = new System.Globalization.CultureInfo("en-US"),
                            CurrentUICulture = new System.Globalization.CultureInfo("en-US")
                        };
                        tr.Start(ex);
                        if (ew.ShowDialog() != true)
                        {
                            CurrentBackground.Closing -= CurrentBackground.OnClosing;
                            Dispatcher.Invoke(() => {
                                CurrentBackground.Close();
                                Close();
                            });
                        }
                    });
                }
            }));
            t.Start();
            CurrentBackground.ShowDialog();
        }

        private void OnShowAllCheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateBundleList();
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Effects.HasFlag(DragDropEffects.Copy))
                return;
            if (MessageBox.Show("You are about to import the files." + Environment.NewLine
                    + "This will import all files to the smallest bundle of all loaded bundles (doesn't contain which were filtered)." + Environment.NewLine
                    + "The dropped paths must contain Bundles2." + Environment.NewLine
                    + "All files to be imported must be defined by the _.index.bin." + Environment.NewLine
                    + "Are you sure you want to do this?",
                    "Import Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                var droppedFileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
                var checkedPaths = new Dictionary<string, string>();

                int l = loadedBundles[0].UncompressedSize;
                BundleRecord bundleToSave = loadedBundles[0];
                RunBackground(() =>
                {
                    Dispatcher.Invoke(() => { CurrentBackground.Message.Text = "Checking files . . ."; });
                    foreach (var f in droppedFileNames)
                    {
                        if (Directory.Exists(f))
                            foreach (var p in Directory.GetFiles(f, "*", SearchOption.AllDirectories))
                            {
                                var i = p.IndexOf("Bundles2");
                                if (i >= 0 && i + 9 < p.Length)
                                {
                                    var path = p.Substring(i + 9).Replace("\\", "/");
                                    if (!ic.Paths.Contains(path))
                                    {
                                        MessageBox.Show("The index didn't define the file:" + Environment.NewLine + path, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }
                                    checkedPaths.Add(p, path);
                                }
                            }
                        else
                        {
                            var i = f.IndexOf("Bundles2");
                            if (i >= 0 && i + 9 < f.Length)
                            {
                                var path = f.Substring(i + 9).Replace("\\", "/");
                                if (!ic.Paths.Contains(path))
                                {
                                    MessageBox.Show("The index didn't define the file:" + Environment.NewLine + path, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                                checkedPaths.Add(f, path);
                            }
                        }
                    }

                    foreach (var b in loadedBundles)
                    {
                        if (b.UncompressedSize < l)
                        {
                            l = b.UncompressedSize;
                            bundleToSave = b;
                        }
                    }
                    string str = "Imported {0}/" + checkedPaths.Count.ToString() + " Files";
                    int count = 0;
                    foreach (var f in checkedPaths)
                    {
                        var fr = ic.FindFiles[IndexContainer.FNV1a64Hash(f.Value)];
                        fr.Write(File.ReadAllBytes(f.Key));
                        fr.Move(bundleToSave);
                        ++count;
                        Dispatcher.Invoke(() => { CurrentBackground.Message.Text = string.Format(str, count); });
                    }
                    if(count > 0)
                        changed.Add(bundleToSave);
                });
                ButtonSave.IsEnabled = true;
                MessageBox.Show("Imported " + checkedPaths.Count.ToString() + " files into " + bundleToSave.Name, "Done");
            }
        }
    }
}