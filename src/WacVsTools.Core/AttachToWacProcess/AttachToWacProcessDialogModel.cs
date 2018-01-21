namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class AttachToWacProcessDialogModel : INotifyPropertyChanged
    {
        private DebuggerEngines m_debuggerEngines;

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
                return m_debuggerEngines;
            }
            set
            {
                if (m_debuggerEngines != value)
                {
                    if (m_debuggerEngines != null)
                    {
                        m_debuggerEngines.PropertyChanged -= DebuggerEngines_PropertyChanged;
                    }

                    m_debuggerEngines = value;

                    m_debuggerEngines.PropertyChanged += DebuggerEngines_PropertyChanged;

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
