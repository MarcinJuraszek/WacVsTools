using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Microsoft.WacVsTools.AttachToWacProcess
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
        }

        private void Processes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProcesses.AddRange(e.AddedItems.Cast<WacProcessInfo>().Select(x => x.Id));
            foreach (var removedItem in e.RemovedItems.Cast<WacProcessInfo>())
                selectedProcesses.Remove(removedItem.Id);
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
    }
}
