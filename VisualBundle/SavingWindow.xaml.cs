using System.Windows;
namespace VisualBundle
{
    public partial class SavingWindow : Window
    {
        public SavingWindow()
        {
            InitializeComponent();
            Closing += OnClosing;
        }

        internal void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}