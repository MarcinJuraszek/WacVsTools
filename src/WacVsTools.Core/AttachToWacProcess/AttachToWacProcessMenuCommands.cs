namespace WacVsTools.Core.AttachToWacProcess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    public class AttachToWacProcessMenuCommands : MenuCommandsBase, IMenuCommands
    {
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
            var model = ShowWacProcessesList();
            if (model == null || !model.SelectedProcesses.Any())
                return;

            model.DebuggerEngines.PersistSelectionToRegistry();

            Processes envProcesses;
            if (model.Host == Environment.MachineName)
            {
                envProcesses = _dte.Debugger.LocalProcesses;
            }
            else
            {
                var debugger = (Debugger2)_dte.Debugger;
                Transport transport = debugger.Transports.Item(model.SelectedConnectionType);
                string transportQualifier = model.Host;
                if (!string.IsNullOrWhiteSpace(model.Port))
                {
                    transportQualifier += ":" + model.Port;
                }
                envProcesses = debugger.GetProcesses(transport, transportQualifier);
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

        private AttachToWacProcessDialogModel ShowWacProcessesList()
        {
            var connectionTypes = new List<string>();
            foreach (Transport transport in ((Debugger2)_dte.Debugger).Transports)
            {
                connectionTypes.Add(transport.Name);
            }

            var model = new AttachToWacProcessDialogModel(this, connectionTypes);
            var window = new AttachToWacProcessDialog(model);
            var result = ShowDialog(window);

            return result.GetValueOrDefault() ? model : null;
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
