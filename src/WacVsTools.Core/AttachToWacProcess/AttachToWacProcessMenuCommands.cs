namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Management;
    using System.Text.RegularExpressions;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    public class AttachToWacProcessMenuCommands : MenuCommandsBase, IMenuCommands
    {
        public Dictionary<string, Func<string, string>> WacProcessCommandLineToAppNameResolvers =
            new Dictionary<string, Func<string, string>> {
#if DEBUG
                { "svchost.exe", (c) => null },
#else
                { "w3wp.exe", GetAppNameFromW3wpProcessCommandLine },
#endif
            };

        private ObjectQuery WacObjectQuery
        {
            get
            {
                string formatQuery = @"
                    SELECT ProcessId, CommandLine, Name
                    FROM Win32_Process
                    WHERE
                        {0}";

                var whereClauses = WacProcessCommandLineToAppNameResolvers.Keys.Select(appName => $"(Name LIKE '%{appName}%')");
                var query = String.Format(formatQuery, string.Join(" OR\n", whereClauses));
                return new ObjectQuery(query);
            }
        }

        public AttachToWacProcessMenuCommands(DTE2 dte, OleMenuCommandService mcs, IVsUIShell shell)
            : base(dte, mcs, shell)
        {
        }

        public DebuggerEngines ShowSelectDebuggerEngineDialog(DebuggerEngines current)
        {
            bool firstTime = current.IsLazy;
            if (firstTime)
            {
                current = new DebuggerEngines(
                    isAutomatic: true,
                    manualSelection: GetAvailableDebuggerEngines());
                current.RestoreSelectionFromRegistry();
            }

            var model = new SelectDebuggerEngineDialogModel(current);
            var window = new SelectDebuggerEngineDialog(model);
            var result = ShowDialog(window).GetValueOrDefault();

            if (firstTime && !result)
            {
                // this covers an edge case with the first run, since we restore the last selection from the registry,
                // if the user cancels out we still want to leave the selected engine to be automatic.
                current.IsAutomatic = true;
            }

            return result ? model.DebuggerEngines : current;
        }

        internal void SetupCommands()
        {
            CommandID command = new CommandID(GuidList.guidWacVsToolsCmdSet, (int)PkgCmdIDList.cmdidAttachToWacProcess);
            MenuCommand menuCommand = new OleMenuCommand((s, e) => Execute(), command);
            _mcs.AddCommand(menuCommand);
        }

        private void Execute()
        {
            var connectionModel = ShowConnectionTypeSelector();
            var processes = GetWacProcesses(connectionModel.Host);
            var model = ShowWacProcessesList(processes);

            if (model == null || !model.SelectedProcesses.Any())
                return;

            model.DebuggerEngines.PersistSelectionToRegistry();

            Processes envProcesses;
            if (connectionModel.Host == Environment.MachineName)
            {
                envProcesses = _dte.Debugger.LocalProcesses;
            }
            else
            {
                Debugger2 debugger = (Debugger2)_dte.Debugger;
                Transport transport = debugger.Transports.Item("Remote (No Authentication)");
                envProcesses = debugger.GetProcesses(transport, connectionModel.Host);
            }

            var manuallySelectedDebuggerEngineIds = model.DebuggerEngines.ManuallySelectedEngines.Select(debuggerEngine => debuggerEngine.ID).ToArray();
            var selectedProcesses = envProcesses.Cast<Process2>().Where(p => model.SelectedProcesses.Contains(p.ProcessID));
            foreach (var process in selectedProcesses)
            {
                if (model.DebuggerEngines.IsAutomatic)
                    process.Attach();
                else
                    process.Attach2(manuallySelectedDebuggerEngineIds);
            }
        }

        private ConnectionTypeDialogModel ShowConnectionTypeSelector()
        {
            var model = new ConnectionTypeDialogModel();
            var window = new ConnectionTypeDialog(model);
            var result = ShowDialog(window);

            return result.GetValueOrDefault() ? model : null;
        }

        private AttachToWacProcessDialogModel ShowWacProcessesList(IEnumerable<WacProcessInfo> processes)
        {
            var model = new AttachToWacProcessDialogModel(this, processes.ToList());
            var window = new AttachToWacProcessDialog(model);
            var result = ShowDialog(window);

            return result.GetValueOrDefault() ? model : null;
        }

        private IEnumerable<WacProcessInfo> GetWacProcesses(string computerName)
        {
            ManagementScope scope = new ManagementScope($"\\\\{computerName}\\root\\cimv2");
            scope.Connect();

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

        private static string GetAppNameFromW3wpProcessCommandLine(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
                return null;

            const string appNamePattern = "-ap \"([^\"]+)\"";
            var matchResult = Regex.Match(commandLine, appNamePattern);
            return matchResult.Success ? matchResult.Groups[1].Value : null;
        }

        private IList<DebuggerEngine> GetAvailableDebuggerEngines()
        {
            var engines = ((EnvDTE100.Debugger5)_dte.Debugger).Transports.Item("Default").Engines;

            var availableEngines = new List<DebuggerEngine>();
            foreach (Engine engine in engines)
            {
                availableEngines.Add(new DebuggerEngine(engine.Name, engine.ID, isSelected: false));
            }

            availableEngines.Sort();

            return availableEngines;
        }
    }
}
