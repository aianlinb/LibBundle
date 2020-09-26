using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace VisualBundle
{
    abstract public class ItemModel
    {
        protected ItemModel()
        {
            ChildItems = new ObservableCollection<ItemModel>();
        }
        virtual public ImageSource Icon { get; set; }
        virtual public string Name { get; set; }
        virtual public string Type { get; set; }
        virtual public ItemModel Parent { get; set; }
        virtual public ObservableCollection<ItemModel> ChildItems { get; set; }
        virtual public object Record { get; set; }
        public virtual string Path
        {
            get
            {
                if (string.IsNullOrEmpty(Parent?.Name))
                {
                    return Name;
                }
                return Parent.Path + "/" + Name;
            }
        }
        public void AddChildItem(ItemModel Item)
        {
            ChildItems.Add(Item);
            Item.Parent = this;
        }
        public ItemModel GetChildItem(string Name)
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
            Type = "Folder";
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
            Type = "File";
        }
        public FileModel(string name) : this()
        {
            Name = name;
        }
    }
}