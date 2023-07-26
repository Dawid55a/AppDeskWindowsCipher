using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace CipherLibrary.Wpf
{
    public static class AttachedProperties
    {
        // Declare the attached property
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(AttachedProperties),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedItems_PropertyChanged));

        // This method gets called when our custom property "SelectedItems" changes
        private static void SelectedItems_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                // Remove previous handler if one was attached
                dataGrid.SelectionChanged -= DataGrid_SelectionChanged;

                // If a new collection was provided, start synchronizing the selection
                if (e.NewValue is IList)
                {
                    dataGrid.SelectionChanged += DataGrid_SelectionChanged;
                }
            }
        }

        // Synchronize the selection in the DataGrid with the "SelectedItems" property
        private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is DataGrid dataGrid)) return;
            var selectedItems = GetSelectedItems(dataGrid);
            if (selectedItems == null) return;
            // Add newly selected items
            foreach (var item in e.AddedItems)
            {
                selectedItems.Add(item);
            }

            // Remove deselected items
            foreach (var item in e.RemovedItems)
            {
                selectedItems.Remove(item);
            }
        }

        // The setter for "SelectedItems"
        public static void SetSelectedItems(DependencyObject element, IList value)
        {
            element.SetValue(SelectedItemsProperty, value);
        }

        // The getter for "SelectedItems"
        public static IList GetSelectedItems(DependencyObject element)
        {
            return (IList)element.GetValue(SelectedItemsProperty);
        }
    }
}
