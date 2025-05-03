using System.Windows;
using TestPGD.Models;
using System.Windows.Controls;
using System.ComponentModel;
using System.Reflection;

namespace TestPGD
{
    internal partial class MainWindow : Window
    {
        public MainWindow()
        {          
            InitializeComponent();

            var heightDescriptor = DependencyPropertyDescriptor.FromProperty(RowDefinition.HeightProperty, typeof(ItemsControl));
            heightDescriptor.AddValueChanged(MainGrid.RowDefinitions[2], HeightChanged);    //subscribe to RowHeightChanged event

            DataContext = new ObjectInfoMVVM();
        }

        private void HeightChanged(object? sender, EventArgs e)
        {
            MethodInfo? methodInfo = DataContext.GetType().GetMethod("DrawGraph");
            _ = methodInfo?.Invoke(DataContext, null);

        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var selectedItem = ((DataGrid)sender).SelectedItem;
            ((DataGrid)sender).SelectedItem = null;             //refreshed SelectedItem
            ((DataGrid)sender).SelectedItem = selectedItem;
        }
    }
}