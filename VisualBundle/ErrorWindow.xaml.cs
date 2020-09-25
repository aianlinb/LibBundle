﻿using System.Diagnostics;
using System.Windows;
namespace VisualBundle
{
    public partial class ErrorWindow : Window
    {
        public ErrorWindow()
        {
            InitializeComponent();
            Closing += OnClosing;
        }

        public void ShowError(object e)
        {
            var ex = e as System.Exception;
            Dispatcher.Invoke(new System.Action(() => {
                ErrorBox.Text = ex.ToString();
                ButtonCopy.IsEnabled = true;
                ButtonResume.IsEnabled = true;
                ButtonStop.IsEnabled = true;
                Closing -= OnClosing;
            }));
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ErrorBox.Text);
        }

        private void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/aianlinb/LibBundle");
        }

        private void OnResumeClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}