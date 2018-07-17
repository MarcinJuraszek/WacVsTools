namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Management;
    using System.Text.RegularExpressions;

    public class AttachToWacProcessDialogModel : INotifyPropertyChanged
    {
        private Dictionary<string, Func<string, string>> WacProcessCommandLineToAppNameResolvers =
           new Dictionary<string, Func<string, string>> {
#if DEBUG
               { "svchost.exe", (c) => null },
#else
               { "w3wp.exe", GetAppNameFromW3wpProcessCommandLine },
#endif
           };

        private DebuggerEngines debuggerEngines;

        public AttachToWacProcessDialogModel(IMenuCommands menuCommands)
        {
            MenuCommands = menuCommands ?? throw new ArgumentNullException(nameof(menuCommands));
            Processes = new ObservableCollection<WacProcessInfo>();
            DebuggerEngines = DebuggerEngines.DefaultLazy;
        }

        public ObservableCollection<string> ConnectionTypes = new ObservableCollection<string> { "Local Machine", "Remote Connection" };

        public string Host { get; set; }
        public string Port { get; set; }

        private ObjectQuery WacObjectQuery
        {
            get
            {
                return new SelectQuery(
                    "Win32_Process",
                    string.Join(" OR ", WacProcessCommandLineToAppNameResolvers.Keys.Select(appName => $"(Name LIKE '%{appName}%')")),
                    new string[] { "ProcessId", "CommandLine", "Name" });
            }
        }

        private static string GetAppNameFromW3wpProcessCommandLine(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
                return null;

            const string appNamePattern = "-ap \"([^\"]+)\"";
            var matchResult = Regex.Match(commandLine, appNamePattern);
            return matchResult.Success ? matchResult.Groups[1].Value : null;
        }

        public ObservableCollection<WacProcessInfo> Processes { get; private set; }

        public ISet<int> SelectedProcesses { get; set; }

        /// <remarks>Returns null on an invalid connection.</remarks>
        public IEnumerable<WacProcessInfo> GetWacProcessesFromHost()
        {
            ManagementScope scope = new ManagementScope($@"\\{Host}\root\cimv2");
            try
            {
                scope.Connect();
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, WacObjectQuery);
            ManagementObjectCollection retObjectCollection = searcher.Get();

            var processes = retObjectCollection.Cast<ManagementObject>().Select(x => new
            {
                Id = (uint)x["ProcessId"],
                Name = (string)x["Name"],
                CommandLine = (string)x["CommandLine"]
            }).ToLookup(x => x.Name);

            var wacProcesses =
                processes
                    .SelectMany(g =>
                    {
                        var appNameResolver = WacProcessCommandLineToAppNameResolvers[g.Key];
                        return g.Select(p => new WacProcessInfo()
                        {
                            Id = (int)p.Id,
                            Name = p.Name,
                            App = appNameResolver(p.CommandLine),
                            CommandLine = p.CommandLine
                        });
                    })
                    .OrderBy(p => p.Name)
                    .ThenBy(p => p.App);

            return wacProcesses;
        }

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
