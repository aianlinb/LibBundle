using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace VisualBundle
{
    abstract public class ItemModel
    {
        public ItemModel()
        {
            ChildItems = new SortedSet<ItemModel>(new SortComp());
        }
        virtual public ImageSource Icon { get; set; }
        virtual public string Name { get; set; }
        virtual public ItemModel Parent { get; set; }
        virtual public SortedSet<ItemModel> ChildItems { get; set; }
        virtual public object Record { get; set; }
        virtual public string Path
        {
            get
            {
                if (string.IsNullOrEmpty(Parent?.Name))
                    return Name;
                else
                    return Parent.Path + "/" + Name;
            }
        }
        virtual public void AddChildItem(ItemModel Item)
        {
            ChildItems.Add(Item);
            Item.Parent = this;
        }
        virtual public ItemModel GetChildItem(string Name)
        {
            return ChildItems.FirstOrDefault(ItemCollection => ItemCollection.Name == Name);
        }
    }
    public class FolderModel : ItemModel
    {
        override public ImageSource Icon
        {
            get
            {
                 return Properties.Resources.dir;
            }
        }
        public FolderModel() : base()
        {
        }
        public FolderModel(string name) : this()
        {
            Name = name;
        }
    }
    public class FileModel : ItemModel
    {
        override public ImageSource Icon
        {
            get
            {
                return Properties.Resources.file;
            }
        }
        public FileModel()
        {
        }
        public FileModel(string name) : this()
        {
            Name = name;
        }
    }
    public class SortComp : IComparer<ItemModel>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string x, string y);
        virtual public int Compare(ItemModel x, ItemModel y)
        {
            if (x is FolderModel)
                if (y is FolderModel)
                    return StrCmpLogicalW(x.Name, y.Name);
                else
                    return -1;
            else
                if (y is FolderModel)
                    return 1;
                else
                    return StrCmpLogicalW(x.Name, y.Name);
        }
    }
}