namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public partial class ConnectionTypeDialog : Window
    {
        private ConnectionTypeDialogModel model;
        private TypingAssistant assistant;

        public ConnectionTypeDialog(ConnectionTypeDialogModel model)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.DataContext = this.model;

            assistant = new TypingAssistant();
            assistant.Idled += assistant_Idled;

            InitializeComponent();
            ConnectionType.ItemsSource = model.ConnectionTypes;
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
        
        private void ConnectionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((string)ConnectionType.SelectedItem == "Local Machine")
            {
                ConnectionTarget.IsEnabled = false;
                ConnectionTarget.Text = Environment.MachineName;
                ConnectionTarget.Background = Background.Clone();
            }
            else if ((string)ConnectionType.SelectedItem == "Remote Connection")
            {
                ConnectionTarget.IsEnabled = true;
                ConnectionTarget.Text = String.Empty;
                ConnectionTarget.Background = Brushes.White;
            }
        }

        private void SelectMachine_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void assistant_Idled(object sender, EventArgs e)
        {
            string host = model.Host;
            bool enableSelectMachine = false;
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(host, timeout: 500);

                if (reply.Status == IPStatus.Success)
                {
                    enableSelectMachine = true;
                }
            }
            catch (PingException)
            {
                enableSelectMachine = false;
            }

            SelectMachine.Dispatcher.Invoke(() =>
            {
                // Avoid race condition where another task/thread reaches here first
                if (host == model.Host)
                {
                    SelectMachine.IsEnabled = enableSelectMachine;
                }
            });
        }

        private  void ConnectionTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ConnectionTarget.Text))
            {
                SelectMachine.IsEnabled = false;
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
