namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    public class ConnectionTypeDialogModel : INotifyPropertyChanged
    {
        public ConnectionTypeDialogModel() { }

        public string Host { get; set; }
        public string Port { get; set; }

        public ObservableCollection<string> ConnectionTypes = new ObservableCollection<string> { "Local Machine", "Remote Connection" };

        public event PropertyChangedEventHandler PropertyChanged;
        private void Host_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DebuggerEngines)));
        }
    }
}
