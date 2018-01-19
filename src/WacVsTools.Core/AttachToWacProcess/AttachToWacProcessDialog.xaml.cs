using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WacVsTools.Core.AttachToWacProcess
{
    public partial class AttachToWacProcessDialog : Window
    {
        List<int> selectedProcesses;

        public AttachToWacProcessDialog(AttachToWacProcessDialogModel model)
        {
            selectedProcesses = new List<int>();
            model.SelectedProcesses = selectedProcesses;

            this.DataContext = model;

            InitializeComponent();

            NoRecordsError.Visibility = model.Processes.Count == 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private void Processes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProcesses.AddRange(e.AddedItems.Cast<WacProcessInfo>().Select(x => x.Id));
            foreach (var removedItem in e.RemovedItems.Cast<WacProcessInfo>())
                selectedProcesses.Remove(removedItem.Id);

            btnOk.IsEnabled = selectedProcesses.Count > 0;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

		private void Processes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var grid = sender as DataGrid;
			var selectedProcessInfo = grid.SelectedItem as WacProcessInfo;

			if (selectedProcessInfo != null)
			{
				selectedProcesses.Clear();
				selectedProcesses.Add(selectedProcessInfo.Id);

				DialogResult = true;
				Close();
			}
		}
	}
}
