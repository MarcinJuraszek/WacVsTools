using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.WacVsTools
{
    public class MenuCommandsBase
    {
        protected readonly DTE2 _dte;
        protected readonly OleMenuCommandService _mcs;
        protected IVsUIShell _shell;

        public MenuCommandsBase(EnvDTE80.DTE2 dte, VisualStudio.Shell.OleMenuCommandService mcs, VisualStudio.Shell.Interop.IVsUIShell shell)
        {
            _dte = dte;
            _mcs = mcs;
            _shell = shell;
        }


        protected bool? ShowDialog(Window window)
        {
            try
            {
                if (ErrorHandler.Failed(_shell.EnableModeless(0)))
                    return null;
                else
                    return window.ShowDialog();
            }
            finally
            {
                _shell.EnableModeless(1);
            }
        }
    }
}
