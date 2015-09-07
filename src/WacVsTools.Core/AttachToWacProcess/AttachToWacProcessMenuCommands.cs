using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Management;
using System.Text.RegularExpressions;

namespace WacVsTools.Core.AttachToWacProcess
{
    public class AttachToWacProcessMenuCommands : MenuCommandsBase
    {
        public Dictionary<string, Func<string, string>> WacProcessCommandLineToAppNameResolvers =
            new Dictionary<string, Func<string, string>> {
#if DEBUG
                { "svchost.exe", (c) => null },
#else
                { "w3wp.exe", GetAppNameFromW3wpProcessCommandLine },
#endif
            };

        public AttachToWacProcessMenuCommands(DTE2 dte, OleMenuCommandService mcs, IVsUIShell shell)
            : base(dte, mcs, shell)
        {
        }

        internal void SetupCommands()
        {
            CommandID command = new CommandID(GuidList.guidWacVsToolsCmdSet, (int)PkgCmdIDList.cmdidAttachToWacProcess);
            MenuCommand menuCommand = new OleMenuCommand((s, e) => Execute(), command);
            _mcs.AddCommand(menuCommand);
        }

        private void Execute()
        {
            var processes = GetWacProcesses();
            var selectedProcessIds = ShowWacProcessesList(processes).ToList();

            if (!selectedProcessIds.Any())
                return;

            var selectedProcesses = _dte.Debugger.LocalProcesses.Cast<EnvDTE.Process>().Where(p => selectedProcessIds.Contains(p.ProcessID));

            foreach (var process in selectedProcesses)
                process.Attach();
        }

        private IEnumerable<int> ShowWacProcessesList(IEnumerable<WacProcessInfo> processes)
        {
            var model = new AttachToWacProcessDialogModel() { Processes = processes.ToList() };
            var window = new AttachToWacProcessDialog(model);
            var result = ShowDialog(window);

            return result.HasValue && result.Value ? model.SelectedProcesses : Enumerable.Empty<int>();
        }

        private IEnumerable<WacProcessInfo> GetWacProcesses()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select ProcessId, CommandLine, Name from Win32_Process");
            ManagementObjectCollection retObjectCollection = searcher.Get();

            var processes = retObjectCollection.Cast<ManagementObject>().Select(x => new
            {
                Id = (uint)x["ProcessId"],
                Name = (string)x["Name"],
                CommandLine = (string)x["CommandLine"]
            }).ToLookup(x => x.Name);

            var wacProcesses =
                processes
                    .Where(g => WacProcessCommandLineToAppNameResolvers.ContainsKey(g.Key))
                    .SelectMany(g => {
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
    }
}
