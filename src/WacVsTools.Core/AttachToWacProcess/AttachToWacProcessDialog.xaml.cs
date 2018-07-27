namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public partial class AttachToWacProcessDialog : Window
    {
        private AttachToWacProcessDialogModel model;
        private HashSet<int> selectedProcesses;
        private TypingAssistant assistant;

        public AttachToWacProcessDialog(AttachToWacProcessDialogModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));

            selectedProcesses = new HashSet<int>();
            model.SelectedProcesses = selectedProcesses;

            this.DataContext = this.model;

            assistant = new TypingAssistant();
            assistant.Idled += assistant_Idled;

            InitializeComponent();
            NoRecordsError.Visibility = model.Processes.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            ConnectionType.ItemsSource = model.ConnectionTypes;
        }

        private void Processes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedProcesses.UnionWith(e.AddedItems.Cast<WacProcessInfo>().Select(x => x.Id));
            foreach (var removedItem in e.RemovedItems.Cast<WacProcessInfo>())
                selectedProcesses.Remove(removedItem.Id);

            btnOk.IsEnabled = selectedProcesses.Count > 0;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            model.Processes.Clear();
            UpdateConnection(validConnection: false);
            assistant.TextChanged();
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

        private void SelectEngines_Click(object sender, RoutedEventArgs e)
        {
            model.DebuggerEngines = model.MenuCommands.ShowSelectDebuggerEngineDialog(model.DebuggerEngines.Clone());
        }

        private void ConnectionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            model.SelectedConnectionType = ConnectionType.SelectedItem.ToString();

            if ((string)ConnectionType.SelectedItem == "Default")
            {
                ConnectionTarget.Text = Environment.MachineName;
            }
        }

        private void ConnectionTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            model.Processes.Clear();
            UpdateConnection(validConnection: false);
            if (string.IsNullOrWhiteSpace(ConnectionTarget.Text))
            {
                return;
            }

            string[] address = ConnectionTarget.Text.Split(':');
            model.Host = address[0];
            if (address.Length >= 2)
            {
                model.Port = address[1];
            }

            assistant.TextChanged();
        }

        private void assistant_Idled(object sender, EventArgs e)
        {
            string host = model.Host;
            bool machineExists = false;
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(host, timeout: 500);

                if (reply.Status == IPStatus.Success)
                {
                    machineExists = true;
                }
            }
            catch (PingException)
            {
                machineExists = false;
            }

            Processes.Dispatcher.Invoke(() =>
            {
                if (machineExists && host == model.Host)
                {
                    var processes = model.GetWacProcessesFromHost();
                    if (processes != null)
                    {
                        foreach (var process in processes)
                        {
                            model.Processes.Add(process);
                        }
                    }
                    UpdateConnection(validConnection: processes != null);
                }
            });
        }

        private void UpdateConnection(bool validConnection)
        {
            StatusLight.Fill = validConnection ? Brushes.Green : Brushes.Red;
            btnRefresh.IsEnabled = validConnection;
            NoRecordsError.Visibility = validConnection && model.Processes.Count == 0 ? Visibility.Visible : Visibility.Hidden;
        }
    }

    // Based on https://stackoverflow.com/questions/33776387/dont-raise-textchanged-while-continuous-typing
    public class TypingAssistant
    {
        public event EventHandler Idled = delegate { };
        private readonly int timeoutMilliseconds;
        Timer timer;

        public TypingAssistant(int timeoutMilliseconds = 600)
        {
            this.timeoutMilliseconds = timeoutMilliseconds;
            this.timer = new Timer(p =>
            {
                Idled(this, EventArgs.Empty);
            });
        }
        public void TextChanged()
        {
            timer.Change(timeoutMilliseconds, Timeout.Infinite);
        }
    }
}
