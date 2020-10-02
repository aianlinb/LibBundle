using System.Windows;
namespace VisualBundle
{
    public partial class BackgroundWindow : Window
    {
        public BackgroundWindow()
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