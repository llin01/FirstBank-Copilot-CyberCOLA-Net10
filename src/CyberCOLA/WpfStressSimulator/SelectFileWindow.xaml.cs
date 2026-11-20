using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfStressSimulator
{
    /// <summary>
    /// SelectFileWindow.xaml的互動邏輯
    /// </summary>
    public partial class SelectFileWindow : Window
    {
        private readonly string[] _filePaths;

        public SelectFileWindow(string[] filePaths)
        {
            InitializeComponent();
            _filePaths = filePaths;
        }

        private void DataGridLoaded(object sender, RoutedEventArgs e)
        {
            // ... Create a List of objects.
            var items = new List<SelectFile>();
            foreach (var filePath in _filePaths)
                items.Add(new SelectFile(false, filePath));


            // ... Assign ItemsSource of DataGrid.
            var grid = sender as DataGrid;
            if (grid != null) grid.ItemsSource = items;
        }

        public List<string> GetFilePaths()
        {
            var returnList = new List<string>();
            foreach (var item in DataGridSelectFile.Items)
            {
                if (item is SelectFile selectFile && selectFile.IsSelect)
                    returnList.Add(selectFile.FilePath);

            }
            return (returnList);
        }


        class SelectFile
        {
            public bool IsSelect { get; set; }
            public string FilePath { get; }

            public SelectFile(bool isSelect, string filePath)
            {
                this.IsSelect = isSelect;
                this.FilePath = filePath;
            }
        }

        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int index = 0; index < DataGridSelectFile?.Items.Count; index++)
            {
                ((SelectFile)DataGridSelectFile.Items[index]).IsSelect = true;
            }

            DataGridSelectFile?.Items.Refresh();
        }

        private void BtnUnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            for (int index = 0; index < DataGridSelectFile?.Items.Count; index++)
            {
                ((SelectFile)DataGridSelectFile.Items[index]).IsSelect = false;
            }

            DataGridSelectFile?.Items.Refresh();
        }
    }
}
