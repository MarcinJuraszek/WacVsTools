namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    public partial class AttachToWacProcessDialog : Window
    {
        private AttachToWacProcessDialogModel m_model;
        private HashSet<int> m_selectedProcesses;

        public AttachToWacProcessDialog(AttachToWacProcessDialogModel model)
        {
            m_model = model ?? throw new ArgumentNullException(nameof(model));

            m_selectedProcesses = new HashSet<int>();
            model.SelectedProcesses = m_selectedProcesses;

            this.DataContext = m_model;

            InitializeComponent();

            NoRecordsError.Visibility = model.Processes.Count == 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private void Processes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_selectedProcesses.UnionWith(e.AddedItems.Cast<WacProcessInfo>().Select(x => x.Id));
            foreach (var removedItem in e.RemovedItems.Cast<WacProcessInfo>())
                m_selectedProcesses.Remove(removedItem.Id);

            btnOk.IsEnabled = m_selectedProcesses.Count > 0;
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
                m_selectedProcesses.Clear();
                m_selectedProcesses.Add(selectedProcessInfo.Id);

                DialogResult = true;
                Close();
            }
        }

        private void SelectEngines_Click(object sender, RoutedEventArgs e)
        {
            m_model.DebuggerEngines = m_model.MenuCommands.ShowSelectDebuggerEngineDialog(m_model.DebuggerEngines.Clone());
        }
    }
}
