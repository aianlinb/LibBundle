﻿using System.Diagnostics;
using System.Windows;
namespace VisualBundle
{
    public partial class ErrorWindow : Window
    {
        public ErrorWindow()
        {
            InitializeComponent();
        }

        public void ShowError(object e)
        {
            var ex = e as System.Exception;
            var error = ex.ToString();
            Dispatcher.Invoke(new System.Action(() => {
                ErrorBox.Text = error;
                ButtonCopy.IsEnabled = true;
                ButtonResume.IsEnabled = true;
                ButtonStop.IsEnabled = true;
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
            Closing -= OnClosing;
            Close();
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Closing -= OnClosing;
            Close();
        }
    }
}