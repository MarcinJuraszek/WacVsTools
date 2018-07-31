using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WacVsTools.Core.AttachToWacProcess;

namespace WacVsTools.Test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var model = new AttachToWacProcessDialogModel(new TestMenuCommands(), new string[] { "Default", "Remote Computer" });
            var window = new AttachToWacProcessDialog(model);
            window.ShowDialog();
        }
    }

    internal sealed class TestMenuCommands : IMenuCommands
    {
        public DebuggerEngines ShowSelectDebuggerEngineDialog(DebuggerEngines current)
        {
            if (current.IsLazy)
            {
                current = new DebuggerEngines(isAutomatic: true, manualSelection: SelectDebuggerEngineDialogModelDesignSample.SampleEngines);
            }

            var model = new SelectDebuggerEngineDialogModel(current);
            var window = new SelectDebuggerEngineDialog(model);
            return window.ShowDialog().GetValueOrDefault(false) ? model.DebuggerEngines : current;
        }
    }
}
