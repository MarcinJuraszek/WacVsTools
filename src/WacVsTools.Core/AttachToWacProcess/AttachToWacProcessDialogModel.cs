namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class AttachToWacProcessDialogModel : INotifyPropertyChanged
    {
        private DebuggerEngines debuggerEngines;

        public AttachToWacProcessDialogModel(IMenuCommands menuCommands, IList<WacProcessInfo> processes)
        {
            MenuCommands = menuCommands ?? throw new ArgumentNullException(nameof(menuCommands));
            Processes = processes ?? throw new ArgumentNullException(nameof(processes));
            DebuggerEngines = DebuggerEngines.DefaultLazy;
        }

        public IList<WacProcessInfo> Processes { get; set; }

        public ISet<int> SelectedProcesses { get; set; }

        public DebuggerEngines DebuggerEngines
        {
            get
            {
                return debuggerEngines;
            }
            set
            {
                if (debuggerEngines != value)
                {
                    if (debuggerEngines != null)
                    {
                        debuggerEngines.PropertyChanged -= DebuggerEngines_PropertyChanged;
                    }

                    debuggerEngines = value;

                    debuggerEngines.PropertyChanged += DebuggerEngines_PropertyChanged;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DebuggerEngines)));
                }
            }
        }

        private void DebuggerEngines_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DebuggerEngines)));
        }

        public IMenuCommands MenuCommands { get; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class WacProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string App { get; set; }
        public string CommandLine { get; set; }
    }
}
