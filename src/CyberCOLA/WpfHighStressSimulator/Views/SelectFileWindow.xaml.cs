using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfHighStressSimulator.Views
{
    /// <summary>
    /// Interaction logic for SelectFileWindow.xaml
    /// </summary>
    public partial class SelectFileWindow : Window
    {
        public ObservableCollection<FileSelectorItem> FileItems { get; } = new();

        public SelectFileWindow(string[] filePaths, string transactionType)
        {
            InitializeComponent();

            // Dynamic window icon and header titles depending on selected transaction type
            Title = $"Template Payload Selector - [{transactionType.ToUpper()}]";
            TextBlockTitle.Text = $"Select {transactionType.ToUpper()} Template Payloads";
            TextBlockSubtitle.Text = $"Please check the {transactionType.ToUpper()} packet templates you want to load into the Session queues:";

            foreach (var path in filePaths)
            {
                FileItems.Add(new FileSelectorItem { FilePath = path, IsSelected = true });
            }
            ListBoxFiles.ItemsSource = FileItems;
        }

        public string[] GetFilePaths()
        {
            return FileItems.Where(x => x.IsSelected).Select(x => x.FilePath).ToArray();
        }

        private void ButtonSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in FileItems)
            {
                item.IsSelected = true;
            }
        }

        private void ButtonDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in FileItems)
            {
                item.IsSelected = false;
            }
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// File selector item model implementing INotifyPropertyChanged for real-time check box interaction.
    /// </summary>
    public class FileSelectorItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
